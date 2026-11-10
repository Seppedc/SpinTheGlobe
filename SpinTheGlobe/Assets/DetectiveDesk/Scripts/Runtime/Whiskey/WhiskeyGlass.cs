using System.Collections;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Power-up: click the whiskey glass to pour. Every click raises the whiskey inside the glass one step;
    /// when it is full the detective picks the glass up, drinks it empty and puts it back, and then the boost
    /// (globe earnings and spin speed) starts. Afterwards the glass needs a cooldown before it can be refilled.
    /// An ice cube from a lootbox goes in first and makes the glass fill faster.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WhiskeyGlass : MonoBehaviour
    {
        public float Fill { get; private set; }
        public float BoostRemaining { get; private set; }
        public float Cooldown { get; private set; }
        public bool Boosting => BoostRemaining > 0;
        public bool IceInGlass { get; private set; }
        /// <summary>The drinking animation is playing (clicks are ignored).</summary>
        public bool Drinking { get; private set; }
        public bool Unlocked => game && game.Shop.Owns(GoldShopCatalog.Whiskey);

        [Header("Drink objects (resolved by name when unassigned)")]
        public Transform glassModel, glassInteraction, glassRim;
        public Transform bottleModel, bottleInteraction, bottleCap;

        [Tooltip("Seconds the whiskey takes to rise to the next step after a click.")]
        public float pourSeconds = 0.25f;
        [Tooltip("Seconds for lift, drink and put back.")]
        public float liftSeconds = 0.45f, drinkSeconds = 1.1f, returnSeconds = 0.45f;

        GameManager game;
        Transform glass;                                   // the visible glass model
        readonly List<Transform> carried = new List<Transform>();   // everything that moves with it when drinking
        public Transform GlassTransform => glass;
        Renderer[] renderers;
        MaterialPropertyBlock block;

        // Liquid: scaled on its local up axis from the bottom of the cavity.
        Transform liquid;
        Renderer liquidRenderer;
        Vector3 liquidPos0, liquidScale0;
        float liquidBottom;            // mesh-space bottom of the liquid along its up axis
        int upAxis = 1;                // which local axis of the liquid points up (FBX imports can differ)
        float shownLevel;              // 0..1 as drawn
        readonly List<GameObject> ice = new List<GameObject>();

        static readonly StatId[] Boosted = { StatId.GlobeMultiplier, StatId.PinPayout, StatId.GlobeMaxSpeed };   // auto-rotate follows the max speed

        public void Initialize(GameManager owner)
        {
            game = owner;
            // The authored model (Generated_WhiskeyGlass) is what the player sees; the older "Whiskey glass"
            // placeholder and its rim still carry the click collider, so all of them are clickable and move together.
            if (!glassModel) glassModel = FindDrinkObject("Generated_WhiskeyGlass");
            if (!glassInteraction) glassInteraction = FindDrinkObject("Whiskey glass") ?? FindDrinkObject("WhiskeyGlass");
            if (!glassRim) glassRim = FindDrinkObject("Glass rim");
            if (!bottleModel) bottleModel = FindDrinkObject("Generated_WhiskeyBottle");
            if (!bottleInteraction) bottleInteraction = FindDrinkObject("Whiskey bottle");
            if (!bottleCap) bottleCap = FindDrinkObject("Bottle cap");
            var model = glassModel;
            var placeholder = glassInteraction;
            var rim = glassRim;
            glass = model ? model : placeholder;
            block = new MaterialPropertyBlock();
            if (!glass) { RefreshUnlock(); return; }

            foreach (var t in new[] { glass, placeholder, rim })
                if (t && !carried.Contains(t)) { carried.Add(t); if (game.Clicks) game.Clicks.Register(t, Sip); }
            var glow = new List<Renderer>();
            foreach (var t in glass.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.Contains("AmberWhiskey") && t.GetComponent<MeshFilter>()) liquid = t;
                else if (t.name.StartsWith("IceCube")) ice.Add(t.gameObject);
            }
            foreach (var r in glass.GetComponentsInChildren<Renderer>(true)) if (!liquid || r.transform != liquid) glow.Add(r);
            renderers = glow.ToArray();

            if (liquid)
            {
                liquidRenderer = liquid.GetComponent<Renderer>();
                liquidPos0 = liquid.localPosition;
                liquidScale0 = liquid.localScale;
                // Find the liquid's local axis that points up in the world.
                Vector3 up = glass.up;
                float dx = Vector3.Dot(liquid.right, up), dy = Vector3.Dot(liquid.up, up), dz = Vector3.Dot(liquid.forward, up);
                upAxis = Mathf.Abs(dx) > Mathf.Abs(dy) ? (Mathf.Abs(dx) > Mathf.Abs(dz) ? 0 : 2) : (Mathf.Abs(dy) > Mathf.Abs(dz) ? 1 : 2);
                float sign = upAxis == 0 ? Mathf.Sign(dx) : upAxis == 1 ? Mathf.Sign(dy) : Mathf.Sign(dz);
                var mb = liquid.GetComponent<MeshFilter>().sharedMesh.bounds;
                liquidBottom = sign > 0 ? mb.min[upAxis] : mb.max[upAxis];
            }
            shownLevel = Boosting ? 0 : Fill;
            ApplyLiquid(shownLevel);
            RefreshUnlock();
        }

        Transform FindDrinkObject(string objectName)
        {
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.gameObject.scene == game.gameObject.scene && t.name == objectName) return t;
            return null;
        }

        public void RefreshUnlock()
        {
            bool visible = Unlocked;
            foreach (var t in new[] { glassModel, glassInteraction, glassRim, bottleModel, bottleInteraction, bottleCap })
                if (t && t.gameObject.activeSelf != visible) t.gameObject.SetActive(visible);
            if (!visible)
            {
                if (Drinking) StopAllCoroutines();
                if (Boosting) foreach (var stat in Boosted) game.Stats.SetTemporaryMultiplier(stat, 1);
                Fill = BoostRemaining = Cooldown = 0;
                Drinking = false;
                IceInGlass = false;
                shownLevel = 0;
                ApplyLiquid(0);
                foreach (var cube in ice) if (cube) cube.SetActive(false);
                if (game && game.globe) game.globe.EnforceLimits();
            }
        }

        /// <summary>Click on the glass: pour one step.</summary>
        public void Sip()
        {
            if (!Unlocked || Drinking) return;
            if (Boosting) { game.ShowHint("Boost active: " + UIFactory.Seconds(BoostRemaining) + " left."); return; }
            if (Cooldown > 0) { game.ShowHint("The glass is empty. Refill in " + UIFactory.Seconds(Cooldown) + "."); return; }
            if (Fill <= 0 && !IceInGlass && game.Inventory.TryUseIce()) { IceInGlass = true; game.ShowHint("Ice cube added: the glass fills faster."); }
            Fill = Mathf.Min(1, Fill + game.Balance.whiskeyFillPerClick * (IceInGlass ? game.Balance.iceCubeFillMultiplier : 1));
            if (game.audioManager) game.audioManager.Click();
            if (Fill >= 1) StartCoroutine(DrinkThenBoost());
            game.Notify();
        }

        /// <summary>Pick the glass up, drink it empty, put it back, then start the boost.</summary>
        IEnumerator DrinkThenBoost()
        {
            Drinking = true;
            // Let the last pour finish rising first.
            while (shownLevel < 0.999f)
            {
                shownLevel = Mathf.MoveTowards(shownLevel, 1, Time.deltaTime / Mathf.Max(0.01f, pourSeconds) * Mathf.Max(0.1f, game.Balance.whiskeyFillPerClick));
                ApplyLiquid(shownLevel);
                yield return null;
            }
            yield return new WaitForSeconds(0.15f);

            if (glass)
            {
                var cam = game.GameCamera;
                var b = ClickRouter.RendererBounds(glass, glass.position);
                float h = Mathf.Max(0.05f, b.size.y);
                Vector3 startPos = glass.position;
                Quaternion startRot = glass.rotation;
                var others = new List<(Transform t, Vector3 p, Quaternion r)>();
                foreach (var t in carried) if (t && t != glass) others.Add((t, t.position, t.rotation));
                void SetPose(Vector3 p, Quaternion r)
                {
                    glass.position = p; glass.rotation = r;
                    var delta = r * Quaternion.Inverse(startRot);
                    foreach (var (t, op, or) in others) { t.position = p + delta * (op - startPos); t.rotation = delta * or; }
                }
                Vector3 toCam = cam ? Vector3.ProjectOnPlane(cam.transform.position - b.center, Vector3.up).normalized : Vector3.back;
                Vector3 side = Vector3.Cross(Vector3.up, toCam).normalized;
                Vector3 liftPos = startPos + Vector3.up * h * 2.6f + toCam * h * 2.2f;
                // Tip the rim towards the camera, as if raised to the drinker's mouth.
                Quaternion tipped = Quaternion.AngleAxis(-70f, side) * startRot;

                for (float t = 0; t < 1; t += Time.deltaTime / liftSeconds)
                {
                    float e = Smooth(t);
                    SetPose(Vector3.Lerp(startPos, liftPos, e), Quaternion.Slerp(startRot, Quaternion.AngleAxis(-25f, side) * startRot, e));
                    yield return null;
                }
                for (float t = 0; t < 1; t += Time.deltaTime / drinkSeconds)
                {
                    float e = Smooth(t);
                    SetPose(liftPos + Vector3.up * Mathf.Sin(t * Mathf.PI) * h * 0.25f,
                            Quaternion.Slerp(Quaternion.AngleAxis(-25f, side) * startRot, tipped, Smooth(Mathf.Min(1, t * 1.6f))));
                    shownLevel = 1 - e;
                    ApplyLiquid(shownLevel);
                    yield return null;
                }
                shownLevel = 0;
                ApplyLiquid(0);
                Quaternion from = glass.rotation;
                for (float t = 0; t < 1; t += Time.deltaTime / returnSeconds)
                {
                    float e = Smooth(t);
                    SetPose(Vector3.Lerp(liftPos, startPos, e), Quaternion.Slerp(from, startRot, e));
                    yield return null;
                }
                SetPose(startPos, startRot);
                if (game.audioManager) game.audioManager.Click();   // glass set down
            }

            Drinking = false;
            StartBoost();
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        void StartBoost()
        {
            if (!Unlocked) return;
            Fill = 0;
            BoostRemaining = (float)game.Stats.Get(StatId.WhiskeyBoostDuration);
            double strength = game.Stats.Permanent(StatId.WhiskeyBoostStrength);
            foreach (var s in Boosted) game.Stats.SetTemporaryMultiplier(s, strength);
            game.ShowHint("Cheers! ×" + strength.ToString("0.##") + " globe earnings and spin speed for " + UIFactory.Seconds(BoostRemaining) + ".");
            EventBus.Publish(new WhiskeyBoostChanged(true));
            game.Notify();
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
            if (!Unlocked) return;
            if (BoostRemaining > 0) { BoostRemaining -= Time.deltaTime; if (BoostRemaining <= 0) { BoostRemaining = 0; EndBoost(); } }
            else if (Cooldown > 0 && !Drinking)
            {
                Cooldown = Mathf.Max(0, Cooldown - Time.deltaTime);
                // Bottomless Glass: the glass refills itself and is drunk straight away.
                if (Cooldown <= 0 && game.Stats.Get(StatId.WhiskeyAutoRefill) >= 1) { Fill = 1; StartCoroutine(DrinkThenBoost()); }
            }

            if (!Drinking)
            {
                // The whiskey rises step by step as it is poured.
                float target = Boosting ? 0 : Fill;
                if (!Mathf.Approximately(shownLevel, target))
                {
                    shownLevel = Mathf.MoveTowards(shownLevel, target, Time.deltaTime / Mathf.Max(0.01f, pourSeconds) * Mathf.Max(0.1f, game.Balance.whiskeyFillPerClick));
                    ApplyLiquid(shownLevel);
                }
            }
            foreach (var cube in ice) if (cube) { bool show = IceInGlass && (shownLevel > 0.02f || Drinking); if (cube.activeSelf != show) cube.SetActive(show); }
            Glow();
        }

        void ApplyLiquid(float level)
        {
            if (!liquid) return;
            level = Mathf.Clamp01(level);
            bool visible = level > 0.01f;
            if (liquidRenderer && liquidRenderer.enabled != visible) liquidRenderer.enabled = visible;
            float k = Mathf.Max(0.01f, level);
            var scale = liquidScale0;
            scale[upAxis] *= k;
            liquid.localScale = scale;
            // Keep the bottom of the whiskey on the bottom of the glass while it scales.
            var offset = Vector3.zero;
            offset[upAxis] = liquidBottom * (1 - k) * liquidScale0[upAxis];
            liquid.localPosition = liquidPos0 + liquid.localRotation * offset;
        }

        void Glow()
        {
            if (renderers == null) return;
            if (block == null) block = new MaterialPropertyBlock();   // e.g. after a script reload in Play mode
            float pulse = Boosting ? 0.35f + 0.2f * Mathf.Sin(Time.time * 6) : 0;
            var emission = new Color(0.9f, 0.45f, 0.08f) * pulse;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Testing only: ends any boost/cooldown and fills the glass to one pour from full.</summary>
        public void DevAlmostFull()
        {
            if (!Unlocked || Drinking) return;
            if (Boosting) { BoostRemaining = 0; foreach (var s in Boosted) game.Stats.SetTemporaryMultiplier(s, 1); }
            Cooldown = 0;
            Fill = Mathf.Max(0, 1 - game.Balance.whiskeyFillPerClick);
            game.ShowHint("[dev] whiskey glass almost full");
            game.Notify();
        }

        void OnDisable() { if (game && Boosting) foreach (var s in Boosted) game.Stats.SetTemporaryMultiplier(s, 1); }

        // Every session starts with the glass ready to pour: a saved refill cooldown is not carried over
        // (could later become an unlock bought with gold).
        public void LoadState(SaveData d) { Fill = Mathf.Clamp01(d.whiskeyFill); Cooldown = 0; if (Fill >= 1) Fill = 0.99f; }
        // A boost in progress is not saved; it ends as if it had run out.
        public void SaveState(SaveData d) { d.whiskeyFill = Boosting || Drinking ? 0 : Fill; d.whiskeyCooldown = Boosting || Drinking ? (float)game.Stats.Get(StatId.WhiskeyCooldown) : Cooldown; }
    }
}
