using DetectiveDesk.Core;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Power-up: click the whiskey glass to fill it. When full it boosts globe earnings and spin speed
    /// for a while, then needs a cooldown. An ice cube from a lootbox makes the next fill faster.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiskeyGlass : MonoBehaviour
    {
        public float Fill { get; private set; }
        public float BoostRemaining { get; private set; }
        public float Cooldown { get; private set; }
        public bool Boosting => BoostRemaining > 0;
        public bool IceInGlass { get; private set; }

        GameManager game;
        Transform glass;
        public Transform GlassTransform => glass;
        Renderer[] renderers;
        MaterialPropertyBlock block;

        static readonly StatId[] Boosted = { StatId.GlobeMultiplier, StatId.GlobeMaxSpeed, StatId.GlobeAutoSpeed };

        public void Initialize(GameManager owner)
        {
            game = owner;
            glass = ClickRouter.FindByName("Whiskey glass", "WhiskeyGlass");
            if (glass)
            {
                renderers = glass.GetComponentsInChildren<Renderer>();
                game.Clicks.Register(glass, Sip);
            }
            block = new MaterialPropertyBlock();
        }

        /// <summary>Click on the glass (or the HUD button).</summary>
        public void Sip()
        {
            if (!game) return;
            if (Boosting) { game.ShowHint("Boost active: " + UIFactory.Seconds(BoostRemaining) + " left."); return; }
            if (Cooldown > 0) { game.ShowHint("The glass is empty. Refill in " + UIFactory.Seconds(Cooldown) + "."); return; }
            if (Fill <= 0 && !IceInGlass && game.Inventory.TryUseIce()) { IceInGlass = true; game.ShowHint("Ice cube added: the glass fills faster."); }
            Fill = Mathf.Min(1, Fill + game.Balance.whiskeyFillPerClick * (IceInGlass ? game.Balance.iceCubeFillMultiplier : 1));
            if (game.audioManager) game.audioManager.Click();
            if (Fill >= 1) StartBoost();
            game.Notify();
        }

        void StartBoost()
        {
            BoostRemaining = (float)game.Stats.Get(StatId.WhiskeyBoostDuration);
            double strength = game.Stats.Permanent(StatId.WhiskeyBoostStrength);
            foreach (var s in Boosted) game.Stats.SetTemporaryMultiplier(s, strength);
            game.ShowHint("Cheers! ×" + strength.ToString("0.##") + " globe earnings and spin speed for " + UIFactory.Seconds(BoostRemaining) + ".");
            EventBus.Publish(new WhiskeyBoostChanged(true));
        }

        void EndBoost()
        {
            foreach (var s in Boosted) game.Stats.SetTemporaryMultiplier(s, 1);
            if (game.globe) game.globe.EnforceLimits();
            Fill = 0; IceInGlass = false;
            Cooldown = (float)game.Stats.Get(StatId.WhiskeyCooldown);
            game.ShowHint("The boost wore off. The glass can be refilled in " + UIFactory.Seconds(Cooldown) + ".");
            EventBus.Publish(new WhiskeyBoostChanged(false));
            game.Notify();
        }

        void Update()
        {
            if (!game) return;
            if (BoostRemaining > 0) { BoostRemaining -= Time.deltaTime; if (BoostRemaining <= 0) { BoostRemaining = 0; EndBoost(); } }
            else if (Cooldown > 0)
            {
                Cooldown = Mathf.Max(0, Cooldown - Time.deltaTime);
                // Bottomless Glass: refills and starts the next boost by itself.
                if (Cooldown <= 0 && game.Stats.Get(StatId.WhiskeyAutoRefill) >= 1) { Fill = 1; StartBoost(); }
            }
            Glow();
        }

        void Glow()
        {
            if (renderers == null) return;
            float pulse = Boosting ? 0.35f + 0.2f * Mathf.Sin(Time.time * 6) : Fill * 0.12f;
            var emission = new Color(0.9f, 0.45f, 0.08f) * pulse;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(block);
            }
        }

        void OnDisable() { if (game && Boosting) foreach (var s in Boosted) game.Stats.SetTemporaryMultiplier(s, 1); }

        public void LoadState(SaveData d) { Fill = Mathf.Clamp01(d.whiskeyFill); Cooldown = Mathf.Max(0, d.whiskeyCooldown); }
        // A boost in progress is not saved; it ends as if it had run out.
        public void SaveState(SaveData d) { d.whiskeyFill = Boosting ? 0 : Fill; d.whiskeyCooldown = Boosting ? (float)game.Stats.Get(StatId.WhiskeyCooldown) : Cooldown; }
    }
}
