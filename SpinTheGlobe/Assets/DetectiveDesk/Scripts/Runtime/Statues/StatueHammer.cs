using System.Collections;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Picks up the jeweler hammer, carries it over the golden statue that was clicked, winds up and strikes,
    /// bursts the statue into gold shards (paying gold) and puts the hammer back exactly where it was.
    /// Works for any number of statues: the target comes from the clicked statue's bounds.
    /// Only the hammer moves (the visible model plus its old placeholder parts), never its parent group.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatueHammer : MonoBehaviour
    {
        [Tooltip("Seconds: pick up and carry, wind up, strike, put back.")]
        public float carrySeconds = 0.4f, windUpSeconds = 0.18f, strikeSeconds = 0.08f, returnSeconds = 0.45f;
        [Tooltip("Degrees the hammer leans back before the strike and forward on impact.")]
        public float windUpAngle = 55f, strikeAngle = 30f;

        GameManager game;
        Transform hammer;                                            // visible model (moved by its bounds centre)
        readonly List<Transform> carried = new List<Transform>();    // everything that moves with it
        Material shardMaterial;
        bool busy;

        public void Initialize(GameManager owner)
        {
            game = owner;
            var model = ClickRouter.FindByName("Generated_JewelerHammer", "JewelerHammer");
            var head = ClickRouter.FindByName("Hammer head");
            var handle = ClickRouter.FindByName("Small jeweler hammer handle");
            hammer = model ? model : head;
            foreach (var t in new[] { hammer, head, handle })
                if (t && !carried.Contains(t) && !IsGroupOfOthers(t)) carried.Add(t);
        }

        /// <summary>Never carry a container such as "Decorations" (it holds the glass and the bottle too).</summary>
        static bool IsGroupOfOthers(Transform t) => t.name == "Decorations" || t.name == "GameplayObjects" || t.name == "Environment";

        public void Smash(FigurinePedestal statue)
        {
            if (!statue || statue.IsBusy || busy) return;
            StartCoroutine(SmashRoutine(statue));
        }

        IEnumerator SmashRoutine(FigurinePedestal statue)
        {
            busy = true;
            statue.IsBusy = true;
            var target = statue.VisualBounds;
            Vector3 impact = target.center + Vector3.up * target.extents.y * 0.55f;

            if (hammer)
            {
                var cam = game.GameCamera;
                var hb = ClickRouter.RendererBounds(hammer, hammer.position);
                Vector3 restCenter = hb.center;
                var poses = new List<(Transform t, Vector3 offset, Quaternion rot)>();
                foreach (var t in carried) if (t) poses.Add((t, t.position - restCenter, t.rotation));

                // Find the head and the grip end of the hammer model, and which way the striking face points.
                Vector3 head = restCenter, face = Vector3.down;
                var headPart = Find(hammer, "Head_BrassBody") ?? Find(hammer, "Head");
                if (headPart) head = ClickRouter.RendererBounds(headPart, headPart.position).center;
                var strike = Find(hammer, "Head_StrikingFace");
                if (strike) face = (ClickRouter.RendererBounds(strike, strike.position).center - head).normalized;
                Vector3 shaft = head - restCenter;
                if (shaft.sqrMagnitude < 1e-6f) shaft = hammer.forward;
                // The far end of the handle: the handle part's point furthest from the head, else mirror the head through the middle.
                Vector3 grip = restCenter - shaft;
                var handlePart = Find(hammer, "Handle_Walnut") ?? Find(hammer, "Handle");
                if (handlePart)
                {
                    var hbnd = ClickRouter.RendererBounds(handlePart, handlePart.position);
                    grip = hbnd.center - (head - hbnd.center).normalized * hbnd.extents.magnitude;
                }
                Vector3 shaftDir = (head - grip).normalized;
                float length = (head - grip).magnitude;
                face = Vector3.ProjectOnPlane(face, shaftDir).normalized;
                if (face.sqrMagnitude < 0.5f) face = Vector3.ProjectOnPlane(Vector3.down, shaftDir).normalized;
                Quaternion restBasis = Quaternion.LookRotation(shaftDir, face);

                // At impact the hammer is seen side-on: the grip is to the right of the statue (screen right), the handle
                // runs leftwards to the head nearly level, and the striking face points straight down onto the statue.
                // The swing therefore happens in the plane of the screen, where it reads clearly.
                Vector3 screenRight = cam ? Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized : Vector3.right;
                Vector3 hitShaft = (-screenRight + Vector3.down * 0.12f).normalized;
                Quaternion hitBasis = Quaternion.LookRotation(hitShaft, Vector3.ProjectOnPlane(Vector3.down, hitShaft));
                Quaternion hitRot = hitBasis * Quaternion.Inverse(restBasis);
                float headRadius = headPart ? ClickRouter.RendererBounds(headPart, headPart.position).extents.magnitude * 0.5f : length * 0.08f;
                Vector3 headAtImpact = impact + Vector3.up * headRadius;
                Vector3 gripAtImpact = headAtImpact - hitShaft * length;
                Vector3 liftAxis = Vector3.Cross(hitShaft, Vector3.up).normalized;           // turning about it raises the head

                // Places the hammer so that its grip end is at gripPos, turned by rot (relative to how it lies on the desk).
                void Pose(Vector3 gripPos, Quaternion rot)
                {
                    Vector3 center = gripPos - rot * (grip - restCenter);
                    foreach (var (t, offset, r) in poses) if (t) t.SetPositionAndRotation(center + rot * offset, rot * r);
                }
                Quaternion Raised(float degrees) => Quaternion.AngleAxis(degrees, liftAxis) * hitRot;

                Vector3 gripRest = grip;
                Vector3 gripLifted = grip + Vector3.up * length * 0.4f;
                Vector3 gripReady = gripAtImpact + Vector3.up * length * 0.15f;
                Quaternion ready = Raised(windUpAngle * 0.5f), wound = Raised(windUpAngle), struck = Raised(-6f);

                yield return Animate(carrySeconds * 0.35f, e => Pose(Vector3.Lerp(gripRest, gripLifted, e), Quaternion.Slerp(Quaternion.identity, ready, e * 0.5f)));
                yield return Animate(carrySeconds, e => Pose(Vector3.Lerp(gripLifted, gripReady, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * length * 0.2f,
                                                              Quaternion.Slerp(Quaternion.Slerp(Quaternion.identity, ready, 0.5f), ready, e)));
                yield return Animate(windUpSeconds, e => Pose(Vector3.Lerp(gripReady, gripAtImpact, e), Quaternion.Slerp(ready, wound, e)));
                yield return Animate(strikeSeconds, e => Pose(gripAtImpact, Quaternion.Slerp(wound, struck, e * e)));

                Shatter(statue, target);

                yield return new WaitForSeconds(0.12f);
                yield return Animate(returnSeconds * 0.35f, e => Pose(Vector3.Lerp(gripAtImpact, gripReady, e), Quaternion.Slerp(struck, ready, e)));
                yield return Animate(returnSeconds, e => Pose(Vector3.Lerp(gripReady, gripRest, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * length * 0.25f,
                                                              Quaternion.Slerp(ready, Quaternion.identity, e)));
                Pose(gripRest, Quaternion.identity);
            }
            else Shatter(statue, target);

            statue.IsBusy = false;
            busy = false;
        }

        void Shatter(FigurinePedestal statue, Bounds target)
        {
            Burst(target);
            if (game.audioManager) game.audioManager.Click();
            double gold = statue.Shatter();
            game.CreditGold(gold);
            if (game.Inventory.SparePieces > 0)
                while (!statue.IsComplete && game.Inventory.TryUseSparePiece()) statue.AddPiece();
            game.ShowHint("Statue " + (statue.Index + 1) + " smashed: +" + NumberFormatter.Format(gold) + " gold!");
            EventBus.Publish(new StatueSmashed(statue.Index, gold));
            game.Notify();
        }

        static Transform Find(Transform root, string prefix)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(prefix) && t.GetComponent<Renderer>()) return t;
            return null;
        }

        static IEnumerator Animate(float seconds, System.Action<float> step)
        {
            for (float k = 0; k < 1; k += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                step(k * k * (3 - 2 * k));
                yield return null;
            }
            step(1);
        }

        void Burst(Bounds b)
        {
            if (!shardMaterial)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) return;
                shardMaterial = new Material(shader) { name = "Runtime gold shard" };
                shardMaterial.SetColor("_BaseColor", new Color(0.9f, 0.66f, 0.2f));
                shardMaterial.SetFloat("_Metallic", 0.95f);
                shardMaterial.SetFloat("_Smoothness", 0.8f);
            }
            float size = Mathf.Clamp(b.size.magnitude * 0.06f, 0.004f, 0.03f);
            for (int i = 0; i < 18; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "Gold shard";
                shard.GetComponent<Renderer>().sharedMaterial = shardMaterial;
                shard.transform.position = b.center + Random.insideUnitSphere * b.extents.magnitude * 0.3f;
                shard.transform.rotation = Random.rotation;
                shard.transform.localScale = Vector3.one * size * Random.Range(0.6f, 1.4f);
                var body = shard.AddComponent<Rigidbody>();
                body.mass = 0.01f;
                body.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.2f) * 1.4f;
                body.angularVelocity = Random.insideUnitSphere * 12f;
                Destroy(shard, 2.5f);
            }
        }

        void OnDestroy() { if (shardMaterial) Destroy(shardMaterial); }
    }
}
