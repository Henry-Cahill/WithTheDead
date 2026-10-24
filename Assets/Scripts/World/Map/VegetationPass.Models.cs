using UnityEngine;

namespace CloneZ
{
    /// <summary>The trees the map grows: Kentucky's winter woods and yards.</summary>
    internal enum TreeKind : byte
    {
        /// <summary>White or red oak: a wide, spreading crown of crooked limbs, dark bark.</summary>
        Oak,
        /// <summary>Sugar or red maple: grey bark, a rounded crown round a central leader.</summary>
        Maple,
        /// <summary>Tulip poplar: tall and straight, a narrow crown high up.</summary>
        Tulip,
        /// <summary>Hackberry or elm: limbs fanning up from low on the trunk, a vase.</summary>
        Hackberry,
        /// <summary>Bradford pear: the small, upright, twiggy yard tree every 1980s subdivision planted.</summary>
        Pear,
        /// <summary>Dogwood or redbud: small, low and spreading.</summary>
        Dogwood,
        /// <summary>Sycamore: huge and leaning, its upper limbs bone white; along creeks.</summary>
        Sycamore,
        /// <summary>Eastern red cedar: narrow, dark olive-bronze; fence rows, old fields, cemeteries.</summary>
        Cedar,
        /// <summary>White pine: soft blue-green layers with the sky between them.</summary>
        WhitePine,
        /// <summary>Blue spruce: the front yard's dense silver-blue cone.</summary>
        Spruce,
        /// <summary>Virginia pine: scrubby, yellow-green, on the eastern ridges and worn-out fields.</summary>
        ScrubPine,
    }

    /// <summary>How much of a tree to draw: Full near the survivor, Wood for the many trees of a forest, Lite for a fixed
    /// map's scenery past its edge, Blob for a far streamed piece (a trunk under one mass of crown).</summary>
    internal enum TreeTier : byte { Full, Wood, Lite, Blob }

    /// <summary>The bushes: evergreen foundation plantings and bare thickets.</summary>
    internal enum ShrubKind : byte { Yew, Boxwood, Juniper, Thicket }

    /// <summary>
    /// What a tree's crown must keep clear of: up to three obstacles (the nearest walls, the nearest overhead wire), each
    /// as the plan direction to it in the tree's own frame (degrees clockwise from the frame's +z) and its distance from
    /// the trunk. A hardwood's limbs reaching towards one are cut back short of it, the way a yard tree is pruned off a
    /// house and the power company side-trims the trees under its lines.
    /// </summary>
    internal struct TreeRoom
    {
        public int Count;
        public float Az0, D0, Az1, D1, Az2, D2;

        public void Add(float azimuth, float distance)
        {
            if (Count == 0)
            {
                Az0 = azimuth;
                D0 = distance;
            }
            else if (Count == 1)
            {
                Az1 = azimuth;
                D1 = distance;
            }
            else if (Count == 2)
            {
                Az2 = azimuth;
                D2 = distance;
            }
            else return;
            Count++;
        }

        /// <summary>How far out from the trunk a limb heading along <paramref name="azimuth"/> may reach.</summary>
        public float Reach(float azimuth)
        {
            float best = float.MaxValue;
            if (Count > 0) best = Limit(azimuth, Az0, D0, best);
            if (Count > 1) best = Limit(azimuth, Az1, D1, best);
            if (Count > 2) best = Limit(azimuth, Az2, D2, best);
            return best;
        }

        // The obstacle as a plane square to its direction: d away straight towards it, farther off to the side.
        private static float Limit(float azimuth, float towards, float d, float best)
        {
            float delta = Mathf.Abs(Mathf.DeltaAngle(azimuth, towards));
            return delta >= 80f ? best : Mathf.Min(best, d / Mathf.Cos(delta * Mathf.Deg2Rad));
        }
    }

    /// <summary>
    /// The nature passes' winter trees, bushes and deadwood, drawn into a <see cref="RaidMesh"/> round the local frame's
    /// origin (the trunk's foot on the ground). Hardwoods are bare: a tapered trunk with a flared foot, scaffold limbs in a
    /// spiral, branches and twigs, snow lying along the lower limbs; conifers are stacked cones with a ring of snow on
    /// each. Every shape comes from the caller's random stream, so a tree is the same wherever it's built. Flat palette
    /// colours only (one material), no caps where nobody looks.
    /// </summary>
    internal static class TreeKit
    {
        private static Surf M(Color c) => DecorPalette.Matte(c);
        private static float R(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);
        private static Color V(Color c, System.Random rng, float amount) => DecorPalette.Vary(c, amount, rng);

        private static readonly Color Snow = DecorProps.SnowWhite;
        private static readonly Color PaleBark = new Color(0.86f, 0.85f, 0.79f);

        /// <summary>The shape of a kind of hardwood.</summary>
        private struct Form
        {
            public Color Bark;
            /// <summary>Trunk radius per metre of height.</summary>
            public float Girth;
            /// <summary>Where the crown starts, as a share of the height.</summary>
            public float BaseMin, BaseMax;
            /// <summary>The crown's radius per metre of height.</summary>
            public float Spread;
            /// <summary>The scaffold limbs' rise, degrees above level.</summary>
            public float ElevMin, ElevMax;
            public int Limbs;
            /// <summary>A trunk running on up through the crown.</summary>
            public bool Leader;
            /// <summary>Most degrees the trunk leans.</summary>
            public float Lean;
            /// <summary>White upper bark (sycamore).</summary>
            public bool Pale;
        }

        private static Form FormOf(TreeKind kind)
        {
            switch (kind)
            {
                case TreeKind.Maple:
                    return new Form { Bark = new Color(0.36f, 0.34f, 0.31f), Girth = 0.022f, BaseMin = 0.22f, BaseMax = 0.32f, Spread = 0.36f,
                        ElevMin = 38f, ElevMax = 58f, Limbs = 6, Leader = true, Lean = 3f };
                case TreeKind.Tulip:
                    return new Form { Bark = new Color(0.40f, 0.38f, 0.33f), Girth = 0.019f, BaseMin = 0.42f, BaseMax = 0.55f, Spread = 0.22f,
                        ElevMin = 42f, ElevMax = 64f, Limbs = 6, Leader = true, Lean = 2f };
                case TreeKind.Hackberry:
                    return new Form { Bark = new Color(0.37f, 0.35f, 0.32f), Girth = 0.024f, BaseMin = 0.24f, BaseMax = 0.32f, Spread = 0.40f,
                        ElevMin = 50f, ElevMax = 70f, Limbs = 5, Leader = false, Lean = 4f };
                case TreeKind.Pear:
                    return new Form { Bark = new Color(0.31f, 0.29f, 0.28f), Girth = 0.026f, BaseMin = 0.2f, BaseMax = 0.26f, Spread = 0.30f,
                        ElevMin = 55f, ElevMax = 74f, Limbs = 7, Leader = true, Lean = 2f };
                case TreeKind.Dogwood:
                    return new Form { Bark = new Color(0.27f, 0.23f, 0.20f), Girth = 0.03f, BaseMin = 0.18f, BaseMax = 0.26f, Spread = 0.55f,
                        ElevMin = 14f, ElevMax = 38f, Limbs = 5, Leader = false, Lean = 7f };
                case TreeKind.Sycamore:
                    return new Form { Bark = new Color(0.47f, 0.44f, 0.38f), Girth = 0.026f, BaseMin = 0.26f, BaseMax = 0.36f, Spread = 0.46f,
                        ElevMin = 16f, ElevMax = 50f, Limbs = 5, Leader = false, Lean = 9f, Pale = true };
                default:
                    return new Form { Bark = new Color(0.25f, 0.22f, 0.19f), Girth = 0.026f, BaseMin = 0.26f, BaseMax = 0.36f, Spread = 0.48f,
                        ElevMin = 18f, ElevMax = 46f, Limbs = 5, Leader = false, Lean = 4f };
            }
        }

        /// <summary>True for the evergreens.</summary>
        public static bool Conifer(TreeKind kind) => kind >= TreeKind.Cedar;

        /// <summary>A height for a tree of the kind, from <paramref name="rng"/>.</summary>
        public static float Height(TreeKind kind, System.Random rng)
        {
            switch (kind)
            {
                case TreeKind.Oak: return R(rng, 11f, 18f);
                case TreeKind.Maple: return R(rng, 10f, 16f);
                case TreeKind.Tulip: return R(rng, 15f, 23f);
                case TreeKind.Hackberry: return R(rng, 9f, 14f);
                case TreeKind.Pear: return R(rng, 6f, 9f);
                case TreeKind.Dogwood: return R(rng, 3.5f, 6f);
                case TreeKind.Sycamore: return R(rng, 14f, 21f);
                case TreeKind.Cedar: return R(rng, 4f, 9.5f);
                case TreeKind.WhitePine: return R(rng, 10f, 16f);
                case TreeKind.Spruce: return R(rng, 6f, 11f);
                default: return R(rng, 6f, 10f);
            }
        }

        /// <summary>The half-width of the solid part of a tree (what its collider covers): the trunk, or a conifer's
        /// skirt up close.</summary>
        public static float SolidRadius(TreeKind kind, float height)
        {
            if (Conifer(kind)) return Mathf.Clamp(height * 0.06f, 0.3f, 0.6f);
            return Mathf.Clamp(height * FormOf(kind).Girth * 1.25f, 0.15f, 0.55f);
        }

        /// <summary>A conifer's widest skirt per metre of height: its tiers at their widest, most ragged and snow-rimmed.</summary>
        public static float Skirt(TreeKind kind)
        {
            switch (kind)
            {
                case TreeKind.Cedar: return 0.34f;
                case TreeKind.Spruce: return 0.41f;
                default: return 0.43f;
            }
        }

        /// <summary>How far from the trunk a tree of the kind reaches at most (a hardwood's limbs with their branches, a
        /// conifer's skirt).</summary>
        public static float Reach(TreeKind kind, float height)
            => Conifer(kind) ? height * Skirt(kind) : height * FormOf(kind).Spread * 1.6f;

        /// <summary>The shortest a tree of the kind is drawn: a smaller one isn't worth planting where its room cuts it
        /// down.</summary>
        public static float Shortest(TreeKind kind)
        {
            switch (kind)
            {
                case TreeKind.Dogwood: case TreeKind.Cedar: return 3f;
                case TreeKind.Pear: case TreeKind.Spruce: case TreeKind.ScrubPine: return 4f;
                default: return 6f;
            }
        }

        /// <summary>Draws a tree of <paramref name="height"/> metres standing at the frame's origin.</summary>
        public static void Tree(RaidMesh m, TreeKind kind, float height, System.Random rng, TreeTier tier)
            => Tree(m, kind, height, rng, tier, default);

        /// <summary>Draws a tree of <paramref name="height"/> metres standing at the frame's origin, a hardwood's limbs cut
        /// back short of what's in <paramref name="room"/> (a conifer is sized to its room by the caller).</summary>
        public static void Tree(RaidMesh m, TreeKind kind, float height, System.Random rng, TreeTier tier, in TreeRoom room)
        {
            if (tier == TreeTier.Blob)
            {
                Blob(m, kind, height, rng);
                return;
            }
            switch (kind)
            {
                case TreeKind.Cedar: Cedar(m, height, rng, tier); break;
                case TreeKind.WhitePine: WhitePine(m, height, rng, tier); break;
                case TreeKind.Spruce: Spruce(m, height, rng, tier); break;
                case TreeKind.ScrubPine: ScrubPine(m, height, rng, tier); break;
                default: Hardwood(m, FormOf(kind), height, rng, tier, room); break;
            }
        }

        // A far tree: a trunk under one low mass of crown, a bare hardwood's grey-brown haze of twigs (a sycamore's paler)
        // or a conifer's dark cone with snow on its upper half.
        private static void Blob(RaidMesh m, TreeKind kind, float h, System.Random rng)
        {
            if (Conifer(kind))
            {
                Color needles = kind == TreeKind.Spruce ? new Color(0.24f, 0.33f, 0.35f) : kind == TreeKind.Cedar
                    ? new Color(0.20f, 0.23f, 0.13f) : kind == TreeKind.ScrubPine ? new Color(0.24f, 0.30f, 0.16f) : new Color(0.14f, 0.23f, 0.20f);
                float foot = kind == TreeKind.WhitePine ? h * 0.25f : kind == TreeKind.ScrubPine ? h * 0.3f : h * 0.05f;
                float r = h * Skirt(kind) * R(rng, 0.7f, 0.85f);
                m.Cylinder(Vector3.zero, Mathf.Max(0.1f, h * 0.025f), foot + 0.3f, 3, M(new Color(0.30f, 0.24f, 0.20f)), Quaternion.identity, 0.7f, false);
                m.Cylinder(new Vector3(0f, foot, 0f), r, h - foot, 5, M(V(needles, rng, 0.06f)), Quaternion.identity, 0.04f, false);
                float y = foot + (h - foot) * 0.45f;
                m.Cylinder(new Vector3(0f, y, 0f), r * 0.6f, h - y + 0.05f, 5, M(Snow), Quaternion.identity, 0.05f, false);
                return;
            }
            Form f = FormOf(kind);
            float crownBase = h * R(rng, f.BaseMin, f.BaseMax);
            float crownR = h * f.Spread * R(rng, 0.7f, 0.9f);
            Color bark = V(f.Bark, rng, 0.05f);
            // Bare twigs seen from afar: a grey-brown haze, lighter than the bark (a sycamore's paler still).
            Color haze = Color.Lerp(bark, f.Pale ? PaleBark : new Color(0.60f, 0.57f, 0.54f), f.Pale ? 0.6f : 0.55f);
            m.Cylinder(Vector3.zero, Mathf.Max(0.12f, h * f.Girth), crownBase + (h - crownBase) * 0.3f, 4, M(bark), Quaternion.identity, 0.6f, false);
            m.Ellipsoid(new Vector3(0f, (crownBase + h) * 0.5f, 0f), new Vector3(crownR * 2f, (h - crownBase) * 0.9f, crownR * 2f),
                Quaternion.Euler(0f, R(rng, 0f, 90f), 0f), M(haze), 6, 3);
        }

        #region Hardwoods
        private static Vector3 Heading(float azimuth, float elevation)
        {
            float a = azimuth * Mathf.Deg2Rad, e = elevation * Mathf.Deg2Rad;
            float c = Mathf.Cos(e);
            return new Vector3(Mathf.Sin(a) * c, Mathf.Sin(e), Mathf.Cos(a) * c);
        }

        private static Quaternion Along(Vector3 dir) => Quaternion.FromToRotation(Vector3.up, dir);

        // A bare hardwood: the flared foot and the trunk (on a sycamore, grey-brown below and white above), scaffold limbs
        // round it on a golden-angle spiral (a leader tree's climbing it, shorter and steeper towards the top, so the crown
        // rounds off), two or three branches off each, two twigs off each branch, and snow along the lower limbs. A limb
        // heading for something in its room (a wall, a wire) is cut back short of it, its branches with it.
        private static void Hardwood(RaidMesh m, Form f, float h, System.Random rng, TreeTier tier, in TreeRoom room)
        {
            bool lite = tier == TreeTier.Lite, full = tier == TreeTier.Full;
            Color barkColour = V(f.Bark, rng, 0.06f);
            Surf bark = M(barkColour);
            Surf limbs = f.Pale ? M(V(PaleBark, rng, 0.04f)) : bark;
            Surf twigs = f.Pale ? M(V(new Color(0.52f, 0.47f, 0.40f), rng, 0.05f)) : M(barkColour * 1.08f);
            Surf snow = M(Snow);
            float r0 = h * f.Girth * R(rng, 0.85f, 1.15f);
            float crownBase = h * R(rng, f.BaseMin, f.BaseMax);
            float crownR = h * f.Spread * R(rng, 0.85f, 1.15f) * (tier == TreeTier.Wood ? 0.85f : 1f);
            Quaternion lean = Quaternion.AngleAxis(R(rng, 0f, f.Lean), Quaternion.Euler(0f, R(rng, 0f, 360f), 0f) * Vector3.right);
            Vector3 up = lean * Vector3.up;
            int sides = lite ? 4 : 7;

            float trunkTop = f.Leader ? h * 0.9f : crownBase * 1.08f;
            float taper = f.Leader ? 0.2f : 0.62f;
            if (!lite) m.Cylinder(Vector3.zero, r0 * 1.42f, Mathf.Min(0.6f, h * 0.05f), sides, bark, lean, 0.7f, false);
            if (f.Pale && !lite)
            {
                float split = crownBase * 0.5f;
                float atSplit = 1f - (1f - taper) * split / trunkTop;
                m.Cylinder(Vector3.zero, r0, split, sides, bark, lean, atSplit, false);
                m.Cylinder(up * split, r0 * atSplit, trunkTop - split, sides, limbs, lean, taper / atSplit, false);
            }
            else m.Cylinder(Vector3.zero, r0, trunkTop, sides, bark, lean, taper, false);

            int count = lite ? 3 : tier == TreeTier.Wood ? Mathf.Max(3, f.Limbs - 1) : f.Limbs + rng.Next(0, 2);
            int branches = lite ? 1 : full ? 3 : 2;
            int snowy = full ? 3 : tier == TreeTier.Wood ? 1 : 0;
            int limbSides = lite ? 3 : 5;
            float spin = R(rng, 0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float frac = (i + 0.5f) / count;
                float y = f.Leader ? Mathf.Lerp(crownBase, h * 0.76f, frac) : crownBase * (0.86f + 0.3f * frac);
                float azimuth = spin + i * 137.5f + R(rng, -18f, 18f);
                float rise = Mathf.Min(80f, R(rng, f.ElevMin, f.ElevMax) + (f.Leader ? 14f * frac : 0f));
                Vector3 dir = Heading(azimuth, rise);
                float horizontal = Mathf.Max(0.25f, Mathf.Cos(rise * Mathf.Deg2Rad));
                float vertical = Mathf.Max(0.2f, Mathf.Sin(rise * Mathf.Deg2Rad));
                float outward = crownR * (f.Leader ? 1f - 0.55f * frac : 1f) * R(rng, 0.75f, 1.05f);
                Vector3 start = up * y;
                if (room.Count > 0)
                {
                    // The knee's second piece and the branches carry a limb a third again past its heading's reach, and a
                    // leaning trunk has already carried the limb's foot some way towards (or away from) what's there.
                    float a = azimuth * Mathf.Deg2Rad;
                    float carried = start.x * Mathf.Sin(a) + start.z * Mathf.Cos(a);
                    outward = Mathf.Min(outward, (room.Reach(azimuth) - 0.3f - carried) / 1.35f);
                }
                float len = Mathf.Min(outward / horizontal, (h * 0.98f - y) / vertical);
                if (len < 0.4f) continue;
                float radius = r0 * (f.Leader ? 0.45f : 0.58f) * R(rng, 0.85f, 1.1f) * (1f - 0.3f * frac);
                // The limb in two pieces: out along its heading to a knee, then turning up and aside.
                float bend = lite ? 1f : R(rng, 0.45f, 0.62f);
                float len1 = len * bend;
                Vector3 knee = start + dir * len1;
                m.Cylinder(start, radius, len1, limbSides, limbs, Along(dir), lite ? 0.35f : 0.62f, false);
                Vector3 dir2 = dir;
                float len2 = 0f, r1 = radius * 0.62f;
                if (!lite)
                {
                    dir2 = Heading(azimuth + R(rng, -24f, 24f), Mathf.Min(84f, rise + R(rng, 8f, 26f)));
                    len2 = len * (1f - bend) * 1.08f;
                    m.Cylinder(knee, r1, len2, limbSides - 1, limbs, Along(dir2), 0.45f, false);
                }
                if (i < snowy && rise < 54f)
                    m.Box(start + dir * (len1 * 0.5f) + Vector3.up * (radius * 0.85f), new Vector3(radius * 1.5f, radius * 0.6f, len1 * 0.8f),
                        Quaternion.LookRotation(dir), snow, true);
                for (int j = 0; j < branches; j++)
                {
                    // Off the outer piece or the end of the inner one, alternately.
                    bool outer = !lite && j % 2 == 0;
                    float t = outer ? R(rng, 0.15f, 0.85f) : R(rng, 0.55f, 1f);
                    Vector3 from = outer ? knee + dir2 * (len2 * t) : start + dir * (len1 * t);
                    Vector3 baseDir = outer ? dir2 : dir;
                    float baseAzimuth = Mathf.Atan2(baseDir.x, baseDir.z) * Mathf.Rad2Deg;
                    float baseRise = Mathf.Asin(Mathf.Clamp(baseDir.y, -1f, 1f)) * Mathf.Rad2Deg;
                    float bAzimuth = baseAzimuth + (j % 2 == 0 ? 1f : -1f) * R(rng, 25f, 60f);
                    float bRise = Mathf.Min(82f, baseRise + R(rng, 6f, 28f));
                    Vector3 bDir = Heading(bAzimuth, bRise);
                    float bLen = len * R(rng, 0.32f, 0.52f) * (outer ? 0.9f : 1.05f);
                    float bRadius = Mathf.Max(0.025f, (outer ? r1 * (1f - 0.5f * t) : radius * (1f - 0.4f * t)) * 0.62f);
                    m.Cylinder(from, bRadius, bLen, lite ? 3 : 4, j == 1 ? limbs : twigs, Along(bDir), 0.3f, false);
                    if (!full) continue;
                    for (int k = 0; k < 2; k++)
                    {
                        Vector3 tFrom = from + bDir * (bLen * R(rng, 0.45f, 0.95f));
                        Vector3 tDir = Heading(bAzimuth + R(rng, -50f, 50f), Mathf.Min(85f, bRise + R(rng, -12f, 24f)));
                        m.Cylinder(tFrom, Mathf.Max(0.016f, bRadius * 0.5f), bLen * R(rng, 0.35f, 0.6f), 3, twigs, Along(tDir), 0.3f, false);
                    }
                }
            }
            if (!full || !f.Leader) return;
            // The leader's tip splits into a few whips.
            Vector3 top = up * trunkTop;
            for (int k = 0; k < 3; k++)
            {
                Vector3 d = Heading(spin + k * 120f + R(rng, -20f, 20f), R(rng, 55f, 78f));
                m.Cylinder(top, Mathf.Max(0.02f, r0 * 0.14f), h * R(rng, 0.08f, 0.14f), 3, twigs, Along(d), 0.3f, false);
            }
        }
        #endregion

        #region Conifers
        /// <summary>The proportions of one stack of conifer tiers.</summary>
        private struct Stack
        {
            public int Tiers;
            /// <summary>The lowest tier's foot and the top, as shares of the height.</summary>
            public float Foot, Top;
            /// <summary>Each tier's height, as a share of the tree's.</summary>
            public float TierHeight;
            /// <summary>The lowest tier's radius per metre of height, and the top one's share of it.</summary>
            public float Width, Narrowing;
            /// <summary>How far a tier may stray off the axis, as a share of its radius.</summary>
            public float Ragged;
            /// <summary>Where each tier's ring of snow lies, as shares of its height.</summary>
            public float SnowFrom, SnowTo;
            public int Sides;
        }

        // Tiers of cones up a lean, each with a ring of snow on its upper face: the green shows below the snow and the
        // tier above's skirt hides its tip; the top tier wears a cap.
        private static void Stacked(RaidMesh m, float h, Stack s, Color needles, Quaternion lean, System.Random rng)
        {
            Surf snow = M(Snow);
            for (int i = 0; i < s.Tiers; i++)
            {
                float f = s.Tiers == 1 ? 0f : i / (float)(s.Tiers - 1);
                float th = h * s.TierHeight;
                float y = Mathf.Min(h * Mathf.Lerp(s.Foot, s.Top, f), h - th);
                float r = h * s.Width * Mathf.Lerp(1f, s.Narrowing, f) * R(rng, 0.88f, 1.12f);
                var off = new Vector3(R(rng, -1f, 1f), 0f, R(rng, -1f, 1f)) * (r * s.Ragged);
                Quaternion turn = lean * Quaternion.Euler(0f, R(rng, 0f, 360f), 0f);
                Vector3 foot = lean * new Vector3(0f, y, 0f) + off;
                m.Cylinder(foot, r, th, s.Sides, M(V(needles, rng, 0.05f)), turn, 0.06f, false);
                bool top = i == s.Tiers - 1;
                float from = s.SnowFrom, to = top ? 0.94f : s.SnowTo;
                float rFrom = r * (1f - from * 0.94f) * 1.06f + 0.02f, rTo = r * (1f - to * 0.94f) * 1.06f + 0.01f;
                m.Cylinder(foot + turn * new Vector3(0f, th * from, 0f), rFrom, th * (to - from), s.Sides, snow, turn,
                    Mathf.Clamp01(rTo / rFrom), false);
            }
        }

        private static Quaternion Lean(System.Random rng, float most)
            => Quaternion.AngleAxis(R(rng, 0f, most), Quaternion.Euler(0f, R(rng, 0f, 360f), 0f) * Vector3.right);

        // Eastern red cedar: a short red-brown trunk under a narrow, ragged flame of olive-bronze tiers, lightly snowed.
        private static void Cedar(RaidMesh m, float h, System.Random rng, TreeTier tier)
        {
            Quaternion lean = Lean(rng, 4f);
            m.Cylinder(Vector3.zero, Mathf.Max(0.08f, h * 0.03f), h * 0.3f, tier == TreeTier.Lite ? 4 : 5,
                M(V(new Color(0.36f, 0.25f, 0.19f), rng, 0.06f)), lean, 0.6f, false);
            Color needles = Color.Lerp(new Color(0.16f, 0.22f, 0.13f), new Color(0.26f, 0.25f, 0.13f), R(rng, 0f, 0.7f));
            Stacked(m, h, new Stack
            {
                Tiers = tier == TreeTier.Lite ? 1 : tier == TreeTier.Wood ? 3 : 4,
                Foot = 0.06f, Top = 0.46f, TierHeight = tier == TreeTier.Lite ? 0.94f : 0.52f,
                Width = R(rng, 0.17f, 0.25f), Narrowing = 0.6f, Ragged = 0.14f, SnowFrom = 0.38f, SnowTo = 0.7f,
                Sides = tier == TreeTier.Lite ? 5 : 7,
            }, V(needles, rng, 0.06f), lean, rng);
        }

        // White pine: a tall grey trunk through soft blue-green whorls, flat and wide, with daylight between them.
        private static void WhitePine(RaidMesh m, float h, System.Random rng, TreeTier tier)
        {
            Quaternion lean = Lean(rng, 3f);
            m.Cylinder(Vector3.zero, Mathf.Max(0.12f, h * 0.022f), h * 0.86f, tier == TreeTier.Lite ? 4 : 6,
                M(V(new Color(0.26f, 0.23f, 0.21f), rng, 0.05f)), lean, 0.3f, false);
            int tiers = tier == TreeTier.Lite ? 2 : tier == TreeTier.Wood ? 4 : 5;
            Stacked(m, h, new Stack
            {
                Tiers = tiers, Foot = 0.3f, Top = 0.84f, TierHeight = tier == TreeTier.Lite ? 0.36f : 0.15f,
                Width = R(rng, 0.22f, 0.3f), Narrowing = 0.45f, Ragged = 0.22f, SnowFrom = 0.25f, SnowTo = 0.8f,
                Sides = tier == TreeTier.Lite ? 6 : 8,
            }, V(new Color(0.14f, 0.23f, 0.20f), rng, 0.07f), lean, rng);
        }

        // Blue spruce: a dense, regular cone to the ground, silver-blue (or, one in three, Norway-spruce dark green),
        // heavy with snow.
        private static void Spruce(RaidMesh m, float h, System.Random rng, TreeTier tier)
        {
            Quaternion lean = Lean(rng, 1.5f);
            m.Cylinder(Vector3.zero, Mathf.Max(0.08f, h * 0.025f), h * 0.2f, 4, M(new Color(0.24f, 0.20f, 0.18f)), lean, 0.7f, false);
            Color needles = rng.NextDouble() < 0.66 ? new Color(0.31f, 0.40f, 0.43f) : new Color(0.12f, 0.20f, 0.15f);
            Stacked(m, h, new Stack
            {
                Tiers = tier == TreeTier.Lite ? 1 : tier == TreeTier.Wood ? 4 : 5,
                Foot = 0.04f, Top = 0.62f, TierHeight = tier == TreeTier.Lite ? 0.96f : 0.38f,
                Width = R(rng, 0.27f, 0.33f), Narrowing = 0.42f, Ragged = 0.04f, SnowFrom = 0.3f, SnowTo = 0.78f,
                Sides = tier == TreeTier.Lite ? 6 : 8,
            }, V(needles, rng, 0.05f), lean, rng);
        }

        // Virginia pine: a crooked trunk, a few untidy yellow-green clumps.
        private static void ScrubPine(RaidMesh m, float h, System.Random rng, TreeTier tier)
        {
            Quaternion lean = Lean(rng, 8f);
            m.Cylinder(Vector3.zero, Mathf.Max(0.08f, h * 0.024f), h * 0.7f, 4, M(V(new Color(0.30f, 0.25f, 0.21f), rng, 0.05f)), lean, 0.4f, false);
            Stacked(m, h, new Stack
            {
                Tiers = tier == TreeTier.Lite ? 1 : 3, Foot = 0.28f, Top = 0.66f, TierHeight = tier == TreeTier.Lite ? 0.7f : 0.3f,
                Width = R(rng, 0.2f, 0.27f), Narrowing = 0.6f, Ragged = 0.35f, SnowFrom = 0.3f, SnowTo = 0.75f, Sides = 6,
            }, V(new Color(0.24f, 0.30f, 0.16f), rng, 0.08f), lean, rng);
        }
        #endregion

        #region Bushes and deadwood
        /// <summary>A bush about <paramref name="size"/> metres tall at the frame's origin: an evergreen mound under a cap of
        /// snow, or a bare thicket of upswept canes.</summary>
        public static void Shrub(RaidMesh m, ShrubKind kind, float size, System.Random rng)
        {
            if (kind == ShrubKind.Thicket)
            {
                Surf canes = M(V(new Color(0.38f, 0.31f, 0.25f), rng, 0.08f));
                int n = rng.Next(5, 8);
                float spin = R(rng, 0f, 360f);
                for (int i = 0; i < n; i++)
                {
                    Vector3 d = Heading(spin + i * (360f / n) + R(rng, -20f, 20f), R(rng, 52f, 80f));
                    var foot = new Vector3(R(rng, -0.12f, 0.12f), 0f, R(rng, -0.12f, 0.12f)) * size;
                    m.Cylinder(foot, Mathf.Max(0.015f, size * 0.02f), size * R(rng, 0.85f, 1.25f), 3, canes, Along(d), 0.3f, false);
                }
                return;
            }
            Color green = kind == ShrubKind.Yew ? new Color(0.10f, 0.20f, 0.12f)
                : kind == ShrubKind.Boxwood ? new Color(0.17f, 0.29f, 0.14f) : new Color(0.22f, 0.31f, 0.27f);
            float w = size * R(rng, 1.1f, 1.45f), d2 = size * R(rng, 1.0f, 1.3f);
            m.Ellipsoid(new Vector3(0f, size * 0.42f, 0f), new Vector3(w, size * 0.9f, d2), M(V(green, rng, 0.08f)), 6, 3);
            m.Ellipsoid(new Vector3(0f, size * 0.72f, 0f), new Vector3(w * 0.82f, size * 0.36f, d2 * 0.82f), M(Snow), 6, 2);
        }

        /// <summary>A fallen trunk lying along the frame's x axis, half sunk in the snow, a ridge of snow along its top.</summary>
        public static void Log(RaidMesh m, float length, float radius, System.Random rng)
        {
            Color bark = V(new Color(0.27f, 0.23f, 0.19f), rng, 0.08f);
            m.CylinderX(new Vector3(0f, radius * 0.7f, 0f), radius, length, 6, M(bark));
            m.Box(new Vector3(0f, radius * 1.62f, 0f), new Vector3(length * R(rng, 0.7f, 0.9f), radius * 0.32f, radius * 1.1f),
                Quaternion.identity, M(Snow), true);
            // A broken limb stub, cocked up.
            Vector3 stub = new Vector3(R(rng, -0.3f, 0.3f) * length, radius * 1.1f, 0f);
            m.Cylinder(stub, radius * 0.35f, radius * R(rng, 2f, 3.5f), 4, M(bark), Quaternion.Euler(R(rng, -40f, 40f), 0f, R(rng, -40f, 40f)), 0.4f, false);
        }

        /// <summary>A sawn or snapped stump with a cap of snow.</summary>
        public static void Stump(RaidMesh m, float radius, System.Random rng)
        {
            float h = R(rng, 0.25f, 0.6f);
            m.Cylinder(Vector3.zero, radius, h, 6, M(V(new Color(0.30f, 0.25f, 0.20f), rng, 0.08f)), Quaternion.identity, 0.9f, true);
            m.Cylinder(new Vector3(0f, h, 0f), radius * 0.86f, 0.06f, 6, M(Snow), Quaternion.identity, 0.8f, true);
        }

        /// <summary>A grey limestone outcrop breaking the snow (the Knobs and the Bluegrass are full of them).</summary>
        public static void Rock(RaidMesh m, float size, System.Random rng)
        {
            Color stone = V(new Color(0.55f, 0.54f, 0.50f), rng, 0.08f);
            var s = new Vector3(size * R(rng, 1f, 1.6f), size * R(rng, 0.35f, 0.6f), size * R(rng, 0.7f, 1.1f));
            Quaternion tilt = Quaternion.Euler(R(rng, -8f, 8f), R(rng, 0f, 180f), R(rng, -8f, 8f));
            m.Box(new Vector3(0f, s.y * 0.3f, 0f), s, tilt, M(stone));
            m.Box(new Vector3(0f, s.y * 0.8f + 0.02f, 0f), new Vector3(s.x * 0.8f, 0.06f, s.z * 0.75f), tilt, M(Snow), true);
        }
        #endregion
    }
}
