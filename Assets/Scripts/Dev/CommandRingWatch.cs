using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Watches the render thread's command ring, in the editor and development builds with multithreaded rendering. The
    /// main thread writes each frame's rendering commands into a ring (16 MB unless <c>-gfx-ring-buffer-size</c> says
    /// otherwise) that the render thread reads. Whenever it's full the main thread waits (profiler marker
    /// <c>Gfx.WaitForGfxCommandWriteSpace</c>), and Unity logs "Ran out of Graphics Ring Buffer space" on every 301st wait,
    /// counting across the editor session, never in a batch editor. This names the frames that waited and what they drew
    /// and uploaded, at most once a second, and counts the play session's waits for the smoke test (SmokeTest.Ring.cs).
    /// </summary>
    public sealed class CommandRingWatch : MonoBehaviour
    {
        private static readonly string[] CounterNames =
        {
            "SetPass Calls Count", "Triangles Count", "Shadow Casters Count", "Vertex Buffer Upload In Frame Bytes",
            "Index Buffer Upload In Frame Bytes", "Render Textures Changes Count",
        };
        private static readonly string[] CounterLabels =
            { "set-pass calls", "triangles", "shadow casters", "vertex bytes uploaded", "index bytes uploaded", "render target changes" };

        /// <summary>True while the ring is watched.</summary>
        public static bool Watching { get; private set; }
        /// <summary>The play session's waits for room in the ring.</summary>
        public static long Waits { get; private set; }
        /// <summary>The play session's time spent waiting for room in the ring, in milliseconds.</summary>
        public static double WaitMs { get; private set; }
        /// <summary>The frame that waited longest and what it drew, or empty.</summary>
        public static string WorstFrame { get; private set; } = "";

        private ProfilerRecorder _wait;
        private readonly ProfilerRecorder[] _counters = new ProfilerRecorder[CounterNames.Length];
        private double _worstMs;
        private long _unreportedWaits;
        private double _unreportedMs;
        private int _unreportedFrames;
        private string _lastFrame;
        private float _nextReport;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            // Statics outlive a play session in an editor that skips the domain reload.
            Watching = false;
            Waits = 0;
            WaitMs = 0;
            WorstFrame = "";
            if (!Debug.isDebugBuild || SystemInfo.renderingThreadingMode != RenderingThreadingMode.MultiThreaded) return;
            var go = new GameObject("[CommandRingWatch]");
            DontDestroyOnLoad(go);
            go.AddComponent<CommandRingWatch>();
        }

        private void OnEnable()
        {
            _wait = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Gfx.WaitForGfxCommandWriteSpace", 1);
            for (int i = 0; i < CounterNames.Length; i++) _counters[i] = ProfilerRecorder.StartNew(ProfilerCategory.Render, CounterNames[i], 1);
            Watching = _wait.Valid;
        }

        private void OnDisable()
        {
            Report();
            Watching = false;
            _wait.Dispose();
            for (int i = 0; i < _counters.Length; i++) _counters[i].Dispose();
        }

        // The recorders hold the last frame that finished: the one before this.
        private void Update()
        {
            if (_wait.Count > 0)
            {
                ProfilerRecorderSample s = _wait.GetSample(0);
                if (s.Count > 0)
                {
                    double ms = s.Value / 1e6;
                    Waits += s.Count;
                    WaitMs += ms;
                    _unreportedWaits += s.Count;
                    _unreportedMs += ms;
                    _unreportedFrames++;
                    _lastFrame = Describe(Time.frameCount - 1, s.Count, ms);
                    if (ms >= _worstMs)
                    {
                        _worstMs = ms;
                        WorstFrame = _lastFrame;
                    }
                }
            }
            if (_unreportedWaits > 0 && Time.unscaledTime >= _nextReport) Report();
        }

        private void Report()
        {
            if (_unreportedWaits == 0) return;
            Debug.LogWarning("[CloneZ] Render command ring full: the main thread waited " + _unreportedWaits + " times ("
                + _unreportedMs.ToString("0.0") + " ms) for room in the render thread's commands, in " + _unreportedFrames
                + (_unreportedFrames == 1 ? " frame" : " frames") + "; the last was " + _lastFrame + ".");
            _unreportedWaits = 0;
            _unreportedMs = 0;
            _unreportedFrames = 0;
            _nextReport = Time.unscaledTime + 1f;
        }

        private string Describe(int frame, long waits, double ms)
        {
            var sb = new StringBuilder();
            sb.Append("frame ").Append(frame).Append(" (").Append(Time.realtimeSinceStartup.ToString("0.0")).Append(" s): ")
                .Append(waits).Append(waits == 1 ? " wait, " : " waits, ").Append(ms.ToString("0.0")).Append(" ms");
            for (int i = 0; i < _counters.Length; i++)
                if (_counters[i].Valid) sb.Append(", ").Append(_counters[i].LastValue).Append(' ').Append(CounterLabels[i]);
            return sb.ToString();
        }
    }
}
