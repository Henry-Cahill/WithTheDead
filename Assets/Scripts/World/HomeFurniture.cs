using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>The loose furniture that dresses the starter home.</summary>
    public enum HomeFurnitureKind { Counter, Table, Chair, Nightstand, Dresser, Bookshelf, Armchair, FloorLamp, Shelf, Boxes }

    /// <summary>
    /// A piece of the starter home's loose furniture — a kitchen counter, the dining table and chairs, a
    /// bedside table, a dresser, a bookcase, an armchair, a standard lamp, steel shelving or a stack of
    /// boxes — drawn with the raid map's furniture builders so the home matches the extraction town. Each
    /// piece is solid (its collider and mesh are baked into the navmesh), can be dismantled for salvage
    /// like the bed and stove, and is saved with its kind, place, facing and variation seed, so a
    /// continued run rebuilds it exactly. The lamps light up from dusk to dawn (<see cref="HomeLamp"/>).
    /// Keeps a static registry for save/restore and the build tool's demolish picking.
    /// </summary>
    public class HomeFurniture : MonoBehaviour, IDemolishable
    {
        public static readonly List<HomeFurniture> All = new List<HomeFurniture>();

        public HomeFurnitureKind Kind { get; private set; }
        /// <summary>Facing in degrees (0 = front toward +Z). The root stays unrotated; the mesh is drawn turned.</summary>
        public float Yaw { get; private set; }
        /// <summary>Variation seed (fabric, wood tone, what's left on it), kept so a reload looks identical.</summary>
        public int Seed { get; private set; }

        private static readonly Color CabinetPaint = new Color(0.78f, 0.76f, 0.66f);
        private static readonly Color Worktop = new Color(0.58f, 0.44f, 0.28f);
        private static readonly Color ShelfSteel = new Color(0.36f, 0.40f, 0.38f);
        private static readonly Color LampShade = new Color(0.86f, 0.78f, 0.60f);
        private static readonly Color LampGlow = new Color(1f, 0.84f, 0.58f);
        private static readonly Color[] Fabrics =
        {
            new Color(0.58f, 0.44f, 0.18f), new Color(0.46f, 0.20f, 0.14f), new Color(0.26f, 0.34f, 0.24f),
        };

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region Layout
        /// <summary>A kind's size: width along its local X, height, depth along its local Z (front at +Z).</summary>
        public static Vector3 SizeOf(HomeFurnitureKind kind) => kind switch
        {
            HomeFurnitureKind.Counter => new Vector3(1.22f, 0.95f, 0.63f),
            HomeFurnitureKind.Table => new Vector3(1.0f, 0.78f, 0.8f),
            HomeFurnitureKind.Chair => new Vector3(0.45f, 0.9f, 0.45f),
            HomeFurnitureKind.Nightstand => new Vector3(0.45f, 0.62f, 0.4f),
            HomeFurnitureKind.Dresser => new Vector3(1.04f, 0.9f, 0.5f),
            HomeFurnitureKind.Bookshelf => new Vector3(0.9f, 1.8f, 0.32f),
            HomeFurnitureKind.Armchair => new Vector3(0.95f, 0.85f, 0.88f),
            HomeFurnitureKind.FloorLamp => new Vector3(0.36f, 1.55f, 0.36f),
            HomeFurnitureKind.Shelf => new Vector3(1.2f, 1.85f, 0.45f),
            _ => new Vector3(0.6f, 0.95f, 0.5f),   // Boxes
        };

        /// <summary>The world XZ rect a kind covers standing at <paramref name="position"/>, turned by <paramref name="yaw"/>.</summary>
        public static Rect Footprint(HomeFurnitureKind kind, Vector3 position, float yaw)
        {
            Vector3 a = AabbSize(SizeOf(kind), yaw);
            return new Rect(position.x - a.x * 0.5f, position.z - a.z * 0.5f, a.x, a.z);
        }

        private static Vector3 AabbSize(Vector3 size, float yaw)
        {
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(rad)), s = Mathf.Abs(Mathf.Sin(rad));
            return new Vector3(size.x * c + size.z * s, size.y, size.x * s + size.z * c);
        }
        #endregion

        #region Building
        /// <summary>Places a piece of furniture on the ground (or the house floor) at <paramref name="position"/>.</summary>
        public static HomeFurniture Spawn(Transform parent, HomeFurnitureKind kind, Vector3 position, float yaw, int seed)
        {
            position.y = BuildPiece.FloorTopAt(position);
            var go = new GameObject("Furniture_" + kind);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.identity);
            var f = go.AddComponent<HomeFurniture>();
            f.Kind = kind;
            f.Yaw = Mathf.Repeat(yaw, 360f);
            f.Seed = seed;
            f.Build();
            return f;
        }

        private void Build()
        {
            Vector3 p = transform.position;
            var rng = new System.Random(Seed);
            var m = new RaidMesh();
            m.SetFrame(p, Yaw);
            RaidMesh glow = null;
            Vector3 bulb = Vector3.zero;
            switch (Kind)
            {
                case HomeFurnitureKind.Counter:
                    FurnitureBuilder.KitchenCounter(m, rng, 1.2f, CabinetPaint, Worktop, false);
                    break;
                case HomeFurnitureKind.Table:
                    FurnitureBuilder.Table(m, rng, 1.0f, 0.8f, FurnitureBuilder.Pine);
                    break;
                case HomeFurnitureKind.Chair:
                    FurnitureBuilder.WoodChair(m, rng, FurnitureBuilder.Pine);
                    break;
                case HomeFurnitureKind.Nightstand:
                {
                    bulb = m.ToMeshSpace(FurnitureBuilder.Nightstand(m, rng, FurnitureBuilder.Walnut, false));
                    glow = new RaidMesh();
                    glow.SetFrame(p, Yaw);
                    glow.Cylinder(new Vector3(0f, 0.836f, -0.04f), 0.158f, 0.208f, 10, DecorPalette.Glow(LampGlow), Quaternion.identity, 0.65f, false);
                    break;
                }
                case HomeFurnitureKind.Dresser:
                    FurnitureBuilder.Dresser(m, rng, FurnitureBuilder.Walnut);
                    break;
                case HomeFurnitureKind.Bookshelf:
                    FurnitureBuilder.Bookshelf(m, rng, FurnitureBuilder.Oak, 1);
                    break;
                case HomeFurnitureKind.Armchair:
                    FurnitureBuilder.Sofa(m, rng, 0.95f, Fabrics[rng.Next(Fabrics.Length)]);
                    break;
                case HomeFurnitureKind.FloorLamp:
                {
                    bulb = m.ToMeshSpace(StandardLamp(m));
                    glow = new RaidMesh();
                    glow.SetFrame(p, Yaw);
                    glow.Cylinder(new Vector3(0f, 1.236f, 0f), 0.228f, 0.288f, 14, DecorPalette.Glow(LampGlow), Quaternion.identity, 0.62f, false);
                    break;
                }
                case HomeFurnitureKind.Shelf:
                    FurnitureBuilder.MetalShelf(m, rng, 1.2f, 0.45f, 1.85f, 4, (Seed & 1) == 0 ? 0 : 3, ShelfSteel);
                    break;
                default:
                    FurnitureBuilder.BoxStack(m, rng, Vector3.zero, 0f, 2 + rng.Next(2));
                    break;
            }
            m.ResetFrame();
            m.ApplyTo(gameObject, p);
            AddCollider(SizeOf(Kind));
            if (glow != null) HomeLamp.Attach(transform, bulb, RoomLight.LampRange, RoomLight.LampIntensity, true, glow);
            DecorPalette.Apply();
        }

        // A standard lamp: weighted base, brass pole and a fabric drum shade (open top and bottom, so the
        // light spills up the wall and down onto the chair). Returns the bulb in the local frame.
        private static Vector3 StandardLamp(RaidMesh m)
        {
            Color bronze = new Color(0.26f, 0.20f, 0.12f);
            m.Cylinder(Vector3.zero, 0.16f, 0.035f, 14, DecorPalette.Metal(bronze));
            m.Cylinder(new Vector3(0f, 0.035f, 0f), 0.02f, 1.25f, 8, DecorPalette.Metal(FurnitureBuilder.Brass));
            m.Cylinder(new Vector3(0f, 0.6f, 0f), 0.032f, 0.05f, 8, DecorPalette.Metal(bronze));
            m.Cylinder(new Vector3(0f, 1.28f, 0f), 0.028f, 0.07f, 8, DecorPalette.Metal(FurnitureBuilder.Brass));
            m.Cylinder(new Vector3(0f, 1.24f, 0f), 0.22f, 0.28f, 14, DecorPalette.Matte(LampShade), Quaternion.identity, 0.62f, false);
            return new Vector3(0f, 1.36f, 0f);
        }

        // Solid footprint: on the root when the piece is square to the world, else on a turned child.
        private void AddCollider(Vector3 size)
        {
            float k = Mathf.Repeat(Yaw, 90f);
            if (k < 1f || k > 89f)
            {
                var bc = gameObject.AddComponent<BoxCollider>();
                bc.center = new Vector3(0f, size.y * 0.5f, 0f);
                bc.size = AabbSize(size, Yaw);
                return;
            }
            var go = new GameObject("Collider");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(0f, Yaw, 0f);
            var turned = go.AddComponent<BoxCollider>();
            turned.center = new Vector3(0f, size.y * 0.5f, 0f);
            turned.size = size;
        }
        #endregion

        #region IDemolishable
        public string DemolishName => Kind switch
        {
            HomeFurnitureKind.Counter => "kitchen counter",
            HomeFurnitureKind.Table => "table",
            HomeFurnitureKind.Chair => "chair",
            HomeFurnitureKind.Nightstand => "bedside table",
            HomeFurnitureKind.Dresser => "dresser",
            HomeFurnitureKind.Bookshelf => "bookcase",
            HomeFurnitureKind.Armchair => "armchair",
            HomeFurnitureKind.FloorLamp => "lamp",
            HomeFurnitureKind.Shelf => "shelving",
            _ => "boxes",
        };

        public bool CanDemolish(out string reason) { reason = null; return true; }

        public void GetSalvage(out int planks, out int nails)
        {
            switch (Kind)
            {
                case HomeFurnitureKind.Counter: planks = CloneZConfig.SalvageCounterPlanks; nails = CloneZConfig.SalvageCounterNails; break;
                case HomeFurnitureKind.Table: planks = CloneZConfig.SalvageTablePlanks; nails = CloneZConfig.SalvageTableNails; break;
                case HomeFurnitureKind.Chair: planks = CloneZConfig.SalvageChairPlanks; nails = CloneZConfig.SalvageChairNails; break;
                case HomeFurnitureKind.Nightstand: planks = CloneZConfig.SalvageNightstandPlanks; nails = CloneZConfig.SalvageNightstandNails; break;
                case HomeFurnitureKind.Dresser: planks = CloneZConfig.SalvageDresserPlanks; nails = CloneZConfig.SalvageDresserNails; break;
                case HomeFurnitureKind.Bookshelf: planks = CloneZConfig.SalvageBookshelfPlanks; nails = CloneZConfig.SalvageBookshelfNails; break;
                case HomeFurnitureKind.Armchair: planks = CloneZConfig.SalvageArmchairPlanks; nails = CloneZConfig.SalvageArmchairNails; break;
                case HomeFurnitureKind.Shelf: planks = CloneZConfig.SalvageShelvingPlanks; nails = CloneZConfig.SalvageShelvingNails; break;
                default: planks = 0; nails = 0; break;   // a lamp, cardboard boxes: nothing a builder can use
            }
        }

        public bool CarvesNavMesh => true;   // baked into the navmesh -> removing it needs a rebuild

        public void Dismantle()
        {
            All.Remove(this);
            gameObject.SetActive(false);   // drop out of the navmesh bake before the rebuild runs
            Destroy(gameObject);
        }
        #endregion
    }
}
