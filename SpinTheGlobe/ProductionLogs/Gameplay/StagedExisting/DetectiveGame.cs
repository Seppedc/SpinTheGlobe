using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk
{
    public class DetectiveGame : MonoBehaviour
    {
        [Header("Economy")]
        public double money = 422;
        public double moneyPerMinute = 12.5;
        public int gold;
        public Text moneyText, incomeText, goldText, hintText;
        public GameObject settingsPanel, upgradesPanel;
        public Transform globeRotor;
        public float globeSpeed = 7f;
        public Light globeLight;
        public GameObject stampMark;
        public DetectiveDesk.Gameplay.GameManager Gameplay { get; set; }
        public void RefreshHUD() { displayedMoney = money; UpdateHUD(); }
        double displayedMoney;
        bool solved;
        public bool IsCaseSolved => solved;
        public event System.Action CaseSolved;
        public event System.Action FilesOpened;

        void Start() { displayedMoney = money; UpdateHUD(); if (stampMark) stampMark.SetActive(false); }
        void Update()
        {
            if (!Gameplay || !Gameplay.isActiveAndEnabled)
            {
                money += moneyPerMinute * Time.deltaTime / 60d;
                if (globeRotor) globeRotor.Rotate(Vector3.up, globeSpeed * Time.deltaTime, Space.World);
            }
            if (globeLight) globeLight.intensity = 0.45f + Mathf.Sin(Time.time * 1.6f) * 0.14f;
            if (System.Math.Abs(displayedMoney - money) > .005d) { displayedMoney = money; UpdateHUD(); }
            if (Input.GetKeyDown(KeyCode.Escape)) { if (settingsPanel) settingsPanel.SetActive(false); if (upgradesPanel) upgradesPanel.SetActive(false); }
        }
        void UpdateHUD()
        {
            if (moneyText) moneyText.text = "€" + money.ToString("N2");
            if (incomeText) incomeText.text = "+€" + moneyPerMinute.ToString("N2") + " / min";
            if (goldText) goldText.text = gold.ToString();
        }
        public void ToggleSettings() { if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf); if (upgradesPanel) upgradesPanel.SetActive(false); }
        public void ToggleUpgrades() { if (upgradesPanel) upgradesPanel.SetActive(!upgradesPanel.activeSelf); if (settingsPanel) settingsPanel.SetActive(false); }
        public void UpgradeIncome()
        {
            if (Gameplay && Gameplay.isActiveAndEnabled) { Gameplay.BuyUpgrade(DetectiveDesk.Gameplay.UpgradeKind.AutoMotor); return; }
            if (money < 100) { ShowHint("Need €100 for the income upgrade"); return; }
            money -= 100; moneyPerMinute += 5; UpdateHUD(); ShowHint("Income increased by €5/min");
        }
        public void SolveCase()
        {
            if (solved) { ShowHint("Case already marked SOLVED"); return; }
            solved = true; money += 50; if (stampMark) stampMark.SetActive(true);
            UpdateHUD(); ShowHint("CASE SOLVED  •  +€50");
            CaseSolved?.Invoke();
        }
        public void OpenFiles() { ShowHint("Case file inspected. Click the stamp to solve it."); FilesOpened?.Invoke(); }
        public void SelectPin(PinCofferSlot slot) { if (Gameplay && Gameplay.isActiveAndEnabled) Gameplay.SelectPin(slot); else SelectPin(); }
        public void SelectPin() { if (Gameplay && Gameplay.isActiveAndEnabled) { Gameplay.SelectPin(null); return; } ShowHint("Pin selected — launcher prototype is ready."); }
        public void InspectGlobe() { if (Gameplay && Gameplay.isActiveAndEnabled) { ShowHint("Globe: " + (Gameplay.globe.CurrentVelocity / 6).ToString("0.0") + " RPM. Drag left to right to accelerate."); return; } ShowHint("Mechanical globe • spinning at " + globeSpeed.ToString("0") + "°/s"); }
        public void InspectStatue() { if (Gameplay && Gameplay.isActiveAndEnabled) { Gameplay.InspectFigurine(null); return; } ShowHint("Collectible statue • display slot"); }
        public void FirePin() { if (Gameplay && Gameplay.isActiveAndEnabled) { Gameplay.ArmLauncher(); return; } ShowHint("Toy launcher activated — firing animation to add next."); }
        public void ShowHint(string message) { if (hintText) hintText.text = message; }
    }
}
