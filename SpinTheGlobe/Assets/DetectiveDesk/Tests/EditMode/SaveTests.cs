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
            var data = new SaveData { money = 1234.5, gold = 7, lifetimeMoney = 5000 };
            data.skills.Add(new SkillRank { id = "root", rank = 1 });
            store.Save(data);

            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(1234.5, loaded.money, 1e-9);
            Assert.AreEqual(7, loaded.gold, 1e-9);
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
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            Assert.IsTrue(store.TryLoad(out var loaded));
            Assert.AreEqual(10, loaded.money, 1e-9);
        }

        [Test]
        public void MissingFile_ReturnsFalse()
        {
            Assert.IsFalse(store.TryLoad(out _));
            Assert.IsFalse(store.Exists);
        }
    }
}
