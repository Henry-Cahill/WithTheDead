using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // The buildings' openings: the wooden frame and the knob of the front door, and the windows (dark panes with frames,
    // sills and shutters) along every wall, on every storey.
    internal static partial class BuildingPass
    {
        private const float DoorFrameThickness = 0.08f;   // the frame's side studs and lintel
        private const float DoorLintelHeight = 0.12f;
        private const float WindowFrameThickness = 0.04f;

        private static Surf Glass => DecorPalette.Gloss(new Color(0.08f, 0.09f, 0.11f));

        // A frame round the door: two studs and a lintel, just outside the wall, each with a collider. The door is
        // centred <paramref name="doorPos"/> along the wall from <paramref name="a"/>.
        private static void DoorFrame(Site site, Vector2 a, Vector2 d, float doorPos, float doorYaw, Vector2 outward, in Surf exterior)
        {
            float halfWidth = DoorWidth * 0.5f;
            Vector3 offset = new Vector3(outward.x, 0f, outward.y) * (Thick * 0.5f + DoorFrameThickness * 0.5f);
            var studSize = new Vector3(DoorFrameThickness, DoorHeight, DoorFrameThickness);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 stud = To3(a + d * (doorPos + halfWidth * side), DoorHeight * 0.5f) + offset;
                site.Town.Out.Solid(stud).Box(stud, studSize, doorYaw, exterior);
                site.Town.PropCollider(stud, studSize, doorYaw);
            }
            Vector3 lintel = To3(a + d * doorPos, DoorHeight + DoorLintelHeight * 0.5f) + offset;
            var lintelSize = new Vector3(DoorWidth + 2f * DoorFrameThickness, DoorLintelHeight, DoorFrameThickness);
            site.Town.Out.Solid(lintel).Box(lintel, lintelSize, doorYaw, exterior);
            site.Town.PropCollider(lintel, lintelSize, doorYaw);
        }

        private static void DoorKnob(Site site, Vector2 doorPoint, Vector2 doorOutward, float doorYaw)
        {
            const float depth = 0.02f, radius = 0.07f;
            Vector3 centre = To3(doorPoint + doorOutward * (Thick * 0.5f + depth * 0.5f), DoorHeight * 0.5f);
            site.Town.Out.Solid(centre).Box(centre, new Vector3(depth, radius * 2f, radius * 2f), doorYaw,
                DecorPalette.Gloss(new Color(0.8f, 0.8f, 0.8f)));
        }

        // Dark panes about every 3.2 m along a wall, on every storey, clear of the door (<paramref name="doorAt"/> along
        // the wall, or negative for none), each with a wooden frame, a sill and a shutter either side. The ground
        // floor's go with the walls, the upper storeys' overhead; the ground floor's are listed in <paramref name="record"/>
        // (when there is one) as the openings of wall <paramref name="edge"/>.
        private static void Windows(Site site, BuildingShell shell, Vector2 a, Vector2 d, Vector2 outward, float len, float height,
            float doorAt, System.Random rng, in Surf outside, int edge, EnterableBuilding record)
        {
            if (len < 2.4f) return;
            int count = Mathf.Max(1, Mathf.FloorToInt(len / 3.2f));
            float step = len / count;
            const float winWidth = 1.0f, winHeight = 1.15f;
            const float shutterWidth = 0.26f, shutterDepth = 0.03f;
            Vector3 normal = To3(outward, 0f);
            Vector3 along = new Vector3(d.x, 0f, d.y);
            float yaw = -Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            Surf frame = DecorMaterials.WoodFloor(Stain);
            float half = WindowFrameThickness * 0.5f;
            var across = new Vector3(winWidth + WindowFrameThickness * 2f, WindowFrameThickness, WindowFrameThickness);
            var upright = new Vector3(WindowFrameThickness, winHeight + WindowFrameThickness * 2f, WindowFrameThickness);
            var shutter = new Vector3(shutterWidth, winHeight, shutterDepth);
            float shutterOffset = 0.5f * winWidth + 0.07f + shutterWidth * 0.5f;
            for (int k = 0; k < count; k++)
            {
                float s = step * (k + 0.5f);
                if (doorAt >= 0f && Mathf.Abs(s - doorAt) < 1.5f) continue;
                Vector2 at = a + d * s + outward * 0.012f;
                for (float y = 1.55f; y + 0.7f < height; y += Storey)
                {
                    Vector3 c = To3(at, y);
                    bool ground = y < GroundStorey;
                    RaidMesh m = ground ? shell.WallsFacing(outward) : shell.Upper;
                    if (ground && record != null)
                        record.Windows.Add(new WindowSpan
                        {
                            Edge = edge, Storey = 0, From = s - winWidth * 0.5f, To = s + winWidth * 0.5f,
                            Sill = y - winHeight * 0.5f, Head = y + winHeight * 0.5f,
                        });
                    m.Upright(c, normal, winWidth, winHeight, Glass);
                    m.Box(c + new Vector3(0f, winHeight * 0.5f + half, 0f), across, yaw, frame);
                    m.Box(c + new Vector3(0f, -(winHeight * 0.5f) - half, 0f), across, yaw, frame);
                    m.Box(c + new Vector3(-(winWidth * 0.5f) - half, 0f, 0f), upright, yaw, frame);
                    m.Box(c + new Vector3(winWidth * 0.5f + half, 0f, 0f), upright, yaw, frame);

                    const float sillHeight = 0.08f;
                    float sillDepth = 0.02f + 0.02f * (float)rng.NextDouble();
                    m.Box(To3(at, y - sillHeight * 0.5f), new Vector3(1.0f, sillHeight, sillDepth), yaw, outside);

                    Vector3 proud = normal * (Thick * 0.5f + shutterDepth * 0.5f);   // the shutters stand on the wall's face
                    m.Box(c + along * shutterOffset + proud, shutter, yaw, outside);
                    m.Box(c - along * shutterOffset + proud, shutter, yaw, outside);
                }
            }
        }
    }
}
