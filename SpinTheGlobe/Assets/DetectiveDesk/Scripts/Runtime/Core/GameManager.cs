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
        public PlayerGuidanceState Guidance { get; } = new PlayerGuidanceState();
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
        public LootboxOpeningView LootboxView { get; private set; }
        public FirstUseGuidance GuidanceView { get; private set; }

        // ------------------------------------------------------------------ prestige state
        /// <summary>Money earned since the last retirement (drives the prestige gold).</summary>
        public double RunEarnings { get; private set; }
        /// <summary>How many times the agency has been retired.</summary>
        public int Prestiges { get; private set; }
        /// <summary>Gold the player would get for retiring right now.</summary>
        public double PrestigeGold => Prestige.GoldFor(RunEarnings, Balance, Prestiges);
        public double PrestigeRequirement => Prestige.RequirementFor(Prestiges, Balance);
        public bool IsFinalRank => Prestige.IsFinalRank(Prestiges, Balance);
        public bool GameCompleted { get; private set; }
        public bool CanRetire => !GameCompleted && PrestigeGold >= 1;
        public double PrestigeIncomeBonus => Prestige.IncomeBonusFor(Prestiges, Balance);
        public double GlobalIncomeMultiplier => Stats.Permanent(StatId.IncomeMultiplier);
        public double NextRunIncomeMultiplier => Prestige.IncomeMultiplierFor(Prestiges + 1, Balance, Shop);
        public double AgencyIncomeBonus
        {
            get
            {
                double bonus = 0;
                var item = Shop.Find(GoldShopCatalog.Reputation);
                if (item != null) foreach (var modifier in item.modifiers)
                    if (modifier.stat == StatId.IncomeMultiplier && modifier.kind == ModifierKind.Percent)
                        bonus += modifier.valuePerRank * Shop.Rank(item.id);
                return bonus;
            }
        }
        public string GlobalIncomeSummary => "Global money ×" + GlobalIncomeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            + "  ·  Prestige +" + (PrestigeIncomeBonus * 100).ToString("0.#") + "%"
            + "  ·  Agency +" + (AgencyIncomeBonus * 100).ToString("0.#") + "%";
        public string RetirementIncomeSummary => "+" + (Balance.prestigeIncomeBonusPerRetirement * 100).ToString("0.#")
            + "% permanent money income. Next run: ×" + NextRunIncomeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ".";
        bool retiring;
        /// <summary>Message shown once the new run has loaded after retiring (survives the scene reload).</summary>
        static string pendingHint;

        /// <summary>The detective rank earned by retiring (title, see GameBalance.ranks).</summary>
        public GameBalance.DetectiveRank Rank => DetectiveRanks.For(Balance, Prestiges);
        public bool TryNextRank(out GameBalance.DetectiveRank next) => DetectiveRanks.TryNext(Balance, Prestiges, out next);

        public double Money => Wallet.Money;
        public double PlayTime { get; private set; }
        public Camera GameCamera => globe && globe.gameCamera ? globe.gameCamera : Camera.main;

        public bool WorldInputBlocked =>
            (LootboxView && LootboxView.IsOpen) || (archive && archive.IsArchiveOpen) || (TreeView && TreeView.IsOpen) || (Settings && Settings.IsOpen) || (ShopView && ShopView.IsOpen) || (PaperBalls && (PaperBalls.IsAiming || PaperBalls.IsThrowing)) ||
            (legacy && legacy.settingsPanel && legacy.settingsPanel.activeInHierarchy);

        public event Action Changed;
        public event Action<double> CashAwarded;
        /// <summary>Every gold payout (statues, lootboxes, Press Coverage).</summary>
        public event Action<double> GoldAwarded;
        public PayoutPopups Popups { get; private set; }
        /// <summary>World point the current payout comes from (null = the globe), read by the payout popups.</summary>
        public Vector3? PayoutOrigin { get; private set; }

        SaveStore store;
        float autosaveTimer;
        double incomeAccumulator;
        float incomeSampleTimer;

        /// <summary>Recent earnings per second from the globe and pins (≈30 s moving average). Drives case rewards.</summary>
        public double IncomeRate { get; private set; }

        /// <summary>Multiplier on spin earnings, including global income bonuses.</summary>
        public double IncomeScale => Stats.Get(StatId.GlobeMultiplier) * Stats.Get(StatId.IncomeMultiplier);
        /// <summary>Global income bonuses for pins; their own Pay stat supplies pin-specific bonuses.</summary>
        public double PinIncomeScale => Stats.Get(StatId.IncomeMultiplier);
        bool loaded;

        /// <summary>Prepare visual layouts for authoring without loading saves or starting gameplay.</summary>
        public void PrepareScenePresentation()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Scene layout authoring is only available outside Play mode.");
            if (!balance || !skillTreeAsset) throw new InvalidOperationException("Assign the balance and skill tree assets before authoring layouts.");
            Balance = balance;
            Shop.Balance = Balance;
            Tree = skillTreeAsset;
            Balance.ApplyTo(Stats);
            Hud = Ensure<GameHUD>();
            TreeView = Ensure<SkillTreeView>();
            DeskInfo = Ensure<DeskInfoSystem>();
            Settings = Ensure<SettingsMenu>();
            ShopView = Ensure<GoldShopView>();
            LootboxView = Ensure<LootboxOpeningView>();
        }

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            Balance = balance ? balance : ScriptableObject.CreateInstance<GameBalance>();
            Shop.Balance = Balance;
            if (skillTreeAsset) Tree = skillTreeAsset;
            else
            {
                Tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
                Tree.SetNodes(DefaultSkillTreeFactory.Create());
                Tree.SetSections(DefaultSkillTreeFactory.CreateSections());
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
            LootboxView = Ensure<LootboxOpeningView>();
            Popups = Ensure<PayoutPopups>();
            GuidanceView = Ensure<FirstUseGuidance>();
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

        void OnEnable() { if (globe) { globe.RevolutionsCompleted += Revolution; globe.TicksCompleted += Tick; } }
        void OnDisable() { if (globe) { globe.RevolutionsCompleted -= Revolution; globe.TicksCompleted -= Tick; } }

        /// <summary>The globe pays in small steps during each turn (lots of small payouts instead of one per turn).</summary>
        void Tick(long count)
        {
            int ticks = Mathf.Max(1, Balance.globeTicksPerRevolution);
            for (long i = 0; i < count && i < 8; i++) CreditIncome(Balance.revolutionReward / ticks * IncomeScale * (count > 8 && i == 7 ? count - 7 : 1));
        }

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
            LootboxView.Initialize(this);
            Popups.Initialize(this);
            GuidanceView.Initialize(this);

            loaded = true;
            CheckCompletion();
            EventBus.Publish(new GameLoaded());
            Notify();
            if (!string.IsNullOrEmpty(pendingHint)) { ShowHint(pendingHint); pendingHint = null; }
            else ShowHint(Skills.Owns(DefaultSkillTreeFactory.RootId)
                ? "Welcome back, " + Rank.title.ToLowerInvariant() + "."
                : "Drag and release the globe to spin it. Earn " + NumberFormatter.Money(Tree.Find(DefaultSkillTreeFactory.RootId).cost.Cost(0)) + " to open the skill tree (T).");
        }

        void Update()
        {
            if (!loaded) return;
            CheckCompletion();
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
            float fill = (float)(count * Balance.statueGoldFillPerRevolution * Stats.Get(StatId.StatueGoldFillRate));
            if (figurines != null) foreach (var f in figurines) if (f) f.AddGold(fill);
            EventBus.Publish(new GlobeRevolutions(count));
        }

        /// <param name="origin">Where the payout number should float up (e.g. the case file); null = above the globe.</param>
        public void Credit(double amount, Vector3? origin = null)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
            Wallet.Add(Currency.Money, amount);
            PayoutOrigin = origin;
            CashAwarded?.Invoke(amount);
            PayoutOrigin = null;
            if (audioManager) audioManager.Payout();
        }

        /// <summary>Recurring earnings (globe turns, pin passes): credited and counted in <see cref="IncomeRate"/>.</summary>
        public void CreditIncome(double amount)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
            incomeAccumulator += amount;
            Credit(amount);
        }

        public void CreditGold(double amount, Vector3? origin = null)
        {
            if (double.IsNaN(amount) || double.IsInfinity(amount) || amount <= 0) return;
            Wallet.Add(Currency.Gold, amount);
            PayoutOrigin = origin;
            GoldAwarded?.Invoke(amount);
            PayoutOrigin = null;
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
        public bool PurchaseSkill(string id, int quantity = 1)
        {
            bool hadPins = UnlockedPins > 0;
            if (!Skills.TryPurchase(Tree, id, Wallet, Stats, quantity, out _, out string reason)) { ShowHint(reason); return false; }
            if (!hadPins && UnlockedPins > 0) Guidance.Complete(PlayerGuidanceState.PinIntroStarted);
            if (globe) globe.EnforceLimits();
            if (PinBox) PinBox.Refresh();
            if (map) map.RefreshPinAppearance();
            if (audioManager) audioManager.Click();
            Notify();
            return true;
        }

        // ------------------------------------------------------------------ pins
        public int UnlockedPins => Mathf.Clamp((int)Math.Round(Stats.Get(StatId.PinSlots)), 0, DefaultSkillTreeFactory.MaxPins);
        public int PinPasses(int index) => Math.Max(1, (int)Math.Round(Stats.Get(StatId.PinDurability, index)));
        public int PinDurabilityRank(int index) => Skills.Rank(DefaultSkillTreeFactory.PinDurabilityId(index + 1));
        public void ApplyPinAppearance(PinSkin pin, int index)
        {
            if (!pin) return;
            if (pin.SkinIndex != 0) pin.SetSkin(0);
            int rank = PinDurabilityRank(index);
            var colors = Balance.pinStrengthColors;
            if (colors == null || colors.Length == 0) return;
            int maximum = Tree.Find(DefaultSkillTreeFactory.PinDurabilityId(index + 1)).maxRank;
            int colorIndex = Mathf.Clamp(Mathf.RoundToInt(rank * (colors.Length - 1f) / Mathf.Max(1, maximum)), 0, colors.Length - 1);
            pin.SetStrengthAppearance(rank, colors[colorIndex], rank >= maximum ? Balance.pinMaxStrengthMetallic : Balance.pinEnamelMetallic);
        }
        public double PinPayoutMultiplier(int index) => Stats.Get(StatId.PinPayout, index);

        public void FirePin()
        {
            if (!map) return;
            if (map.CooldownRemaining > 0 || (UnlockedPins > 0 && map.ReadyCount == 0)) return;
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
                Guidance.Load(data);
                Shop.Load(data.goldShop);
                Prestiges = Mathf.Clamp(data.prestiges, 0, Mathf.Max(0, Balance.maxPrestiges));
                GameCompleted = data.gameCompleted;
                // Saves from before prestige existed: the whole history counts as this run.
                RunEarnings = data.runMoney > 0 || Prestiges > 0 ? data.runMoney : data.lifetimeMoney;
                MigrateGoldNodes(data);
                Skills.Load(Tree, data.skills);
                ApplyGoldShopSkills();
                Inventory.Load(data.iceCubes, data.paperBalls, data.sparePieces);
                PlayTime = data.playTimeSeconds;
                Lootbox.LoadState(data);
                PaperBalls.LoadState(data);
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
                    runMoney = RunEarnings, prestiges = Prestiges, gameCompleted = GameCompleted, goldShop = Shop.ToSave(),
                    skills = Skills.ToSave(), playTimeSeconds = PlayTime,
                    completedGuidance = Guidance.ToSave(),
                    iceCubes = Inventory.IceCubes, paperBalls = Inventory.PaperBalls, sparePieces = Inventory.SparePieces,
                    statues = new List<StatueState>(),
                };
                Lootbox.SaveState(data);
                if (PaperBalls) PaperBalls.SaveState(data);
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
        public bool CheckCompletion()
        {
            if ((LootboxView && LootboxView.IsOpen) || GameCompleted || !Prestige.HasCompleted(Prestiges, RunEarnings, Balance)) return false;
            GameCompleted = true;
            Notify();
            if (ShopView) ShopView.ShowEnding();
            if (loaded && useSaveFile) SaveGame();
            return true;
        }

        public void Retire()
        {
            if (!CanRetire)
            {
                ShowHint(GameCompleted ? "Your agency is complete." : IsFinalRank
                    ? "Earn " + NumberFormatter.Money(PrestigeRequirement) + " in this final run to complete the game."
                    : "Earn " + NumberFormatter.Money(PrestigeRequirement) + " in this run to retire.");
                return;
            }
            if (!useSaveFile) { ShowHint("Retiring needs the save file (useSaveFile is off)."); return; }
            double gain = PrestigeGold;
            var data = new SaveData
            {
                money = Balance.startingMoney,
                gold = Wallet.Gold + gain,
                lifetimeMoney = Wallet.LifetimeMoney,
                lifetimeGold = Wallet.LifetimeGold + gain,
                runMoney = 0,
                prestiges = Prestiges + 1,
                goldShop = Shop.ToSave(),
                skills = FreshStartSkills(),
                playTimeSeconds = PlayTime,
                completedGuidance = Guidance.ToSave(),
                sparePieces = Inventory.SparePieces,
                statues = new List<StatueState>(),
            };
            if (PaperBalls) PaperBalls.SaveState(data);
            Lamp.SaveState(data);
            if (figurines != null) foreach (var f in figurines) data.statues.Add(f ? f.SaveState() : default);
            try { Store.Save(data); }
            catch (Exception e) { Debug.LogWarning("Retiring failed: " + e.Message); ShowHint("Retiring failed: the game could not be saved."); return; }
            retiring = true;
            var before = Rank;
            var after = DetectiveRanks.For(Balance, Prestiges + 1);
            pendingHint = "Agency retired: +" + NumberFormatter.Format(gain) + " gold." +
                " " + RetirementIncomeSummary + (after.title != before.title ? " Promoted to " + after.title + "." : "");
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.buildIndex >= 0 ? gameObject.scene.buildIndex : 0);
        }

        /// <summary>Skill ranks a new run starts with, from the Gold shop's fresh-start items.</summary>
        List<SkillRank> FreshStartSkills() => Shop.FreshStartSkills();

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
            ApplyGoldShopSkills();
            Stats.Rebuild(Tree, Skills);
            if (globe) globe.EnforceLimits();
            if (PinBox) PinBox.Refresh();
            if (figurines != null) foreach (var f in figurines) if (f) f.RefreshVisual();
            if (Whiskey) Whiskey.RefreshUnlock();
            if (audioManager) audioManager.Click();
            Notify();
            return true;
        }

        void ApplyGoldShopSkills()
        {
            bool hadPins = UnlockedPins > 0;
            foreach (var grant in Shop.FreshStartSkills())
                Skills.GrantMinimum(Tree, grant.id, grant.rank, Stats);
            if (!hadPins && UnlockedPins > 0) Guidance.Complete(PlayerGuidanceState.PinIntroStarted);
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
