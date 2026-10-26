using UnityEngine;
using System;
namespace CloneZ
{
    internal static class SignDrawer
    {
        // Draws a rectangular plank with optional raised lettering (letters are just smaller boxes for now).
    // Adds subtle color variation for weathered wood.
    static Color VaryWood(Color baseCol, System.Random rng)
    {
        // DecorPalette.Vary varies hue/saturation; we keep it thin for wood grain.
        return DecorPalette.Vary(baseCol, 0.07f, rng);
    }

    // Draw with optional weathering flag.
    internal static void Draw(RaidMesh m, System.Random rng, string label, Vector3 origin, float width, bool weathered)
    {
        // board background – warm‑brown wood (now with optional variation)
        var baseBoard = new Color(0.65f, 0.45f, 0.35f);
        var boardCol = weathered ? VaryWood(baseBoard, rng) : baseBoard;
        var board = DecorPalette.Matte(boardCol);
        // thin border around the board to give a frame
        var baseBorder = new Color(0.45f, 0.30f, 0.25f);
        var borderCol = weathered ? VaryWood(baseBorder, rng) : baseBorder;
        var border = DecorPalette.Matte(borderCol);
        // board (0.02 m thick)
        m.Box(origin + new Vector3(0f, 0.02f, 0f), new Vector3(width, 0.04f, 0.01f), board);
        // border strip (slightly larger)
        m.Box(origin + new Vector3(0f, 0.021f, 0f), new Vector3(width + 0.02f, 0.001f, 0.012f), border);
        // add a tiny dent – a shallow recessed box at a random X offset
        if (weathered)
        {
            float dentX = (float)(rng.NextDouble() * (width - 0.04) - (width/2 - 0.02));
            // dent is 0.002 m deep, 0.005 m tall, centered 0.006 m forward
            var dentSurf = DecorPalette.Matte(new Color(0.50f, 0.35f, 0.28f));
            m.Box(origin + new Vector3(dentX, 0.022f, 0.006f), new Vector3(0.01f, 0.005f, 0.003f), dentSurf);
        }
        // Very rough “letters”: a row of thin vertical boxes spaced evenly.
        int letters = Mathf.Clamp(label.Length, 1, 6);
        float letterWidth = (width - 0.02f) / letters * 0.8f;
        float gap = letters > 1 ? (width - 0.02f - letters * letterWidth) / (letters - 1) : 0f;
        float baseX = -width * 0.5f + 0.01f + letterWidth * 0.5f;
        var letter = DecorPalette.Matte(new Color(0.20f, 0.15f, 0.10f));
        for (int i = 0; i < letters; i++)
        {
            float x = baseX + i * (letterWidth + gap);
            m.Box(origin + new Vector3(x, 0.025f, 0.006f), new Vector3(letterWidth, 0.01f, 0.002f), letter);
        }
    }

    // Existing non‑weathered overload – forwards to the new method.
    internal static void Draw(RaidMesh m, System.Random rng, string label, Vector3 origin, float width)
    {
        Draw(m, rng, label, origin, width, false);
    }
    }
}
