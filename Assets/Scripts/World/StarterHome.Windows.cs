using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The starter home builds window fittings (sashes, curtains, blinds, etc.).
    internal static partial class StarterHome
    {
        #region Window fittings
        private const float SashWidth = WindowWidth - 0.05f;   // between the linings
        private const float SashDepth = 0.04f;
        private const float SashTrack = 0.024f;                // each sash's offset from the wall line (the lower one room-side)
        private const float SashLift = 0.48f;                  // how far the lower sash slides up
        private const float KitchenBlindBottom = 1.2f;         // the kitchen blind stops above the sink's backsplash
        private static readonly Color SashGlass = DecorPalette.ClearTint;

        // A working double-hung window in a finished window piece: the upper sash fixed in the outer track (drawn into
        // the wall), the lower sash in the inner track sliding up to open, with a sash lock on the meeting rails; on the
        // room side, curtains on a brass rod (drawn back, or drawn across) — or, in the kitchen, a valance with a roller
        // blind (rolled up, or pulled down). The HomeWindow fixture works them, and screens the opening while the
        // curtains are drawn so nothing sees in (or out).
        /// <summary>
        /// Constructs the full window assembly, adding sashes, lintel, brackets, curtains/blinds and interactive parts.
        /// </summary>
        static partial void WindowFittings(BuildPiece piece, RaidMesh m, Frame f, HomeRoom[] rooms, System.Random rng, bool kitchen)
        {
            // Determine which side of the window opens to the outside (negative) or interior (positive)
            int outSide = rooms[0] == null ? -1 : rooms[1] == null ? 1 : 0;
            // Derive the sign for the room‑side direction based on which side is the outside.
            int roomSide = outSide != 0 ? -outSide : 1;
            // Compute the vertical limits of the clear pane between the sill and head linings.
            float bottom = WindowSill + 0.03f, top = WindowHead - 0.024f;   // the clear opening between the sill and head linings
            // Mid‑point where the two sashes meet; used for positioning the lock and decorative elements.
            float meet = (bottom + top) * 0.5f;
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);

            // Create a parent GameObject to hold all window parts for easy hierarchy management.
            var root = new GameObject("Window");
            root.transform.SetParent(piece.transform, false);
            // Position the root at the window frame's centre with no rotation.
            root.transform.SetPositionAndRotation(f.P(f.Mid, 0f, 0f), Quaternion.identity);

            // The upper sash, fixed, and the keeper of the sash lock on its meeting rail.
            // Compute offsets for outer (wall‑side) and inner (room‑side) tracks based on window orientation.
            float outer = -roomSide * SashTrack, inner = roomSide * SashTrack;
            // Add the fixed upper sash and a decorative metal bar across the meeting rail.
            WindowSash(m, f, meet - 0.02f, top, outer, false, roomSide);
            // Add a thin brass decorative strip at the top of the upper sash.
            m.Box(f.P(f.Mid, meet + 0.034f, outer), f.Size(0.04f, 0.008f, 0.03f), brass);

            // The lower sash, sliding up in the inner track; the lock's lever rides on it, turned onto the keeper or away.
            // Build the sliding lower sash which can be raised via the latch lever.
            var lower = new RaidMesh();
            // Position and construct the lower sash geometry.
            WindowSash(lower, f, bottom, meet + 0.02f, inner, true, roomSide);
            // Add a decorative metal bar at the bottom of the lower sash.
            lower.Box(f.P(f.Mid, meet + 0.024f, inner), f.Size(0.05f, 0.008f, 0.032f), brass);
            // exterior stone sill – period‑correct overhang below the window opening
            // Create the stone sill below the opening – a period‑correct overhang.
            float sillDepth = 0.06f;
            // Height of the stone sill element.
            float sillHeight = 0.020f;
            // Y position centres the sill half its height below the sill line.
            float sillY = WindowSill - sillHeight * 0.5f;
            // outer (wall‑side) track already stored in variable 'outer'
            // Add the concrete stone sill box.
            m.Box(f.P(f.Mid, sillY, outer - sillDepth * 0.5f), f.Size(WindowWidth, sillHeight, sillDepth), DecorMaterials.Concrete);
            // Wood material (pine) used for the apron strip and later lintel.
            Surf wood = DecorPalette.Matte(FurnitureBuilder.Pine); // wood for the strip below
            // wooden apron strip below the stone sill (period‑correct exterior trim)
            // Add the wooden apron strip below the stone sill to represent period‑correct exterior trim.
            float apronHeight = 0.008f;
            // Depth of the wooden apron strip.
            float apronDepth = 0.015f;
            // Compute Y position for the apron strip, just below the stone sill.
            float apronY = sillY - sillHeight*0.5f - apronHeight*0.5f;
            // Add the wooden apron box using the pine material.
            m.Box(f.P(f.Mid, apronY, outer - sillDepth*0.5f - apronDepth*0.5f), f.Size(WindowWidth, apronHeight, apronDepth), wood);
            // rust‑tinted metal surface for brackets
            // rust‑tinted metal used for the exterior brackets (matches the material used elsewhere)
Surf rustMetal = DecorPalette.Matte(new Color(0.55f, 0.35f, 0.30f));
            // stone coping cap
            float capHeight = 0.005f;
            float capDepth = sillDepth + 0.01f;
            m.Box(f.P(f.Mid, sillY + sillHeight * 0.5f + capHeight * 0.5f, outer - sillDepth * 0.5f), f.Size(WindowWidth + 0.02f, capHeight, capDepth), DecorMaterials.Concrete);
            // recessed drip-groove
            float grooveHeight = 0.002f;
            m.Box(f.P(f.Mid, sillY + sillHeight * 0.5f - capHeight - grooveHeight * 0.5f, outer - sillDepth * 0.5f), f.Size(WindowWidth + 0.02f, grooveHeight, sillDepth), DecorPalette.Matte(FurnitureBuilder.Pine));

            // wooden lintel above the window head
            float lintelHeight = 0.015f; // metres
            float lintelDepth = 0.03f;
            float lintelY = WindowHead + lintelHeight * 0.5f;
            m.Box(f.P(f.Mid, lintelY, outer - sillDepth * 0.5f), f.Size(WindowWidth, lintelHeight, lintelDepth), wood);

            // dentil blocks along the front of the lintel
            int dentilCount = Mathf.Max(2, Mathf.FloorToInt(WindowWidth / 0.025f));
            float dentilSpacing = WindowWidth / dentilCount;
            float dentilWidth = 0.004f;
            float dentilDepth = lintelDepth;
            for (int i = 0; i < dentilCount; i++)
            {
                float offsetX = -WindowWidth * 0.5f + dentilSpacing * (i + 0.5f);
                float dentilHeight = 0.006f + 0.001f * ((float)Random.value - 0.5f);
                float dentilY = lintelY + lintelHeight * 0.5f + dentilHeight * 0.5f;
                m.Box(f.P(offsetX, dentilY, outer - sillDepth * 0.5f), f.Size(dentilWidth, dentilHeight, dentilDepth), wood);
            }

            // decorative iron strap under the lintel (period‑correct hardware)
            float strapHeight = 0.004f;
            float strapDepth = 0.008f;
            float strapY = lintelY - lintelHeight * 0.5f - strapHeight * 0.5f;
            m.Box(f.P(f.Mid, strapY, outer - sillDepth * 0.5f - lintelDepth * 0.5f - strapDepth * 0.5f),
                f.Size(WindowWidth, strapHeight, strapDepth), rustMetal);
            // irregular stone bumps
            int bumpCount = 3;
            for (int i = 0; i < bumpCount; i++)
            {
                float offsetX = (float)(Random.value * (WindowWidth - 0.04) - (WindowWidth - 0.04) * 0.5);
                float bumpSize = 0.01f + (float)Random.value * 0.005f;
                m.Box(f.P(f.Mid + offsetX, sillY, outer - sillDepth * 0.5f + bumpSize * 0.5f), f.Size(bumpSize, bumpSize, bumpSize), DecorMaterials.Concrete);
            }
            // brass strike-plate on latch lever
            float plateW = 0.04f, plateH = 0.02f, plateD = 0.005f;
            Vector3 latchCenter = f.P(f.Mid, meet + 0.042f, 0f);
            m.Box(f.P(latchCenter.x, latchCenter.y, -0.01f), f.Size(plateW, plateH, plateD), brass);

            float w = SashWidth;
            float s0 = f.Mid - w * 0.5f;
            float s1 = f.Mid + w * 0.5f;
            // left bracket
            float bracketW = 0.02f, bracketH = 0.015f, bracketD = 0.005f;
            float bracketZ = outer - sillDepth*0.5f - apronDepth*0.5f; // same depth as apron front
            m.Box(f.P(s0 + 0.0225f, apronY, bracketZ), f.Size(bracketW, bracketH, bracketD), rustMetal);
            // right bracket
            m.Box(f.P(s1 - 0.0225f, apronY, bracketZ), f.Size(bracketW, bracketH, bracketD), rustMetal);

            // The shutters are the louvred pair standing open beside the window (StarterHome.Shell's Shutters): nothing hangs
            // across the glass outside, so it's seen through.

            // nail‑head grid on the wooden apron (3 × 2)
            float nailRadius = 0.0015f, nailHeight = 0.001f;
            int nailCols = 3, nailRows = 2;
            float nailSpacingX = (WindowWidth - 0.04f) / (nailCols - 1);
            float nailSpacingZ = apronDepth * 0.6f;
            for (int ix = 0; ix < nailCols; ix++)
            {
                for (int iz = 0; iz < nailRows; iz++)
                {
                    float nx = f.Mid - (WindowWidth * 0.5f) + 0.02f + ix * nailSpacingX;
                    float nz = bracketZ - apronDepth * 0.5f + nailSpacingZ / 2f + iz * nailSpacingZ;
                    m.CylinderX(f.P(nx, apronY + apronHeight * 0.5f + nailHeight * 0.5f, nz),
                        nailRadius, nailHeight, 6, brass);
                }
            }

            FixturePart sash = FixturePart.Slide(root.transform, "LowerSash", lower, f.P(f.Mid, bottom, inner), Vector3.up * SashLift)
                .WithSpeed(CloneZConfig.WindowSashSeconds);
            Renderer latchShut = WindowLever(sash.transform, "LatchShut", f.P(f.Mid, meet + 0.042f, 0f), f.Size(0.02f, 0.008f, 0.06f), brass);
            Renderer latchOpen = WindowLever(sash.transform, "LatchOpen", f.P(f.Mid + 0.03f, meet + 0.032f, inner), f.Size(0.06f, 0.008f, 0.02f), brass);

            // Curtains (the kitchen: a valance and a roller blind) on each room side.
            var open = new RaidMesh();
            var drawn = new RaidMesh();
            for (int side = -1; side <= 1; side += 2)
            {
                HomeRoom room = rooms[(side + 1) / 2];
                if (room == null) continue;
                if (kitchen)
                {
                    WindowValance(m, f, side, room.Curtain);
                    WindowBlind(open, drawn, f, side, Color.Lerp(room.Curtain, FurnitureBuilder.Cream, 0.55f));
                }
                else WindowCurtains(m, open, drawn, f, side, room.Curtain);
            }
            Vector3 origin = f.P(f.Mid, 0f, 0f);
            Renderer curtainsOpen = EmitWindowPart(open, "CurtainsOpen", root.transform, origin);
            Renderer curtainsDrawn = EmitWindowPart(drawn, "CurtainsDrawn", root.transform, origin);
            DecorPalette.Apply();

            var setup = new HomeWindow.Setup
            {
                Name = WindowName(piece, f, rooms, kitchen),
                Sash = sash,
                CurtainsOpen = curtainsOpen,
                CurtainsDrawn = curtainsDrawn,
                LatchShut = latchShut,
                LatchOpen = latchOpen,
                Screen = new Bounds(f.P(f.Mid, (bottom + top) * 0.5f, 0f), f.Size(SashWidth, top - bottom, 0.02f)),
                Point = f.P(f.Mid, (WindowSill + WindowHead) * 0.5f, 0f),
                Normal = f.N(1),
                Along = f.Along,
                OutSide = outSide,
                Blind = kitchen,
            };
            HomeWindow.Attach(root, piece, setup);
        }

        // "Kitchen window", "Store room window", "Bathroom window", else the house wall it's in: "Front window".
        private static string WindowName(BuildPiece piece, in Frame f, HomeRoom[] rooms, bool kitchen)
        {
            if (kitchen) return "Kitchen window";
            HomeRoom room = rooms[0] ?? rooms[1];
            if (room == Store) return "Store room window";
            if (room == Bath) return "Bathroom window";
            if (f.AlongX) return Mathf.Abs(f.Coord - Outline.yMin) < 0.05f ? "Front window" : Mathf.Abs(f.Coord - Outline.yMax) < 0.05f ? "Back window" : "Window";
            return Mathf.Abs(f.Coord - Outline.xMin) < 0.05f ? "West window" : Mathf.Abs(f.Coord - Outline.xMax) < 0.05f ? "East window" : "Window";
        }

        // One sash between `y0` and `y1`, `across` from the wall line: stiles, rails (the meeting rail deeper), a glazing
        // bar down the middle and the glass. The lower sash gets a pair of brass lifts on its bottom rail.
        private static void WindowSash(RaidMesh m, in Frame f, float y0, float y1, float across, bool lower, int roomSide)
        {
            Surf frame = DecorPalette.Matte(TrimWhite);
            float w = SashWidth, h = y1 - y0, cy = (y0 + y1) * 0.5f;
            float s0 = f.Mid - w * 0.5f, s1 = f.Mid + w * 0.5f;
            float topRail = lower ? 0.05f : 0.045f, bottomRail = lower ? 0.06f : 0.05f;
            m.Box(f.P(s0 + 0.0225f, cy, across), f.Size(0.045f, h, SashDepth), frame);
            m.Box(f.P(s1 - 0.0225f, cy, across), f.Size(0.045f, h, SashDepth), frame);
            m.Box(f.P(f.Mid, y1 - topRail * 0.5f, across), f.Size(w - 0.09f, topRail, lower ? SashDepth + 0.005f : SashDepth), frame);
            m.Box(f.P(f.Mid, y0 + bottomRail * 0.5f, across), f.Size(w - 0.09f, bottomRail, lower ? SashDepth : SashDepth + 0.005f), frame);
            // Add wooden pegs at the four corners of the sash (period‑correct muntin detail)
            float pegSize = 0.008f;
            // top‑left
            m.Box(f.P(s0 + 0.0225f, y1 - topRail * 0.5f, across), f.Size(pegSize, pegSize, SashDepth), frame);
            // top‑right
            m.Box(f.P(s1 - 0.0225f, y1 - topRail * 0.5f, across), f.Size(pegSize, pegSize, SashDepth), frame);
            // bottom‑left
            m.Box(f.P(s0 + 0.0225f, y0 + bottomRail * 0.5f, across), f.Size(pegSize, pegSize, SashDepth), frame);
            // bottom‑right
            m.Box(f.P(s1 - 0.0225f, y0 + bottomRail * 0.5f, across), f.Size(pegSize, pegSize, SashDepth), frame);
            float glass = h - topRail - bottomRail, gy = y0 + bottomRail + glass * 0.5f;
            // slight random variation in glass tint for each window
            float tintFactor = 0.9f + 0.2f * Random.value; // 0.9 – 1.1 range
            Color glassTint = new Color(SashGlass.r * tintFactor, SashGlass.g * tintFactor, SashGlass.b * tintFactor, SashGlass.a);
            m.Box(f.P(f.Mid, gy, across), f.Size(0.03f, glass, 0.03f), frame);
            m.Box(f.P(f.Mid, gy, across), f.Size(w - 0.09f, glass, 0.006f), DecorPalette.WindowGlass(glassTint));
            // period‑correct leaded‑glass pattern
            Surf lead = DecorPalette.Metal(new Color(0.55f, 0.55f, 0.48f)); // dark‑bronze, period‑correct for leaded glass
            int vertStrips = 2;
            float stripWidth = 0.002f;
            for (int i = 0; i < vertStrips; i++)
            {
                float offset = ((float)i / (vertStrips - 1) - 0.5f) * (w - 0.09f) + 0.001f * ((float)Random.value - 0.5f);
                m.Box(f.P(f.Mid + offset, gy, across), f.Size(stripWidth, glass, 0.006f), lead);
            }
            float horizStripWidth = 0.002f;
            float horizY = gy + 0.001f * ((float)Random.value - 0.5f);
            m.Box(f.P(f.Mid, horizY, across), f.Size(w - 0.09f, horizStripWidth, 0.006f), lead);
            // Add decorative iron grille on the lower sash (period‑correct hardware)
            if (lower)
            {
                Surf iron = DecorPalette.Metal(FurnitureBuilder.Brass);
                int verticalBars = 3;
                int horizontalBars = 2;
                float barThickness = 0.004f;
                // vertical bars
                for (int i = 0; i < verticalBars; i++)
                {
                    float x = s0 + 0.0225f + (i + 1) * (w - 0.045f) / (verticalBars + 1);
                    m.Box(f.P(x, gy, across), f.Size(barThickness, glass, 0.006f), iron);
                }
                // horizontal bars
                for (int j = 0; j < horizontalBars; j++)
                {
                    float y = y0 + bottomRail + (j + 1) * (glass) / (horizontalBars + 1);
                    m.Box(f.P(f.Mid, y, across), f.Size(w - 0.09f, barThickness, 0.006f), iron);
                }
            }
            // Add period‑correct wooden muntin grid to the glazing
            Surf muntin = DecorPalette.Matte(FurnitureBuilder.Pine);
            // vertical muntin strip (≈3 mm wide, full glass height)
            m.Box(f.P(f.Mid, gy, across), f.Size(0.003f, glass, 0.006f), muntin);
            // horizontal muntin strip (≈3 mm tall, spans glass width)
            m.Box(f.P(f.Mid, gy, across), f.Size(w - 0.09f, 0.003f, 0.006f), muntin);
            // Add wooden bead trim along top and bottom rails
            Surf bead = DecorPalette.Matte(FurnitureBuilder.Pine);
            float beadThickness = 0.003f;
            // top rail bead
            m.Box(f.P(f.Mid, y1 - topRail * 0.5f, across), f.Size(w - 0.09f, beadThickness, lower ? SashDepth + 0.005f : SashDepth), bead);
            // bottom rail bead
            m.Box(f.P(f.Mid, y0 + bottomRail * 0.5f, across), f.Size(w - 0.09f, beadThickness, lower ? SashDepth : SashDepth + 0.005f), bead);
            // thin inner‑face wooden bead round the glass (period‑correct finish): a strip along each edge, the pane left clear
            float trimThickness = 0.002f, beadWidth = 0.012f;
            // position on inner face of sash
            // inner face is opposite the room side; subtract to move the trim inward
float innerOffset = across - roomSide * (SashDepth * 0.5f + trimThickness * 0.5f);
            m.Box(f.P(f.Mid, y0 + bottomRail + beadWidth * 0.5f, innerOffset), f.Size(w - 0.09f, beadWidth, trimThickness), bead);
            m.Box(f.P(f.Mid, y1 - topRail - beadWidth * 0.5f, innerOffset), f.Size(w - 0.09f, beadWidth, trimThickness), bead);
            for (int e = -1; e <= 1; e += 2)
                m.Box(f.P(f.Mid + e * ((w - 0.09f) * 0.5f - beadWidth * 0.5f), gy, innerOffset), f.Size(beadWidth, glass, trimThickness), bead);
// material for cap rivets
Surf brassCap = DecorPalette.Metal(FurnitureBuilder.Brass);
// rust‑tinted metal used for the exterior brackets (matches the material used elsewhere)
Surf rustMetal = DecorPalette.Matte(new Color(0.55f, 0.35f, 0.30f));
            // three subtle rivets on the coping cap (evenly spaced)
            float capRivetSize = 0.004f;
            float capRivetDepth = 0.003f;
            // use local copies to ensure variables are in scope
            float capHeight_local = 0.005f; // duplicated for scope
            float sillDepth_local = 0.06f; // duplicated for scope
            float capY = WindowSill + capHeight_local * 0.5f; // centre of coping cap
            float capZ = -roomSide * SashTrack - sillDepth_local * 0.5f; // centre of cap depth
            for (int i = -1; i <= 1; i += 2)
            {
                float rx = f.Mid + i * (WindowWidth * 0.33f);
                m.Box(f.P(rx, capY, capZ), f.Size(capRivetSize, capRivetSize, capRivetDepth), brassCap);
            }

            // brackets using rustMetal
            for (int e = -1; e <= 1; e += 2)
                m.Box(f.P(f.Mid + e * (w * 0.5f - 0.0225f), y1 - topRail * 0.5f, across), f.Size(0.015f, 0.015f, 0.01f), rustMetal);
            // top‑rail brass rivets – subtle hardware detail
            float rivetSize = 0.006f;
            float rivetDepth = 0.004f;
            // left rivet
            m.Box(f.P(s0 + 0.03f, y1 - topRail * 0.5f, across), f.Size(rivetSize, rivetSize, rivetDepth), brassCap);
            // center rivet
            m.Box(f.P(f.Mid, y1 - topRail * 0.5f, across), f.Size(rivetSize, rivetSize, rivetDepth), brassCap);
            // right rivet
            m.Box(f.P(s1 - 0.03f, y1 - topRail * 0.5f, across), f.Size(rivetSize, rivetSize, rivetDepth), brassCap);
            if (!lower) return;
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);
            for (int e = -1; e <= 1; e += 2)
                m.Box(f.P(f.Mid + e * 0.3f, y0 + 0.03f, across + roomSide * (SashDepth * 0.5f + 0.008f)), f.Size(0.05f, 0.015f, 0.016f), brass);
            // Add brass pull‑knobs at inner ends of the bottom rail
            for (int e = -1; e <= 1; e += 2)
                m.Box(f.P(f.Mid + e * 0.3f, y0 + 0.03f, across + roomSide * (SashDepth * 0.5f + 0.008f)), f.Size(0.02f, 0.005f, 0.016f), brass);
        }

        // A brass rod with finials above the casing (fixed, drawn into the wall), and the curtains in their two states:
        // gathered panels drawn back to the sides with tie-backs, and the panels drawn across in shallow folds.
        private static void WindowCurtains(RaidMesh fixedMesh, RaidMesh open, RaidMesh drawn, in Frame f, int side, Color fabric)
        {
            float rodY = WindowHead + 0.13f, rodOff = side * (Half + 0.075f);
            Surf brass = DecorPalette.Metal(FurnitureBuilder.Brass);
            fixedMesh.Box(f.P(f.Mid, rodY, rodOff), f.Size(WindowWidth + 0.5f, 0.024f, 0.024f), brass);
            for (int e = -1; e <= 1; e += 2)
                fixedMesh.Ellipsoid(f.P(f.Mid + e * (WindowWidth * 0.5f + 0.26f), rodY, rodOff), new Vector3(0.045f, 0.045f, 0.045f), brass, 6, 4);

            float top = rodY - 0.015f, bottom = WindowSill + 0.06f, h = top - bottom;
            for (int e = -1; e <= 1; e += 2)
            {
                float c = f.Mid + e * (WindowWidth * 0.5f + 0.08f);
                for (int k = 0; k < 3; k++)
                {
                    float fc = c + (k - 1) * 0.085f;
                    float depth = side * (Half + 0.055f + (k % 2) * 0.022f);
                    open.Box(f.P(fc, bottom + h * 0.5f, depth), f.Size(0.1f, h, 0.028f), DecorPalette.Matte(fabric * (k % 2 == 0 ? 1f : 0.84f)));
                }
                // A tie-back gathering each panel a little below the middle.
                open.Box(f.P(c, bottom + h * 0.4f, side * (Half + 0.09f)), f.Size(0.3f, 0.04f, 0.012f), DecorPalette.Matte(fabric * 0.7f));
            }

            const int folds = 14;
            float span = WindowWidth + 0.4f, fw = span / folds;
            for (int k = 0; k < folds; k++)
            {
                float along = f.Mid - span * 0.5f + (k + 0.5f) * fw;
                float depth = side * (Half + 0.058f + (k % 2) * 0.018f);
                drawn.Box(f.P(along, bottom + h * 0.5f, depth), f.Size(fw + 0.012f, h, 0.026f), DecorPalette.Matte(fabric * (k % 2 == 0 ? 1f : 0.86f)));
            }
        }

        // A short gathered valance across the top of the window.
        private static void WindowValance(RaidMesh m, in Frame f, int side, Color fabric)
        {
            float y = WindowHead - 0.05f;
            for (int k = 0; k < 9; k++)
            {
                float along = f.Mid - WindowWidth * 0.5f - 0.1f + (k + 0.5f) * (WindowWidth + 0.2f) / 9f;
                float depth = side * (Half + 0.05f + (k % 2) * 0.02f);
                m.Box(f.P(along, y, depth), f.Size((WindowWidth + 0.2f) / 9f + 0.01f, 0.3f, 0.025f), DecorPalette.Matte(fabric * (k % 2 == 0 ? 1f : 0.84f)));
            }
        }

        // A roller blind tucked under the valance: rolled up (the roll, its bottom bar and pull cord peeping out under the
        // valance) or pulled down over the glazing, stopping above the sink's backsplash.
        private static void WindowBlind(RaidMesh up, RaidMesh down, in Frame f, int side, Color fabric)
        {
            float rollY = WindowHead - 0.19f, across = side * (Half + 0.03f), w = WindowWidth + 0.06f;
            Surf cloth = DecorPalette.Matte(fabric), bar = DecorPalette.Matte(FurnitureBuilder.Pine), cord = DecorPalette.Matte(fabric * 0.6f);
            Vector3 along = f.Along;
            foreach (RaidMesh m in new[] { up, down })
                m.CylinderX(f.P(f.Mid, rollY, across), 0.032f, w, 10, cloth, Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg - 90f);

            // Rolled up: the bar hangs just under the roll.
            float upBar = rollY - 0.05f;
            up.Box(f.P(f.Mid, upBar, across), f.Size(w + 0.02f, 0.022f, 0.018f), bar);
            up.Box(f.P(f.Mid, upBar - 0.08f, across), f.Size(0.006f, 0.15f, 0.006f), cord);
            up.Ellipsoid(f.P(f.Mid, upBar - 0.165f, across), new Vector3(0.022f, 0.03f, 0.022f), bar, 6, 4);

            // Pulled down: the cloth over the glazing, the bar at its foot.
            float downBar = KitchenBlindBottom;
            down.Box(f.P(f.Mid, (rollY + downBar) * 0.5f, across), f.Size(w, rollY - downBar, 0.006f), cloth);
            down.Box(f.P(f.Mid, downBar, across), f.Size(w + 0.02f, 0.022f, 0.018f), bar);
            down.Box(f.P(f.Mid, downBar - 0.08f, across), f.Size(0.006f, 0.15f, 0.006f), cord);
            down.Ellipsoid(f.P(f.Mid, downBar - 0.165f, across), new Vector3(0.022f, 0.03f, 0.022f), bar, 6, 4);
        }

        // A small separately drawn box (a sash lock's lever) under `parent`, so it can be shown or hidden on its own.
        private static Renderer WindowLever(Transform parent, string name, Vector3 centre, Vector3 size, in Surf s)
        {
            var m = new RaidMesh();
                        // back‑plate wood (period‑correct)
            Surf wood = DecorPalette.Matte(FurnitureBuilder.Pine);
            float plateDepth = 0.002f; // metres
            Vector3 platePos = centre + new Vector3(0f, 0f, -size.z * 0.5f - plateDepth * 0.5f);
            Vector3 plateSize = new Vector3(size.x + 0.01f, size.y + 0.01f, plateDepth);
            m.Box(platePos, plateSize, wood);
            // brass key‑hole slot centered on the back‑plate
            Vector3 keyPos = centre + new Vector3(0f, 0f, -size.z * 0.5f - plateDepth - 0.001f);
            Vector3 keySize = new Vector3(0.004f, 0.008f, 0.002f);
            m.Box(keyPos, keySize, s);
            // tiny brass hinge pin at the lever’s inner pivot end
            Vector3 hingePos = centre + new Vector3(-size.x * 0.5f, 0f, 0f);
            m.CylinderX(hingePos, 0.002f, 0.004f, 6, s);
            // small brass knob at the end of the lever for tactile detail
            float knobRadius = 0.008f;
            // wooden material for back‑plate already defined above
            Vector3 knobPos = centre + new Vector3(size.x * 0.5f + knobRadius, 0f, 0f);
            m.CylinderX(knobPos, knobRadius, 0.005f, 8, DecorPalette.Metal(FurnitureBuilder.Brass));
            return EmitWindowPart(m, name, parent, centre);
        }

        // Emits world-axis geometry as its own object under `parent` (shadows off for the little parts), returning its renderer.
        private static Renderer EmitWindowPart(RaidMesh m, string name, Transform parent, Vector3 origin)
        {
            GameObject go = m.Emit(name, parent, origin, ShadowCastingMode.On);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;
            return go.GetComponent<Renderer>();
        }
        #endregion
    }
}
