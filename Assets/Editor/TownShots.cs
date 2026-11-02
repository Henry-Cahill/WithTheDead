using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// The Introduction town's harness (see <see cref="global::CloneZ.TownShots"/>), run headless:
    /// <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod CloneZ.EditorTools.TownShots.Run
    /// -clonez-out &lt;outDir&gt; [-clonez-town-seed &lt;n&gt;] [-clonez-town-quick] [-clonez-town-checks] -logFile &lt;log&gt;</c>. Writes
    /// &lt;outDir&gt;\*.png and report.md and exits by itself: 0 ok, 1 harness failure, 2 the town logged errors or failed a check.
    /// <c>-clonez-town-checks</c> runs the checks alone, without the pictures.
    /// </summary>
    public static class TownShots
    {
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outDir = ArgValue(args, "-clonez-out");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "TownShots");
            int? seed = int.TryParse(ArgValue(args, "-clonez-town-seed"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : (int?)null;
            bool quick = Array.IndexOf(args, "-clonez-town-quick") >= 0;
            bool shoot = Array.IndexOf(args, "-clonez-town-checks") < 0;
            int code = global::CloneZ.TownShots.ExitHarness;
            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            try
            {
                // Edit mode in a fresh, empty scene: play mode would boot the whole game.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ShaderUtil.allowAsyncCompilation = false;   // never a picture of a shader still compiling
                code = global::CloneZ.TownShots.Run(outDir, seed, GameVolumeProfile(), quick, shoot);
            }
            catch (Exception e)
            {
                Debug.LogError("[TownShots] the harness failed to start: " + e);
                try
                {
                    Directory.CreateDirectory(outDir);
                    File.WriteAllText(Path.Combine(outDir, "report.md"), "# CLONE-Z Introduction town harness\n\nFailed to start: " + e + "\n");
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
