using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The wave-survival starter home, finished to the extraction town's standard. The house is still
    /// raised from ordinary <see cref="BuildPiece"/>s — so it saves, carves the navmesh, is battered down
    /// by zombies and can be dismantled by the survivor (though, raised for free, it yields no materials)
    /// — but a piece standing in one of the home's slots
    /// builds the finished look instead of rough planking: clapboard siding, corner boards and trims
    /// outside; wainscot, wallpaper, baseboards and chair rails inside; cased, glazed windows with
    /// curtains, shutters and flower boxes; panelled doors; wood, linoleum and tiled floors with rugs;
    /// pictures, a clock, kitchen cupboards, light switches and lamps. The blueprint is keyed by
    /// position, so a restored save — or a piece rebuilt in the same slot — finishes itself the same way.
    /// Everything a household has is usable (see <see cref="Interactable"/>): the doors open, close and
    /// lock, the windows open and draw their curtains, the taps run, the cupboards and drawers hold
    /// things, the lights switch on and off — the per-area partial files (<c>StarterHome.Doors.cs</c>,
    /// <c>.Windows.cs</c>, <c>.Kitchen.cs</c>, <c>.Lighting.cs</c>, <c>.Bathroom.cs</c>) build those fittings.
    /// This class also lays out the home's loose furniture (<see cref="HomeFurniture"/>), the yard and
    /// the street out front. The house stays open-topped, so the isometric camera looks straight in.
    /// </summary>
    internal static partial class StarterHome
    {
        #region State
        /// <summary>True while the world is the wave-survival world. Only then do house pieces in the
        /// home's slots take its finished look (pieces a survivor builds during a raid stay rough).</summary>
        public static bool Active { get; private set; }

        /// <summary>Call before the world's house pieces are spawned or restored.</summary>
        public static void SetActive(bool active) => Active = active;
        #endregion

        #region Blueprint
        private const float Tile = CloneZConfig.HouseTileSize;
        private const float WallH = CloneZConfig.HouseWallHeight;
        private const float Half = CloneZConfig.HouseWallThickness * 0.5f;
        private const float FloorTop = CloneZConfig.HouseFloorThickness;

        /// <summary>The house outline on the wall centre lines: 6 × 6 m, x 1..7, z 3..9.</summary>
        private static readonly Rect Outline = Rect.MinMaxRect(1f, 3f, 7f, 9f);

        /// <summary>One room of the home: its rectangle on the wall centre lines, wall finish and curtains.</summary>
        internal sealed class HomeRoom
        {
            public string Name;
            public Rect Rect;
            public WallDesign Design;
            public Color Curtain;
        }

        // Cream beadboard under sage-striped paper in the living room; plain utility paint in the store
        // room. Both lived in but kept up — far fresher than the abandoned raid town.
        private static readonly WallDesign LivingWalls = new WallDesign
        {
            Key = "home_living", Wainscot = new Color(0.80f, 0.77f, 0.68f), Paper = new Color(0.55f, 0.63f, 0.50f),
            Trim = new Color(0.86f, 0.84f, 0.77f), Lower = WainscotStyle.Beadboard, Upper = PaperStyle.Stripes, RailHeight = 0.92f, Wear = 0.3f,
        };

        private static readonly WallDesign StoreWalls = new WallDesign
        {
            Key = "home_store", Wainscot = new Color(0.40f, 0.43f, 0.38f), Paper = new Color(0.74f, 0.72f, 0.63f),
            Trim = new Color(0.30f, 0.30f, 0.28f), Lower = WainscotStyle.Paint, Upper = PaperStyle.Plain, RailHeight = 1.1f, Wear = 0.45f,
        };

        // Glossy white panelling under pale aqua paint in the bathroom.
        private static readonly WallDesign BathWalls = new WallDesign
        {
            Key = "home_bath", Wainscot = new Color(0.88f, 0.88f, 0.85f), Paper = new Color(0.62f, 0.76f, 0.74f),
            Trim = new Color(0.92f, 0.92f, 0.90f), Lower = WainscotStyle.Panels, Upper = PaperStyle.Plain, RailHeight = 1.15f, Wear = 0.2f,
        };

        private static readonly HomeRoom Living = new HomeRoom
        {
            Name = "Living Room", Rect = Rect.MinMaxRect(1f, 3f, 7f, 7f), Design = LivingWalls, Curtain = new Color(0.56f, 0.20f, 0.15f),
        };

        private static readonly HomeRoom Store = new HomeRoom
        {
            Name = "Store Room", Rect = Rect.MinMaxRect(1f, 7f, 5f, 9f), Design = StoreWalls, Curtain = new Color(0.64f, 0.60f, 0.48f),
        };

        private static readonly HomeRoom Bath = new HomeRoom
        {
            Name = "Bathroom", Rect = Rect.MinMaxRect(5f, 7f, 7f, 9f), Design = BathWalls, Curtain = new Color(0.70f, 0.82f, 0.84f),
        };

        private static readonly HomeRoom[] Rooms = { Living, Store, Bath };

        /// <summary>The room containing world XZ (x, z), or null outside the home.</summary>
        private static HomeRoom RoomAt(float x, float z)
        {
            for (int i = 0; i < Rooms.Length; i++)
            {
                Rect r = Rooms[i].Rect;
                if (x > r.xMin + 0.01f && x < r.xMax - 0.01f && z > r.yMin + 0.01f && z < r.yMax - 0.01f) return Rooms[i];
            }
            return null;
        }

        private readonly struct Slot
        {
            public readonly BuildPieceType Type;
            public readonly float X, Z, Yaw;
            public Slot(BuildPieceType type, float x, float z, float yaw) { Type = type; X = x; Z = z; Yaw = yaw; }
        }

        /// <summary>The bathroom's partition: a doorway between the store room and the bathroom. Added after the
        /// home was first laid out, so older saves gain it on load if its place is clear (see <see cref="MigrateToFixtureLayout"/>).
        /// Declared before <see cref="Slots"/>, whose initializer reads it.</summary>
        private static readonly Slot BathroomPartition = new Slot(BuildPieceType.Doorway, 5f, 8f, 90f);

        // The house as a fresh run raises it: a 3 × 3 floor of 2 m tiles, a street front with a doorway
        // between two windows, a window in each other outside wall, a partition with an interior doorway
        // splitting the living room (south) from the store room (north), and a second partition with a
        // doorway closing a small bathroom off the store room's east end.
        private static readonly Slot[] Slots =
        {
            new Slot(BuildPieceType.Floor, 2f, 4f, 0f), new Slot(BuildPieceType.Floor, 4f, 4f, 0f), new Slot(BuildPieceType.Floor, 6f, 4f, 0f),
            new Slot(BuildPieceType.Floor, 2f, 6f, 0f), new Slot(BuildPieceType.Floor, 4f, 6f, 0f), new Slot(BuildPieceType.Floor, 6f, 6f, 0f),
            new Slot(BuildPieceType.Floor, 2f, 8f, 0f), new Slot(BuildPieceType.Floor, 4f, 8f, 0f), new Slot(BuildPieceType.Floor, 6f, 8f, 0f),

            new Slot(BuildPieceType.Window, 2f, 3f, 0f), new Slot(BuildPieceType.Doorway, 4f, 3f, 0f), new Slot(BuildPieceType.Window, 6f, 3f, 0f),
            new Slot(BuildPieceType.Wall, 2f, 9f, 0f), new Slot(BuildPieceType.Window, 4f, 9f, 0f), new Slot(BuildPieceType.Wall, 6f, 9f, 0f),
            new Slot(BuildPieceType.Wall, 1f, 4f, 90f), new Slot(BuildPieceType.Window, 1f, 6f, 90f), new Slot(BuildPieceType.Wall, 1f, 8f, 90f),
            new Slot(BuildPieceType.Wall, 7f, 4f, 90f), new Slot(BuildPieceType.Window, 7f, 6f, 90f), new Slot(BuildPieceType.Wall, 7f, 8f, 90f),
            new Slot(BuildPieceType.Wall, 2f, 7f, 0f), new Slot(BuildPieceType.Doorway, 4f, 7f, 0f), new Slot(BuildPieceType.Wall, 6f, 7f, 0f),
            BathroomPartition,
        };
        #endregion

        #region Layout
        /// <summary>Where a fresh run puts the bed: head against the front wall in the bedroom corner.</summary>
        public static readonly Vector3 BedSpot = new Vector3(6.0f, 0f, 4.26f);
        /// <summary>The stove, at the end of the kitchen run along the front wall.</summary>
        public static readonly Vector3 StoveSpot = new Vector3(2.72f, 0f, 3.45f);
        /// <summary>The kitchen sink (the plumbed tap), under the front window.</summary>
        public static readonly Vector3 SinkSpot = new Vector3(1.72f, 0f, 3.44f);
        /// <summary>The rain barrel, outside under the west window.</summary>
        public static readonly Vector3 BarrelSpot = new Vector3(0.5f, 0f, 6f);
        /// <summary>The supply crate, in the store room's corner by the kitchen partition, beside the breaker panel: the
        /// shelving and the boxes along the back wall keep their fronts clear, and the way from the door through to the
        /// bathroom stays open, so the dead can still follow the survivor in.</summary>
        private static readonly Vector3 CrateSpot = new Vector3(1.62f, 0f, 7.47f);

        // Where the bare shelter of older saves kept its fittings (before the home was furnished).
        private static readonly Vector3 OldBedSpot = new Vector3(5f, 0f, 5f);
        private static readonly Vector3 OldStoveSpot = new Vector3(2f, 0f, 5.5f);
        private static readonly Vector3 OldSinkSpot = new Vector3(2f, 0f, 3.6f);
        private static readonly Vector3 OldCrateSpot = new Vector3(5f, 0f, 8f);

        // Where the furnished home kept things before it had a bathroom and a fridge (see MigrateToFixtureLayout).
        private static readonly Vector3 FurnishedCrateSpot = new Vector3(5.55f, 0f, 8.52f);
        private static readonly Item FurnishedEastShelf = new Item(HomeFurnitureKind.Shelf, 6.655f, 7.85f, 270f);
        private static readonly Item FurnishedBoxes = new Item(HomeFurnitureKind.Boxes, 1.42f, 7.6f, 90f);
        private static readonly Item FurnishedDiningChair = new Item(HomeFurnitureKind.Chair, 2.68f, 6.15f, 270f);

        private readonly struct Item
        {
            public readonly HomeFurnitureKind Kind;
            public readonly float X, Z, Yaw;
            public Item(HomeFurnitureKind kind, float x, float z, float yaw) { Kind = kind; X = x; Z = z; Yaw = yaw; }
        }

        // The loose furniture a fresh run starts with (and an older save is given). The hall from the
        // front door to the store room (x 3.3..4.7) stays clear, as does each door leaf's swing; the store
        // room's door swings in toward the back window and the bathroom's in toward its south wall.
        private static readonly Item[] Layout =
        {
            // Kitchen corner: a worktop along the west wall, round the corner from the sink and stove, with
            // the kitchen radio on it, and the fridge against the partition by the hall.
            new Item(HomeFurnitureKind.Counter, 1.435f, 4.37f, 90f),
            new Item(HomeFurnitureKind.Radio, 1.36f, 4.62f, 90f),
            new Item(HomeFurnitureKind.Fridge, 2.93f, 6.55f, 180f),
            // Dining: a table under the west window with two chairs.
            new Item(HomeFurnitureKind.Table, 1.95f, 6.15f, 90f),
            new Item(HomeFurnitureKind.Chair, 2.68f, 5.82f, 270f),
            new Item(HomeFurnitureKind.Chair, 1.95f, 5.3f, 0f),
            // Bedroom corner: a bedside lamp, a dresser under the east window with a portable TV facing the
            // bed, a reading chair with a standard lamp, and a bookcase against the partition.
            new Item(HomeFurnitureKind.Nightstand, 5.0f, 3.33f, 0f),
            new Item(HomeFurnitureKind.Dresser, 6.63f, 6.0f, 270f),
            new Item(HomeFurnitureKind.Tv, 6.60f, 5.85f, 180f),
            new Item(HomeFurnitureKind.Armchair, 5.35f, 5.95f, 270f),
            new Item(HomeFurnitureKind.FloorLamp, 6.08f, 6.2f, 0f),
            new Item(HomeFurnitureKind.Bookshelf, 5.85f, 6.72f, 180f),
            // Store room: steel shelving and a stack of boxes along the back wall (the supply crate stands in the corner
            // by the kitchen partition). The middle of the room stays clear: it's the way through to the bathroom, and the
            // dead must have room to follow the survivor in.
            new Item(HomeFurnitureKind.Shelf, 1.85f, 8.655f, 180f),
            new Item(HomeFurnitureKind.Boxes, 2.78f, 8.63f, 180f),
            // Bathroom: a toilet against the east wall, a shower in the back corner and a basin on the back wall.
            new Item(HomeFurnitureKind.Toilet, 6.55f, 7.55f, 270f),
            new Item(HomeFurnitureKind.Shower, 6.45f, 8.45f, 270f),
            new Item(HomeFurnitureKind.Basin, 5.55f, 8.675f, 180f),
        };

        /// <summary>Raises the home's floors and walls — and its supply crate — under <paramref name="world"/>.
        /// The pieces are <see cref="BuildPiece.Prebuilt"/>: the survivor can pull them down, but not for planks.
        /// Does nothing when a restored save has already rebuilt the house (or the survivor's edits to it).</summary>
        public static void Raise(Transform world)
        {
            if (BuildPiece.All.Count > 0) return;
foreach (Slot s in Slots)
            {
                var piece = BuildPiece.Spawn(world, s.Type, BuildPiece.CenterFor(s.Type, new Vector3(s.X, 0f, s.Z)), s.Yaw, 1f, prebuilt: true);
                // Add subtle metal corner‑cap to exterior walls
                if (s.Type == BuildPieceType.Wall)
                {
                    int seed = StarterHome.SlotSeed(s.Type, s.X, s.Z);
                    UnityEngine.Random.InitState(seed);
                    var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cap.name = "WallCap";
                    cap.transform.SetParent(piece.transform, false);
                    Vector3 capOffset = s.Yaw switch
                    {
                        0f => new Vector3(0f, WallH - 0.04f, 0.36f),
                        180f => new Vector3(0f, WallH - 0.04f, -0.36f),
                        90f => new Vector3(0.36f, WallH - 0.04f, 0f),
                        270f => new Vector3(-0.36f, WallH - 0.04f, 0f),
                        _ => new Vector3(0f, WallH - 0.04f, 0.36f)
                    };
                    // Apply tiny random jitter ±0.01 on X and Z
                    capOffset.x += (UnityEngine.Random.value - 0.5f) * 0.02f;
                    capOffset.z += (UnityEngine.Random.value - 0.5f) * 0.02f;
                    cap.transform.localPosition = capOffset;
                    cap.transform.localScale = new Vector3(0.04f, 0.02f, 0.02f);
                    var rend = piece.GetComponentInChildren<Renderer>();
                    if (rend != null) cap.GetComponent<Renderer>().sharedMaterial = rend.sharedMaterial;
                }
                if (s.Type == BuildPieceType.Doorway)
                {
                    int seed = StarterHome.SlotSeed(s.Type, s.X, s.Z);
                    UnityEngine.Random.InitState(seed);
var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                      handle.name = "Handle";
                      handle.transform.SetParent(piece.transform, false);
                      Vector3 offset = s.Yaw switch
                      {
                          0f => new Vector3(0.35f, 0.9f, 0f),
                          90f => new Vector3(0f, 0.9f, 0.35f),
                          180f => new Vector3(-0.35f, 0.9f, 0f),
                          270f => new Vector3(0f, 0.9f, -0.35f),
                          _ => new Vector3(0.35f, 0.9f, 0f)
                      };
                      offset.x += (UnityEngine.Random.value - 0.5f) * 0.02f;
                      offset.z += (UnityEngine.Random.value - 0.5f) * 0.02f;
                      handle.transform.localPosition = offset;
                      handle.transform.localScale = new Vector3(0.04f, 0.02f, 0.02f);
                      var rend = piece.GetComponentInChildren<Renderer>();
                      if (rend != null) handle.GetComponent<Renderer>().sharedMaterial = rend.sharedMaterial;
// Add metal strike‑plate above the handle
                     var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                     plate.name = "Plate";
                     plate.transform.SetParent(piece.transform, false);
                     Vector3 plateOffset = s.Yaw switch
                     {
                         0f => new Vector3(0.35f, 0.95f, 0f),
                         90f => new Vector3(0f, 0.95f, 0.35f),
                         180f => new Vector3(-0.35f, 0.95f, 0f),
                         270f => new Vector3(0f, 0.95f, -0.35f),
                         _ => new Vector3(0.35f, 0.95f, 0f)
                     };
                     plate.transform.localPosition = plateOffset;
                     plate.transform.localScale = new Vector3(0.06f, 0.02f, 0.01f);
                     if (rend != null) plate.GetComponent<Renderer>().sharedMaterial = rend.sharedMaterial;

}
                    
                    // Add wooden latch to windows
                    if (s.Type == BuildPieceType.Window)
                    {
                        UnityEngine.Random.InitState(StarterHome.SlotSeed(s.Type, s.X, s.Z));
                        var latch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        latch.name = "Latch";
                        latch.transform.SetParent(piece.transform, false);
                        Vector3 latchOffset = s.Yaw switch
                        {
                            0f => new Vector3(0.35f, 0.9f, 0f),
                            90f => new Vector3(0f, 0.9f, 0.35f),
                            180f => new Vector3(-0.35f, 0.9f, 0f),
                            270f => new Vector3(0f, 0.9f, -0.35f),
                            _ => new Vector3(0.35f, 0.9f, 0f)
                        };
                        latch.transform.localPosition = latchOffset;
                        latch.transform.localScale = new Vector3(0.04f, 0.02f, 0.01f);
                        var rend = piece.GetComponentInChildren<Renderer>();
                        if (rend != null) latch.GetComponent<Renderer>().sharedMaterial = rend.sharedMaterial;
                    }
                }
            LootContainer.Spawn(world, ContainerType.Crate, CrateSpot, 7777);
        }

        /// <summary>True when a <paramref name="type"/> piece centred at <paramref name="center"/> stands in one of the
        /// slots a fresh run raises the home in — how a save from before pieces recorded
        /// <see cref="BuildPiece.Prebuilt"/> tells the original house from the survivor's own building.</summary>
        public static bool IsBlueprintSlot(BuildPieceType type, Vector3 center)
        {
            if (!Active) return false;
            foreach (Slot s in Slots)
                if (s.Type == type && Mathf.Abs(center.x - s.X) < 0.1f && Mathf.Abs(center.z - s.Z) < 0.1f) return true;
            return false;
        }

        /// <summary>Places the default furniture: every piece whose floor tile still stands and whose spot
        /// is clear of the run's beds, stoves, taps, containers, walls, barricades and other furniture —
        /// the full set in a fresh run, and whatever fits around an older save's own arrangement.</summary>
        public static void Furnish(Transform world)
        {
            if (!Active) return;
            var taken = new List<Rect>(48);
            CollectTaken(taken);
            for (int i = 0; i < Layout.Length; i++)
            {
                Item it = Layout[i];
                var p = new Vector3(it.X, 0f, it.Z);
                if (BuildPiece.FloorTopAt(p) <= 0f) continue;   // its floor tile has been torn up
                if (HomeFurniture.IsSurfaceItem(it.Kind))
                {
                    // A TV or radio needs its dresser or counter under it, and only one of each per spot.
                    if (!HomeFurniture.IsSupported(it.Kind, p) || AlreadyStands(it.Kind, p)) continue;
                    HomeFurniture.Spawn(world, it.Kind, p, it.Yaw, 7001 + i * 131);
                    continue;
                }
                Rect fp = Shrink(HomeFurniture.Footprint(it.Kind, p, it.Yaw), 0.03f);
                if (Overlaps(taken, fp)) continue;
                HomeFurniture.Spawn(world, it.Kind, p, it.Yaw, 7001 + i * 131);
                taken.Add(fp);
            }
        }

        private static bool AlreadyStands(HomeFurnitureKind kind, Vector3 p)
        {
            for (int i = 0; i < HomeFurniture.All.Count; i++)
            {
                HomeFurniture f = HomeFurniture.All[i];
                if (f != null && f.Kind == kind && At(f.transform.position, p)) return true;
            }
            return false;
        }

        /// <summary>Makes room in an older save's home for what came with the interactive fixtures — the
        /// bathroom, the fridge — before <see cref="Furnish"/> places them: the steel shelving at the store room's east
        /// end is taken away (there's no room left for it), the boxes move along to the back wall, the supply crate
        /// moves into the corner they left, the dining chair steps aside for the fridge, and the bathroom's partition
        /// goes up. Only things still standing exactly where the furnished home put them are touched (none of that
        /// furniture had anything kept in it yet), and things move only into empty spots; the partition rises only
        /// where its slot and footprint are clear. Call before the navmesh bake.</summary>
        public static void MigrateToFixtureLayout(Transform world)
        {
            if (!Active) return;

            RemoveOldFurniture(FurnishedEastShelf);
            Relocate(world, FurnishedBoxes, FindLayout(HomeFurnitureKind.Boxes, 2.78f, 8.63f));
            for (int i = 0; i < LootContainer.All.Count; i++)
            {
                LootContainer c = LootContainer.All[i];
                if (c == null || c.IsSack || c.IsFixture || c.Type != ContainerType.Crate || !At(c.transform.position, FurnishedCrateSpot)) continue;
                if (Free(CrateSpot, c.transform)) MoveTo(c.transform, CrateSpot, c.transform.position.y - BuildPiece.FloorTopAt(c.transform.position));
            }
            Relocate(world, FurnishedDiningChair, FindLayout(HomeFurnitureKind.Chair, 2.68f, 5.82f));

            Slot s = BathroomPartition;
            Vector3 center = BuildPiece.CenterFor(s.Type, new Vector3(s.X, 0f, s.Z));
            if (!BuildPiece.Occupied(center, CloneZConfig.HousePieceSnapEpsilon) && PartitionClear(s))
                BuildPiece.Spawn(world, s.Type, center, s.Yaw, 1f, prebuilt: true);
        }

        // Takes away a piece of the home's furniture still standing exactly where an older layout put it.
        private static void RemoveOldFurniture(Item at)
        {
            for (int i = 0; i < HomeFurniture.All.Count; i++)
            {
                HomeFurniture f = HomeFurniture.All[i];
                if (f == null || f.Kind != at.Kind || !At(f.transform.position, new Vector3(at.X, 0f, at.Z))) continue;
                f.Dismantle();
                return;
            }
        }

        private static Item FindLayout(HomeFurnitureKind kind, float x, float z)
        {
            for (int i = 0; i < Layout.Length; i++)
                if (Layout[i].Kind == kind && Mathf.Abs(Layout[i].X - x) < 0.01f && Mathf.Abs(Layout[i].Z - z) < 0.01f) return Layout[i];
            return new Item(kind, x, z, 0f);
        }

        // Moves a piece of furniture standing exactly at its old place to its new one (rebuilt there with the same
        // look), when the new spot is clear of everything but itself.
        private static void Relocate(Transform world, Item from, Item to)
        {
            for (int i = 0; i < HomeFurniture.All.Count; i++)
            {
                HomeFurniture f = HomeFurniture.All[i];
                if (f == null || f.Kind != from.Kind || !At(f.transform.position, new Vector3(from.X, 0f, from.Z))) continue;
                var p = new Vector3(to.X, 0f, to.Z);
                if (BuildPiece.FloorTopAt(p) <= 0f) return;
                var taken = new List<Rect>(48);
                CollectTaken(taken, f.transform);
                if (Overlaps(taken, Shrink(HomeFurniture.Footprint(to.Kind, p, to.Yaw), 0.03f))) return;
                int seed = f.Seed;
                f.Dismantle();
                HomeFurniture.Spawn(world, to.Kind, p, to.Yaw, seed);
                return;
            }
        }

        // The bathroom partition's line (x 4.9..5.1 along z 7..9) holds nothing standing on the floor.
        private static bool PartitionClear(Slot s)
        {
            var line = Rect.MinMaxRect(s.X - Half, s.Z - Tile * 0.5f + 0.05f, s.X + Half, s.Z + Tile * 0.5f - 0.05f);
            var taken = new List<Rect>(48);
            CollectTaken(taken);
            for (int i = 0; i < taken.Count; i++)
            {
                Rect r = taken[i];
                // The walls the partition butts against at each end don't count.
                bool wallAtEnd = (r.width < Half * 2f + 0.01f || r.height < Half * 2f + 0.01f) && (r.yMax <= line.yMin + 0.06f || r.yMin >= line.yMax - 0.06f);
                if (!wallAtEnd && r.Overlaps(line)) return false;
            }
            return true;
        }

        /// <summary>Moves an older save's bed, stove, sink and supply crate from where the bare shelter kept
        /// them to their places in the furnished home. Only fittings still standing exactly where the old
        /// layout put them move (the survivor can't move them, only tear them down), and only into an
        /// empty spot on a standing floor.</summary>
        public static void MigrateOldLayout()
        {
            if (!Active) return;
            // The sink first: the stove's new place is where the old sink stood.
            for (int i = 0; i < WaterSource.All.Count; i++)
            {
                WaterSource w = WaterSource.All[i];
                if (w != null && w.Kind == WaterSourceKind.Tap && At(w.transform.position, OldSinkSpot) && Free(SinkSpot, w.transform))
                    MoveTo(w.transform, SinkSpot, 0f);
            }
            for (int i = 0; i < CookingStation.All.Count; i++)
                if (CookingStation.All[i] != null && At(CookingStation.All[i].transform.position, OldStoveSpot) && Free(StoveSpot, CookingStation.All[i].transform))
                    MoveTo(CookingStation.All[i].transform, StoveSpot, 0f);
            for (int i = 0; i < SleepSpot.All.Count; i++)
                if (SleepSpot.All[i] != null && At(SleepSpot.All[i].transform.position, OldBedSpot) && Free(BedSpot, SleepSpot.All[i].transform))
                    MoveTo(SleepSpot.All[i].transform, BedSpot, 0f);
            for (int i = 0; i < LootContainer.All.Count; i++)
            {
                LootContainer c = LootContainer.All[i];
                if (c == null || c.IsSack || c.Type != ContainerType.Crate || !At(c.transform.position, OldCrateSpot) || !Free(CrateSpot, c.transform)) continue;
                MoveTo(c.transform, CrateSpot, c.transform.position.y - BuildPiece.FloorTopAt(c.transform.position));
            }
        }

        private static bool At(Vector3 p, Vector3 spot) => Mathf.Abs(p.x - spot.x) < 0.05f && Mathf.Abs(p.z - spot.z) < 0.05f;

        // A spot is free for a fitting when its floor stands and no other fitting already sits on it.
        private static bool Free(Vector3 spot, Transform self)
        {
            if (BuildPiece.FloorTopAt(spot) <= 0f) return false;
            var probe = Rect.MinMaxRect(spot.x - 0.3f, spot.z - 0.3f, spot.x + 0.3f, spot.z + 0.3f);
            var taken = new List<Rect>(48);
            CollectTaken(taken, self);
            return !Overlaps(taken, probe);
        }

        // Moves a fitting onto the floor at a new spot, keeping its height above the floor.
        private static void MoveTo(Transform t, Vector3 spot, float aboveFloor)
        {
            spot.y = BuildPiece.FloorTopAt(spot) + aboveFloor;
            t.position = spot;
        }

        // The XZ footprints of everything already standing in the world that furniture must keep clear of.
        private static void CollectTaken(List<Rect> into, Transform except = null)
        {
            for (int i = 0; i < SleepSpot.All.Count; i++) AddBounds(into, SleepSpot.All[i], except);
            for (int i = 0; i < CookingStation.All.Count; i++) AddBounds(into, CookingStation.All[i], except);
            for (int i = 0; i < WaterSource.All.Count; i++) AddBounds(into, WaterSource.All[i], except);
            for (int i = 0; i < Cover.All.Count; i++) AddBounds(into, Cover.All[i], except);
            for (int i = 0; i < LootContainer.All.Count; i++)
            {
                LootContainer c = LootContainer.All[i];
                if (c != null && !c.IsSack && !c.IsFixture && c.Type != ContainerType.Corpse) AddBounds(into, c, except);
            }
            for (int i = 0; i < HomeFurniture.All.Count; i++)
            {
                HomeFurniture f = HomeFurniture.All[i];
                if (f != null && f.transform != except && !HomeFurniture.IsSurfaceItem(f.Kind))
                    into.Add(HomeFurniture.Footprint(f.Kind, f.transform.position, f.Yaw));
            }
            for (int i = 0; i < Barricade.All.Count; i++)
            {
                Barricade b = Barricade.All[i];
                if (b == null) continue;
                float r = CloneZConfig.BarricadeWidth * 0.5f;
                Vector3 p = b.transform.position;
                into.Add(Rect.MinMaxRect(p.x - r, p.z - r, p.x + r, p.z + r));
            }
            for (int i = 0; i < BuildPiece.All.Count; i++)
            {
                BuildPiece w = BuildPiece.All[i];
                if (w == null || !w.Obstructs) continue;
                Vector3 p = w.transform.position;
                bool alongX = Mathf.Abs(Mathf.Sin(w.transform.eulerAngles.y * Mathf.Deg2Rad)) < 0.5f;
                float hx = alongX ? Tile * 0.5f : Half, hz = alongX ? Half : Tile * 0.5f;
                into.Add(Rect.MinMaxRect(p.x - hx, p.z - hz, p.x + hx, p.z + hz));
            }
        }

        private static void AddBounds(List<Rect> into, Component c, Transform except)
        {
            if (c == null || c.transform == except) return;
            Renderer[] rs = c.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            into.Add(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z));
        }

        private static Rect Shrink(Rect r, float m) => Rect.MinMaxRect(r.xMin + m, r.yMin + m, r.xMax - m, r.yMax - m);

        private static bool Overlaps(List<Rect> rects, Rect r)
        {
            for (int i = 0; i < rects.Count; i++) if (rects[i].Overlaps(r)) return true;
            return false;
        }

        /// <summary>A stable per-slot seed, so a piece's random details are the same every time it is built.</summary>
        private static int SlotSeed(BuildPieceType type, float x, float z)
            => unchecked(((int)type + 1) * 73856093 ^ Mathf.RoundToInt(x * 10f) * 19349663 ^ Mathf.RoundToInt(z * 10f) * 83492791) & 0x7FFFFFFF;
        #endregion
    }
}
