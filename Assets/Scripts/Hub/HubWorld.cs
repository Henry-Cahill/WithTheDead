using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Rules that hold in the hub: a safe place. No fighting, no hunger ticking down while you shop.</summary>
    public static class HubRules
    {
        public static bool SafeZone;
        public static bool HeaterOn; // <‑‑ new flag, persisted by the Heater component
    }

    /// <summary>
    /// A counter, desk or board in the hub that opens one of the hub's screens — the trader's wares, the stash,
    /// the hideout out back, the map table by the door. Players return to people, not menus: each station has
    /// its person standing at it.
    /// </summary>
    public class HubStation : Interactable
    {
        public string Title = "Station";
        public string Screen;
        public string Arg;
        public string Verb = "Talk";
        public Vector3 Point;
        public readonly List<(string Label, string Screen, string Arg)> Extra = new List<(string, string, string)>();

        public override string DisplayName => Title;
        public override Vector3 InteractPoint => Point;
        public override float Reach => 2.6f;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            string screen = Screen, arg = Arg;
            into.Add(new InteractAction(Verb, c => Open(screen, arg)));
            for (int i = 0; i < Extra.Count; i++)
            {
                (string label, string s, string a) = Extra[i];
                into.Add(new InteractAction(label, c => Open(s, a)));
            }
            if (Title == HubWorld.ZoneDoorTitle)
                into.Add(new InteractAction("Inspect door", c => GameEvents.RaiseToast(
                    "The door is locked tight, but you can hear the cold wind outside.")));
        }

        private static void Open(string screen, string arg)
        {
            if (HubScreens.Has(screen))
            {
                HubScreens.Open(screen, arg);
                if (!(UIScreen.Current is HubScreenBase)) Sfx.PlayUI(SfxIds.UiClick);   // the hub's own screens sound their opening
            }
            else
            {
                Sfx.PlayUI(SfxIds.UiClick);
                GameEvents.RaiseToast("Nobody's behind the counter right now.");
            }
        }
    }

    /// <summary>The stash locker: opens the character's stash in the inventory window, beside the carried kit.</summary>
    public class HubStashLocker : Interactable
    {
        public LootContainer Container;
        public Vector3 Point;

        public override string DisplayName => "Your stash";
        public override Vector3 InteractPoint => Point;
        public override float Reach => 2.6f;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Open your stash", c =>
            {
                if (Container == null) return;
                Container.Contents = ProfileStore.StashInventory;
                Container.Searched = true;
                c.Interactor.SearchOrOpen(Container);
            }));
            into.Add(new InteractAction("Insurance & returns", c => HubScreens.Open(HubScreens.Insurance)));
        }
    }

    /// <summary>
    /// Builds a hub — the bar runners live in between raids — from code: the room, the bar, the tables, a stage,
    /// every station with its keeper and a sign over it, the stash locker, the door out, and the patrons who make it
    /// feel like home. Three hubs share the plan with their own materials and light. The Rusty Kettle is a Kentucky
    /// roadhouse (knotty-pine panelling, neon, a pool table and a jukebox, a woodstove with the kettle on it, the
    /// Christmas lights up for December): on its own (<see cref="Build"/>), or standing on its lot in Bardstown with its
    /// clapboard shell, tin roof, porch and front door onto the town (<see cref="BuildInTown"/>, see KettleLobby).
    /// The plan is laid out around the room's middle with the door on the -z side; a hub in the town is built there
    /// first and then moved onto its lot.
    /// </summary>
    public static partial class HubWorld
    {
        private const float HalfX = 13f, HalfZ = 9f, WallH = 3.2f, WallT = 0.3f;

        /// <summary>How far the bar's floor stands above the ground in the town: a slab on a foundation, up from the porch.</summary>
        public const float Lift = 0.3f;

        /// <summary>The standalone hub's door station (the door to the Zone's deploy screen).</summary>
        public const string ZoneDoorTitle = "Door to the Zone";

        /// <summary>Half the room's width (local x) and depth (local z), inside the walls.</summary>
        public static Vector2 HalfSize => new Vector2(HalfX, HalfZ);

        /// <summary>Where a new arrival stands: just inside the door, facing the bar.</summary>
        private static readonly Vector3 SpawnLocal = new Vector3(0f, 0.1f, -6.8f);

        public sealed class Result
        {
            public Vector3 PlayerSpawn;
            public float PlayerYaw;
            /// <summary>The bar itself (under the world, so the NavMesh bake sees it).</summary>
            public Transform Root;
            /// <summary>Its ceiling, roof and roof sign: kept out of the world, and the bake.</summary>
            public Transform Overhead;
        }

        private struct Style
        {
            public Surf Floor, Wall, Trim, Wood, DarkWood, Metal, Ceiling;
            public Surf Siding, SidingTrim, Roof;
            public Color Light, Ambient, Fog;
            public float LightIntensity;
            public string Sign;
            public bool Roadhouse;
        }

        // The Kettle's walls: knotty-pine boards over a dark stained wainscot.
        private static readonly WallDesign KettlePine = new WallDesign
        {
            Key = "kettlepine", Wainscot = new Color(0.30f, 0.19f, 0.11f), Paper = new Color(0.62f, 0.44f, 0.26f), Trim = new Color(0.21f, 0.13f, 0.08f),
            Lower = WainscotStyle.Panels, Upper = PaperStyle.Stripes, RailHeight = 1.0f, Wear = 0.55f,
        };

        /// <summary>Builds the hub on its own (the other hubs, a networked session, or a Kettle the town couldn't hold):
        /// the room in a void, lit by its own lamps.</summary>
        public static Result Build(Transform world, HubDef hub)
        {
            Style st = StyleFor(hub);
            var root = new GameObject("Hub").transform;
            root.SetParent(world, false);
            var overhead = new GameObject("HubOverhead").transform;
            CeilingVisibility.Clear();
            overhead.gameObject.AddComponent<CeilingVisibility>();

            BuildInterior(root, overhead, hub, st, false);
            Shelter.Register(new Bounds(new Vector3(0f, WallH * 0.5f, 0f), new Vector3(HalfX * 2f, WallH, HalfZ * 2f)));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = st.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = st.Fog;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 70f;
            return new Result { PlayerSpawn = SpawnLocal, PlayerYaw = 0f, Root = root, Overhead = overhead };
        }

        /// <summary>
        /// Builds the hub standing on <paramref name="lot"/> in the town: the room with its clapboard shell, roof, porch
        /// and a front door that opens onto the street (<see cref="KettleDoor"/>). The town's own light and weather
        /// stand; the room keeps its lamps. Its ceiling joins the map's (shown only from inside views).
        /// </summary>
        public static Result BuildInTown(Transform world, HubDef hub, KentuckyMap.Lot lot)
        {
            Style st = StyleFor(hub);
            var root = new GameObject("Kettle").transform;
            root.SetParent(world, false);
            var overhead = new GameObject("KettleOverhead").transform;

            BuildInterior(root, overhead, hub, st, true);
            BuildExterior(root, overhead, st);
            DecorPalette.Apply();

            // Built at the origin, where the plan's local and world positions agree; moved onto its lot now.
            var at = new Vector3(lot.Center.x, Lift, lot.Center.y);
            Quaternion turn = Quaternion.Euler(0f, lot.Yaw, 0f);
            root.SetPositionAndRotation(at, turn);
            overhead.SetPositionAndRotation(at, turn);
            Relocate(root);
            RegisterShelter(root);
            return new Result { PlayerSpawn = root.TransformPoint(SpawnLocal), PlayerYaw = lot.Yaw, Root = root, Overhead = overhead };
        }

        private static void BuildInterior(Transform root, Transform overhead, HubDef hub, Style st, bool inTown)
        {
            BuildShell(root, overhead, st, inTown);
            BuildBar(root, st);
            BuildFurniture(root, overhead, st, inTown);
            if (st.Roadhouse) BuildRoadhouse(root, overhead, st);
            BuildWindows(root, st, inTown);
            BuildLights(root, st);
            DecorPalette.Apply();
            BuildStations(root, hub, inTown);
            BuildSigns(root, overhead, inTown);
            BuildPatrons(root, hub);
            DecorPalette.Apply();
        }

        /// <summary>Selects the visual style and lighting parameters for a hub based on its identifier.</summary>
        private static Style StyleFor(HubDef hub)
        {
            switch (hub.Id)
            {
                case HubCatalog.NorthCamp:
                    return new Style
                    {
                        Floor = DecorMaterials.Planks(new Color(0.42f, 0.33f, 0.24f)),
                        Wall = DecorMaterials.Planks(new Color(0.38f, 0.30f, 0.22f)),
                        Trim = DecorPalette.Matte(new Color(0.25f, 0.20f, 0.15f)),
                        Wood = DecorPalette.Matte(new Color(0.46f, 0.34f, 0.22f)),
                        DarkWood = DecorPalette.Matte(new Color(0.24f, 0.18f, 0.12f)),
                        Metal = DecorPalette.Metal(new Color(0.40f, 0.40f, 0.42f)),
                        Ceiling = DecorMaterials.Planks(new Color(0.30f, 0.24f, 0.18f)),
                        Siding = DecorMaterials.Planks(new Color(0.38f, 0.30f, 0.22f)),
                        SidingTrim = DecorPalette.Matte(new Color(0.25f, 0.20f, 0.15f)),
                        Roof = DecorMaterials.Corrugated(new Color(0.36f, 0.36f, 0.38f)),
                        Light = new Color(1f, 0.74f, 0.45f), LightIntensity = 1.6f,
                        Ambient = new Color(0.20f, 0.18f, 0.16f), Fog = new Color(0.08f, 0.07f, 0.06f),
                        Sign = "NORTH CAMP",
                    };
                case HubCatalog.Dam:
                    return new Style
                    {
                        Floor = DecorMaterials.Concrete,
                        Wall = DecorMaterials.Concrete,
                        Trim = DecorPalette.Matte(new Color(0.30f, 0.32f, 0.30f)),
                        Wood = DecorPalette.Matte(new Color(0.38f, 0.40f, 0.36f)),
                        DarkWood = DecorPalette.Matte(new Color(0.22f, 0.24f, 0.22f)),
                        Metal = DecorPalette.Metal(new Color(0.46f, 0.48f, 0.46f)),
                        Ceiling = DecorMaterials.Concrete,
                        Siding = DecorMaterials.Concrete,
                        SidingTrim = DecorPalette.Matte(new Color(0.30f, 0.32f, 0.30f)),
                        Roof = DecorMaterials.Concrete,
                        Light = new Color(0.86f, 0.95f, 1f), LightIntensity = 1.4f,
                        Ambient = new Color(0.20f, 0.22f, 0.22f), Fog = new Color(0.06f, 0.07f, 0.07f),
                        Sign = "DAM BUNKER  /  VANGUARD",
                    };
                default:
                    return new Style
                    {
                        Floor = DecorMaterials.WoodFloor(new Color(0.34f, 0.23f, 0.15f)),
                        Wall = DecorMaterials.WallInterior(KettlePine),
                        Trim = DecorPalette.Matte(new Color(0.22f, 0.14f, 0.08f)),
                        Wood = DecorPalette.Matte(new Color(0.48f, 0.32f, 0.19f)),
                        DarkWood = DecorPalette.Matte(new Color(0.22f, 0.14f, 0.09f)),
                        Metal = DecorPalette.Metal(new Color(0.58f, 0.52f, 0.42f)),
                        Ceiling = DecorMaterials.Planks(new Color(0.33f, 0.23f, 0.15f)),
                        // Barn-red clapboard with white trim under a rusting tin roof: the Kettle earned its name.
                        Siding = DecorMaterials.WallExterior(ExteriorStyle.Siding, new Color(0.52f, 0.18f, 0.12f)),
                        SidingTrim = DecorPalette.Matte(new Color(0.86f, 0.84f, 0.78f)),
                        Roof = DecorMaterials.Corrugated(new Color(0.46f, 0.27f, 0.16f)),
                        Light = new Color(1f, 0.71f, 0.42f), LightIntensity = 1.9f,
                        Ambient = new Color(0.24f, 0.19f, 0.15f), Fog = new Color(0.07f, 0.06f, 0.05f),
                        Sign = "THE RUSTY KETTLE",
                        Roadhouse = true,
                    };
            }
        }

        #region Geometry helpers
        // A solid piece: rendered (so the navmesh bake carves it) and collidable. Geometry is drawn in world
        // space and emitted at its centre, so the object's local space is centred on the piece.
        private static GameObject Solid(Transform parent, string name, Vector3 center, Vector3 size, in Surf s)
        {
            var m = new RaidMesh();
            m.Box(center, size, s);
            GameObject go = m.Emit(name, parent, center);
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            return go;
        }

        // A detailed piece with a plain box collider round it (furniture): drawn in world space, emitted at its centre.
        private static GameObject Prop(Transform parent, string name, Vector3 center, Vector3 colliderSize, System.Action<RaidMesh> draw)
        {
            var m = new RaidMesh();
            draw(m);
            GameObject go = m.Emit(name, parent, center);
            if (go == null) return null;
            var col = go.AddComponent<BoxCollider>();
            col.size = colliderSize;
            return go;
        }

        // A decorative piece with no collider (geometry in world space, emitted at origin).
        private static GameObject Deco(Transform parent, string name, System.Action<RaidMesh> draw, Vector3 origin)
        {
            var m = new RaidMesh();
            draw(m);
            return m.Emit(name, parent, origin);
        }

        // A wall: its core with the room's finish and, in the town, a skin of siding on its outer face, drawn as one piece
        // with one collider, so the camera's cutaway takes both together.
        private static GameObject WallPiece(Transform parent, string name, Vector3 center, Vector3 size, Vector3 outward, in Surf inner,
            in Surf outer, bool skin)
        {
            const float Skin = 0.05f;
            var m = new RaidMesh();
            m.Box(center, size, inner);
            Vector3 colCenter = Vector3.zero, colSize = size;
            if (skin)
            {
                bool alongX = Mathf.Abs(outward.x) > 0.5f;
                float half = (alongX ? size.x : size.z) * 0.5f;
                Vector3 skinSize = alongX ? new Vector3(Skin, size.y, size.z) : new Vector3(size.x, size.y, Skin);
                m.Box(center + outward * (half + Skin * 0.5f), skinSize, outer);
                colCenter = outward * (Skin * 0.5f);
                colSize = alongX ? new Vector3(size.x + Skin, size.y, size.z) : new Vector3(size.x, size.y, size.z + Skin);
            }
            GameObject go = m.Emit(name, parent, center);
            var col = go.AddComponent<BoxCollider>();
            col.center = colCenter;
            col.size = colSize;
            return go;
        }
        #endregion

        private static void BuildShell(Transform world, Transform overhead, Style st, bool inTown)
        {
            if (inTown)
            {
                // A concrete foundation standing proud of the snow, the floor laid on it.
                Solid(world, "Foundation", new Vector3(0f, -(Lift + 0.08f) * 0.5f, 0f),
                    new Vector3(HalfX * 2f + WallT + 0.12f, Lift + 0.08f, HalfZ * 2f + WallT + 0.12f), DecorMaterials.Concrete);
                Solid(world, "Floor", new Vector3(0f, 0.006f, 0f), new Vector3(HalfX * 2f, 0.012f, HalfZ * 2f), st.Floor);
            }
            else Solid(world, "Floor", new Vector3(0f, -0.15f, 0f), new Vector3(HalfX * 2f + 2f, 0.3f, HalfZ * 2f + 2f), st.Floor);

            // Walls, leaving the doorway in the middle of the south wall.
            float sx = (HalfX + WallT * 0.5f + 1.1f) * 0.5f, sw = HalfX + WallT * 0.5f - 1.1f;
            WallPiece(world, "Wall_N", new Vector3(0f, WallH * 0.5f, HalfZ), new Vector3(HalfX * 2f + WallT, WallH, WallT), Vector3.forward, st.Wall, st.Siding, inTown);
            WallPiece(world, "Wall_E", new Vector3(HalfX, WallH * 0.5f, 0f), new Vector3(WallT, WallH, HalfZ * 2f + WallT + 0.1f), Vector3.right, st.Wall, st.Siding, inTown);
            WallPiece(world, "Wall_W", new Vector3(-HalfX, WallH * 0.5f, 0f), new Vector3(WallT, WallH, HalfZ * 2f + WallT + 0.1f), Vector3.left, st.Wall, st.Siding, inTown);
            WallPiece(world, "Wall_S1", new Vector3(-sx, WallH * 0.5f, -HalfZ), new Vector3(sw, WallH, WallT), Vector3.back, st.Wall, st.Siding, inTown);
            WallPiece(world, "Wall_S2", new Vector3(sx, WallH * 0.5f, -HalfZ), new Vector3(sw, WallH, WallT), Vector3.back, st.Wall, st.Siding, inTown);
            WallPiece(world, "Lintel", new Vector3(0f, WallH - 0.4f, -HalfZ), new Vector3(2.2f, 0.8f, WallT), Vector3.back, st.Wall, st.Siding, inTown);
            // Door casing (inside), and on its own the door to the Zone, shut against the cold (the deploy station). In the
            // town the front door is a real one (BuildFrontDoor).
            Deco(WallOf(world, "Lintel"), "DoorCasing", m =>
            {
                float z = -HalfZ + WallT * 0.5f + 0.02f;
                m.Box(new Vector3(-1.16f, 1.2f, z), new Vector3(0.12f, 2.4f, 0.04f), st.Trim);
                m.Box(new Vector3(1.16f, 1.2f, z), new Vector3(0.12f, 2.4f, 0.04f), st.Trim);
                m.Box(new Vector3(0f, 2.46f, z), new Vector3(2.44f, 0.12f, 0.04f), st.Trim);
            }, Vector3.zero);
            if (!inTown)
            {
                Solid(world, "ZoneDoor", new Vector3(0f, 1.2f, -HalfZ - 0.05f), new Vector3(2.0f, 2.4f, 0.12f), st.DarkWood);
                Deco(world, "DoorHardware", m =>
                {
                    m.Box(new Vector3(0.75f, 1.05f, -HalfZ + 0.02f), new Vector3(0.36f, 0.06f, 0.05f), st.Metal);
                    m.Cylinder(new Vector3(0.7f, 1.2f, -HalfZ - 0.12f), 0.04f, 0.04f, 12, st.Metal, Quaternion.Euler(90f, 0f, 0f));
                }, Vector3.zero);
            }
            // Skirting round the room.
            Deco(world, "Skirting", m =>
            {
                float y = 0.06f, t = 0.04f;
                m.Box(new Vector3(0f, y, HalfZ - WallT * 0.5f - t * 0.5f), new Vector3(HalfX * 2f, 0.12f, t), st.Trim);
                m.Box(new Vector3(HalfX - WallT * 0.5f - t * 0.5f, y, 0f), new Vector3(t, 0.12f, HalfZ * 2f), st.Trim);
                m.Box(new Vector3(-HalfX + WallT * 0.5f + t * 0.5f, y, 0f), new Vector3(t, 0.12f, HalfZ * 2f), st.Trim);
                m.Box(new Vector3(-sx, y, -HalfZ + WallT * 0.5f + t * 0.5f), new Vector3(sw - WallT * 0.5f, 0.12f, t), st.Trim);
                m.Box(new Vector3(sx, y, -HalfZ + WallT * 0.5f + t * 0.5f), new Vector3(sw - WallT * 0.5f, 0.12f, t), st.Trim);
            }, Vector3.zero);

            Surf sign = DecorMaterials.Sign(st.Sign, new Color(0.12f, 0.08f, 0.05f), new Color(0.95f, 0.75f, 0.35f), out float aspect);
            var signGo = Deco(WallOf(world, "Wall_N"), "Sign", m =>
            {
                var at = new Vector3(-2f, 2.82f, HalfZ - WallT * 0.5f - 0.03f);
                m.Box(at + new Vector3(0f, 0f, 0.012f), new Vector3(0.62f * aspect + 0.12f, 0.74f, 0.03f), st.DarkWood);
                m.Upright(at - new Vector3(0f, 0f, 0.005f), Vector3.back, 0.62f * aspect, 0.62f, sign, new Rect(0f, 0f, 1f, 1f));
            }, Vector3.zero);
            signGo.AddComponent<WeatherSign>();
            // The ceiling: shown from the shoulder and first-person views only (the isometric view looks in).
            GameObject ceiling = Deco(overhead, "Ceiling", m =>
            {
                m.Box(new Vector3(0f, WallH + 0.05f, 0f), new Vector3(HalfX * 2f, 0.1f, HalfZ * 2f), st.Ceiling);
                for (int i = -3; i <= 3; i++)
                    m.Box(new Vector3(i * 3.6f, WallH - 0.08f, 0f), new Vector3(0.22f, 0.26f, HalfZ * 2f), st.DarkWood);
            }, Vector3.zero);
            CeilingVisibility.Register(ceiling.GetComponent<Renderer>());
        }

        private static void BuildBar(Transform world, Style st)
        {
            Prop(world, "BarCounter", new Vector3(-2f, 0.55f, 6.2f), new Vector3(10f, 1.1f, 0.8f), m =>
            {
                m.Box(new Vector3(-2f, 0.52f, 6.24f), new Vector3(9.8f, 1.04f, 0.7f), st.DarkWood);
                // Raised panels down the front and a brass foot rail.
                for (int i = 0; i < 9; i++)
                    m.Box(new Vector3(-6.4f + i * 1.1f, 0.6f, 5.87f), new Vector3(0.9f, 0.62f, 0.03f), st.Wood);
                m.CylinderX(new Vector3(-2f, 0.2f, 5.66f), 0.03f, 9.8f, 10, DecorPalette.Metal(new Color(0.78f, 0.62f, 0.30f)));
                for (int i = 0; i < 6; i++)
                    m.Box(new Vector3(-6.6f + i * 1.9f, 0.13f, 5.76f), new Vector3(0.03f, 0.14f, 0.2f), DecorPalette.Metal(new Color(0.78f, 0.62f, 0.30f)));
            });
            Deco(world, "BarTop", m => m.Box(new Vector3(-2f, 1.12f, 6.2f), new Vector3(10.2f, 0.05f, 0.9f), DecorPalette.Gloss(new Color(0.42f, 0.25f, 0.13f))), Vector3.zero);
            Solid(world, "BackShelf", new Vector3(-2f, 1.2f, HalfZ - 0.35f), new Vector3(9f, 2.4f, 0.4f), st.Wood);
            var rng = new System.Random(7);
            Deco(world, "Bottles", m =>
            {
                // A mirror behind the bottles, then three shelves of bourbon, rye and the rest.
                m.Box(new Vector3(-2f, 1.2f, HalfZ - 0.56f), new Vector3(8.4f, 1.7f, 0.01f), DecorPalette.Glass(new Color(0.55f, 0.58f, 0.60f)));
                for (int s = 0; s < 3; s++)
                    m.Box(new Vector3(-2f, 0.5f + s * 0.65f, HalfZ - 0.62f), new Vector3(8.6f, 0.04f, 0.26f), st.DarkWood);
                for (int i = 0; i < 54; i++)
                {
                    float x = -6.2f + (float)rng.NextDouble() * 8.4f;
                    float y = 0.52f + rng.Next(3) * 0.65f;
                    bool bourbon = rng.NextDouble() < 0.6;
                    Color c = bourbon ? new Color(0.55f + (float)rng.NextDouble() * 0.2f, 0.28f + (float)rng.NextDouble() * 0.12f, 0.08f)
                        : new Color(0.2f + (float)rng.NextDouble() * 0.4f, 0.3f + (float)rng.NextDouble() * 0.35f, 0.15f + (float)rng.NextDouble() * 0.3f);
                    float h = 0.24f + (float)rng.NextDouble() * 0.1f;
                    m.Cylinder(new Vector3(x, y, HalfZ - 0.62f), 0.04f, h, 6, DecorPalette.Glass(c));
                    m.Cylinder(new Vector3(x, y + h, HalfZ - 0.62f), 0.013f, 0.06f, 5, DecorPalette.Matte(new Color(0.12f, 0.10f, 0.08f)));
                }
            }, Vector3.zero);
            // Bar stools: red vinyl seats on chrome posts.
            for (int i = 0; i < 6; i++)
            {
                var pos = new Vector3(-6f + i * 1.6f, 0f, 5.3f);
                Prop(world, "BarStool", pos + new Vector3(0f, 0.38f, 0f), new Vector3(0.38f, 0.76f, 0.38f), m =>
                {
                    m.Cylinder(pos, 0.2f, 0.03f, 12, st.Metal);
                    m.Cylinder(pos, 0.035f, 0.7f, 8, st.Metal);
                    m.Cylinder(pos + new Vector3(0f, 0.3f, 0f), 0.16f, 0.02f, 12, st.Metal);
                    m.Cylinder(pos + new Vector3(0f, 0.68f, 0f), 0.19f, 0.09f, 14, DecorPalette.Gloss(new Color(0.55f, 0.10f, 0.08f)));
                });
            }
        }

        // A table on a pedestal (its collider the old block) and chairs round it, each facing the table.
        private static void Table(Transform world, Vector3 t, Style st)
        {
            Prop(world, "Table", t + new Vector3(0f, 0.38f, 0f), new Vector3(1.2f, 0.76f, 1.2f), m =>
            {
                m.Box(t + new Vector3(0f, 0.74f, 0f), new Vector3(1.2f, 0.05f, 1.2f), st.Wood);
                m.Box(t + new Vector3(0f, 0.7f, 0f), new Vector3(1.08f, 0.04f, 1.08f), st.DarkWood);
                m.Cylinder(t, 0.07f, 0.7f, 8, st.DarkWood);
                m.Box(t + new Vector3(0f, 0.025f, 0f), new Vector3(0.62f, 0.05f, 0.62f), st.DarkWood);
                // An ashtray and a bottle of something.
                m.Cylinder(t + new Vector3(0.25f, 0.765f, -0.2f), 0.07f, 0.02f, 10, DecorPalette.Glass(new Color(0.40f, 0.45f, 0.42f)));
                m.Cylinder(t + new Vector3(-0.2f, 0.765f, 0.15f), 0.035f, 0.22f, 6, DecorPalette.Glass(new Color(0.55f, 0.30f, 0.09f)));
            });
            foreach (Vector3 off in new[] { new Vector3(0.95f, 0f, 0f), new Vector3(-0.95f, 0f, 0f), new Vector3(0f, 0f, 0.95f) })
            {
                Vector3 c = t + off;
                Vector3 back = off.normalized;   // the backrest away from the table
                Prop(world, "Chair", c + new Vector3(0f, 0.23f, 0f), new Vector3(0.45f, 0.46f, 0.45f), m =>
                {
                    m.Box(c + new Vector3(0f, 0.44f, 0f), new Vector3(0.44f, 0.05f, 0.44f), st.Wood);
                    foreach (Vector2 l in new[] { new Vector2(-0.18f, -0.18f), new Vector2(0.18f, -0.18f), new Vector2(-0.18f, 0.18f), new Vector2(0.18f, 0.18f) })
                        m.Box(c + new Vector3(l.x, 0.21f, l.y), new Vector3(0.04f, 0.42f, 0.04f), st.DarkWood);
                    Vector3 side = new Vector3(back.z, 0f, -back.x);
                    m.Box(c + back * 0.2f + new Vector3(0f, 0.72f, 0f), new Vector3(Mathf.Abs(side.x) * 0.42f + Mathf.Abs(back.x) * 0.04f, 0.5f,
                        Mathf.Abs(side.z) * 0.42f + Mathf.Abs(back.z) * 0.04f), st.DarkWood);
                });
            }
        }

        private static void BuildFurniture(Transform world, Transform overhead, Style st, bool inTown)
        {
            foreach (Vector3 t in new[] { new Vector3(-6f, 0f, 1.5f), new Vector3(-1.5f, 0f, 0.5f), new Vector3(3f, 0f, 1.8f), new Vector3(-4f, 0f, -3.5f), new Vector3(1.5f, 0f, -3.2f) })
                Table(world, t, st);
            // The stage corner, with an amp and a mic stand.
            Solid(world, "Stage", new Vector3(9f, 0.12f, -2f), new Vector3(3.2f, 0.24f, 3.2f), st.DarkWood);
            Deco(world, "StageKit", m =>
            {
                m.Box(new Vector3(10.15f, 0.55f, -3.05f), new Vector3(0.55f, 0.62f, 0.34f), DecorPalette.Matte(new Color(0.10f, 0.10f, 0.11f)));
                m.Box(new Vector3(10.15f, 0.55f, -2.875f), new Vector3(0.44f, 0.42f, 0.01f), DecorPalette.Matte(new Color(0.24f, 0.22f, 0.20f)));
                m.Cylinder(new Vector3(8.2f, 0.24f, -1.2f), 0.12f, 0.02f, 10, st.Metal);
                m.Cylinder(new Vector3(8.2f, 0.24f, -1.2f), 0.012f, 1.45f, 6, st.Metal);
                m.Ellipsoid(new Vector3(8.2f, 1.72f, -1.2f), new Vector3(0.05f, 0.07f, 0.05f), DecorPalette.Metal(new Color(0.20f, 0.20f, 0.22f)));
            }, Vector3.zero);
            // The fence's corner table, institute desk, gunsmith bench, medic cot, guide's map table.
            Solid(world, "FenceTable", new Vector3(-10.8f, 0.38f, 6.2f), new Vector3(1.6f, 0.76f, 1f), st.DarkWood);
            Solid(world, "InstituteDesk", new Vector3(10.4f, 0.4f, 6.4f), new Vector3(2.2f, 0.8f, 0.9f), st.Metal);
            Deco(world, "InstituteKit", m =>
            {
                m.Box(new Vector3(10.0f, 0.95f, 6.5f), new Vector3(0.5f, 0.3f, 0.4f), DecorPalette.Matte(new Color(0.85f, 0.85f, 0.82f)));
                m.Box(new Vector3(10.9f, 1.05f, 6.6f), new Vector3(0.4f, 0.5f, 0.3f), DecorPalette.Glow(new Color(0.3f, 0.9f, 0.6f)));
            }, Vector3.zero);
            Solid(world, "Workbench", new Vector3(12.2f, 0.45f, 0.5f), new Vector3(0.9f, 0.9f, 3.2f), st.Wood);
            Solid(world, "GunRack", new Vector3(12.7f, 1.5f, 0.5f), new Vector3(0.25f, 1.6f, 2.6f), st.DarkWood);
            Solid(world, "Cot", new Vector3(11.4f, 0.25f, -6.2f), new Vector3(2f, 0.5f, 0.9f), DecorPalette.Matte(new Color(0.55f, 0.58f, 0.50f)));
            Solid(world, "MedCabinet", new Vector3(12.5f, 1f, -4.2f), new Vector3(0.5f, 2f, 1.1f), DecorPalette.Matte(new Color(0.85f, 0.86f, 0.84f)));
            Deco(world, "MedCross", m =>
            {
                Surf red = DecorPalette.Matte(new Color(0.75f, 0.12f, 0.10f));
                m.Box(new Vector3(12.24f, 1.55f, -4.2f), new Vector3(0.01f, 0.36f, 0.12f), red);
                m.Box(new Vector3(12.24f, 1.55f, -4.2f), new Vector3(0.01f, 0.12f, 0.36f), red);
            }, Vector3.zero);
            Solid(world, "MapTable", new Vector3(4.5f, 0.42f, -7f), new Vector3(1.8f, 0.84f, 1.1f), st.Wood);
            Deco(world, "Map", m => m.Box(new Vector3(4.5f, 0.86f, -7f), new Vector3(1.6f, 0.01f, 0.95f), DecorMaterials.Paper(3)), Vector3.zero);
            Solid(world, "MercTable", new Vector3(-6.5f, 0.38f, -6.5f), new Vector3(1.2f, 0.76f, 1.2f), st.Wood);
            Solid(world, "NoticeBoard", new Vector3(-12.75f, 1.6f, -1.5f), new Vector3(0.1f, 1.4f, 2.2f), DecorPalette.Matte(new Color(0.55f, 0.42f, 0.28f)));
            Deco(world, "Notices", m =>
            {
                for (int i = 0; i < 7; i++)
                    m.Box(new Vector3(-12.68f, 1.25f + (i % 3) * 0.35f, -2.3f + i * 0.28f), new Vector3(0.01f, 0.28f, 0.2f), DecorMaterials.Paper(i % 4));
            }, Vector3.zero);
            Solid(world, "FleaDesk", new Vector3(-12.1f, 0.4f, -6f), new Vector3(1f, 0.8f, 1.6f), st.Metal);
            Deco(world, "FleaTerminal", m =>
            {
                m.Box(new Vector3(-12.2f, 1.05f, -6f), new Vector3(0.45f, 0.4f, 0.5f), DecorPalette.Matte(new Color(0.75f, 0.72f, 0.62f)));
                m.Box(new Vector3(-11.97f, 1.07f, -6f), new Vector3(0.01f, 0.28f, 0.38f), DecorPalette.Glow(new Color(0.35f, 0.95f, 0.45f)));
            }, Vector3.zero);
            Solid(world, "Lockers", new Vector3(-12.55f, 1f, 2.5f), new Vector3(0.6f, 2f, 2.4f), st.Metal);
            Deco(world, "LockerDoors", m =>
            {
                for (int i = 0; i < 4; i++)
                {
                    float z = 1.55f + i * 0.6f;
                    m.Box(new Vector3(-12.24f, 1f, z - 0.29f), new Vector3(0.01f, 1.9f, 0.015f), DecorPalette.Matte(new Color(0.20f, 0.21f, 0.22f)));
                    m.Box(new Vector3(-12.24f, 1.25f, z + 0.18f), new Vector3(0.02f, 0.12f, 0.02f), st.Metal);
                    for (int v = 0; v < 3; v++)
                        m.Box(new Vector3(-12.244f, 1.78f - v * 0.05f, z), new Vector3(0.005f, 0.02f, 0.3f), DecorPalette.Matte(new Color(0.18f, 0.18f, 0.19f)));
                }
            }, Vector3.zero);
            Solid(world, "HideoutDoor", new Vector3(-12.83f, 1.15f, 5f), new Vector3(0.08f, 2.3f, 1.1f), st.DarkWood);
            Solid(world, "LedgerDesk", new Vector3(7.5f, 0.4f, -7.8f), new Vector3(1.4f, 0.8f, 0.8f), st.DarkWood);
            Solid(world, "Bookshelf", new Vector3(-3f, 1f, -HalfZ + 0.3f), new Vector3(1.6f, 2f, 0.35f), st.Wood);
            Deco(world, "Books", m =>
            {
                var rng = new System.Random(31);
                float z = -HalfZ + 0.48f;
                for (int shelf = 0; shelf < 3; shelf++)
                {
                    float y = 0.45f + shelf * 0.55f, x = -3.7f;
                    while (x < -2.35f)
                    {
                        float w = 0.05f + (float)rng.NextDouble() * 0.05f, h = 0.32f + (float)rng.NextDouble() * 0.14f;
                        Color c = DecorPalette.Vary(new Color(0.42f, 0.18f + shelf * 0.06f, 0.12f), 0.35f, rng);
                        m.Box(new Vector3(x + w * 0.5f, y + h * 0.5f, z), new Vector3(w, h, 0.22f), DecorPalette.Matte(c));
                        x += w + 0.008f;
                    }
                }
            }, Vector3.zero);
            Deco(world, "Mirror", m =>
            {
                m.Box(new Vector3(-8f, 1.6f, -HalfZ + 0.17f), new Vector3(1.04f, 1.54f, 0.03f), st.DarkWood);
                m.Box(new Vector3(-8f, 1.6f, -HalfZ + 0.19f), new Vector3(0.9f, 1.4f, 0.01f), DecorPalette.Glass(new Color(0.75f, 0.82f, 0.85f)));
            }, Vector3.zero);
            Deco(world, "Rugs", m =>
            {
                m.Box(new Vector3(-1.5f, 0.005f, -1.5f), new Vector3(5f, 0.01f, 3.4f), DecorMaterials.Rug(1));
                m.Box(new Vector3(9f, 0.25f, -2f), new Vector3(2.4f, 0.01f, 2.4f), DecorMaterials.Rug(2));
            }, Vector3.zero);
            BuildWoodstove(world, overhead, st, inTown);
            BuildHeater(world, st);
        }

        // The woodstove in the middle of the west side, on a brick hearth, its pipe up through the ceiling and (in the
        // town) out of the chimney; the rusty kettle the place is named for sits on top.
        private static readonly Vector3 StoveAt = new Vector3(-9.6f, 0f, -3.4f);

        /// <summary>How high the stove's chimney rises above the room's floor, in the town.</summary>
        private const float ChimneyTop = 5.5f;

        private static void BuildWoodstove(Transform world, Transform overhead, Style st, bool inTown)
        {
            Vector3 s = StoveAt;
            Surf iron = DecorPalette.Metal(new Color(0.16f, 0.15f, 0.14f));
            Surf brick = DecorPalette.Matte(new Color(0.46f, 0.24f, 0.18f));
            Deco(world, "Hearth", m =>
            {
                m.Box(s + new Vector3(0f, 0.025f, 0f), new Vector3(1.5f, 0.05f, 1.4f), brick);
                var rng = new System.Random(5);
                for (int i = 0; i < 6; i++)
                    m.CylinderX(s + new Vector3(0.95f, 0.08f + (i / 3) * 0.13f, -0.35f + (i % 3) * 0.16f), 0.065f, 0.55f, 7,
                        DecorPalette.Matte(DecorPalette.Vary(new Color(0.42f, 0.30f, 0.20f), 0.2f, rng)), 90f);
            }, Vector3.zero);
            Deco(world, "Woodstove", m =>
            {
                foreach (Vector2 l in new[] { new Vector2(-0.3f, -0.24f), new Vector2(0.3f, -0.24f), new Vector2(-0.3f, 0.24f), new Vector2(0.3f, 0.24f) })
                    m.Box(s + new Vector3(l.x, 0.11f, l.y), new Vector3(0.06f, 0.16f, 0.06f), iron);
                m.Box(s + new Vector3(0f, 0.5f, 0f), new Vector3(0.76f, 0.66f, 0.62f), iron);
                m.Box(s + new Vector3(0f, 0.86f, 0f), new Vector3(0.8f, 0.05f, 0.66f), iron);
                // The door with its glowing window, facing the room.
                m.Box(s + new Vector3(0f, 0.48f, -0.32f), new Vector3(0.44f, 0.4f, 0.03f), iron);
                m.Box(s + new Vector3(0f, 0.5f, -0.338f), new Vector3(0.28f, 0.2f, 0.01f), DecorPalette.Glow(new Color(1f, 0.45f, 0.12f)));
                m.Box(s + new Vector3(0.18f, 0.5f, -0.35f), new Vector3(0.03f, 0.1f, 0.03f), st.Metal);
                // The rusty kettle.
                Surf rust = DecorPalette.Matte(new Color(0.48f, 0.24f, 0.12f));
                m.Ellipsoid(s + new Vector3(-0.12f, 0.98f, 0.04f), new Vector3(0.14f, 0.11f, 0.14f), rust);
                m.Rod(s + new Vector3(0.0f, 0.98f, 0.04f), s + new Vector3(0.12f, 1.05f, 0.04f), 0.022f, rust);
                m.Rod(s + new Vector3(-0.22f, 1.07f, 0.04f), s + new Vector3(-0.02f, 1.07f, 0.04f), 0.012f, iron);
            }, s);
            // The stovepipe: up through the ceiling (the overhead part shows only from inside views).
            Deco(world, "Stovepipe", m => m.Cylinder(s + new Vector3(0f, 0.88f, 0.1f), 0.085f, WallH - 0.9f, 10, iron), Vector3.zero);
            // The fire: lit and banked like any fireplace (its collider is the stove's, so the cursor finds it); in the
            // town its smoke leaves by the chimney.
            var fireGo = new GameObject("Woodstove fire");
            fireGo.transform.SetParent(world, false);
            fireGo.transform.position = s;
            var col = fireGo.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.size = new Vector3(0.8f, 0.9f, 0.7f);
            fireGo.AddComponent<Fireplace>();
            Transform smoke = fireGo.transform.Find("Smoke");
            if (smoke != null)
            {
                if (inTown) smoke.localPosition = new Vector3(0f, ChimneyTop + 0.2f, 0.1f);
                else Object.Destroy(smoke.gameObject);
            }
            Transform flame = fireGo.transform.Find("FlameLight");
            if (flame != null) flame.localPosition = new Vector3(0f, 0.5f, -0.5f);
        }

        // A kerosene heater by the front door, where Old Nell warms her hands.
        private static void BuildHeater(Transform world, Style st)
        {
            var at = new Vector3(2.7f, 0f, -8.25f);
            var go = new GameObject("Heater");
            go.transform.SetParent(world, false);
            go.transform.position = at;
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0f);
            col.size = new Vector3(0.45f, 0.5f, 0.45f);
            Deco(world, "HeaterBody", m =>
            {
                m.Cylinder(at, 0.2f, 0.42f, 14, DecorPalette.Gloss(new Color(0.20f, 0.30f, 0.22f)));
                m.Cylinder(at + new Vector3(0f, 0.12f, 0f), 0.205f, 0.16f, 14, DecorPalette.Glow(new Color(0.95f, 0.45f, 0.14f)));
                m.Cylinder(at + new Vector3(0f, 0.42f, 0f), 0.16f, 0.04f, 14, st.Metal);
                m.Rod(at + new Vector3(-0.12f, 0.46f, 0f), at + new Vector3(0.12f, 0.46f, 0f), 0.012f, st.Metal);
            }, Vector3.zero);
            go.AddComponent<Heater>();
        }

        /// <summary>Creates point lights and decorative lamps at predefined positions in the hub.</summary>
        private static void BuildLights(Transform world, Style st)
        {
            Vector3[] spots =
            {
                new Vector3(-2f, 2.75f, 5.6f), new Vector3(-6f, 2.75f, 1.5f), new Vector3(3f, 2.75f, 1.8f), new Vector3(-1.5f, 2.75f, -3.3f),
                new Vector3(9f, 2.75f, -2f), new Vector3(10.4f, 2.6f, 6.2f), new Vector3(11.5f, 2.75f, -5.5f), new Vector3(-11f, 2.5f, 6f),
                new Vector3(-11.5f, 2.75f, -4f), new Vector3(5f, 2.75f, -6.5f), new Vector3(-6.5f, 2.75f, -6.5f),
            };
            Surf shade = st.Roadhouse ? DecorPalette.Gloss(new Color(0.12f, 0.24f, 0.15f)) : st.Metal;
            Surf cord = DecorPalette.Matte(new Color(0.06f, 0.06f, 0.06f));
            Surf bulb = DecorPalette.Glow(st.Light);
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 at = spots[i];
                var go = new GameObject("HubLight");
                go.transform.SetParent(world, false);
                go.transform.position = at + Vector3.down * 0.15f;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = i == 7 ? new Color(0.9f, 0.5f, 0.25f) : st.Light;
                l.intensity = i == 7 ? st.LightIntensity * 0.5f : st.LightIntensity;
                l.range = 7.5f;
                l.shadows = i < 4 ? LightShadows.Soft : LightShadows.None;
                // A green enamel pendant on its cord, the bulb under it (drawn where it hangs: the globes once all
                // rendered at the room's origin).
                Deco(world, "Lamp", m =>
                {
                    m.Box(at + new Vector3(0f, (WallH - at.y) * 0.5f + 0.05f, 0f), new Vector3(0.015f, WallH - at.y - 0.1f, 0.015f), cord);
                    m.Cylinder(at + new Vector3(0f, -0.16f, 0f), 0.24f, 0.2f, 12, shade, Quaternion.identity, 0.35f, false);
                    m.Cylinder(at + new Vector3(0f, 0.04f, 0f), 0.09f, 0.05f, 10, shade);
                    m.Sphere(at + new Vector3(0f, -0.12f, 0f), 0.07f, bulb);
                }, at);
            }
        }

        #region Stations
        private static void BuildStations(Transform world, HubDef hub, bool inTown)
        {
            Station(world, "Mott, the barkeep", HubScreens.Trader, TraderCatalog.Barkeep, "Trade", new Vector3(-2f, 0f, 7.4f), new Vector3(-2f, 1.1f, 6.2f), Factions.Freehold, "barkeep",
                ("Ask about work", HubScreens.Quests, TraderCatalog.Barkeep), ("Buy a rumour", "rumours", null), ("Pay the rent", HubScreens.Upkeep, null));
            Station(world, "Vasko, the fence", HubScreens.Trader, TraderCatalog.Fence, "Trade", new Vector3(-10.9f, 0f, 7.2f), new Vector3(-10.8f, 0.8f, 6.2f), Factions.Lantern, "fence",
                ("Ask about work", HubScreens.Quests, TraderCatalog.Fence));
            Station(world, "Dr. Brandt (the Institute)", HubScreens.Trader, TraderCatalog.Institute, "Trade", new Vector3(10.4f, 0f, 7.4f), new Vector3(10.4f, 0.9f, 6.4f), Factions.Institute, "institute",
                ("Ask about contracts", HubScreens.Quests, TraderCatalog.Institute));
            Station(world, "Kasimir, technician", HubScreens.Trader, TraderCatalog.Gunsmith, "Trade", new Vector3(11.3f, 0f, 2.2f), new Vector3(12.2f, 0.9f, 0.5f), Factions.Vanguard, "gunsmith",
                ("Repairs, upgrades & gun builds", HubScreens.Technician, null), ("Ask about work", HubScreens.Quests, TraderCatalog.Gunsmith));
            Station(world, "Sister Agathe, medic", HubScreens.Medic, null, "Get treated", new Vector3(10.4f, 0f, -4.8f), new Vector3(11.4f, 0.5f, -6.2f), Factions.Freehold, "medic",
                ("Trade", HubScreens.Trader, TraderCatalog.Medic), ("Insurance", HubScreens.Insurance, null), ("Ask about work", HubScreens.Quests, TraderCatalog.Medic));
            Station(world, "Old Lev, the guide", HubScreens.Guide, null, "Travel", new Vector3(5.6f, 0f, -6.3f), new Vector3(4.5f, 0.85f, -7f), Factions.Freehold, "guide",
                ("Trade", HubScreens.Trader, TraderCatalog.Guide), ("Ask about work", HubScreens.Quests, TraderCatalog.Guide));
            // In the town the front door is a real door onto the street (KettleDoor); on its own, the door to the Zone opens
            // the deploy screen.
            if (!inTown)
                Station(world, ZoneDoorTitle, HubScreens.Deploy, null, "Head into the Zone", Vector3.zero, new Vector3(0f, 1.2f, -HalfZ + 0.2f), null, null);
            Station(world, "Mercenaries' table", "companion", null, "Hire a companion", new Vector3(-6.5f, 0f, -7.6f), new Vector3(-6.5f, 0.8f, -6.5f), Factions.Freehold, "merc");
            Station(world, "Notice board", HubScreens.Quests, null, "Read the notices", Vector3.zero, new Vector3(-12.6f, 1.6f, -1.5f), null, null);
            Station(world, "Flea market terminal", HubScreens.Flea, null, "Browse the market", Vector3.zero, new Vector3(-12f, 1.05f, -6f), null, null);
            Station(world, "Back door (hideout)", HubScreens.Hideout, null, "Go to the hideout", Vector3.zero, new Vector3(-12.8f, 1.2f, 5f), null, null);
            Station(world, "Landlord's ledger", HubScreens.Upkeep, null, "Rent, debts & family", Vector3.zero, new Vector3(7.5f, 0.85f, -7.8f), null, null);
            Station(world, "Mirror", HubScreens.Character, null, "Look at yourself", Vector3.zero, new Vector3(-8f, 1.6f, -HalfZ + 0.3f), null, null,
                ("Settings", HubScreens.Settings, null), ("Sandbox rules", HubScreens.Sandbox, null));
            Station(world, "Bookshelf", HubScreens.Manual, null, "Read the field manual", Vector3.zero, new Vector3(-3f, 1.2f, -HalfZ + 0.5f), null, null);

            // The stash locker: a bodiless container wrapped round the profile's stash.
            var stashGo = new GameObject("StashContainer");
            stashGo.transform.SetParent(world, false);
            stashGo.transform.position = new Vector3(-12.2f, 1f, 2.5f);
            var box = stashGo.AddComponent<LootContainer>();
            box.DisplayName = "Stash";
            box.Type = ContainerType.Locker;
            box.Contents = ProfileStore.StashInventory ?? new Inventory(9999f);
            box.Searched = true;
            box.FixtureKey = "hub_stash";
            var locker = stashGo.AddComponent<HubStashLocker>();
            locker.Container = box;
            locker.Point = new Vector3(-12.2f, 1.2f, 2.5f);
        }

        private static void Station(Transform world, string title, string screen, string arg, string verb, Vector3 keeperPos, Vector3 point,
            string faction, string keeper, params (string, string, string)[] extra)
        {
            var go = new GameObject("Station " + title);
            go.transform.SetParent(world, false);
            go.transform.position = point;
            var s = go.AddComponent<HubStation>();
            s.Title = title;
            s.Screen = screen;
            s.Arg = arg;
            s.Verb = verb;
            s.Point = point;
            if (extra != null) s.Extra.AddRange(extra);
            if (keeper != null) HubNpc.Spawn(world, keeperPos, point, title.Split(',')[0], faction, HubNpc.Role.Keeper, keeper);
        }
        #endregion

        #region Patrons
        private static void BuildPatrons(Transform world, HubDef hub)
        {
            HubNpc.Spawn(world, new Vector3(9f, 0.24f, -2f), new Vector3(9f, 0.24f, -4f), "Yuri", Factions.Freehold, HubNpc.Role.Musician, "guitar", seated: true);
            HubNpc.Spawn(world, new Vector3(-6.95f, 0f, 1.5f), new Vector3(-6f, 0.8f, 1.5f), "One-Eyed Pell", Factions.Freehold, HubNpc.Role.Storyteller, "story_pell", seated: true);
            HubNpc.Spawn(world, new Vector3(3.95f, 0f, 1.8f), new Vector3(3f, 0.8f, 1.8f), "Dana", Factions.Freehold, HubNpc.Role.Storyteller, "story_dana", seated: true);
            HubNpc.Spawn(world, new Vector3(-1.5f, 0f, 1.45f), new Vector3(-1.5f, 0.8f, 0.5f), "Gorsh", Factions.Lantern, HubNpc.Role.Patron, "patron", seated: true);
            HubNpc.Spawn(world, new Vector3(-4f, 0f, -2.55f), new Vector3(-4f, 0.8f, -3.5f), "Kit", Factions.Vanguard, HubNpc.Role.Patron, "patron", seated: true);
            HubNpc.Spawn(world, new Vector3(-3f, 0.76f, 5.3f), new Vector3(-3f, 1.1f, 6.2f), "Bram", Factions.Freehold, HubNpc.Role.Patron, "patron", seated: true);
            HubNpc.Spawn(world, new Vector3(1.6f, 0f, -8.1f), new Vector3(2.7f, 0.4f, -8.25f), "Old Nell", Factions.Freehold, HubNpc.Role.Beggar, "beggar");
            HubNpc.Spawn(world, new Vector3(6.6f, 0f, -2.6f), new Vector3(5.4f, 0.8f, -0.9f), "Lisko", Factions.Freehold, HubNpc.Role.Patron, "patron");
        }
        #endregion

        #region In the town
        // The pieces that kept a point in world space while the hub was built at the origin: theirs move with it.
        private static void Relocate(Transform root)
        {
            foreach (HubStation s in root.GetComponentsInChildren<HubStation>(true)) s.Point = root.TransformPoint(s.Point);
            foreach (HubStashLocker l in root.GetComponentsInChildren<HubStashLocker>(true)) l.Point = root.TransformPoint(l.Point);
            foreach (HubNpc n in root.GetComponentsInChildren<HubNpc>(true)) n.Relocate(root);
        }

        // The room under its roof, as boxes the shelter can test (each the bounds of a cell of the turned room), so the
        // wind and the snow stay outside.
        private static void RegisterShelter(Transform root)
        {
            const int Nx = 10, Nz = 8;
            float cw = HalfX * 2f / Nx, cd = HalfZ * 2f / Nz;
            for (int iz = 0; iz < Nz; iz++)
                for (int ix = 0; ix < Nx; ix++)
                {
                    var c = new Vector3(-HalfX + (ix + 0.5f) * cw, WallH * 0.5f, -HalfZ + (iz + 0.5f) * cd);
                    var b = new Bounds(root.TransformPoint(c), Vector3.zero);
                    for (int k = 0; k < 8; k++)
                        b.Encapsulate(root.TransformPoint(c + new Vector3((k & 1) == 0 ? -cw : cw, (k & 2) == 0 ? -WallH : WallH, (k & 4) == 0 ? -cd : cd) * 0.5f));
                    Shelter.Register(b);
                }
        }

        /// <summary>True when a point (world space) is inside the hub's walls, given its root.</summary>
        public static bool Inside(Transform root, Vector3 p, float margin = 0f)
        {
            if (root == null) return false;
            Vector3 l = root.InverseTransformPoint(p);
            return Mathf.Abs(l.x) < HalfX + margin && Mathf.Abs(l.z) < HalfZ + margin;
        }
        #endregion
    }
}
