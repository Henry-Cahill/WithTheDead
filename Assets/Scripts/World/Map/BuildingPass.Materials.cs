using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The buildings' own textures and colours: brick, lap siding, barn boards, ribbed metal, painted block, limestone,
    // stucco, trailer siding and precast concrete for the walls; three-tab shingles, standing-seam metal, tar, slate and
    // the snow lying on them for the roofs; striped canvas for awnings; the painted window grids of tall and far
    // buildings. Each is painted once per session per colour (from small palettes, so a streamed state never grows past
    // a few dozen) and every planar surface tiles in world metres with a period that divides RaidMesh.PlanarPeriod, so no
    // seam shows where a streamed piece's UVs wrap. Wall textures carry no foundation band of their own (DecorTextures'
    // do): the foundations are geometry. Lit windows and lamp globes glow through an emission that follows the dark
    // (see Glowing): faint by day, bright by night, so a lit window reads as one after dark without looking like a yellow
    // board at noon. Candidates for DecorMaterials once the revamp settles.
    internal static partial class BuildingPass
    {
        internal enum WallKind { Brick, Siding, Boards, Metal, Block, Stone, Stucco, Trailer, Concrete }
        internal enum RoofCover { Shingle, Metal, Copper, Membrane, Slate }

        #region Palettes
        // Kentucky in 1993: red and brown brick, white and pastel clapboard, black and red barns, galvanised roofs.
        private static readonly Color[] BrickTones =
        {
            new Color(0.56f, 0.27f, 0.20f), new Color(0.47f, 0.21f, 0.16f), new Color(0.44f, 0.30f, 0.22f),
            new Color(0.63f, 0.36f, 0.25f), new Color(0.74f, 0.62f, 0.46f), new Color(0.86f, 0.85f, 0.81f),
            new Color(0.52f, 0.33f, 0.28f),
        };
        private const int BrickPainted = 5;
        private static readonly Color[] SidingTones =
        {
            new Color(0.92f, 0.92f, 0.89f), new Color(0.89f, 0.85f, 0.72f), new Color(0.90f, 0.82f, 0.56f),
            new Color(0.67f, 0.75f, 0.81f), new Color(0.65f, 0.72f, 0.59f), new Color(0.64f, 0.66f, 0.67f),
            new Color(0.77f, 0.69f, 0.55f), new Color(0.82f, 0.80f, 0.76f), new Color(0.55f, 0.24f, 0.20f),
            new Color(0.46f, 0.53f, 0.60f),
        };
        private static readonly Color[] BoardTones =
        {
            new Color(0.50f, 0.15f, 0.12f), new Color(0.11f, 0.10f, 0.09f), new Color(0.46f, 0.43f, 0.39f),
            new Color(0.86f, 0.86f, 0.82f), new Color(0.38f, 0.27f, 0.18f),
            // A tobacco barn's, with every third board hung open: black and weathered grey.
            new Color(0.11f, 0.10f, 0.09f), new Color(0.46f, 0.43f, 0.39f),
        };
        private const int BoardBlack = 1, BoardRed = 0, BoardGrey = 2, TobaccoBlack = 5, TobaccoGrey = 6;
        private static readonly Color[] MetalTones =
        {
            new Color(0.62f, 0.64f, 0.66f), new Color(0.75f, 0.69f, 0.57f), new Color(0.86f, 0.87f, 0.86f),
            new Color(0.36f, 0.48f, 0.62f), new Color(0.34f, 0.46f, 0.36f), new Color(0.45f, 0.35f, 0.27f),
            new Color(0.55f, 0.19f, 0.15f),
        };
        private static readonly Color[] BlockTones =
        {
            new Color(0.88f, 0.87f, 0.83f), new Color(0.84f, 0.80f, 0.68f), new Color(0.66f, 0.66f, 0.64f),
            new Color(0.76f, 0.70f, 0.58f), new Color(0.70f, 0.78f, 0.72f),
        };
        private static readonly Color[] StoneTones = { new Color(0.76f, 0.73f, 0.65f), new Color(0.62f, 0.61f, 0.58f) };
        private static readonly Color[] StuccoTones =
        {
            new Color(0.90f, 0.89f, 0.85f), new Color(0.86f, 0.80f, 0.66f), new Color(0.78f, 0.70f, 0.58f),
        };
        // Trailer siding: the body and the band along it.
        private static readonly Color[] TrailerBodies =
        {
            new Color(0.92f, 0.91f, 0.87f), new Color(0.88f, 0.84f, 0.72f), new Color(0.90f, 0.90f, 0.88f),
            new Color(0.78f, 0.73f, 0.62f),
        };
        private static readonly Color[] TrailerBands =
        {
            new Color(0.42f, 0.28f, 0.18f), new Color(0.30f, 0.44f, 0.30f), new Color(0.28f, 0.38f, 0.56f),
            new Color(0.48f, 0.22f, 0.16f),
        };
        private static readonly Color[] ConcreteTones = { new Color(0.78f, 0.77f, 0.73f), new Color(0.64f, 0.63f, 0.60f) };

        private static readonly Color[] ShingleTones =
        {
            new Color(0.21f, 0.21f, 0.22f), new Color(0.13f, 0.13f, 0.14f), new Color(0.43f, 0.42f, 0.40f),
            new Color(0.34f, 0.26f, 0.20f), new Color(0.22f, 0.31f, 0.25f), new Color(0.42f, 0.17f, 0.14f),
            new Color(0.26f, 0.30f, 0.37f),
        };
        private const int ShingleCharcoal = 0, ShingleBlack = 1, ShingleGrey = 2, ShingleBrown = 3, ShingleGreen = 4,
            ShingleRed = 5, ShingleBlue = 6;
private static readonly Color[] CopperTones =
{
    new Color(0.72f, 0.39f, 0.25f), // bright new‑copper
    new Color(0.60f, 0.30f, 0.20f), // aged copper
    new Color(0.48f, 0.24f, 0.15f), // weathered patina
};
        private const int MetalGalv = 0, MetalRusty = 1, MetalRed = 2, MetalGreen = 3, MetalWhite = 4, MetalBlue = 5, MetalBlack = 6;
        private static readonly Color[] MembraneTones = { new Color(0.24f, 0.24f, 0.25f), new Color(0.50f, 0.48f, 0.45f) };
        private static readonly Color[] SlateTones = { new Color(0.29f, 0.30f, 0.35f) };
        // Awnings: canvas stripes, the colour and white (or cream).
        private static readonly Color[] AwningTones =
        {
            new Color(0.17f, 0.36f, 0.24f), new Color(0.60f, 0.13f, 0.12f), new Color(0.17f, 0.25f, 0.45f),
            new Color(0.42f, 0.16f, 0.20f), new Color(0.65f, 0.48f, 0.14f), new Color(0.26f, 0.26f, 0.27f),
        };

        /// <summary>White and cream trim, dark greens, black, brown and oxblood: window frames, fascias, corner boards.</summary>
        private static readonly Color[] TrimTones =
        {
            new Color(0.93f, 0.93f, 0.90f), new Color(0.88f, 0.84f, 0.72f), new Color(0.16f, 0.27f, 0.20f),
            new Color(0.12f, 0.12f, 0.12f), new Color(0.36f, 0.24f, 0.16f), new Color(0.38f, 0.14f, 0.13f),
            new Color(0.62f, 0.62f, 0.60f),
        };
        private const int TrimWhite = 0, TrimCream = 1, TrimGreen = 2, TrimBlack = 3, TrimBrown = 4, TrimOxblood = 5, TrimGrey = 6;
        /// <summary>Shutters and front doors: deep, slightly faded colours.</summary>
        private static readonly Color[] ShutterTones =
        {
            new Color(0.11f, 0.20f, 0.15f), new Color(0.10f, 0.10f, 0.11f), new Color(0.36f, 0.11f, 0.10f),
            new Color(0.16f, 0.22f, 0.34f), new Color(0.33f, 0.24f, 0.16f), new Color(0.88f, 0.88f, 0.85f),
        };

        private static Color[] TonesOf(WallKind kind)
        {
            switch (kind)
            {
                case WallKind.Brick: return BrickTones;
                case WallKind.Siding: return SidingTones;
                case WallKind.Boards: return BoardTones;
                case WallKind.Metal: return MetalTones;
                case WallKind.Block: return BlockTones;
                case WallKind.Stone: return StoneTones;
                case WallKind.Stucco: return StuccoTones;
                case WallKind.Trailer: return TrailerBodies;
                default: return ConcreteTones;
            }
        }

        private static Color[] TonesOf(RoofCover cover)
        {
            switch (cover)
            {
                case RoofCover.Shingle: return ShingleTones;
                case RoofCover.Copper: return CopperTones;
                case RoofCover.Membrane: return MembraneTones;
                default: return SlateTones;
            }
        }

        /// <summary>The tone in <paramref name="tones"/> nearest a mapped "#rrggbb" colour, or -1 when there's none.</summary>
        private static int NearestTone(string hex, Color[] tones)
        {
            if (!TryParseHex(hex, out Color c)) return -1;
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < tones.Length; i++)
            {
                float dr = tones[i].r - c.r, dg = tones[i].g - c.g, db = tones[i].b - c.b;
                float d = dr * dr * 0.3f + dg * dg * 0.59f + db * db * 0.11f;
                if (d >= bestD) continue;
                bestD = d;
                best = i;
            }
            return best;
        }

        private static bool TryParseHex(string hex, out Color c)
        {
            c = Color.white;
            if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#') return false;
            int v = 0;
            for (int i = 1; i < 7; i++)
            {
                int d = HexDigit(hex[i]);
                if (d < 0) return false;
                v = v * 16 + d;
            }
            c = new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
            return true;
        }

        private static int HexDigit(char ch)
        {
            if (ch >= '0' && ch <= '9') return ch - '0';
            if (ch >= 'a' && ch <= 'f') return ch - 'a' + 10;
            if (ch >= 'A' && ch <= 'F') return ch - 'A' + 10;
            return -1;
        }
        #endregion

        #region Surfaces
        // Metres per repeat; each divides RaidMesh.PlanarPeriod (252 m).
        private const float BrickPeriod = 1.26f, SidingPeriod = 1.2f, BoardPeriod = 2.4f, MetalWallPeriod = 0.9f,
            BlockPeriod = 1.2f, StonePeriod = 2.4f, StuccoPeriod = 2.4f, TrailerPeriod = 3.6f, ConcretePeriod = 3.6f,
            ShinglePeriod = 1.68f, SeamPeriod = 1.8f, MembranePeriod = 2.4f, SlatePeriod = 1.2f, RoofSnowPeriod = 6f,
            AwningPeriod = 0.6f;

        private static readonly Dictionary<int, Surf> s_Surfs = new Dictionary<int, Surf>(64);
        private static readonly Dictionary<int, Surf> s_Fitted = new Dictionary<int, Surf>(32);
        private static readonly Dictionary<int, Material> s_Mats = new Dictionary<int, Material>(64);
        private static Shader s_Lit;

        /// <summary>A wall finish, tiled in world metres.</summary>
        private static Surf WallSurf(WallKind kind, int tone)
        {
            Color[] tones = TonesOf(kind);
            tone = Mathf.Clamp(tone, 0, tones.Length - 1);
            int key = 1000 + (int)kind * 32 + tone;
            if (s_Surfs.TryGetValue(key, out Surf s) && s.Valid) return s;
            Texel texel = WallTexel(kind, tone, out string name, out int size);
            Texture2D tex = Paint(name, size, texel);
            float period, smooth = 0.08f;
            switch (kind)
            {
                case WallKind.Brick: period = BrickPeriod; break;
                case WallKind.Siding: period = SidingPeriod; smooth = 0.14f; break;
                case WallKind.Boards: period = BoardPeriod; break;
                case WallKind.Metal: period = MetalWallPeriod; smooth = 0.3f; break;
                case WallKind.Block: period = BlockPeriod; break;
                case WallKind.Stone: period = StonePeriod; break;
                case WallKind.Stucco: period = StuccoPeriod; break;
                case WallKind.Trailer: period = TrailerPeriod; smooth = 0.25f; break;
                default: period = ConcretePeriod; break;
            }
            s = new Surf(MaterialFor(key, tex, smooth, kind == WallKind.Metal ? 0.25f : 0f), Vector2.zero, 1f / period);
            s_Surfs[key] = s;
            return s;
        }

        /// <summary>A roof covering, tiled in world metres (for faces drawn as triangles).</summary>
        private static Surf RoofSurf(RoofCover cover, int tone) => RoofSurface(cover, tone, false);

        /// <summary>The same covering for faces with explicit UVs (RaidMesh.QuadUV): rows run along the eaves and ribs down
        /// the slope, one repeat per <see cref="RoofPeriod"/>.</summary>
        private static Surf RoofSurfFitted(RoofCover cover, int tone) => RoofSurface(cover, tone, true);

private static float RoofPeriod(RoofCover cover)
    => cover == RoofCover.Shingle ? ShinglePeriod :
       cover == RoofCover.Metal ? SeamPeriod :
       cover == RoofCover.Copper ? SeamPeriod :
       cover == RoofCover.Slate ? SlatePeriod : MembranePeriod;

        private static Surf RoofSurface(RoofCover cover, int tone, bool fitted)
        {
            Color[] tones = TonesOf(cover);
            tone = Mathf.Clamp(tone, 0, tones.Length - 1);
            int key = 2000 + (int)cover * 32 + tone;
            Dictionary<int, Surf> cache = fitted ? s_Fitted : s_Surfs;
            if (cache.TryGetValue(key, out Surf s) && s.Valid) return s;
            Texture2D tex = Paint(RoofTexel(cover, tone, out string texName, out int size), texName, size);
            float smooth = 0.1f, metal = 0f;
            switch (cover)
            {
                case RoofCover.Metal: smooth = 0.35f; metal = 0.3f; break;
                case RoofCover.Slate: smooth = 0.25f; break;
            }
            Material m = MaterialFor(key, tex, smooth, metal);
            Surf planar = new Surf(m, Vector2.zero, 1f / RoofPeriod(cover));
            Surf explicitUv = new Surf(m, Vector2.zero, 0f);
            s_Surfs[key] = planar;
            s_Fitted[key] = explicitUv;
            return fitted ? explicitUv : planar;
        }

        /// <summary>Snow lying on a roof: brighter and smoother than the trodden ground's.</summary>
        private static Surf RoofSnow
        {
            get
            {
                const int key = 3000;
                if (s_Surfs.TryGetValue(key, out Surf s) && s.Valid) return s;
                Texture2D tex = Paint("roofsnow", 128, RoofSnowTexel);
                s = new Surf(MaterialFor(key, tex, 0.22f, 0f), Vector2.zero, 1f / RoofSnowPeriod);
                s_Surfs[key] = s;
                return s;
            }
        }

        /// <summary>Striped awning canvas, for explicit UVs (stripes run down the slope, one pair per 0.6 m).</summary>
        private static Surf AwningSurf(int tone)
        {
            tone = Mathf.Clamp(tone, 0, AwningTones.Length - 1);
            int key = 3100 + tone;
            if (s_Fitted.TryGetValue(key, out Surf s) && s.Valid) return s;
            Color a = AwningTones[tone];
            Texture2D tex = Paint("awning" + tone, 64, (x, y) => AwningTexel(x, y, a));
            s = new Surf(MaterialFor(key, tex, 0.05f, 0f), Vector2.zero, 0f);
            s_Fitted[key] = s;
            return s;
        }

        private static Material MaterialFor(int key, Texture2D tex, float smoothness, float metallic)
        {
            if (s_Mats.TryGetValue(key, out Material m) && m != null) return m;
            if (s_Lit == null) s_Lit = Shader.Find("Universal Render Pipeline/Lit");
            if (s_Lit == null) s_Lit = Shader.Find("Standard");
            m = new Material(s_Lit) { name = "Bldg_" + tex.name, hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            s_Mats[key] = m;
            return m;
        }

        private delegate Color Texel(int x, int y);

        // The texel painter of each wall finish and roof covering, its texture's name and size: what WallSurf and
        // RoofSurface paint, and what the background painter (PrePaint) paints ahead of them.
        private static Texel WallTexel(WallKind kind, int tone, out string name, out int size)
        {
            Color[] tones = TonesOf(kind);
            tone = Mathf.Clamp(tone, 0, tones.Length - 1);
            Color c = tones[tone];
            size = 128;
            switch (kind)
            {
                case WallKind.Brick: name = "brick" + tone; bool painted = tone == BrickPainted; return (x, y) => BrickTexel(x, y, c, painted);
                case WallKind.Siding: name = "siding" + tone; return (x, y) => SidingTexel(x, y, c);
                case WallKind.Boards: name = "boards" + tone; bool tobacco = tone >= TobaccoBlack; return (x, y) => BoardTexel(x, y, c, tobacco);
                case WallKind.Metal: name = "ribwall" + tone; size = 64; return (x, y) => RibTexel(x, y, 64, 4, c, false);
                case WallKind.Block: name = "block" + tone; return (x, y) => BlockTexel(x, y, c);
                case WallKind.Stone: name = "stone" + tone; return (x, y) => StoneTexel(x, y, c);
                case WallKind.Stucco: name = "stucco" + tone; size = 64; return (x, y) => StuccoTexel(x, y, c);
                case WallKind.Trailer:
                    name = "trailer" + tone;
                    Color band = TrailerBands[tone % TrailerBands.Length];
                    return (x, y) => TrailerTexel(x, y, c, band);
                default: name = "precast" + tone; return (x, y) => ConcreteTexel(x, y, c);
            }
        }

        private static Texel RoofTexel(RoofCover cover, int tone, out string name, out int size)
        {
            Color[] tones = TonesOf(cover);
            tone = Mathf.Clamp(tone, 0, tones.Length - 1);
            Color c = tones[tone];
            size = 128;
            switch (cover)
            {
                case RoofCover.Shingle: name = "shingle" + tone; return (x, y) => ShingleTexel(x, y, c);
                case RoofCover.Metal: name = "seam" + tone; bool weathered = tone == MetalGalv || tone == MetalRusty; return (x, y) => SeamTexel(x, y, c, weathered);
                case RoofCover.Slate: name = "slate" + tone; return (x, y) => SlateTexel(x, y, c);
                default: name = "tar" + tone; size = 64; return (x, y) => MembraneTexel(x, y, c);
            }
        }

        private static Texture2D Paint(Texel texel, string name, int size) => Paint(name, size, texel);

        private static Texture2D Paint(string name, int size, Texel texel)
        {
            Color32[] px = Painted(name, size, texel);
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Bldg_" + name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            t.SetPixels32(px);
            t.Apply(true, true);
            return t;
        }

        // A texture's pixels: from the background painter when it got there first, else painted now.
        private static Color32[] Painted(string name, int size, Texel texel)
        {
            lock (s_Ahead)
            {
                s_Taken.Add(name);
                if (s_Ahead.TryGetValue(name, out Color32[] ready) && ready.Length == size * size)
                {
                    s_Ahead.Remove(name);
                    return ready;
                }
            }
            return PaintPixels(size, texel);
        }

        private static Color32[] PaintPixels(int size, Texel texel)
        {
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color c = texel(x, y);
                    px[y * size + x] = new Color32((byte)Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255), (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255), 255);
                }
            return px;
        }

        // Pixels painted ahead of need, by name; the names already made (or being made) on the main thread, which the
        // background painter skips; and whether it has been started this session.
        private static readonly Dictionary<string, Color32[]> s_Ahead = new Dictionary<string, Color32[]>(96);
        private static readonly HashSet<string> s_Taken = new HashSet<string>();
        private static bool s_PrePainted;

        /// <summary>Starts painting, on a worker thread, every wall and roof texture not yet made (the most used first), so
        /// a streamed piece meeting a finish for the first time doesn't paint it inside its time slice. Once a session.
        /// The painters are pure functions of their texel's place, so the pixels are the same whichever thread paints them.</summary>
        private static void PrePaint()
        {
            if (s_PrePainted) return;
            s_PrePainted = true;
            var jobs = new List<(string name, int size, Texel texel)>(96);
            void Wall(WallKind kind)
            {
                for (int tone = 0; tone < TonesOf(kind).Length; tone++)
                {
                    if (s_Surfs.ContainsKey(1000 + (int)kind * 32 + tone)) continue;
                    Texel t = WallTexel(kind, tone, out string name, out int size);
                    jobs.Add((name, size, t));
                }
            }
            void Roof(RoofCover cover)
            {
                for (int tone = 0; tone < TonesOf(cover).Length; tone++)
                {
                    if (s_Surfs.ContainsKey(2000 + (int)cover * 32 + tone)) continue;
                    Texel t = RoofTexel(cover, tone, out string name, out int size);
                    jobs.Add((name, size, t));
                }
            }
            Roof(RoofCover.Shingle);
            Wall(WallKind.Siding);
            Wall(WallKind.Brick);
            Roof(RoofCover.Metal);
            Roof(RoofCover.Membrane);
            Wall(WallKind.Boards);
            Wall(WallKind.Block);
            Wall(WallKind.Metal);
            Wall(WallKind.Trailer);
            Wall(WallKind.Stone);
            Wall(WallKind.Concrete);
            Wall(WallKind.Stucco);
            Roof(RoofCover.Slate);
            if (jobs.Count == 0) return;
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    foreach ((string name, int size, Texel texel) in jobs)
                    {
                        lock (s_Ahead)
                            if (s_Taken.Contains(name)) continue;
                        Color32[] px = PaintPixels(size, texel);
                        lock (s_Ahead)
                            if (!s_Taken.Contains(name)) s_Ahead[name] = px;
                    }
                }
                catch (System.Exception)
                {
                    // Whatever wasn't painted ahead is painted when it's needed.
                }
            });
        }
        #endregion

        #region Painting
        private static uint TexHash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }

        private static float TexRand(int x, int y, int seed) => (TexHash(x, y, seed) & 0xFFFFFF) / 16777215f;

        // Tileable value noise over the texture: cells lattice points across, wrapping.
        private static float TexNoise(float u, float v, int cells, int seed)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int x0 = ((xi % cells) + cells) % cells, x1 = (x0 + 1) % cells;
            int y0 = ((yi % cells) + cells) % cells, y1 = (y0 + 1) % cells;
            float a = TexRand(x0, y0, seed), b = TexRand(x1, y0, seed), c = TexRand(x0, y1, seed), d = TexRand(x1, y1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float TexFbm(float u, float v, int cells, int seed)
            => TexNoise(u, v, cells, seed) * 0.57f + TexNoise(u, v, cells * 2, seed + 7) * 0.29f + TexNoise(u, v, cells * 4, seed + 13) * 0.14f;

        private static Color Shade(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        // Running bond, 16 courses and six bricks a course per 1.26 m; each brick its own shade, light mortar joints.
        private static Color BrickTexel(int x, int y, Color brick, bool painted)
        {
            const int S = 128, Courses = 16, Bricks = 6;
            float fy = y * Courses / (float)S;
            int course = Mathf.FloorToInt(fy);
            float bx = x * Bricks / (float)S + ((course & 1) == 0 ? 0f : 0.5f);
            int b = Mathf.FloorToInt(bx);
            float jy = fy - course, jx = bx - b;
            var mortar = painted ? Shade(brick, 0.88f) : new Color(0.66f, 0.64f, 0.60f);
            if (jy < 0.14f || jx < 0.045f) return Shade(mortar, 0.94f + TexRand(x, y, 31) * 0.1f);
            float k = painted ? 0.95f + TexRand(b % Bricks, course, 32) * 0.07f : 0.80f + TexRand(b % Bricks, course, 33) * 0.32f;
            Color c = Shade(brick, k);
            if (!painted && TexRand(b % Bricks, course, 34) < 0.12f) c = Color.Lerp(c, new Color(0.30f, 0.17f, 0.13f), 0.35f);
            float grain = (TexRand(x, y, 35) - 0.5f) * 0.08f;
            return Shade(c, 1f + grain + (TexFbm(x / (float)S, y / (float)S, 4, 36) - 0.5f) * 0.12f);
        }

        // Ten painted lap boards per 1.2 m: lighter at each board's top, a shadow line under its lower edge.
        private static Color SidingTexel(int x, int y, Color paint)
        {
            const int S = 128, Boards = 10;
            float fy = y * Boards / (float)S;
            float t = fy - Mathf.FloorToInt(fy);   // 0 at a board's lower edge, 1 at its top
            float k = 0.86f + 0.16f * t;
            if (t < 0.12f) k = 0.58f + t * 1.5f;
            int board = Mathf.FloorToInt(fy);
            // A butt joint here and there, staggered.
            int joint = (int)(TexHash(board, 0, 41) % S);
            if (Mathf.Abs(x - joint) < 1) k *= 0.8f;
            float grain = (TexNoise(x / (float)S * 8f, y / (float)S, 16, 42) - 0.5f) * 0.05f;
            return Shade(paint, k + grain + (TexFbm(x / (float)S, y / (float)S, 3, 43) - 0.5f) * 0.06f);
        }

        // Vertical boards 0.3 m wide with battens over the joints; a tobacco barn's every third board hangs open on a
        // dark gap (they're hinged to cure the leaf).
        private static Color BoardTexel(int x, int y, Color paint, bool tobacco)
        {
            const int S = 128, Boards = 8;
            float fx = x * Boards / (float)S;
            int board = Mathf.FloorToInt(fx);
            float t = fx - board;
            float k = 0.84f + TexRand(board, 0, 51) * 0.16f;
            float weather = TexFbm(x / (float)S, y / (float)S, 5, 52);
            if (t < 0.12f) k = 1.06f;                       // the batten
            else if (t < 0.16f) k = 0.7f;                   // its shadow
            if (tobacco && board % 3 == 1 && t > 0.2f && t < 0.42f) return Shade(paint, 0.35f);
            float grain = (TexNoise(x / (float)S, y / (float)S * 6f, 24, 53) - 0.5f) * 0.12f;
            return Shade(paint, (k + grain) * (0.9f + weather * 0.2f));
        }

        // Ribbed sheet: trapezoidal ribs, ribs per tile across x; the odd rust streak when rusty.
        private static Color RibTexel(int x, int y, int S, int ribs, Color paint, bool rusty)
        {
            float fx = x * ribs / (float)S;
            float t = fx - Mathf.FloorToInt(fx);
            float k;
            if (t < 0.12f) k = 1.12f;
            else if (t < 0.2f) k = 0.72f;
            else k = 0.92f + Mathf.Sin(t * Mathf.PI) * 0.04f;
            Color c = Shade(paint, k * (0.94f + TexFbm(x / (float)S, y / (float)S, 4, 61) * 0.12f));
            if (rusty)
            {
                float streak = TexNoise(x / (float)S, y / (float)S * 0.25f, 16, 62);
                if (streak > 0.7f) c = Color.Lerp(c, new Color(0.45f, 0.26f, 0.15f), (streak - 0.7f) * 2f);
            }
            return c;
        }

        // Painted concrete block, 0.4 x 0.2 m, raked joints.
        private static Color BlockTexel(int x, int y, Color paint)
        {
            const int S = 128;
            float fy = y * 6f / S;
            int course = Mathf.FloorToInt(fy);
            float fx = x * 3f / S + ((course & 1) == 0 ? 0f : 0.5f);
            float jy = fy - course, jx = fx - Mathf.FloorToInt(fx);
            if (jy < 0.06f || jx < 0.03f) return Shade(paint, 0.8f);
            return Shade(paint, 0.94f + TexRand(x, y, 71) * 0.06f + (TexFbm(x / (float)S, y / (float)S, 6, 72) - 0.5f) * 0.08f);
        }

        // Kentucky limestone in courses 0.3 m high of random lengths.
        private static Color StoneTexel(int x, int y, Color stone)
        {
            const int S = 128, Courses = 8;
            float fy = y * Courses / (float)S;
            int course = Mathf.FloorToInt(fy);
            float jy = fy - course;
            // Two to four blocks a course, their joints from the course's hash.
            int blocks = 2 + (int)(TexHash(course, 0, 81) % 3);
            float off = TexRand(course, 1, 82);
            float fx = x / (float)S * blocks + off;
            int block = Mathf.FloorToInt(fx);
            float jx = fx - block;
            if (jy < 0.05f || jx < 0.015f) return Shade(stone, 0.72f);
            float k = 0.9f + TexRand(block, course, 83) * 0.14f;
            return Shade(stone, k + (TexFbm(x / (float)S, y / (float)S, 8, 84) - 0.5f) * 0.14f);
        }

        private static Color StuccoTexel(int x, int y, Color paint)
            => Shade(paint, 0.93f + TexFbm(x / 64f, y / 64f, 8, 91) * 0.1f + (TexRand(x, y, 92) - 0.5f) * 0.04f);

        // A trailer's ribbed aluminium, a coloured band 1.1-1.3 m up, vinyl skirting below 0.55 m.
        private static Color TrailerTexel(int x, int y, Color body, Color band)
        {
            const int S = 128;
            float h = y * TrailerPeriod / S;
            if (h < 0.55f)
            {
                float fx = x * 18f / S;
                float t = fx - Mathf.FloorToInt(fx);
                return Shade(new Color(0.80f, 0.79f, 0.76f), t < 0.15f ? 0.78f : 0.95f);
            }
            if (h < 0.6f) return Shade(body, 0.6f);
            float rib = x * 18f / S;
            float r = rib - Mathf.FloorToInt(rib);
            Color c = h > 1.1f && h < 1.3f ? band : body;
            return Shade(c, (r < 0.12f ? 0.84f : 0.98f) * (0.96f + TexFbm(x / (float)S, y / (float)S, 4, 101) * 0.06f));
        }

        // Precast panels with joints every 1.8 m across and 3.6 m up.
        private static Color ConcreteTexel(int x, int y, Color c)
        {
            const int S = 128;
            if (x % 64 < 1 || y < 1) return Shade(c, 0.7f);
            return Shade(c, 0.94f + TexFbm(x / (float)S, y / (float)S, 6, 111) * 0.1f + (TexRand(x, y, 112) - 0.5f) * 0.03f);
        }

        // Three-tab shingles: twelve courses of 0.14 m per 1.68 m, tabs 0.28 m with keyways, staggered by half a tab;
        // granules speckle each tab and the tabs vary a little (a blend, as they were sold).
        private static Color ShingleTexel(int x, int y, Color tone)
        {
            const int S = 128, Courses = 12, Tabs = 6;
            float fy = y * Courses / (float)S;
            int course = Mathf.FloorToInt(fy);
            float t = fy - course;   // 0 at the course's exposed lower edge
            float fx = x * Tabs / (float)S + ((course & 1) == 0 ? 0f : 0.5f);
            int tab = Mathf.FloorToInt(fx);
            float jx = fx - tab;
            float k = 0.86f + TexRand(tab % Tabs, course, 121) * 0.22f;
            if (t < 0.1f) k *= 0.55f;                    // the drip shadow under the course above
            else if (jx < 0.035f && t < 0.75f) k *= 0.5f;  // a keyway
            float speck = TexRand(x, y, 122);
            if (speck > 0.93f) k *= 1.25f;
            else if (speck < 0.07f) k *= 0.75f;
            return Shade(tone, k);
        }

        // Standing seam: four 0.45 m pans per 1.8 m, a bright raised seam and its shadow; galvanised and rusty ones
        // mottled, with rust running down from the seams.
        private static Color SeamTexel(int x, int y, Color tone, bool weathered)
        {
            const int S = 128, Pans = 4;
            float fx = x * Pans / (float)S;
            float t = fx - Mathf.FloorToInt(fx);
            float k = 0.93f + Mathf.Sin(t * Mathf.PI) * 0.05f;
            if (t < 0.04f) k = 1.2f;
            else if (t < 0.09f) k = 0.66f;
            Color c = Shade(tone, k);
            if (weathered)
            {
                float m = TexFbm(x / (float)S, y / (float)S, 6, 131);
                c = Shade(c, 0.9f + m * 0.2f);
                float streak = TexNoise(x / (float)S, y / (float)S * 0.2f, 32, 132);
                if (streak > 0.66f) c = Color.Lerp(c, new Color(0.46f, 0.27f, 0.16f), Mathf.Clamp01((streak - 0.66f) * 2.5f));
            }
            return c;
        }

        private static Color SlateTexel(int x, int y, Color tone)
        {
            const int S = 128, Courses = 8, Tiles = 4;
            float fy = y * Courses / (float)S;
            int course = Mathf.FloorToInt(fy);
            float t = fy - course;
            float fx = x * Tiles / (float)S + ((course & 1) == 0 ? 0f : 0.5f);
            int tile = Mathf.FloorToInt(fx);
            float jx = fx - tile;
            float k = 0.82f + TexRand(tile % Tiles, course, 141) * 0.3f;
            if (t < 0.08f || jx < 0.03f) k *= 0.6f;
            return Shade(tone, k);
        }

        private static Color MembraneTexel(int x, int y, Color tone)
            => Shade(tone, 0.85f + TexFbm(x / 64f, y / 64f, 8, 151) * 0.2f + (TexRand(x, y, 152) - 0.5f) * 0.12f);

        // Fresh snow on a roof: near white, the faintest blue in the hollows, wind ripples across it and a glint here and
        // there.
        private static Color RoofSnowTexel(int x, int y)
        {
            const int S = 128;
            float u = x / (float)S, v = y / (float)S;
            float n = TexFbm(u, v, 3, 161) * 0.7f + TexNoise(u, v, 24, 162) * 0.3f;
            // Sastrugi: shallow ripples the wind combs into it, bent by the noise (whole waves per tile, so it tiles).
            float ripple = Mathf.Sin((u * 5f + v * 2f) * Mathf.PI * 2f + TexNoise(u, v, 6, 164) * 4f);
            n += ripple * 0.06f;
            Color c = Color.Lerp(new Color(0.80f, 0.84f, 0.90f), new Color(0.97f, 0.98f, 1.0f), Mathf.SmoothStep(0.15f, 0.85f, n));
            float r = TexRand(x, y, 163);
            if (r > 0.996f) c = Color.white;
            return c;
        }

        // Canvas stripes 0.15 m wide: the colour and an off-white.
        private static Color AwningTexel(int x, int y, Color tone)
        {
            const int S = 64;
            bool coloured = x * 4 / S % 2 == 0;
            Color c = coloured ? tone : new Color(0.90f, 0.88f, 0.82f);
            return Shade(c, 0.92f + TexFbm(x / (float)S, y / (float)S, 8, 171) * 0.1f);
        }
        #endregion

        #region Lights after dark
        // Materials whose emission follows the dark: their full emission colours, and the share of it shown now.
        private static readonly List<Material> s_Glowing = new List<Material>(16);
        private static readonly List<Color> s_GlowFull = new List<Color>(16);
        private static float s_GlowShown = -1f;
        private static bool s_GlowHooked;
        private static readonly int s_EmissionColor = Shader.PropertyToID("_EmissionColor");

        /// <summary>Lit windows' and lamps' emission, as a share of full, by how dark the scene is: by day a hint of warmth
        /// in the glass, no more (at 8% a lit pane on a shaded wall read as an orange board).</summary>
        private const float GlowByDay = 0.03f;

        // Makes <paramref name="m"/> glow with <paramref name="emission"/> (times its emission map, if it has one) after dark,
        // a little by day: once a camera renders, the share follows the ambient light (which the day and night cycle sets,
        // and so does the map harness), worked out once a frame for all of them.
        private static void Glowing(Material m, Color emission)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            s_Glowing.Add(m);
            s_GlowFull.Add(emission);
            m.SetColor(s_EmissionColor, emission * GlowShare());
            // Made under whatever light the build had: the next camera to render sets every one again.
            s_GlowShown = -1f;
            if (s_GlowHooked) return;
            s_GlowHooked = true;
            RenderPipelineManager.beginCameraRendering += OnCameraRendering;
        }

        private static void OnCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            float share = GlowShare();
            if (Mathf.Abs(share - s_GlowShown) < 0.005f) return;
            s_GlowShown = share;
#if UNITY_EDITOR
            if (s_Profiling) Debug.Log($"[Buildings] glow {share:F2} for {s_Glowing.Count} materials (ambient {RenderSettings.ambientLight}, camera {camera.name})");
#endif
            for (int i = s_Glowing.Count - 1; i >= 0; i--)
            {
                Material m = s_Glowing[i];
                if (m == null)
                {
                    s_Glowing.RemoveAt(i);
                    s_GlowFull.RemoveAt(i);
                    continue;
                }
                m.SetColor(s_EmissionColor, s_GlowFull[i] * share);
            }
        }

        // 1 at night, GlowByDay at noon: from the ambient light's brightest channel (day about 0.42, night about 0.1).
        private static float GlowShare()
        {
            Color a = RenderSettings.ambientLight;
            float level = Mathf.Max(a.r, Mathf.Max(a.g, a.b));
            float dark = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.15f, level));
            return Mathf.Lerp(GlowByDay, 1f, dark);
        }

        // The panes of lit windows, lit stained glass and the globes of lamps that are on: a palette of a few tones (base
        // and emission side by side in two small point-sampled textures), one material.
        private static Material s_PaneMat;
        private const int PaneColumns = 8, PaneTones = 4;

        /// <summary>Stained glass: blue, ruby, amber, green.</summary>
        private static readonly Color[] Stained =
        {
            new Color(0.12f, 0.18f, 0.44f), new Color(0.48f, 0.10f, 0.10f), new Color(0.58f, 0.40f, 0.12f), new Color(0.12f, 0.32f, 0.18f),
        };

        // Row 0: lamplight panes (by day glass like the rest with a hint of the room's light; their light is all in the
        // emission), then stained glass; row 1: lamp globes.
        private static readonly Color[] PaneBase =
        {
            new Color(0.15f, 0.14f, 0.13f), new Color(0.16f, 0.15f, 0.12f), new Color(0.12f, 0.14f, 0.17f), new Color(0.16f, 0.13f, 0.11f),
            new Color(0.07f, 0.11f, 0.26f), new Color(0.29f, 0.06f, 0.06f), new Color(0.35f, 0.24f, 0.07f), new Color(0.07f, 0.19f, 0.11f),
            new Color(0.93f, 0.91f, 0.84f), new Color(0.95f, 0.92f, 0.80f), new Color(0.93f, 0.91f, 0.84f), new Color(0.95f, 0.92f, 0.80f),
            new Color(0.93f, 0.91f, 0.84f), new Color(0.95f, 0.92f, 0.80f), new Color(0.93f, 0.91f, 0.84f), new Color(0.95f, 0.92f, 0.80f),
        };
        private static readonly Color[] PaneGlow =
        {
            new Color(1.00f, 0.78f, 0.46f), new Color(1.00f, 0.86f, 0.42f), new Color(0.58f, 0.72f, 1.00f), new Color(1.00f, 0.66f, 0.34f),
            new Color(0.25f, 0.40f, 1.00f), new Color(1.00f, 0.22f, 0.20f), new Color(1.00f, 0.72f, 0.24f), new Color(0.26f, 0.80f, 0.40f),
            new Color(1.00f, 0.86f, 0.62f), new Color(1.00f, 0.80f, 0.50f), new Color(1.00f, 0.86f, 0.62f), new Color(1.00f, 0.80f, 0.50f),
            new Color(1.00f, 0.86f, 0.62f), new Color(1.00f, 0.80f, 0.50f), new Color(1.00f, 0.86f, 0.62f), new Color(1.00f, 0.80f, 0.50f),
        };

        /// <summary>A lit window's pane: warm lamplight, a yellower one, a television's blue, amber.</summary>
        private static Surf LitPane(int tone) => PaneSurf(Mathf.Abs(tone) % PaneTones, 0);

        /// <summary>Stained glass lit from inside (tone as <see cref="Stained"/>).</summary>
        private static Surf StainedGlow(int tone) => PaneSurf(PaneTones + Mathf.Abs(tone) % PaneTones, 0);

        /// <summary>A lamp's globe, lit.</summary>
        private static Surf LampGlobe(int tone) => PaneSurf(Mathf.Abs(tone) % PaneTones, 1);

        private static Surf PaneSurf(int x, int y)
        {
            if (s_PaneMat == null)
            {
                Texture2D tex = Swatches("panes", PaneBase), glow = Swatches("paneglow", PaneGlow);
                s_PaneMat = NewLit("Bldg_Panes", tex, 0.8f, 0f);
                s_PaneMat.SetTexture("_EmissionMap", glow);
                Glowing(s_PaneMat, new Color(0.85f, 0.85f, 0.85f));
            }
            return new Surf(s_PaneMat, new Vector2((x + 0.5f) / PaneColumns, (y + 0.5f) / 2f), 0f);
        }

        // A PaneColumns x 2 swatch texture, point-sampled.
        private static Texture2D Swatches(string name, Color[] colours)
        {
            var t = new Texture2D(PaneColumns, 2, TextureFormat.RGBA32, false)
            {
                name = "Bldg_" + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            t.SetPixels(colours);
            t.Apply(false, true);
            return t;
        }

        private static Material NewLit(string name, Texture2D tex, float smoothness, float metallic)
        {
            if (s_Lit == null) s_Lit = Shader.Find("Universal Render Pipeline/Lit");
            if (s_Lit == null) s_Lit = Shader.Find("Standard");
            var m = new Material(s_Lit) { name = name, hideFlags = HideFlags.DontUnloadUnusedAsset, enableInstancing = true };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }
        #endregion

        #region Painted window grids
        private static readonly Dictionary<int, Texture2D> s_GridTex = new Dictionary<int, Texture2D>(8);
        private static readonly Dictionary<int, Texture2D> s_GridGlow = new Dictionary<int, Texture2D>(8);
        private static int s_Warmed;

        // Makes the next of the finishes the pass makes on first use (the painted window grids and the pane palette),
        // false once they're all made: the first pieces built make them between their time slices, so the first tall
        // building or Lite piece met later doesn't make them inside its own.
        private static bool WarmNext()
        {
            const int Grids = 5 * 4;
            if (s_Warmed > Grids) return false;
            int k = s_Warmed++;
            if (k == Grids) PaneSurf(0, 0);
            else FacadeSurf((FacadeStyle)(k / 4), (k & 1) == 1, (k & 2) == 2);
            return true;
        }
        // The cells of a 4 x 4 grid whose panes are lit, and those with their blinds down.
        private static readonly bool[] GridLit = { false, true, false, false, false, false, true, false, false, false, false, true, true, false, false, false };
        private static readonly bool[] GridBlind = { false, false, false, true, false, false, false, false, true, false, false, false, false, false, true, false };

        /// <summary>A grid of painted windows (fitted UVs: one unit is <see cref="GridCells"/> bays by as many storeys).
        /// <paramref name="lit"/>: a few panes glow after dark; <paramref name="white"/>: white frames (else dark
        /// bronze).</summary>
        private static Surf FacadeSurf(FacadeStyle style, bool lit, bool white)
        {
            int texKey = (int)style * 2 + (white ? 1 : 0);
            int key = 4000 + texKey * 2 + (lit ? 1 : 0);
            if (s_Fitted.TryGetValue(key, out Surf s) && s.Valid) return s;
            if (!s_GridTex.TryGetValue(texKey, out Texture2D tex) || tex == null)
            {
                PaintGrid(style, white, out tex, out Texture2D glow);
                s_GridTex[texKey] = tex;
                s_GridGlow[texKey] = glow;
            }
            Material m = NewLit("Bldg_Grid" + style + (white ? "W" : "D") + (lit ? "Lit" : ""), tex, style == FacadeStyle.Curtain ? 0.7f : 0.5f, 0.05f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest;
            if (lit)
            {
                m.SetTexture("_EmissionMap", s_GridGlow[texKey]);
                Glowing(m, new Color(0.8f, 0.8f, 0.8f));
            }
            s = new Surf(m, Vector2.zero, 0f);
            s_Fitted[key] = s;
            return s;
        }

        // 128 px: 4 x 4 cells of 32, each a bay of one storey with its window(s); alpha 0 where the wall shows.
        private static void PaintGrid(FacadeStyle style, bool white, out Texture2D tex, out Texture2D glow)
        {
            const int S = 128, C = 32;
            var px = new Color32[S * S];
            var em = new Color32[S * S];
            Color frame = white ? new Color(0.90f, 0.90f, 0.87f) : new Color(0.19f, 0.18f, 0.17f);
            Color sill = white ? new Color(0.86f, 0.85f, 0.80f) : new Color(0.62f, 0.61f, 0.58f);
            Color spandrel = new Color(0.16f, 0.18f, 0.20f);
            Color warm = new Color(1.0f, 0.78f, 0.46f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    int cx = x / C, cy = y / C, lx = x % C, ly = y % C;
                    int cell = cy * 4 + cx;
                    bool lit = GridLit[cell], blind = GridBlind[cell];
                    Color c = default;
                    bool solid = false, pane = false;
                    float gy0, gy1;   // the glass's bottom and top in the cell
                    switch (style)
                    {
                        case FacadeStyle.Punched:
                            gy0 = 9f; gy1 = 26f;
                            if (lx >= 10 && lx < 22 && ly >= gy0 && ly < gy1)
                            {
                                solid = true;
                                bool rim = lx == 10 || lx == 21 || ly == (int)gy0 || ly == (int)gy1 - 1 || ly == 17;
                                if (rim) c = frame;
                                else pane = true;
                            }
                            else if (lx >= 9 && lx < 23 && ly >= 7 && ly < 9) { solid = true; c = sill; }
                            break;
                        case FacadeStyle.Ribbon:
                            gy0 = 10f; gy1 = 25f;
                            if (lx >= 1 && lx < 31 && ly >= gy0 && ly < gy1)
                            {
                                solid = true;
                                bool rim = lx == 1 || lx == 30 || lx == 16 || ly == (int)gy0 || ly == (int)gy1 - 1;
                                if (rim) c = frame;
                                else pane = true;
                            }
                            else if (lx >= 1 && lx < 31 && ly >= 8 && ly < 10) { solid = true; c = sill; }
                            break;
                        case FacadeStyle.Band:
                            gy0 = 9f; gy1 = 26f;
                            if (ly >= gy0 && ly < gy1)
                            {
                                solid = true;
                                bool rim = lx == 0 || lx == 16 || ly == (int)gy0 || ly == (int)gy1 - 1;
                                if (rim) c = frame;
                                else pane = true;
                            }
                            break;
                        case FacadeStyle.Small:
                            gy0 = 12f; gy1 = 21f;
                            if (lx >= 12 && lx < 20 && ly >= gy0 && ly < gy1)
                            {
                                solid = true;
                                bool rim = lx == 12 || lx == 19 || ly == (int)gy0 || ly == (int)gy1 - 1;
                                if (rim) c = frame;
                                else pane = true;
                            }
                            else if (lx >= 11 && lx < 21 && ly >= 10 && ly < 12) { solid = true; c = sill; }
                            break;
                        default:   // a curtain wall: a dark spandrel under each floor's vision glass, mullions between
                            gy0 = 8f; gy1 = 32f;
                            solid = true;
                            if (lx == 0 || lx == 16 || ly == 0 || ly == (int)gy0) c = frame;
                            else if (ly < gy0) c = Shade(spandrel, 0.9f + TexRand(cx, cy, 181) * 0.2f);
                            else pane = true;
                            break;
                    }
                    if (pane)
                    {
                        float t = (ly - gy0) / Mathf.Max(1f, gy1 - gy0);
                        Color glass = style == FacadeStyle.Curtain ? new Color(0.16f, 0.24f, 0.27f) : new Color(0.10f, 0.12f, 0.15f);
                        glass = Shade(glass, 0.82f + t * 0.45f + TexRand(cx, cy, 182) * 0.15f);
                        // A lit pane is glass like the rest by day (its light is all in the emission map).
                        if (blind && t > 0.55f) glass = new Color(0.60f, 0.56f, 0.46f);
                        c = glass;
                        if (lit) em[y * S + x] = new Color32((byte)(warm.r * 255f), (byte)(warm.g * 255f), (byte)(warm.b * 255f), 255);
                    }
                    if (em[y * S + x].a == 0) em[y * S + x] = new Color32(0, 0, 0, 255);
                    px[y * S + x] = solid
                        ? new Color32((byte)Mathf.Clamp(c.r * 255f, 0f, 255f), (byte)Mathf.Clamp(c.g * 255f, 0f, 255f), (byte)Mathf.Clamp(c.b * 255f, 0f, 255f), 255)
                        : new Color32(0, 0, 0, 0);
                }
            string name = "grid" + style + (white ? "W" : "D");
            tex = new Texture2D(S, S, TextureFormat.RGBA32, true)
            {
                name = "Bldg_" + name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            tex.SetPixels32(px);
            tex.Apply(true, true);
            glow = new Texture2D(S, S, TextureFormat.RGBA32, true)
            {
                name = "Bldg_" + name + "Glow", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            glow.SetPixels32(em);
            glow.Apply(true, true);
        }
        #endregion
    }
}
