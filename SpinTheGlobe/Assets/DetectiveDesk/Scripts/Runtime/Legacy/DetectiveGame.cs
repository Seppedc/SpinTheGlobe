using DetectiveDesk.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace DetectiveDesk
{
    /// <summary>
    /// Original desk HUD adapter: shows money/gold/income in the authored top-left HUD, hint text,
    /// settings/upgrades panels, and forwards desk clicks (DeskInteractable) to the GameManager.
    /// Holds no economy of its own once the GameManager is present.
    /// </summary>
    public class DetectiveGame : MonoBehaviour
    {
        [Header("Economy (display mirror of the GameManager wallet)")]
        public double money = 0;
        public double moneyPerMinute = 0;
        public int gold;
        public Text moneyText, incomeText, goldText, hintText;
        public GameObject settingsPanel, upgradesPanel;
        public Transform globeRotor;
        public float globeSpeed = 7f;
        public Light globeLight;
        public GameObject stampMark;
        public DetectiveDesk.Gameplay.GameManager Gameplay { get; set; }

        bool solved;
        public bool IsCaseSolved => solved;
        public event System.Action CaseSolved;
        public event System.Action FilesOpened;

        bool HasGameplay => Gameplay && Gameplay.isActiveAndEnabled;

        void Start() { UpdateHUD(); if (stampMark) stampMark.SetActive(false); }

        void Update()
        {
            if (!HasGameplay && globeRotor) globeRotor.Rotate(Vector3.up, globeSpeed * Time.deltaTime, Space.World);
            if (globeLight) globeLight.intensity = 0.45f + Mathf.Sin(Time.time * 1.6f) * 0.14f;
            if (Input.GetKeyDown(KeyCode.Escape)) { if (settingsPanel) settingsPanel.SetActive(false); if (upgradesPanel) upgradesPanel.SetActive(false); }
        }

        public void RefreshHUD()
        {
            if (HasGameplay)
            {
                money = Gameplay.Wallet.Money;
                gold = (int)System.Math.Min(int.MaxValue, Gameplay.Wallet.Gold);
            }
            UpdateHUD();
        }

        void UpdateHUD()
        {
            if (moneyText) moneyText.text = NumberFormatter.Money(money);
            if (incomeText) incomeText.text = "+" + NumberFormatter.Money(moneyPerMinute) + " / min";
            if (goldText) goldText.text = NumberFormatter.Format(HasGameplay ? Gameplay.Wallet.Gold : gold);
        }

        public void ToggleSettings() { if (settingsPanel) settingsPanel.SetActive(!settingsPanel.activeSelf); if (upgradesPanel) upgradesPanel.SetActive(false); }

        public void ToggleUpgrades()
        {
            if (settingsPanel) settingsPanel.SetActive(false);
            if (HasGameplay && Gameplay.TreeView) { if (upgradesPanel) upgradesPanel.SetActive(false); Gameplay.TreeView.Toggle(); return; }
            if (upgradesPanel) upgradesPanel.SetActive(!upgradesPanel.activeSelf);
        }

        public void UpgradeIncome() { if (HasGameplay && Gameplay.TreeView) Gameplay.TreeView.Open(); }

        /// <summary>Desk stamp click.</summary>
        public void SolveCase()
        {
            if (HasGameplay && Gameplay.Cases) { Gameplay.Cases.TryStamp(); return; }
            ShowHint("The case system is not running.");
        }

        /// <summary>Called by the case file when a case is approved: shows the SOLVED mark and plays the completion effect.</summary>
        public void PlayCaseSolvedEffects()
        {
            solved = true;
            if (stampMark) stampMark.SetActive(true);
            CaseSolved?.Invoke();
        }

        /// <summary>Called when a new case opens.</summary>
        public void ResetCaseVisual()
        {
            solved = false;
            if (stampMark) stampMark.SetActive(false);
        }

        public void OpenFiles()
        {
            if (HasGameplay && Gameplay.Cases) Gameplay.Cases.ShowStatus();
            FilesOpened?.Invoke();
        }

        public void SelectPin(PinCofferSlot slot) { if (HasGameplay) Gameplay.SelectPin(slot); }
        public void SelectPin() { if (HasGameplay) Gameplay.FirePin(); }

        public void InspectGlobe()
        {
            if (HasGameplay && Gameplay.globe)
            {
                ShowHint("Globe: " + (Gameplay.globe.CurrentVelocity / 6).ToString("0.0") + " / " + (Gameplay.globe.MaximumVelocity / 6).ToString("0.0") +
                         " RPM. Drag left to right to spin it fast, or click for a small push.");
                return;
            }
            ShowHint("Mechanical globe • spinning at " + globeSpeed.ToString("0") + "°/s");
        }

        public void InspectStatue() { if (HasGameplay) Gameplay.InspectFigurine(null); }
        public void FirePin() { if (HasGameplay) Gameplay.FirePin(); }
        public void ShowHint(string message) { if (hintText) hintText.text = message; }
    }
}
