using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Entry points for running the editor headless (<c>Unity.exe -batchmode -executeMethod ...</c>):
    /// regenerating the IDE project files after a package change, building a standalone player for
    /// the multiplayer smoke test (a host and a client launched side by side), the co-op release (the game and its
    /// Linux dedicated server, see Tools/Server), and the map harness.
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

        /// <summary>Builds a Windows player into Builds/WithTheDead (used by the networked smoke test).</summary>
        public static void BuildWindowsPlayer()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WithTheDead");
            Directory.CreateDirectory(outDir);
            RuntimeShaders.Ensure();
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "WithTheDead.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[CloneZBatch] Build result: " + report.summary.result + " (" + report.summary.totalErrors + " errors)");
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>
        /// The co-op release (Tools/Server/Release.ps1 runs it in a batch editor on a copy of the project): the Windows game
        /// for players and the Linux Dedicated Server, both stamped with one version, into
        /// <c>-clonez-release-out &lt;dir&gt;</c>\client and \server. <c>-clonez-release-stamp &lt;stamp&gt;</c> is added to the
        /// project's version ("0.1.0+&lt;stamp&gt;", <see cref="global::CloneZ.BuildInfo"/>); <c>-clonez-release-only
        /// client|server</c> builds just one. It changes the project's player settings (the version; the server's
        /// scripting backend and defines; the server optimizations that would strip the shaders and fonts the game makes
        /// its materials and text from) and brings <see cref="RuntimeShaders"/> up to date, so run it on a copy, never on
        /// the project you work in. Exits 0 when all of it built.
        /// </summary>
        public static void BuildRelease()
        {
            int code = 1;
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                string outDir = ArgValue(args, "-clonez-release-out");
                string stamp = ArgValue(args, "-clonez-release-stamp");
                string only = ArgValue(args, "-clonez-release-only");
                if (string.IsNullOrEmpty(outDir) || string.IsNullOrEmpty(stamp))
                    throw new ArgumentException("BuildRelease needs -clonez-release-out <dir> and -clonez-release-stamp <stamp>");
                PlayerSettings.bundleVersion = PlayerSettings.bundleVersion.Split('+')[0] + "+" + stamp;
                PlayerSettings.dedicatedServerOptimizations = false;
                // Mono: an IL2CPP Linux build from Windows would need the cross-compiling toolchain.
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Server, ScriptingImplementation.Mono2x);
                // The server compiles with the game's defines (APP_UI_EDITOR_ONLY, for one, keeps App UI's runtime, which
                // opens a desktop's GTK libraries, out of builds).
                PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Server,
                    PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone));
                Debug.Log("[CloneZBatch] Runtime shader materials made: " + RuntimeShaders.Ensure());
                string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
                if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };
                bool ok = true;
                if (only != "server")
                    ok = BuildFor(scenes, Path.Combine(outDir, "client", "WithTheDead.exe"), BuildTarget.StandaloneWindows64, StandaloneBuildSubtarget.Player);
                if (ok && only != "client")
                    ok = BuildFor(scenes, Path.Combine(outDir, "server", "WithTheDead.x86_64"), BuildTarget.StandaloneLinux64, StandaloneBuildSubtarget.Server);
                Debug.Log("[CloneZBatch] Release " + PlayerSettings.bundleVersion + ": " + (ok ? "built" : "FAILED"));
                // The version the builds report (Application.version), for the download page's latest.json.
                if (ok) File.WriteAllText(Path.Combine(outDir, "version.txt"), PlayerSettings.bundleVersion);
                code = ok ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[CloneZBatch] Release failed: " + e);
            }
            EditorApplication.Exit(code);
        }

        private static bool BuildFor(string[] scenes, string path, BuildTarget target, StandaloneBuildSubtarget subtarget)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                subtarget = (int)subtarget,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[CloneZBatch] " + target + " (" + subtarget + "): " + report.summary.result + ", " + report.summary.totalErrors + " errors, "
                + (report.summary.totalSize / (1024 * 1024)) + " MB, " + report.summary.totalTime.TotalMinutes.ToString("0.0") + " min -> " + path);
            return report.summary.result == BuildResult.Succeeded;
        }

        /// <summary>
        /// Opens the game scene and enters play mode in a batch editor. Pair it with <c>-clonez-smoke solo|hunter|host|client|lobby|wave</c>:
        /// the runtime <see cref="CloneZ.SmokeTest"/> plays the flows and exits the editor with its result.
        /// </summary>
        public static void SmokePlay()
        {
            string scene = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).FirstOrDefault() ?? "Assets/Scenes/SampleScene.unity";
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene);
            SmokeGuard.Arm();
            Debug.Log("[CloneZBatch] Entering play mode in " + scene);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>
        /// The map harness: builds the Kentucky map's start square in an empty edit-mode scene, photographs it as the game
        /// shows it, times the build and streamed chunks, and writes &lt;outDir&gt;\*.png, report.json and report.md
        /// (see <see cref="global::CloneZ.MapShots"/>). Run it with no <c>-quit</c> (it exits by itself) and with
        /// <c>CLONEZ_WORLD</c> pointing at a world store:
        /// <c>Unity.exe -batchmode -projectPath &lt;project&gt; -executeMethod CloneZ.EditorTools.CloneZBatch.MapShots
        /// -clonez-mapshots &lt;outDir&gt; [-clonez-mapshots-place &lt;name&gt;] [-clonez-mapshots-quick] [-clonez-mapshots-lite]
        /// [-clonez-mapshots-painted] -logFile &lt;log&gt;</c> (lite: the streamed chunks are built in Lite detail; painted: the
        /// windows that don't work left painted on, for "before" counts).
        /// Exit code: 0 ok, 1 harness failure, 2 the map failed to build or logged errors while building, 3 no world store
        /// or no such place.
        /// </summary>
        public static void MapShots()
        {
            string[] args = Environment.GetCommandLineArgs();
            string outDir = ArgValue(args, "-clonez-mapshots");
            if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "MapShots");
            int code = global::CloneZ.MapShots.ExitHarness;
            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            try
            {
                // Edit mode in a fresh, empty scene: play mode would boot the whole game (GameBootstrapper).
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // A picture must never catch a shader still compiling (it would draw as the cyan placeholder).
                ShaderUtil.allowAsyncCompilation = false;
                var options = new global::CloneZ.MapShots.Options
                {
                    OutDir = outDir,
                    Place = ArgValue(args, "-clonez-mapshots-place"),
                    Quick = Array.IndexOf(args, "-clonez-mapshots-quick") >= 0,
                    Lite = Array.IndexOf(args, "-clonez-mapshots-lite") >= 0,
                    PaintedWindows = Array.IndexOf(args, "-clonez-mapshots-painted") >= 0,
                    GameProfile = GameVolumeProfile(),
                };
                code = global::CloneZ.MapShots.Run(options);
            }
            catch (Exception e)
            {
                Debug.LogError("[MapShots] the harness failed to start: " + e);
                global::CloneZ.MapShots.WriteFailure(outDir, e.GetType().Name + ": " + e.Message);
                code = global::CloneZ.MapShots.ExitHarness;
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

    /// <summary>
    /// Keeps a batch smoke run's play mode going. An error logged by the editor itself (its search indexer throws on
    /// startup in this Unity) pauses play mode when the Console's Error Pause is on, which freezes the game while the
    /// editor idles, so the run never ends. While the command line has <c>-clonez-smoke</c>, play mode is unpaused
    /// whenever anything pauses it.
    /// </summary>
    [InitializeOnLoad]
    internal static class SmokeGuard
    {
        private static bool s_Armed;

        static SmokeGuard()
        {
            if (Application.isBatchMode && Array.IndexOf(Environment.GetCommandLineArgs(), "-clonez-smoke") >= 0) Arm();
        }

        internal static void Arm()
        {
            if (s_Armed) return;
            s_Armed = true;
            EditorApplication.pauseStateChanged += state =>
            {
                if (state != PauseState.Paused) return;
                Debug.Log("[CloneZBatch] play mode was paused (Error Pause?); carrying on");
                EditorApplication.isPaused = false;
            };
            EditorApplication.update += () => { if (EditorApplication.isPlaying && EditorApplication.isPaused) EditorApplication.isPaused = false; };
        }
    }
}