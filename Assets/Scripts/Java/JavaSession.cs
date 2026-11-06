using System;
using System.Collections.Generic;
using AOT;

namespace CloneZ.Java
{
    /// <summary>What a Java session does to the game (withthedead.api.Game, through withthedead.host's native methods).</summary>
    public interface IJavaGame
    {
        /// <summary>A line for the game's log; <paramref name="level"/> 0 is info, 1 a warning, 2 an error.</summary>
        void Log(int level, string message);

        void Toast(string message);

        void Subtitle(string speaker, string line);
    }

    /// <summary>
    /// A session of Java scripts (Java/README.md): withthedead.boot loads the session's modules into a module layer of
    /// their own, withthedead.host's native methods are bound to <see cref="IJavaGame"/>, and the scripts are started.
    /// The typed methods send the game's events (C# enum values go by name), <see cref="Pump"/> carries out what the
    /// scripts queued and should run every frame, and <see cref="Dispose"/> ends the session. One session at a time,
    /// called from the thread the <see cref="Jni"/> belongs to.
    /// </summary>
    public sealed class JavaSession : IDisposable
    {
        private const string S = "Ljava/lang/String;";
        private const string Two = "(" + S + S + ")V";

        private static JavaSession s_Current;
        private static IJavaGame s_Game;
        private static Jni s_Jni, s_BootOf;
        private static IntPtr s_Boot, s_Open, s_LeftOut;

        private readonly Jni _jni;
        private readonly IntPtr _host;
        private readonly Dictionary<string, IntPtr> _methods = new Dictionary<string, IntPtr>();

        /// <summary>The scripts the session started, as "module/Class".</summary>
        public string[] Scripts { get; private set; }

        static JavaSession()
        {
            // Native methods bound to this domain's delegates must not outlive it (the editor reloads it on every
            // script change); a session nobody ended is ended here.
            AppDomain.CurrentDomain.DomainUnload += (_, __) => s_Current?.Dispose();
        }

        private JavaSession(Jni jni, IntPtr host)
        {
            _jni = jni;
            _host = host;
        }

        /// <summary>
        /// Starts a session with the modules on <paramref name="modulePath"/> (entries separated by the platform's path
        /// separator). <paramref name="version"/> is what <c>Game.version()</c> reports. A module withthedead.boot leaves
        /// out (one that can't be read, is there twice or doesn't resolve) is logged as a warning; the rest run.
        /// </summary>
        public static JavaSession Start(Jni jni, string modulePath, string version, IJavaGame game)
        {
            if (s_Current != null) throw new InvalidOperationException("A Java session is already running.");
            if (s_BootOf != jni)
            {
                s_Boot = jni.FindClass("withthedead/boot/Boot");
                s_Open = jni.StaticMethod(s_Boot, "open", "(" + S + S + S + ")Ljava/lang/Class;");
                jni.DescribeExceptionsWith(s_Boot, jni.StaticMethod(s_Boot, "describe", "(Ljava/lang/Throwable;)" + S));
                // A JVM the editor made before withthedead.boot could say this keeps its old Boot until a restart.
                try { s_LeftOut = jni.StaticMethod(s_Boot, "leftOut", "()" + S); }
                catch (JavaException) { s_LeftOut = IntPtr.Zero; }
                s_BootOf = jni;
            }
            IntPtr host = jni.CallStaticObject(s_Boot, s_Open, modulePath, "withthedead.host", "withthedead.host.Host");
            var session = new JavaSession(jni, host);
            try
            {
                if (s_LeftOut != IntPtr.Zero)
                    foreach (string line in (jni.CallStaticString(s_Boot, s_LeftOut) ?? "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        game.Log(1, "Left out of the session: " + line);
                jni.RegisterNatives(host,
                    ("log", "(I" + S + ")V", s_LogFn),
                    ("toast", "(" + S + ")V", s_ToastFn),
                    ("subtitle", Two, s_SubtitleFn));
                s_Jni = jni;
                s_Game = game;
                s_Current = session;
                string names = jni.CallStaticString(host, session.Method("start", "(" + S + ")" + S), version ?? "");
                session.Scripts = names.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                return session;
            }
            catch
            {
                try { session.Dispose(); }
                catch (JavaException) { }
                throw;
            }
        }

        public void PlayerSpawned() => Send("playerSpawned", "()V");

        public void RaidStarted(string zone, string kind) => Send("raidStarted", Two, zone, kind);

        public void RaidEnded(string outcome, string exit) => Send("raidEnded", Two, outcome, exit);

        /// <summary>An empty <paramref name="killerKind"/> means nobody did it.</summary>
        public void ActorKilled(string victimName, string victimKind, string victimFaction,
            string killerName, string killerKind, string killerFaction, string weapon, bool surrendered)
            => Send("actorKilled", "(" + S + S + S + S + S + S + S + "Z)V",
                victimName, victimKind, victimFaction, killerName, killerKind, killerFaction, weapon, surrendered);

        public void ItemLooted(string item, string name, int count, string source)
            => Send("itemLooted", "(" + S + S + "I" + S + ")V", item, name, count, source);

        public void ItemCrafted(string item, int count) => Send("itemCrafted", "(" + S + "I)V", item, count);

        public void LocationVisited(string location) => Send("locationVisited", "(" + S + ")V", location);

        public void EmissionChanged(bool active) => Send("emissionChanged", "(Z)V", active);

        public void ArtifactLearned(string artifact, string property) => Send("artifactLearned", Two, artifact, property);

        public void DayAdvanced(int day) => Send("dayAdvanced", "(I)V", day);

        /// <summary>Carries out what the scripts' other threads asked of the game and the timers that are due.</summary>
        public void Pump() => Send("pump", "()V");

        /// <summary>Stops the scripts and unloads the session (the JVM stays for the next one).</summary>
        public void Dispose()
        {
            if (s_Current != this)
            {
                _jni.DeleteGlobalRef(_host);
                return;
            }
            try
            {
                _jni.CallStaticVoid(_host, Method("stop", "()V"));
            }
            finally
            {
                s_Current = null;
                s_Game = null;
                try { _jni.UnregisterNatives(_host); }
                finally { _jni.DeleteGlobalRef(_host); }
            }
        }

        private void Send(string name, string signature, params object[] args)
        {
            if (s_Current != this) throw new ObjectDisposedException(nameof(JavaSession));
            // The Java side reads empty strings, never null ones.
            for (int i = 0; i < args.Length; i++)
                if (args[i] == null) args[i] = "";
            _jni.CallStaticVoid(_host, Method(name, signature), args);
        }

        private IntPtr Method(string name, string signature)
        {
            if (!_methods.TryGetValue(name, out IntPtr id)) _methods[name] = id = _jni.StaticMethod(_host, name, signature);
            return id;
        }

        private delegate void LogFn(IntPtr env, IntPtr type, int level, IntPtr message);
        private delegate void ToastFn(IntPtr env, IntPtr type, IntPtr message);
        private delegate void SubtitleFn(IntPtr env, IntPtr type, IntPtr speaker, IntPtr line);

        // Statics, so the collector never takes a delegate the JVM calls.
        private static readonly LogFn s_LogFn = OnLog;
        private static readonly ToastFn s_ToastFn = OnToast;
        private static readonly SubtitleFn s_SubtitleFn = OnSubtitle;

        [MonoPInvokeCallback(typeof(LogFn))]
        private static void OnLog(IntPtr env, IntPtr type, int level, IntPtr message)
            => Deliver(game => game.Log(level, s_Jni.ReadString(env, message)));

        [MonoPInvokeCallback(typeof(ToastFn))]
        private static void OnToast(IntPtr env, IntPtr type, IntPtr message)
            => Deliver(game => game.Toast(s_Jni.ReadString(env, message)));

        [MonoPInvokeCallback(typeof(SubtitleFn))]
        private static void OnSubtitle(IntPtr env, IntPtr type, IntPtr speaker, IntPtr line)
            => Deliver(game => game.Subtitle(s_Jni.ReadString(env, speaker), s_Jni.ReadString(env, line)));

        // Nothing may be thrown back into the JVM.
        private static void Deliver(Action<IJavaGame> call)
        {
            IJavaGame game = s_Game;
            if (game == null) return;
            try
            {
                call(game);
            }
            catch (Exception e)
            {
                try { game.Log(2, "The game failed to carry out a call from Java: " + e); }
                catch (Exception) { }
            }
        }
    }
}
