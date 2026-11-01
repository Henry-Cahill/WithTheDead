using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The headless dedicated server: the Linux Dedicated Server build that Tools/Server/Release.ps1 makes and runs in
    /// Docker on hts-llm. It plays nobody. It serves the session (<see cref="NetSession.Serve"/>) and waits with no world
    /// built. The first runner to deploy opens their Zone on it for everyone, as a host's raid is, and the server builds
    /// that raid's world with no survivor of its own (the dead and the Zone's people go after the runners). It closes the
    /// raid once everyone is out. A Dedicated Server build is always one; any other build, or the editor, is one with
    /// <c>-clonez-server</c> on its command line. <c>-clonez-port &lt;port&gt;</c> (or CLONEZ_PORT) picks the UDP port, and
    /// <c>-clonez-status &lt;seconds&gt;</c> (or CLONEZ_STATUS) how often the log gets a status line (every 5 minutes; 0
    /// for never).
    /// </summary>
    public static class DedicatedServer
    {
        /// <summary>The port it serves on unless told otherwise, and the one the title screen joins.</summary>
        public const ushort DefaultPort = 7777;

        private const float DefaultStatusEvery = 300f;

        /// <summary>True when this process is the dedicated server.</summary>
        public static readonly bool Requested = AskedFor();

        private static bool s_Started;
        // The status line's schedule, and the frames since the last one.
        private static float s_StatusEvery, s_StatusAt, s_FrameTime, s_SlowestFrame;
        private static int s_Frames;

        private static bool AskedFor()
        {
#if UNITY_SERVER
            return true;
#else
            return Array.IndexOf(Environment.GetCommandLineArgs(), "-clonez-server") >= 0;
#endif
        }

        /// <summary>The UDP port to serve on.</summary>
        public static ushort Port => ushort.TryParse(Setting("-clonez-port", "CLONEZ_PORT"), out ushort port) && port != 0 ? port : DefaultPort;

        // Seconds between the log's status lines; 0 turns them off.
        private static float StatusEvery => float.TryParse(Setting("-clonez-status", "CLONEZ_STATUS"), NumberStyles.Float,
            CultureInfo.InvariantCulture, out float every) && every >= 0f ? every : DefaultStatusEvery;

        // A setting from the command line ("-name value"), else from the environment.
        private static string Setting(string arg, string variable)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, arg);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : Environment.GetEnvironmentVariable(variable);
        }

        // An editor that skips the domain reload keeps statics from one play session to the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Started = false;
            s_StatusEvery = s_StatusAt = s_FrameTime = s_SlowestFrame = 0f;
            s_Frames = 0;
        }

        /// <summary>Starts serving the first time the bootstrapper comes up; later scene loads find it serving. Returns
        /// false, and quits, when the port can't be served.</summary>
        public static bool EnsureServing()
        {
            if (s_Started) return NetSession.Active;
            s_Started = true;
            // Nothing to draw: a frame per network tick is plenty, and the machine has other work to do.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            Application.runInBackground = true;
            // The log is the server's console (docker logs): a line per message, with stack traces for errors only.
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            GameEvents.Toast -= LogToast;
            GameEvents.Toast += LogToast;
            ushort port = Port;
            Debug.Log("[Server] WithTheDead " + BuildInfo.Version + " (protocol " + NetSession.Version + ") on UDP port " + port);
            s_StatusEvery = StatusEvery;
            s_StatusAt = Time.unscaledTime + s_StatusEvery;
            NetSession.Serve(port);
            if (NetSession.Active) return true;
            Debug.LogError("[Server] Could not serve on UDP port " + port + "; quitting.");
            Application.Quit(1);
            return false;
        }

        /// <summary>A runner as the server's log names them: by name and connection number ("Runner #3"), as runners
        /// often share a name.</summary>
        public static string Who(ulong client) => (NetSession.Clients.TryGetValue(client, out NetClientInfo c) ? c.Name : "A runner") + " #" + client;

        /// <summary>A span of time as the log puts it: "45 s", "12 min", "3 h 05 min".</summary>
        public static string Duration(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            if (s < 60) return s + " s";
            int m = s / 60;
            return m < 60 ? m + " min" : m / 60 + " h " + (m % 60).ToString("00") + " min";
        }

        /// <summary>Every frame while serving (<see cref="NetSession"/>): times the frames and, every
        /// <c>-clonez-status</c> seconds, logs a status line, so a quiet log still shows the server is alive and how it's
        /// doing.</summary>
        internal static void Tick()
        {
            if (s_StatusEvery <= 0f) return;
            float dt = Time.unscaledDeltaTime;
            s_Frames++;
            s_FrameTime += dt;
            s_SlowestFrame = Mathf.Max(s_SlowestFrame, dt);
            if (Time.unscaledTime < s_StatusAt) return;
            s_StatusAt = Time.unscaledTime + s_StatusEvery;
            Debug.Log("[Server] Status: " + Status());
            s_Frames = 0;
            s_FrameTime = s_SlowestFrame = 0f;
        }

        // How long it's been up; who's on, and where; the raid; and since the last line, the frame rate and the slowest
        // frame, and the memory it holds.
        private static string Status()
        {
            var sb = new StringBuilder(256);
            sb.Append("up ").Append(Duration(Time.realtimeSinceStartup)).Append("; ");
            if (NetSession.Clients.Count == 0) sb.Append("nobody connected");
            else
            {
                sb.Append(NetSession.Clients.Count).Append(" connected (");
                bool first = true;
                foreach (NetClientInfo c in NetSession.Clients.Values)
                {
                    sb.Append(first ? "" : ", ").Append(Who(c.Id)).Append(c.InRaid ? " in the raid" : " at the bar");
                    first = false;
                }
                sb.Append(')');
            }
            sb.Append("; ");
            if (!GameSession.IsExtraction) sb.Append("no raid open");
            else
            {
                float since = NetSession.ServedRaidSince;
                sb.Append(ZoneCatalog.Current.Name).Append(since < 0f ? " being built" : " open for " + Duration(Time.unscaledTime - since));
            }
            float fps = s_FrameTime > 0f ? s_Frames / s_FrameTime : 0f;
            sb.Append("; ").Append(fps.ToString("0.0", CultureInfo.InvariantCulture)).Append(" fps, slowest frame ")
                .Append(Mathf.RoundToInt(s_SlowestFrame * 1000f)).Append(" ms; memory ").Append(Size(ProcessMemory()))
                .Append(" (managed ").Append(Size(GC.GetTotalMemory(false))).Append(')');
            return sb.ToString();
        }

        private static string Size(long bytes) => bytes >= 1L << 30
            ? (bytes / (double)(1L << 30)).ToString("0.00", CultureInfo.InvariantCulture) + " GB"
            : (bytes >> 20) + " MB";

        // The memory the process holds: what the container's memory cap counts.
        private static long ProcessMemory()
        {
            try
            {
                using (var self = System.Diagnostics.Process.GetCurrentProcess()) return self.WorkingSet64;
            }
            catch (Exception) { return UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong(); }
        }

        // What a runner's HUD would show (who joined, who left, who opened a raid) goes in the server's log.
        private static void LogToast(string message) => Debug.Log("[Server] " + message);
    }
}
