using System.IO;
using DetectiveDesk.Save;
using NUnit.Framework;

namespace DetectiveDesk.Tests
{
    public sealed class SaveTests
    {
        string dir;
        SaveStore store;

        [SetUp]
        public void SetUp()
        {
            dir = Path.Combine(Path.GetTempPath(), "SpinTheGlobeTests_" + System.Guid.NewGuid().ToString("N"));
            store = new SaveStore(dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        [Test]
        public void SaveAndLoad_RoundTrips()
        {
            var data = new SaveData { money = 1234.5, gold = 7, lifetimeMoney = 5000, caseLevel = 10000, caseEvidence = 42, gameCompleted = true, paperShotsScored = 12345, whiskeyIceCount = 2, whiskeyIceMeltTimer = 45.5f };
            data.skills.Add(new SkillRank { id = "root", rank = 1 });
            data.completedGuidance.Add("globe_spin");
            store.Save(data);

            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(1234.5, loaded.money, 1e-9);
            Assert.AreEqual(7, loaded.gold, 1e-9);
            Assert.AreEqual(10000, loaded.caseLevel);
            Assert.AreEqual(42, loaded.caseEvidence);
            Assert.AreEqual(12345, loaded.paperShotsScored);
            Assert.AreEqual(2, loaded.whiskeyIceCount);
            Assert.AreEqual(45.5f, loaded.whiskeyIceMeltTimer, 1e-4f);
            Assert.IsTrue(loaded.gameCompleted);
            Assert.AreEqual(1, loaded.skills.Count);
            Assert.AreEqual("root", loaded.skills[0].id);
            CollectionAssert.Contains(loaded.completedGuidance, "globe_spin");
            Assert.Greater(loaded.savedAtUnixSeconds, 0);
        }

        [Test]
        public void LegacyGoldShop_MigratesOnce_WithoutChangingWalletOrSkillRanks()
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(store.FilePath, "{\"version\":1,\"gold\":17,\"skills\":[{\"id\":\"root\",\"rank\":1}],\"goldShop\":[{\"id\":\"unlock_statue_2\",\"rank\":1},{\"id\":\"statue_tier_2\",\"rank\":2},{\"id\":\"fast_casting\",\"rank\":1},{\"id\":\"start_money\",\"rank\":3}]}");
            Assert.IsTrue(store.TryLoad(out var data));
            Assert.AreEqual(SaveData.CurrentVersion, data.version);
            Assert.AreEqual(17, data.gold);
            Assert.AreEqual(1, data.skills[0].rank);
            Assert.AreEqual(1, data.goldShop.Find(r => r.id == "unlock_statue_1").rank);
            Assert.AreEqual(2, data.goldShop.Find(r => r.id == "statue_gold_chance").rank);
            Assert.AreEqual(2, data.goldShop.Find(r => r.id == "fast_casting").rank);
            Assert.IsFalse(data.goldShop.Exists(r => r.id == "start_money"));
            store.Save(data);
            Assert.IsTrue(store.TryLoad(out data));
            Assert.AreEqual(2, data.goldShop.Find(r => r.id == "fast_casting").rank);
        }

        [Test]
        public void CorruptMainFile_FallsBackToBackup()
        {
            store.Save(new SaveData { money = 10 });
            store.Save(new SaveData { money = 20 }); // first save becomes .bak
            File.WriteAllText(store.FilePath, "{ not json");
            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(10, loaded.money, 1e-9);
        }

        [Test]
        public void Delete_RemovesAllSaveCopies_AndPurchasedSkills()
        {
            var data = new SaveData { money = 10, gold = 1, prestiges = 2 };
            data.skills.Add(new SkillRank { id = "root", rank = 1 });
            store.Save(data);
            store.Save(data); // creates the fallback backup
            File.WriteAllText(store.FilePath + ".tmp", "unfinished save");

            store.Delete();

            Assert.IsFalse(File.Exists(store.FilePath));
            Assert.IsFalse(File.Exists(store.FilePath + ".bak"));
            Assert.IsFalse(File.Exists(store.FilePath + ".tmp"));
            Assert.IsFalse(store.Exists);
            Assert.IsFalse(store.TryLoad(out _), "the skill tree must not return from a backup after reset");
            store.Delete(); // resetting an already-empty save is safe
        }

        [Test]
        public void MissingFile_ReturnsFalse()
        {
            Assert.IsFalse(store.TryLoad(out _));
            Assert.IsFalse(store.Exists);
        }
    }
}
