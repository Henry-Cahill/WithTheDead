using UnityEngine;

namespace CloneZ
{
    /// <summary>A straight wall line in world axes, as the raid town's door and window builders draw on it: a point lies so
    /// far along the line, so high, and so far across it toward its +1 side.</summary>
    internal readonly struct OpeningLine
    {
        public readonly bool AlongX;
        public readonly float Coord;

        public OpeningLine(bool alongX, float coord)
        {
            AlongX = alongX;
            Coord = coord;
        }

        public Vector3 P(float along, float y, float across)
            => AlongX ? new Vector3(along, y, Coord + across) : new Vector3(Coord + across, y, along);

        /// <summary>The line's normal toward its <paramref name="side"/> (+1 / -1).</summary>
        public Vector3 N(int side) => AlongX ? new Vector3(0f, 0f, side) : new Vector3(side, 0f, 0f);

        /// <summary>A box's size: <paramref name="len"/> along the line, <paramref name="h"/> high, <paramref name="thick"/> across.</summary>
        public Vector3 Size(float len, float h, float thick) => AlongX ? new Vector3(len, h, thick) : new Vector3(thick, h, len);

        public Vector3 Along => AlongX ? Vector3.right : Vector3.forward;

        /// <summary>The yaw (degrees) that turns a builder's local +X along the line.</summary>
        public float Yaw => AlongX ? 0f : -90f;
    }

    internal static partial class FurnitureBuilder
    {
        #region Raid windows
        private static readonly Color ShardTint = new Color(0.30f, 0.36f, 0.40f);
        private static readonly Color PlankWood = new Color(0.50f, 0.40f, 0.28f);

        // A sash's rails: the lower sash's bottom rail is the deeper (it takes the lifts), the upper's a little shallower.
        private static float SashTopRail(bool lower) => lower ? 0.05f : 0.045f;
        private static float SashBottomRail(bool lower) => lower ? 0.06f : 0.05f;

        /// <summary>One sash of a double-hung window, <paramref name="w"/> wide between heights <paramref name="y0"/> and
        /// <paramref name="y1"/>, <paramref name="across"/> from the wall line: stiles, rails and a glazing bar down the middle.
        /// Its glass is drawn apart (<see cref="OpeningPane"/>, <see cref="OpeningShards"/>) so it can break. The lower sash
        /// gets a pair of brass lifts on its bottom rail, on the <paramref name="roomSide"/>.</summary>
        public static void OpeningSash(RaidMesh m, in OpeningLine l, float mid, float w, float y0, float y1, float across,
            bool lower, int roomSide, Color paint)
        {
            Surf frame = M(paint);
            const float depth = 0.04f;
            float h = y1 - y0, cy = (y0 + y1) * 0.5f;
            float s0 = mid - w * 0.5f, s1 = mid + w * 0.5f;
            float top = SashTopRail(lower), bottom = SashBottomRail(lower);
            m.Box(l.P(s0 + 0.0225f, cy, across), l.Size(0.045f, h, depth), frame);
            m.Box(l.P(s1 - 0.0225f, cy, across), l.Size(0.045f, h, depth), frame);
            m.Box(l.P(mid, y1 - top * 0.5f, across), l.Size(w - 0.09f, top, lower ? depth + 0.005f : depth), frame);
            m.Box(l.P(mid, y0 + bottom * 0.5f, across), l.Size(w - 0.09f, bottom, lower ? depth : depth + 0.005f), frame);
            float glass = h - top - bottom;
            m.Box(l.P(mid, y0 + bottom + glass * 0.5f, across), l.Size(0.03f, glass, 0.03f), frame);
            // Add tiny brass pull‑knobs on the top rail of each sash (both left and right sides)
            Surf brass = Mt(Brass);
            float knobY = y1 - top * 0.5f - 0.01f; // a few centimetres below the top rail
            float knobDepth = across + roomSide * (depth * 0.5f + 0.008f);
            // Left knob
            m.Box(l.P(s0 + 0.0225f, knobY, knobDepth), new Vector3(0.012f, 0.025f, 0.008f), brass);
            // Right knob
            m.Box(l.P(s1 - 0.0225f, knobY, knobDepth), new Vector3(0.012f, 0.025f, 0.008f), brass);
            if (!lower) return;
            // Existing brass lifts on the lower sash's bottom rail
            for (int e = -1; e <= 1; e += 2)
                m.Box(l.P(mid + e * w * 0.25f, y0 + 0.03f, across + roomSide * (depth * 0.5f + 0.008f)), l.Size(0.05f, 0.015f, 0.016f), brass);
        }

        /// <summary>The glass of a sash drawn by <see cref="OpeningSash"/> (the same measures).</summary>
        public static void OpeningPane(RaidMesh m, in OpeningLine l, float mid, float w, float y0, float y1, float across, bool lower, Color tint)
        {
            float g0 = y0 + SashBottomRail(lower), g1 = y1 - SashTopRail(lower);
            m.Box(l.P(mid, (g0 + g1) * 0.5f, across), l.Size(w - 0.09f, g1 - g0, 0.006f), Gs(tint));
        }

        /// <summary>What a smashed sash keeps of its glass: jagged shards in the corners of the panes either side of its
        /// glazing bar (the same measures as <see cref="OpeningSash"/>).</summary>
        public static void OpeningShards(RaidMesh m, System.Random rng, in OpeningLine l, float mid, float w, float y0, float y1, float across, bool lower)
        {
            float g0 = y0 + SashBottomRail(lower), g1 = y1 - SashTopRail(lower);
            float e0 = mid - (w - 0.09f) * 0.5f, e1 = mid + (w - 0.09f) * 0.5f;
            Surf shard = Gs(ShardTint);
            Vector3 along = l.Along;
            for (int pane = 0; pane < 2; pane++)
            {
                float a0 = pane == 0 ? e0 : mid + 0.015f, a1 = pane == 0 ? mid - 0.015f : e1;
                for (int c = 0; c < 4; c++)
                {
                    if (rng.NextDouble() < 0.2) continue;
                    bool left = (c & 1) == 0, low = c < 2;
                    float k = R(rng, 0.04f, 0.1f);
                    float sa = left ? a0 + k * 0.35f : a1 - k * 0.35f;
                    float sy = low ? g0 + k * 0.6f : g1 - k * 0.6f;
                    Vector3 right = along * k, up = Vector3.up * k * R(rng, 1.3f, 2.1f);
                    // Two-sided: a sliver facing each way.
                    m.Diamond(l.P(sa, sy, across + 0.002f), right, up, shard);
                    m.Diamond(l.P(sa, sy, across - 0.002f), -right, up, shard);
                }
                // Now and then a long dagger left hanging from the top rail.
                if (rng.NextDouble() < 0.4)
                {
                    float k = R(rng, 0.03f, 0.05f), sa = R(rng, a0 + 0.06f, a1 - 0.06f);
                    Vector3 right = along * k, up = Vector3.up * R(rng, 0.1f, 0.18f);
                    m.Diamond(l.P(sa, g1, across + 0.002f), right, up, shard);
                    m.Diamond(l.P(sa, g1, across - 0.002f), -right, up, shard);
                }
            }
        }

        /// <summary>Planks nailed across the outside of a window (its <paramref name="outside"/> face), a little askew,
        /// <paramref name="off"/> out from the wall line.</summary>
        public static void OpeningBoards(RaidMesh m, System.Random rng, in OpeningLine l, float mid, float w, float sill, int outside, float off)
        {
            Surf plank = DecorMaterials.Planks(PlankWood), nail = Mt(DarkSteel);
            Quaternion face = Quaternion.LookRotation(l.N(outside));
            for (int i = 0; i < 3; i++)
            {
                float y = sill + 0.25f + i * 0.38f;
                Quaternion q = face * Quaternion.Euler(0f, 0f, R(rng, -7f, 7f));
                Vector3 c = l.P(mid, y, outside * off);
                m.Box(c, new Vector3(w + 0.3f, 0.16f, 0.03f), q, plank);
                // Dark‑steel brackets near lower corners of the board for a weathered look
                for (int e = -1; e <= 1; e += 2)
                    m.Box(c + q * new Vector3(e * (w * 0.5f - 0.02f), -0.07f, 0f), new Vector3(0.012f, 0.012f, 0.006f), q, nail);
                // Existing nails (small fasteners)
                for (int e = -1; e <= 1; e += 2)
                    m.Box(c + q * new Vector3(e * (w * 0.5f + 0.06f), 0f, 0.016f), new Vector3(0.014f, 0.014f, 0.006f), q, nail);
            }
        }

        /// <summary>A window's curtains on its <paramref name="side"/> (the room's): a brass rod with finials over the casing,
        /// drawn into both <paramref name="open"/> (the panels gathered back to either side with tie-backs) and
        /// <paramref name="drawn"/> (the panels drawn across in shallow folds). <paramref name="lo"/>..<paramref name="hi"/>
        /// bound the rod along the wall (the room's corners), <paramref name="face"/> is the wall face's distance from its line.</summary>
        public static void OpeningCurtains(RaidMesh open, RaidMesh drawn, in OpeningLine l, float mid, float w, float sill, float head,
            float face, float lo, float hi, int side, Color fabric)
        {
            float rodY = head + 0.13f, rodOff = side * (face + 0.075f);
            float r0 = Mathf.Max(mid - w * 0.5f - 0.25f, lo + 0.06f), r1 = Mathf.Min(mid + w * 0.5f + 0.25f, hi - 0.06f);
            Surf brass = Mt(Brass);
            for (int pass = 0; pass < 2; pass++)
            {
                RaidMesh m = pass == 0 ? open : drawn;
                m.Box(l.P((r0 + r1) * 0.5f, rodY, rodOff), l.Size(r1 - r0, 0.024f, 0.024f), brass);
                m.Ellipsoid(l.P(r0, rodY, rodOff), new Vector3(0.045f, 0.045f, 0.045f), brass, 6, 4);
                m.Ellipsoid(l.P(r1, rodY, rodOff), new Vector3(0.045f, 0.045f, 0.045f), brass, 6, 4);
            }

            float top = rodY - 0.015f, bottom = sill + 0.06f, h = top - bottom;
            for (int e = -1; e <= 1; e += 2)
            {
                float c = Mathf.Clamp(mid + e * (w * 0.5f + 0.08f), r0 + 0.14f, r1 - 0.14f);
                for (int k = 0; k < 3; k++)
                {
                    float depth = side * (face + 0.055f + (k % 2) * 0.022f);
                    open.Box(l.P(c + (k - 1) * 0.085f, bottom + h * 0.5f, depth), l.Size(0.1f, h, 0.028f), M(fabric * (k % 2 == 0 ? 1f : 0.84f)));
                }
                open.Box(l.P(c, bottom + h * 0.4f, side * (face + 0.09f)), l.Size(0.3f, 0.04f, 0.012f), M(fabric * 0.7f));
            }

            float span = r1 - r0 - 0.04f;
            int folds = Mathf.Max(6, Mathf.RoundToInt(span / 0.11f));
            float fw = span / folds;
            for (int k = 0; k < folds; k++)
            {
                float along = r0 + 0.02f + (k + 0.5f) * fw;
                float depth = side * (face + 0.058f + (k % 2) * 0.018f);
                drawn.Box(l.P(along, bottom + h * 0.5f, depth), l.Size(fw + 0.012f, h, 0.026f), M(fabric * (k % 2 == 0 ? 1f : 0.86f)));
            }
        }

        /// <summary>A roller blind hung inside the window's reveal on its <paramref name="side"/>, <paramref name="across"/> from
        /// the wall line and <paramref name="w"/> wide: rolled up under the head (<paramref name="up"/>) or pulled down to the
        /// sill (<paramref name="down"/>). A <paramref name="cloth"/> is a blackout cloth instead — a heavier roll tied up with
        /// cord, or let down in slack folds.</summary>
        public static void OpeningBlind(RaidMesh up, RaidMesh down, in OpeningLine l, float mid, float w, float sill, float head,
            float across, int side, Color fabric, bool cloth)
        {
            float a = side * across, rollY = head - 0.05f, roll = cloth ? 0.028f : 0.022f;
            Surf stuff = M(fabric), bar = M(cloth ? fabric * 0.7f : Pine), cord = M(cloth ? new Color(0.55f, 0.50f, 0.38f) : fabric * 0.6f);
            for (int pass = 0; pass < 2; pass++)
                (pass == 0 ? up : down).CylinderX(l.P(mid, rollY, a), roll, w, 10, stuff, l.Yaw);

            if (cloth)
            {
                // Rolled up: two cords tied round the roll.
                for (int e = -1; e <= 1; e += 2)
                    up.Box(l.P(mid + e * w * 0.3f, rollY, a), l.Size(0.012f, roll * 2f + 0.012f, roll * 2f + 0.012f), cord);
                // Let down: slack cloth in three hanging folds, tacked at the top corners.
                float h = rollY - (sill + 0.05f), fw = w / 3f;
                for (int k = 0; k < 3; k++)
                    down.Box(l.P(mid - w * 0.5f + (k + 0.5f) * fw, rollY - h * 0.5f, a + side * (k % 2) * 0.008f),
                        l.Size(fw + 0.01f, h, 0.008f), M(fabric * (k % 2 == 0 ? 1f : 0.82f)));
                for (int e = -1; e <= 1; e += 2)
                    down.Box(l.P(mid + e * (w * 0.5f - 0.03f), rollY - 0.02f, a + side * 0.008f), l.Size(0.016f, 0.016f, 0.01f), Mt(DarkSteel));
                return;
            }

            // Rolled up: the bottom bar just under the roll, its pull cord and knob hanging below.
            float upBar = rollY - roll - 0.016f;
            up.Box(l.P(mid, upBar, a), l.Size(w, 0.018f, 0.014f), bar);
            up.Box(l.P(mid, upBar - 0.07f, a), l.Size(0.005f, 0.13f, 0.005f), cord);
            up.Ellipsoid(l.P(mid, upBar - 0.145f, a), new Vector3(0.02f, 0.028f, 0.02f), bar, 6, 4);

            // Pulled down: the cloth over the glazing, the bar at its foot, the cord and knob just clear of the sill.
            float downBar = sill + 0.13f;
            down.Box(l.P(mid, (rollY + downBar) * 0.5f, a), l.Size(w, rollY - downBar, 0.005f), stuff);
            down.Box(l.P(mid, downBar, a), l.Size(w, 0.018f, 0.014f), bar);
            down.Box(l.P(mid, downBar - 0.045f, a), l.Size(0.005f, 0.07f, 0.005f), cord);
            down.Ellipsoid(l.P(mid, downBar - 0.085f, a), new Vector3(0.02f, 0.028f, 0.02f), bar, 6, 4);
        }

        /// <summary>Louvred shutters either side of a window, on its <paramref name="side"/> (outside) face.</summary>
        public static void OpeningShutters(RaidMesh m, in OpeningLine l, float mid, float w, float sill, float head, float face, int side, Color paint)
        {
            const float sw = 0.26f;
            float sh = head - sill + 0.1f, cy = (sill + head) * 0.5f;
            Surf panel = M(paint), slat = M(paint * 0.78f);
            for (int e = -1; e <= 1; e += 2)
            {
                float c = mid + e * (w * 0.5f + 0.07f + sw * 0.5f);
                m.Box(l.P(c, cy, side * (face + 0.02f)), l.Size(sw, sh, 0.03f), panel);
                // Brass hinge near top‑outer edge of the panel
                m.Box(l.P(c, cy + sh * 0.5f - 0.02f, side * (face + 0.04f)), l.Size(0.018f, 0.04f, 0.008f), Mt(Brass));
                // Brass latch near lower‑outer edge of the panel
                m.Box(l.P(c, sill + 0.12f, side * (face + 0.045f)), l.Size(0.032f, 0.064f, 0.008f), Mt(Brass));
                for (float y = sill + 0.04f; y < head + 0.03f; y += 0.075f)
                    m.Box(l.P(c, y, side * (face + 0.038f)), l.Size(sw - 0.06f, 0.024f, 0.012f), slat);
            }
        }

        /// <summary>A painted planter under a window's sill, outside: last summer's flowers dead brown sticks, the soil under a
        /// cap of snow.</summary>
        public static void OpeningFlowerBox(RaidMesh m, System.Random rng, in OpeningLine l, float mid, float w, float sill, float face, int side, Color paint)
        {
            float across = side * (face + 0.12f);
            m.Box(l.P(mid, sill - 0.15f, across), l.Size(w + 0.1f, 0.2f, 0.2f), M(paint));
            m.Box(l.P(mid, sill - 0.055f, across), l.Size(w + 0.04f, 0.012f, 0.16f), M(new Color(0.20f, 0.15f, 0.10f)));
            Surf stalk = M(new Color(0.34f, 0.27f, 0.18f)), dry = M(new Color(0.44f, 0.36f, 0.24f));
            int n = Mathf.Max(5, Mathf.RoundToInt(w * 8f));
            for (int i = 0; i < n; i++)
            {
                float along = mid - w * 0.5f + (i + 0.5f) * w / n + R(rng, -0.03f, 0.03f);
                Vector3 root = l.P(along, sill - 0.05f, across + side * R(rng, -0.05f, 0.05f));
                Vector3 tip = root + Vector3.up * R(rng, 0.12f, 0.3f) + l.Along * R(rng, -0.06f, 0.06f) + l.N(side) * R(rng, -0.04f, 0.06f);
                m.Rod(root, tip, 0.012f, Chance(rng, 0.5) ? stalk : dry);
                if (Chance(rng, 0.35)) m.Ellipsoid(tip, new Vector3(0.035f, 0.025f, 0.035f), dry, 5, 3);
            }
            // The snow lying on the soil and along the box's rim.
            Surf snow = M(DecorProps.SnowWhite);
            for (int i = 0; i < 4; i++)
            {
                float along = mid - w * 0.5f + (i + 0.5f) * w / 4f;
                m.Ellipsoid(l.P(along, sill - 0.045f, across), l.Size(w / 4f + 0.08f, 0.07f, 0.17f), snow, 6, 3);
            }
        }
        #endregion

        #region Raid doors
        /// <summary>Splintered wood on the floor round a doorway a door has been battered out of — most of it on the
        /// <paramref name="side"/> it burst toward.</summary>
        public static void DoorSplinters(RaidMesh m, System.Random rng, in OpeningLine l, float mid, float width, int side, Color wood)
        {
            Color raw = Color.Lerp(wood, new Color(0.80f, 0.68f, 0.50f), 0.55f);   // fresh-broken wood is pale inside
            for (int i = 0; i < 14; i++)
            {
                int s = Chance(rng, 0.75) ? side : -side;
                float along = mid + R(rng, -0.55f, 0.55f) * width;
                float across = s * R(rng, 0.18f, 1.1f);
                float len = R(rng, 0.06f, 0.34f), thick = R(rng, 0.012f, 0.04f);
                Quaternion q = Quaternion.Euler(R(rng, -5f, 5f), R(rng, 0f, 180f), R(rng, -5f, 5f));
                m.Box(l.P(along, 0.032f + thick * 0.4f, across), new Vector3(len, thick * 0.8f, thick), q, M(V(Chance(rng, 0.6) ? raw : wood, rng, 0.12f)));
            }
            // A couple of bigger chunks of the frame and the lock's strike plate.
            for (int i = 0; i < 2; i++)
            {
                Quaternion q = Quaternion.Euler(0f, R(rng, 0f, 180f), 0f);
                m.Box(l.P(mid + R(rng, -0.4f, 0.4f) * width, 0.05f, side * R(rng, 0.3f, 0.9f)), new Vector3(R(rng, 0.25f, 0.5f), 0.03f, 0.06f), q, M(V(wood, rng, 0.1f)));
            }
            m.Box(l.P(mid + R(rng, -0.3f, 0.3f), 0.034f, side * R(rng, 0.3f, 0.7f)), new Vector3(0.12f, 0.004f, 0.03f), Quaternion.Euler(0f, R(rng, 0f, 180f), 0f), Mt(Brass));
        }

        /// <summary>One band of a roll-up door's steel curtain, <paramref name="len"/> wide between heights <paramref name="y0"/>
        /// and <paramref name="y1"/>, <paramref name="across"/> from the wall line: a corrugated sheet ribbed on both faces; the
        /// bottom band carries the heavy bottom bar and a lift handle on each face.</summary>
        public static void ShutterBand(RaidMesh m, in OpeningLine l, float mid, float len, float y0, float y1, float across, bool bottom)
        {
            Surf sheet = Mt(new Color(0.55f, 0.57f, 0.55f)), rib = Mt(new Color(0.44f, 0.46f, 0.45f));
            float h = y1 - y0;
            m.Box(l.P(mid, (y0 + y1) * 0.5f, across), l.Size(len, h, 0.012f), sheet);
            int ribs = Mathf.Max(1, Mathf.RoundToInt(h / 0.075f));
            for (int i = 0; i < ribs; i++)
            {
                float y = y0 + (i + 0.5f) * h / ribs;
                m.Box(l.P(mid, y, across + 0.008f), l.Size(len, 0.014f, 0.005f), rib);
                m.Box(l.P(mid, y, across - 0.008f), l.Size(len, 0.014f, 0.005f), rib);
            }
            if (!bottom) return;
            Surf bar = Mt(new Color(0.30f, 0.31f, 0.31f));
            m.Box(l.P(mid, y0 + 0.025f, across), l.Size(len, 0.05f, 0.035f), bar);
            for (int s = -1; s <= 1; s += 2)
                m.Box(l.P(mid, y0 + 0.12f, across + s * 0.022f), l.Size(0.16f, 0.025f, 0.02f), bar);
        }
        #endregion
    }
}
