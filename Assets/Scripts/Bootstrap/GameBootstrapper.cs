using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CloneZ
{
    /// <summary>
    /// Builds the entire playable world from code — ground, cover, lighting, the
    /// player, the isometric camera, a baked NavMesh, the HUD and the wave spawner.
    /// It auto-instantiates on Play via <see cref="RuntimeInitializeOnLoadMethod"/>,
    /// so you can just press Play in any (ideally empty) scene.
    ///
    /// To disable auto-boot, delete the [RuntimeInitializeOnLoadMethod] method
    /// below, or remove this component from the scene.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        private static bool s_hooked;
        private bool _built;
        private bool _worldBuilt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            EnsureExists();
            if (!s_hooked)
            {
                s_hooked = true;
                SceneManager.sceneLoaded += (scene, mode) => EnsureExists();
            }
        }

        private static void EnsureExists()
        {
            if (FindAnyObjectByType<GameBootstrapper>() != null) return;
            var go = new GameObject("[CloneZ]");
            go.AddComponent<GameBootstrapper>();
        }

        private void Start()
        {
            if (_built) return;
            _built = true;
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            Time.timeScale = 1f;
            EnsureEventSystem();
            GameModules.EnsureInit();
            ServerRules.FromSandbox(SandboxSettings.Current);
            WorldMarkers.Clear();
            HubRules.SafeZone = false;
            KettleLobby.Reset();
            CentralEconomy.ForgetWorld();

            // "Next Raid" / a deploy from the hub reloads the scene straight into a raid; a raid's end (or
            // "Return to hub") reloads straight into the hub.
            if (GameSession.AutoStartRaid)
            {
                GameSession.AutoStartRaid = false;
                StartRaid();
                return;
            }
            if (GameSession.AutoStartHub)
            {
                GameSession.AutoStartHub = false;
                StartHub();
                return;
            }
            ShowMainMenu();
        }

        // The title screen decides whether to start fresh or continue a saved run.
        private void ShowMainMenu()
        {
            LoadingScreen.Hide();
            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            menu.Bind(this);
        }

        /// <summary>Begins an extraction raid into <see cref="GameSession.ZoneId"/> as <see cref="GameSession.Raid"/>.
        /// Raids never touch the wave-survival save; the kit belongs to the active character.</summary>
        public void StartRaid()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            GameSession.Mode = GameMode.Extraction;
            try { BuildRaidWorld(); }
            finally { LoadingScreen.Hide(); }
        }

        /// <summary>Opens the hub (the bar between raids) for the active character: The Rusty Kettle standing in Bardstown
        /// with the town around it when it can (<see cref="KettleLobby"/>), else the hub on its own.</summary>
        public void StartHub()
        {
            if (_worldBuilt) return;
            if (ProfileStore.ActiveCharacter == null || ProfileStore.ActiveCharacter.Dead)
            {
                ShowMainMenu();
                return;
            }
            _worldBuilt = true;
            GameSession.Mode = GameMode.Hub;
            HubDef hub = HubCatalog.Get(ProfileStore.ActiveCharacter.Hub);
            if (KettleLobby.Wants(hub))
            {
                StartCoroutine(BuildLobbyWorld(hub));
                return;
            }
            try { BuildHubWorld(); }
            finally { LoadingScreen.Hide(); }
        }

        /// <summary>Begins a brand-new wave-survival run, discarding any existing save (called by the title screen).</summary>
        public void StartNewGame()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            GameSession.Mode = GameMode.WaveSurvival;
            GameSession.SurvivorName = SurvivorNames.Roll();
            WorldMarkers.Clear();
            SaveSystem.Delete();
            try { BuildWorld(false); }
            finally { LoadingScreen.Hide(); }
        }

        /// <summary>Resumes the saved wave-survival run (called by the title screen's Continue button).</summary>
        public void ContinueGame()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            GameSession.Mode = GameMode.WaveSurvival;
            try { BuildWorld(true); }
            finally { LoadingScreen.Hide(); }
        }

        // A raid into one of the Zones. The map's layout is the zone's own, fixed layout (the same streets every
        // time, so routes can be learned); the loot, the open exits, the anomalies and the threats are rolled
        // fresh from the raid's seed. The survivor arrives by helicopter at the chosen landing zone (or on foot at
        // the map's spawn), with whatever kit the character carries, and must reach an exit — before dawn,
        // unless it's an open-zone run.
        private void BuildRaidWorld()
        {
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            UIState.ActionMenuOpen = false;
            Time.timeScale = 1f;
            ZoneDef zone = ZoneCatalog.Current;
            Climate.Winter = zone.Winter;
            Shelter.Clear();
            WorldMarkers.Clear();
            StarterHome.SetActive(false);   // pieces built during a raid stay rough planking
            if (!GameSession.Networked || GameSession.RaidSeed == 0) GameSession.RaidSeed = Random.Range(1, int.MaxValue);
            var raidRng = new System.Random(GameSession.RaidSeed);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            if (TimeOfDay.Instance != null)
                TimeOfDay.Instance.Configure(zone.StartHour, CloneZConfig.RaidDayLengthSeconds,
                    CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay);
            EnsureEventSystem();

            var world = new GameObject("World").transform;
            CloneZConfig.ResetArena(CloneZConfig.ArenaHalfSize);
            RaidMapInfo map = null;
            if (!string.IsNullOrEmpty(zone.RealPlace))
            {
                // A real place, built from the world store on the NAS; if it can't be read, the procedural town instead.
                map = KentuckyMap.Build(world, zone, out string why);
                if (map == null) Debug.LogWarning("[CloneZ] " + zone.Name + " couldn't be built from the world store (" + why + "); building the procedural town instead.");
            }
            if (map == null) map = RaidMapBuilder.Build(world, zone.LayoutSeed);
            CloneZConfig.ArenaHalfSize = map.HalfSize;
            RaidWorldRules.RerollLoot(world, GameSession.RaidSeed);
            RaidWorldRules.RollExits(map, raidRng);
            WorldHooks.RaiseRaidMapBuilt(world, map, zone, new System.Random(GameSession.RaidSeed ^ 0x2F6E));
            BakeNavMesh(world);

            Vector3 spawn = map.PlayerSpawn;
            float yaw = map.PlayerSpawnYaw;
            bool heliInsertion = RaidWorldRules.PickInsertion(map, GameSession.InsertionPoint, ref spawn, ref yaw);

            PlayerStats player = BuildPlayer(spawn, yaw);
            gm.Player = player;
            if (!GameSession.IsMonsterRun)
                RaidLoadout.Apply(player.GetComponent<PlayerInventory>(), player.GetComponent<PlayerClothing>());

            BuildSurvivorViews(player);

            ZombieSpawner spawner = BuildSpawner(player.transform);
            spawner.Mode = SpawnerMode.Ambient;
            spawner.AmbientPopulation = Mathf.RoundToInt(CloneZConfig.RaidZombiePopulation * zone.ZombieDensity * SandboxSettings.Current.ZombiePopulation);
            spawner.enabled = NetAuthority.IsServer;

            ExtractionManager.Begin(player, map.Exits, CloneZConfig.RaidSpawnProtectionSeconds);
            if (heliInsertion) RaidWorldRules.PlayInsertion(player, spawn);

            CharacterProfile character = ProfileStore.ActiveCharacter;
            if (character != null)
            {
                character.Raids++;
                if (GameSession.IsScavRun) character.ScavRuns++;
            }
            WorldHooks.RaiseRaidStarted(world, map, zone);
            GameEvents.RaiseRaidStarted(zone.Id, GameSession.Raid);

            new GameObject("PauseMenu").AddComponent<PauseMenuController>();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // The hub: the bar, its people and its counters. A safe place: no fighting, no dead, needs paused.
        private void BuildHubWorld()
        {
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            UIState.ActionMenuOpen = false;
            Time.timeScale = 1f;
            Climate.Winter = true;
            Shelter.Clear();
            WorldMarkers.Clear();
            StarterHome.SetActive(false);
            HubRules.SafeZone = true;
            CloneZConfig.ResetArena(16f);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.Configure(20f, CloneZConfig.RaidDayLengthSeconds * 4f, CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay);
            sun.intensity = 0.05f;
            EnsureEventSystem();

            CharacterProfile c = ProfileStore.ActiveCharacter;
            HubDef hub = HubCatalog.Get(c != null ? c.Hub : HubCatalog.Kettle);
            var world = new GameObject("World").transform;
            HubWorld.Result built = HubWorld.Build(world, hub);
            BakeNavMesh(world);

            PlayerStats player = BuildPlayer(built.PlayerSpawn, built.PlayerYaw);
            gm.Player = player;
            RaidLoadout.Apply(player.GetComponent<PlayerInventory>(), player.GetComponent<PlayerClothing>());
            BuildSurvivorViews(player);

            WorldHooks.RaiseHubBuilt(world);
            new GameObject("PauseMenu").AddComponent<PauseMenuController>();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            GameEvents.RaiseToast("Welcome back to " + hub.Name + ". Day " + (c != null ? c.Day : 1) + ".");
        }

        #region The lobby in the town
        // The Kettle standing in Bardstown (KettleLobby): the statewide map is built around the bar a step at a time
        // under the loading card, the survivor starts inside by the door, and walking out of it starts the raid where
        // they stand (BeginLobbyRaid): no reload. Anything that goes wrong falls back to the hub on its own.
        private Transform _lobbyWorld;
        private RaidMapInfo _lobbyMap;
        private ZoneDef _lobbyZone;
        private GameManager _lobbyGm;
        private HUDController _hud;
        private GameObject _pauseMenu;

        // Seconds a frame may spend building the town under the loading card (which still animates in between).
        private const float LobbyFrameBudget = 0.045f;

        private IEnumerator BuildLobbyWorld(HubDef hub)
        {
            if (!LoadingScreen.Showing) LoadingScreen.Show(hub.Name.ToUpperInvariant(), KettleLobby.Where(hub));
            LoadingScreen.SetProgress(0f, "Reading the map");
            UIState.GameplayBlocked = true;
            yield return null;
            yield return null;

            ZoneDef zone = ZoneCatalog.Get(KettleLobby.ZoneId);
            // Nothing is worth drawing until the town is up: the camera draws nothing meanwhile.
            Camera cam = Camera.main;
            int mask = cam != null ? cam.cullingMask : ~0;
            if (cam != null) cam.cullingMask = 0;

            string fail = null;
            KentuckyMap.MapBuildJob job = null;
            Transform world = null;
            try
            {
                world = BeginLobby(zone);
                KentuckyMap.WantLot = KettleLobby.LotSize;
                job = KentuckyMap.BeginBuild(world, zone);
            }
            catch (System.Exception e) { fail = e.ToString(); }

            while (fail == null)
            {
                float until = Time.realtimeSinceStartup + LobbyFrameBudget;
                bool more = true;
                while (more && Time.realtimeSinceStartup < until) more = job.Step();
                LoadingScreen.SetProgress(job.Progress * 0.9f, job.Stage);
                if (!string.IsNullOrEmpty(KentuckyMap.LotWhy)) fail = KentuckyMap.LotWhy;
                else if (!more)
                {
                    if (job.Info == null) fail = job.Why;
                    else if (KentuckyMap.ReservedLot == null) fail = "no lot was kept for the bar";
                    break;
                }
                else yield return null;
            }
            if (fail == null)
            {
                // In the same frame the town finished: the streamer's own update would bake the NavMesh without the bar.
                LoadingScreen.SetProgress(0.95f, "Opening the Kettle");
                try { FinishLobby(hub, zone, world, job.Info, cam, mask); }
                catch (System.Exception e) { fail = e.ToString(); }
            }
            if (fail != null)
            {
                Debug.LogWarning("[CloneZ] The Kettle couldn't stand in " + zone.Name + " (" + fail + "); the hub is built on its own instead.");
                KettleLobby.Disabled = true;
                KentuckyMap.ForgetLot();
                if (cam != null) cam.cullingMask = mask;
                GameFlow.GoToHub();
                yield break;
            }
            LoadingScreen.SetProgress(1f, "Open");
            UIState.GameplayBlocked = false;
            LoadingScreen.Hide();
        }

        // The session, the light and the clock for the lobby: the hub's rules over the zone's town.
        private Transform BeginLobby(ZoneDef zone)
        {
            UIState.BuildMode = false;
            UIState.ActionMenuOpen = false;
            Time.timeScale = 1f;
            Climate.Winter = zone.Winter;
            Shelter.Clear();
            WorldMarkers.Clear();
            StarterHome.SetActive(false);
            HubRules.SafeZone = true;
            GameSession.Mode = GameMode.Hub;
            GameSession.HubInRaidTown = true;   // the town round the bar is the raid's: its loot is rolled as the raid's
            GameSession.ZoneId = zone.Id;
            GameSession.Raid = RaidKind.Operative;
            GameSession.InsertionPoint = RaidWorldRules.OnFoot;
            GameSession.RaidSeed = Random.Range(1, int.MaxValue);

            _lobbyGm = new GameObject("GameManager").AddComponent<GameManager>();
            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            // The clock waits just short of the zone's hour, barely moving while the survivor is inside; it keeps raid
            // pace from the moment they step out (the dawn deadline is counted from then).
            if (TimeOfDay.Instance != null)
                TimeOfDay.Instance.Configure(zone.StartHour - 0.25f, CloneZConfig.RaidDayLengthSeconds * 8f, CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay);
            EnsureEventSystem();

            var world = new GameObject("World").transform;
            CloneZConfig.ResetArena(CloneZConfig.ArenaHalfSize);
            // A fresh hub: whatever a raid left running stops now, before this town's overlays go up.
            WorldHooks.RaiseHubBuilt(world);
            _lobbyWorld = world;
            _lobbyZone = zone;
            return world;
        }

        // Everything after the town is up: the raid's rolls, the bar on its lot, the zone's overlays round it, the
        // NavMesh, and the survivor inside by the door.
        private void FinishLobby(HubDef hub, ZoneDef zone, Transform world, RaidMapInfo map, Camera cam, int mask)
        {
            _lobbyMap = map;
            CloneZConfig.ArenaHalfSize = map.HalfSize;
            RaidWorldRules.RerollLoot(world, GameSession.RaidSeed);
            RaidWorldRules.RollExits(map, new System.Random(GameSession.RaidSeed));
            HubWorld.Result bar = HubWorld.BuildInTown(world, hub, KentuckyMap.ReservedLot);
            // The bar stands: known from here on (the zone's overlays and hazards keep out of it as they're laid out).
            KettleLobby.Opened(bar.Root, BeginLobbyRaid);
            // The raid starts at the bar's door, not where the map would have set the survivor down: whatever keeps clear of
            // the start (anomalies, radiation, landmarks, outposts, trees) keeps clear of the porch.
            map.PlayerSpawn = bar.Root.TransformPoint(new Vector3(0f, 0f, -HubWorld.HalfSize.y - 2.5f));
            map.PlayerSpawnYaw = bar.Root.eulerAngles.y + 180f;
            WorldHooks.RaiseRaidMapBuilt(world, map, zone, new System.Random(GameSession.RaidSeed ^ 0x2F6E));
            BakeNavMesh(world);

            PlayerStats player = BuildPlayer(bar.PlayerSpawn, bar.PlayerYaw);
            _lobbyGm.Player = player;
            RaidLoadout.Apply(player.GetComponent<PlayerInventory>(), player.GetComponent<PlayerClothing>());
            if (cam != null) cam.cullingMask = mask;
            BuildSurvivorViews(player);
            _pauseMenu = new GameObject("PauseMenu");
            _pauseMenu.AddComponent<PauseMenuController>();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            CharacterProfile c = ProfileStore.ActiveCharacter;
            GameEvents.RaiseToast("Welcome back to " + hub.Name + ", Bardstown. Day " + (c != null ? c.Day : 1) + ". The front door opens onto the town.");
            Debug.Log("[CloneZ] The Kettle stands in " + zone.RealPlace + " (" + map.Exits.Count + " exits rolled); walk out of its door to start the raid.");
        }

        /// <summary>The survivor has walked out of the Kettle's door: the raid starts where they stand. The zone's rules
        /// switch on, the clock keeps raid pace, the dead start to walk, the exits light up, and the bar shuts behind them.</summary>
        private void BeginLobbyRaid()
        {
            if (!GameSession.IsHub || _lobbyMap == null || _lobbyGm == null || _lobbyGm.Player == null) return;
            PlayerStats player = _lobbyGm.Player;
            ZoneDef zone = _lobbyZone;

            if (UIScreen.Current != null) UIScreen.Current.Close();
            HubKit.AfterChange();   // the kit that walks out of the door is the kit at risk (committed while still the hub's)
            GameSession.Mode = GameMode.Extraction;
            GameSession.HubInRaidTown = false;
            GameSession.Raid = RaidKind.Operative;
            HubRules.SafeZone = false;
            if (TimeOfDay.Instance != null)
                TimeOfDay.Instance.Configure(TimeOfDay.Instance.Hour, CloneZConfig.RaidDayLengthSeconds, CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay);

            // The HUD and the pause menu were made for the hub (the hub's quit keeps the kit); the raid's differ.
            if (_hud != null) Destroy(_hud.gameObject);
            BuildHUD(player, player.GetComponent<PlayerInventory>(), player.GetComponent<PlayerInteractor>(), player.GetComponent<PlayerCombat>(),
                player.GetComponent<PlayerSleep>(), player.GetComponent<PlayerSkills>(), player.GetComponent<PlayerBuilder>(), player.GetComponent<PlayerWounds>(),
                player.GetComponent<PlayerPanic>(), player.GetComponent<PlayerFoodSickness>(), player.GetComponent<PlayerFarming>(), player.GetComponent<PlayerHydration>());
            if (_pauseMenu != null) Destroy(_pauseMenu);
            _pauseMenu = new GameObject("PauseMenu");
            _pauseMenu.AddComponent<PauseMenuController>();

            ZombieSpawner spawner = BuildSpawner(player.transform);
            spawner.Mode = SpawnerMode.Ambient;
            spawner.AmbientPopulation = Mathf.RoundToInt(CloneZConfig.RaidZombiePopulation * zone.ZombieDensity * SandboxSettings.Current.ZombiePopulation);
            spawner.enabled = NetAuthority.IsServer;

            ExtractionManager.Begin(player, _lobbyMap.Exits, CloneZConfig.RaidSpawnProtectionSeconds);
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null) c.Raids++;
            KettleLobby.CloseUp();
            WorldHooks.RaiseRaidStarted(_lobbyWorld, _lobbyMap, zone);
            GameEvents.RaiseRaidStarted(zone.Id, GameSession.Raid);
            ProfileStore.Save();

            TimeOfDay tod = TimeOfDay.Instance;
            string clock = tod != null ? tod.Clock12Text + " " + tod.AmPm + "  \u00b7  " : "";
            KettleLobby.ShowArrival(zone.RealPlace.ToUpperInvariant() + ", KENTUCKY", clock + "Reach an exit before dawn");
            Debug.Log("[CloneZ] The raid begins at the Kettle's door: " + zone.Name + ", " + _lobbyMap.Exits.Count + " exits.");
        }
        #endregion

        private void BuildWorld(bool loadSave)
        {
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            UIState.ActionMenuOpen = false;
            Time.timeScale = 1f;
            Climate.Winter = false;
            Shelter.Clear();
            StarterHome.SetActive(true);    // before any house piece is raised or restored
            CloneZConfig.ResetArena(CloneZConfig.WaveArenaHalfSize);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            EnsureEventSystem();

            // Continue from a saved run when asked (and one exists); otherwise start fresh.
            SaveData save = null;
            bool loaded = loadSave && SaveSystem.TryLoad(out save) && save.Player != null;
            if (loaded && !string.IsNullOrEmpty(save.SurvivorName)) GameSession.SurvivorName = save.SurvivorName;

            var world = BuildArena();
            if (loaded)
            {
                // Restore the saved world (clock/weather/utilities/score + every dynamic object)
                // before the bake, so restored world containers carve the navmesh as they did before.
                SaveSystem.ApplyWorld(save, world);
                // A save written before furniture became demolishable has no stored props — rebuild
                // the default bed, stove and cover so the base isn't left empty.
                if (!save.HasWorldProps) BuildDefaultProps(world);
                // A save from before the home was furnished: move its fittings to their places in the
                // finished home and furnish it around whatever the run kept.
                if (!save.HasFurniture)
                {
                    StarterHome.MigrateOldLayout();
                    StarterHome.Furnish(world);
                }
                // A save from before the fixtures were interactive: make room for the bathroom and the
                // fridge, raise the bathroom's partition, then add the furniture that came with them.
                if (!save.HasFixtures)
                {
                    StarterHome.MigrateToFixtureLayout(world);
                    StarterHome.Furnish(world);
                }
            }
            else
            {
                BuildDefaultProps(world);
                BuildFarm(world);
                BuildWaterSources(world);
                StarterHome.Furnish(world);
                // World loot containers are no longer scattered across the arena — the only
                // container in a fresh run is the starter house's own supply crate.
            }
            BakeNavMesh(world);

            // The street and the yard are cosmetic geometry, kept out of the navmesh (this bake and any
            // later rebuild), so they never affect pathing.
            StarterHome.BuildStreet(world);
            StarterHome.DressGrounds(world);

            // Every fixture now stands (the yard's too), so pour the saved states back in: doors, windows,
            // taps, lamps, switches, and what's kept in the cupboards and drawers.
            if (loaded) SaveSystem.ApplyFixtures(save);

            PlayerStats player = BuildPlayer(PickPlayerSpawn(), 0f);
            gm.Player = player;

            if (loaded)
            {
                SaveSystem.ApplyPlayer(save, player);
            }
            else
            {
                // Dress first: the starting backpack adds the grid section the kit then packs into.
                EquipStartingClothes(player.GetComponent<PlayerClothing>());
                GiveStartingItems(player.GetComponent<PlayerInventory>());
            }

            BuildSurvivorViews(player);
            BuildSpawner(player.transform);
            WorldHooks.RaiseArenaBuilt(world);

            // The pause menu (Esc) freezes the world and offers save / quit-to-menu.
            new GameObject("PauseMenu").AddComponent<PauseMenuController>();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Everything that shows the survivor and the world to the player, shared by both modes: the
        // camera, the inventory / skills / health panels, the HUD and the vision rig (line-of-sight
        // darkness, hidden zombies, night vision, flashlight, snow).
        private void BuildSurvivorViews(PlayerStats player)
        {
            var inventory = player.GetComponent<PlayerInventory>();
            var interactor = player.GetComponent<PlayerInteractor>();
            var combat = player.GetComponent<PlayerCombat>();
            var sleep = player.GetComponent<PlayerSleep>();
            var skills = player.GetComponent<PlayerSkills>();
            var builder = player.GetComponent<PlayerBuilder>();
            var wounds = player.GetComponent<PlayerWounds>();
            var panic = player.GetComponent<PlayerPanic>();
            var clothing = player.GetComponent<PlayerClothing>();
            var temperature = player.GetComponent<PlayerTemperature>();
            var nutrition = player.GetComponent<PlayerFoodSickness>();
            var farming = player.GetComponent<PlayerFarming>();
            var hydration = player.GetComponent<PlayerHydration>();

            SetupCamera(player.transform);

            var invUi = new GameObject("InventoryUI").AddComponent<InventoryUIController>();
            invUi.Bind(inventory);
            if (interactor != null) interactor.Bind(invUi);

            var skillsUi = new GameObject("SkillsUI").AddComponent<SkillsPanelController>();
            skillsUi.Bind(skills);

            var healthUi = new GameObject("HealthUI").AddComponent<HealthPanelController>();
            healthUi.Bind(wounds, player, inventory, panic, temperature, clothing, nutrition);

            BuildHUD(player, inventory, interactor, combat, sleep, skills, builder, wounds, panic, nutrition, farming, hydration);

            VisionRig.Install(Camera.main, player);
        }

        #region Environment
        private Light SetupEnvironment()
        {
            Light sun = FindAnyObjectByType<Light>();
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.95f, 0.85f);
                sun.intensity = 1.25f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.42f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.09f, 0.10f, 0.13f);
            RenderSettings.fogStartDistance = 35f;
            RenderSettings.fogEndDistance = 75f;
            return sun;
        }

        // The day/night clock and weather live on their own manager object; the
        // TimeOfDay component swings the sun we just created.
        private void SetupTimeAndWeather(Light sun)
        {
            var env = new GameObject("Environment");
            var tod = env.AddComponent<TimeOfDay>();
            tod.Sun = sun;
            env.AddComponent<WeatherSystem>();
            env.AddComponent<WaterMains>();      // schedules the water shut-off a few days in
            env.AddComponent<PowerGrid>();       // .. and the power cut a little later
        }

        // The default base for a fresh run (or a save too old to have persisted its props): the starter
        // home first, so the bed and stove stand on its floor.
        private void BuildDefaultProps(Transform world)
        {
            StarterHome.Raise(world);
            BuildBed(world);
            BuildStove(world);
        }

        // A bed in the bedroom corner where the player can safely sleep off the night.
        private void BuildBed(Transform world)
        {
            SleepSpot.Spawn(world, StarterHome.BedSpot);
        }

        // A stove at the end of the kitchen run along the front wall, beside the sink.
        private void BuildStove(Transform world)
        {
            CookingStation.Spawn(world, StarterHome.StoveSpot);
        }

        // A small garden just west of the spawn. One plot is already sown with potatoes so the
        // grow/water/harvest cycle is visible; the rest are bare, ready for the player's own seeds.
        private void BuildFarm(Transform world)
        {
            var sown = FarmPlot.Spawn(world, new Vector3(-3f, 0f, 4.5f));
            sown.Plant(FarmCrop.Get("potato"));
            FarmPlot.Spawn(world, new Vector3(-4.6f, 0f, 4.5f));
            FarmPlot.Spawn(world, new Vector3(-3f, 0f, 6f));
        }

        // The kitchen sink (a plumbed tap) sits inside the house under the front window, beside the
        // stove; the rain barrel stays outside by the west wall to catch the rain. The tap runs clean
        // until the mains are shut off a few days in; after that the barrel's collected rain — tainted,
        // so boil it — is the renewable water supply.
        private void BuildWaterSources(Transform world)
        {
            WaterSource.Spawn(world, StarterHome.SinkSpot, WaterSourceKind.Tap);
            WaterSource.Spawn(world, StarterHome.BarrelSpot, WaterSourceKind.RainBarrel);
        }


        private Transform BuildArena()
        {
            var world = new GameObject("World").transform;

            // Ground
            float groundSize = (CloneZConfig.ArenaHalfSize + 6f) * 2f;
            var ground = CloneZUtil.Box("Ground", new Vector3(0f, -0.5f, 0f),
                new Vector3(groundSize, 1f, groundSize), new Color(0.16f, 0.17f, 0.16f), world);

            // Perimeter walls (keep the horde in, block sight lines).
            float half = CloneZConfig.ArenaHalfSize + 1f;
            var wallColor = new Color(0.22f, 0.22f, 0.24f);
            CloneZUtil.Box("Wall_N", new Vector3(0f, 1.5f, half), new Vector3(half * 2f, 3f, 1f), wallColor, world);
            CloneZUtil.Box("Wall_S", new Vector3(0f, 1.5f, -half), new Vector3(half * 2f, 3f, 1f), wallColor, world);
            CloneZUtil.Box("Wall_E", new Vector3(half, 1.5f, 0f), new Vector3(1f, 3f, half * 2f), wallColor, world);
            CloneZUtil.Box("Wall_W", new Vector3(-half, 1.5f, 0f), new Vector3(1f, 3f, half * 2f), wallColor, world);

            return world;
        }

        private void BakeNavMesh(Transform world)
        {
            // The whole of Kentucky can't be baked at once: its streamer bakes the start now and the state around the
            // survivor as they travel.
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            if (streamer != null && streamer.World == world)
            {
                streamer.BakeNow();
                return;
            }
            // Bake the whole arena so the player and the horde can move (and zombies can spawn)
            // anywhere inside the perimeter walls. The surface lives on the World root and collects
            // only its children, so a runtime rebuild (after demolishing a baked-in obstacle) ignores
            // the root-level player and zombies. Built before they exist, so the first bake sees only
            // static geometry.
            var surface = world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
            WorldNavMesh.Register(surface);
        }
        #endregion

        #region Player & camera
        private PlayerStats BuildPlayer(Vector3 position, float yaw)
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var cc = root.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.4f;

            root.AddComponent<PlayerStats>();
            root.AddComponent<PlayerSkills>();
            root.AddComponent<PlayerWounds>();
            root.AddComponent<PlayerPanic>();
            root.AddComponent<PlayerClothing>();       // worn gear: insulation + bite/scratch defence
            root.AddComponent<PlayerTemperature>();     // body warmth & wetness (reads clothing + weather)
            root.AddComponent<PlayerFoodSickness>();     // food poisoning / nausea (raised by eating spoiled food)
            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerCombat>();
            root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerInteractor>();
            root.AddComponent<PlayerSleep>();
            root.AddComponent<PlayerSeat>();          // sitting down on a chair, the armchair, the toilet lid
            root.AddComponent<PlayerBuilder>();
            root.AddComponent<PlayerFarming>();       // till / sow / water / harvest crops (F)
            root.AddComponent<PlayerHydration>();     // drink / fill bottles at a tap or barrel (G)
            root.AddComponent<PlayerGear>();          // night vision (N), flashlight (L), gas mask + filters
            root.AddComponent<PlayerAvatar>();        // the survivor's body: a rigged humanoid animated from all of the above
            root.AddComponent<PlayerModifiers>();     // the sum of traits, artifacts, Zone marks, broken limbs...
            root.AddComponent<TraitEffects>();        // the character's chosen traits
            root.AddComponent<PlayerLimbs>();         // seven hit zones, fractures, blacked-out limbs, armour
            root.AddComponent<PlayerBlood>();         // blood volume, shock, unconsciousness, blood type
            root.AddComponent<Stealth>();             // how visible the survivor is (light, stance, movement)
            root.AddComponent<GearEffects>();         // armour weight, exo-frame carry

            CharacterProfile character = ProfileStore.ActiveCharacter;
            Actor actor = root.AddComponent<Actor>();
            actor.Height = 1.8f;
            actor.DisplayName = character != null ? character.Name : GameSession.SurvivorName;
            actor.Faction = GameSession.IsScavRun ? Factions.Scavs : (character != null ? character.Faction : Factions.Freehold);
            actor.NetId = Actor.AllocateNetId();
            actor.MakeLocalPlayer();
            if (character != null) GameSession.SurvivorName = character.Name;

            PlayerStats stats = root.GetComponent<PlayerStats>();
            WorldHooks.RaisePlayerBuilt(stats);
            GameEvents.RaiseLocalPlayerSpawned(stats);
            return stats;
        }

        // Picks a random clear spot anywhere in the arena for the player to start a new game.
        // Spots blocked by a wall, house piece or the crate are rejected (tested at torso height,
        // so the flat ground, floors and road don't count); colliderless props are ignored. On a
        // continued save this is overwritten by the restored position, so it only affects new games.
        private Vector3 PickPlayerSpawn()
        {
            const float y = 0.6f;
            float limit = CloneZConfig.ArenaHalfSize - 3f;   // stay clear of the perimeter walls
            for (int i = 0; i < 40; i++)
            {
                float x = UnityEngine.Random.Range(-limit, limit);
                float z = UnityEngine.Random.Range(-limit, limit);
                if (!Physics.CheckSphere(new Vector3(x, 1f, z), 0.6f))
                    return new Vector3(x, y, z);
            }
            return new Vector3(0f, y, 0f);                   // fallback: the original street spawn
        }

        private void SetupCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
            cam.farClipPlane = 400f;
            cam.fieldOfView = 55f;

            var follow = cam.GetComponent<IsoCameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<IsoCameraFollow>();
            follow.Target = target;
            follow.Snap();

            EnsureSingleAudioListener(cam.gameObject);
        }

        private static void EnsureSingleAudioListener(GameObject keep)
        {
            if (keep.GetComponent<AudioListener>() == null) keep.AddComponent<AudioListener>();
            foreach (var listener in FindObjectsByType<AudioListener>())
                if (listener.gameObject != keep) listener.enabled = false;
        }
        #endregion

        #region UI & spawner
        private void BuildHUD(PlayerStats player, PlayerInventory inventory, PlayerInteractor interactor, PlayerCombat combat, PlayerSleep sleep, PlayerSkills skills, PlayerBuilder builder, PlayerWounds wounds, PlayerPanic panic, PlayerFoodSickness nutrition, PlayerFarming farming, PlayerHydration hydration)
        {
            var hud = new GameObject("HUD").AddComponent<HUDController>();
            hud.Bind(player, inventory, interactor, combat, sleep, skills, builder, wounds, panic, nutrition, farming, hydration);
            _hud = hud;
        }

        private ZombieSpawner BuildSpawner(Transform player)
        {
            var spawner = new GameObject("ZombieSpawner").AddComponent<ZombieSpawner>();
            spawner.Player = player;
            spawner.StartWave = GameManager.Instance != null ? GameManager.Instance.Wave : 0;
            return spawner;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            // Build the object inactive so the input module adopts its default UI
            // actions before OnEnable runs (required under the new Input System).
            var es = new GameObject("EventSystem");
            es.SetActive(false);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
            es.SetActive(true);
        }

        private void GiveStartingItems(PlayerInventory inventory)
        {
            if (inventory == null) return;
            inventory.AddStarter("waterbottle", 1);
            inventory.AddStarter("emptybottle", 2);   // fill these at the tap or the rain barrel
            inventory.AddStarter("bandage", 2);
            inventory.AddStarter("rag", 2);
            inventory.AddStarter("disinfectant", 1);
            inventory.AddStarter("pills", 1);
            inventory.AddStarter("betablockers", 1);      // one dose to steady the nerves in a pinch
            inventory.AddStarter("cannedbeans", 1);
            inventory.AddStarter("rawmeat", 2);           // raw — cook it at the stove by the bed
            inventory.AddStarter("apple", 2);             // perishable; eat it before it spoils
            inventory.AddStarter("kitchenknife", 1);
            inventory.AddStarter("canopener", 1);         // open tins cleanly, without blunting the knife
            inventory.AddStarter("ducttape", 2);          // patch a worn weapon or tool back up
            inventory.AddStarter("needle", 1);            // with thread, mend holes worn into clothing
            inventory.AddStarter("thread", 2);
            inventory.AddStarter("pistol", 1);
            inventory.AddStarter("ammo9mm", 30);
            inventory.AddStarter("hammer", 1);        // tool for barricades and house building (B)
            inventory.AddStarter("plank", 12);        // enough to frame a small shelter; loot more to expand
            inventory.AddStarter("nails", 8);
            inventory.AddStarter("trowel", 1);        // break ground and pull ripe crops
            inventory.AddStarter("wateringcan", 1);   // keep the plots watered
            inventory.AddStarter("potatoseeds", 3);   // sow the bare garden plots
            inventory.AddStarter("carrotseeds", 2);
            inventory.AddStarter("tomatoseeds", 2);
            inventory.Equip(ItemDatabase.Get("pistol"));   // start armed; chambers a magazine from the pack
            // A spare hoodie in the pack for when the nights turn cold.
            inventory.AddStarter("hoodie", 1);
        }

        // Dresses the survivor in the starting outfit — a teal T-shirt, jeans and leather boots — with a
        // hiking bag for the building kit. It's enough for a mild day but no match for a cold night
        // or a downpour; better clothing has to be looted.
        private void EquipStartingClothes(PlayerClothing clothing)
        {
            if (clothing == null) return;
            clothing.Equip(ItemDatabase.Get("tshirt"));
            clothing.Equip(ItemDatabase.Get("jeans"));
            clothing.Equip(ItemDatabase.Get("boots"));
            clothing.Equip(ItemDatabase.Get("hikingbag"));
        }
        #endregion
    }
}
