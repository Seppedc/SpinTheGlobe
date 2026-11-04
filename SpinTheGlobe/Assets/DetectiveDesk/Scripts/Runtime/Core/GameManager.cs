using System;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Composition root of the game. Owns the economy (wallet, inventory), the stat sheet and the
    /// skill-tree state, creates the feature systems on its own GameObject, and saves/loads progress.
    /// Scene references (globe, map, figurines, coffer slots, legacy HUD) are wired in the scene.
    /// </summary>
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Scene")]
        public DetectiveGame legacy;
        public GlobeController globe;
        public WallMapManager map;
        public FigurinePedestal[] figurines;
        public AudioManager audioManager;
        public DeskAssetPresentation archive;
        public PinCofferSlot[] pinSlots;

        [Header("Data (optional: defaults are used when empty)")]
        public GameBalance balance;
        public SkillTreeAsset skillTreeAsset;

        [Header("Saving")]
        [Tooltip("Load and write the save file in Application.persistentDataPath.")]
        public bool useSaveFile = true;

        public Wallet Wallet { get; } = new Wallet();
        public StatSheet Stats { get; } = new StatSheet();
        public SkillTreeState Skills { get; } = new SkillTreeState();
        public GoldShopState Shop { get; } = new GoldShopState();
        public Inventory Inventory { get; } = new Inventory();
        public GameBalance Balance { get; private set; }
        public SkillTreeAsset Tree { get; private set; }

        public CaseFileSystem Cases { get; private set; }
        public WhiskeyGlass Whiskey { get; private set; }
        public LootboxSystem Lootbox { get; private set; }
        public PaperBallThrower PaperBalls { get; private set; }
        public StatueHammer Hammer { get; private set; }
        public PinBox PinBox { get; private set; }
        public ClickRouter Clicks { get; private set; }
        public GameHUD Hud { get; private set; }
        public SkillTreeView TreeView { get; private set; }
        public DeskInfoSystem DeskInfo { get; private set; }
        public DeskLamp Lamp { get; private set; }
        public SettingsMenu Settings { get; private set; }
        public MusicPlayer Music { get; private set; }
        public GoldShopView ShopView { get; private set; }

        // ------------------------------------------------------------------ prestige state
        /// <summary>Money earned since the last retirement (drives the prestige gold).</summary>
        public double RunEarnings { get; private set; }
        /// <summary>How many times the agency has been retired.</summary>
        public int Prestiges { get; private set; }
        /// <summary>Gold the player would get for retiring right now.</summary>
        public double PrestigeGold => Prestige.GoldFor(RunEarnings, Balance);
        public bool CanRetire => PrestigeGold >= 1;
        bool retiring;
        /// <summary>Message shown once the new run has loaded after retiring (survives the scene reload).</summary>
        static string pendingHint;

        /// <summary>The detective rank earned by retiring (title + permanent income bonus, see GameBalance.ranks).</summary>
        public GameBalance.DetectiveRank Rank => DetectiveRanks.For(Balance, Prestiges);
        public bool TryNextRank(out GameBalance.DetectiveRank next) => DetectiveRanks.TryNext(Balance, Prestiges, out next);

        public double Money => Wallet.Money;
        public double PlayTime { get; private set; }
        public Camera GameCamera => globe && globe.gameCamera ? globe.gameCamera : Camera.main;

        public bool WorldInputBlocked =>
            (archive && archive.IsArchiveOpen) || (TreeView && TreeView.IsOpen) || (Settings && Settings.IsOpen) || (ShopView && ShopView.IsOpen) || (PaperBalls && PaperBalls.IsAiming) ||
            (legacy && legacy.settingsPanel && legacy.settingsPanel.activeInHierarchy);

        public event Action Changed;
        public event Action<double> CashAwarded;
        /// <summary>Every gold payout (statues, lootboxes, Press Coverage).</summary>
        public event Action<double> GoldAwarded;
        public PayoutPopups Popups { get; private set; }

        SaveStore store;
        float autosaveTimer;
        double incomeAccumulator;
        float incomeSampleTimer;

        /// <summary>Recent earnings per second from the globe and pins (≈30 s moving average). Drives case rewards.</summary>
        public double IncomeRate { get; private set; }

        /// <summary>Multiplier on all globe-related earnings: Globe earnings × the golden income multiplier.</summary>
        public double IncomeScale => Stats.Get(StatId.GlobeMultiplier) * Stats.Get(StatId.IncomeMultiplier);
        bool loaded;

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            Balance = balance ? balance : ScriptableObject.CreateInstance<GameBalance>();
            if (skillTreeAsset) Tree = skillTreeAsset;
            else
            {
                Tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
                Tree.SetNodes(DefaultSkillTreeFactory.Create());
            }
            Balance.ApplyTo(Stats);
            // Gold shop bonuses count like skills; statue and Press Coverage nodes stay hidden until their unlock is bought.
            Stats.ExtraSource = ExtraModifiers;
            Skills.UnlockCheck = Shop.Owns;

            if (legacy) { legacy.Gameplay = this; legacy.globeSpeed = 0; }
            if (!globe) Debug.LogError("GameManager needs its GlobeController reference.", this);

            Clicks = Ensure<ClickRouter>();
            PinBox = Ensure<PinBox>();
            Cases = Ensure<CaseFileSystem>();
            Whiskey = Ensure<WhiskeyGlass>();
            Lootbox = Ensure<LootboxSystem>();
            PaperBalls = Ensure<PaperBallThrower>();
            Hammer = Ensure<StatueHammer>();
            Hud = Ensure<GameHUD>();
            TreeView = Ensure<SkillTreeView>();
            DeskInfo = Ensure<DeskInfoSystem>();
            Lamp = Ensure<DeskLamp>();
            Settings = Ensure<SettingsMenu>();
            Music = Ensure<MusicPlayer>();
            ShopView = Ensure<GoldShopView>();
            Popups = Ensure<PayoutPopups>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Ensure<DevCheats>();
#endif

            Wallet.Changed += (c, b, d) => { if (c == Currency.Money && d > 0) RunEarnings += d; Notify(); };
            Inventory.Changed += Notify;
        }

        /// <summary>
        /// All statue pedestals in the scene: the ones wired in <see cref="figurines"/> first (their order is the save order),
        /// then any other FigurinePedestal found, sorted by name. Adding a statue = adding a pedestal to the scene.
        /// </summary>
        FigurinePedestal[] CollectStatues()
        {
            var list = new List<FigurinePedestal>();
            if (figurines != null) foreach (var f in figurines) if (f && !list.Contains(f)) list.Add(f);
            var found = new List<FigurinePedestal>(FindObjectsByType<FigurinePedestal>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
            found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            foreach (var f in found) if (!list.Contains(f)) list.Add(f);
            return list.ToArray();
        }

        void OnEnable() { if (globe) globe.RevolutionsCompleted += Revolution; }
        void OnDisable() { if (globe) globe.RevolutionsCompleted -= Revolution; }

        void Start()
        {
            figurines = CollectStatues();
            for (int i = 0; i < figurines.Length; i++) figurines[i].Bind(this, i);
            Clicks.Initialize(this, GameCamera);

            LoadGame();
            Stats.Rebuild(Tree, Skills);

            PinBox.Initialize(this);
            Cases.Initialize(this);
            Whiskey.Initialize(this);
            Lootbox.Initialize(this);
            PaperBalls.Initialize(this);
            Hammer.Initialize(this);
            Hud.Initialize(this);
            TreeView.Initialize(this);
            if (map) map.Bind(this);
            Lamp.Initialize(this);
            DeskInfo.Initialize(this);
            Settings.Initialize(this);
            Music.Initialize(this);
            ShopView.Initialize(this);
            Popups.Initialize(this);

            loaded = true;
            EventBus.Publish(new GameLoaded());
            Notify();
            if (!string.IsNullOrEmpty(pendingHint)) { ShowHint(pendingHint); pendingHint = null; }
            else ShowHint(Skills.Owns(DefaultSkillTreeFactory.RootId)
                ? "Welcome back, " + Rank.title.ToLowerInvariant() + "."
                : "Drag or click the globe to spin it. Earn " + NumberFormatter.Money(Tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0)) + " to open the skill tree (T).");
        }

        void Update()
        {
            if (!loaded) return;
            PlayTime += Time.unscaledDeltaTime;
            incomeSampleTimer += Time.deltaTime;
            if (incomeSampleTimer >= 1f)
            {
                double sample = incomeAccumulator / incomeSampleTimer;
                IncomeRate = IncomeRate <= 0 ? sample : IncomeRate + (sample - IncomeRate) * (incomeSampleTimer / 30.0);
                incomeAccumulator = 0;
                incomeSampleTimer = 0;
            }
            autosaveTimer += Time.unscaledDeltaTime;
            if (useSaveFile && autosaveTimer >= Balance.autosaveInterval) { autosaveTimer = 0; SaveGame(); }

            if (legacy)
            {
                legacy.moneyPerMinute = EstimatedIncomePerMinute();
                legacy.RefreshHUD();
            }
        }

        void OnApplicationPause(bool paused) { if (paused && loaded && useSaveFile) SaveGame(); }
        void OnApplicationQuit() { if (loaded && useSaveFile) SaveGame(); }

        T Ensure<T>() where T : Component
        {
            var c = GetComponent<T>();
            return c ? c : gameObject.AddComponent<T>();
        }

        // ------------------------------------------------------------------ economy
        void Revolution(long count)
        {
            CreditIncome(count * Balance.revolutionReward * IncomeScale);
            float fill = (float)(count * Balance.statueGoldFillPerRevolution * Stats.Get(StatId.StatueGoldFillRate));
            if (figurines != null) foreach (var f in figurines) if (f) f.AddGold(fill);
            EventBus.Publish(new GlobeRevolutions(count));
        }

        public void Credit(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
            Wallet.Add(Currency.Money, amount);
            CashAwarded?.Invoke(amount);
            if (audioManager) audioManager.Payout();
        }

        /// <summary>Recurring earnings (globe turns, pin passes): credited and counted in <see cref="IncomeRate"/>.</summary>
        public void CreditIncome(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
            incomeAccumulator += amount;
            Credit(amount);
        }

        public void CreditGold(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
            Wallet.Add(Currency.Gold, amount);
            GoldAwarded?.Invoke(amount);
            if (audioManager) audioManager.Payout();
        }

        public bool TrySpend(double amount) => Wallet.TrySpend(Currency.Money, amount);

        public double EstimatedIncomePerMinute()
        {
            if (!globe) return 0;
            double perTurn = Balance.revolutionReward * IncomeScale + (map ? map.ActiveValuePerRevolution : 0);
            return perTurn * globe.CurrentVelocity / 6.0; // revolutions per minute = °/s × 60 / 360
        }

        // ------------------------------------------------------------------ skill tree
        public bool PurchaseSkill(string id)
        {
            if (!Skills.TryPurchase(Tree, id, Wallet, Stats, out string reason)) { ShowHint(reason); return false; }
            var node = Tree.Find(id);
            if (globe) globe.EnforceLimits();
            if (PinBox) PinBox.Refresh();
            if (audioManager) audioManager.Click();
            ShowHint(node.title + (node.maxRank > 1 ? " rank " + Skills.Rank(id) : "") + " unlocked.");
            Notify();
            return true;
        }

        // ------------------------------------------------------------------ pins
        public int UnlockedPins => Mathf.Clamp((int)Math.Round(Stats.Get(StatId.PinSlots)), 0, DefaultSkillTreeFactory.MaxPins);
        public int PinPasses(int index) => Math.Max(1, (int)Math.Round(Stats.Get(StatId.PinDurability, index)));
        public int PinDurabilityRank(int index) => Skills.Rank(DefaultSkillTreeFactory.PinDurabilityId(index + 1));
        public double PinPayoutMultiplier(int index) => Stats.Get(StatId.PinPayout, index);

        public void FirePin()
        {
            if (!map) return;
            string problem = map.FireNext();
            if (problem != null) ShowHint(problem);
        }

        // ------------------------------------------------------------------ legacy entry points (desk clicks)
        public void SelectPin(PinCofferSlot slot) => FirePin();
        public void ArmLauncher() => FirePin();
        public void InspectFigurine(FigurinePedestal pedestal)
        {
            pedestal = StatueUnderMouse(pedestal);
            if (pedestal) pedestal.Click();
            else ShowHint("Statue pieces come from lootboxes. A finished statue fills with gold as the globe spins.");
        }

        /// <summary>
        /// Statues stand close together and overlap on screen, so the clicked collider can belong to the statue in
        /// front. Of the statues under the mouse, take the one whose middle is closest to the cursor.
        /// </summary>
        FigurinePedestal StatueUnderMouse(FigurinePedestal fallback)
        {
            var cam = GameCamera;
            if (!cam || figurines == null) return fallback;
            Vector2 mouse = Input.mousePosition;
            FigurinePedestal best = fallback;
            float bestDistance = float.MaxValue;
            foreach (var f in figurines)
            {
                if (!f) continue;
                var b = f.VisualBounds;
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
                for (int i = 0; i < 8; i++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 sp = cam.WorldToScreenPoint(corner);
                    min = Vector2.Min(min, sp); max = Vector2.Max(max, sp);
                }
                if (mouse.x < min.x || mouse.x > max.x || mouse.y < min.y || mouse.y > max.y) continue;
                float d = Vector2.Distance(mouse, (min + max) * 0.5f);
                if (d < bestDistance) { bestDistance = d; best = f; }
            }
            return best;
        }

        public void ShowHint(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            if (legacy) legacy.ShowHint(message);
            if (Hud) Hud.Toast(message);
        }
        public void Notify() { Changed?.Invoke(); }

        // ------------------------------------------------------------------ save / load
        SaveStore Store => store ?? (store = SaveStore.Default);

        void LoadGame()
        {
            if (useSaveFile && Store.TryLoad(out var data))
            {
                Wallet.Load(data.money, data.gold, data.lifetimeMoney, data.lifetimeGold);
                Shop.Load(data.goldShop);
                Prestiges = Mathf.Max(0, data.prestiges);
                // Saves from before prestige existed: the whole history counts as this run.
                RunEarnings = data.runMoney > 0 || Prestiges > 0 ? data.runMoney : data.lifetimeMoney;
                MigrateGoldNodes(data);
                Skills.Load(Tree, data.skills);
                Inventory.Load(data.iceCubes, data.paperBalls, data.sparePieces);
                PlayTime = data.playTimeSeconds;
                Lootbox.LoadState(data);
                Cases.LoadState(data);
                Whiskey.LoadState(data);
                Lamp.LoadState(data);
                if (figurines != null)
                    for (int i = 0; i < figurines.Length && i < data.statues.Count; i++)
                        if (figurines[i]) figurines[i].LoadState(data.statues[i]);
            }
            else
            {
                Wallet.Load(Balance.startingMoney, Balance.startingGold, Balance.startingMoney, Balance.startingGold);
            }
        }

        public void SaveGame()
        {
            if (retiring) return;
            try
            {
                var data = new SaveData
                {
                    money = Wallet.Money, gold = Wallet.Gold, lifetimeMoney = Wallet.LifetimeMoney, lifetimeGold = Wallet.LifetimeGold,
                    runMoney = RunEarnings, prestiges = Prestiges, goldShop = Shop.ToSave(),
                    skills = Skills.ToSave(), playTimeSeconds = PlayTime,
                    iceCubes = Inventory.IceCubes, paperBalls = Inventory.PaperBalls, sparePieces = Inventory.SparePieces,
                    statues = new List<StatueState>(),
                };
                Lootbox.SaveState(data);
                Cases.SaveState(data);
                Whiskey.SaveState(data);
                Lamp.SaveState(data);
                if (figurines != null) foreach (var f in figurines) data.statues.Add(f ? f.SaveState() : default);
                Store.Save(data);
            }
            catch (Exception e) { Debug.LogWarning("Saving failed: " + e.Message); }
        }

        /// <summary>
        /// Gold upgrades used to be ★ nodes in the skill tree. A save that owns one gets the matching Gold shop item,
        /// and a save from before statues were locked keeps the statues it already started.
        /// </summary>
        void MigrateGoldNodes(SaveData data)
        {
            if (data.goldShop != null && data.goldShop.Count > 0) return;
            var map = new Dictionary<string, string>
            {
                ["globe_golden_meridian"] = "golden_meridian", ["pin_steel_tips"] = "steel_tips", ["pin_twin_barrel"] = "twin_barrel",
                ["case_press"] = GoldShopCatalog.PressCoverage, ["statue_fast_casting"] = "fast_casting",
                ["loot_double"] = "double_box", ["whiskey_bottomless"] = "bottomless_glass",
            };
            var grant = new List<SkillRank>();
            if (data.skills != null)
                foreach (var s in data.skills)
                    if (s.rank > 0 && map.TryGetValue(s.id, out var shopId)) grant.Add(new SkillRank { id = shopId, rank = 1 });
            if (Prestiges == 0 && data.statues != null)
                for (int i = 0; i < data.statues.Count; i++)
                    if (data.statues[i].pieces > 0)
                        for (int k = 0; k <= Mathf.Min(i, 2); k++) grant.Add(new SkillRank { id = GoldShopCatalog.StatueUnlockId(k), rank = 1 });
            if (grant.Count > 0) Shop.Load(grant);
        }

        // ------------------------------------------------------------------ prestige
        /// <summary>
        /// Retire the agency: bank the prestige gold and start a new run. Money, the skill tree, pins, cases, the whiskey
        /// glass, lootbox prices and items reset; gold, the Gold shop, statues, settings and the lamp are kept.
        /// Fresh-start items from the Gold shop give the new run its head start.
        /// </summary>
        public void Retire()
        {
            if (!CanRetire) { ShowHint("Earn " + NumberFormatter.Money(Balance.prestigeThreshold) + " in this run to retire."); return; }
            if (!useSaveFile) { ShowHint("Retiring needs the save file (useSaveFile is off)."); return; }
            double gain = PrestigeGold;
            var data = new SaveData
            {
                money = Balance.startingMoney + GoldShopCatalog.SeedMoneyAmounts[Mathf.Clamp(Shop.Rank(GoldShopCatalog.SeedMoney), 0, GoldShopCatalog.SeedMoneyAmounts.Length - 1)],
                gold = Wallet.Gold + gain,
                lifetimeMoney = Wallet.LifetimeMoney,
                lifetimeGold = Wallet.LifetimeGold + gain,
                runMoney = 0,
                prestiges = Prestiges + 1,
                goldShop = Shop.ToSave(),
                skills = FreshStartSkills(),
                playTimeSeconds = PlayTime,
                sparePieces = Inventory.SparePieces,
                statues = new List<StatueState>(),
            };
            Lamp.SaveState(data);
            if (figurines != null) foreach (var f in figurines) data.statues.Add(f ? f.SaveState() : default);
            try { Store.Save(data); }
            catch (Exception e) { Debug.LogWarning("Retiring failed: " + e.Message); ShowHint("Retiring failed: the game could not be saved."); return; }
            retiring = true;
            var before = Rank;
            var after = DetectiveRanks.For(Balance, Prestiges + 1);
            pendingHint = "Agency retired: +" + NumberFormatter.Format(gain) + " gold." +
                (after.title != before.title ? " Promoted to " + after.title + (after.incomeBonus > 0 ? " (+" + Mathf.RoundToInt(after.incomeBonus * 100) + "% all income)." : ".") : "");
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
        }

        /// <summary>Skill ranks a new run starts with, from the Gold shop's fresh-start items.</summary>
        List<SkillRank> FreshStartSkills()
        {
            var list = new List<SkillRank>();
            if (!Shop.Owns(GoldShopCatalog.KeptLedger)) return list;
            list.Add(new SkillRank { id = DefaultSkillTreeFactory.RootId, rank = 1 });
            if (Shop.Owns(GoldShopCatalog.WoundClockwork)) list.Add(new SkillRank { id = "globe_auto", rank = GoldShopCatalog.WoundClockworkRanks });
            if (Shop.Owns(GoldShopCatalog.SparePin)) list.Add(new SkillRank { id = DefaultSkillTreeFactory.PinId(1), rank = 1 });
            return list;
        }

        /// <summary>Permanent modifiers outside the skill tree: Gold shop items and the detective rank bonus.</summary>
        IEnumerable<(StatModifier modifier, int rank)> ExtraModifiers()
        {
            foreach (var m in Shop.Modifiers()) yield return m;
            foreach (var m in DetectiveRanks.Modifiers(Balance, Prestiges)) yield return m;
        }

        // ------------------------------------------------------------------ gold shop
        public bool PurchaseShopItem(string id)
        {
            if (!Shop.TryBuy(id, Wallet, out string reason)) { ShowHint(reason); return false; }
            Stats.Rebuild(Tree, Skills);
            if (globe) globe.EnforceLimits();
            if (PinBox) PinBox.Refresh();
            if (figurines != null) foreach (var f in figurines) if (f) f.RefreshVisual();
            if (audioManager) audioManager.Click();
            var item = GoldShopCatalog.Find(id);
            ShowHint(item.title + (item.maxRank > 1 ? " rank " + Shop.Rank(id) : "") + " bought.");
            Notify();
            return true;
        }

        /// <summary>Deletes the save file and reloads the scene (debug / settings).</summary>
        public void ResetProgress()
        {
            useSaveFile = false;
            Store.Delete();
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
        }
    }
}
