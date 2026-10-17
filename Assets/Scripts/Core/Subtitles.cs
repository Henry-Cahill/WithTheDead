using System;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The caption line for things the survivor hears: a radio or TV broadcast, a voice on the street. Sources
    /// push lines here; the HUD (<see cref="HudSubtitles"/>) shows the latest few at the bottom of the screen
    /// with the source's name in its colour, each fading after its time is up.
    /// </summary>
    public static class Subtitles
    {
        /// <summary>One caption.</summary>
        public struct Line
        {
            public string Source;   // "Radio — 91.3 FM", "TV"
            public string Text;
            public Color Color;
            public float Seconds;
        }

        /// <summary>Raised for every new caption (the HUD listens).</summary>
        public static event Action<Line> Shown;

        /// <summary>Shows <paramref name="text"/> from <paramref name="source"/> for <paramref name="seconds"/>.</summary>
        public static void Show(string source, string text, Color color, float seconds = 6f)
        {
            if (string.IsNullOrEmpty(text)) return;
            Shown?.Invoke(new Line { Source = source, Text = text, Color = color, Seconds = Mathf.Max(1f, seconds) });
        }
    }
}
