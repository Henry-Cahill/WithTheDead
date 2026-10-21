using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Rules that hold in the hub: a safe place. No fighting, no hunger ticking down while you shop.</summary>
    public static class HubRules
    {
        public static bool SafeZone;
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
                // Special inspection for the zone door
                if (Title == "Door to the Zone")
                {
                    into.Add(new InteractAction("Inspect door", c => GameEvents.RaiseToast(
                        "The door is locked tight, but you can hear the cold wind outside.")));
                }
            }
            // Toggle safe zone action for the zone door
            if (Title == "Door to the Zone")
            {
                into.Add(new InteractAction("Toggle safe zone", c =>
                {
                    HubRules.SafeZone = !HubRules.SafeZone;
                    GameEvents.RaiseToast($"Safe zone is now {(HubRules.SafeZone ? "ON" : "OFF")}." );
                }));
            }
        }


        private static void Open(string screen, string arg)
        {
            Sfx.PlayUI(SfxIds.UiClick);
            if (HubScreens.Has(screen)) HubScreens.Open(screen, arg);
            else GameEvents.RaiseToast("Nobody's behind the counter right now.");
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
    /// every station with its keeper, the stash locker, the door out to the Zone, and the patrons who make it
    /// feel like home. Three hubs share the plan with their own materials and light.
    /// </summary>
    public static class HubWorld
    {
        private const float HalfX = 13f, HalfZ = 9f, WallH = 3.2f, WallT = 0.3f;

        public sealed class Result
        {
            public Vector3 PlayerSpawn;
            public float PlayerYaw;
        }

        private struct Style
        {
            public Surf Floor, Wall, Trim, Wood, DarkWood, Metal;
            public Color Light, Ambient, Fog;
            public float LightIntensity;
            public string Sign;
        }

        public static Result Build(Transform world, HubDef hub)
        {
            Style st = StyleFor(hub);
            var overhead = new GameObject("HubOverhead").transform;
            CeilingVisibility.Clear();
            overhead.gameObject.AddComponent<CeilingVisibility>();

            BuildShell(world, overhead, st);
            BuildBar(world, st);
            BuildFurniture(world, st);
            BuildLights(world, st);
            DecorPalette.Apply();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = st.Ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = st.Fog;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 70f;

            BuildStations(world, hub);
            BuildPatrons(world, hub);
            return new Result { PlayerSpawn = new Vector3(0f, 0.1f, -6.8f), PlayerYaw = 0f };
        }

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
                        Light = new Color(0.86f, 0.95f, 1f), LightIntensity = 1.4f,
                        Ambient = new Color(0.20f, 0.22f, 0.22f), Fog = new Color(0.06f, 0.07f, 0.07f),
                        Sign = "DAM BUNKER  /  VANGUARD",
                    };
                default:
                    return new Style
                    {
                        Floor = DecorMaterials.WoodFloor(new Color(0.40f, 0.28f, 0.18f)),
                        Wall = DecorMaterials.WallInterior(RaidLayouts.ArmyMaroon),
                        Trim = DecorPalette.Matte(new Color(0.28f, 0.20f, 0.13f)),
                        Wood = DecorPalette.Matte(new Color(0.44f, 0.30f, 0.18f)),
                        DarkWood = DecorPalette.Matte(new Color(0.22f, 0.15f, 0.10f)),
                        Metal = DecorPalette.Metal(new Color(0.55f, 0.50f, 0.42f)),
                        Light = new Color(1f, 0.70f, 0.40f), LightIntensity = 1.8f,
                        Ambient = new Color(0.22f, 0.18f, 0.14f), Fog = new Color(0.07f, 0.06f, 0.05f),
                        Sign = "THE RUSTY KETTLE",
                    };
            }
        }

        #region Geometry helpers
        // A solid piece: rendered (so the navmesh bake carves it) and collidable. Geometry is drawn in world
        // space and emitted at its centre, so the object's local space is centred on the piece.
        private static GameObject Solid(Transform parent, string name, Vector3 center, Vector3 size, in Surf s, float yaw = 0f)
        {
            var m = new RaidMesh();
            m.Box(center, size, s);
            GameObject go = m.Emit(name, parent, center);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            return go;
        }

        // A decorative piece with no collider (geometry in world space, emitted at origin).
        private static GameObject Deco(Transform parent, string name, System.Action<RaidMesh> draw, Vector3 origin)
        {
            var m = new RaidMesh();
            draw(m);
            return m.Emit(name, parent, origin);
        }
        #endregion

        private static void BuildShell(Transform world, Transform overhead, Style st)
        {
            Solid(world, "Floor", new Vector3(0f, -0.15f, 0f), new Vector3(HalfX * 2f + 2f, 0.3f, HalfZ * 2f + 2f), st.Floor);
            // Walls, leaving the door gap in the south wall.
            Solid(world, "Wall_N", new Vector3(0f, WallH * 0.5f, HalfZ), new Vector3(HalfX * 2f + WallT, WallH, WallT), st.Wall);
            Solid(world, "Wall_E", new Vector3(HalfX, WallH * 0.5f, 0f), new Vector3(WallT, WallH, HalfZ * 2f), st.Wall);
            Solid(world, "Wall_W", new Vector3(-HalfX, WallH * 0.5f, 0f), new Vector3(WallT, WallH, HalfZ * 2f), st.Wall);
            Solid(world, "Wall_S1", new Vector3(-(HalfX + 1.1f) * 0.5f, WallH * 0.5f, -HalfZ), new Vector3(HalfX - 1.1f, WallH, WallT), st.Wall);
            Solid(world, "Wall_S2", new Vector3((HalfX + 1.1f) * 0.5f, WallH * 0.5f, -HalfZ), new Vector3(HalfX - 1.1f, WallH, WallT), st.Wall);
            Solid(world, "Lintel", new Vector3(0f, WallH - 0.4f, -HalfZ), new Vector3(2.2f, 0.8f, WallT), st.Wall);
            // The door to the Zone, shut against the cold (it's the deploy station).
            Solid(world, "ZoneDoor", new Vector3(0f, 1.2f, -HalfZ - 0.05f), new Vector3(2.0f, 2.4f, 0.12f), st.DarkWood);
            // Subtle metal latch detail on the upper‑right edge of the door
            Deco(world, "DoorLatch", m => {
                // metal hinge plate
                m.Box(new Vector3(0.9f, 1.78f, -HalfZ - 0.07f), new Vector3(0.02f, 0.1f, 0.005f), st.Metal);
                // latch bar
                m.Cylinder(new Vector3(0.85f, 1.74f, -HalfZ - 0.07f), 0.015f, 0.03f, 12, st.Metal);
            }, Vector3.zero);
            Deco(world, "DoorKnob", m => m.Cylinder(new Vector3(0.7f, 1.2f, -HalfZ - 0.07f), 0.04f, 0.02f, 12, st.Metal), Vector3.zero);
            // Skirting and a sign over the bar.
            Deco(world, "Trim", m =>
            {
                m.Box(new Vector3(0f, 0.06f, HalfZ - 0.17f), new Vector3(HalfX * 2f, 0.12f, 0.04f), st.Trim);
                m.Box(new Vector3(HalfX - 0.17f, 0.06f, 0f), new Vector3(0.04f, 0.12f, HalfZ * 2f), st.Trim);
                m.Box(new Vector3(-HalfX + 0.17f, 0.06f, 0f), new Vector3(0.04f, 0.12f, HalfZ * 2f), st.Trim);
                // Add subtle metal nail heads (tiny rivets) along the bottom edge of the trim for a weathered look
                for (int i = 0; i < 12; i++)
                {
                    float x = -HalfX + 0.2f + i * ((HalfX * 2f - 0.4f) / 11f);
                    // place just above floor, centered on the trim edge
                    m.Cylinder(new Vector3(x, 0.03f, HalfZ - 0.18f), 0.01f, 0.005f, 12, st.Metal);
                }
            }, Vector3.zero);
            Surf sign = DecorMaterials.Sign(st.Sign, new Color(0.12f, 0.08f, 0.05f), new Color(0.95f, 0.75f, 0.35f), out float aspect);
            Deco(world, "Sign", m => m.Upright(new Vector3(0f, 2.75f, HalfZ - 0.18f), Vector3.back, 0.5f * aspect, 0.5f, sign), Vector3.zero);
            // The ceiling: shown from the shoulder and first-person views only (the isometric view looks in).
            GameObject ceiling = Deco(overhead, "Ceiling", m => m.Box(new Vector3(0f, WallH + 0.05f, 0f), new Vector3(HalfX * 2f, 0.1f, HalfZ * 2f), DecorMaterials.Ceiling), Vector3.zero);
            CeilingVisibility.Register(ceiling.GetComponent<Renderer>());
            Shelter.Register(new Bounds(new Vector3(0f, WallH * 0.5f, 0f), new Vector3(HalfX * 2f, WallH, HalfZ * 2f)));
        }

        private static void BuildBar(Transform world, Style st)
        {
            Solid(world, "BarCounter", new Vector3(-2f, 0.55f, 6.2f), new Vector3(10f, 1.1f, 0.8f), st.DarkWood);
            Deco(world, "BarTop", m =>
{
    m.Box(new Vector3(-2f, 1.12f, 6.2f), new Vector3(10.2f, 0.05f, 0.9f), st.Wood);
    for (int i = 0; i < 8; i++)
    {
        float x = -6.5f + i * 1.5f;
        // Place a small metal rivet along the bar edge
        m.Cylinder(new Vector3(x, 1.13f, 6.2f), 0.02f, 0.02f, 12, st.Metal);
    }
}, Vector3.zero);
            Solid(world, "BackShelf", new Vector3(-2f, 1.2f, HalfZ - 0.35f), new Vector3(9f, 2.4f, 0.4f), st.Wood);
            var rng = new System.Random(7);
            Deco(world, "Bottles", m =>
            {
                for (int i = 0; i < 46; i++)
                {
                    float x = -6.3f + (float)rng.NextDouble() * 8.6f;
                    float y = 0.55f + (rng.Next(3)) * 0.65f;
                    var c = new Color(0.2f + (float)rng.NextDouble() * 0.5f, 0.3f + (float)rng.NextDouble() * 0.4f, 0.15f + (float)rng.NextDouble() * 0.3f);
                    m.Cylinder(new Vector3(x, y, HalfZ - 0.48f), 0.04f, 0.26f + (float)rng.NextDouble() * 0.1f, 6, DecorPalette.Glass(c));
                }
            }, Vector3.zero);
            for (int i = 0; i < 6; i++)
                {
                    var pos = new Vector3(-6f + i * 1.6f, 0.38f, 5.3f);
                    Solid(world, "BarStool", pos, new Vector3(0.38f, 0.76f, 0.38f), st.Metal);
                    var footPos = new Vector3(-6f + i * 1.6f, 0.01f, 5.3f);
                    Deco(world, "BarStoolFoot", m => m.Cylinder(footPos, 0.04f, 0.02f, 12, st.Metal), Vector3.zero);
        }

    }


        private static void BuildFurniture(Transform world, Style st)
        {
            // Patron tables with chairs.
            Vector3[] tables = { new Vector3(-6f, 0f, 1.5f), new Vector3(-1.5f, 0f, 0.5f), new Vector3(3f, 0f, 1.8f), new Vector3(-4f, 0f, -3.5f), new Vector3(1.5f, 0f, -3.2f) };
            foreach (Vector3 t in tables)
            {
                Solid(world, "Table", t + new Vector3(0f, 0.38f, 0f), new Vector3(1.2f, 0.76f, 1.2f), st.Wood);
                foreach (Vector3 off in new[] { new Vector3(0.95f, 0f, 0f), new Vector3(-0.95f, 0f, 0f), new Vector3(0f, 0f, 0.95f) })
                    Solid(world, "Chair", t + off + new Vector3(0f, 0.23f, 0f), new Vector3(0.45f, 0.46f, 0.45f), st.DarkWood);
            }
            // The stage corner with a stool.
            Solid(world, "Stage", new Vector3(9f, 0.12f, -2f), new Vector3(3.2f, 0.24f, 3.2f), st.DarkWood);
            // Fence's corner table, institute desk, gunsmith bench, medic cot, guide's map table.
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
            Solid(world, "HideoutDoor", new Vector3(-12.83f, 1.15f, 5f), new Vector3(0.08f, 2.3f, 1.1f), st.DarkWood);
            Solid(world, "LedgerDesk", new Vector3(7.5f, 0.4f, -7.8f), new Vector3(1.4f, 0.8f, 0.8f), st.DarkWood);
            Solid(world, "Bookshelf", new Vector3(-3f, 1f, -HalfZ + 0.3f), new Vector3(1.6f, 2f, 0.35f), st.Wood);
            Deco(world, "Mirror", m => m.Box(new Vector3(-8f, 1.6f, -HalfZ + 0.18f), new Vector3(0.9f, 1.4f, 0.03f), DecorPalette.Glass(new Color(0.75f, 0.82f, 0.85f))), Vector3.zero);
            Deco(world, "Rugs", m =>
            {
                m.Box(new Vector3(-1.5f, 0.005f, -1.5f), new Vector3(5f, 0.01f, 3.4f), DecorMaterials.Rug(1));
                m.Box(new Vector3(9f, 0.25f, -2f), new Vector3(2.4f, 0.01f, 2.4f), DecorMaterials.Rug(2));
            }, Vector3.zero);
            // Fireplace near the western wall, provides warmth when lit
            var fireGo = new GameObject("Fireplace");
            fireGo.transform.SetParent(world, false);
            fireGo.transform.position = new Vector3(-HalfX + 0.8f, 0f, -2f);
            fireGo.AddComponent<BoxCollider>();
            fireGo.AddComponent<Fireplace>();
        }

        private static void BuildLights(Transform world, Style st)
        {
            Vector3[] spots =
            {
                new Vector3(-2f, 2.9f, 5.6f), new Vector3(-6f, 2.9f, 1.5f), new Vector3(3f, 2.9f, 1.8f), new Vector3(-1.5f, 2.9f, -3.3f),
                new Vector3(9f, 2.9f, -2f), new Vector3(10.4f, 2.6f, 6.2f), new Vector3(11.5f, 2.9f, -5.5f), new Vector3(-11f, 2.4f, 6f),
                new Vector3(-11.5f, 2.9f, -4f), new Vector3(5f, 2.9f, -6.5f),
            };
            for (int i = 0; i < spots.Length; i++)
            {
                var go = new GameObject("HubLight");
                go.transform.SetParent(world, false);
                go.transform.position = spots[i];
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = i == 7 ? new Color(0.9f, 0.5f, 0.25f) : st.Light;
                l.intensity = i == 7 ? st.LightIntensity * 0.5f : st.LightIntensity;
                l.range = 7.5f;
                l.shadows = i < 4 ? LightShadows.Soft : LightShadows.None;
                Vector3 lampAt = spots[i] + Vector3.up * 0.15f;
                Deco(world, "Lamp", m => m.Ellipsoid(lampAt, new Vector3(0.28f, 0.2f, 0.28f), DecorPalette.Glow(st.Light)), lampAt);
            }
        }

        #region Stations
        private static void BuildStations(Transform world, HubDef hub)
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
            Station(world, "Door to the Zone", HubScreens.Deploy, null, "Head into the Zone", Vector3.zero, new Vector3(0f, 1.2f, -HalfZ + 0.2f), null, null);
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
            HubNpc.Spawn(world, new Vector3(1.6f, 0f, -8.1f), new Vector3(0f, 1f, -8f), "Old Nell", Factions.Freehold, HubNpc.Role.Beggar, "beggar");
            HubNpc.Spawn(world, new Vector3(6.5f, 0f, 3.8f), new Vector3(3f, 0.8f, 1.8f), "Lisko", Factions.Freehold, HubNpc.Role.Patron, "patron");
        }
        #endregion
    }
}
