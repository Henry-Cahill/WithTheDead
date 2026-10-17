using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// One digit of the digital watch: seven hexagonal segments built from sliced images, with the
    /// unlit segments left faintly visible like a real LCD. Only recolours when the digit changes.
    /// </summary>
    public sealed class HudSevenSegment
    {
        // Segment masks for 0-9, bits a..g = 1, 2, 4, 8, 16, 32, 64 (a top, then clockwise, g middle).
        private static readonly byte[] Glyphs = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };

        private readonly Image[] _segments = new Image[7];
        private readonly Color _lit, _ghost;
        private int _digit = int.MinValue;

        /// <summary>Builds a digit whose top-left corner sits at <paramref name="topLeft"/> in the parent.</summary>
        public HudSevenSegment(Transform parent, Vector2 topLeft, float width, float height, float thickness, Color lit, Color ghost)
        {
            _lit = lit;
            _ghost = ghost;
            RectTransform box = HudUI.Rect(parent, "Digit", new Vector2(0f, 1f), Vector2.zero,
                new Vector2(topLeft.x, topLeft.y - height), new Vector2(width, height));

            float t = thickness, w = width, h = height, gap = Mathf.Max(0.6f, t * 0.18f);
            float horiz = w - t - 2f * gap;
            float vert = (h - t) * 0.5f - 2f * gap;
            _segments[0] = Segment(box, w * 0.5f, h - t * 0.5f, horiz, t, false);                      // a
            _segments[1] = Segment(box, w - t * 0.5f, h * 0.75f - t * 0.25f, vert, t, true);            // b
            _segments[2] = Segment(box, w - t * 0.5f, h * 0.25f + t * 0.25f, vert, t, true);            // c
            _segments[3] = Segment(box, w * 0.5f, t * 0.5f, horiz, t, false);                           // d
            _segments[4] = Segment(box, t * 0.5f, h * 0.25f + t * 0.25f, vert, t, true);                // e
            _segments[5] = Segment(box, t * 0.5f, h * 0.75f - t * 0.25f, vert, t, true);                // f
            _segments[6] = Segment(box, w * 0.5f, h * 0.5f, horiz, t, false);                           // g
            Set(-1);
        }

        /// <summary>Shows a digit 0-9 (anything else blanks it).</summary>
        public void Set(int digit)
        {
            if (digit == _digit) return;
            _digit = digit;
            int mask = digit >= 0 && digit <= 9 ? Glyphs[digit] : 0;
            for (int i = 0; i < 7; i++)
                _segments[i].color = (mask & (1 << i)) != 0 ? _lit : _ghost;
        }

        private static Image Segment(RectTransform box, float cx, float cy, float length, float thickness, bool vertical)
        {
            // The segment sprite is 16 px tall with 8 px pointed tips; scale the slice so a tip is half the thickness.
            Image img = HudUI.Sliced(box, "Seg", HudIcon.Segment, Color.white, Vector2.zero, new Vector2(0.5f, 0.5f),
                new Vector2(cx, cy), new Vector2(length, thickness), 16f / thickness);
            if (vertical) img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            return img;
        }
    }
}
