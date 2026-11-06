using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CloneZ.Java;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Checks the Java host inside the editor's own process (Java/README.md, "Checking it"). It builds the scripts, opens
    /// the JVM, runs a session with RaidJournal and the self-test's scripts (Java/withthedead.selftest), sends it every
    /// game event, and checks what comes back: log lines, System.out, toasts, a subtitle, a timer, a call from a virtual
    /// thread, a script that throws, an event Java can't read, two mods that can't join (left out, the rest running) and a
    /// session that can't open. Headless
    /// (Tools/Java/SelfTest.ps1 runs <see cref="Run"/>) it then reloads the scripts' domain, as every script change does,
    /// checks again on the JVM that outlived it, and exits 0 when everything passed. CloneZ ▸ Java ▸ Self Test runs one
    /// round in the open editor.
    /// </summary>
    public static class JavaSelfTest
    {
        private const string AfterReloadKey = "CloneZ.Java.SelfTest.AfterReload";

        [MenuItem("CloneZ/Java/Self Test")]
        private static void Menu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[JavaSelfTest] Stop playing first: one Java session runs at a time.");
                return;
            }
            Debug.Log("[JavaSelfTest] RESULT " + (Round("in this editor", null) ? "PASS" : "FAIL"));
        }

        /// <summary>The headless run: a round on a new JVM, a domain reload, then a round on the same JVM.</summary>
        public static void Run()
        {
            if (!Round("on a new JVM", true))
            {
                Finish(false);
                return;
            }
            SessionState.SetBool(AfterReloadKey, true);
            EditorUtility.RequestScriptReload();
        }

        [InitializeOnLoadMethod]
        private static void AfterReload()
        {
            if (!SessionState.GetBool(AfterReloadKey, false)) return;
            SessionState.EraseBool(AfterReloadKey);
            EditorApplication.delayCall += () => Finish(Round("after a domain reload", false));
        }

        private static void Finish(bool ok)
        {
            Debug.Log("[JavaSelfTest] RESULT " + (ok ? "PASS" : "FAIL"));
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        private sealed class Recorder : IJavaGame
        {
            public readonly List<string> Logs = new List<string>(), Toasts = new List<string>(), Subtitles = new List<string>();

            public void Log(int level, string message) => Logs.Add(level + " " + message);

            public void Toast(string message) => Toasts.Add(message);

            public void Subtitle(string speaker, string line) => Subtitles.Add(speaker + ": " + line);
        }

        private static bool Round(string when, bool? newJvm)
        {
            int failed = 0;
            void Expect(bool ok, string what, string detail = "")
            {
                Debug.Log("[JavaSelfTest] " + (ok ? "PASS " : "FAIL ") + what + " (" + when + ")" + (ok || detail.Length == 0 ? "" : ": " + detail));
                if (!ok) failed++;
            }

            try
            {
                if (!JavaBuild.BuildIfStale(out string problem))
                {
                    Expect(false, "the Java scripts build", problem);
                    return false;
                }
                JavaLayout layout = JavaBuild.Layout(true);
                if (layout.Missing != null)
                {
                    Expect(false, "the editor's Java is there", layout.Missing);
                    return false;
                }
                var watch = Stopwatch.StartNew();
                Jni jni = JavaScripting.Jvm(layout);
                long opened = watch.ElapsedMilliseconds;
                if (newJvm.HasValue) Expect(jni.Created == newJvm.Value, newJvm.Value ? "the JVM is made" : "the JVM that outlived the domain is found");

                var game = new Recorder();
                // Two mods that can't join: a jar that isn't one, and a second withthedead.scripts.
                string mods = Path.Combine(JavaBuild.Output, "selftest-mods");
                if (Directory.Exists(mods)) Directory.Delete(mods, true);
                Directory.CreateDirectory(mods);
                File.WriteAllText(Path.Combine(mods, "broken.jar"), "not a jar");
                string scripts = Path.Combine(JavaBuild.Classes, "withthedead.scripts");
                foreach (string file in Directory.GetFiles(scripts, "*", SearchOption.AllDirectories))
                {
                    string copy = Path.Combine(mods, "withthedead.scripts", file.Substring(scripts.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                    File.Copy(file, copy);
                }
                string modulePath = layout.ModulePath + Path.PathSeparator + mods;
                watch.Restart();
                JavaSession session = JavaSession.Start(jni, modulePath, "selftest-version", game);
                long started = watch.ElapsedMilliseconds;
                Debug.Log("[JavaSelfTest] JVM in " + opened + " ms, session in " + started + " ms: " + string.Join(", ", session.Scripts));
                Expect(session.Scripts.SequenceEqual(new[] { "withthedead.scripts/RaidJournal", "withthedead.selftest/Faulty", "withthedead.selftest/Probe" }),
                    "every script starts, in order", string.Join(", ", session.Scripts));
                try
                {
                    session.PlayerSpawned();
                    session.RaidStarted("rookie_village", "OpenZone");
                    session.ActorKilled("Walker", "Zombie", "zombies", "Tester", "Player", "loners", "pistol_makarov", false);
                    session.ActorKilled("Bandit", "Human", "bandits", "", "", "", "", true);
                    session.ItemLooted("bolts", "Bolts", 3, "body");
                    session.LocationVisited("old_mill");
                    session.ItemCrafted("bandage", 2);
                    session.EmissionChanged(true);
                    session.ArtifactLearned("jellyfish", "radiation");
                    session.RaidEnded("Extracted", "North Gate");
                    session.DayAdvanced(13);
                    session.RaidStarted("nowhere", "NoSuchKind");
                    session.DayAdvanced(1);
                    var until = DateTime.UtcNow.AddSeconds(10);
                    while (DateTime.UtcNow < until && !(game.Toasts.Contains("selftest: timer") && game.Toasts.Contains("selftest: virtual thread")))
                    {
                        session.Pump();
                        System.Threading.Thread.Sleep(5);
                    }
                }
                finally
                {
                    session.Dispose();
                }

                foreach (string line in game.Logs.Concat(game.Toasts).Concat(game.Subtitles)) Debug.Log("[JavaSelfTest]   " + line.Split('\n')[0]);
                Expect(game.Logs.Any(l => l.StartsWith("0 RaidJournal is keeping the journal, on Java ", StringComparison.Ordinal)), "RaidJournal starts");
                Expect(game.Logs.Contains("0 selftest: started on Java 27 for selftest-version"), "a script logs, with the game's version");
                Expect(game.Logs.Contains("0 selftest: stdout"), "System.out goes to the log");
                Expect(game.Subtitles.Contains("Probe: selftest: subtitle"), "a script shows a subtitle");
                Expect(game.Toasts.Contains("selftest: timer"), "a timer fires on the main thread");
                Expect(game.Toasts.Contains("selftest: virtual thread"), "a virtual thread's call is carried out on the main thread");
                Expect(game.Toasts.Any(t => t.StartsWith("Out through North Gate: 1 killed, 3 picked up, 1 place found, ", StringComparison.Ordinal)),
                    "RaidJournal sums up the raid", string.Join(" | ", game.Toasts));
                string saw = game.Toasts.FirstOrDefault(t => t.StartsWith("selftest: saw ", StringComparison.Ordinal)) ?? "";
                foreach (string e in new[]
                {
                    "PlayerSpawned[]", "RaidStarted[zone=rookie_village, kind=OPEN_ZONE]",
                    "ActorKilled[victim=Actor[name=Walker, kind=ZOMBIE, faction=zombies], killer=Optional[Actor[name=Tester, kind=PLAYER, faction=loners]], weapon=pistol_makarov, surrendered=false]",
                    "ActorKilled[victim=Actor[name=Bandit, kind=HUMAN, faction=bandits], killer=Optional.empty, weapon=, surrendered=true]",
                    "ItemLooted[item=bolts, name=Bolts, count=3, source=body]", "LocationVisited[location=old_mill]",
                    "ItemCrafted[item=bandage, count=2]", "EmissionChanged[active=true]",
                    "ArtifactLearned[artifact=jellyfish, property=radiation]", "RaidEnded[outcome=EXTRACTED, exit=North Gate]",
                    "DayAdvanced[day=13]",
                })
                    Expect(saw.Contains(e), "the scripts get " + e.Substring(0, e.IndexOf('[')) + (e.Contains("Optional.empty") ? " with no killer" : ""), e);
                Expect(game.Logs.Any(l => l.StartsWith("2 withthedead.selftest/Faulty failed in on(DayAdvanced[day=13]): java.lang.IllegalStateException: selftest: a deliberate failure", StringComparison.Ordinal)),
                    "a script that throws is logged and the others go on");
                Expect(game.Logs.Any(l => l.StartsWith("2 An event from the game didn't match withthedead.api", StringComparison.Ordinal) && l.Contains("RaidKind.NO_SUCH_KIND")),
                    "an enum value Java doesn't have is logged, not thrown");
                Expect(game.Logs.Contains("0 selftest: stopped"), "the scripts are stopped");
                Expect(game.Logs.Any(l => l.StartsWith("1 Left out of the session: ", StringComparison.Ordinal) && l.Contains("broken.jar")),
                    "a mod jar that can't be read is left out, with a warning");
                Expect(game.Logs.Any(l => l.StartsWith("1 Left out of the session: withthedead.scripts (", StringComparison.Ordinal)),
                    "a second copy of a module is left out, with a warning");

                try
                {
                    JavaSession.Start(jni, "", "x", new Recorder()).Dispose();
                    Expect(false, "a session with no modules fails");
                }
                catch (JavaException e)
                {
                    Expect(e.Message.Contains("Module withthedead.host not found"), "a session with no modules fails", e.Message);
                }
                using (JavaSession again = JavaSession.Start(jni, layout.ModulePath, "again", new Recorder()))
                    Expect(again.Scripts.Length == 3, "a session starts after one that failed");
            }
            catch (Exception e)
            {
                Expect(false, "no exception", e.ToString());
            }
            return failed == 0;
        }
    }
}
