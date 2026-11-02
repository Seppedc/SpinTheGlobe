using DetectiveDesk.Progression;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Paper balls from lootboxes: press Throw, then click where to throw. A ball that lands in the
    /// waste bin pays a (placeholder) reward. The bin is a simple placeholder model next to the desk
    /// until a final asset exists; move it with <see cref="binOffset"/> (relative to the globe).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PaperBallThrower : MonoBehaviour
    {
        [Tooltip("Where the bin stands, as a point on screen (0..1). A ray from the camera through it finds the desk surface.")]
        public Vector2 binViewportPoint = new Vector2(0.5f, 0.35f);
        [Tooltip("Bin height in metres; the width is 80% of it.")]
        [Min(0.02f)] public float binHeight = 0.5f;
        [Min(0.1f)] public float flightTime = 0.75f;

        public bool IsAiming { get; private set; }
        public Transform Bin { get; private set; }

        GameManager game;
        Material paperMaterial, binMaterial;

        public void Initialize(GameManager owner)
        {
            game = owner;
            CreateBin();
        }

        public void StartAiming()
        {
            if (!game) return;
            if (game.Inventory.PaperBalls <= 0) { game.ShowHint("No paper balls. They come from lootboxes."); return; }
            IsAiming = !IsAiming;
            game.ShowHint(IsAiming ? "Click where to throw the paper ball (aim for the bin). Right-click to cancel." : "Throw cancelled.");
        }

        void Update()
        {
            if (!IsAiming || !game) return;
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) { IsAiming = false; return; }
            if (!Input.GetMouseButtonDown(0) || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            var cam = game.GameCamera;
            if (!cam) { IsAiming = false; return; }
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 target = Physics.Raycast(ray, out var hit, 20f, ~0, QueryTriggerInteraction.Collide) ? hit.point : ray.GetPoint(1.5f);
            if (hit.collider && Bin && hit.collider.transform.IsChildOf(Bin)) target = Bin.position + Vector3.up * binHeight;
            if (game.Inventory.TryUsePaperBall()) Throw(cam, target);
            IsAiming = false;
        }

        void Throw(Camera cam, Vector3 target)
        {
            Vector3 start = cam.transform.position + cam.transform.forward * 0.25f - cam.transform.up * 0.12f;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Paper ball";
            ball.transform.position = start;
            ball.transform.localScale = Vector3.one * 0.09f;
            ball.GetComponent<Renderer>().sharedMaterial = PaperMaterial();
            ball.AddComponent<PaperBallMarker>();
            var body = ball.AddComponent<Rigidbody>();
            body.mass = 0.01f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            float t = flightTime;
            // Ballistic launch so the ball reaches the clicked point after t seconds.
            body.linearVelocity = (target - start) / t - 0.5f * Physics.gravity * t;
            body.angularVelocity = Random.insideUnitSphere * 8f;
            Destroy(ball, 5f);
            if (game.audioManager) game.audioManager.Launch();
        }

        public void Scored(GameObject ball)
        {
            if (!game) return;
            double reward = game.Balance.paperBallReward * game.IncomeScale;
            game.CreditIncome(reward);
            game.ShowHint("Nothing but bin! +" + UIFactory.Money(reward));
            Destroy(ball);
        }

        void CreateBin()
        {
            if (Bin || !game || !game.globe) return;
            var cam = game.GameCamera;
            Vector3 position = game.globe.Center + Vector3.right * 0.6f;
            if (cam)
            {
                // Aim through the chosen screen point, then drop straight down onto the desk collider; repeat once to settle.
                var ray = cam.ViewportPointToRay(binViewportPoint);
                float y = game.globe.Center.y - game.globe.markerLocalRadius * Mathf.Abs(game.globe.rotor ? game.globe.rotor.lossyScale.y : 1f) * 1.5f;
                if (new Plane(Vector3.up, new Vector3(0, y, 0)).Raycast(ray, out float distance) && distance > 0 && distance < 30)
                {
                    position = ray.GetPoint(distance);
                    // Snap onto the desk surface just below/above the estimate.
                    if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out var down, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                        position.y = down.point.y;
                }
            }

            var root = new GameObject("Waste bin (placeholder)");
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            Bin = root.transform;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Bin body";
            body.transform.SetParent(root.transform, false);
            float w = binHeight * 0.8f;
            body.transform.localScale = new Vector3(w, binHeight * 0.5f, w); // primitive cylinder is 2 units tall
            body.transform.localPosition = new Vector3(0, binHeight * 0.5f, 0);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = BinMaterial();

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, binHeight * 0.75f, 0);
            trigger.size = new Vector3(w * 0.9f, binHeight * 0.6f, w * 0.9f);
            root.AddComponent<PaperBallBin>().thrower = this;
        }

        Material PaperMaterial()
        {
            if (paperMaterial) return paperMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            paperMaterial = new Material(shader ? shader : Shader.Find("Standard")) { name = "Runtime paper ball" };
            paperMaterial.SetColor("_BaseColor", new Color(0.92f, 0.9f, 0.84f));
            return paperMaterial;
        }

        Material BinMaterial()
        {
            if (binMaterial) return binMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            binMaterial = new Material(shader ? shader : Shader.Find("Standard")) { name = "Runtime bin" };
            binMaterial.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.08f));
            binMaterial.SetFloat("_Metallic", 0.6f);
            return binMaterial;
        }

        void OnDestroy() { if (paperMaterial) Destroy(paperMaterial); if (binMaterial) Destroy(binMaterial); }
    }
}
