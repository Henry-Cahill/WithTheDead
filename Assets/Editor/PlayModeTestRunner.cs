// ============================================================
// PLAY MODE TEST BOOTSTRAP — MULTI-FRAME VARIANT
// Task: start game -> main menu -> hub -> walk to Zone door ->
// "Head into the Zone" -> Deploy screen -> "Go" -> raid.
// ============================================================
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";
        private const string SentinelLog = "PLAY_MODE_TEST_COMPLETE";

        private static readonly int WaitFrames = SessionState.GetInt("PlayModeTest.WaitFrames", 3);
        private static readonly float TestTimeout = SessionState.GetFloat("PlayModeTest.TestTimeout", 10.0f);

        private static List<string> _capturedLogs = new List<string>();
        private const int MaxCapturedLogs = 50;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            switch (state)
            {
                case "Idle":
                    break;
                case "WaitingForCompile":
                    Debug.Log("[PlayModeTest] Bootstrap compiled. Scheduling Play Mode entry.");
                    EditorApplication.delayCall += () =>
                    {
                        SessionState.SetString(StateKey, "EnteringPlayMode");
                        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                        EditorApplication.isPlaying = true;
                    };
                    break;
                case "EnteringPlayMode":
                    EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                        SessionState.SetString(StateKey, "InPlayMode");
                        EditorApplication.update += WaitFramesThenRun;
                    }
                    break;
                case "InPlayMode":
                    if (EditorApplication.isPlaying)
                        EditorApplication.update += WaitFramesThenRun;
                    break;
                case "Done":
                    Debug.Log(SentinelLog);
                    if (string.IsNullOrEmpty(SessionState.GetString("PlayModeTest.ScreenshotPath", "")))
                        EditorApplication.delayCall += SelfDestruct;
                    break;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += WaitFramesThenRun;
            }
        }

        private static int _frameCount = 0;
        private static bool _setupDone = false;
        private static bool _testDone = false;
        private static double _testStartTime = 0;

        private static void WaitFramesThenRun()
        {
            _frameCount++;
            if (_frameCount < WaitFrames) return;
            if (_testDone) return;

            if (!_setupDone)
            {
                _setupDone = true;
                Application.logMessageReceived += OnLogMessage;
                _testStartTime = EditorApplication.timeSinceStartup;
                try { Setup(); }
                catch (System.Exception e)
                {
                    Debug.LogError("[PlayModeTest] Setup threw exception: " + e);
                    FinishTest(true, e.Message);
                    return;
                }
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - _testStartTime);
            bool timedOut = elapsed >= TestTimeout;
            try
            {
                bool complete = Tick(elapsed);
                if (complete || timedOut)
                {
                    if (timedOut && !complete)
                        Debug.LogWarning("[PlayModeTest] Test timed out after " + elapsed + "s");
                    FinishTest(timedOut && !complete, timedOut ? "Test timed out after " + TestTimeout + "s" : null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PlayModeTest] Tick threw exception: " + e);
                FinishTest(true, e.Message);
            }
        }

        private static void FinishTest(bool isError, string errorMessage)
        {
            _testDone = true;
            EditorApplication.update -= WaitFramesThenRun;
            Application.logMessageReceived -= OnLogMessage;
            string resultJson;
            try { resultJson = GetResult(); }
            catch (System.Exception e)
            {
                resultJson = JsonUtility.ToJson(new TestResult { success = false, error = "GetResult() threw: " + e.Message, logs = _capturedLogs.ToArray() });
            }
            if (isError && errorMessage != null)
                resultJson = JsonUtility.ToJson(new TestResult { success = false, error = errorMessage, logs = _capturedLogs.ToArray() });
            SessionState.SetString(ResultKey, resultJson);
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (_capturedLogs.Count >= MaxCapturedLogs) return;
            if (type == LogType.Error || type == LogType.Exception || message.Contains("[Test]") || message.Contains("TEST_RESULT"))
                _capturedLogs.Add("[" + type + "] " + message);
        }

        private static void SelfDestruct()
        {
            string scriptPath = SessionState.GetString(ScriptPathKey, "");
            if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.AssetPathExists(scriptPath))
                AssetDatabase.DeleteAsset(scriptPath);
            SessionState.EraseString(StateKey);
            SessionState.EraseString(ScriptPathKey);
        }

        [System.Serializable]
        private class TestResult
        {
            public bool success;
            public string error;
            public string[] logs;
            public string status;
            public string walkNote;
            public string playerPos;
            public string screenshotPath;
        }

        // ============ WALK-TO-RAID FLOW ============
        private static int _phase; // 0 menu, 1 hub wait, 2 walk, 3 interact, 4 deploy, 5 raid wait, 6 capture
        private static float _nextAction;
        private static float _phaseSince;
        private static CharacterController _walker;
        private static CloneZ.PlayerController _pc;
        private static Vector3 _lastCheckPos;
        private static float _lastCheckTime;
        private static float _strafeUntil;
        private static float _strafeSign = 1f;
        private static bool _screenshotCaptured;
        private static string _screenshotPath = "";
        private static string _walkNote = "";

        private const float DoorX = 0f, DoorZ = -8.8f;

        private static void Setup()
        {
            _phase = 0;
            _nextAction = 0f;
            Debug.Log("[Test] Walk-to-raid: setup complete, looking for the main menu");
        }

        private static bool Tick(float elapsed)
        {
            switch (_phase)
            {
                case 0: return TickMenu(elapsed);
                case 1: return TickHubWait(elapsed);
                case 2: return TickWalk(elapsed);
                case 3: return TickInteract(elapsed);
                case 4: return TickDeploy(elapsed);
                case 5: return TickRaidWait(elapsed);
                case 6: return TickCapture(elapsed);
            }
            return true;
        }

        private static void Goto(int phase, float elapsed)
        {
            _phase = phase;
            _phaseSince = elapsed;
        }

        private static Button FindButton(Transform root, string prefix)
        {
            if (root == null) return null;
            var buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                var t = buttons[i].GetComponentInChildren<Text>();
                if (t != null && t.text != null && t.text.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return buttons[i];
            }
            return null;
        }

        private static void Click(Button b, string what, float elapsed)
        {
            Debug.Log("[Test] Clicking: " + what);
            _nextAction = elapsed + 0.5f;
            b.onClick.Invoke();
        }

        private static bool TickMenu(float elapsed)
        {
            var menu = GameObject.Find("MainMenu");
            if (menu == null) { Goto(1, elapsed); return false; }
            if (elapsed < _nextAction) return false;
            var canvas = GameObject.Find("MainMenu Canvas");
            Transform root = canvas != null ? canvas.transform : menu.transform;
            var intoBar = FindButton(root, "Into the bar");
            if (intoBar != null)
            {
                var inputs = root.GetComponentsInChildren<InputField>(true);
                for (int i = 0; i < inputs.Length; i++)
                    if (string.IsNullOrEmpty(inputs[i].text)) inputs[i].text = "Runner";
                Click(intoBar, "Into the bar (create runner)", elapsed);
                return false;
            }
            var play = FindButton(root, "Play");
            if (play != null) { Click(play, "Play (existing runner)", elapsed); return false; }
            var newRunner = FindButton(root, "New runner");
            if (newRunner != null) { Click(newRunner, "New runner", elapsed); return false; }
            return false;
        }

        private static bool TickHubWait(float elapsed)
        {
            if (CloneZ.GameSession.Mode != CloneZ.GameMode.Hub) return false;
            var player = GameObject.Find("Player");
            if (player == null) return false;
            if (elapsed - _phaseSince < 1.0f) return false;
            _walker = player.GetComponent<CharacterController>();
            _pc = player.GetComponent<CloneZ.PlayerController>();
            if (_pc != null) _pc.enabled = false;
            _lastCheckPos = player.transform.position;
            _lastCheckTime = elapsed;
            Debug.Log("[Test] In hub, player at " + player.transform.position + " - walking to the Zone door");
            Goto(2, elapsed);
            return false;
        }

        private static bool TickWalk(float elapsed)
        {
            var player = GameObject.Find("Player");
            if (player == null || _walker == null) { Goto(1, elapsed); return false; }
            Vector3 pos = player.transform.position;
            Vector3 flat = new Vector3(DoorX - pos.x, 0f, DoorZ - pos.z);
            float dist = flat.magnitude;
            if (dist <= 1.8f || elapsed - _phaseSince > 20f)
            {
                if (_pc != null) _pc.enabled = true;
                _walkNote = "walked from spawn to " + pos.ToString() + " (flat dist to door " + dist.ToString("F2") + "m)";
                Debug.Log("[Test] At the door: " + _walkNote);
                Goto(3, elapsed);
                return false;
            }
            Vector3 dir = flat.normalized;
            if (elapsed < _strafeUntil)
            {
                dir = new Vector3(-dir.z, 0f, dir.x) * _strafeSign;
            }
            else if (elapsed - _lastCheckTime > 0.8f)
            {
                Vector3 flatPos = new Vector3(pos.x, 0f, pos.z);
                Vector3 flatLast = new Vector3(_lastCheckPos.x, 0f, _lastCheckPos.z);
                if (Vector3.Distance(flatPos, flatLast) < 0.05f)
                {
                    _strafeSign = -_strafeSign;
                    _strafeUntil = elapsed + 0.7f;
                    Debug.Log("[Test] Path blocked, sidestepping");
                }
                _lastCheckPos = pos;
                _lastCheckTime = elapsed;
            }
            _walker.Move((dir * 3.0f + Vector3.down * 2f) * Time.deltaTime);
            return false;
        }

        private static void DeployDirect(float elapsed)
        {
            Debug.LogWarning("[Test] Deploying directly via GameFlow.Deploy (fallback)");
            CloneZ.GameFlow.Deploy(CloneZ.ZoneCatalog.DefaultZone, CloneZ.RaidWorldRules.OnFoot, CloneZ.RaidKind.Operative);
            Goto(5, elapsed);
        }

        private static bool TickInteract(float elapsed)
        {
            if (elapsed < _nextAction) return false;
            var interactor = CloneZ.PlayerInteractor.Current;
            CloneZ.HubStation door = null;
            var stations = Object.FindObjectsByType<CloneZ.HubStation>(FindObjectsSortMode.None);
            for (int i = 0; i < stations.Length; i++)
                if (stations[i] != null && stations[i].Title == "Door to the Zone") { door = stations[i]; break; }
            if (interactor == null || door == null)
            {
                if (elapsed - _phaseSince > 8f) DeployDirect(elapsed);
                return false;
            }
            var actions = new List<CloneZ.InteractAction>();
            door.GetActions(interactor.Context, actions);
            CloneZ.InteractAction go = null;
            for (int i = 0; i < actions.Count; i++)
                if (actions[i] != null && actions[i].Label == "Head into the Zone") { go = actions[i]; break; }
            if (go == null) { DeployDirect(elapsed); return false; }
            Debug.Log("[Test] Interacting with the door: '" + go.Label + "' enabled=" + go.Enabled);
            interactor.Perform(door, go);
            _nextAction = elapsed + 0.5f;
            Goto(4, elapsed);
            return false;
        }

        private static bool TickDeploy(float elapsed)
        {
            if (elapsed < _nextAction) return false;
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                var t = buttons[i].GetComponentInChildren<Text>();
                if (t != null && t.text != null && t.text.StartsWith("Go —", System.StringComparison.Ordinal))
                {
                    Click(buttons[i], "'" + t.text + "' (deploy)", elapsed);
                    Goto(5, elapsed);
                    return false;
                }
            }
            if (elapsed - _phaseSince > 6f) DeployDirect(elapsed);
            return false;
        }

        private static bool TickRaidWait(float elapsed)
        {
            if (CloneZ.GameSession.Mode != CloneZ.GameMode.Extraction) return false;
            var player = GameObject.Find("Player");
            if (player == null) return false;
            if (elapsed - _phaseSince < 4f) return false;
            Debug.Log("[Test] In raid - zone=" + CloneZ.GameSession.ZoneId + " player at " + player.transform.position);
            Goto(6, elapsed);
            return false;
        }

        private static bool TickCapture(float elapsed)
        {
            if (!_screenshotCaptured)
            {
                _screenshotPath = "Assets/RaidWalk_Screenshot.png";
                ScreenCapture.CaptureScreenshot(_screenshotPath);
                SessionState.SetString("PlayModeTest.ScreenshotPath", _screenshotPath);
                _screenshotCaptured = true;
                Debug.Log("[Test] Screenshot captured: " + _screenshotPath);
                return false;
            }
            return true;
        }

        private static string GetResult()
        {
            var player = GameObject.Find("Player");
            var result = new TestResult
            {
                success = true,
                status = "mode=" + CloneZ.GameSession.Mode + " zone=" + CloneZ.GameSession.ZoneId + " raidKind=" + CloneZ.GameSession.Raid,
                walkNote = _walkNote,
                playerPos = player != null ? player.transform.position.ToString() : "null",
                screenshotPath = _screenshotPath,
                logs = _capturedLogs.ToArray()
            };
            return JsonUtility.ToJson(result);
        }
    }
}
