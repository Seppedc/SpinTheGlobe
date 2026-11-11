using System;
using System.IO;
using UnityEngine;

namespace DetectiveDesk.Save
{
    /// <summary>
    /// Crash-safe JSON save file: writes to a temp file, keeps the previous save as .bak,
    /// and falls back to the backup if the main file is unreadable.
    /// </summary>
    public sealed class SaveStore
    {
        public const string DefaultFileName = "spintheglobe_save.json";

        readonly string path;

        public SaveStore(string directory, string fileName = DefaultFileName)
        {
            path = Path.Combine(directory, fileName);
        }

        /// <summary>The store used by the game (Application.persistentDataPath).</summary>
        public static SaveStore Default => new SaveStore(Application.persistentDataPath);

        public string FilePath => path;
        string BackupPath => path + ".bak";
        string TempPath => path + ".tmp";

        public bool Exists => File.Exists(path) || File.Exists(BackupPath);

        public void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.version = SaveData.CurrentVersion;
            data.savedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string json = JsonUtility.ToJson(data, true);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(TempPath, json);
            if (File.Exists(path)) File.Copy(path, BackupPath, true);
            File.Copy(TempPath, path, true);
            File.Delete(TempPath);
        }

        public bool TryLoad(out SaveData data)
        {
            return TryRead(path, out data) || TryRead(BackupPath, out data);
        }

        public void Delete()
        {
            foreach (var p in new[] { path, BackupPath, TempPath })
                if (File.Exists(p)) File.Delete(p);
        }

        static bool TryRead(string file, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(file)) return false;
                var parsed = JsonUtility.FromJson<SaveData>(File.ReadAllText(file));
                if (parsed == null || parsed.version <= 0 || parsed.version > SaveData.CurrentVersion) return false;
                Migrate(parsed);
                parsed.skills ??= new System.Collections.Generic.List<SkillRank>();
                data = parsed;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save file '{file}' could not be read: {e.Message}");
                return false;
            }
        }

        static void Migrate(SaveData data)
        {
            if (data.version < 2 && data.goldShop != null)
            {
                int statueUnlock = 0, bonusRanks = 0;
                for (int i = data.goldShop.Count - 1; i >= 0; i--)
                {
                    var rank = data.goldShop[i];
                    if (rank.id == "unlock_statue_1" || rank.id == "unlock_statue_2" || rank.id == "unlock_statue_3")
                    { statueUnlock = Math.Max(statueUnlock, rank.rank); data.goldShop.RemoveAt(i); }
                    else if (rank.id != null && rank.id.StartsWith("statue_tier_", StringComparison.Ordinal))
                    { bonusRanks += Math.Max(0, rank.rank); data.goldShop.RemoveAt(i); }
                    else if (rank.id == "fast_casting")
                    { rank.rank = Math.Min(2, rank.rank * 2); data.goldShop[i] = rank; }
                    else if (rank.id == "golden_meridian" || rank.id == "start_money") data.goldShop.RemoveAt(i);
                }
                if (statueUnlock > 0) data.goldShop.Add(new SkillRank { id = "unlock_statue_1", rank = 1 });
                if (bonusRanks > 0) data.goldShop.Add(new SkillRank { id = "statue_gold_chance", rank = Math.Min(10, bonusRanks) });
            }
            data.version = SaveData.CurrentVersion;
        }
    }
}
