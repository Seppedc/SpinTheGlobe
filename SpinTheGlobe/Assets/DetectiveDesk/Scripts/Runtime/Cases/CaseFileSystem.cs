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
        CaseFolderVisual visual;
        public Transform StampTransform => stamp;
        /// <summary>The clickable case-file object on the desk (hover/click target).</summary>
        public Transform FolderTransform => folder;
        /// <summary>Top of the folder's current stack (moves up as clues arrive).</summary>
        public Vector3 FolderLabelAnchor => visual && visual.IsValid ? visual.LabelAnchor : folder ? folder.position : Vector3.zero;
        bool stamping;
        readonly System.Random rng = new System.Random();

        GameBalance B => game.Balance;
        public int Required => game ? CaseProgression.RequiredEvidence(B, Level,
            Mathf.RoundToInt((float)game.Stats.Get(StatId.CaseEvidenceReduction))) : 3;
        public int RequiredDurability => game ? CaseProgression.RequiredStrength(B, Level) : 0;
        public string EvidenceLabel => CaseProgression.EvidenceLabel(Evidence, Required, OnCooldown);
        public bool IsFull => Evidence >= Required;
        public bool OnCooldown => Cooldown > 0;
        /// <summary>Small base-reward increases, with an income-scaled minimum; upgrades apply on top.</summary>
        public double Reward => game
            ? CaseProgression.Reward(B, Level, game.IncomeRate,
                game.Stats.Get(StatId.IncomeMultiplier), game.Stats.Get(StatId.CasePayout))
            : 0;
        /// <summary>Chance an approved case also drops gold (Press Coverage), never above the balance cap (10%).</summary>
        public double GoldChance => game ? System.Math.Min(B.caseGoldChanceCap, game.Stats.Get(StatId.CaseGoldChance)) : 0;
        /// <summary>Gold paid when a case drops gold.</summary>
        public double GoldReward => game ? System.Math.Round(game.Stats.Get(StatId.CaseGold) * (1 + Level / (double)B.caseGoldLevelDivisor)) : 0;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Evidence = Mathf.Min(Evidence, Required);
            stamp = ClickRouter.FindByName("SOLVED Stamp", "Stamp body");
            folder = ClickRouter.FindByName("CaseFiles", "Case file");
            visual = gameObject.AddComponent<CaseFolderVisual>();
            visual.Initialize(ClickRouter.FindByName("Generated_CaseFiles", "CaseFiles_Review"));
            if (!folder) folder = visual.Folder;
            else visual.FitClickCollider(folder);
            EventBus.Subscribe<PinFellOff>(OnPinFell);
            if (game.legacy) game.legacy.ResetCaseVisual();
            if (visual) visual.SetCount(Evidence, Level, false);
        }

        void OnDestroy() { EventBus.Unsubscribe<PinFellOff>(OnPinFell); }

        void Update()
        {
            if (!game || Cooldown <= 0) return;
            Cooldown = Mathf.Max(0, Cooldown - Time.deltaTime);
            if (Cooldown <= 0)
            {
                if (game.legacy) game.legacy.ResetCaseVisual();
                game.Notify();
            }
        }

        void OnPinFell(PinFellOff e)
        {
            if (!game) return;
            if (OnCooldown || IsFull) return;
            if (e.DurabilityLevel < RequiredDurability)
            {
                game.ShowHint("Pin " + (e.PinIndex + 1) + " needs Strength " + RequiredDurability + "+ for this case.");
                return;
            }
            Evidence++;
            if (visual) visual.SetCount(Evidence, Level, true);
            if (IsFull && rng.NextDouble() < game.Stats.Get(StatId.CaseAutoStampChance)) Stamp(true);
            game.Notify();
        }

        /// <summary>Stamp button / clicking the stamp on the desk.</summary>
        public void TryStamp()
        {
            if (!game || stamping || OnCooldown || !IsFull) return;
            Stamp(false);
        }

        void Stamp(bool automatic)
        {
            if (stamping) return;
            stamping = true;
            StartCoroutine(StampSequence(automatic));
        }

        /// <summary>
        /// The stamp lifts off its spot, swings over the case folder, presses SOLVED onto the cover and returns home.
        /// Then the solved folder is slid away and a fresh, empty one is set down.
        /// </summary>
        IEnumerator StampSequence(bool automatic)
        {
            double reward = Reward;
            double gold = rng.NextDouble() < GoldChance ? GoldReward : 0;
            int level = Level;

            Vector3 target = visual && visual.IsValid ? visual.StampPoint : (folder ? folder.position : Vector3.zero);
            if (stamp && target != Vector3.zero)
            {
                Vector3 rest = stamp.position;
                var b = ClickRouter.RendererBounds(stamp, rest);
                // The stamp's pivot is not on the model, so place it by the model's bounds: centre over the folder.
                Vector3 pivotToCenter = b.center - rest;
                Vector3 hover = target + Vector3.up * (b.extents.y + b.size.y * 0.6f) - pivotToCenter;
                Vector3 press = target + Vector3.up * b.extents.y - pivotToCenter;
                Vector3 lift = rest + Vector3.up * b.size.y * 0.8f;

                for (float t = 0; t < 1; t += Time.deltaTime / 0.18f) { stamp.position = Vector3.Lerp(rest, lift, Smooth(t)); yield return null; }
                for (float t = 0; t < 1; t += Time.deltaTime / 0.38f)
                {
                    float e = Smooth(t);
                    stamp.position = Vector3.Lerp(lift, hover, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * b.size.y * 0.5f;
                    yield return null;
                }
                yield return new WaitForSeconds(0.08f);
                for (float t = 0; t < 1; t += Time.deltaTime / 0.09f) { stamp.position = Vector3.Lerp(hover, press, t * t); yield return null; }
                stamp.position = press;

                Approve(automatic, reward, gold, level);
                yield return new WaitForSeconds(0.18f);

                for (float t = 0; t < 1; t += Time.deltaTime / 0.15f) { stamp.position = Vector3.Lerp(press, hover, Smooth(t)); yield return null; }
                for (float t = 0; t < 1; t += Time.deltaTime / 0.42f)
                {
                    float e = Smooth(t);
                    stamp.position = Vector3.Lerp(hover, lift, e) + Vector3.up * Mathf.Sin(t * Mathf.PI) * b.size.y * 0.4f;
                    yield return null;
                }
                for (float t = 0; t < 1; t += Time.deltaTime / 0.18f) { stamp.position = Vector3.Lerp(lift, rest, Smooth(t)); yield return null; }
                stamp.position = rest;
            }
            else Approve(automatic, reward, gold, level);

            // Let the player see the stamped folder, then swap it for a new empty one.
            yield return new WaitForSeconds(0.7f);
            if (visual && visual.IsValid) yield return visual.SwapFolder();
            stamping = false;
        }

        /// <summary>The stamp hits the folder: pay out, mark the folder SOLVED and start the next case.</summary>
        void Approve(bool automatic, double reward, double gold, int level)
        {
            // The payout numbers float up from the case file, not the globe.
            Vector3? at = null;
            var folder = visual && visual.Folder ? visual.Folder : FolderTransform;
            if (folder) { var b = ClickRouter.RendererBounds(folder, folder.position); at = new Vector3(b.center.x, b.max.y, b.center.z); }
            game.Credit(reward, at);
            if (gold > 0) game.CreditGold(gold, at);
            if (visual) visual.ShowSolvedMark(level);
            if (game.audioManager) game.audioManager.Click();
            Level++;
            Evidence = 0;
            Cooldown = (float)game.Stats.Get(StatId.CaseCooldown);
            EventBus.Publish(new CaseStamped(level, reward, automatic));
            game.Notify();
        }

        static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }

        /// <summary>Testing only: fills the open case so it can be stamped.</summary>
        public void DevFill()
        {
            if (!game || OnCooldown) return;
            Evidence = Required;
            if (visual) visual.SetCount(Evidence, Level, true);
            game.Notify();
        }

        public void LoadState(SaveData d) { Level = Mathf.Max(0, d.caseLevel); Evidence = Mathf.Max(0, d.caseEvidence); Cooldown = Mathf.Max(0, d.caseCooldown); }
        public void SaveState(SaveData d) { d.caseLevel = Level; d.caseEvidence = Evidence; d.caseCooldown = Cooldown; }
    }
}
