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
            var data = new SaveData { money = 1234.5, gold = 7, lifetimeMoney = 5000, caseLevel = 10000, caseEvidence = 42 };
            data.skills.Add(new SkillRank { id = "root", rank = 1 });
            store.Save(data);

            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(1234.5, loaded.money, 1e-9);
            Assert.AreEqual(7, loaded.gold, 1e-9);
            Assert.AreEqual(10000, loaded.caseLevel);
            Assert.AreEqual(42, loaded.caseEvidence);
            Assert.AreEqual(1, loaded.skills.Count);
            Assert.AreEqual("root", loaded.skills[0].id);
            Assert.Greater(loaded.savedAtUnixSeconds, 0);
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
