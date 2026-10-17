using UnityEngine;

namespace CloneZ
{
    internal static partial class StarterHome
    {
        #region Kitchen wall fittings
        private const float KitchenWallCupboardDoorAngle = 100f;

        // The kitchen's wall cupboards over the worktop on the west wall.
        static partial void KitchenWallFixtures(BuildPiece piece, RaidMesh m, Frame f, System.Random rng)
        {
            if (Is(piece, BuildPieceType.Wall, 1f, 4f))
                KitchenWallCupboards(piece, m, f, 1, 4.37f, rng);
        }

        // Wall cupboards over the kitchen worktop, in the same paint as the units below. The carcass, its shelf and the
        // crockery put away inside never move, so they're drawn into the wall's own mesh (and vanish with it when the
        // camera cuts the wall away); the two doors hang under the piece as moving parts, and a storage fixture behind
        // them holds whatever's kept there. Its sight line is its own: from the far side, the wall blocks it.
        private static void KitchenWallCupboards(BuildPiece piece, RaidMesh m, in Frame f, int side, float along, System.Random rng)
        {
            Vector3 n = f.N(side);
            float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            Vector3 origin = Face(f, side, along, FloorTop);
            FurnitureBuilder.KitchenTone tone = FurnitureBuilder.HomeKitchen;
            m.SetFrame(origin, yaw);
            FurnitureBuilder.WallCupboardBody(m, rng, tone);
            m.ResetFrame();

            // The doors hang on the cupboard's outer ends and meet in the middle.
            var kf = new KitchenFrame(origin, yaw);
            const float h = FurnitureBuilder.WallCupLength * 0.5f, front = FurnitureBuilder.WallCupDepth + 0.02f;
            const float y0 = FurnitureBuilder.WallCupY0, y1 = FurnitureBuilder.WallCupY1;
            FixturePart left = KitchenParts.Door(piece.transform, "WallCupboardDoorL", kf, d => FurnitureBuilder.WallCupboardDoor(d, tone, true),
                new Vector3(-h + 0.002f, y0, front), Vector3.right, Vector3.forward, KitchenWallCupboardDoorAngle);
            FixturePart right = KitchenParts.Door(piece.transform, "WallCupboardDoorR", kf, d => FurnitureBuilder.WallCupboardDoor(d, tone, false),
                new Vector3(h - 0.002f, y0, front), Vector3.left, Vector3.forward, KitchenWallCupboardDoorAngle);
            // A wall the survivor raised in this slot gets the cupboard too, but nothing in it: only the house as it came is stocked.
            StorageFixture.Create(piece.transform, "Wall cupboard", Interactable.KeyFor("wallcupboard", origin),
                kf.P(0f, (y0 + y1) * 0.5f, front + 0.01f), new[] { left, right }, null, rng.Next(), new StorageOptions
                {
                    LootType = ContainerType.Pantry,
                    StartEmpty = !piece.Prebuilt,
                    EmptyChance = CloneZConfig.WallCupboardEmptyChance,
                    MaxStacks = CloneZConfig.WallCupboardMaxStacks,
                });
        }
        #endregion
    }
}