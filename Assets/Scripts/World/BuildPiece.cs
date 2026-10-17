using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>The kind of structural piece a survivor can raise when building a house.</summary>
    public enum BuildPieceType { Floor, Wall, Doorway, Roof, Window }

    /// <summary>
    /// A single player-built structural piece — a floor slab, a wall, a doorway frame or a
    /// roof panel. Pieces snap to a 2 m tile grid (see <see cref="PlayerBuilder"/>) so they
    /// assemble into a house: floors tile the ground, walls stand on the tile edges and roofs
    /// cap the walls. Walls and doorways are solid and carve the baked NavMesh (like a
    /// <see cref="Barricade"/>), so zombies have to beat them down to get through; a doorway
    /// leaves a walkable opening in its frame. Keeps a static registry mirroring
    /// <see cref="Barricade"/> / <see cref="LootContainer"/> for save/restore and AI queries.
    /// A piece built (or restored) in one of the wave-survival starter home's slots takes the home's
    /// finished look — siding, wallpaper, glazed windows, hung doors, wood floors — from
    /// <see cref="StarterHome"/> instead of rough planking; its colliders and carvers are the same.
    /// </summary>
    public class BuildPiece : MonoBehaviour, IDamageable
    {
        public static readonly List<BuildPiece> All = new List<BuildPiece>();

        public BuildPieceType Type { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }

        public Vector3 Position => transform.position;
        public float HealthNormalized => MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f;
        /// <summary>Walls, doorways and windows stand in a zombie's way and must be bashed down; floors/roofs don't.</summary>
        public bool Obstructs => Type == BuildPieceType.Wall || Type == BuildPieceType.Doorway || Type == BuildPieceType.Window;

        bool IDamageable.IsDead => this == null || Health <= 0f;
        Transform IDamageable.Transform => transform;

        private static readonly Color FloorColor = new Color(0.50f, 0.40f, 0.28f);
        private static readonly Color WallColor = new Color(0.58f, 0.46f, 0.32f);
        private static readonly Color FrameColor = new Color(0.44f, 0.34f, 0.23f);
        private static readonly Color RoofColor = new Color(0.34f, 0.27f, 0.21f);

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region Layout helpers
        /// <summary>The world-space centre a piece of this type occupies for a given ground spot
        /// (floors sit just above the ground, walls stand at half height, roofs cap the walls).
        /// Used for placement, the ghost preview and occupancy tests so there is one source of truth.</summary>
        public static Vector3 CenterFor(BuildPieceType type, Vector3 ground)
        {
            switch (type)
            {
                case BuildPieceType.Floor: ground.y = CloneZConfig.HouseFloorThickness * 0.5f; break;
                case BuildPieceType.Roof: ground.y = CloneZConfig.HouseWallHeight + CloneZConfig.HouseRoofThickness * 0.5f; break;
                default: ground.y = CloneZConfig.HouseWallHeight * 0.5f; break; // Wall, Doorway, Window
            }
            return ground;
        }

        /// <summary>Base (level-1 Carpentry) health for a piece type.</summary>
        public static float BaseHealthFor(BuildPieceType type)
        {
            switch (type)
            {
                case BuildPieceType.Floor: return CloneZConfig.HouseFloorHealth;
                case BuildPieceType.Wall: return CloneZConfig.HouseWallHealth;
                case BuildPieceType.Doorway: return CloneZConfig.HouseDoorwayHealth;
                case BuildPieceType.Window: return CloneZConfig.HouseWindowHealth;
                default: return CloneZConfig.HouseRoofHealth; // Roof
            }
        }

        /// <summary>True when a piece already stands at (or overlapping) <paramref name="center"/>.
        /// Because each type sits at a characteristic height, a simple radius test keeps floors,
        /// walls and roofs on separate layers while blocking duplicates on the same slot.</summary>
        public static bool Occupied(Vector3 center, float radius)
        {
            float sqr = radius * radius;
            for (int i = 0; i < All.Count; i++)
            {
                BuildPiece p = All[i];
                if (p == null) continue;
                if ((p.transform.position - center).sqrMagnitude < sqr) return true;
            }
            return false;
        }
        #endregion

        #region Queries
        /// <summary>
        /// Nearest wall/doorway within reach of <paramref name="from"/> that stands roughly between the
        /// searcher and <paramref name="towards"/> — the piece a zombie should bash to reach its target
        /// rather than one off to the side. Mirrors <see cref="Barricade.NearestObstructing"/>.
        /// </summary>
        public static BuildPiece NearestObstructing(Vector3 from, Vector3 towards, float range)
        {
            Vector3 toTarget = CloneZUtil.Flat(towards - from);
            if (toTarget == Vector3.zero) return null;

            BuildPiece best = null;
            float bestSqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                BuildPiece p = All[i];
                if (p == null || !p.Obstructs || p.Health <= 0f) continue;
                Vector3 toP = p.transform.position - from;
                float s = toP.sqrMagnitude;
                if (s > bestSqr) continue;
                if (Vector3.Dot(CloneZUtil.Flat(toP), toTarget) < 0.25f) continue; // off to the side
                bestSqr = s;
                best = p;
            }
            return best;
        }
        #endregion

        #region Building
        /// <summary>Raises a fresh piece of the given type at <paramref name="center"/> (its occupancy
        /// centre from <see cref="CenterFor"/>), facing <paramref name="yaw"/> degrees.</summary>
        public static BuildPiece Spawn(Transform parent, BuildPieceType type, Vector3 center, float yaw, float healthMult)
        {
            float hp = Mathf.Max(1f, BaseHealthFor(type) * healthMult);
            return Build(parent, type, center, yaw, hp, hp);
        }

        /// <summary>Rebuilds a piece from a saved game with its exact health.</summary>
        public static BuildPiece SpawnRestored(Transform parent, BuildPieceType type, Vector3 center, float yaw, float health, float maxHealth)
        {
            float max = maxHealth > 0f ? maxHealth : BaseHealthFor(type);
            float cur = Mathf.Clamp(health, 1f, max);
            return Build(parent, type, center, yaw, cur, max);
        }

        private static BuildPiece Build(Transform parent, BuildPieceType type, Vector3 center, float yaw, float health, float maxHealth)
        {
            var root = new GameObject("House_" + type);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));

            var piece = root.AddComponent<BuildPiece>();
            piece.Type = type;
            piece.MaxHealth = maxHealth;
            piece.Health = health;
            piece.BuildGeometry();
            return piece;
        }

        private void BuildGeometry()
        {
            // A piece standing in one of the wave-survival starter home's slots builds the home's finished
            // look; everywhere else it's rough planking. Both keep the same colliders and navmesh carvers.
            bool visuals = !StarterHome.TryFinish(this);
            float t = CloneZConfig.HouseTileSize;
            switch (Type)
            {
                case BuildPieceType.Floor:
                    Slab(new Vector3(t, CloneZConfig.HouseFloorThickness, t), FloorColor, visuals);
                    break;

                case BuildPieceType.Roof:
                    Slab(new Vector3(t + 0.15f, CloneZConfig.HouseRoofThickness, t + 0.15f), RoofColor, visuals);
                    break;

                case BuildPieceType.Wall:
                    BuildWall(visuals);
                    break;

                case BuildPieceType.Doorway:
                    BuildDoorway(visuals);
                    break;

                case BuildPieceType.Window:
                    BuildWindow(visuals);
                    break;
            }
        }

        /// <summary>Adds a blocking box in the piece's local space (the finished window's solid jambs).</summary>
        internal void AddSolid(Vector3 localCenter, Vector3 size)
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = localCenter;
            col.size = size;
        }

        /// <summary>Height of the top of a floor piece under <paramref name="p"/> (XZ), or 0 on bare ground —
        /// where furniture and fittings standing there should rest.</summary>
        public static float FloorTopAt(Vector3 p)
        {
            float half = CloneZConfig.HouseTileSize * 0.5f;
            for (int i = 0; i < All.Count; i++)
            {
                BuildPiece b = All[i];
                if (b == null || b.Type != BuildPieceType.Floor || b.Health <= 0f) continue;
                Vector3 c = b.transform.position;
                if (Mathf.Abs(p.x - c.x) <= half && Mathf.Abs(p.z - c.z) <= half) return CloneZConfig.HouseFloorThickness;
            }
            return 0f;
        }

        // A single flat panel (floor or roof): one visual cube plus a matching, non-carving collider.
        private void Slab(Vector3 size, Color color, bool visual)
        {
            if (visual) Block(transform, Vector3.zero, size, color);
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = size;
        }

        // A full-height solid wall: one visual cube, a blocking collider and a carving NavMesh
        // obstacle so the pre-baked mesh routes zombies around it (or dead-ends them into bashing it).
        private void BuildWall(bool visual)
        {
            float t = CloneZConfig.HouseTileSize;
            float h = CloneZConfig.HouseWallHeight;
            float thick = CloneZConfig.HouseWallThickness;
            var size = new Vector3(t, h, thick);

            if (visual) Block(transform, Vector3.zero, size, WallColor);

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = size;

            AddCarver(gameObject, Vector3.zero, size);
        }

        // A wall with a walkable opening: two side jambs (each solid and carving) and a header beam
        // across the top. The central gap is left clear so a survivor — and a zombie — can pass through.
        private void BuildDoorway(bool visual)
        {
            float t = CloneZConfig.HouseTileSize;
            float h = CloneZConfig.HouseWallHeight;
            float thick = CloneZConfig.HouseWallThickness;
            float gap = Mathf.Min(CloneZConfig.HouseDoorwayGapWidth, t - 0.2f);
            float lintelH = CloneZConfig.HouseDoorwayLintelHeight;

            float jambW = (t - gap) * 0.5f;
            float jambX = (gap + jambW) * 0.5f;               // centre of each jamb along local X
            var jambSize = new Vector3(jambW, h, thick);

            BuildDoorPost(new Vector3(-jambX, 0f, 0f), jambSize, visual);
            BuildDoorPost(new Vector3(jambX, 0f, 0f), jambSize, visual);

            // Header beam across the top — VISUAL ONLY (no collider), so the survivor never catches
            // their head on it, even standing on a floor tile. The jambs are the only solid parts.
            var lintelSize = new Vector3(t, lintelH, thick);
            var lintelPos = new Vector3(0f, (h - lintelH) * 0.5f, 0f);
            if (visual) Block(transform, lintelPos, lintelSize, FrameColor);
        }

        // One doorway jamb: a solid post (visual + collider) that also carves the NavMesh, leaving
        // the doorway's centre open for passage.
        private void BuildDoorPost(Vector3 localCenter, Vector3 size, bool visual)
        {
            if (visual) Block(transform, localCenter, size, FrameColor);

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = localCenter;
            col.size = size;

            var post = new GameObject("Carver");
            post.transform.SetParent(transform, false);
            post.transform.localPosition = localCenter;
            AddCarver(post, Vector3.zero, size);
        }

        // A wall with an open glazing band: a solid sill along the bottom and a header across the
        // top, with the gap between them left clear so you can see (and shoot) out. The sill is too
        // tall to step over, and the whole footprint carves the NavMesh, so it blocks passage like a
        // wall while zombies batter it down as a weak point.
        private void BuildWindow(bool visual)
        {
            float t = CloneZConfig.HouseTileSize;
            float h = CloneZConfig.HouseWallHeight;
            float thick = CloneZConfig.HouseWallThickness;
            float sillH = CloneZConfig.HouseWindowSillHeight;
            float headH = CloneZConfig.HouseWindowHeadHeight;

            // Local space: the piece centre sits at wall mid-height, so the wall spans y -h/2..+h/2.
            WindowPanel(new Vector3(0f, -h * 0.5f + sillH * 0.5f, 0f), new Vector3(t, sillH, thick), visual);
            WindowPanel(new Vector3(0f, h * 0.5f - headH * 0.5f, 0f), new Vector3(t, headH, thick), visual);

            AddCarver(gameObject, Vector3.zero, new Vector3(t, h, thick));
        }

        // One solid slab of a window (sill or header): a visual block plus a matching collider.
        private void WindowPanel(Vector3 localCenter, Vector3 size, bool visual)
        {
            if (visual) Block(transform, localCenter, size, WallColor);
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = localCenter;
            col.size = size;
        }

        private static void AddCarver(GameObject go, Vector3 center, Vector3 size)
        {
            var obstacle = go.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = center;
            obstacle.size = size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }

        // A single cube child with a URP material, its own collider stripped (the piece's own
        // BoxCollider does the blocking); placed in the piece's local space.
        private static GameObject Block(Transform parent, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Part";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(color);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }
        #endregion

        #region Damage
        public void TakeDamage(in DamageInfo info)
        {
            if (Health <= 0f) return;
            Health -= Mathf.Max(0f, info.Amount);
            if (Health <= 0f) { Break(); return; }
            // Splintering wood is noisy — a struggling wall draws still more zombies.
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeHit);
        }

        private void Break()
        {
            Health = 0f;
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseHammering);
            All.Remove(this);
            Destroy(gameObject);
        }

        /// <summary>Tears the piece down for salvage (the builder handles the material refund and
        /// the prying noise). Removes it from the registry at once so AI queries drop it immediately.</summary>
        public void Dismantle()
        {
            Health = 0f;
            All.Remove(this);
            Destroy(gameObject);
        }
        #endregion

        #region IDemolishable
        public string DemolishName => Type switch
        {
            BuildPieceType.Floor => "floor",
            BuildPieceType.Wall => "wall",
            BuildPieceType.Doorway => "doorway",
            BuildPieceType.Window => "window",
            _ => "roof",
        };
        public bool CanDemolish(out string reason) { reason = null; return Health > 0f; }
        public void GetSalvage(out int planks, out int nails)
        {
            int pc, nc;
            switch (Type)
            {
                case BuildPieceType.Floor: pc = CloneZConfig.HouseFloorPlankCost; nc = CloneZConfig.HouseFloorNailCost; break;
                case BuildPieceType.Wall: pc = CloneZConfig.HouseWallPlankCost; nc = CloneZConfig.HouseWallNailCost; break;
                case BuildPieceType.Doorway: pc = CloneZConfig.HouseDoorwayPlankCost; nc = CloneZConfig.HouseDoorwayNailCost; break;
                case BuildPieceType.Window: pc = CloneZConfig.HouseWindowPlankCost; nc = CloneZConfig.HouseWindowNailCost; break;
                default: pc = CloneZConfig.HouseRoofPlankCost; nc = CloneZConfig.HouseRoofNailCost; break;
            }
            float f = CloneZConfig.HouseSalvageFraction;
            planks = Mathf.Max(0, Mathf.RoundToInt(pc * f));
            nails = Mathf.Max(0, Mathf.RoundToInt(nc * f));
        }
        public bool CarvesNavMesh => false;   // walls carve via NavMeshObstacle (auto-freed); floors/roofs don't carve
        #endregion
    }
}
