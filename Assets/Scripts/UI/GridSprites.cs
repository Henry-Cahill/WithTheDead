using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Procedural sprites for the grid inventory, built once and cached: the tiling cell, a small gear,
    /// a hand badge and a resize grip. The glyphs are white on transparent and take their Image colour.
    /// </summary>
    public static class GridSprites
    {
        /// <summary>Side of one inventory cell in reference pixels — and in texels, so a grid is
        /// pixel-exact at 1920×1080.</summary>
        public const int CellPx = 30;

        /// <summary>The empty cell's fill (#15181b).</summary>
        public static readonly Color CellFill = new Color32(0x15, 0x18, 0x1b, 0xff);
        /// <summary>The 1 px lines between cells (#3a3f45).</summary>
        public static readonly Color CellLine = new Color32(0x3a, 0x3f, 0x45, 0xff);

        private static Sprite s_Cell, s_Gear, s_Hand, s_Grip;

        // The hand badge marking the weapon in hand, drawn top row first ('#' = ink).
        private static readonly string[] s_HandBits =
        {
            "....#.......",
            "..#.#.#.....",
            "..#.#.#.#...",
            "..#.#.#.#...",
            "..#######...",
            "#.#######...",
            "#########...",
            ".########...",
            "..#######...",
            "...#####....",
            "...#####....",
            "............",
        };

        /// <summary>
        /// One cell to tile across a grid Image (<see cref="UnityEngine.UI.Image.Type.Tiled"/>): the fill
        /// with a line along its left and bottom edges. Tiling starts at the Image's bottom-left, so an
        /// Image one pixel larger than cols×rows cells draws every line exactly once, closing edges
        /// included — the top line of the grid and the right one land in that extra pixel.
        /// </summary>
        public static Sprite Cell()
        {
            if (s_Cell != null) return s_Cell;
            var px = new Color32[CellPx * CellPx];
            Color32 fill = CellFill, line = CellLine;
            for (int y = 0; y < CellPx; y++)
                for (int x = 0; x < CellPx; x++)
                    px[y * CellPx + x] = x == 0 || y == 0 ? line : fill;
            s_Cell = Make(px, CellPx, CellPx, TextureWrapMode.Repeat);
            return s_Cell;
        }

        /// <summary>A small cog for the "Transfer / sort options" button (drawn rather than relying on
        /// the font having U+2699).</summary>
        public static Sprite Gear()
        {
            if (s_Gear != null) return s_Gear;
            const int n = 24;
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - c, dy = y - c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float tooth = Mathf.Clamp01((Mathf.Cos(Mathf.Atan2(dy, dx) * 8f) - 0.2f) * 3f);   // 0 between teeth, 1 on one
                    float outer = 8.2f + 2.8f * tooth;
                    float a = Mathf.Clamp01(outer - r + 0.5f) * Mathf.Clamp01(r - 3.1f);                // ring with a hole
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            s_Gear = Make(px, n, n, TextureWrapMode.Clamp);
            return s_Gear;
        }

        /// <summary>A 12×12 open hand: the badge on the weapon in hand.</summary>
        public static Sprite Hand()
        {
            if (s_Hand != null) return s_Hand;
            int h = s_HandBits.Length, w = s_HandBits[0].Length;
            var px = new Color32[w * h];
            for (int row = 0; row < h; row++)
                for (int x = 0; x < w; x++)
                    px[(h - 1 - row) * w + x] = s_HandBits[row][x] == '#' ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            s_Hand = Make(px, w, h, TextureWrapMode.Clamp);
            return s_Hand;
        }

        /// <summary>Three short diagonal strokes in the bottom-right corner: a window's resize grip.</summary>
        public static Sprite Grip()
        {
            if (s_Grip != null) return s_Grip;
            const int n = 12;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int d = x - y;   // y counts up from the bottom, so x - y is constant along a "/" stroke
                    bool ink = d == 9 || d == 5 || d == 1;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(ink ? 255 : 0));
                }
            s_Grip = Make(px, n, n, TextureWrapMode.Clamp);
            return s_Grip;
        }

        private static Sprite Make(Color32[] px, int w, int h, TextureWrapMode wrap)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
