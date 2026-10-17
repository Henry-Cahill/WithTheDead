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

            // "Next Raid" from a raid summary reloads the scene straight into a fresh raid.
            if (GameSession.AutoStartRaid)
            {
                GameSession.AutoStartRaid = false;
                StartRaid();
                return;
            }
            ShowMainMenu();
        }

        // The title screen decides whether to start fresh or continue a saved run.
        private void ShowMainMenu()
        {
            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            menu.Bind(this);
        }

        /// <summary>Begins an extraction raid — the default mode (called by the title screen). Raids
        /// never touch the wave-survival save; the kit carried out of a raid persists on its own.</summary>
        public void StartRaid()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            GameSession.Mode = GameMode.Extraction;
            BuildRaidWorld();
        }

        /// <summary>Begins a brand-new wave-survival run, discarding any existing save (called by the title screen).</summary>
        public void StartNewGame()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            GameSession.Mode = GameMode.WaveSurvival;
            SaveSystem.Delete();
            BuildWorld(false);
        }

        /// <summary>Resumes the saved wave-survival run (called by the title screen's Continue button).</summary>
        public void ContinueGame()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            GameSession.Mode = GameMode.WaveSurvival;
            BuildWorld(true);
        }

        // A raid: a winter night in a run-down town. The survivor arrives under spawn protection with
        // whatever kit they last extracted with, loots what they can and must reach an exit before dawn.
        private void BuildRaidWorld()
        {
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            Time.timeScale = 1f;
            Climate.Winter = true;
            Shelter.Clear();
            GameSession.RaidSeed = Random.Range(1, int.MaxValue);

            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            if (TimeOfDay.Instance != null)
                TimeOfDay.Instance.Configure(CloneZConfig.RaidStartHour, CloneZConfig.RaidDayLengthSeconds,
                    CloneZConfig.RaidStartMonth, CloneZConfig.RaidStartDay);
            EnsureEventSystem();

            var world = new GameObject("World").transform;
            RaidMapInfo map = RaidMapBuilder.Build(world, GameSession.RaidSeed);
            CloneZConfig.ArenaHalfSize = map.HalfSize;
            BakeNavMesh(world);

            PlayerStats player = BuildPlayer(map.PlayerSpawn, map.PlayerSpawnYaw);
            gm.Player = player;
            RaidLoadout.Apply(player.GetComponent<PlayerInventory>(), player.GetComponent<PlayerClothing>());

            BuildSurvivorViews(player);

            ZombieSpawner spawner = BuildSpawner(player.transform);
            spawner.Mode = SpawnerMode.Ambient;
            spawner.AmbientPopulation = CloneZConfig.RaidZombiePopulation;

            ExtractionManager.Begin(player, map.Exits, CloneZConfig.RaidSpawnProtectionSeconds);

            new GameObject("PauseMenu").AddComponent<PauseMenuController>();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void BuildWorld(bool loadSave)
        {
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            Time.timeScale = 1f;
            Climate.Winter = false;
            Shelter.Clear();
            CloneZConfig.ArenaHalfSize = CloneZConfig.WaveArenaHalfSize;

            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            EnsureEventSystem();

            // Continue from a saved run when asked (and one exists); otherwise start fresh.
            SaveData save = null;
            bool loaded = loadSave && SaveSystem.TryLoad(out save) && save.Player != null;

            var world = BuildArena();
            if (loaded)
            {
                // Restore the saved world (clock/weather/utilities/score + every dynamic object)
                // before the bake, so restored world containers carve the navmesh as they did before.
                SaveSystem.ApplyWorld(save, world);
                // A save written before furniture became demolishable has no stored props — rebuild
                // the default bed, stove and cover so the base isn't left empty.
                if (!save.HasWorldProps) BuildDefaultProps(world);
            }
            else
            {
                BuildDefaultProps(world);
                BuildFarm(world);
                BuildWaterSources(world);
                // World loot containers are no longer scattered across the arena — the only
                // container in a fresh run is the starter house's own supply crate.
            }
            BakeNavMesh(world);

            // The street is flat cosmetic geometry, laid down after the bake so it never affects pathing.
            BuildStreet(world);

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
        }

        // The default base furniture for a fresh run (or a save too old to have persisted it).
        private void BuildDefaultProps(Transform world)
        {
            BuildBed(world);
            BuildStove(world);
            BuildStarterHouse(world);
        }

        // A bed in the living room where the player can safely sleep off the night.
        private void BuildBed(Transform world)
        {
            SleepSpot.Spawn(world, new Vector3(5f, 0f, 5f));
        }

        // A stove in the kitchen, set back along the west side so it isn't crammed against the sink.
        private void BuildStove(Transform world)
        {
            CookingStation.Spawn(world, new Vector3(2f, 0f, 5.5f));
        }

        // A starter house on the north side of the street: an open-topped (roofless, so the iso
        // camera sees straight in) two-room shelter raised from the very same grid pieces the survivor
        // can build by hand (see PlayerBuilder). The living room holds the bed and stove; an interior
        // doorway leads to a back store room with a supply crate, and windows ring the walls. Built
        // before the navmesh bake so its walls carve it (zombies have to batter them down), and
        // captured by the save system like any built structure, so it persists and can be demolished.
        private void BuildStarterHouse(Transform world)
        {
            // If a save has already restored this house (or the player's edits to it), its pieces
            // are present by now — don't raise a duplicate on top of them.
            if (BuildPiece.All.Count > 0) return;

            // A 3-wide by 3-deep floor on the 2 m grid (x[1,7] x z[3,9]), left open to the sky.
            float[] cols = { 2f, 4f, 6f };        // tile-centre X
            float[] rows = { 4f, 6f, 8f };        // tile-centre Z

            foreach (float x in cols)
                foreach (float z in rows)
                    PlaceHousePiece(world, BuildPieceType.Floor, x, z, 0f);

            // South wall (z = 3) faces the street: a centred doorway flanked by windows.
            PlaceHousePiece(world, BuildPieceType.Window, 2f, 3f, 0f);
            PlaceHousePiece(world, BuildPieceType.Doorway, 4f, 3f, 0f);
            PlaceHousePiece(world, BuildPieceType.Window, 6f, 3f, 0f);

            // North wall (z = 9), with a window in the middle.
            PlaceHousePiece(world, BuildPieceType.Wall, 2f, 9f, 0f);
            PlaceHousePiece(world, BuildPieceType.Window, 4f, 9f, 0f);
            PlaceHousePiece(world, BuildPieceType.Wall, 6f, 9f, 0f);

            // West wall (x = 1) and east wall (x = 7) run north–south (yaw 90), each with a window.
            PlaceHousePiece(world, BuildPieceType.Wall, 1f, 4f, 90f);
            PlaceHousePiece(world, BuildPieceType.Window, 1f, 6f, 90f);
            PlaceHousePiece(world, BuildPieceType.Wall, 1f, 8f, 90f);
            PlaceHousePiece(world, BuildPieceType.Wall, 7f, 4f, 90f);
            PlaceHousePiece(world, BuildPieceType.Window, 7f, 6f, 90f);
            PlaceHousePiece(world, BuildPieceType.Wall, 7f, 8f, 90f);

            // Interior partition (z = 7) splits the living room (south) from the store room (north),
            // joined by an interior doorway.
            PlaceHousePiece(world, BuildPieceType.Wall, 2f, 7f, 0f);
            PlaceHousePiece(world, BuildPieceType.Doorway, 4f, 7f, 0f);
            PlaceHousePiece(world, BuildPieceType.Wall, 6f, 7f, 0f);

            // A storage crate in the back room to get the survivor started.
            LootContainer.Spawn(world, ContainerType.Crate, new Vector3(5f, 0f, 8f), 7777);
        }

        // Raises one house piece at the (x, z) ground tile, lifted to its layer's occupancy centre.
        private static void PlaceHousePiece(Transform world, BuildPieceType type, float x, float z, float yaw)
        {
            Vector3 center = BuildPiece.CenterFor(type, new Vector3(x, 0f, z));
            BuildPiece.Spawn(world, type, center, yaw, 1f);
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

        // The kitchen sink (a plumbed tap) sits inside the house beside the stove, under the
        // south-west window; the rain barrel stays outside by the west wall to catch the rain. The
        // tap runs clean until the mains are shut off a few days in; after that the barrel's collected
        // rain — tainted, so boil it — is the renewable water supply. Both are visual-only (colliders
        // stripped), so the navmesh is untouched.
        private void BuildWaterSources(Transform world)
        {
            WaterSource.Spawn(world, new Vector3(2f, 0f, 3.6f), WaterSourceKind.Tap);
            WaterSource.Spawn(world, new Vector3(0.5f, 0f, 6f), WaterSourceKind.RainBarrel);
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

        // A road running east–west across the middle of the map, past the front of the house. Purely
        // cosmetic: flat decals (asphalt, kerb lines and a dashed centre line) laid over the ground with
        // their colliders stripped, so they never affect movement or the navmesh.
        private void BuildStreet(Transform world)
        {
            float len = CloneZConfig.ArenaHalfSize * 2f;   // span the arena, inside the perimeter walls
            const float lineY = 0.05f;

            FlatDecal("Street", new Vector3(0f, 0.02f, 0f), new Vector3(len, 0.04f, 6f), new Color(0.13f, 0.13f, 0.14f), world);

            // White kerb lines just inside each edge.
            var white = new Color(0.82f, 0.82f, 0.80f);
            FlatDecal("StreetEdgeN", new Vector3(0f, lineY, 2.9f), new Vector3(len, 0.03f, 0.16f), white, world);
            FlatDecal("StreetEdgeS", new Vector3(0f, lineY, -2.9f), new Vector3(len, 0.03f, 0.16f), white, world);

            // Dashed yellow centre line.
            var yellow = new Color(0.85f, 0.72f, 0.20f);
            for (float x = -len * 0.5f + 1.5f; x < len * 0.5f; x += 3f)
                FlatDecal("StreetDash", new Vector3(x, lineY, 0f), new Vector3(1.5f, 0.03f, 0.18f), yellow, world);
        }

        // A flat, collider-less decal box laid on the ground (used for road markings).
        private GameObject FlatDecal(string name, Vector3 pos, Vector3 size, Color color, Transform parent)
        {
            var go = CloneZUtil.Box(name, pos, size, color, parent);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }

        private void BakeNavMesh(Transform world)
        {
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
            root.AddComponent<PlayerBuilder>();
            root.AddComponent<PlayerFarming>();       // till / sow / water / harvest crops (F)
            root.AddComponent<PlayerHydration>();     // drink / fill bottles at a tap or barrel (G)
            root.AddComponent<PlayerGear>();          // night vision (N), flashlight (L), gas mask + filters
            root.AddComponent<PlayerAvatar>();        // Clone Z's body: a rigged humanoid animated from all of the above
            return root.GetComponent<PlayerStats>();
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

        // Dresses the survivor in Clone Z's outfit — a teal T-shirt, jeans and leather boots — with a
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
