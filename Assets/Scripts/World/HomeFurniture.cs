using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    /// <summary>The loose furniture that dresses the starter home. New kinds go on the end: saves store the number.</summary>
    public enum HomeFurnitureKind
    {
        Counter, Table, Chair, Nightstand, Dresser, Bookshelf, Armchair, FloorLamp, Shelf, Boxes,
        Fridge, Toilet, Basin, Shower, Tv, Radio,
    }

    /// <summary>
    /// A piece of the starter home's loose furniture — a kitchen counter, the dining table and chairs, a
    /// bedside table, a dresser, a bookcase, an armchair, a standard lamp, steel shelving, a stack of
    /// boxes, the fridge, the bathroom's toilet, basin and shower, and the TV and radio — drawn with the
    /// raid map's furniture builders so the home matches the extraction town. Each piece is solid (its
    /// collider and mesh are baked into the navmesh — all but the bathroom's, kept out so the dead can follow the
    /// survivor into that small room), can be dismantled for salvage like the bed and stove
    /// (once whatever is kept in it has been taken out and nothing stands on top of it), and is saved with its kind, place, facing and
    /// variation seed, so a continued run rebuilds it exactly. The TV and the radio are surface items: they
    /// stand on the dresser and the kitchen counter (<see cref="IsSurfaceItem"/>) rather than the floor.
    ///
    /// Every piece is interactive (see <see cref="Interactable"/>): its doors, drawers and switches are
    /// built by the per-area partial files — the kitchen's (<c>HomeFurniture.Kitchen.cs</c>), the living
    /// room's and bedroom's (<c>HomeFurniture.Living.cs</c>) and the bathroom's (<c>HomeFurniture.Bathroom.cs</c>)
    /// — through the Draw*/Attach* hooks below. Keeps a static registry for save/restore and the build
    /// tool's demolish picking.
    /// </summary>
    public partial class HomeFurniture : MonoBehaviour, IDemolishable
    {
        public static readonly List<HomeFurniture> All = new List<HomeFurniture>();

        /// <summary>The kind of furniture this instance represents.</summary>
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
            HomeFurnitureKind.Fridge => new Vector3(0.72f, 1.72f, 0.70f),
            HomeFurnitureKind.Toilet => new Vector3(0.42f, 0.80f, 0.70f),
            HomeFurnitureKind.Basin => new Vector3(0.52f, 0.90f, 0.45f),
            HomeFurnitureKind.Shower => new Vector3(0.90f, 2.05f, 0.90f),
            HomeFurnitureKind.Tv => new Vector3(0.46f, 0.40f, 0.36f),
            HomeFurnitureKind.Radio => new Vector3(0.32f, 0.18f, 0.14f),
            _ => new Vector3(0.6f, 0.95f, 0.5f),   // Boxes
        };

        /// <summary>True for a small thing that stands on another piece of furniture instead of the floor — the TV
        /// on the dresser, the radio on the kitchen counter. It has no collider and claims no floor space.</summary>
        public static bool IsSurfaceItem(HomeFurnitureKind kind) => kind == HomeFurnitureKind.Tv || kind == HomeFurnitureKind.Radio;

        /// <summary>The furniture a surface item stands on, and the height of that surface above the floor.</summary>
        public static HomeFurnitureKind SupportOf(HomeFurnitureKind kind, out float height)
        {
            switch (kind)
            {
                case HomeFurnitureKind.Tv: height = 0.825f; return HomeFurnitureKind.Dresser;   // the dresser's top
                case HomeFurnitureKind.Radio: height = 0.94f; return HomeFurnitureKind.Counter; // the worktop
                default: height = 0f; return kind;
            }
        }

        /// <summary>True when a surface item at <paramref name="position"/> has its supporting furniture under it.</summary>
        public static bool IsSupported(HomeFurnitureKind kind, Vector3 position)
        {
            if (!IsSurfaceItem(kind)) return true;
            HomeFurnitureKind support = SupportOf(kind, out _);
            for (int i = 0; i < All.Count; i++)
            {
                HomeFurniture f = All[i];
                if (f != null && f.Kind == support && Footprint(f.Kind, f.transform.position, f.Yaw).Contains(new Vector2(position.x, position.z)))
                    return true;
            }
            return false;
        }

        /// <summary>False for the bathroom's fittings. Grown by the dead's radius, their footprints would leave that
        /// small room no floor to walk on — a refuge they could never follow the survivor into — so they're kept out
        /// of the navmesh bake: solid to the survivor, but not shaping where the dead can go.</summary>
        public static bool BakesIntoNavMesh(HomeFurnitureKind kind) =>
            kind != HomeFurnitureKind.Toilet && kind != HomeFurnitureKind.Basin && kind != HomeFurnitureKind.Shower;

        // The surface item (the TV, the radio) standing on this piece, or null.
        private HomeFurniture Carrying()
        {
            for (int i = 0; i < All.Count; i++)
            {
                HomeFurniture f = All[i];
                if (f == null || f == this || !IsSurfaceItem(f.Kind) || SupportOf(f.Kind, out _) != Kind) continue;
                Vector3 p = f.transform.position;
                if (Footprint(Kind, transform.position, Yaw).Contains(new Vector2(p.x, p.z))) return f;
            }
            return null;
        }

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
        /// <summary>Places a piece of furniture on the ground (or the house floor) at <paramref name="position"/> —
        /// a surface item (<see cref="IsSurfaceItem"/>) on top of the furniture it stands on.</summary>
        public static HomeFurniture Spawn(Transform parent, HomeFurnitureKind kind, Vector3 position, float yaw, int seed)
        {
            SupportOf(kind, out float surface);
            position.y = BuildPiece.FloorTopAt(position) + surface;
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

            // Each area draws the static body of its own kinds (leaving out the doors and drawers it animates);
            // anything no area takes over keeps its plain look.
            bool drawn = false;
            DrawKitchen(m, rng, ref drawn);
            if (!drawn) DrawLiving(m, rng, ref drawn, ref glow, ref bulb);
            if (!drawn) DrawBathroom(m, rng, ref drawn);
            if (!drawn) DrawPlain(m, rng, ref glow, ref bulb);

            m.ResetFrame();
            m.ApplyTo(gameObject, p);
            if (!IsSurfaceItem(Kind)) AddCollider(SizeOf(Kind));
            if (!BakesIntoNavMesh(Kind)) gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            if (glow != null) HomeLamp.Attach(transform, bulb, RoomLight.LampRange, RoomLight.LampIntensity, true, glow);

            // Then the moving parts and the fixtures (Interactables) that make the piece usable.
            AttachKitchen();
            AttachLiving();
            AttachBathroom();
            DecorPalette.Apply();
        }

        // The per-area hooks (implemented in HomeFurniture.Kitchen.cs / .Living.cs / .Bathroom.cs). A Draw hook
        // draws this piece's static body into the local frame (front toward +Z, origin on the floor at the
        // piece's centre — or on top of the supporting furniture for a surface item) and sets drawn when its
        // area owns this Kind; a lamp-bearing piece also returns its glowing shade and bulb (mesh space). An
        // Attach hook adds the moving parts and the Interactable fixtures for its own kinds.
        partial void DrawKitchen(RaidMesh m, System.Random rng, ref bool drawn);
        partial void DrawLiving(RaidMesh m, System.Random rng, ref bool drawn, ref RaidMesh glow, ref Vector3 bulb);
        partial void DrawBathroom(RaidMesh m, System.Random rng, ref bool drawn);
        partial void AttachKitchen();
        partial void AttachLiving();
        partial void AttachBathroom();

        // The original, static look of every kind.
        private void DrawPlain(RaidMesh m, System.Random rng, ref RaidMesh glow, ref Vector3 bulb)
        {
            Vector3 p = transform.position;
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
                    // Add a small metal drawer pull knob with left/right variation
                    bool leftDrawerKnob = rng.Next(2) == 0;
                    Vector3 knobPosDrawer = new Vector3(leftDrawerKnob ? -0.15f : 0.15f, 0.55f, 0.32f);
                    Vector3 knobSizeDrawer = new Vector3(0.02f, 0.07f, 0.02f);
                    m.Box(knobPosDrawer, knobSizeDrawer, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    break;
                }
                case HomeFurnitureKind.Dresser:
                {
                    FurnitureBuilder.Dresser(m, rng, FurnitureBuilder.Walnut);
                    // three drawer pulls – left/right variation per drawer
                    for (int i = 0; i < 3; i++)
                    {
bool leftSide = rng.Next(2) == 0;
                        float y = 0.27f + i * 0.22f; // drawer centre heights
                        float x = leftSide ? -0.18f : 0.18f;
                        Vector3 pullPos = new Vector3(x, y, 0.45f);
                        Vector3 pullSize = new Vector3(0.02f, 0.07f, 0.02f);
                        m.Box(pullPos, pullSize, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    }
                    // thin metal trim along the top front edge
                    m.Cylinder(new Vector3(0f, 0.88f, 0.45f), 0.02f, 0.30f, 8,
                        DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)), Quaternion.identity, 1f, false);
                    // faint wear scuff – darkened wood patch
                    bool scuffLeft = rng.Next(2) == 0;
                    float scuffX = scuffLeft ? -0.10f : 0.10f;
                    Vector3 scuffPos = new Vector3(scuffX, 0.15f, 0.42f);
                    Vector3 scuffSize = new Vector3(0.05f, 0.02f, 0.02f);
                    m.Box(scuffPos, scuffSize, DecorPalette.Matte(new Color(0.55f, 0.48f, 0.38f)));
                    break;
                }
                case HomeFurnitureKind.Bookshelf:
                    FurnitureBuilder.Bookshelf(m, rng, FurnitureBuilder.Oak, 1);
                    // metal brackets – thin vertical supports near the top edges
                    bool leftBracket = rng.Next(2) == 0;
                    float bracketX = leftBracket ? -0.42f : 0.42f; // relative to centre
                    Vector3 bracketPos = new Vector3(bracketX, 0.9f, 0.15f);
                    Vector3 bracketSize = new Vector3(0.02f, 0.25f, 0.02f);
                    m.Box(bracketPos, bracketSize, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // thin top trim – metal strip along the front top edge
                    Vector3 topTrimPos = new Vector3(0f, 1.79f, 0f);
                    Vector3 topTrimSize = new Vector3(0.9f, 0.02f, 0.02f);
                    m.Box(topTrimPos, topTrimSize, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // back panel – thin wooden board covering the back of the shelf
                    Vector3 backPanelPos = new Vector3(0f, 0.89f, -0.16f);
                    Vector3 backPanelSize = new Vector3(0.9f, 1.78f, 0.005f);
                    m.Box(backPanelPos, backPanelSize, DecorPalette.Matte(FurnitureBuilder.Oak));
                    // side label – tiny metal plaque near middle height
                    Vector3 labelPos = new Vector3(-0.44f, 0.6f, 0f);
                    Vector3 labelSize = new Vector3(0.05f, 0.008f, 0.02f);
                    m.Box(labelPos, labelSize, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // wood scuff – darkened patch near bottom left
                    bool scuffLeftBs = rng.Next(2) == 0;
                    float scuffXB = scuffLeftBs ? -0.35f : 0.35f;
                    Vector3 scuffPosBs = new Vector3(scuffXB, 0.07f, 0f);
                    Vector3 scuffSizeBs = new Vector3(0.08f, 0.02f, 0.02f);
                    m.Box(scuffPosBs, scuffSizeBs, DecorPalette.Matte(new Color(0.45f, 0.38f, 0.30f)));
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
                case HomeFurnitureKind.Fridge:
                    FurnitureBuilder.Fridge(m, rng, new Color(0.88f, 0.87f, 0.83f));
                    // Add a small metal door handle with left/right variation
bool leftHandle = rng.Next(2) == 0;
                    Vector3 handlePos = new Vector3(leftHandle ? -0.33f : 0.33f, 0.5f, 0.05f);
                    Vector3 handleSize = new Vector3(0.02f, 0.12f, 0.02f);
                    m.Box(handlePos, handleSize, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    break;
                case HomeFurnitureKind.Toilet:
                    FurnitureBuilder.Toilet(m, rng);
                    // seat slab – thin white box centred on the front of the bowl
                    Vector3 seatPos = new Vector3(0f, 0.07f, 0.15f);
                    Vector3 seatSize = new Vector3(0.35f, 0.02f, 0.25f);
                    m.Box(seatPos, seatSize, DecorPalette.Matte(new Color(0.93f, 0.93f, 0.95f)));
                    // lid – possibly opened
                    bool lidOpen = rng.Next(2) == 0;
                    float lidY = lidOpen ? 0.12f : 0.07f;
                    Vector3 lidPos = new Vector3(0f, lidY, 0.15f);
                    Vector3 lidSize = new Vector3(0.36f, 0.02f, 0.26f);
                    m.Box(lidPos, lidSize, DecorPalette.Matte(new Color(0.95f, 0.95f, 0.97f)));
                    // metal flush lever on back‑right of tank
                    Vector3 leverPos = new Vector3(-0.08f, 0.45f, -0.28f);
                    m.Cylinder(leverPos, 0.01f, 0.12f, 6, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // wall‑mounted toilet‑paper holder
                    Vector3 holderPos = new Vector3(-0.18f, 0.70f, -0.30f);
                    Vector3 holderSize = new Vector3(0.04f, 0.12f, 0.02f);
                    m.Box(holderPos, holderSize, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // random water‑stain on floor near front
                    bool stainLeft = rng.Next(2) == 0;
                    float stainX = stainLeft ? -0.12f : 0.12f;
                    Vector3 stainPos = new Vector3(stainX, 0.001f, 0.20f);
                    Vector3 stainSize = new Vector3(0.06f, 0.001f, 0.06f);
                    m.Box(stainPos, stainSize, DecorPalette.Matte(new Color(0.60f, 0.60f, 0.60f)));
                    break;
                case HomeFurnitureKind.Basin:
                     FurnitureBuilder.WashBasin(m, rng);
                     bool left = rng.Next(2) == 0;
                     Vector3 faucetPos = new Vector3(left ? -0.08f : 0.08f, 0.95f, 0.22f);
                     m.Cylinder(faucetPos, 0.01f, 0.12f, 8, DecorPalette.Metal(new Color(0.18f,0.18f,0.20f)));
                      Vector3 basinKnobPos = new Vector3(left ? -0.08f : 0.08f, 1.07f, 0.22f);
                      Vector3 knobSize = new Vector3(0.02f, 0.03f, 0.01f);
                      m.Box(basinKnobPos, knobSize, DecorPalette.Metal(new Color(0.18f,0.18f,0.20f)));

                    break;
                case HomeFurnitureKind.Shower:
                    m.Box(new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.08f, 0.9f), DecorPalette.Gloss(new Color(0.9f, 0.9f, 0.88f)));
                    // Add visual details: pipe, frosted glass door, towel rack, tiny water drip
                    bool doorLeft = rng.Next(2) == 0;
                    // Pipe feeding the showerhead
                    m.Cylinder(new Vector3(0f, 1.75f, -0.25f), 0.015f, 0.30f, 8, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // Frosted‑glass door (left or right side)
                    float doorX = doorLeft ? -0.45f : 0.45f;
                    m.Box(new Vector3(doorX, 0.5f, 0f), new Vector3(0.01f, 1.0f, 0.9f), DecorPalette.Glass(new Color(0.8f, 0.9f, 0.95f, 0.6f)));
                    // Towel rack on the opposite wall
                    float rackX = doorLeft ? 0.45f : -0.45f;
                    m.Cylinder(new Vector3(rackX, 0.9f, 0f), 0.015f, 0.6f, 8, DecorPalette.Metal(new Color(0.18f, 0.18f, 0.20f)));
                    // Tiny water drip from the pipe ceiling
                    m.Ellipsoid(new Vector3(0f, 1.9f, -0.25f), new Vector3(0.01f, 0.01f, 0.01f), DecorPalette.Glass(new Color(0.66f, 0.82f, 0.90f, 0.45f)), 6, 4);
                    break;
case HomeFurnitureKind.Tv:
                     m.Box(new Vector3(0f, 0.2f, -0.02f), new Vector3(0.46f, 0.4f, 0.32f), DecorPalette.Matte(new Color(0.12f, 0.12f, 0.13f)));
                     // short power‑cord hanging from the back
                     m.Cylinder(new Vector3(0f, 0.05f, -0.18f), 0.01f, 0.12f, 6, DecorPalette.Matte(new Color(0.08f, 0.08f, 0.08f)));
                     break;
                case HomeFurnitureKind.Radio:
                    m.Box(new Vector3(0f, 0.09f, 0f), new Vector3(0.32f, 0.18f, 0.14f), DecorPalette.Matte(new Color(0.30f, 0.20f, 0.12f)));
bool leftKnob = rng.Next(2) == 0;
Vector3 knobPos = new Vector3(leftKnob ? -0.04f : 0.04f, 0.11f, 0.07f);
m.Cylinder(knobPos, 0.015f, 0.02f, 8, DecorPalette.Metal(new Color(0.20f, 0.20f, 0.20f)), Quaternion.Euler(90f, 0f, 0f), 1f, false);
                    break;
                default:
                    FurnitureBuilder.BoxStack(m, rng, Vector3.zero, 0f, 2 + rng.Next(2));
                    break;
            }
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
        /// <summary>Human‑readable name used when demolishing this piece of furniture.</summary>
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
            HomeFurnitureKind.Fridge => "fridge",
            HomeFurnitureKind.Toilet => "toilet",
            HomeFurnitureKind.Basin => "basin",
            HomeFurnitureKind.Shower => "shower",
            HomeFurnitureKind.Tv => "TV",
            HomeFurnitureKind.Radio => "radio",
            _ => "boxes",
        };

        /// <summary>Returns whether the furniture can be safely demolished and, if not, why.</summary>
        public bool CanDemolish(out string reason)
        {
            // Whatever is kept in its drawers or cupboards would be lost with it.
            LootContainer holding = FixtureContents.FirstHolding(transform);
            if (holding != null) { reason = "Empty the " + holding.DisplayName.ToLowerInvariant() + " first"; return false; }
            // Nor can it go from under the TV or the radio and leave that standing on thin air.
            HomeFurniture load = Carrying();
            if (load != null) { reason = "Dismantle the " + load.DemolishName + " on top of it first"; return false; }
            reason = null;
            return true;
        }

        /// <summary>Outputs the amount of planks and nails that can be salvaged from this furniture.</summary>
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
                case HomeFurnitureKind.Fridge: planks = CloneZConfig.SalvageFridgePlanks; nails = CloneZConfig.SalvageFridgeNails; break;
                case HomeFurnitureKind.Shower: planks = CloneZConfig.SalvageShowerPlanks; nails = CloneZConfig.SalvageShowerNails; break;
                default: planks = 0; nails = 0; break;   // a lamp, cardboard boxes, porcelain, electronics: nothing a builder can use
            }
        }

        /// <summary>Indicates whether this piece contributes to the navigation mesh.</summary>
        public bool CarvesNavMesh => BakesIntoNavMesh(Kind);   // baked into the navmesh -> removing it needs a rebuild

        /// <summary>Removes the furniture from the world and cleans up its resources.</summary>
        public void Dismantle()
        {
            All.Remove(this);
            gameObject.SetActive(false);   // drop out of the navmesh bake before the rebuild runs
            Destroy(gameObject);
        }
        #endregion
    }
}
