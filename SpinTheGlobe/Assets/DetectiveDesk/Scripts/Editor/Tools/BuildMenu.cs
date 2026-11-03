using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DetectiveDesk.Editor
{
    /// <summary>One-click Windows build into SpinTheGlobe/Builds/Windows (ignored by git), to test things the
    /// editor can't show, such as window mode and resolution.</summary>
    public static class BuildMenu
    {
        [MenuItem("Tools/Spin The Globe/Build Windows (test)", priority = 200)]
        public static void BuildWindows()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Windows"));
            Directory.CreateDirectory(dir);
            var scenes = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = Path.Combine(dir, "SpinTheGlobe.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("Build succeeded: " + dir + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB)");
            else Debug.LogError("Build failed: " + report.summary.result + " (" + report.summary.totalErrors + " errors)");
        }
    }
}
