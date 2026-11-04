using System;
using System.IO;
using DetectiveDesk.Save;
using UnityEditor;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>
    /// Copies the save file to SpinTheGlobe/Builds/SaveBackups (ignored by git) and puts it back, so destructive tests
    /// (retiring, restarting) can run on a real save without losing it.
    /// </summary>
    public static class SaveBackupMenu
    {
        static string BackupDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "SaveBackups"));
        static string SavePath => SaveStore.Default.FilePath;

        [MenuItem("Tools/Spin The Globe/Save Backup/Back Up Save File", priority = 300)]
        public static void Backup()
        {
            if (!File.Exists(SavePath)) { Debug.LogWarning("No save file to back up at " + SavePath); return; }
            Directory.CreateDirectory(BackupDir);
            string target = Path.Combine(BackupDir, "save_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json");
            File.Copy(SavePath, target, true);
            File.Copy(SavePath, Path.Combine(BackupDir, "latest.json"), true);
            Debug.Log("Save backed up to " + target);
        }

        [MenuItem("Tools/Spin The Globe/Save Backup/Restore Latest Backup", priority = 301)]
        public static void Restore()
        {
            string latest = Path.Combine(BackupDir, "latest.json");
            if (!File.Exists(latest)) { Debug.LogWarning("No backup found at " + latest); return; }
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode before restoring the save."); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.Copy(latest, SavePath, true);
            string bak = SavePath + ".bak";
            if (File.Exists(bak)) File.Delete(bak);
            Debug.Log("Save restored from " + latest);
        }

        [MenuItem("Tools/Spin The Globe/Save Backup/Restore Latest Backup", true)]
        static bool CanRestore() => !EditorApplication.isPlaying;
    }
}
