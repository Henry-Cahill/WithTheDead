using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// An automated smoke test, inert unless the game (or a batch-mode editor in play mode) is started with
    /// <c>-clonez-smoke solo|hunter|host|client</c>. It plays through the real flows — makes a character, walks into
    /// the bar, deploys into a Zone — and pokes the systems (a grenade, a drone, a car, a camp gate and a breaching
    /// charge, the tablet, cuffs, the economy; as a hunter, claws and a trick; as host and client, a networked raid),
    /// counting every error and exception the game logs. It uses its own profile file and never touches the
    /// player's. Results are logged as "[Smoke] ..." lines; the process exits with 0 (clean) or 2 (errors).
    /// </summary>
    public sealed class SmokeTest : MonoBehaviour
    {
        private const ushort Port = 7791;
        private static string s_Mode;
        private static int s_Errors;
        private static readonly List<string> s_FirstErrors = new List<string>();
        private readonly List<string> _checks = new List<string>();
        private int _failed;

        public static bool Running => s_Mode != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-clonez-smoke");
            if (i < 0 || i + 1 >= args.Length || s_Mode != null) return;
            s_Mode = args[i + 1].ToLowerInvariant();
            s_Errors = 0;
            s_FirstErrors.Clear();
            ProfileStore.PathOverride = "smoke_" + s_Mode + "_profile.json";
            try { if (File.Exists(ProfileStore.FilePath)) File.Delete(ProfileStore.FilePath); } catch { }
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            var go = new GameObject("[Smoke]");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>();
            Debug.Log("[Smoke] mode=" + s_Mode + " profile=" + ProfileStore.FilePath);
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            if (message.StartsWith("[Smoke]")) return;
            // The editor's own AI Assistant relay complains in batch mode, its search indexer throws on startup in this Unity,
            // and with -nographics its play-mode layout has no device to draw on; none of it is any part of the game.
            if (stack != null && (stack.Contains("com.unity.ai.assistant") || stack.Contains("UnityEditor.Search.")
                || stack.Contains("UnityEditor.EditorApplicationLayout"))) return;
            s_Errors++;
            if (s_FirstErrors.Count < 30) s_FirstErrors.Add(type + ": " + message + (string.IsNullOrEmpty(stack) ? "" : " @ " + FirstLine(stack)));
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
            MakeCharacter();
            switch (s_Mode)
            {
                case "host": yield return Host(); break;
                case "client": yield return Client(); break;
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
            yield return RunnerPass();
            if (KentuckyStreamer.Active != null) yield return StatewidePass();
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
            // The host may still be starting up: keep knocking (a refused attempt drops back to the menu) until let in.
            yield return new WaitForSeconds(5f);
            float giveUp = Time.realtimeSinceStartup + 300f;
            int attempts = 0;
            while (Time.realtimeSinceStartup < giveUp && !NetSession.Active)
            {
                if (NetSession.Instance == null)
                {
                    attempts++;
                    MainMenuController.JoinAction("127.0.0.1", Port);
                }
                yield return new WaitForSeconds(3f);
            }
            Check("join", NetSession.Active, attempts + " attempt(s)");
            yield return Until(() => GameSession.IsHub && Actor.LocalPlayer != null && NetSession.Active, 180f);
            Check("client hub", GameSession.IsHub && NetSession.Active);
            Try("squad screen (networked)", () => { HubScreens.Open(HubScreens.Squad); if (UIScreen.Current != null) UIScreen.Current.Close(); });
            yield return Until(() => NetSession.HostRaidZone != null, 300f);
            Check("host raid open", NetSession.HostRaidZone != null, NetSession.HostRaidZone);
            GiveRaidKit();
            yield return Deploy(RaidKind.Operative);
            yield return Until(() => NetPlayers.Proxies.Count > 0, 60f);
            Check("client sees the host", NetPlayers.Proxies.Count > 0, NetPlayers.Proxies.Count + " proxies");
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
            try { if (File.Exists(ProfileStore.FilePath)) File.Delete(ProfileStore.FilePath); } catch { }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
