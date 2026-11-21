using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A tiny 5×7 bitmap font for painting text into procedural textures (the "U.S. ARMY" poster,
    /// shop signs, hazard placards). Only what the raid map needs: A–Z, 0–9 and a little punctuation.
    /// </summary>
    internal static class DecorFont
    {
        public const int GlyphW = 5;
        public const int GlyphH = 7;

        private static Dictionary<char, string[]> s_Glyphs;

        private static Dictionary<char, string[]> Glyphs => s_Glyphs ??= Build();

        private static Dictionary<char, string[]> Build()
        {
            var g = new Dictionary<char, string[]>(64);
            void Add(char c, params string[] rows) => g[c] = rows;

            Add('A', ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
            Add('B', "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####.");
            Add('C', ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###.");
            Add('D', "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####.");
            Add('E', "#####", "#....", "#....", "####.", "#....", "#....", "#####");
            Add('F', "#####", "#....", "#....", "####.", "#....", "#....", "#....");
            Add('G', ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####");
            Add('H', "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
            Add('I', ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###.");
            Add('J', "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##..");
            Add('K', "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#");
            Add('L', "#....", "#....", "#....", "#....", "#....", "#....", "#####");
            Add('M', "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#");
            Add('N', "#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#");
            Add('O', ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
            Add('P', "####.", "#...#", "#...#", "####.", "#....", "#....", "#....");
            Add('Q', ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#");
            Add('R', "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#");
            Add('S', ".####", "#....", "#....", ".###.", "....#", "....#", "####.");
            Add('T', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..");
            Add('U', "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
            Add('V', "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..");
            Add('W', "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#.");
            Add('X', "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#");
            Add('Y', "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..");
            Add('Z', "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####");
            Add('0', ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###.");
            Add('1', "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###.");
            Add('2', ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####");
            Add('3', "####.", "....#", "....#", ".###.", "....#", "....#", "####.");
            Add('4', "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#.");
            Add('5', "#####", "#....", "####.", "....#", "....#", "#...#", ".###.");
            Add('6', ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###.");
            Add('7', "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#...");
            Add('8', ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###.");
            Add('9', ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###.");
            Add('.', ".....", ".....", ".....", ".....", ".....", ".##..", ".##..");
            Add('!', "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#..");
            Add('-', ".....", ".....", ".....", "#####", ".....", ".....", ".....");
            Add('\'', "..#..", "..#..", ".#...", ".....", ".....", ".....", ".....");
            Add(':', ".....", ".##..", ".##..", ".....", ".##..", ".##..", ".....");
            Add('/', "....#", "....#", "...#.", "..#..", ".#...", "#....", "#....");
            Add('&', ".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#");
            Add('+', ".....", "..#..", "..#..", "#####", "..#..", "..#..", ".....");
            Add(' ', ".....", ".....", ".....", ".....", ".....", ".....", ".....");
            return g;
        }

        /// <summary>Pixel width of <paramref name="text"/> at the given scale (1 px gap between glyphs).</summary>
        public static int Width(string text, int scale) => text.Length == 0 ? 0 : (text.Length * (GlyphW + 1) - 1) * scale;

        /// <summary>Paints <paramref name="text"/> into <paramref name="px"/> (row 0 = bottom) with its
        /// top-left corner at (<paramref name="x"/>, <paramref name="top"/>).</summary>
        public static void Draw(Color32[] px, int texW, int texH, string text, int x, int top, int scale, Color32 color)
        {
            Dictionary<char, string[]> glyphs = Glyphs;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = char.ToUpperInvariant(text[i]);
                if (!glyphs.TryGetValue(ch, out string[] rows)) rows = glyphs[' '];
                int gx = x + i * (GlyphW + 1) * scale;
                for (int r = 0; r < GlyphH; r++)
                {
                    string row = rows[r];
                    for (int c = 0; c < GlyphW; c++)
                    {
                        if (row[c] != '#') continue;
                        for (int sy = 0; sy < scale; sy++)
                        {
                            int py = top - r * scale - sy;
                            if (py < 0 || py >= texH) continue;
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int pxX = gx + c * scale + sx;
                                if (pxX < 0 || pxX >= texW) continue;
                                px[py * texW + pxX] = color;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Paints text horizontally centred on <paramref name="cx"/>.</summary>
        public static void DrawCentered(Color32[] px, int texW, int texH, string text, int cx, int top, int scale, Color32 color)
            => Draw(px, texW, texH, text, cx - Width(text, scale) / 2, top, scale, color);
    }
}
