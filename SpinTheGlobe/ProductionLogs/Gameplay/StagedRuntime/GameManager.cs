using System;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        public DetectiveGame legacy;
        public UpgradeConfig config;
        public GlobeController globe;
        public WallMapManager map;
        public FigurinePedestal[] figurines;
        public GameplayPanel panel;
        public AudioManager audioManager;
        public DeskAssetPresentation archive;
        public PinCofferSlot[] pinSlots;
        public bool populateEmptyCofferOnStart = true;
        [SerializeField, Min(0)] int speedLevel, motorLevel, durabilityLevel;
        [SerializeField, Range(0, 11)] int selectedSkin;
        public int SpeedLevel => speedLevel;
        public int MotorLevel => motorLevel;
        public int DurabilityLevel => durabilityLevel;
        public int SelectedSkin => selectedSkin;
        public double Money => legacy ? legacy.money : 0;
        public double GlobalMultiplier
        {
            get { double total = 0; if (figurines != null) foreach (var f in figurines) if (f) total += f.Multiplier; return Math.Max(1, total); }
        }
        public bool WorldInputBlocked => (archive && archive.IsArchiveOpen) || (legacy &&
            ((legacy.settingsPanel && legacy.settingsPanel.activeInHierarchy) || (legacy.upgradesPanel && legacy.upgradesPanel.activeInHierarchy)));
        public event Action Changed;
        public event Action<double> CashAwarded;

        void Awake()
        {
            if (legacy) { legacy.Gameplay = this; legacy.globeSpeed = 0; }
            if (!config || !globe || !legacy) { Debug.LogError("Gameplay needs its legacy game, globe and configuration references.", this); enabled = false; }
        }
        void OnEnable() { if (globe) globe.RevolutionsCompleted += Revolution; }
        void OnDisable() { if (globe) globe.RevolutionsCompleted -= Revolution; }
        void Start()
        {
            if (populateEmptyCofferOnStart && pinSlots != null)
                foreach (var slot in pinSlots) if (slot && slot.IsEmpty && slot.PinPrefab) slot.PlacePin((slot.SlotNumber - 1) % PinSkin.SkinCount);
            Notify(); legacy.ShowHint("Drag the globe left to right. Select a pin color, then click a country on the map.");
        }
        void Update()
        {
            if (!legacy || !globe || !config) return;
            double perTurn = config.baseRevolutionReward + (map ? map.ActiveValuePerRevolution : 0);
            // HUD estimate only. This value never grants passive money.
            legacy.moneyPerMinute = perTurn * GlobalMultiplier * globe.CurrentVelocity / 6;
            legacy.RefreshHUD();
        }
        void Revolution(long count) { Credit(count * config.baseRevolutionReward * GlobalMultiplier); }
        public void Credit(double amount)
        {
            if (!GameplayMath.Finite(amount) || amount <= 0 || !legacy || !GameplayMath.Finite(legacy.money + amount)) return;
            legacy.money += amount;
            CashAwarded?.Invoke(amount);
            audioManager?.Payout();
            Notify();
        }
        public bool TrySpend(double amount)
        {
            if (!GameplayMath.Finite(amount) || amount < 0 || !legacy || legacy.money + 1e-9 < amount) return false;
            legacy.money = Math.Max(0, legacy.money - amount); Notify(); return true;
        }
        public int Level(UpgradeKind kind) => kind == UpgradeKind.Speed ? speedLevel : kind == UpgradeKind.AutoMotor ? motorLevel : durabilityLevel;
        public bool BuyUpgrade(UpgradeKind kind)
        {
            if (!config || Level(kind) >= config.maximumUpgradeLevel) { legacy?.ShowHint("This upgrade is at maximum level."); return false; }
            double cost = config.Cost(kind, Level(kind));
            if (!TrySpend(cost)) { legacy?.ShowHint("Need €" + cost.ToString("N0") + " for this upgrade."); return false; }
            switch (kind) { case UpgradeKind.Speed: speedLevel++; break; case UpgradeKind.AutoMotor: motorLevel++; break; case UpgradeKind.Durability: durabilityLevel++; break; }
            globe.EnforceLimits(); audioManager?.Click(); Notify();
            legacy.ShowHint(kind + " upgraded. Durability upgrades apply to newly fired pins.");
            return true;
        }
        public bool RestoreFigurine(int index)
        {
            if (figurines == null || index < 0 || index >= figurines.Length || !figurines[index]) return false;
            var f = figurines[index];
            if (f.Tier >= 3) { legacy.ShowHint("This figurine is fully restored."); return false; }
            double cost = config.RestorationCost(f.Tier);
            if (!TrySpend(cost)) { legacy.ShowHint("Need €" + cost.ToString("N0") + " to restore this figurine."); return false; }
            f.SetTier(f.Tier + 1); audioManager?.Click(); Notify(); return true;
        }
        public void SelectPin(PinCofferSlot slot)
        {
            if (slot && slot.Occupant) selectedSkin = slot.Occupant.SkinIndex;
            legacy.ShowHint(PinSkin.SkinNames[selectedSkin] + " pin selected. Click a country; the coffer keeps your collectible.");
            audioManager?.Click(); Notify();
        }
        public void ArmLauncher() { legacy.ShowHint("Click a country on the wall map to launch the selected " + PinSkin.SkinNames[selectedSkin] + " pin."); }
        public void InspectFigurine(FigurinePedestal pedestal)
        {
            if (legacy.upgradesPanel) legacy.upgradesPanel.SetActive(true);
            panel?.Refresh();
            legacy.ShowHint(pedestal ? pedestal.name + ": " + pedestal.TierName + " — use Restore in Upgrades." : "Restore the three figurines in Upgrades.");
        }
        public void Notify() { if (legacy) legacy.RefreshHUD(); Changed?.Invoke(); }
    }
}
