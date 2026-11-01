using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// An automated smoke test, inert unless the game (or a batch-mode editor in play mode) is started with
    /// <c>-clonez-smoke solo|hunter|host|client|lobby|wave</c>. It plays through the real flows — makes a character, walks into
    /// the bar, deploys into a Zone — and pokes the systems (a grenade, a drone, a car, a camp gate and a breaching
    /// charge, the tablet, cuffs, the economy; as a hunter, claws and a trick; as host and client, a networked raid),
    /// counting every error and exception the game logs. <c>lobby</c> plays the starter lobby in the town instead: into
    /// the Kettle from the title screen, out of its front door into the raid where you stand, and back through an exit;
    /// with <c>-clonez-smoke-shots &lt;dir&gt;</c> it photographs each step. <c>wave</c> plays Wave Survival through its own
    /// controls: every key and mouse action, every fixture action, every button of every panel, the pause menu, waves,
    /// saving, Continue and a death (see SmokeTest.Wave.cs). It uses its own profile file (and, for <c>wave</c>, its own
    /// save file) and never touches the player's. Results are logged as "[Smoke] ..." lines; the process exits with 0
    /// (clean) or 2 (errors). The editor's CloneZ ▸ Smoke Test menu plays a run in its own Game view instead, to watch it
    /// (stopping play mode when it's done), and <c>-clonez-smoke-pace &lt;seconds&gt;</c> leaves that pause between presses.
    /// On the statewide map <c>solo</c> ends by setting two of the dead on the survivor through a building's partition, then
    /// from outside through its outside wall (they must batter a hole through it and come through);
    /// <c>-clonez-smoke-breach-only</c> goes straight there from the deploy. <c>client</c> with
    /// <c>-clonez-smoke-join &lt;address[:port]&gt;</c> plays against a server already running, such as the in-house
    /// dedicated server (Tools/Server), opening the raid on it itself.
    /// </summary>
    public sealed partial class SmokeTest : MonoBehaviour
    {
        private const ushort Port = 7791;
        private static string s_Mode;
        private static int s_Errors;
        private static string s_LastError;
        private static string s_ShotDir;
        private static int s_SceneLoads;
        private static readonly List<string> s_FirstErrors = new List<string>();
        private readonly List<string> _checks = new List<string>();
        private int _failed;

        /// <summary>Session-state keys the editor's Smoke Test menu sets to play a mode in its own Game view, with a pace
        /// (seconds) between presses to watch it by.</summary>
        public const string MenuModeKey = "CloneZ.Smoke.Mode", MenuPaceKey = "CloneZ.Smoke.Pace";
        private static bool s_FromMenu, s_Finished, s_RanInBackground, s_BreachOnly;

        public static bool Running => s_Mode != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            if (s_Mode != null) return;
            string[] args = Environment.GetCommandLineArgs();
            s_FromMenu = false;
            s_Pace = 0f;
            string mode = ArgAfter(args, "-clonez-smoke") ?? MenuRequest();
            if (string.IsNullOrEmpty(mode)) return;
            s_Mode = mode.ToLowerInvariant();
            // An editor that skips the domain reload keeps statics from one play session to the next: start clean.
            s_Errors = 0;
            s_LastError = null;
            s_Saves = 0;
            s_SceneLoads = 0;
            s_Finished = false;
            s_FirstErrors.Clear();
            if (float.TryParse(ArgAfter(args, "-clonez-smoke-pace"), NumberStyles.Float, CultureInfo.InvariantCulture, out float pace)) s_Pace = pace;
            // Solo straight from the deploy to the breakable walls' checks (the runner's and the state's passes left out).
            s_BreachOnly = Array.IndexOf(args, "-clonez-smoke-breach-only") >= 0;
            s_ShotDir = ArgAfter(args, "-clonez-smoke-shots");
            if (s_ShotDir != null)
            {
                try { Directory.CreateDirectory(s_ShotDir); } catch { s_ShotDir = null; }
            }
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            ProfileStore.PathOverride = "smoke_" + s_Mode + "_profile.json";
            try { if (File.Exists(ProfileStore.FilePath)) File.Delete(ProfileStore.FilePath); } catch { }
            if (s_Mode == "wave") PrepareWave();
            Application.logMessageReceived += OnLog;
            s_RanInBackground = Application.runInBackground;
            Application.runInBackground = true;
#if UNITY_EDITOR
            if (s_FromMenu)
            {
                // A script change mid-run would reload the scripts under it and lose the run and its tidying up: the
                // reload waits until the run is over (OnPlayModeChanged).
                UnityEditor.EditorApplication.LockReloadAssemblies();
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
            }
#endif
            var go = new GameObject("[Smoke]");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>();
            Debug.Log("[Smoke] mode=" + s_Mode + " profile=" + ProfileStore.FilePath + (s_Pace > 0f ? " pace=" + s_Pace + " s" : ""));
        }

        private static string ArgAfter(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        // A run the editor's Smoke Test menu asked for, in its session state (which outlives the domain reload into play mode).
        private static string MenuRequest()
        {
#if UNITY_EDITOR
            string mode = UnityEditor.SessionState.GetString(MenuModeKey, "");
            if (mode.Length == 0) return null;
            UnityEditor.SessionState.EraseString(MenuModeKey);
            s_Pace = UnityEditor.SessionState.GetFloat(MenuPaceKey, 0f);
            s_FromMenu = true;
            return mode;
#else
            return null;
#endif
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode how) => s_SceneLoads++;

        private static void OnLog(string message, string stack, LogType type)
        {
            if (message.StartsWith("[CloneZ] Game saved", StringComparison.Ordinal)) s_Saves++;
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (message.StartsWith("[Smoke]")) return;
            // The editor's own AI Assistant relay complains in batch mode, its search indexer throws on startup in this Unity,
            // and with -nographics its play-mode layout has no device to draw on; none of it is any part of the game.
            if (stack != null && (stack.Contains("com.unity.ai.assistant") || stack.Contains("UnityEditor.Search.")
                || stack.Contains("UnityEditor.EditorApplicationLayout"))) return;
            s_Errors++;
            s_LastError = type + ": " + message + (string.IsNullOrEmpty(stack) ? "" : " @ " + FirstLine(stack));
            if (s_FirstErrors.Count < 30) s_FirstErrors.Add(s_LastError);
            // The NavMesh bake names no mesh it rejects; look for the culprit now (the bake is synchronous, so the scene is as
            // the bake saw it) and again on the next frame.
            if (message.Contains("invalid vertex data"))
            {
                ScanMeshes("during the bake");
                s_FindBadMeshes = true;
            }
        }

        private static bool s_FindBadMeshes;

        private void Update()
        {
            if (!s_FindBadMeshes) return;
            s_FindBadMeshes = false;
            ScanMeshes("a frame later");
        }

        private static void ScanMeshes(string when)
        {
            int found = 0;
            foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                Mesh m = mf.sharedMesh;
                if (m == null) continue;
                Vector3 s = mf.transform.lossyScale;
                string why = !m.isReadable ? "not readable" : m.vertexCount == 0 ? "no vertices" : !Finite(m) ? "non-finite"
                    : !IsFinite(mf.transform.position) || !IsFinite(s) ? "non-finite transform"
                    : s.x == 0f || s.y == 0f || s.z == 0f ? "zero scale" : null;
                if (why == null) continue;
                Debug.Log("[Smoke] suspect mesh " + when + " (" + why + "): " + PathOf(mf.transform) + " (mesh '" + m.name + "', "
                    + m.vertexCount + " vertices, " + m.subMeshCount + " submeshes)");
                if (++found >= 20) break;
            }
            if (found == 0) Debug.Log("[Smoke] suspect mesh " + when + ": none among the scene's mesh filters");
            foreach (TextMesh tm in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (tm.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) continue;
                var mod = tm.GetComponentInParent<Unity.AI.Navigation.NavMeshModifier>();
                if (mod != null && mod.ignoreFromBuild) continue;
                Debug.Log("[Smoke] text mesh in reach of a NavMesh bake " + when + ": " + PathOf(tm.transform) + " (text '" + tm.text + "')");
            }
        }

        private static bool Finite(Mesh m)
        {
            Bounds b = m.bounds;
            if (!IsFinite(b.center) || !IsFinite(b.extents)) return false;
            foreach (Vector3 v in m.vertices) if (!IsFinite(v)) return false;
            return true;
        }

        private static bool IsFinite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

        private static string PathOf(Transform t)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        private static string FirstLine(string s)
        {
            int n = s.IndexOf('\n');
            return n < 0 ? s : s.Substring(0, n);
        }

        private void Check(string name, bool ok, string detail = "")
        {
            _checks.Add((ok ? "PASS " : "FAIL ") + name + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")"));
            if (!ok) _failed++;
            Debug.Log("[Smoke] " + (ok ? "PASS " : "FAIL ") + name + (string.IsNullOrEmpty(detail) ? "" : " — " + detail));
        }

        private void Try(string name, Action act)
        {
            try { act(); Check(name, true); }
            catch (Exception e) { Check(name, false, e.GetType().Name + ": " + e.Message + " @ " + FirstLine(e.StackTrace ?? "")); }
        }

        private static IEnumerator Until(Func<bool> cond, float timeout)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!cond() && Time.realtimeSinceStartup < end) yield return null;
        }

        // The compass heading (degrees) with the longest clear run from a point, so a test drive isn't into a wall.
        private static float ClearestHeading(Vector3 from)
        {
            float best = 0f, bestRun = -1f;
            for (float yaw = 0f; yaw < 360f; yaw += 30f)
            {
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float run = Physics.Raycast(from, dir, out RaycastHit hit, 25f, Actor.SightMask, QueryTriggerInteraction.Ignore) ? hit.distance : 25f;
                if (run > bestRun) { bestRun = run; best = yaw; }
            }
            return best;
        }

        private IEnumerator Start()
        {
            yield return null;
            yield return null;
            if (s_Mode != "wave") MakeCharacter();   // the waves need no runner
            switch (s_Mode)
            {
                case "host": yield return Host(); break;
                case "client": yield return Client(); break;
                case "lobby": yield return Lobby(); break;
                case "wave": yield return WaveRun(); break;
                default: yield return Solo(s_Mode == "hunter"); break;
            }
            Finish();
        }

        #region Flows
        private void MakeCharacter()
        {
            Try("create character", () =>
            {
                string prof = ProfessionCatalog.Professions[0].Id;
                CharacterProfile c = ProfileStore.NewCharacter("Smoke " + s_Mode, prof, new List<string>(), Factions.Freehold, "O+", false);
                ProfessionCatalog.ApplyKit(c);
                ProfileStore.AddCharacter(c);
                GameSession.SurvivorName = c.Name;
            });
        }

        private IEnumerator ToHub()
        {
            GameFlow.GoToHub();
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null, 120f);
            Check("hub built", GameSession.IsHub && Actor.LocalPlayer != null);
            yield return new WaitForSeconds(2f);
        }

        private void HubScreensPass()
        {
            foreach (string id in new[] { HubScreens.Deploy, HubScreens.Squad, HubScreens.Tablet, HubScreens.Quests, HubScreens.Stash, HubScreens.Hideout, HubScreens.Sandbox, HubScreens.Manual, HubScreens.Upkeep })
                Try("screen " + id, () =>
                {
                    HubScreens.Open(id);
                    if (UIScreen.Current != null) UIScreen.Current.Close();
                });
        }

        private void GiveRaidKit()
        {
            Try("pack raid kit", () =>
            {
                PlayerInventory inv = HubKit.Carried;
                foreach (var (id, n) in new[] { ("grenade", 3), ("drone_fpv", 1), ("drone_ground", 1), ("tablet", 1), ("carwheel", 4), ("carbattery", 1), ("sparkplug", 1), ("radiator", 1),
                             ("hammer", 1), ("plank", 12), ("nails", 14), ("combolock", 1), ("handcuffs", 1), ("rope", 1), ("radio_walkie", 1), ("breachcharge", 1) })
                {
                    ItemDef d = ItemDatabase.Get(id);
                    if (d == null) throw new Exception("missing item " + id);
                    inv.Inventory.Add(d, n, enforceWeight: false);
                }
                HubKit.AfterChange();
            });
        }

        private IEnumerator Deploy(RaidKind kind)
        {
            if (kind == RaidKind.Monster) HunterForms.Chosen = CreatureCatalog.Shade;
            string[] args = Environment.GetCommandLineArgs();
            int z = Array.IndexOf(args, "-clonez-smoke-zone");
            string zone = z >= 0 && z + 1 < args.Length ? args[z + 1] : ZoneCatalog.DefaultZone;
            GameFlow.Deploy(zone, RaidWorldRules.OnFoot, kind);
            yield return Until(() => GameSession.IsExtraction && Actor.LocalPlayer != null && ExtractionManager.Instance != null, 240f);
            Check("raid built (" + kind + ")", GameSession.IsExtraction && Actor.LocalPlayer != null);
            yield return new WaitForSeconds(3f);
        }

        private IEnumerator Solo(bool hunter)
        {
            yield return ToHub();
            HubScreensPass();
            if (!hunter) GiveRaidKit();
            yield return Deploy(hunter ? RaidKind.Monster : RaidKind.Operative);
            Census();
            if (hunter) { yield return HunterPass(); yield break; }
            if (s_BreachOnly && KentuckyStreamer.Active != null)
            {
                yield return BreachPass(false);
                yield return BreachPass(true);
                yield break;
            }
            yield return RunnerPass();
            if (KentuckyStreamer.Active != null)
            {
                yield return StatewidePass();
                yield return BreachPass(false);
                yield return BreachPass(true);
            }
        }

        // The statewide map's partitions and outside walls, as the starter house's walls: two of the dead set on the survivor
        // through a wall of a furnished building (a partition, or with `outside` its outside wall, the dead outside and the
        // survivor in), every door near it shut, batter a hole through it; the NavMesh takes the hole in and the dead come
        // through it. (Two of them, side by side, as the dead come.)
        private IEnumerator BreachPass(bool outside)
        {
            string what = outside ? "outside walls: " : "partitions: ";
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            Actor me = Actor.LocalPlayer;
            Vector3 home = me.transform.position;
            // The dead must be able to hunt the survivor for this one (an earlier pass may have left them unseen or protected).
            bool untargetable = me.Untargetable;
            me.Untargetable = false;
            yield return Until(() => !ExtractionManager.PlayerProtected, 180f);
            BreakableWall wall = null;
            float spot = 0f;
            Vector3[] tries = { home, home + new Vector3(35f, 0f, 0f), home + new Vector3(-35f, 0f, 0f), home + new Vector3(0f, 0f, 35f),
                home + new Vector3(0f, 0f, -35f), home + new Vector3(70f, 0f, 70f), home + new Vector3(-70f, 0f, -70f) };
            foreach (Vector3 at in tries)
            {
                if (at != home) Teleport(me, at);
                yield return Until(() => streamer.IsNavigableAt(at), 60f);
                yield return new WaitForSeconds(6f);   // the interiors round it furnished, and the NavMesh taking them in
                wall = BreachableWall(me.transform.position, outside, out spot);
                if (wall != null) break;
            }
            Check(what + "a wall to break through near the survivor", wall != null, BreakableWall.All.Count + " breakable walls standing");
            if (wall == null)
            {
                me.Untargetable = untargetable;
                yield break;
            }

            float floor = wall.transform.position.y;
            // A partition's +across side, or an outside wall's inside (its +across is outward), is the survivor's.
            Vector3 across = (wall.WorldAt(0f, 0f, 1f) - wall.WorldAt(0f, 0f, 0f)).normalized * (outside ? -1f : 1f);
            Vector3 centre = wall.WorldAt(spot, 0f, 0f), preyAt = Snap(centre + across * 1.4f, floor);
            int doors = 0;
            foreach (RaidDoor d in RaidDoor.Registry)
                if (d != null && d.IsOpen && FlatDistance(d.transform.position, centre) < 30f)
                {
                    d.SetOpen(false, false);
                    doors++;
                }
            Teleport(me, preyAt);
            yield return new WaitForSeconds(2f);   // the shut doors' carves in
            {
                // Whether the survivor can be walked to at all from where the dead stand (the doors shut), for the log.
                Vector3 from = Snap(centre - across * 0.9f, floor);
                var way = new UnityEngine.AI.NavMeshPath();
                bool got = UnityEngine.AI.NavMesh.CalculatePath(from, preyAt, UnityEngine.AI.NavMesh.AllAreas, way);
                float len = 0f;
                for (int c = 1; c < way.corners.Length; c++) len += Vector3.Distance(way.corners[c - 1], way.corners[c]);
                string open = "";
                foreach (RaidDoor d in RaidDoor.Registry)
                    if (d != null && d.IsOpen && FlatDistance(d.transform.position, centre) < 80f)
                        open += (open.Length > 0 ? ", " : "") + FlatDistance(d.transform.position, centre).ToString("0") + " m";
                Debug.Log("[Smoke] breach setup: " + what + "the way from the dead's side to the survivor " + (got ? way.status + ", " + len.ToString("0.0") + " m, "
                    + way.corners.Length + " corners" + (way.corners.Length > 0 ? ", ending " + FlatDistance(way.corners[way.corners.Length - 1], preyAt).ToString("0.0") + " m off" : "") : "none")
                    + "; doors open within 80 m: " + (open.Length > 0 ? open : "none"));
            }
            var dead = new List<ZombieAI>();
            for (int k = -1; k <= 1; k += 2)
            {
                GameObject z = ZombieSpawner.BuildZombie(Snap(centre - across * 0.9f + (wall.WorldAt(spot + k * 0.45f, 0f, 0f) - centre), floor));
                var ai = z.GetComponent<ZombieAI>();
                ai.Configure(ZombieType.Shambler);
                dead.Add(ai);
            }
            yield return null;
            // They know where the survivor is (as if they'd seen them go in), and keep at it as the dead keep at a door.
            foreach (ZombieAI ai in dead) ai.Command(me);
            float t0 = Time.time;
            bool Open()
            {
                foreach (WallBreach h in wall.Holes)
                    if (h != null && h.IsOpen) return true;
                return false;
            }
            float traceAt = 0f;
            yield return Until(() =>
            {
                if (Time.time - t0 >= traceAt)
                {
                    traceAt += Time.time - t0 < 12f ? 0.5f : 5f;
                    Debug.Log("[Smoke] breach trace " + (Time.time - t0).ToString("0.0") + " s: " + Trace(dead) + "; holes " + wall.Holes.Count);
                }
                return Open();
            }, 75f);
            float took = Time.time - t0;
            string blows = "";
            foreach (WallBreach h in wall.Holes)
                if (h != null) blows += (blows.Length > 0 ? ", " : "") + h.State + " at " + h.Health.ToString("0");
            if (!Open())
            {
                // What the dead were at instead.
                foreach (ZombieAI ai in dead)
                {
                    if (ai == null) { blows += "; a zombie gone"; continue; }
                    var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    Vector3 p = ai.transform.position;
                    WallBreach pick = BreakableWall.NearestObstructing(p, me.transform.position, CloneZConfig.ZombieAttackRange + 0.8f);
                    blows += "; zombie side " + wall.SideOf(p) + " " + FlatDistance(p, centre).ToString("0.0") + " m off the hole, prey "
                        + (ai.Prey == null ? "none" : ai.Prey == me ? "the survivor" : ai.Prey.name) + ", path "
                        + (agent != null && agent.enabled && agent.isOnNavMesh ? agent.pathStatus + (agent.pathPending ? " (pending)" : "") : "off the NavMesh")
                        + ", the wall query: " + (pick == null ? "nothing (" + BreakableWall.LastMiss + ")" : pick.Wall == wall ? "this wall" : "another wall");
                }
                int open = 0;
                foreach (RaidDoor d in RaidDoor.Registry)
                    if (d != null && d.IsOpen && FlatDistance(d.transform.position, centre) < 30f) open++;
                wall.SpotFor(spot, 1f, true, out _, out _, out string why);
                blows += "; doors open now " + open + ", the spot: " + (why ?? "fine") + ", survivor " + FlatDistance(me.transform.position, centre).ToString("0.0")
                    + " m off (side " + wall.SideOf(me.transform.position) + ")";
            }
            Check(what + "the dead batter a hole through the wall", Open(), took.ToString("0.0") + " s, " + doors + " doors shut; " + blows);
            if (Open())
            {
                int side = wall.SideOf(preyAt);
                yield return Until(() =>
                {
                    foreach (ZombieAI ai in dead)
                        if (ai != null && wall.SideOf(ai.transform.position) == side && FlatDistance(ai.transform.position, me.transform.position) < 3f) return true;
                    return false;
                }, 30f);
                bool through = false;
                foreach (ZombieAI ai in dead)
                    if (ai != null && wall.SideOf(ai.transform.position) == side) through = true;
                Check(what + "the dead come through the hole", through, (Time.time - t0 - took).ToString("0.0") + " s after it gave way");
            }
            me.Untargetable = true;
            foreach (ZombieAI ai in dead)
                if (ai != null) ai.Kill();
            Teleport(me, home);
            yield return new WaitForSeconds(1f);
            me.Untargetable = untargetable;
        }

        // What the dead are at (their brains' own state, read for the log).
        private static string Trace(List<ZombieAI> dead)
        {
            var sb = new System.Text.StringBuilder();
            const BindingFlags all = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (ZombieAI ai in dead)
            {
                if (ai == null) { sb.Append("[gone] "); continue; }
                object F(string name) => typeof(ZombieAI).GetField(name, all)?.GetValue(ai);
                var agent = ai.GetComponent<UnityEngine.AI.NavMeshAgent>();
                sb.Append('[').Append(F("_state")).Append(" target ").Append(F("_hasTarget")).Append(" door ").Append(F("_door") != null ? "yes" : "no")
                    .Append(" hole ").Append(F("_wallHole") is UnityEngine.Object h && h != null ? "yes" : "no")
                    .Append(" cd ").Append(((float)F("_attackCooldown")).ToString("0.0"))
                    .Append(" path ").Append(agent != null && agent.isOnNavMesh ? agent.pathStatus + (agent.pathPending ? "*" : "") : "off");
                if (agent != null && agent.isOnNavMesh && agent.hasPath)
                {
                    Vector3[] corners = agent.path.corners;
                    sb.Append(" (").Append(corners.Length).Append(" corners, ").Append(agent.remainingDistance.ToString("0.0")).Append(" m left");
                    if (corners.Length > 0 && ai.Prey != null) sb.Append(", ends ").Append(FlatDistance(corners[corners.Length - 1], ai.Prey.transform.position).ToString("0.0")).Append(" m off the prey");
                    sb.Append(')');
                }
                else if (agent != null && agent.isOnNavMesh) sb.Append(" (no path)");
                sb.Append(" at ").Append(ai.transform.position.ToString("0.0")).Append("] ");
            }
            return sb.ToString();
        }

        // A breakable wall near `near` on a ground floor (a partition, or with `outside` an outside wall) with room for a hole that
        // the NavMesh comes up to both sides of, and the hole's middle along it.
        private static BreakableWall BreachableWall(Vector3 near, bool outside, out float spot)
        {
            spot = 0f;
            BreakableWall best = null;
            float bestD = float.MaxValue;
            foreach (BreakableWall w in BreakableWall.All)
            {
                if (w == null || (outside ? !(w is OutsideWall) : !(w is PartitionWall))) continue;
                if (Mathf.Abs(w.transform.position.y) > 0.5f) continue;
                float d = FlatDistance(w.transform.position, near);
                if (d > 45f || d >= bestD) continue;
                for (float t = -w.Length * 0.5f + 1.2f; t <= w.Length * 0.5f - 1.2f; t += 0.5f)
                {
                    w.AimAt(w.WorldAt(t, 1f, 0f));
                    if (!w.CanDemolish(out _) || !w.Walkable(w.Aimed)) continue;
                    best = w;
                    bestD = d;
                    spot = w.Aimed;
                    break;
                }
            }
            return best;
        }

        // The NavMesh's point nearest `p` on the floor at `floor` (else `p`).
        private static Vector3 Snap(Vector3 p, float floor)
        {
            p.y = floor + 0.1f;
            return UnityEngine.AI.NavMesh.SamplePosition(p, out UnityEngine.AI.NavMeshHit hit, 1f, UnityEngine.AI.NavMesh.AllAreas) ? hit.position : p;
        }

        private static void Teleport(Actor me, Vector3 to)
        {
            var cc = me.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            me.transform.position = to;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
        }

        // The statewide map: far across the state and back. The ground, the NavMesh and the dead follow the survivor; a
        // stretch let go of and built again keeps what was taken from it; the start town stays built.
        private IEnumerator StatewidePass()
        {
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            Actor me = Actor.LocalPlayer;
            bool untargetable = me.Untargetable;
            me.Untargetable = true;   // the dead may come; this pass isn't about them biting
            Vector3 home = me.transform.position;
            Check("statewide: start built", streamer.IsBuiltAt(home), streamer.Summary);
            yield return Until(() => streamer.IsNavigableAt(home), 60f);
            Check("statewide: start navigable", streamer.IsNavigableAt(home));

            Vector3 near = home + new Vector3(1500f, 0f, 300f), far = home + new Vector3(9000f, 0f, -4500f);
            yield return Travel(streamer, me, near, "1.5 km east");
            LootContainer box = null;
            foreach (LootContainer c in LootContainer.All)
                if (c != null && c.transform.IsChildOf(streamer.World) && !c.IsEmpty && FlatDistance(c.transform.position, me.transform.position) < 150f)
                {
                    box = c;
                    break;
                }
            Check("statewide: loot out there", box != null, LootContainer.All.Count + " containers");
            Vector3 boxAt = box != null ? box.transform.position : Vector3.zero;
            if (box != null)
            {
                box.Contents.Stacks.Clear();
                box.Searched = true;
            }
            yield return Travel(streamer, me, far, "9 km east and 4.5 km south");
            if (box != null || boxAt != Vector3.zero)
                Check("statewide: the stretch behind was let go of", box == null && !streamer.IsBuiltAt(near), streamer.Summary);
            yield return Travel(streamer, me, near, "back 1.5 km east");
            if (boxAt != Vector3.zero)
            {
                LootContainer again = null;
                foreach (LootContainer c in LootContainer.All)
                    if (c != null && FlatDistance(c.transform.position, boxAt) < 0.2f) { again = c; break; }
                Check("statewide: an emptied container stays empty", again != null && again.IsEmpty && again.Searched,
                    again == null ? "not found again" : again.Contents.Stacks.Count + " stacks");
            }
            yield return Travel(streamer, me, home, "home");
            Check("statewide: the start town stayed built", streamer.IsBuiltAt(home), streamer.Summary);
            me.Untargetable = untargetable;
        }

        // A long drive in a frame: the survivor is set down at <paramref name="to"/> and the state must catch up.
        private IEnumerator Travel(KentuckyStreamer streamer, Actor me, Vector3 to, string label)
        {
            to.y = 0.1f;
            var cc = me.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            me.transform.position = to;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            float t0 = Time.realtimeSinceStartup;
            yield return Until(() => streamer.IsNavigableAt(to), 120f);
            Check("statewide: " + label + " built and navigable", streamer.IsNavigableAt(to),
                (Time.realtimeSinceStartup - t0).ToString("0.0") + " s; " + streamer.Summary);
            yield return new WaitForSeconds(2f);
            Vector3 p = me.transform.position;
            Check("statewide: standing " + label, p.y > -0.5f && p.y < 2f && FlatDistance(p, to) < 30f, "at " + p);
            yield return Until(() => ZombiesNear(p, 300f) > 0, 120f);
            Check("statewide: the dead roam " + label, ZombiesNear(p, 300f) > 0, ZombiesNear(p, 300f) + " within 300 m");
        }

        private static float FlatDistance(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

        private static int ZombiesNear(Vector3 p, float radius)
        {
            int n = 0;
            foreach (Actor a in Actor.All)
                if (a != null && a.Kind == ActorKind.Zombie && FlatDistance(a.transform.position, p) < radius) n++;
            return n;
        }

        private void Census()
        {
            int zombies = 0, humans = 0, beasts = 0;
            foreach (Actor a in Actor.All)
            {
                if (a == null) continue;
                if (a.Kind == ActorKind.Zombie) zombies++;
                else if (a.Kind == ActorKind.Human || a.Kind == ActorKind.Companion) humans++;
                else if (a.Kind == ActorKind.Mutant || a.Kind == ActorKind.Animal) beasts++;
            }
            int anomalies = FindObjectsByType<Anomaly>().Length;
            Debug.Log("[Smoke] census: zombies=" + zombies + " humans=" + humans + " beasts=" + beasts + " anomalies=" + anomalies
                + " containers=" + LootContainer.All.Count + " cars=" + Vehicle.All.Count + " outposts=" + OutpostSystem.Sites.Count);
            Check("zone populated", LootContainer.All.Count > 0 && anomalies > 0, "containers=" + LootContainer.All.Count + " anomalies=" + anomalies);
            Check("cars parked", Vehicle.All.Count > 0, Vehicle.All.Count + " cars");
        }

        private IEnumerator RunnerPass()
        {
            PlayerStats me = Actor.LocalPlayer.GetComponent<PlayerStats>();
            PlayerInventory inv = me.GetComponent<PlayerInventory>();

            Try("tablet pages", () =>
            {
                TabletScreen t = UIScreen.Show<TabletScreen>();
                FieldInfo tab = typeof(TabletScreen).GetField("s_Tab", BindingFlags.NonPublic | BindingFlags.Static);
                foreach (int page in new[] { 0, 1, 2, 4 }) { tab.SetValue(null, page); t.Rebuild(); }
                t.Close();
            });

            Try("grenade thrown", () => Explosives.Throw(inv));
            yield return new WaitForSeconds(4.5f);

            Try("drone flight", () =>
            {
                DronePilot.Launch(inv, DroneKind.Fpv);
                if (DronePilot.Local == null) throw new Exception("no pilot");
                for (int i = 0; i < 60; i++) DronePilot.Local.Drone.Fly(new Vector2(0f, 1f), 0.5f, 2f, 0.02f);
            });
            yield return new WaitForSeconds(1f);
            Try("drone grenade + land", () =>
            {
                if (DronePilot.Local == null) return;
                DronePilot.Local.Drone.DropGrenade(me.gameObject);
                DronePilot.Local.End();
            });
            yield return new WaitForSeconds(4f);

            // The ground drone must keep to the ground as it drives (it once climbed its own back).
            Drone ground = null;
            float groundY0 = 0f;
            Vector3 groundFrom = Vector3.zero;
            Try("ground drone deployed", () =>
            {
                DronePilot.Launch(inv, DroneKind.Ground);
                if (DronePilot.Local == null) throw new Exception("no pilot");
                ground = DronePilot.Local.Drone;
                groundY0 = ground.transform.position.y;
                groundFrom = ground.transform.position;
            });
            if (ground != null)
            {
                for (int i = 0; i < 90 && ground != null; i++) { ground.Fly(new Vector2(0f, 1f), 0f, 0f, Mathf.Min(Time.deltaTime, 0.05f)); yield return null; }
                if (ground != null)
                {
                    Vector3 d = ground.transform.position - groundFrom;
                    Check("ground drone drove on the ground", Mathf.Abs(ground.transform.position.y - groundY0) < 0.3f,
                        "moved " + new Vector2(d.x, d.z).magnitude.ToString("0.0") + " m, rose " + (ground.transform.position.y - groundY0).ToString("0.00") + " m");
                }
                Try("ground drone recalled", () => { if (DronePilot.Local != null) DronePilot.Local.End(); });
            }

            Vehicle car = Vehicle.All.Count > 0 ? Vehicle.All[0] : null;
            if (car != null)
            {
                Try("car assembled and started", () =>
                {
                    car.Wheels = 4; car.Battery = car.SparkPlug = car.Radiator = car.Hotwired = true; car.Fuel = 20f; car.Charge = 1f; car.Engine = 1f;
                    car.RefreshParts();
                    car.transform.rotation = Quaternion.Euler(0f, ClearestHeading(car.transform.position + Vector3.up * 0.8f), 0f);
                    Physics.SyncTransforms();
                    car.Enter(me);
                    if (!car.TryStart(out string why)) throw new Exception(why);
                });
                Vector3 before = car.transform.position;
                for (int i = 0; i < 120; i++) { if (car.Driver != null) car.Drive(1f, 0f, false, 0.02f); yield return new WaitForFixedUpdate(); }
                Vector3 moved = car.transform.position - before;
                float flat = new Vector2(moved.x, moved.z).magnitude;
                Check("car drove", flat > 0.5f && flat < 40f && Mathf.Abs(moved.y) < 1f, "moved " + flat.ToString("0.0") + " m, rose " + moved.y.ToString("0.00") + " m");
                Try("car exit", () => { if (VehicleDriver.Local != null) VehicleDriver.Local.Leave(); });
            }

            BuildPiece door = null;
            Try("camp doorway + gate + lock", () =>
            {
                Vector3 at = me.transform.position + me.transform.forward * 6f;
                at.y = CloneZConfig.HouseWallHeight * 0.5f;
                door = BuildPiece.Spawn(null, BuildPieceType.Doorway, at, 0f, 1f);
                door.Owner = ZoneCamps.MyTag;
                BuildPiece.RaiseRaised(door);
                CampGate g = door.GetComponent<CampGate>();
                if (g == null) throw new Exception("no gate component");
                g.Apply(true, false, true, "123");
                ZoneCamps.Save(GameSession.ZoneId);
                if (ProfileStore.Account.Zone(GameSession.ZoneId).Camp.Count < 1) throw new Exception("camp not saved");
            });
            if (door != null)
            {
                Try("breaching charge armed", () => BreachCharge.Place(door.transform.position - door.transform.forward * 0.5f, me.gameObject));
                yield return new WaitForSeconds(11f);
                Check("charge blew the gate", door == null || door.Health <= 0f);
            }

            Try("cuffs and rope", () =>
            {
                PlayerRestraint r = PlayerRestraint.Local;
                if (r == null) throw new Exception("no restraint component");
                r.Cuff("Smoke", "rope");
                if (!r.Cuffed) throw new Exception("not cuffed");
                r.Free();
            });
            Try("central economy tick", () => { CentralEconomy.Recount(); CentralEconomy.Tick(); });
            Try("quest progress", () => QuestBook.Apply("kill:human", 1f));
            yield return new WaitForSeconds(3f);
        }

        #region The lobby in the town
        // The starter lobby: the title screen, into the Kettle under the loading card (the town built a step at a time), the
        // bar standing on its clear lot in Bardstown, walking out of its front door into the raid where you stand (no
        // reload), and back through an exit to the bar again. With -clonez-smoke-shots <dir>, pictures of each.
        private IEnumerator Lobby()
        {
            // The title, rebuilt so the runner made above has its card.
            var boot = FindAnyObjectByType<GameBootstrapper>();
            MainMenuController old = FindAnyObjectByType<MainMenuController>();
            if (old != null) Destroy(old.gameObject);
            yield return null;
            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            menu.Bind(boot);
            yield return new WaitForSeconds(1.5f);
            Shot("01-title", null);

            // Into the Kettle the way the title screen goes.
            int loadsBefore = s_SceneLoads;
            float t0 = Time.realtimeSinceStartup;
            Try("into the Kettle", () => typeof(MainMenuController).GetMethod("EnterHub", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(menu, null));
            yield return null;
            yield return null;
            yield return null;
            Check("loading card up", LoadingScreen.Showing);
            Shot("02-loading", null);
            int frames = 0;
            while (!(GameSession.IsHub && Actor.LocalPlayer != null) && Time.realtimeSinceStartup - t0 < 180f)
            {
                frames++;
                if (frames == 120) Shot("02b-loading-progress", null);
                yield return null;
            }
            float secs = Time.realtimeSinceStartup - t0;
            Check("hub up", GameSession.IsHub && Actor.LocalPlayer != null, secs.ToString("0.0") + " s");
            Check("the Kettle stands in the town", KettleLobby.Active && KentuckyStreamer.Active != null,
                KettleLobby.Active ? "lot " + KentuckyMap.ReservedLot.Center + " on " + KentuckyMap.ReservedLot.Street : "standalone (" + KentuckyMap.LotWhy + ")");
            Check("the town went up a step at a time under the card", frames > 30, frames + " frames in " + secs.ToString("0.0") + " s");
            if (!KettleLobby.Active) yield break;
            yield return Until(() => !LoadingScreen.Showing, 10f);
            Check("loading card gone", !LoadingScreen.Showing);
            yield return new WaitForSeconds(2.5f);

            Actor me = Actor.LocalPlayer;
            Transform room = KettleLobby.Room;
            Check("survivor inside the bar", KettleLobby.Inside(me.transform.position), "at " + me.transform.position);
            Check("hub rules hold in the bar", GameSession.IsHub && HubRules.SafeZone && ExtractionManager.Instance == null);
            List<string> intruders = TownInsideBar(room);
            Check("nothing of the town stands in the bar", intruders.Count == 0, intruders.Count == 0 ? "clear" : string.Join(", ", intruders));
            Check("stations and keepers", room.GetComponentsInChildren<HubStation>().Length >= 12 && room.GetComponentsInChildren<HubNpc>().Length >= 10,
                room.GetComponentsInChildren<HubStation>().Length + " stations, " + room.GetComponentsInChildren<HubNpc>().Length + " people");
            Check("sheltered inside", Shelter.IsSheltered(me.transform.position + Vector3.up));
            Check("the town's loot is the raid's (found in raid)", TownLootFound(room, out string loot), loot);
            Check("no anomaly or radiation in the safe zone round the bar", SafeZoneClear(out string hazards), hazards);
            Check("no anomaly in the Rusty Kettle, and none can be put there", KettleClear(room, out string kettle), kettle);
            RaidMapInfo raidMap = ZoneWorld.Map;
            float startToDoor = raidMap != null && KettleLobby.Door != null
                ? Vector2.Distance(new Vector2(raidMap.PlayerSpawn.x, raidMap.PlayerSpawn.z),
                    new Vector2(KettleLobby.Door.transform.position.x, KettleLobby.Door.transform.position.z))
                : -1f;
            Check("the raid starts at the bar's door", startToDoor >= 0f && startToDoor < 4f, startToDoor.ToString("0.0") + " m from the door");

            // No rain or snow in the bar, whatever the sky does: a mild night's rain first, then the winter night's flurry
            // (a dry sky after dark always brings one; rain over thawing air would wash it out).
            bool winter = Climate.Winter;
            WeatherSystem weather = WeatherSystem.Instance;
            Transform rainHost = weather != null ? weather.transform.Find("Rain") : null;
            ParticleSystem rain = rainHost != null ? rainHost.GetComponent<ParticleSystem>() : null;
            SnowFX snowFx = FindAnyObjectByType<SnowFX>();
            ParticleSystem snow = snowFx != null ? snowFx.GetComponent<ParticleSystem>() : null;
            Climate.Winter = false;
            if (weather != null) weather.Restore(Weather.Rain, 0.6f, 0.28f, 0.75f);
            yield return new WaitForSeconds(1f);
            Check("no rain in the bar", rain != null && weather.RainIntensity > 0.3f && rain.particleCount == 0,
                rain != null ? "raining " + weather.RainIntensity.ToString("0.00") + ", " + rain.particleCount + " drops" : "no rain");
            Climate.Winter = true;
            if (weather != null) weather.Restore(Weather.Overcast, 0f, 0.28f, 1f);
            yield return new WaitForSeconds(1f);
            Check("no snow in the bar", snow != null && Climate.SnowIntensity > 0.1f && snow.particleCount == 0,
                snow != null ? "snowing " + Climate.SnowIntensity.ToString("0.00") + ", " + snow.particleCount + " flakes" : "no snow");
            Climate.Winter = winter;
            Shot("03-bar-iso", null);
            HubScreensPass();
            GiveRaidKit();

            // Out of the door.
            KettleDoor door = KettleLobby.Door;
            Check("front door", door != null);
            if (door == null) yield break;
            Try("open the door", () => door.SetOpen(true, false));
            yield return new WaitForSeconds(1f);
            var cc = me.GetComponent<CharacterController>();
            Vector3 outward = door.transform.rotation * Vector3.back;
            Vector3 porch = door.transform.position + outward * 2.2f;
            for (int i = 0; i < 400 && !KettleLobby.RaidUnderway; i++)
            {
                Vector3 to = porch - me.transform.position;
                to.y = 0f;
                // Line up on the doorway first, then straight out through it.
                Vector3 lane = door.transform.position + outward * Vector3.Dot(me.transform.position - door.transform.position, outward);
                Vector3 side = lane - me.transform.position;
                side.y = 0f;
                Vector3 step = (side.magnitude > 0.15f ? side : to).normalized * 0.06f;
                if (cc != null && cc.enabled) cc.Move(step + Vector3.down * 0.02f);
                yield return null;
            }
            Check("walked out of the door", !KettleLobby.Inside(me.transform.position), "at " + me.transform.position);
            Check("the raid began where we stood", KettleLobby.RaidUnderway && GameSession.IsExtraction && ExtractionManager.Instance != null && !HubRules.SafeZone);
            Check("no reload", s_SceneLoads == loadsBefore, (s_SceneLoads - loadsBefore) + " scene loads since the title");
            yield return null;
            yield return null;
            Check("door barred behind us", door.Barred && !door.IsOpen);
            Check("stash gone with the hub", room.GetComponentsInChildren<HubStashLocker>(true).Length == 0);
            Check("one HUD, one pause menu", FindObjectsByType<HUDController>().Length == 1 && FindObjectsByType<PauseMenuController>().Length == 1,
                FindObjectsByType<HUDController>().Length + " HUDs, " + FindObjectsByType<PauseMenuController>().Length + " menus");
            Check("the dead are coming", FindAnyObjectByType<ZombieSpawner>() != null && FindAnyObjectByType<ZombieSpawner>().enabled);
            Check("exits up", ExtractionManager.Instance != null && ExtractionManager.Instance.Exits.Count > 0, ExtractionManager.Instance != null ? ExtractionManager.Instance.Exits.Count + " exits" : "none");
            Shot("05-out-the-door", null);
            // On out across the porch and down the steps into the street: the roof comes back over the bar.
            for (int i = 0; i < 160 && Vector3.Dot(me.transform.position - door.transform.position, outward) < 5.5f; i++)
            {
                if (cc != null && cc.enabled) cc.Move(outward * 0.06f + Vector3.down * 0.02f);
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            Check("roof back over the bar from the street", RoofShown(), "survivor " + Vector3.Dot(me.transform.position - door.transform.position, outward).ToString("0.0") + " m out of the door");
            Check("standing in the street, not fallen through it", FallWatch.Caught == 0 && cc != null && cc.isGrounded && me.transform.position.y > -1f,
                "y " + me.transform.position.y.ToString("0.00") + (FallWatch.Caught > 0 ? ", fell out of the world " + FallWatch.Caught + " time(s)" : ""));
            // From the street, rain and snow fall round the bar and none inside it.
            Climate.Winter = false;
            if (weather != null) weather.Restore(Weather.Rain, 0.6f, 0.28f, 0.75f);
            yield return new WaitForSeconds(1.5f);
            int wet = rain != null ? InBar(rain, room, 0f) : -1;
            Check("rain falls round the bar, none inside it", rain != null && rain.particleCount > 100 && wet == 0,
                rain != null ? rain.particleCount + " drops, " + wet + " inside" : "no rain");
            Shot("06a-street-rain", null);
            Climate.Winter = true;
            if (weather != null) weather.Restore(Weather.Overcast, 0f, 0.28f, 1f);
            yield return new WaitForSeconds(2f);
            // (a flake's noise may carry it a hand's breadth past a wall; none gets further in)
            int flakesIn = snow != null ? InBar(snow, room, 0.5f) : -1;
            Check("snow falls round the bar, none inside it", snow != null && Climate.SnowIntensity > 0.1f && snow.particleCount > 0 && flakesIn == 0,
                snow != null ? snow.particleCount + " flakes, " + flakesIn + " inside (snowing " + Climate.SnowIntensity.ToString("0.00") + ")" : "no snow");
            Climate.Winter = winter;
            Shot("06-street-iso", null);
            KentuckyMap.Lot lot = KentuckyMap.ReservedLot;
            Vector2 front = lot.ToWorld(new Vector2(8f, -30f));
            Vector2 aim = lot.ToWorld(new Vector2(0f, -4f));
            ShotFrom("07-bar-street", new Vector3(front.x, 16f, front.y), new Vector3(aim.x, 2.5f, aim.y), 45f);
            yield return Until(() => ZombiesNear(me.transform.position, 200f) > 0, 90f);
            Check("the dead roam the town", ZombiesNear(me.transform.position, 200f) > 0, ZombiesNear(me.transform.position, 200f) + " within 200 m");
            Census();

            // Back through an exit, and so to the bar again (its town built afresh).
            ExtractionManager em = ExtractionManager.Instance;
            RaidExit exit = null;
            foreach (RaidExit e in em.Exits) if (e.Open && e.Condition == ExitCondition.None) { exit = e; break; }
            Check("an open exit", exit != null);
            if (exit == null) yield break;
            me.Untargetable = true;
            if (cc != null) cc.enabled = false;
            me.transform.position = exit.Position + Vector3.up * 0.6f;
            if (cc != null) cc.enabled = true;
            Physics.SyncTransforms();
            yield return Until(() => em == null || em.State == ExtractionManager.RaidState.Extracted, 60f);
            Check("extracted", em != null && em.State == ExtractionManager.RaidState.Extracted, exit.Name);
            yield return new WaitForSecondsRealtime(1.5f);
            Try("back to the bar", ExtractionManager.ReturnFromRaid);
            yield return Until(() => s_SceneLoads > loadsBefore, 30f);
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null && KettleLobby.Active, 180f);
            Check("back in the Kettle, in the town", GameSession.IsHub && KettleLobby.Active && KettleLobby.Inside(Actor.LocalPlayer.transform.position));
            yield return new WaitForSeconds(2f);

            // The hub on its own (another hub, a networked session, or a town the world store can't build): the same bar,
            // whose door opens the deploy screen.
            KettleLobby.Disabled = true;
            int loads = s_SceneLoads;
            Try("the hub on its own", GameFlow.GoToHub);
            yield return Until(() => s_SceneLoads > loads, 30f);
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null && !LoadingScreen.Showing, 60f);
            Check("the hub stands on its own", GameSession.IsHub && Actor.LocalPlayer != null && !KettleLobby.Active && KentuckyStreamer.Active == null
                && FindAnyObjectByType<KettleDoor>() == null);
            yield return new WaitForSeconds(2f);
            Shot("08-standalone-iso", null);
            KettleLobby.Disabled = false;
        }

        // Anything of the town's own (a prop, a tree, a car, a wall) standing inside the bar's walls.
        private static List<string> TownInsideBar(Transform room)
        {
            var found = new List<string>();
            Vector2 half = HubWorld.HalfSize;
            Collider[] hits = Physics.OverlapBox(room.TransformPoint(new Vector3(0f, 1.3f, 0f)), new Vector3(half.x - 0.4f, 1.0f, half.y - 0.4f),
                room.rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider c in hits)
                if (c != null && !c.transform.IsChildOf(room) && c.GetComponentInParent<Actor>() == null) found.Add(c.name);
            return found;
        }

        // True when the Kettle's roof is drawn (it lifts away only while the survivor is inside, in the isometric view).
        private static bool RoofShown()
        {
            KettleRoof roof = FindAnyObjectByType<KettleRoof>();
            if (roof == null || roof.Parts == null) return false;
            foreach (Renderer r in roof.Parts.GetComponentsInChildren<Renderer>())
                if (r.shadowCastingMode != ShadowCastingMode.On) return false;
            return true;
        }

        // Every stack the town's containers were filled with round the bar is found in raid (the bar's own stash aside).
        private static bool TownLootFound(Transform room, out string detail)
        {
            int stacks = 0, found = 0;
            foreach (LootContainer c in LootContainer.All)
            {
                if (c == null || c.Contents == null || c.IsSack || c.Type == ContainerType.Corpse || c.transform.IsChildOf(room)) continue;
                foreach (ItemStack s in c.Contents.Stacks)
                {
                    if (s == null || s.Def == null) continue;
                    stacks++;
                    if (s.IsFoundInRaid()) found++;
                }
            }
            detail = found + " of " + stacks + " stacks found in raid";
            return stacks > 0 && found == stacks;
        }

        // None of the Zone's hazards reaches into the safe zone round the bar.
        private static bool SafeZoneClear(out string detail)
        {
            int inside = 0;
            float nearest = float.MaxValue;
            Vector3 door = KettleLobby.Door != null ? KettleLobby.Door.transform.position : Vector3.zero;
            void Look(Vector3 at, float radius)
            {
                if (KettleLobby.InSafeZone(at, radius)) inside++;
                nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(at.x, at.z), new Vector2(door.x, door.z)) - radius);
            }
            foreach (Anomaly a in Anomaly.All) if (a != null) Look(a.transform.position, a.Radius);
            foreach (RadiationSpot r in RadiationSpot.All) if (r != null) Look(r.transform.position, r.Radius);
            detail = Anomaly.All.Count + " anomalies and " + RadiationSpot.All.Count + " radiation spots in the town, " + inside
                + " in the safe zone; the nearest reaches to " + (nearest < float.MaxValue ? nearest.ToString("0") + " m" : "-") + " from the door";
            return inside == 0;
        }

        // By the bar's own walls (not the lot the hazards were kept off): nothing of the Zone's reaches inside, and every kind
        // of anomaly asked for in its middle is refused.
        private static bool KettleClear(Transform room, out string detail)
        {
            int reaching = 0;
            foreach (Anomaly a in Anomaly.All) if (a != null && HubWorld.Inside(room, a.transform.position, a.Radius)) reaching++;
            foreach (RadiationSpot r in RadiationSpot.All) if (r != null && HubWorld.Inside(room, r.transform.position, r.Radius)) reaching++;
            var put = new List<string>();
            foreach (AnomalyDef def in AnomalyCatalog.All.Values)
            {
                Anomaly stray = Anomaly.Spawn(room, def, room.position, 1f, new System.Random(7));
                if (stray == null) continue;
                put.Add(def.Id);
                UnityEngine.Object.DestroyImmediate(stray.gameObject);
            }
            detail = reaching + " of the Zone's hazards reach inside its walls; "
                + (put.Count == 0 ? "all " + AnomalyCatalog.All.Count + " kinds of anomaly refused in the bar" : "put in the bar: " + string.Join(", ", put));
            return reaching == 0 && put.Count == 0;
        }

        // The particles of a falling effect inside the bar's walls (more than <paramref name="inset"/> in), between its floor
        // and its ceiling.
        private static int InBar(ParticleSystem ps, Transform room, float inset)
        {
            var parts = new ParticleSystem.Particle[ps.main.maxParticles];
            int n = ps.GetParticles(parts);
            int inside = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = parts[i].position;
                float y = room.InverseTransformPoint(p).y;
                if (y > 0f && y < 3.2f && HubWorld.Inside(room, p, -inset)) inside++;
            }
            return inside;
        }

        // A picture through the game's camera (with the screen's UI when <paramref name="cam"/> is null).
        private static void Shot(string name, Camera cam)
        {
            if (string.IsNullOrEmpty(s_ShotDir)) return;
            Camera main = Camera.main;
            var go = new GameObject("[SmokeShot]");
            Camera shot = go.AddComponent<Camera>();
            if (cam != null) shot.CopyFrom(cam);
            else if (main != null) shot.CopyFrom(main);
            else
            {
                shot.clearFlags = CameraClearFlags.SolidColor;
                shot.backgroundColor = Color.black;
                shot.cullingMask = 0;
            }
            Capture(name, shot, cam == null);
            Destroy(go);
        }

        // A picture from a place of our own choosing (the bar from the street, from above).
        private static void ShotFrom(string name, Vector3 eye, Vector3 at, float fov)
        {
            if (string.IsNullOrEmpty(s_ShotDir)) return;
            var go = new GameObject("[SmokeShot]");
            Camera shot = go.AddComponent<Camera>();
            Camera main = Camera.main;
            if (main != null) shot.CopyFrom(main);
            shot.orthographic = false;
            shot.fieldOfView = fov;
            shot.nearClipPlane = 0.2f;
            shot.farClipPlane = 800f;
            shot.cullingMask = ~0;
            go.transform.position = eye;
            go.transform.LookAt(at);
            Capture(name, shot, false);
            Destroy(go);
        }

        private static void Capture(string name, Camera cam, bool withUi)
        {
            const int W = 1600, H = 900;
            var moved = new List<(Canvas canvas, RenderMode mode, Camera camera, float distance)>();
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { name = "[SmokeShot]", antiAliasing = 1 };
            rt.Create();
            RenderTexture active = RenderTexture.active;
            try
            {
                cam.enabled = false;
                cam.aspect = W / (float)H;
                if (withUi)
                    foreach (Canvas c in FindObjectsByType<Canvas>())
                    {
                        if (c == null || !c.isRootCanvas || !c.isActiveAndEnabled || c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                        moved.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                        c.renderMode = RenderMode.ScreenSpaceCamera;
                        c.worldCamera = cam;
                        c.planeDistance = cam.nearClipPlane + 0.02f + (5000 - Mathf.Clamp(c.sortingOrder, 0, 5000)) * 0.0001f;
                    }
                Canvas.ForceUpdateCanvases();
                for (int pass = 0; pass < 2; pass++)
                {
                    var request = new RenderPipeline.StandardRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(cam, request)) RenderPipeline.SubmitRenderRequest(cam, request);
                    else
                    {
                        cam.targetTexture = rt;
                        cam.Render();
                        cam.targetTexture = null;
                    }
                }
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0, false);
                tex.Apply(false);
                File.WriteAllBytes(Path.Combine(s_ShotDir, name + ".png"), tex.EncodeToPNG());
                Destroy(tex);
                Debug.Log("[Smoke] shot " + name);
            }
            catch (Exception e) { Debug.LogWarning("[Smoke] shot " + name + " failed: " + e.Message); }
            finally
            {
                foreach (var m in moved)
                {
                    if (m.canvas == null) continue;
                    m.canvas.renderMode = m.mode;
                    m.canvas.worldCamera = m.camera;
                    m.canvas.planeDistance = m.distance;
                }
                RenderTexture.active = active;
                rt.Release();
                Destroy(rt);
            }
        }
        #endregion

        private IEnumerator HunterPass()
        {
            PlayerMonster m = PlayerMonster.Local;
            Check("hunter body", m != null && Actor.LocalPlayer.Kind == ActorKind.PlayerMonster, m != null ? m.Def.Name : "none");
            if (m == null) yield break;
            MethodInfo strike = typeof(PlayerMonster).GetMethod("Strike", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo ability = typeof(PlayerMonster).GetMethod("Ability", BindingFlags.NonPublic | BindingFlags.Instance);
            Try("hunter strike", () => strike.Invoke(m, new object[] { 1f }));
            Try("hunter pounce", () => ability.Invoke(m, null));
            yield return new WaitForSeconds(2f);
            Check("hunter alive", !Actor.LocalPlayer.GetComponent<PlayerStats>().IsDead);
        }

        private IEnumerator Host()
        {
            Try("host", () => MainMenuController.HostAction(Port));
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null, 120f);
            Check("host hub", GameSession.IsHub);
            yield return Until(() => NetSession.Clients.Count >= 2, 420f);
            Check("client connected", NetSession.Clients.Count >= 2, NetSession.Clients.Count + " in session");
            GiveRaidKit();
            yield return Deploy(RaidKind.Operative);
            ulong client = ulong.MaxValue;
            foreach (NetClientInfo c in NetSession.Clients.Values) if (c.Id != NetSession.NM.LocalClientId) client = c.Id;
            yield return Until(() => NetSession.Clients.TryGetValue(client, out NetClientInfo ci) && ci.InRaid, 300f);
            Check("client followed into the raid", NetSession.Clients.TryGetValue(client, out NetClientInfo info) && info.InRaid);
            yield return Until(() => NetPlayers.Proxies.Count > 0, 60f);
            Check("host sees the client", NetPlayers.Proxies.Count > 0, NetPlayers.Proxies.Count + " proxies");
            Try("server explosion", () => NetExplosions.Detonate(Actor.LocalPlayer.transform.position + Vector3.forward * 30f, 4f, 100f, null, "smoke"));
            // Hold the raid open while the client runs its checks (it disconnects when done).
            yield return Until(() => NetSession.Clients.Count < 2, 150f);
            Check("client left cleanly", NetSession.Clients.Count < 2);
            NetSession.Stop();
        }

        private IEnumerator Client()
        {
            // With -clonez-smoke-join <address[:port]> it plays against a server already running (the in-house dedicated
            // server, say) rather than the host a paired run starts on this machine.
            string address = "127.0.0.1";
            ushort port = Port;
            string join = ArgAfter(Environment.GetCommandLineArgs(), "-clonez-smoke-join");
            if (join != null && !JoinAddress.TryParse(join, out address, out port)) Check("join address", false, join);
            // The host may still be starting up: keep knocking (a refused attempt drops back to the menu) until let in.
            yield return new WaitForSeconds(5f);
            float giveUp = Time.realtimeSinceStartup + 300f;
            int attempts = 0;
            while (Time.realtimeSinceStartup < giveUp && !NetSession.Active)
            {
                if (NetSession.Instance == null)
                {
                    attempts++;
                    MainMenuController.JoinAction(address, port);
                }
                yield return new WaitForSeconds(3f);
            }
            Check("join", NetSession.Active, attempts + " attempt(s) at " + address + ":" + port);
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null && NetSession.Active, 180f);
            Check("client hub", GameSession.IsHub && NetSession.Active);
            Try("squad screen (networked)", () => { HubScreens.Open(HubScreens.Squad); if (UIScreen.Current != null) UIScreen.Current.Close(); });
            // A dedicated server plays nobody: the deploy below opens the raid on it (or joins the one under way).
            bool dedicated = NetSession.ServerDedicated;
            if (!dedicated)
            {
                yield return Until(() => NetSession.HostRaidZone != null, 300f);
                Check("host raid open", NetSession.HostRaidZone != null, NetSession.HostRaidZone);
            }
            GiveRaidKit();
            yield return Deploy(RaidKind.Operative);
            if (dedicated) Check("server opened the raid", NetSession.HostRaidZone != null, NetSession.HostRaidZone + " on build " + NetSession.ServerBuild);
            else
            {
                yield return Until(() => NetPlayers.Proxies.Count > 0, 60f);
                Check("client sees the host", NetPlayers.Proxies.Count > 0, NetPlayers.Proxies.Count + " proxies");
            }
            yield return Until(() => FindObjectsByType<NetAgentProxy>().Length > 0, 60f);
            Check("AI replicated", FindObjectsByType<NetAgentProxy>().Length > 0, FindObjectsByType<NetAgentProxy>().Length + " agents");
            PlayerInventory inv = Actor.LocalPlayer.GetComponent<PlayerInventory>();
            Try("client grenade (server blast)", () => Explosives.Throw(inv));
            yield return new WaitForSeconds(5f);
            Try("client builds a wall", () =>
            {
                Vector3 at = Actor.LocalPlayer.transform.position + Actor.LocalPlayer.transform.forward * 5f;
                at.y = CloneZConfig.HouseWallHeight * 0.5f;
                BuildPiece p = BuildPiece.Spawn(null, BuildPieceType.Wall, at, 0f, 1f);
                p.Owner = ZoneCamps.MyTag;
                BuildPiece.RaiseRaised(p);
            });
            yield return new WaitForSeconds(5f);
            Check("still connected", NetSession.Active);
            NetSession.Stop();
            yield return new WaitForSeconds(2f);
        }
        #endregion

        private void Finish()
        {
            Debug.Log("[Smoke] ---- " + s_Mode + ": " + _checks.Count + " checks, " + _failed + " failed, " + s_Errors + " logged errors ----");
            foreach (string c in _checks) Debug.Log("[Smoke] " + c);
            foreach (string e in s_FirstErrors) Debug.Log("[Smoke] error: " + e);
            int code = _failed > 0 || s_Errors > 0 ? 2 : 0;
            Debug.Log("[Smoke] RESULT " + (code == 0 ? "PASS" : "FAIL") + " exit=" + code);
            s_Finished = true;
#if UNITY_EDITOR
            if (s_FromMenu)
            {
                UnityEditor.EditorApplication.isPlaying = false;   // back to edit mode, which tidies up (OnPlayModeChanged)
                return;
            }
#endif
            CleanUp();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }

        // Puts back what a run changed outside itself: its profile file, and for the waves its save file, the settings
        // its presses wrote and the input it took over.
        private static void CleanUp()
        {
            try { if (File.Exists(ProfileStore.FilePath)) File.Delete(ProfileStore.FilePath); } catch { }
            if (s_Mode == "wave") EndWave();
        }

#if UNITY_EDITOR
        // A menu run ends in edit mode, whether it finished or the Play button stopped it; edit mode comes after every
        // quit-save the run's world made. This editor goes on to play the game on its own profile, save and settings.
        private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange change)
        {
            // The input goes back while the run's settings object still exists: play mode's own objects go as it ends,
            // and the Input System then restores its settings into whichever object is current.
            if (change == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                RemoveInput();
                return;
            }
            if (change != UnityEditor.PlayModeStateChange.EnteredEditMode) return;
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            try
            {
                if (!s_Finished) Debug.Log("[Smoke] " + s_Mode + ": stopped before the end");
                CleanUp();
                SaveSystem.PathOverride = null;
                ProfileStore.PathOverride = null;
                typeof(ProfileStore).GetField("s_Account", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, null);   // the run's account
                GameSettings.Load();
                Application.logMessageReceived -= OnLog;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
                Application.runInBackground = s_RanInBackground;
                s_Mode = null;
                s_FromMenu = false;
                s_Pace = 0f;
            }
            finally
            {
                UnityEditor.EditorApplication.UnlockReloadAssemblies();
            }
        }
#endif
    }
}
