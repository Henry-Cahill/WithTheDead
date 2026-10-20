using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The starter home's lighting and wall decor.
    internal static partial class StarterHome
    {
        #region Lighting and wall decor
        // The lights, switches and wall decor hung on particular walls — all of them working: the wall switches by the
        // doors and the lights on their circuits (the kitchen-diner's sconce, the store room's bulkhead, the bathroom's
        // vanity light and the porch lantern), the breaker panel, the wall clock, the pictures and the house number.
        private const float ToggleTilt = 35f;       // degrees a switch's toggle stands off level: down when off, up when on
        private const float BreakerTilt = 40f;      // .. and the main breaker's handle
        private const float SwitchHeight = 1.28f;   // a little above the store room's and bathroom's chair rails (about 1.16 m off the floor)
        private const string HouseNo = "12";

        private const string KitchenPicture =
            "Green hills under a peach-coloured evening sky, the sun hanging low over the ridge. You could almost walk into it.";
        private const string BedroomPicture =
            "The same hills again, smaller \u2014 someone bought the pair. A water stain is creeping in from one corner.";

        static partial void LightingWallFixtures(BuildPiece piece, RaidMesh m, Frame f, System.Random rng)
        {
            if (Is(piece, BuildPieceType.Wall, 2f, 7f))
            {
                Picture(piece, m, f, rng, -1, 1.6f, 1.55f, 0.42f, 0.56f, KitchenPicture);
                Sconce(piece, m, f, -1, 2.48f, 1.95f);
            }
            else if (Is(piece, BuildPieceType.Wall, 6f, 7f))
            {
                Clock(piece, m, f, rng, -1, 5.85f, 2.26f);
            }
            else if (Is(piece, BuildPieceType.Wall, 7f, 4f))
            {
                Picture(piece, m, f, rng, -1, 4.55f, 1.62f, 0.36f, 0.48f, BedroomPicture);
            }
            else if (Is(piece, BuildPieceType.Wall, 6f, 9f))
            {
                VanityLight(piece, m, f, -1, 5.55f, 2.22f);
            }
            else if (Is(piece, BuildPieceType.Window, 4f, 9f))
            {
                Bulkhead(piece, m, f, -1, 4.0f, 2.32f);
            }
            else if (Is(piece, BuildPieceType.Wall, 1f, 8f))
            {
                Breakers(piece, m, f, 1, 8.05f, 1.55f);
            }
            else if (Is(piece, BuildPieceType.Doorway, 4f, 3f))
            {
                PorchLantern(piece, m, f, -1, 4.86f, 1.85f);
                HouseNumber(piece, m, f, -1, 3.14f, 1.62f);
                // Inside, beside the door: the living room's light, and the porch light nearest the door.
                Switches(piece, m, f, 1, "Light switches", new[] { LightCircuits.Living, LightCircuits.Porch }, new[] { 4.915f, 4.855f });
            }
            else if (Is(piece, BuildPieceType.Doorway, 4f, 7f))
            {
                Switches(piece, m, f, 1, "Light switch", new[] { LightCircuits.Store }, new[] { 4.835f });
            }
            else if (Is(piece, BuildPieceType.Doorway, 5f, 8f))
            {
                Switches(piece, m, f, 1, "Light switch", new[] { LightCircuits.Bath }, new[] { 8.5f });
            }
        }

        // A light switch plate by a door: a toggle for each circuit (listed in menu order) at its own spot along the wall.
        private static void Switches(BuildPiece piece, RaidMesh m, in Frame f, int side, string name, string[] circuits, float[] alongs)
        {
            Vector3 n = f.N(side);
            float lo = alongs[0], hi = alongs[0];
            for (int i = 1; i < alongs.Length; i++)
            {
                lo = Mathf.Min(lo, alongs[i]);
                hi = Mathf.Max(hi, alongs[i]);
            }
            Vector3 at = Face(f, side, (lo + hi) * 0.5f, SwitchHeight);
            FurnitureBuilder.SwitchPlate(m, at, n, hi - lo + 0.07f);
            LightSwitch sw = LightSwitch.Create(piece.transform, name, at + n * 0.03f, at, n, circuits);
            var toggles = new FixturePart[circuits.Length];
            for (int i = 0; i < circuits.Length; i++)
            {
                Vector3 spot = Face(f, side, alongs[i], SwitchHeight);
                FurnitureBuilder.SwitchSlot(m, spot, n);
                Vector3 pivot = spot + n * 0.009f;
                var toggle = new RaidMesh();
                FurnitureBuilder.SwitchToggle(toggle, pivot, n, ToggleTilt);
                // tiny dark‑metal screw detail on the toggle for a lived‑in look
                float a = ToggleTilt * Mathf.Deg2Rad;
                Vector3 d = (n * Mathf.Cos(a) - Vector3.up * Mathf.Sin(a)).normalized;
                const float len = 0.026f; // same length as in SwitchToggle
                Vector3 screwPos = pivot + d * (len * 0.5f) + n * 0.001f + Vector3.up * 0.001f;
                toggle.Box(screwPos, new Vector3(0.004f, 0.004f, 0.004f), DecorPalette.Matte(new Color(0.08f, 0.08f, 0.09f)));
                toggles[i] = FixturePart.Hinge(sw.transform, "Toggle", toggle, pivot, Vector3.Cross(n, Vector3.up), ToggleTilt * 2f,
                    ShadowCastingMode.Off).WithSpeed(0.08f);
            }
            sw.SetToggles(toggles);
        }

        // A brass wall sconce with a frosted globe: the kitchen-diner's light, on the living room's circuit.
        private static void Sconce(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, float y)
        {
            Vector3 n = f.N(side), wall = Face(f, side, along, y);
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);
            m.Box(wall + n * 0.01f, f.Size(0.1f, 0.16f, 0.02f), brass);
            m.Rod(wall + n * 0.02f, wall + n * 0.13f + Vector3.up * 0.03f, 0.018f, brass);
            // subtle brass knob and optional screw on the sconce rod
            Vector3 knobPos = wall + n * 0.13f + Vector3.up * 0.03f;
            Surf knobMat = UnityEngine.Random.value < 0.3f ? DecorPalette.Metal(new Color(0.75f,0.60f,0.45f)) : brass;
            m.Cylinder(knobPos, 0.008f, 0.015f, 6, knobMat);
            // tiny screw detail (adds realism)
            Vector3 screwPos = knobPos + n * 0.005f + Vector3.up * 0.005f;
            m.Box(screwPos, new Vector3(0.004f,0.004f,0.004f), DecorPalette.Matte(new Color(0.08f,0.08f,0.09f)));
            Vector3 globe = wall + n * 0.15f + Vector3.up * 0.07f;
            m.Ellipsoid(globe, new Vector3(0.13f, 0.15f, 0.13f), DecorPalette.Matte(new Color(0.86f, 0.82f, 0.70f)), 8, 5);
            var glow = new RaidMesh();
            glow.Ellipsoid(globe, new Vector3(0.14f, 0.16f, 0.14f), DecorPalette.Glow(WarmGlow), 8, 5);
            float intensityFactor = LightVariation(globe);
            HomeLamp.Attach(piece.transform, globe, 5f, 1.6f * intensityFactor, true, glow, LightCircuits.Living);
        }

        // The store room's oval bulkhead light, high over the back window, on the store room's circuit.
        private static void Bulkhead(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, float y)
        {
            Vector3 n = f.N(side);
            var glow = new RaidMesh();
            Vector3 bulb = FurnitureBuilder.BulkheadLight(m, glow, Face(f, side, along, y), n);
            float intensityFactor = LightVariation(bulb);
            HomeLamp.Attach(piece.transform, bulb, CloneZConfig.StoreLightRange, CloneZConfig.StoreLightIntensity * intensityFactor, true, glow, LightCircuits.Store);
        }

        // The bathroom's vanity light, above the mirror cabinet, on the bathroom's circuit.
        private static void VanityLight(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, float y)
        {
            Vector3 n = f.N(side);
            var glow = new RaidMesh();
            Vector3 bulb = FurnitureBuilder.VanityLight(m, glow, Face(f, side, along, y), n, f.Along);
            float intensityFactor = LightVariation(bulb);
            HomeLamp.Attach(piece.transform, bulb, CloneZConfig.BathLightRange, CloneZConfig.BathLightIntensity * intensityFactor, true, glow, LightCircuits.Bath);
        }

        // A black carriage lantern beside the front door, on the porch circuit with a dusk sensor.
        private static void PorchLantern(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, float y)
        {
            Vector3 n = f.N(side), wall = Face(f, side, along, y);
            Surf black = DecorPalette.Metal(new Color(0.08f, 0.08f, 0.09f));
            m.Box(wall + n * 0.01f, f.Size(0.1f, 0.22f, 0.02f), black);
            m.Rod(wall + n * 0.02f, wall + n * 0.1f, 0.02f, black);
            Vector3 lamp = wall + n * 0.13f;
            m.Box(lamp + Vector3.up * 0.125f, new Vector3(0.17f, 0.04f, 0.17f), black);
            m.Box(lamp + Vector3.up * 0.16f, new Vector3(0.08f, 0.04f, 0.08f), black);
            m.Box(lamp - Vector3.up * 0.115f, new Vector3(0.13f, 0.03f, 0.13f), black);
            m.Box(lamp, new Vector3(0.12f, 0.2f, 0.12f), DecorPalette.Matte(new Color(0.72f, 0.64f, 0.44f)));
for (int i = 0; i < 4; i++)
                    m.Box(lamp + new Vector3(i % 2 == 0 ? -0.062f : 0.062f, 0f, i < 2 ? -0.062f : 0.062f), new Vector3(0.016f, 0.21f, 0.016f), black);
                // brass knob crowning the pole
                Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);
                m.Cylinder(lamp + Vector3.up * 0.225f, 0.015f, 0.02f, 6, brass);
                // four rivet caps on the black housing
                Vector3[] rivetPos = new[] {
                    lamp + new Vector3(-0.055f, 0.115f, -0.055f),
                    lamp + new Vector3( 0.055f, 0.115f, -0.055f),
                    lamp + new Vector3(-0.055f, 0.115f,  0.055f),
                    lamp + new Vector3( 0.055f, 0.115f,  0.055f),
                };
                foreach (var rp in rivetPos)
                    m.Box(rp, new Vector3(0.014f, 0.008f, 0.014f), black);
            var glow = new RaidMesh();
            glow.Box(lamp, new Vector3(0.128f, 0.206f, 0.128f), DecorPalette.Glow(WarmGlow));
            HomeLamp porch = HomeLamp.Attach(piece.transform, lamp, 6f, 1.8f, true, glow, LightCircuits.Porch, duskSensor: true);
            OutdoorLantern.Create(piece.transform, porch, lamp + n * 0.08f, wall, n);
        }

        // A small enamelled house-number plate.
        private static void HouseNumber(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, float y)
        {
            Vector3 n = f.N(side), at = Face(f, side, along, y);
            Surf s = DecorMaterials.Sign(HouseNo, new Color(0.86f, 0.85f, 0.80f), new Color(0.10f, 0.12f, 0.20f), out float aspect);
            float h = 0.1f, w = h * aspect;
            m.Box(at + n * 0.008f, f.Size(w + 0.03f, h + 0.03f, 0.016f), DecorPalette.Matte(new Color(0.10f, 0.12f, 0.20f)));
            m.Upright(at + n * 0.0175f, n, w, h, s, new Rect(0f, 0f, 1f, 1f));
            // tiny dark‑metal screw detail on the house‑number plate
            Vector3 screwPos = at + n * 0.018f + Vector3.up * 0.02f;
            m.Box(screwPos, new Vector3(0.004f, 0.004f, 0.004f), DecorPalette.Matte(new Color(0.08f, 0.08f, 0.09f)));
            OutdoorHouseNumber.Create(piece.transform, HouseNo, at + n * 0.04f, at, n);
        }

        // The house's breaker panel on the store room's west wall: the box drawn into the wall, its door (hinged on the
        // left as you face it) and the main breaker's handle as moving parts of the panel fixture.
        private static void Breakers(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, float y)
        {
            const float w = 0.32f, h = 0.42f, d = 0.09f;
            Vector3 n = f.N(side), back = Face(f, side, along, y), front = back + n * d;
            Vector3 right = Vector3.Cross(n, Vector3.up).normalized;   // as you face the panel
            Vector3 pivot = FurnitureBuilder.BreakerBox(m, back, n, w, h, d);
            BreakerPanel panel = BreakerPanel.Create(piece.transform, front + n * 0.05f, back, n);

            var door = new RaidMesh();
            FurnitureBuilder.BreakerDoor(door, front, n, right, w, h);
            Vector3 hinge = front - right * (w * 0.5f + 0.003f) + n * 0.006f;
            FixturePart doorPart = FixturePart.Hinge(panel.transform, "Door", door, hinge, Vector3.Cross(right, n), 105f, ShadowCastingMode.Off);

            var handle = new RaidMesh();
            FurnitureBuilder.BreakerHandle(handle, pivot, n, BreakerTilt);
            FixturePart handlePart = FixturePart.Hinge(panel.transform, "MainBreaker", handle, pivot, Vector3.Cross(n, Vector3.up), BreakerTilt * 2f,
                ShadowCastingMode.Off).WithSpeed(0.12f);
            panel.SetParts(doorPart, handlePart);
        }

        // The living room's battery wall clock: the case and dial drawn into the wall, the hands kept apart to turn.
        private static void Clock(BuildPiece piece, RaidMesh m, in Frame f, System.Random rng, int side, float along, float y)
        {
            // The old stopped clock drew its hands from two draws of the wall's stream; keep the stream as it was.
            rng.NextDouble();
            rng.NextDouble();
            Vector3 n = f.N(side), at = Face(f, side, along, y);
            FurnitureBuilder.ClockDial(m, at, n);
            ClockFixture clock = ClockFixture.Create(piece.transform, at + n * 0.06f, at, n);
            Transform hour = ClockHand(clock.transform, "HourHand", at, n, 0.075f, 0.012f, 0.0455f);
            Transform minute = ClockHand(clock.transform, "MinuteHand", at, n, 0.112f, 0.008f, 0.049f);
            clock.SetHands(hour, minute);
        }

        // A framed print, as crooked as the house came (the same tilt and frame the wall's stream always gave it).
        private static void Picture(BuildPiece piece, RaidMesh m, in Frame f, System.Random rng, int side, float along, float y,
            float w, float h, string description)
        {
            float roll = -4f + (float)rng.NextDouble() * 8f;
            Color frame = rng.NextDouble() < 0.5 ? new Color(0.16f, 0.12f, 0.08f) : new Color(0.55f, 0.55f, 0.52f);
            Vector3 n = f.N(side), at = Face(f, side, along, y);
            PictureFixture picture = PictureFixture.Create(piece, at + n * 0.04f, at, n, roll, description);
            var mesh = new RaidMesh();
            FurnitureBuilder.PictureFrame(mesh, at, n, w, h, frame, 2);
            GameObject go = mesh.Emit("Picture", picture.transform, at, ShadowCastingMode.On);
            picture.SetFrame(go.transform);
        }

        // Returns a subtle intensity variation (0.95‑1.05) derived from the world position of a lamp.
        // The same position always yields the same factor, without using the map's random stream.
        private static float LightVariation(Vector3 worldPos)
        {
            unchecked
            {
                long x = (long)Mathf.FloorToInt(worldPos.x * 73856093);
                long y = (long)Mathf.FloorToInt(worldPos.y * 19349663);
                long z = (long)Mathf.FloorToInt(worldPos.z * 83492791);
                long hash = x ^ y ^ z;
                // Map hash to [0,1]
                float t = (hash & 0xFFFF) / (float)0xFFFF;
                return 0.95f + t * 0.10f; // range 0.95‑1.05
            }
        }

        private static Transform ClockHand(Transform parent, string name, Vector3 centre, Vector3 n, float length, float width, float lift)
        {
            var mesh = new RaidMesh();
            FurnitureBuilder.ClockHand(mesh, centre, n, length, width, lift);
            GameObject go = mesh.Emit(name, parent, centre, ShadowCastingMode.Off);
            go.transform.rotation = Quaternion.identity;   // drawn in world axes, pointing at twelve
            return go.transform;
        }

        #endregion
    }
}
