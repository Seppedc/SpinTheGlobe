using DetectiveDesk.Core;
using DetectiveDesk.Gameplay;
using DetectiveDesk.Progression;
using DetectiveDesk.Save;
using NUnit.Framework;
using UnityEngine;

namespace DetectiveDesk.Tests
{
    public sealed class CaseUnlockTests
    {
        GameObject root;
        GameManager game;
        CaseFileSystem cases;
        GameBalance balance;
        SkillTreeAsset tree;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            root = new GameObject("Case unlock rules test");
            root.SetActive(false);
            game = root.AddComponent<GameManager>();
            balance = ScriptableObject.CreateInstance<GameBalance>();
            tree = ScriptableObject.CreateInstance<SkillTreeAsset>();
            tree.SetNodes(DefaultSkillTreeFactory.Create());
            game.balance = balance;
            game.skillTreeAsset = tree;
            game.useSaveFile = false;
            game.PrepareScenePresentation();
            cases = root.AddComponent<CaseFileSystem>();
            cases.Bind(game);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(balance);
            Object.DestroyImmediate(tree);
            EventBus.Clear();
        }

        [Test]
        public void LockedCases_IgnoreEvidenceAndDoNotPayEvenWithSavedEvidence()
        {
            cases.LoadState(new SaveData { caseEvidence = cases.Required });
            Assert.IsFalse(cases.Unlocked);
            Assert.IsFalse(cases.IsFull);
            Assert.AreEqual("Unlock Case Files", cases.StatusLabel);
            Assert.AreEqual(0, cases.Reward);
            cases.TryStamp();
            Assert.AreEqual(0, game.Wallet.Money);
            Assert.AreEqual(0, cases.Level);
            cases.LoadState(new SaveData());
            EventBus.Publish(new PinFellOff(0, 0));
            cases.DevFill();
            Assert.AreEqual(0, cases.Evidence);
        }

        [Test]
        public void PurchasingUnlock_StartsCollectingOnlyNewEvidence()
        {
            EventBus.Publish(new PinFellOff(0, 0));
            game.Wallet.Add(Economy.Currency.Money, 1000);
            foreach (var id in new[] { DefaultSkillTreeFactory.RootId, DefaultSkillTreeFactory.PinId(1), DefaultSkillTreeFactory.CaseUnlockId })
                Assert.IsTrue(game.Skills.TryPurchase(tree, id, game.Wallet, game.Stats, out _));
            Assert.IsTrue(cases.Unlocked);
            Assert.AreEqual(0, cases.Evidence, "locked pin falls were not banked");
            EventBus.Publish(new PinFellOff(0, 0));
            Assert.AreEqual(1, cases.Evidence);
            Assert.AreEqual("Evidence: 1/3", cases.StatusLabel);
            Assert.Greater(cases.Reward, 0);
        }

        [Test]
        public void UnlockRank_IsRestoredFromSave_AndRemovedWithNormalSkills()
        {
            game.Skills.Load(tree, new[] { new SkillRank { id = DefaultSkillTreeFactory.CaseUnlockId, rank = 1 } });
            game.Stats.Rebuild(tree, game.Skills);
            Assert.IsTrue(cases.Unlocked);
            game.Skills.Load(tree, new SkillRank[0]);
            game.Stats.Rebuild(tree, game.Skills);
            Assert.IsFalse(cases.Unlocked);
        }

        [Test]
        public void ClaimPrompt_IsUnavailableForLockedOrCoolingDownCases()
        {
            cases.LoadState(new SaveData { caseEvidence = cases.Required });
            Assert.IsFalse(cases.CanClaim);
            game.Skills.Load(tree, new[] { new SkillRank { id = DefaultSkillTreeFactory.CaseUnlockId, rank = 1 } });
            game.Stats.Rebuild(tree, game.Skills);
            Assert.IsTrue(cases.CanClaim);
            cases.LoadState(new SaveData { caseEvidence = cases.Required, caseCooldown = 5 });
            Assert.IsFalse(cases.CanClaim, "full evidence during cooldown is not claimable");
        }

        [Test]
        public void ClaimPrompt_ReturnsForEveryNewCompletedCase()
        {
            game.Skills.Load(tree, new[] { new SkillRank { id = DefaultSkillTreeFactory.CaseUnlockId, rank = 1 } });
            game.Stats.Rebuild(tree, game.Skills);
            foreach (int level in new[] { 0, 1, 5, 16 })
            {
                cases.LoadState(new SaveData { caseLevel = level });
                Assert.IsFalse(cases.CanClaim);
                cases.LoadState(new SaveData { caseLevel = level, caseEvidence = cases.Required - 1 });
                Assert.IsFalse(cases.CanClaim);
                cases.LoadState(new SaveData { caseLevel = level, caseEvidence = cases.Required });
                Assert.IsTrue(cases.CanClaim, "completion is recurring rather than a saved first-use prompt");
            }
        }
    }
}
