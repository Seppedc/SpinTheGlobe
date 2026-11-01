using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    public sealed class WallMapManager : MonoBehaviour
    {
        public GameManager game;
        public GlobeController globe;
        public MeridianTrigger meridian;
        public Camera gameCamera;
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
        readonly HashSet<string> pending = new HashSet<string>();
        readonly List<GameObject> flightObjects = new List<GameObject>();
        readonly Queue<GameObject> dropped = new Queue<GameObject>();
        Quaternion launcherRest;
        Vector3 launcherPosition;
        float recoil;
        public int ActiveCount => active.Count;
        public int PendingCount => pending.Count;
        public int DroppedCount { get; private set; }
        public int LaunchedCount { get; private set; }
        public IReadOnlyList<PinInstance> ActivePins => active;
        public double ActiveValuePerRevolution
        {
            get { double value = 0; foreach (var p in active) if (p && p.IsActive) value += p.Country.baseValue * p.PinMultiplier; return value; }
        }

        void Awake()
        {
            if (launcherVisual) { launcherRest = launcherVisual.localRotation; launcherPosition = launcherVisual.localPosition; }
        }
        void Update()
        {
            if (launcherVisual && recoil > 0)
            {
                recoil = Mathf.MoveTowards(recoil, 0, Time.deltaTime * 5);
                launcherVisual.localRotation = launcherRest * Quaternion.Euler(-3 * recoil, 0, 0);
                launcherVisual.localPosition = launcherPosition + Vector3.back * .025f * recoil;
            }
            if (!game || game.WorldInputBlocked || !Input.GetMouseButtonDown(0) ||
                (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            CountryData country; Vector2 coordinate;
            if (TryPick(gameCamera.ScreenPointToRay(Input.mousePosition), out country, out coordinate))
                Launch(country, coordinate.y, coordinate.x);
        }

        public Vector3 GeographicPoint(float latitude, float longitude)
        {
            float u = geographicUV.x + Mathf.InverseLerp(minimumLongitude, maximumLongitude, longitude) * geographicUV.width;
            float v = geographicUV.y + Mathf.InverseLerp(minimumLatitude, maximumLatitude, latitude) * geographicUV.height;
            return mapBottomLeft + (mapBottomRight - mapBottomLeft) * u + (mapTopLeft - mapBottomLeft) * v + mapNormal * .008f;
        }
        public bool TryPick(Ray ray, out CountryData country, out Vector2 longitudeLatitude)
        {
            country = null; longitudeLatitude = Vector2.zero;
            float distance;
            if (!new Plane(mapNormal, mapBottomLeft).Raycast(ray, out distance) || distance <= 0) return false;
            foreach (var obstruction in Physics.RaycastAll(ray, distance - .015f, ~4, QueryTriggerInteraction.Ignore))
            {
                if (mapSurface && (obstruction.transform == mapSurface || obstruction.transform.IsChildOf(mapSurface))) continue;
                // The original desk interactions use intentionally generous box hit
                // volumes. Empty space inside those boxes must not hide the map.
                // Preserve those colliders and refine only map occlusion against the
                // active authored visual bounds under that interaction.
                var interaction = obstruction.collider.GetComponent<DeskInteractable>();
                if (interaction)
                {
                    bool visibleObstruction = false;
                    foreach (var renderer in interaction.GetComponentsInChildren<Renderer>())
                    {
                        if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer is LineRenderer || renderer is ParticleSystemRenderer) continue;
                        float visualDistance;
                        if (renderer.bounds.IntersectRay(ray, out visualDistance) && visualDistance > 0 && visualDistance < distance - .015f)
                        { visibleObstruction = true; break; }
                    }
                    if (!visibleObstruction) continue;
                }
                return false;
            }
            Vector3 p = ray.GetPoint(distance) - mapBottomLeft, x = mapBottomRight - mapBottomLeft, y = mapTopLeft - mapBottomLeft;
            float u = Vector3.Dot(p, x) / x.sqrMagnitude, v = Vector3.Dot(p, y) / y.sqrMagnitude;
            if (!geographicUV.Contains(new Vector2(u, v))) return false;
            float lon = Mathf.Lerp(minimumLongitude, maximumLongitude, (u - geographicUV.x) / geographicUV.width);
            float lat = Mathf.Lerp(minimumLatitude, maximumLatitude, (v - geographicUV.y) / geographicUV.height);
            foreach (var target in targets)
                if (target && target.Contains(lat, lon)) { country = target.data; longitudeLatitude = new Vector2(lon, lat); return true; }
            return false;
        }

        public bool Launch(CountryData country, float latitude, float longitude)
        {
            if (!country || !game || !pinPrefab || !globe || !meridian || !runtimeRoot || !isActiveAndEnabled) return false;
            if (!country.Contains(latitude, longitude)) { game.legacy.ShowHint("Aim inside the country's boundary."); return false; }
            if (pending.Contains(country.id) || active.Exists(p => p && p.IsActive && p.Country.id == country.id))
            { game.legacy.ShowHint(country.displayName + " already has an active pin."); return false; }
            pending.Add(country.id);
            int skin = game.SelectedSkin, charges = 1 + game.DurabilityLevel;
            float multiplier = game.config.SkinMultiplier(skin);
            StartCoroutine(Flight(country, latitude, longitude, skin, charges, multiplier));
            return true;
        }
        IEnumerator Flight(CountryData country, float latitude, float longitude, int skin, int charges, float multiplier)
        {
            Vector3 destination = GeographicPoint(latitude, longitude), start = muzzle ? muzzle.position : destination + mapNormal;
            var visual = PinVisual.Create(pinPrefab, runtimeRoot, start, mapNormal, boardPinLength, skin);
            flightObjects.Add(visual.gameObject); LaunchedCount++; recoil = 1;
            game.audioManager?.Launch();
            float elapsed = 0;
            while (elapsed < flightSeconds)
            {
                elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, flightSeconds));
                visual.transform.position = Vector3.Lerp(start, destination, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .18f);
                yield return null;
            }
            flightObjects.Remove(visual.gameObject); visual.transform.position = destination;
            var markerGO = new GameObject("Globe marker / " + country.id);
            var marker = markerGO.AddComponent<GlobePinMarker>(); marker.Initialize(globe, latitude, longitude);
            Vector3 outward = (marker.transform.position - globe.Center).normalized;
            marker.Illuminate(PinVisual.Create(pinPrefab, marker.transform, marker.transform.position, outward, markerLength, skin));
            var instance = visual.gameObject.AddComponent<PinInstance>();
            instance.Initialize(this, country, latitude, longitude, marker, charges, multiplier, meridian.NextPass(marker.transform.position));
            active.Add(instance); pending.Remove(country.id); yarn?.Rebuild(active, mapNormal);
            game.legacy.ShowHint(country.displayName + " pinned: " + charges + " pass" + (charges == 1 ? "" : "es") + ", €" + (country.baseValue * multiplier * game.GlobalMultiplier).ToString("N2") + " per meridian strike.");
            game.Notify();
        }
        public PinInstance[] ActivePinsSnapshot() => active.ToArray();
        public void Unregister(PinInstance pin) { active.Remove(pin); yarn?.Rebuild(active, mapNormal); if (game) game.Notify(); }
        public void EmitDust(Vector3 position) { if (corkDust) { var emit = new ParticleSystem.EmitParams { position = position }; corkDust.Emit(emit, 9); } }
        public void TrackDropped(GameObject pin)
        {
            DroppedCount++; dropped.Enqueue(pin);
            while (dropped.Count > 12) { var old = dropped.Dequeue(); if (old) Destroy(old); }
        }
        void OnDisable()
        {
            StopAllCoroutines(); pending.Clear();
            foreach (var pin in flightObjects) if (pin) Destroy(pin); flightObjects.Clear();
            if (launcherVisual) { launcherVisual.localRotation = launcherRest; launcherVisual.localPosition = launcherPosition; }
        }
    }
}
