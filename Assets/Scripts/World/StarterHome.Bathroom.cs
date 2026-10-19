using UnityEngine;

namespace CloneZ
{
    // The bathroom's wall fittings: the mirror cabinet over the basin on the back wall and the towel rail over the toilet on the east wall (the vanity light above the cabinet and the switch by the door are the lighting's).
    internal static partial class StarterHome
    {
        #region Bathroom wall fittings
        // The bathroom's wall fittings: the mirror cabinet over the basin on the back wall, and the towel rail over the
        // toilet on the east wall. (The vanity light above the cabinet and the switch by the door are the lighting's.)
        private const float BathCabinetW = 0.50f, BathCabinetH = 0.60f, BathCabinetD = 0.14f;
        private const float BathCabinetDoorAngle = 100f;
        private const float BathRailLength = 0.48f, BathTowelWidth = 0.40f;
        private const float BathTowelDamp = 0.72f;   // how much darker the towel hangs while it's damp

        private static readonly Color[] BathTowels =
        {
            new Color(0.46f, 0.60f, 0.74f), new Color(0.86f, 0.60f, 0.50f), new Color(0.56f, 0.68f, 0.52f), new Color(0.88f, 0.84f, 0.72f),
        };

        static partial void BathroomWallFixtures(BuildPiece piece, RaidMesh m, Frame f, System.Random rng)
        {
            if (Is(piece, BuildPieceType.Wall, 6f, 9f)) BathCabinet(piece, m, f, rng, -1, 5.55f, 1.50f);
            else if (Is(piece, BuildPieceType.Wall, 7f, 8f)) BathTowelRail(piece, m, f, rng, -1, 7.55f, 1.42f);
        }

        // The mirror cabinet: its carcass drawn into the wall, its mirrored door hinged on the right as you face it (so it
        // swings open clear of the bathroom door's sweep), and its storage — a lean medicine cabinet's worth at most, and
        // nothing at all in a cabinet the survivor's rebuilt wall brought back.
        private static void BathCabinet(BuildPiece piece, RaidMesh m, in Frame f, System.Random rng, int side, float along, float y)
        {
            Vector3 n = f.N(side), back = Face(f, side, along, y), front = back + n * BathCabinetD;
            Vector3 right = Vector3.Cross(n, Vector3.up).normalized;   // as you face it
            FurnitureBuilder.MirrorCabinetBody(m, back, n, BathCabinetW, BathCabinetH, BathCabinetD);
            var door = new RaidMesh();
            FurnitureBuilder.MirrorCabinetDoor(door, front, n, right, BathCabinetW, BathCabinetH);
            FixturePart doorPart = FixturePart.Hinge(piece.transform, "MirrorDoor", door, front + right * (BathCabinetW * 0.5f),
                Vector3.Cross(n, right), BathCabinetDoorAngle);
            MirrorCabinet.Create(piece.transform, front + n * 0.03f, doorPart, rng.Next(), piece.Prebuilt);
        }

        // The towel rail: the rail drawn into the wall, and the towel over its bar as a dry look and a darker damp one,
        // on a pivot the rail tilts when the towel's slung back crooked.
        private static void BathTowelRail(BuildPiece piece, RaidMesh m, in Frame f, System.Random rng, int side, float along, float y)
        {
            Vector3 n = f.N(side), at = Face(f, side, along, y);
            Vector3 bar = FurnitureBuilder.TowelRailBar(m, at, n, f.Along, BathRailLength);
            Color dryColour = BathTowels[rng.Next(BathTowels.Length)];
            Color dampColour = new Color(dryColour.r * BathTowelDamp, dryColour.g * BathTowelDamp, dryColour.b * BathTowelDamp);
            TowelRail rail = TowelRail.Create(piece.transform, bar + n * 0.07f - Vector3.up * 0.12f, at, n);

            Transform pivot = new GameObject("Towel").transform;
            pivot.SetParent(rail.transform, false);
            pivot.SetPositionAndRotation(bar, Quaternion.identity);
            var dry = new RaidMesh();
            FurnitureBuilder.TowelOnRail(dry, bar, n, f.Along, BathTowelWidth, dryColour);
            var damp = new RaidMesh();
            FurnitureBuilder.TowelOnRail(damp, bar, n, f.Along, BathTowelWidth, dampColour);
            rail.SetTowel(pivot,
                BathroomFixtures.Emit(dry, "TowelDry", pivot, bar, true, true),
                BathroomFixtures.Emit(damp, "TowelDamp", pivot, bar, true, false));
        }
        #endregion
    }
}
