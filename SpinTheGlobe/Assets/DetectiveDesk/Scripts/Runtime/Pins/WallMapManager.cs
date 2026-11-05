using System.Collections;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Progression;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Pin shooter + wall map. Fires the next ready pin from the pin box to a random spot on the map
    /// (land / city chances are upgradeable stats), mirrors it onto the globe, pays it out at the
    /// meridian and lets it fall when its strength runs out. Pins are individual (index 0..11) with
    /// their own durability and payout upgrades.
    /// </summary>
    public sealed class WallMapManager : MonoBehaviour
    {
        public GameManager game;
        public GlobeController globe;
        public MeridianTrigger meridian;
        public Camera gameCamera;
        [Tooltip("Country hit areas from the old aiming prototype. Unused by random landing; kept for reference data.")]
        public CountryTarget[] targets;
        public PinSkin pinPrefab;
        public Transform muzzle, launcherVisual, runtimeRoot, mapSurface;
        // World-space corners of the unchanged artwork, UV (0,0), (1,0), (0,1).
        public Vector3 mapBottomLeft, mapBottomRight, mapTopLeft;
        public Vector3 mapNormal = Vector3.back;
        // Existing 2048x900 atlas: geographic grid x88..1960, y135..815.
        public Rect geographicUV = new Rect(88f / 2048, 85f / 900, 1872f / 2048, 680f / 900);
        public float minimumLongitude = -180, maximumLongitude = 180, minimumLatitude = -60, maximumLatitude = 85;
        public float boardPinLength = .18f, markerLength = .065f, flightSeconds = .32f;
        public float dropOutwardSpeed = 1.8f;
        public YarnRenderer yarn;
        public ParticleSystem corkDust;

        readonly List<PinInstance> active = new List<PinInstance>();
        readonly HashSet<int> busy = new HashSet<int>();          // pins in flight or on the board
        readonly List<GameObject> flightObjects = new List<GameObject>();
        readonly Queue<GameObject> dropped = new Queue<GameObject>();
        readonly System.Random rng = new System.Random();
        readonly PinShooterCycle cycle = new PinShooterCycle();
        Quaternion launcherRest;
        Vector3 launcherPosition;
        float recoil;

        public float CooldownRemaining => (float)cycle.CooldownRemaining;
        public int ActiveCount => active.Count;
        public int DroppedCount { get; private set; }
        public int LaunchedCount { get; private set; }
        public IReadOnlyList<PinInstance> ActivePins => active;
        public bool IsBusy(int index) => busy.Contains(index);

        public int ReadyCount
        {
            get
            {
                if (!game) return 0;
                int n = 0, unlocked = game.UnlockedPins;
                for (int i = 0; i < unlocked; i++) if (!busy.Contains(i)) n++;
                return n;
            }
        }

        /// <summary>Money per full revolution from every pin on the board (HUD estimate).</summary>
        public double ActiveValuePerRevolution
        {
            get { double v = 0; foreach (var p in active) if (p && p.IsActive) v += PayoutFor(p); return v; }
        }

        public void Bind(GameManager owner) { game = owner; }

        void Awake()
        {
            if (launcherVisual) { launcherRest = launcherVisual.localRotation; launcherPosition = launcherVisual.localPosition; }
        }

        void Update()
        {
            cycle.Advance(Time.deltaTime);
            if (launcherVisual && recoil > 0)
            {
                recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 5);
                launcherVisual.localRotation = launcherRest * Quaternion.Euler(-3 * recoil, 0, 0);
                launcherVisual.localPosition = launcherPosition + Vector3.back * .025f * recoil;
            }
            // Auto-loader upgrade: fire by itself whenever a pin is ready.
            // Carry the fractional reload time across shots so fast reloads also work at 30 FPS.
            if (game && game.Stats.Get(StatId.PinAutoFire) >= 1)
                for (int shot = 0; shot < DefaultSkillTreeFactory.MaxPins && cycle.Ready && ReadyCount > 0; shot++)
                    if (FireNext() != null) break;
            cycle.DiscardOverdueTime();
            if (game && !game.WorldInputBlocked && Input.GetKeyDown(KeyCode.Space)) game.FirePin();
        }

        public double PayoutFor(PinInstance pin)
        {
            if (!game || !pin) return 0;
            return game.Balance.pinBasePayout * game.PinPayoutMultiplier(pin.Index) * game.PinIncomeScale;
        }

        /// <summary>Fire the next ready pin. Returns null on success or a message explaining why not.</summary>
        public string FireNext()
        {
            if (!game || !pinPrefab || !globe || !meridian || !runtimeRoot || !isActiveAndEnabled) return "The pin shooter is not set up.";
            int unlocked = game.UnlockedPins;
            if (unlocked == 0) return game.Skills.Owns(DefaultSkillTreeFactory.RootId)
                ? "Buy Pin 1 in the skill tree (T) to load the shooter."
                : "Unlock the skill tree (T), then buy your first pin.";
            if (CooldownRemaining > 0) return "Shooter reloading… " + CooldownRemaining.ToString("0.0") + "s";
            int index = cycle.TakeNextReady(unlocked, busy);
            if (index < 0) return "All your pins are on the map. They come back when they fall off.";

            var b = game.Balance;
            var landing = PinLanding.Sample(rng, game.Stats.Get(StatId.PinLandChance), game.Stats.Get(StatId.PinCityChance),
                b.cityRadiusDegrees, minimumLatitude, maximumLatitude);
            cycle.Reload(game.Stats.Get(StatId.ShooterCooldown));
            Launch(index, landing);

            // Twin Barrel: chance to fire a second ready pin in the same shot.
            if (rng.NextDouble() < game.Stats.Get(StatId.PinDoubleShotChance))
            {
                int second = cycle.TakeNextReady(unlocked, busy);
                if (second >= 0)
                    Launch(second, PinLanding.Sample(rng, game.Stats.Get(StatId.PinLandChance), game.Stats.Get(StatId.PinCityChance),
                        b.cityRadiusDegrees, minimumLatitude, maximumLatitude));
            }
            return null;
        }

        void Launch(int index, PinLanding landing)
        {
            busy.Add(index);
            if (game.PinBox) game.PinBox.Refresh();
            StartCoroutine(Flight(index, landing));
        }

        public Vector3 GeographicPoint(float latitude, float longitude)
        {
            float u = geographicUV.x + Mathf.InverseLerp(minimumLongitude, maximumLongitude, longitude) * geographicUV.width;
            float v = geographicUV.y + Mathf.InverseLerp(minimumLatitude, maximumLatitude, latitude) * geographicUV.height;
            return mapBottomLeft + (mapBottomRight - mapBottomLeft) * u + (mapTopLeft - mapBottomLeft) * v + mapNormal * .008f;
        }

        IEnumerator Flight(int index, PinLanding landing)
        {
            int skin = index % PinSkin.SkinCount;
            Vector3 destination = GeographicPoint(landing.Latitude, landing.Longitude);
            Vector3 start = muzzle ? muzzle.position : destination + mapNormal;
            var visual = PinVisual.Create(pinPrefab, runtimeRoot, start, mapNormal, boardPinLength, skin);
            flightObjects.Add(visual.gameObject); LaunchedCount++; recoil = 1;
            if (game.audioManager) game.audioManager.Launch();
            float seconds = game ? game.Balance.pinFlightSeconds : flightSeconds, elapsed = 0;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, seconds));
                visual.transform.position = Vector3.Lerp(start, destination, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .18f);
                yield return null;
            }
            flightObjects.Remove(visual.gameObject); visual.transform.position = destination;

            var markerGO = new GameObject("Globe marker / pin " + (index + 1));
            var marker = markerGO.AddComponent<GlobePinMarker>(); marker.Initialize(globe, landing.Latitude, landing.Longitude);
            Vector3 outward = (marker.transform.position - globe.Center).normalized;
            marker.Illuminate(PinVisual.Create(pinPrefab, marker.transform, marker.transform.position, outward, markerLength, skin));

            var instance = visual.gameObject.AddComponent<PinInstance>();
            int passes = game.PinPasses(index);
            instance.Initialize(this, index, landing, marker, passes, meridian.NextPass(marker.transform.position));
            active.Add(instance);
            if (yarn) yarn.Rebuild(active, mapNormal);
            game.Notify();
        }

        // Called by PinInstance ------------------------------------------------------------
        public void OnPinPassed(PinInstance pin)
        {
            double amount = PayoutFor(pin);
            game.CreditIncome(amount);
            EventBus.Publish(new PinPaidOut(pin.Index, amount, pin.RemainingPasses));
        }

        public void OnPinFell(PinInstance pin)
        {
            Unregister(pin);
            EventBus.Publish(new PinFellOff(pin.Index, game ? game.PinDurabilityRank(pin.Index) : 0));
        }

        public PinInstance[] ActivePinsSnapshot() => active.ToArray();

        public void Unregister(PinInstance pin)
        {
            active.Remove(pin);
            busy.Remove(pin.Index);
            if (yarn) yarn.Rebuild(active, mapNormal);
            if (game) { if (game.PinBox) game.PinBox.Refresh(); game.Notify(); }
        }

        public void EmitDust(Vector3 position) { if (corkDust) { var emit = new ParticleSystem.EmitParams { position = position }; corkDust.Emit(emit, 9); } }

        public void TrackDropped(GameObject pin)
        {
            DroppedCount++; dropped.Enqueue(pin);
            while (dropped.Count > 12) { var old = dropped.Dequeue(); if (old) Destroy(old); }
        }

        void OnDisable()
        {
            StopAllCoroutines();
            foreach (var pin in flightObjects) if (pin) Destroy(pin);
            flightObjects.Clear();
            busy.Clear();
            foreach (var p in active) if (p) busy.Add(p.Index);
            if (launcherVisual) { launcherVisual.localRotation = launcherRest; launcherVisual.localPosition = launcherPosition; }
        }
    }
}
