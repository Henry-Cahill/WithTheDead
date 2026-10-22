using UnityEngine;

namespace CloneZ
{
    internal static partial class FurnitureBuilder
    {
        #region Home lighting, switches and wall decor
        // The starter home's working wall fittings. Each is drawn in world space round a point on a wall face
        // (normal pointing out of the wall); the moving bits — a switch's toggle, a panel's door, a breaker's
        // handle, a clock's hands, a picture — are drawn on their own so the fixtures can turn them.
        public static readonly Color SwitchCream = new Color(0.90f, 0.87f, 0.78f);
        public static readonly Color ClockWood = new Color(0.55f, 0.45f, 0.35f);
        public static readonly Color PanelGrey = new Color(0.55f, 0.57f, 0.57f);

        /// <summary>A cream switch plate <paramref name="width"/> wide centred on <paramref name="at"/> (on the wall face).</summary>
        public static void SwitchPlate(RaidMesh m, Vector3 at, Vector3 normal, float width)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Box(at + normal * 0.004f, new Vector3(width, 0.114f, 0.008f), q, G(SwitchCream));
            m.Box(at + normal * 0.004f + Vector3.up * (0.114f * 0.5f - 0.001f), new Vector3(width, 0.002f, 0.001f), q, Mt(Chrome));
        }

        /// <summary>A toggle's slot and the two screws above and below it, on a plate whose face is at <paramref name="at"/>.</summary>
        public static void SwitchSlot(RaidMesh m, Vector3 at, Vector3 normal)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Box(at + normal * 0.0085f, new Vector3(0.011f, 0.026f, 0.001f), q, M(new Color(0.30f, 0.28f, 0.24f)));
            m.Box(at + normal * 0.0085f + Vector3.up * 0.042f, new Vector3(0.006f, 0.006f, 0.001f), q, Mt(Chrome));
            m.Box(at + normal * 0.0085f - Vector3.up * 0.042f, new Vector3(0.006f, 0.006f, 0.001f), q, Mt(Chrome));
        }

        /// <summary>A switch's toggle, drawn thrown down (off) <paramref name="tilt"/> degrees below level from
        /// <paramref name="pivot"/>; it flips up by turning about <c>Vector3.Cross(normal, Vector3.up)</c>.</summary>
        public static void SwitchToggle(RaidMesh m, Vector3 pivot, Vector3 normal, float tilt)
        {
            float a = tilt * Mathf.Deg2Rad;
            Vector3 d = (normal * Mathf.Cos(a) - Vector3.up * Mathf.Sin(a)).normalized;
            const float len = 0.026f;
            m.Box(pivot + d * (len * 0.5f), new Vector3(0.0095f, 0.008f, len), Quaternion.LookRotation(d, Vector3.up), G(SwitchCream * 0.96f));
        }

        /// <summary>An oval bulkhead light — a frosted dome behind two dark bands on an off-white base — on the wall
        /// at <paramref name="at"/>. Returns the bulb; <paramref name="glow"/> (may be null) gets the lit dome.</summary>
        public static Vector3 BulkheadLight(RaidMesh m, RaidMesh glow, Vector3 at, Vector3 normal)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Ellipsoid(at + normal * 0.006f, new Vector3(0.28f, 0.18f, 0.03f), q, M(new Color(0.80f, 0.79f, 0.75f)), 12, 4);
            Vector3 dome = at + normal * 0.02f;
            m.Ellipsoid(dome, new Vector3(0.22f, 0.125f, 0.11f), q, M(new Color(0.88f, 0.86f, 0.79f)), 12, 6);
            // Chrome screw heads on the dome
            m.Box(dome + Vector3.up * 0.032f, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            m.Box(dome - Vector3.up * 0.032f, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            glow?.Ellipsoid(dome, new Vector3(0.226f, 0.13f, 0.116f), q, Gl(WarmGlow), 12, 6);
            // The bands hug the dome a little above and below its middle.
            Surf band = M(new Color(0.20f, 0.20f, 0.21f));
            for (int e = -1; e <= 1; e += 2)
                m.Ellipsoid(dome + Vector3.up * (e * 0.028f), new Vector3(0.215f, 0.012f, 0.112f), q, band, 12, 3);
            return dome;
        }

        /// <summary>A bathroom vanity light: a frosted tube held out from a chrome backplate between two chrome caps, on the
        /// wall at <paramref name="at"/> (<paramref name="along"/> runs along the wall). Returns the bulb; <paramref name="glow"/>
        /// (may be null) gets the lit tube.</summary>
        public static Vector3 VanityLight(RaidMesh m, RaidMesh glow, Vector3 at, Vector3 normal, Vector3 along)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            // side direction for later brackets & knob
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            float yaw = Mathf.Abs(along.z) > 0.5f ? 90f : 0f;
            m.Box(at + normal * 0.01f, new Vector3(0.42f, 0.06f, 0.02f), q, Mt(Chrome));
            Vector3 tube = at + normal * 0.07f;
            for (int e = -1; e <= 1; e += 2)
            {
                Vector3 end = tube + along * (e * 0.185f);
                m.Rod(at + normal * 0.02f + along * (e * 0.185f), end, 0.018f, Mt(Chrome), 6);
                m.CylinderX(end, 0.042f, 0.03f, 10, Mt(Chrome), yaw);
            }
            m.CylinderX(tube, 0.033f, 0.34f, 10, M(new Color(0.90f, 0.89f, 0.84f)), yaw);
                // frosted glass envelope, slightly larger than the metal tube
                m.CylinderX(tube, 0.035f, 0.342f, 10, M(new Color(0.92f, 0.92f, 0.96f, 0.28f)), yaw);
                // left chrome bracket
                m.Box(tube + right * 0.045f, new Vector3(0.006f, 0.05f, 0.004f), q, Mt(Chrome));
                // right chrome bracket
                m.Box(tube - right * 0.045f, new Vector3(0.006f, 0.05f, 0.004f), q, Mt(Chrome));
                // front adjustment knob (west side of the fixture)
                m.Cylinder(tube + right * 0.06f - normal * 0.015f, 0.008f, 0.004f, 12, Mt(Chrome), q);
                // dust film, very thin, covering the tube and brackets
                m.Box(tube + normal * 0.001f, new Vector3(0.07f, 0.07f, 0.001f), q, M(new Color(0f,0f,0f,0.12f)));
            glow?.CylinderX(tube, 0.036f, 0.342f, 10, Gl(WarmGlow), yaw);
            return tube;
        }

        /// <summary>A grey steel breaker box <paramref name="w"/> × <paramref name="h"/>, <paramref name="d"/> deep, whose back is
        /// centred on <paramref name="back"/> (on the wall face): a rim round a recessed dead-front with two columns of
        /// branch breakers, the main breaker's block and a typed circuit card. The door and the main breaker's handle are
        /// drawn apart (<see cref="BreakerDoor"/>, <see cref="BreakerHandle"/>). Returns the main handle's pivot.</summary>
        public static Vector3 BreakerBox(RaidMesh m, Vector3 back, Vector3 normal, float w, float h, float d)
        {
            const float recess = 0.045f, rim = 0.008f;
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            Surf steel = Mt(PanelGrey);
            float solid = d - recess;
            m.Box(back + normal * (solid * 0.5f), new Vector3(w, h, solid), q, steel);
            Vector3 lip = back + normal * (solid + recess * 0.5f);
            m.Box(lip + right * (w * 0.5f - rim * 0.5f), new Vector3(rim, h, recess), q, steel);
            m.Box(lip - right * (w * 0.5f - rim * 0.5f), new Vector3(rim, h, recess), q, steel);
            m.Box(lip + Vector3.up * (h * 0.5f - rim * 0.5f), new Vector3(w, rim, recess), q, steel);
            m.Box(lip - Vector3.up * (h * 0.5f - rim * 0.5f), new Vector3(w, rim, recess), q, steel);

            Vector3 front = back + normal * solid;
            m.Box(front + normal * 0.001f, new Vector3(w - 0.04f, h - 0.04f, 0.002f), q, M(PanelGrey * 0.8f));
            // Add four chrome rivet heads on the front panel
            float rivetX = w * 0.5f - 0.02f;
            float rivetY = h * 0.5f - 0.02f;
            m.Box(front + normal * 0.0015f + right * rivetX + Vector3.up * rivetY, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            m.Box(front + normal * 0.0015f - right * rivetX + Vector3.up * rivetY, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            m.Box(front + normal * 0.0015f + right * rivetX - Vector3.up * rivetY, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            m.Box(front + normal * 0.0015f - right * rivetX - Vector3.up * rivetY, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            Surf body = M(new Color(0.09f, 0.09f, 0.10f)), nub = M(new Color(0.20f, 0.20f, 0.21f));
            for (int col = -1; col <= 1; col += 2)
                for (int row = 0; row < 5; row++)
                {
                    Vector3 c = front + right * (col * 0.055f) + Vector3.up * (0.07f - row * 0.042f);
                    m.Box(c + normal * 0.006f, new Vector3(0.05f, 0.03f, 0.008f), q, body);
                    m.Box(c + normal * 0.0125f, new Vector3(0.012f, 0.012f, 0.005f), q, nub);
                }
            Vector3 main = front + Vector3.up * 0.14f;
            m.Box(main + normal * 0.006f, new Vector3(0.08f, 0.05f, 0.012f), q, body);
            m.Upright(front + normal * 0.0035f - Vector3.up * 0.155f - right * 0.07f, normal, 0.06f, 0.075f, DecorMaterials.Paper(0), new Rect(0f, 0f, 1f, 1f));
            return main + normal * 0.012f;
        }

        /// <summary>A breaker box's tin door, closed over the box's open front centred on <paramref name="front"/>, with a
        /// latch at the edge toward <paramref name="freeSide"/> and a warning label.</summary>
        public static void BreakerDoor(RaidMesh m, Vector3 front, Vector3 normal, Vector3 freeSide, float w, float h)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Box(front + normal * 0.006f, new Vector3(w + 0.006f, h + 0.006f, 0.012f), q, Mt(PanelGrey * 1.06f));
            m.Box(front + normal * 0.015f + freeSide * (w * 0.5f - 0.028f), new Vector3(0.016f, 0.045f, 0.006f), q, Mt(Chrome));
            Surf sign = DecorMaterials.Sign("DANGER", new Color(0.92f, 0.76f, 0.16f), new Color(0.08f, 0.08f, 0.08f), out float aspect);
            const float sw = 0.13f;
            m.Upright(front + normal * 0.0135f + Vector3.up * (h * 0.28f), normal, sw, sw / aspect, sign, new Rect(0f, 0f, 1f, 1f));
        }

        /// <summary>The main breaker's handle, drawn thrown down (off) <paramref name="tilt"/> degrees below level from
        /// <paramref name="pivot"/>; it flips up by turning about <c>Vector3.Cross(normal, Vector3.up)</c>.</summary>
        public static void BreakerHandle(RaidMesh m, Vector3 pivot, Vector3 normal, float tilt)
        {
            float a = tilt * Mathf.Deg2Rad;
            Vector3 d = (normal * Mathf.Cos(a) - Vector3.up * Mathf.Sin(a)).normalized;
            const float len = 0.03f;
            m.Box(pivot + d * (len * 0.5f), new Vector3(0.05f, 0.012f, len), Quaternion.LookRotation(d, Vector3.up), M(new Color(0.05f, 0.05f, 0.05f)));
        }

        /// <summary>A round wall clock's case and dial — hour marks and the boss the hands turn on — centred on
        /// <paramref name="at"/> (on the wall face). The hands are drawn apart (<see cref="ClockHand"/>).</summary>
        public static void ClockDial(RaidMesh m, Vector3 at, Vector3 normal)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Quaternion disc = q * Quaternion.Euler(90f, 0f, 0f);
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;   // three o'clock direction
            m.Cylinder(at, 0.16f, 0.04f, 16, M(new Color(0.15f, 0.15f, 0.16f)), disc);
            // Wooden mounting plate that the clock bolts to (slightly larger than the dial)
            m.Box(at + normal * 0.038f, new Vector3(0.34f, 0.34f, 0.01f), q, M(ClockWood));
            // Small brass keyhole centred on the top of the dial
            var brass = M(new Color(0.80f, 0.60f, 0.30f));
            m.Cylinder(at + normal * 0.042f, 0.005f, 0.002f, 12, brass, disc);
            // Chrome screw head at the top‑center of the mounting plate
            m.Box(at + normal * 0.038f + Vector3.up * 0.055f, new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            // Small winding knob on the left side (west) of the clock
            // Use existing 'right' vector computed earlier
            var knobColour = M(new Color(0.60f, 0.50f, 0.40f));
            m.Cylinder(at + normal * 0.041f - right * 0.12f, 0.006f, 0.006f, 12, knobColour, disc);
            // Very thin dust overlay to suggest age
            m.Box(at + normal * 0.0375f, new Vector3(0.34f, 0.34f, 0.001f), q, M(new Color(0f,0f,0f,0.15f)));
            m.Cylinder(at, 0.165f, 0.004f, 16, Mt(Chrome), disc);
            m.Cylinder(at + normal * 0.041f, 0.14f, 0.002f, 16, M(new Color(0.88f, 0.87f, 0.82f)), disc);

            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Vector3 dir = Vector3.up * Mathf.Cos(a) + right * Mathf.Sin(a);
                bool major = i % 3 == 0;
                float len = major ? 0.024f : 0.012f;
                m.Box(at + normal * 0.0435f + dir * (0.124f - len * 0.5f), new Vector3(major ? 0.007f : 0.004f, len, 0.002f),
                    Quaternion.LookRotation(normal, dir), M(Black));
            }
            m.Cylinder(at + normal * 0.043f, 0.008f, 0.009f, 8, M(Black), disc);
        }

        /// <summary>A clock hand pointing at twelve from <paramref name="centre"/>, <paramref name="lift"/> off the wall face
        /// (turn it about the dial's normal through the centre to tell the time).</summary>
        public static void ClockHand(RaidMesh m, Vector3 centre, Vector3 normal, float length, float width, float lift)
        {
            const float tail = 0.018f;
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Box(centre + normal * lift + Vector3.up * ((length - tail) * 0.5f), new Vector3(width, length + tail, 0.003f), q, M(Black));
        }

        /// <summary>A framed print hanging level, centred on <paramref name="at"/> (on the wall face): a
        /// <paramref name="frame"/>-coloured frame round poster <paramref name="kind"/> (see <see cref="DecorMaterials.Poster"/>).</summary>
        public static void PictureFrame(RaidMesh m, Vector3 at, Vector3 normal, float w, float h, Color frame, int kind)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            m.Box(at + normal * 0.008f, new Vector3(w + 0.05f, h + 0.05f, 0.016f), q, M(frame));
            // Compute right vector for corner brackets
            Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
            // Add four chrome corner brackets at the outer corners of the frame
            float halfW = (w + 0.05f) * 0.5f;
            float halfH = (h + 0.05f) * 0.5f;
            Vector3 frontOffset = normal * 0.0175f;
            m.Box(at + frontOffset + right * halfW + Vector3.up * halfH, new Vector3(0.008f, 0.008f, 0.003f), q, Mt(Chrome));
            m.Box(at + frontOffset - right * halfW + Vector3.up * halfH, new Vector3(0.008f, 0.008f, 0.003f), q, Mt(Chrome));
            m.Box(at + frontOffset + right * halfW - Vector3.up * halfH, new Vector3(0.008f, 0.008f, 0.003f), q, Mt(Chrome));
            m.Box(at + frontOffset - right * halfW - Vector3.up * halfH, new Vector3(0.008f, 0.008f, 0.003f), q, Mt(Chrome));
            m.Upright(at + normal * 0.0175f, normal, w, h, DecorMaterials.Poster(kind), new Rect(0f, 0f, 1f, 1f));
        }
        #endregion

        #region Raid town lamps and switches
        // The raid town's working lights: the lit parts of its lamps drawn apart as glow shells (each just proud of the
        // lamp drawn unlit), the porch lantern by a house's front door and a tent's switch box.

        /// <summary>The lit bulb of a <see cref="DeskLamp"/> standing at <paramref name="at"/> turned <paramref name="yaw"/>,
        /// in the same frame — the lamp's glow shell. Mirrors DeskLamp's arm and shade, so keep the two together.</summary>
        public static void DeskLampGlow(RaidMesh glow, Vector3 at, float yaw)
        {
            Quaternion q = Yaw(yaw);
            Vector3 j2 = at + q * new Vector3(0f, 0.44f, 0.22f);
            Vector3 axis = q * Quaternion.Euler(150f, 0f, 0f) * Vector3.up;
            Vector3 mouth = j2 + axis * 0.15f;
            glow.Ellipsoid(mouth - axis * 0.045f, new Vector3(0.068f, 0.068f, 0.068f), Gl(WarmGlow), 6, 4);
        }

        /// <summary>The lit shade of a <see cref="Nightstand"/>'s lamp, in the nightstand's own frame — its glow shell (the
        /// same shell the starter home's bedside lamp wears).</summary>
        public static void NightstandGlow(RaidMesh glow)
            => glow.Cylinder(new Vector3(0f, 0.836f, -0.04f), 0.158f, 0.208f, 10, Gl(new Color(0.95f, 0.78f, 0.52f)), Quaternion.identity, 0.65f, false);

        /// <summary>A black carriage lantern on a wall beside a door — a backplate and an arm on the wall face at
        /// <paramref name="wall"/> holding a glazed lantern under a little roof, with snow on top when <paramref name="snow"/>.
        /// Returns the lantern's centre (its bulb); <paramref name="glow"/> gets its lit glass.</summary>
        public static Vector3 CarriageLantern(RaidMesh m, RaidMesh glow, Vector3 wall, Vector3 normal, bool snow)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Surf black = Mt(new Color(0.08f, 0.08f, 0.09f));
            m.Box(wall + normal * 0.01f, new Vector3(0.1f, 0.22f, 0.02f), q, black);
            m.Rod(wall + normal * 0.02f, wall + normal * 0.1f, 0.02f, black);
            Vector3 lamp = wall + normal * 0.13f;
            m.Box(lamp + Vector3.up * 0.125f, new Vector3(0.17f, 0.04f, 0.17f), q, black);
            m.Box(lamp + Vector3.up * 0.16f, new Vector3(0.08f, 0.04f, 0.08f), q, black);
            m.Box(lamp - Vector3.up * 0.115f, new Vector3(0.13f, 0.03f, 0.13f), q, black);
            m.Box(lamp, new Vector3(0.12f, 0.2f, 0.12f), q, M(new Color(0.72f, 0.64f, 0.44f)));
            for (int i = 0; i < 4; i++)
                m.Box(lamp + q * new Vector3(i % 2 == 0 ? -0.062f : 0.062f, 0f, i < 2 ? -0.062f : 0.062f), new Vector3(0.016f, 0.21f, 0.016f), q, black);
            // Chrome handle plate on front of carriage lantern
            m.Box(lamp + normal * 0.07f + Vector3.up * 0.15f, new Vector3(0.022f, 0.018f, 0.004f), q, Mt(Chrome));
            if (snow)
            {
                m.Box(lamp + Vector3.up * 0.15f, new Vector3(0.16f, 0.012f, 0.16f), q, M(DecorProps.SnowWhite));
                m.Box(lamp + Vector3.up * 0.185f, new Vector3(0.07f, 0.01f, 0.07f), q, M(DecorProps.SnowWhite));
            }
            glow?.Box(lamp, new Vector3(0.128f, 0.206f, 0.128f), q, Gl(new Color(1f, 0.84f, 0.58f)));
            return lamp;
        }

        /// <summary>A weatherproof olive switch box for a tent's lights, its back centred on <paramref name="at"/> (on the
        /// wall face) and <paramref name="depth"/> deep, with its cable running up to <paramref name="cableTop"/> (a
        /// height). The lever is drawn apart (<see cref="BreakerHandle"/>); returns its pivot.</summary>
        public static Vector3 SwitchBox(RaidMesh m, Vector3 at, Vector3 normal, float depth, float cableTop)
        {
            Quaternion q = Quaternion.LookRotation(normal, Vector3.up);
            Color olive = new Color(0.25f, 0.28f, 0.18f);
            m.Box(at + normal * (depth * 0.5f), new Vector3(0.11f, 0.16f, depth), q, Mt(olive));
            m.Box(at + normal * (depth + 0.003f), new Vector3(0.09f, 0.14f, 0.006f), q, Mt(olive * 1.12f));
            // mounting screw heads on the front face
            float screwX = 0.03f; // offset from centre left/right
            float screwY = 0.07f; // height above base
            m.Box(at + normal * (depth + 0.0035f) + Vector3.right * screwX + Vector3.up * screwY,
                new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            m.Box(at + normal * (depth + 0.0035f) - Vector3.right * screwX + Vector3.up * screwY,
                new Vector3(0.008f, 0.008f, 0.001f), q, Mt(Chrome));
            m.Box(at + normal * (depth + 0.0065f) + Vector3.up * 0.045f, new Vector3(0.06f, 0.025f, 0.002f), q, M(new Color(0.82f, 0.80f, 0.70f)));
            // faded "POWER" label
            float labelY = 0.04f; // lower than screws
            m.Box(at + normal * (depth + 0.004f) + Vector3.up * labelY,
                new Vector3(0.04f, 0.004f, 0.001f), q, M(new Color(0.15f, 0.12f, 0.10f, 0.7f)));
            // rust spot on lower edge
            m.Box(at + normal * (depth + 0.006f) - Vector3.up * 0.08f,
                new Vector3(0.06f, 0.01f, 0.001f), q, M(new Color(0.55f, 0.42f, 0.31f, 0.6f)));
            Vector3 pivot = at + normal * (depth + 0.006f) - Vector3.up * 0.02f;
            // toggle lever – a thin rod extending forward from the pivot
            m.Rod(pivot, pivot + normal * 0.018f + Vector3.up * 0.015f, 0.006f, Mt(olive), 6);
            // grip knob – a small cylinder for the user to pull
            m.Cylinder(pivot + normal * 0.018f + Vector3.up * 0.015f, 0.008f, 0.004f, 12, M(new Color(0.71f, 0.58f, 0.44f)), q);
            m.Box(pivot, new Vector3(0.028f, 0.028f, 0.006f), q, M(Black));
            Vector3 gland = at + normal * (depth * 0.5f) + Vector3.up * 0.08f;
            m.Cylinder(gland, 0.012f, 0.02f, 6, M(Black));
            if (cableTop > gland.y + 0.02f) m.Rod(gland, new Vector3(gland.x, cableTop, gland.z), 0.012f, M(Black));
            return pivot + normal * 0.003f;
        }
        #endregion
    }
}
