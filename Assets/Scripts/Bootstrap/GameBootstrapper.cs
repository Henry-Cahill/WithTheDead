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
            ShowMainMenu();
        }

        // The title screen decides whether to start fresh or continue a saved run.
        private void ShowMainMenu()
        {
            var menu = new GameObject("MainMenu").AddComponent<MainMenuController>();
            menu.Bind(this);
        }

        /// <summary>Begins a brand-new run, discarding any existing save (called by the title screen).</summary>
        public void StartNewGame()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            SaveSystem.Delete();
            BuildWorld(false);
        }

        /// <summary>Resumes the saved run (called by the title screen's Continue button).</summary>
        public void ContinueGame()
        {
            if (_worldBuilt) return;
            _worldBuilt = true;
            BuildWorld(true);
        }

        private void BuildWorld(bool loadSave)
        {
            UIState.GameplayBlocked = false;
            UIState.BuildMode = false;
            Time.timeScale = 1f;

            var gm = new GameObject("GameManager").AddComponent<GameManager>();

            Light sun = SetupEnvironment();
            SetupTimeAndWeather(sun);
            EnsureEventSystem();

            // Continue from a saved run when asked (and one exists); otherwise start fresh.
            SaveData save = null;
            bool loaded = loadSave && SaveSystem.TryLoad(out save) && save.Player != null;

            var world = BuildArena();
            BuildBed(world);
            BuildStove(world);
            if (loaded)
            {
                // Restore the saved world (clock/weather/utilities/score + every dynamic object)
                // before the bake, so restored world containers carve the navmesh as they did before.
                SaveSystem.ApplyWorld(save, world);
            }
            else
            {
                BuildFarm(world);
                BuildWaterSources(world);
                BuildLootContainers(world);
            }
            BakeNavMesh(world);

            PlayerStats player = BuildPlayer();
            gm.Player = player;

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
            if (loaded)
            {
                SaveSystem.ApplyPlayer(save, player);
            }
            else
            {
                GiveStartingItems(inventory);
                EquipStartingClothes(clothing);
            }

            SetupCamera(player.transform);

            var invUi = new GameObject("InventoryUI").AddComponent<InventoryUIController>();
            invUi.Bind(inventory);
            if (interactor != null) interactor.Bind(invUi);

            var skillsUi = new GameObject("SkillsUI").AddComponent<SkillsPanelController>();
            skillsUi.Bind(skills);

            var healthUi = new GameObject("HealthUI").AddComponent<HealthPanelController>();
            healthUi.Bind(wounds, player, inventory, panic, temperature, clothing, nutrition);

            BuildHUD(player, inventory, interactor, combat, sleep, skills, builder, wounds, panic, nutrition, farming, hydration);
            BuildSpawner(player.transform);

            // The pause menu (Esc) freezes the world and offers save / quit-to-menu.
            new GameObject("PauseMenu").AddComponent<PauseMenuController>();

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
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

        // A bed near the spawn point where the player can safely sleep off the night.
        private void BuildBed(Transform world)
        {
            SleepSpot.Spawn(world, new Vector3(4.5f, 0f, 4.5f));
        }

        // A stove beside the bed so the survivor can cook raw ingredients into safe, filling meals.
        private void BuildStove(Transform world)
        {
            CookingStation.Spawn(world, new Vector3(2f, 0f, 4.5f));
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

        // A tap and a rain barrel by the home base. The tap runs clean until the mains are shut
        // off a few days in; after that the barrel's collected rain — tainted, so boil it — is the
        // renewable water supply. Both are visual-only (colliders stripped), so the navmesh is untouched.
        private void BuildWaterSources(Transform world)
        {
            WaterSource.Spawn(world, new Vector3(5f, 0f, 2.5f), WaterSourceKind.Tap);
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

            // Scattered cover — carves the NavMesh and breaks line of sight.
            var random = new System.Random(1337);
            var coverColor = new Color(0.30f, 0.27f, 0.24f);
            int placed = 0, attempts = 0;
            while (placed < 16 && attempts < 200)
            {
                attempts++;
                float x = (float)(random.NextDouble() * 2 - 1) * (CloneZConfig.ArenaHalfSize - 4f);
                float z = (float)(random.NextDouble() * 2 - 1) * (CloneZConfig.ArenaHalfSize - 4f);
                if (new Vector2(x, z).magnitude < 7f) continue; // keep spawn area clear
                float w = 2f + (float)random.NextDouble() * 3f;
                float d = 2f + (float)random.NextDouble() * 3f;
                float h = 2.5f + (float)random.NextDouble() * 1.5f;
                CloneZUtil.Box("Cover", new Vector3(x, h * 0.5f, z), new Vector3(w, h, d), coverColor, world);
                placed++;
            }

            return world;
        }

        private void BakeNavMesh(Transform world)
        {
            // Built before the player/zombies exist, so only static geometry bakes.
            var surfGo = new GameObject("NavMeshSurface");
            surfGo.transform.SetParent(world);
            var surface = surfGo.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();
        }
        #endregion

        #region Player & camera
        private PlayerStats BuildPlayer()
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.transform.position = new Vector3(0f, 0.6f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Destroy(body.GetComponent<Collider>()); // CharacterController is our collider
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.2f, 0.55f, 0.65f));

            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            Destroy(face.GetComponent<Collider>());
            face.transform.SetParent(body.transform);
            face.transform.localScale = new Vector3(0.55f, 0.3f, 0.3f);
            face.transform.localPosition = new Vector3(0f, 0.6f, 0.42f);
            face.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.9f, 0.9f, 0.85f));

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
            return root.GetComponent<PlayerStats>();
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

        private void BuildSpawner(Transform player)
        {
            var spawner = new GameObject("ZombieSpawner").AddComponent<ZombieSpawner>();
            spawner.Player = player;
            spawner.StartWave = GameManager.Instance != null ? GameManager.Instance.Wave : 0;
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

        private void BuildLootContainers(Transform world)
        {
            var rng = new System.Random(4242);
            var placed = new System.Collections.Generic.List<Vector3>();
            var allTypes = (ContainerType[])System.Enum.GetValues(typeof(ContainerType));
            var types = System.Array.FindAll(allTypes, t => t != ContainerType.Corpse);

            int count = 0, attempts = 0;
            while (count < 14 && attempts < 400)
            {
                attempts++;
                float x = (float)(rng.NextDouble() * 2 - 1) * (CloneZConfig.ArenaHalfSize - 4f);
                float z = (float)(rng.NextDouble() * 2 - 1) * (CloneZConfig.ArenaHalfSize - 4f);
                if (new Vector2(x, z).magnitude < 7f) continue; // keep the spawn area clear

                var p = new Vector3(x, 0f, z);
                bool tooClose = false;
                for (int i = 0; i < placed.Count; i++)
                    if ((placed[i] - p).sqrMagnitude < 12f) { tooClose = true; break; }
                if (tooClose) continue;

                ContainerType type = types[rng.Next(types.Length)];
                LootContainer.Spawn(world, type, p, rng.Next());
                placed.Add(p);
                count++;
            }
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
            inventory.AddStarter("pistol", 1);
            inventory.AddStarter("ammo9mm", 30);
            inventory.AddStarter("hammer", 1);        // tool for barricade crafting
            inventory.AddStarter("plank", 3);         // enough to raise a first wall
            inventory.AddStarter("nails", 3);
            inventory.AddStarter("trowel", 1);        // break ground and pull ripe crops
            inventory.AddStarter("wateringcan", 1);   // keep the plots watered
            inventory.AddStarter("potatoseeds", 3);   // sow the bare garden plots
            inventory.AddStarter("carrotseeds", 2);
            inventory.AddStarter("tomatoseeds", 2);
            inventory.Equip(ItemDatabase.Get("pistol"));   // start armed; chambers a magazine from the pack
            // A spare hoodie in the pack for when the nights turn cold.
            inventory.AddStarter("hoodie", 1);
        }

        // Dresses the survivor in a light starting outfit. It's enough for a mild day but no
        // match for a cold night or a downpour — better clothing has to be looted.
        private void EquipStartingClothes(PlayerClothing clothing)
        {
            if (clothing == null) return;
            clothing.Equip(ItemDatabase.Get("tshirt"));
            clothing.Equip(ItemDatabase.Get("jeans"));
            clothing.Equip(ItemDatabase.Get("sneakers"));
        }
        #endregion
    }
}
