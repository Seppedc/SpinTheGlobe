using System.Collections;
using System.Collections.Generic;
using DetectiveDesk.Core;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using UnityEngine;

namespace DetectiveDesk.Gameplay
{
    /// <summary>
    /// Case file: every pin that falls off the map adds evidence. When the case reaches its threshold
    /// the player presses the stamp (or it auto-stamps by chance) for a payout, then a cooldown runs
    /// before the next, harder case opens. Later cases only accept pins with enough durability upgrades.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaseFileSystem : MonoBehaviour
    {
        public int Level { get; private set; }
        public int Evidence { get; private set; }
        public float Cooldown { get; private set; }

        GameManager game;
        Transform stamp, folder;
        readonly List<GameObject> papers = new List<GameObject>();
        Material paperMaterial;
        bool stamping;
        readonly System.Random rng = new System.Random();

        GameBalance B => game.Balance;
        public int Required => game ? Mathf.Min(B.caseMaxEvidence, B.caseBaseEvidence + B.caseEvidencePerLevel * Level) : 3;
        public int RequiredDurability => game ? Mathf.Min(B.caseMaxDurabilityRequirement, Level / Mathf.Max(1, B.caseLevelsPerDurabilityStep)) : 0;
        public bool IsFull => Evidence >= Required;
        public bool OnCooldown => Cooldown > 0;
        public double Reward => game ? B.caseBaseReward * System.Math.Pow(B.caseRewardGrowth, Level) * game.Stats.Get(StatId.CasePayout) : 0;

        public void Initialize(GameManager owner)
        {
            game = owner;
            stamp = ClickRouter.FindByName("SOLVED Stamp", "Stamp body");
            folder = ClickRouter.FindByName("CaseFiles", "Case file");
            EventBus.Subscribe<PinFellOff>(OnPinFell);
            if (game.legacy) game.legacy.ResetCaseVisual();
            RebuildPapers();
        }

        void OnDestroy() { EventBus.Unsubscribe<PinFellOff>(OnPinFell); if (paperMaterial) Destroy(paperMaterial); }

        void Update()
        {
            if (!game || Cooldown <= 0) return;
            Cooldown = Mathf.Max(0, Cooldown - Time.deltaTime);
            if (Cooldown <= 0)
            {
                if (game.legacy) game.legacy.ResetCaseVisual();
                game.ShowHint("A new case file landed on the desk: case #" + (Level + 1) + ". Needs " + Required + " evidence" + DurabilityNote() + ".");
                game.Notify();
            }
        }

        string DurabilityNote() => RequiredDurability > 0 ? " from pins with durability ≥ " + RequiredDurability : "";

        void OnPinFell(PinFellOff e)
        {
            if (!game) return;
            if (OnCooldown) { game.ShowHint("Pin " + (e.PinIndex + 1) + " fell, but no case is open (next in " + UIFactory.Seconds(Cooldown) + ")."); return; }
            if (IsFull) { game.ShowHint("The case file is full. Press the stamp!"); return; }
            if (e.DurabilityLevel < RequiredDurability)
            {
                game.ShowHint("Pin " + (e.PinIndex + 1) + " is too weak for this case (needs durability ≥ " + RequiredDurability + ").");
                return;
            }
            Evidence++;
            RebuildPapers();
            if (IsFull)
            {
                if (rng.NextDouble() < game.Stats.Get(StatId.CaseAutoStampChance)) Stamp(true);
                else game.ShowHint("Case #" + (Level + 1) + " is complete! Press the stamp to approve it.");
            }
            else game.ShowHint("Evidence added: " + Evidence + " / " + Required + ".");
            game.Notify();
        }

        public void ShowStatus()
        {
            if (!game) return;
            if (OnCooldown) game.ShowHint("Case #" + Level + " is approved. The next case arrives in " + UIFactory.Seconds(Cooldown) + ".");
            else game.ShowHint("Case #" + (Level + 1) + ": " + Evidence + " / " + Required + " evidence" + DurabilityNote() + (IsFull ? ". Press the stamp!" : ". Pins that fall off the map become evidence."));
        }

        /// <summary>Stamp button / clicking the stamp on the desk.</summary>
        public void TryStamp()
        {
            if (!game || stamping) return;
            if (OnCooldown) { game.ShowHint("Next case arrives in " + UIFactory.Seconds(Cooldown) + "."); return; }
            if (!IsFull) { ShowStatus(); return; }
            Stamp(false);
        }

        void Stamp(bool automatic)
        {
            double reward = Reward;
            int level = Level;
            stamping = true;
            StartCoroutine(StampAnimation(() =>
            {
                game.Credit(reward);
                if (game.legacy) game.legacy.PlayCaseSolvedEffects();
                game.ShowHint((automatic ? "Auto-stamped! " : "") + "Case #" + (level + 1) + " APPROVED: +" + UIFactory.Money(reward));
                Level++;
                Evidence = 0;
                Cooldown = (float)game.Stats.Get(StatId.CaseCooldown);
                RebuildPapers();
                stamping = false;
                EventBus.Publish(new CaseStamped(level, reward, automatic));
                game.Notify();
            }));
        }

        IEnumerator StampAnimation(System.Action onImpact)
        {
            if (!stamp) { onImpact(); yield break; }
            Vector3 rest = stamp.position;
            Vector3 down = rest + Vector3.down * 0.05f;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.15f) { stamp.position = Vector3.Lerp(rest, down, t * t); yield return null; }
            stamp.position = down;
            onImpact();
            yield return new WaitForSeconds(0.12f);
            for (float t = 0; t < 1; t += Time.deltaTime / 0.25f) { stamp.position = Vector3.Lerp(down, rest, 1 - (1 - t) * (1 - t)); yield return null; }
            stamp.position = rest;
        }

        /// <summary>Thin paper sheets stacked on the case folder, one per evidence item.</summary>
        void RebuildPapers()
        {
            foreach (var p in papers) if (p) Destroy(p);
            papers.Clear();
            if (!folder) return;
            var bounds = ClickRouter.RendererBounds(folder, folder.position);
            if (!paperMaterial)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) return;
                paperMaterial = new Material(shader) { name = "Runtime evidence paper" };
                paperMaterial.SetColor("_BaseColor", new Color(0.93f, 0.89f, 0.78f));
            }
            float w = Mathf.Max(0.02f, bounds.size.x * 0.55f), d = Mathf.Max(0.02f, bounds.size.z * 0.55f);
            for (int i = 0; i < Evidence; i++)
            {
                var sheet = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sheet.name = "Evidence " + (i + 1);
                Destroy(sheet.GetComponent<Collider>());
                sheet.GetComponent<Renderer>().sharedMaterial = paperMaterial;
                sheet.transform.SetParent(transform, true);
                float jitterX = ((i * 37) % 7 - 3) * w * 0.03f, jitterZ = ((i * 53) % 5 - 2) * d * 0.04f;
                sheet.transform.position = new Vector3(bounds.center.x + jitterX, bounds.max.y + 0.002f + i * 0.0025f, bounds.center.z + jitterZ);
                sheet.transform.rotation = Quaternion.Euler(0, (i * 47) % 9 - 4, 0);
                sheet.transform.localScale = new Vector3(w, 0.002f, d);
                papers.Add(sheet);
            }
        }

        /// <summary>Testing only: fills the open case so it can be stamped.</summary>
        public void DevFill()
        {
            if (!game || OnCooldown) return;
            Evidence = Required;
            RebuildPapers();
            game.ShowHint("[dev] case file filled");
            game.Notify();
        }

        public void LoadState(SaveData d) { Level = Mathf.Max(0, d.caseLevel); Evidence = Mathf.Max(0, d.caseEvidence); Cooldown = Mathf.Max(0, d.caseCooldown); }
        public void SaveState(SaveData d) { d.caseLevel = Level; d.caseEvidence = Evidence; d.caseCooldown = Cooldown; }
    }
}
