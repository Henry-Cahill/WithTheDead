using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// CloneZ ▸ Smoke Test: plays a <see cref="SmokeTest"/> run in this editor's Game view, to watch it (a batch run has no
    /// window). "Watch" leaves a moment between presses to follow them by; "Run" goes at full speed. The run drives the
    /// game with a keyboard and a mouse of its own (the real ones are off until it ends), its [Smoke] lines go to the
    /// Console, and play mode stops when it's done; stopping play mode ends it early. Either way the editor then has its
    /// own profile, save, settings and input back.
    /// </summary>
    internal static class SmokeTestMenu
    {
        private const string WatchItem = "CloneZ/Smoke Test/Watch Wave Survival";
        private const string RunItem = "CloneZ/Smoke Test/Run Wave Survival (full speed)";
        private const float WatchPace = 0.3f;

        [MenuItem(WatchItem)]
        private static void WatchWave() => Play("wave", WatchPace);

        [MenuItem(RunItem)]
        private static void RunWave() => Play("wave", 0f);

        [MenuItem(WatchItem, true)]
        [MenuItem(RunItem, true)]
        private static bool CanPlay() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void Play(string mode, float pace)
        {
            // Play mode wouldn't start, and the request would wait for the next press of Play instead.
            if (EditorUtility.scriptCompilationFailed)
            {
                EditorUtility.DisplayDialog("Smoke test", "Fix the compile errors first: play mode can't start with them.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string scene = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).FirstOrDefault() ?? "Assets/Scenes/SampleScene.unity";
            if (EditorSceneManager.GetActiveScene().path != scene) EditorSceneManager.OpenScene(scene);
            SessionState.SetString(SmokeTest.MenuModeKey, mode);
            SessionState.SetFloat(SmokeTest.MenuPaceKey, pace);
            EditorApplication.EnterPlaymode();
        }
    }
}
