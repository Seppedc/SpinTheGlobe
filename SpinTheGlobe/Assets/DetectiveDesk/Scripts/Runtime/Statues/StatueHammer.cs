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
                Vector3 startCenter = hb.center;
                Quaternion startRot = hammer.rotation;
                var poses = new List<(Transform t, Vector3 offset, Quaternion rot)>();
                foreach (var t in carried) if (t) poses.Add((t, t.position - startCenter, t.rotation));

                // Hold the hammer upright-ish, head over the statue, swinging in the plane facing the camera.
                Vector3 toCam = cam ? Vector3.ProjectOnPlane(cam.transform.position - impact, Vector3.up).normalized : Vector3.back;
                Vector3 swingAxis = Vector3.Cross(Vector3.up, toCam).normalized;              // horizontal, across the view
                float reach = Mathf.Max(hb.extents.x, hb.extents.z);
                Vector3 overStatue = impact + Vector3.up * (reach * 0.9f) + toCam * reach * 0.25f;
                Quaternion held = Quaternion.AngleAxis(-20f, swingAxis) * startRot;
                Quaternion wound = Quaternion.AngleAxis(-windUpAngle, swingAxis) * startRot;
                Quaternion struck = Quaternion.AngleAxis(strikeAngle, swingAxis) * startRot;
                Vector3 lifted = startCenter + Vector3.up * reach * 0.6f;

                void Pose(Vector3 center, Quaternion rot)
                {
                    var delta = rot * Quaternion.Inverse(startRot);
                    foreach (var (t, offset, r) in poses) if (t) t.SetPositionAndRotation(center + delta * offset, delta * r);
                }

                yield return Animate(carrySeconds * 0.35f, e => Pose(Vector3.Lerp(startCenter, lifted, e), Quaternion.Slerp(startRot, held, e)));
                yield return Animate(carrySeconds, e => Pose(Vector3.Lerp(lifted, overStatue, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * reach * 0.3f, held));
                yield return Animate(windUpSeconds, e => Pose(overStatue + Vector3.up * reach * 0.25f * e, Quaternion.Slerp(held, wound, e)));
                yield return Animate(strikeSeconds, e => Pose(Vector3.Lerp(overStatue + Vector3.up * reach * 0.25f, impact + Vector3.up * reach * 0.35f, e * e), Quaternion.Slerp(wound, struck, e * e)));

                Shatter(statue, target);

                yield return new WaitForSeconds(0.12f);
                Vector3 after = impact + Vector3.up * reach * 0.35f;
                yield return Animate(returnSeconds * 0.4f, e => Pose(Vector3.Lerp(after, overStatue, e), Quaternion.Slerp(struck, held, e)));
                yield return Animate(returnSeconds, e => Pose(Vector3.Lerp(overStatue, startCenter, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * reach * 0.3f, Quaternion.Slerp(held, startRot, e)));
                Pose(startCenter, startRot);
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
