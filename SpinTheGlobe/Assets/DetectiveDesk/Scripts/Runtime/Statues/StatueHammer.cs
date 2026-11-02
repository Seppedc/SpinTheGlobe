using System.Collections;
using DetectiveDesk.Core;
using DetectiveDesk.Economy;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>Swings the desk's jeweler hammer at a golden statue, bursts it into gold shards and pays gold.</summary>
    [DisallowMultipleComponent]
    public sealed class StatueHammer : MonoBehaviour
    {
        GameManager game;
        Transform hammer;
        Material shardMaterial;

        public void Initialize(GameManager owner)
        {
            game = owner;
            var head = ClickRouter.FindByName("Hammer head", "hammer head");
            hammer = head ? (head.parent ? head.parent : head) : ClickRouter.FindByName("JewelerHammer", "hammer");
        }

        public void Smash(FigurinePedestal statue)
        {
            if (!statue || statue.IsBusy) return;
            StartCoroutine(SmashRoutine(statue));
        }

        IEnumerator SmashRoutine(FigurinePedestal statue)
        {
            statue.IsBusy = true;
            var target = statue.VisualBounds;
            Vector3 impact = target.center + Vector3.up * target.extents.y * 0.6f;

            Vector3 restPos = Vector3.zero; Quaternion restRot = Quaternion.identity;
            if (hammer)
            {
                restPos = hammer.position; restRot = hammer.rotation;
                Vector3 above = impact + Vector3.up * 0.18f + Vector3.back * 0.05f;
                yield return Move(hammer, restPos, above, restRot, restRot * Quaternion.Euler(-35, 0, 0), 0.25f);
                yield return Move(hammer, above, impact, hammer.rotation, restRot * Quaternion.Euler(25, 0, 0), 0.09f);
            }

            Burst(target);
            double gold = statue.Shatter();
            game.CreditGold(gold);
            if (game.Inventory.SparePieces > 0)
                while (!statue.IsComplete && game.Inventory.TryUseSparePiece()) statue.AddPiece();
            game.ShowHint("Statue " + (statue.Index + 1) + " smashed: +" + NumberFormatter.Format(gold) + " gold!");
            EventBus.Publish(new StatueSmashed(statue.Index, gold));
            game.Notify();

            if (hammer)
            {
                yield return new WaitForSeconds(0.1f);
                yield return Move(hammer, hammer.position, restPos, hammer.rotation, restRot, 0.35f);
            }
            statue.IsBusy = false;
        }

        static IEnumerator Move(Transform t, Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float seconds)
        {
            for (float k = 0; k < 1; k += Time.deltaTime / Mathf.Max(0.01f, seconds))
            {
                float e = k * k * (3 - 2 * k);
                t.SetPositionAndRotation(Vector3.Lerp(from, to, e), Quaternion.Slerp(fromRot, toRot, e));
                yield return null;
            }
            t.SetPositionAndRotation(to, toRot);
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
