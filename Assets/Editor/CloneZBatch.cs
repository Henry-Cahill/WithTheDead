using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Entry points for running the editor headless (<c>Unity.exe -batchmode -executeMethod ...</c>):
    /// regenerating the IDE project files after a package change and building a standalone player for
    /// the multiplayer smoke test (a host and a client launched side by side).
    /// </summary>
    public static class CloneZBatch
    {
        /// <summary>Regenerates the .csproj / .sln files so command-line builds see every package.</summary>
        public static void SyncProject()
        {
            AssetDatabase.Refresh();
            Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
            Debug.Log("[CloneZBatch] Project files synced via " + Unity.CodeEditor.CodeEditor.CurrentEditor.GetType().Name);
        }

        /// <summary>Builds a Windows player into Builds/CloneZ (used by the networked smoke test).</summary>
        public static void BuildWindowsPlayer()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "CloneZ");
            Directory.CreateDirectory(outDir);
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "CloneZ.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[CloneZBatch] Build result: " + report.summary.result + " (" + report.summary.totalErrors + " errors)");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
