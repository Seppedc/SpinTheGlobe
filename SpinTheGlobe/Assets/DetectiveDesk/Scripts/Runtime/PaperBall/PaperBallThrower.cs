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
        [Tooltip("Bin position relative to the globe centre, in camera-right / world-up / camera-forward metres.")]
        public Vector3 binOffset = new Vector3(0.75f, -0.22f, -0.15f);
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
            if (hit.collider && Bin && hit.collider.transform.IsChildOf(Bin)) target = Bin.position + Vector3.up * 0.1f;
            if (game.Inventory.TryUsePaperBall()) Throw(cam, target);
            IsAiming = false;
        }

        void Throw(Camera cam, Vector3 target)
        {
            Vector3 start = cam.transform.position + cam.transform.forward * 0.25f - cam.transform.up * 0.12f;
            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "Paper ball";
            ball.transform.position = start;
            ball.transform.localScale = Vector3.one * 0.045f;
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
            double reward = game.Balance.paperBallReward * game.Stats.Get(StatId.GlobeMultiplier);
            game.Credit(reward);
            game.ShowHint("Nothing but bin! +" + UIFactory.Money(reward));
            Destroy(ball);
        }

        void CreateBin()
        {
            if (Bin || !game || !game.globe) return;
            var cam = game.GameCamera;
            Vector3 right = cam ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 position = game.globe.Center + right * binOffset.x + Vector3.up * binOffset.y + forward * binOffset.z;

            var root = new GameObject("Waste bin (placeholder)");
            root.transform.SetParent(transform, false);
            root.transform.position = position;
            Bin = root.transform;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Bin body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.16f, 0.09f, 0.16f); // 0.18 m tall
            body.transform.localPosition = new Vector3(0, 0.09f, 0);
            Destroy(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = BinMaterial();

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 0.12f, 0);
            trigger.size = new Vector3(0.14f, 0.1f, 0.14f);
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
