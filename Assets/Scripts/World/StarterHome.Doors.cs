using UnityEngine;

namespace CloneZ
{
    // The home's doors in their finished doorways: a pair of panelled leaves in the wide gaps (the front door red, the others oak), or — in the bathroom's doorway — a single painted leaf beside a fixed panel finished like the walls.
    internal static partial class StarterHome
    {
        #region Doors
        private static readonly Color BathDoorPaint = new Color(0.86f, 0.85f, 0.80f);
        private const float DoorLeafInset = 0.035f;   // a shut leaf's hinge line, in from the face it swings toward
        private const float DoorLeafClear = 0.03f;    // the hinges' distance in from the jambs (an open leaf clears the lining)
        private const float BathDoorSpan = 1.0f;      // the bathroom's single leaf; a fixed panel fills the rest of the gap
        private const float DoorHandleHeight = 1.05f;

        // The doors hung in a finished doorway: a pair of panelled leaves filling the wide gap (the front door red, the
        // others oak), or — in the bathroom's doorway — a single painted leaf beside a fixed panel finished like the walls.
        // The leaves are drawn shut and swing a quarter turn into the room (the inside, for the front door); the HomeDoor
        // fixture opens, shuts and locks them and makes a shut door solid to the survivor, the dead and their eyes.
        static partial void DoorFittings(BuildPiece piece, RaidMesh m, Frame f, HomeRoom[] rooms, System.Random rng)
        {
            bool outside = rooms[0] == null || rooms[1] == null;
            int swing = rooms[1] != null ? 1 : -1;               // into the room on the +1 side (the inside, for an outer door)
            bool bath = Is(piece, BuildPieceType.Doorway, 5f, 8f);
            float gap = DoorGap, o0 = f.Mid - gap * 0.5f, o1 = f.Mid + gap * 0.5f;
            float end = bath ? o0 + BathDoorSpan : o1;            // where the doorway proper ends
            float mid = (o0 + end) * 0.5f, span = end - o0;

            var root = new GameObject("Door");
            root.transform.SetParent(piece.transform, false);
            root.transform.SetPositionAndRotation(f.P(mid, 0f, 0f), Quaternion.identity);

            Color wood = outside ? FrontDoorRed : bath ? BathDoorPaint : FurnitureBuilder.Oak;
            float y0 = FloorTop + 0.016f, leafH = DoorTop - FloorTop - 0.05f;
            float across = swing * (Half - DoorLeafInset);
            Vector3 into = f.N(swing);
            FixturePart[] leaves;
            if (bath)
            {
                leaves = new[] { DoorLeafPart(root.transform, f, rng, o0 + DoorLeafClear, 1, span - DoorLeafClear - 0.01f, leafH, y0, across, into, wood) };
                DoorFixedPanel(piece, m, f, rooms, end, o1);
            }
            else
            {
                float w = (gap - DoorLeafClear * 2f - 0.01f) * 0.5f;
                leaves = new[]
                {
                    DoorLeafPart(root.transform, f, rng, o0 + DoorLeafClear, 1, w, leafH, y0, across, into, wood),
                    DoorLeafPart(root.transform, f, rng, o1 - DoorLeafClear, -1, w, leafH, y0, across, into, wood),
                };
            }

            string name = DoorName(piece, outside, rooms[(swing + 1) / 2]);
            var setup = new HomeDoor.Setup
            {
                Name = name,
                Noun = name.ToLowerInvariant(),
                Leaves = leaves,
                Point = f.P(mid, DoorHandleHeight, 0f),
                Normal = f.N(1),
                Leaf = new Bounds(f.P(mid, (FloorTop + DoorTop) * 0.5f, 0f), f.Size(span, DoorTop - FloorTop, Half * 2f)),
                Gap = new Bounds(f.P(mid, WallH * 0.5f, 0f), f.Size(span, WallH, Half * 2f)),
                Block = new Bounds(f.P(mid, (0.25f + DoorTop) * 0.5f, 0f), f.Size(span - 0.1f, DoorTop - 0.25f, Half * 2f + 0.1f)),
                // An outer door bolts from inside the house; the bathroom's from inside the bathroom.
                LockSide = outside || bath ? swing : 0,
                LockFrom = outside ? "inside the house" : bath ? "inside the bathroom" : null,
                Deadbolt = outside,
                Peephole = outside,
                Open = !outside,   // a fresh run: the front door shut, the doors inside standing open
            };
            HomeDoor.Attach(root, piece, setup);
        }

        // "Front door", "Side door" (an outer door anywhere else), else the room it opens into: "Store room door".
        private static string DoorName(BuildPiece piece, bool outside, HomeRoom into)
        {
            if (outside) return Is(piece, BuildPieceType.Doorway, 4f, 3f) ? "Front door" : "Side door";
            if (into == null) return "Door";
            string room = into.Name.ToLowerInvariant();
            return char.ToUpperInvariant(room[0]) + room.Substring(1) + " door";
        }

        // One leaf, drawn shut: hinged at `hingeAlong` on the wall, reaching `dir` (+1/-1) along it, and turning a
        // quarter turn about its hinge to lie open against the room side (`into`).
        private static FixturePart DoorLeafPart(Transform root, in Frame f, System.Random rng, float hingeAlong, int dir,
            float width, float height, float y0, float across, Vector3 into, Color wood)
        {
            Vector3 hinge = f.P(hingeAlong, y0, across);
            Vector3 reach = f.Along * dir;
            var m = new RaidMesh();
            m.SetFrame(hinge, Mathf.Atan2(reach.x, reach.z) * Mathf.Rad2Deg - 90f);   // the leaf's local +X along `reach`
            FurnitureBuilder.DoorLeaf(m, rng, width, height, wood, false);
            m.ResetFrame();
            float angle = Vector3.SignedAngle(reach, into, Vector3.up);
            return FixturePart.Hinge(root, "Leaf", m, hinge, Vector3.up, angle).WithSpeed(CloneZConfig.DoorSwingSeconds);
        }

        // The bathroom doorway's fixed panel (`s`..`e` along the wall, up to the door head): a stretch of wall finished
        // like the rooms either side — wallpaper over wainscot, a baseboard and chair rail — with a casing strip framing
        // the door. Solid to the survivor. It needs no carve of its own: the far jamb's carve, grown by the dead's
        // radius, already covers it — another would close the doorway to them altogether.
        private static void DoorFixedPanel(BuildPiece piece, RaidMesh m, in Frame f, HomeRoom[] rooms, float s, float e)
        {
            float len = e - s, mid = (s + e) * 0.5f, h = DoorTop;
            m.Box(f.P(mid, h * 0.5f, 0f), f.Size(len, h, Half * 2f), DecorPalette.Matte(CoreColor));
            for (int side = -1; side <= 1; side += 2)
            {
                HomeRoom room = rooms[(side + 1) / 2];
                Vector3 n = f.N(side);
                if (room == null)
                {
                    m.Upright(f.P(mid, h * 0.5f, side * (Half + 0.004f)), n, len, h, DecorMaterials.WallExterior(ExteriorStyle.Siding, Siding));
                    continue;
                }
                Surf trim = DecorPalette.Matte(room.Design.Trim);
                m.Upright(f.P(mid, h * 0.5f, side * (Half + 0.003f)), n, len, h, DecorMaterials.WallInterior(room.Design));
                m.Box(f.P(mid, 0.11f, side * (Half + 0.008f)), f.Size(len, 0.22f, 0.016f), trim);
                float rail = room.Design.RailHeight;
                m.Box(f.P(mid, rail + 0.01f, side * (Half + 0.012f)), f.Size(len, 0.06f, 0.024f), DecorPalette.Matte(room.Design.Trim * 1.05f));
                m.Box(f.P(s + 0.035f, (DoorTop + 0.07f) * 0.5f, side * (Half + 0.012f)), f.Size(0.07f, DoorTop + 0.07f, 0.024f), trim);
            }

            // Solid in the piece's own (turned) frame.
            Vector3 centre = f.P(mid, h * 0.5f, 0f), size = f.Size(len, h, Half * 2f);
            Transform t = piece.transform;
            Vector3 local = Quaternion.Inverse(t.rotation) * size;
            piece.AddSolid(t.InverseTransformPoint(centre), new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)));
        }
        #endregion
    }
}
