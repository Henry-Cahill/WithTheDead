using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// The isometric cutaway's harness (see <see cref="global::CloneZ.CutawayShots"/>), run headless:
    /// <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod CloneZ.EditorTools.CutawayShots.Run
    /// -clonez-out &lt;outDir&gt; [-clonez-cutaway-place &lt;name&gt;] -logFile &lt;log&gt;</c> with <c>CLONEZ_WORLD</c> pointing at a
    /// world store. Writes &lt;outDir&gt;\*.png and report.md and exits by itself: 0 ok, 1 harness failure, 2 errors logged
    /// (or a check failed), 3 no world store or no such place.
    /// </summary>
    public static class CutawayShots
    {
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outDir = ArgValue(args, "-clonez-out");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "CutawayShots");
            int code = global::CloneZ.CutawayShots.ExitHarness;
            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            try
            {
                // Edit mode in a fresh, empty scene: play mode would boot the whole game.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ShaderUtil.allowAsyncCompilation = false;   // never a picture of a shader still compiling
                code = global::CloneZ.CutawayShots.Run(outDir, ArgValue(args, "-clonez-cutaway-place"), GameVolumeProfile());
            }
            catch (Exception e)
            {
                Debug.LogError("[CutawayShots] the harness failed to start: " + e);
                try
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllText(Path.Combine(outDir, "report.md"), "# CLONE-Z isometric cutaway harness\n\nFailed to start: " + e + "\n");
                }
                catch (Exception) { }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncShaders;
            }
            EditorApplication.Exit(code);
        }

        private static string ArgValue(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal) ? args[i + 1] : null;
        }

        // The game scene's Global Volume profile (its bloom, tonemapping and vignette), so the pictures are graded as the game is.
        private static VolumeProfile GameVolumeProfile()
        {
            string scene = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).FirstOrDefault() ?? "Assets/Scenes/SampleScene.unity";
            foreach (string dep in AssetDatabase.GetDependencies(scene, false))
                if (AssetDatabase.GetMainAssetTypeAtPath(dep) == typeof(VolumeProfile)) return AssetDatabase.LoadAssetAtPath<VolumeProfile>(dep);
            return AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");
        }
    }
}
