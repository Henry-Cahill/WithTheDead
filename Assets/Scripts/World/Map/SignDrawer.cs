using UnityEngine;
using System;
namespace CloneZ
{
    internal static class SignDrawer
    {
        // Draws a rectangular plank with optional raised lettering (letters are just smaller boxes for now).
        internal static void Draw(RaidMesh m, System.Random rng, string label, Vector3 origin, float width)
        {
            // board background – warm‑brown wood
            var board = DecorPalette.Matte(new Color(0.65f, 0.45f, 0.35f));
            // thin border around the board to give a frame
            var border = DecorPalette.Matte(new Color(0.45f, 0.30f, 0.25f));
            // board (0.02 m thick)
            m.Box(origin + new Vector3(0f, 0.02f, 0f), new Vector3(width, 0.04f, 0.01f), board);
            // border strip (slightly larger)
            m.Box(origin + new Vector3(0f, 0.021f, 0f), new Vector3(width + 0.02f, 0.001f, 0.012f), border);
            // Very rough “letters”: a row of thin vertical boxes spaced evenly.
            int letters = Mathf.Clamp(label.Length, 1, 6);
            float letterWidth = (width - 0.02f) / letters * 0.8f;
            float gap = (width - 0.02f - letters * letterWidth) / (letters - 1);
            float baseX = -width * 0.5f + 0.01f + letterWidth * 0.5f;
            var letter = DecorPalette.Matte(new Color(0.20f, 0.15f, 0.10f));
            for (int i = 0; i < letters; i++)
            {
                float x = baseX + i * (letterWidth + gap);
                m.Box(origin + new Vector3(x, 0.025f, 0.006f), new Vector3(letterWidth, 0.01f, 0.002f), letter);
            }
        }
    }
}
