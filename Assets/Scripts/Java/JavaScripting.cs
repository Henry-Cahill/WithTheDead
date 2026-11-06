using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CloneZ.Java
{
    /// <summary>Where a Java session's parts are, or why there are none (see <see cref="JavaScripting"/>).</summary>
    public sealed class JavaLayout
    {
        /// <summary>The JVM library: bin/server/jvm.dll (lib/server/libjvm.so on Linux) in a Java runtime.</summary>
        public string JvmLibrary;
        /// <summary>The JVM's class path: withthedead.boot, as a jar or a directory of classes.</summary>
        public string BootPath;
        /// <summary>The session's modules (withthedead.api, withthedead.host and the scripts), separated by
        /// <see cref="Path.PathSeparator"/>; each a module or a directory of them.</summary>
        public string ModulePath;
        /// <summary>The file the JVM writes its own warnings to.</summary>
        public string JvmLog;
        /// <summary>Why there's no Java here; null when there is.</summary>
        public string Missing;
    }

    /// <summary>
    /// Runs the game's Java 27 scripts (Java/README.md) inside the game. When the game starts (in the editor, with each
    /// Play) it opens the process's JVM through <see cref="Jni"/>, starts a <see cref="JavaSession"/> over the modules a
    /// build carries in WithTheDead_Data/Java (in the editor, those Assets/Editor/JavaBuild.cs compiled into
    /// Library/Java) and forwards <see cref="GameEvents"/> to the scripts; their toasts and subtitles come back through
    /// <see cref="GameEvents"/>, their log lines as "[Java] ..." in the game's log. A build without its Java runtime, or a
    /// game started with <c>-clonez-no-java</c>, plays on without scripts. <c>-clonez-java-debug &lt;port&gt;</c> (or
    /// CLONEZ_JAVA_DEBUG=&lt;port&gt;) opens the JVM with a debugger agent listening on 127.0.0.1:&lt;port&gt;; a JVM is made
    /// once per process, so in the editor that takes the first Play after the editor starts. <c>-clonez-java-check</c>
    /// checks at startup that Mono and the JVM share the process's signals (see <see cref="CheckSignals"/>).
    /// </summary>
    public sealed class JavaScripting : MonoBehaviour, IJavaGame
    {
        public enum State { Off, Unavailable, Failed, Running }

        /// <summary>Whether this session's scripts run; <see cref="Detail"/> says which, or why not.</summary>
        public static State Status { get; private set; }

        public static string Detail { get; private set; } = "";

        /// <summary>Game events the scripts were sent this session.</summary>
        public static int EventsSent { get; private set; }

        /// <summary>Errors this session: the scripts' (and the host's) logged errors and calls into Java that failed.</summary>
        public static int Errors { get; private set; }

        /// <summary>Where the editor's Java is; set by Assets/Editor/JavaBuild.cs.</summary>
        public static Func<JavaLayout> EditorLayout;

        private static Jni s_Jni;
        private static JavaSession s_Session;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            // An editor that skips the domain reload keeps statics from one play session to the next: start clean.
            Status = State.Off;
            Detail = "";
            EventsSent = 0;
            Errors = 0;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clonez-no-java") >= 0)
            {
                Detail = "turned off by -clonez-no-java";
                return;
            }
            var host = new GameObject("[Java]");
            DontDestroyOnLoad(host);
            host.AddComponent<JavaScripting>();
        }

        private void OnEnable()
        {
            Begin();
            if (s_Session == null) return;
            GameEvents.LocalPlayerSpawned += OnPlayerSpawned;
            GameEvents.RaidStarted += OnRaidStarted;
            GameEvents.RaidEnded += OnRaidEnded;
            GameEvents.ActorKilled += OnActorKilled;
            GameEvents.ItemLooted += OnItemLooted;
            GameEvents.ItemCrafted += OnItemCrafted;
            GameEvents.LocationVisited += OnLocationVisited;
            GameEvents.EmissionStateChanged += OnEmissionStateChanged;
            GameEvents.ArtifactPropertyLearned += OnArtifactPropertyLearned;
            GameEvents.DayAdvanced += OnDayAdvanced;
        }

        // Also runs before the editor reloads scripts in play mode: the session's native methods belong to this domain.
        private void OnDisable()
        {
            GameEvents.LocalPlayerSpawned -= OnPlayerSpawned;
            GameEvents.RaidStarted -= OnRaidStarted;
            GameEvents.RaidEnded -= OnRaidEnded;
            GameEvents.ActorKilled -= OnActorKilled;
            GameEvents.ItemLooted -= OnItemLooted;
            GameEvents.ItemCrafted -= OnItemCrafted;
            GameEvents.LocationVisited -= OnLocationVisited;
            GameEvents.EmissionStateChanged -= OnEmissionStateChanged;
            GameEvents.ArtifactPropertyLearned -= OnArtifactPropertyLearned;
            GameEvents.DayAdvanced -= OnDayAdvanced;
            End();
        }

        private void Update() => Send(session => session.Pump(), false);

        private void Begin()
        {
            if (s_Session != null) return;
            JavaLayout layout = Locate();
            if (layout.Missing != null)
            {
                Status = State.Unavailable;
                Detail = layout.Missing;
                Debug.LogWarning("[Java] No Java scripts: " + layout.Missing);
                return;
            }
            try
            {
                var watch = Stopwatch.StartNew();
                bool opened = s_Jni == null;
                Jni jni = Jvm(layout);
                s_Session = JavaSession.Start(jni, layout.ModulePath, Application.version, this);
                Status = State.Running;
                Detail = s_Session.Scripts.Length + (s_Session.Scripts.Length == 1 ? " script: " : " scripts: ")
                    + string.Join(", ", s_Session.Scripts);
                Debug.Log("[Java] Running " + Detail + " (" + watch.ElapsedMilliseconds + " ms"
                    + (opened && jni.Created ? ", JVM started" : "") + ")");
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-clonez-java-check") >= 0) CheckSignals();
            }
            // Whatever goes wrong, the game plays on without its scripts.
            catch (Exception e)
            {
                Status = State.Failed;
                Detail = e.Message;
                Errors++;
                Debug.LogError("[Java] The Java scripts couldn't start: " + e);
            }
        }

        private static void End()
        {
            JavaSession session = s_Session;
            if (session == null) return;
            s_Session = null;
            try
            {
                session.Dispose();
            }
            catch (JavaException e)
            {
                Errors++;
                Debug.LogError("[Java] Stopping the Java scripts failed: " + e.Message);
            }
            if (Status == State.Running) Status = State.Off;
        }

        /// <summary>The process's JVM, opened with the game's options (see <see cref="JavaScripting"/>) the first time this
        /// domain asks for it, or found again if an earlier domain opened it.</summary>
        public static Jni Jvm(JavaLayout layout)
        {
            if (s_Jni == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(layout.JvmLog));
                s_Jni = Jni.Open(layout.JvmLibrary, Options(layout));
            }
            return s_Jni;
        }

        private static string s_Nothing = null;

        // -clonez-java-check, for a new platform or runtime: Mono and the JVM share the process's signal handlers (on Linux,
        // Mono's NullReferenceExceptions arrive as SIGSEGVs the JVM's handler sees first, and its collector stops threads
        // with signals), so this makes Mono catch one and collect garbage with the JVM running, then calls Java again. A
        // process whose handlers don't cooperate dies here, at startup, rather than at the first null in a raid.
        private static void CheckSignals()
        {
            bool caught;
            try
            {
                caught = s_Nothing.Length == int.MinValue;   // s_Nothing is always null: a real null dereference
            }
            catch (NullReferenceException)
            {
                caught = true;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Send(session => session.Pump(), false);
            Debug.Log("[Java] Check: Mono " + (caught ? "caught" : "did NOT catch") + " a NullReferenceException, collected garbage"
                + " and called Java again" + (Status == State.Running ? ", with the JVM running." : ", but the scripts stopped: " + Detail));
        }

        private static JavaLayout Locate()
        {
#if UNITY_EDITOR
            return EditorLayout != null
                ? EditorLayout()
                : new JavaLayout { Missing = "the editor's Java build (Assets/Editor/JavaBuild.cs) isn't loaded" };
#else
            string java = Path.Combine(Application.dataPath, "Java");
            var layout = new JavaLayout
            {
                JvmLibrary = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Path.Combine(java, "runtime", "bin", "server", "jvm.dll")
                    : Path.Combine(java, "runtime", "lib", "server", "libjvm.so"),
                BootPath = Path.Combine(java, "boot.jar"),
                ModulePath = Path.Combine(java, "lib") + Path.PathSeparator + Path.Combine(java, "scripts"),
                JvmLog = Path.Combine(Application.persistentDataPath, "Java.log"),
            };
            if (!File.Exists(layout.JvmLibrary)) layout.Missing = "this build has no Java runtime (" + layout.JvmLibrary + ")";
            return layout;
#endif
        }

        private static List<string> Options(JavaLayout layout)
        {
            // On Windows the JVM reads its options in the system's code page: a path that it can't write goes in its short
            // form, or (the JVM's own log) not at all.
            string boot = Jni.OptionPath(layout.BootPath)
                ?? throw new JavaException("The Java scripts' path (" + layout.BootPath + ") has characters this system's code page lacks.");
            string logDir = Jni.OptionPath(Path.GetDirectoryName(layout.JvmLog));
            var options = new List<string>
            {
                // withthedead.boot goes on the class path, not the module path: if it's missing, FindClass fails and the
                // game plays on, where a boot module that can't be found ends the whole process.
                "-Djava.class.path=" + boot,
                // The game owns the process's signals and console events.
                "-Xrs",
                // A small heap with the serial collector: no collector threads competing with the game's for its cores,
                // and short pauses on a heap this size.
                "-XX:+UseSerialGC", "-Xms16m", "-Xmx256m",
                // No hsperfdata file in the temp folder.
                "-XX:-UsePerfData",
                // The JVM's own warnings go to a file: a game has no console to print them on.
                "-Xlog:disable",
            };
            if (logDir != null) options.Add("-Xlog:all=warning:file=\"" + Path.Combine(logDir, Path.GetFileName(layout.JvmLog)) + "\"");
            int port = DebugPort(layout.JvmLibrary);
            if (port > 0) options.Add("-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address=127.0.0.1:" + port);
            return options;
        }

        // A JVM whose debugger agent can't load or listen ends the process, so the agent is only asked for when the runtime
        // has it (builds' runtimes don't) and the port is free.
        private static int DebugPort(string jvmLibrary)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-clonez-java-debug");
            string value = i >= 0 && i + 1 < args.Length ? args[i + 1] : Environment.GetEnvironmentVariable("CLONEZ_JAVA_DEBUG");
            if (!int.TryParse(value, out int port) || port <= 0 || port > 65535) return 0;
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string agent = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(jvmLibrary)), windows ? "jdwp.dll" : "libjdwp.so");
            if (!File.Exists(agent))
            {
                Debug.LogWarning("[Java] This Java runtime has no debugger agent (" + agent + "); no debugging.");
                return 0;
            }
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException)
            {
                Debug.LogWarning("[Java] Port " + port + " is taken; no debugging.");
                return 0;
            }
            Debug.Log("[Java] A Java debugger can attach to 127.0.0.1:" + port);
            return port;
        }

        private static void Send(Action<JavaSession> send, bool isEvent = true)
        {
            JavaSession session = s_Session;
            if (session == null) return;
            try
            {
                send(session);
                if (isEvent) EventsSent++;
            }
            catch (JavaException e)
            {
                Errors++;
                Debug.LogError("[Java] " + e.Message);
            }
        }

        private static void OnPlayerSpawned(PlayerStats _) => Send(s => s.PlayerSpawned());

        private static void OnRaidStarted(string zone, RaidKind kind) => Send(s => s.RaidStarted(zone, kind.ToString()));

        private static void OnRaidEnded(RaidOutcome outcome, string exit) => Send(s => s.RaidEnded(outcome.ToString(), exit));

        private static void OnActorKilled(KillInfo kill)
        {
            Actor victim = kill.Victim, killer = kill.Killer;
            if (victim == null) return;
            bool nobody = killer == null;
            Send(s => s.ActorKilled(victim.DisplayName, victim.Kind.ToString(), victim.Faction,
                nobody ? "" : killer.DisplayName, nobody ? "" : killer.Kind.ToString(), nobody ? "" : killer.Faction,
                kill.WeaponId, kill.Surrendered));
        }

        private static void OnItemLooted(ItemStack stack, string source)
        {
            ItemDef def = stack?.Def;
            if (def == null) return;
            Send(s => s.ItemLooted(def.Id, def.DisplayName, stack.Count, source));
        }

        private static void OnItemCrafted(string item, int count) => Send(s => s.ItemCrafted(item, count));

        private static void OnLocationVisited(string location) => Send(s => s.LocationVisited(location));

        private static void OnEmissionStateChanged(bool active) => Send(s => s.EmissionChanged(active));

        private static void OnArtifactPropertyLearned(string artifact, string property) => Send(s => s.ArtifactLearned(artifact, property));

        private static void OnDayAdvanced(int day) => Send(s => s.DayAdvanced(day));

        void IJavaGame.Log(int level, string message)
        {
            switch (level)
            {
                case 0:
                    Debug.Log("[Java] " + message);
                    break;
                case 1:
                    Debug.LogWarning("[Java] " + message);
                    break;
                default:
                    Errors++;
                    Debug.LogError("[Java] " + message);
                    break;
            }
        }

        void IJavaGame.Toast(string message) => GameEvents.RaiseToast(message);

        void IJavaGame.Subtitle(string speaker, string line) => GameEvents.RaiseSubtitle(speaker, line);
    }
}
