using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// A dedicated server plays no audio, so the radio music shipped under StreamingAssets/Music (gigabytes of it)
    /// is stripped from Dedicated Server builds once the player has been written.
    /// </summary>
    internal sealed class ServerBuildMusic : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.GetSubtarget<StandaloneBuildSubtarget>() != StandaloneBuildSubtarget.Server) return;
            string root = report.summary.outputPath;
            if (File.Exists(root)) root = Path.GetDirectoryName(root);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return;
            foreach (string dir in Directory.GetDirectories(root, "StreamingAssets", SearchOption.AllDirectories))
            {
                string music = Path.Combine(dir, "Music");
                if (!Directory.Exists(music)) continue;
                Directory.Delete(music, true);
                Debug.Log("[CloneZ] Stripped the radio music from the server build: " + music);
            }
        }
    }
}
