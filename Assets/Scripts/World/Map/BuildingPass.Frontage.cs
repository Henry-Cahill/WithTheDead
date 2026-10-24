using UnityEngine;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    // What stands in front of the buildings: a shop's wooden awning, footboard and sign by the door; a home's porch
    // (with, now and then, a swing), the path to its door, the lamp over it, the low rail along its walls and its
    // mailbox; and the fence posts at the corners of a building in four.
    internal static partial class BuildingPass
    {
        private const int SaltAwning = 0xA417, SaltPorchLamp = 0x9071, SaltMailbox = 0xC0FF, SaltMailboxLook = 0xC100,
            SaltTint = 0x7101;

        // Brown stains for the awnings, picked by building: each new colour bakes a wood texture that's kept for the
        // session, so a colour of its own per shop front would grow without end on a streamed map.
        private static readonly Color[] AwningStains =
        {
            Color.HSVToRGB(0.075f, 0.62f, 0.47f), Color.HSVToRGB(0.09f, 0.58f, 0.52f),
            Color.HSVToRGB(0.105f, 0.60f, 0.49f), Color.HSVToRGB(0.115f, 0.56f, 0.54f),
        };

        private static readonly Color[] FlagColours = { Color.red, new Color(0.9f, 0.6f, 0.2f), Color.white };

        private static bool Retail(MapBuilding b) => b.kind == "retail" || (b.use != null && b.use.StartsWith("shop:"));

        #region Shop fronts
        // A shop front: a wooden awning over the door (with slats, iron corner brackets and a canvas banner), a footboard
        // with a welcome mat before it, and a small painted sign on a post with a lantern, to the right of the door.
        private static void Awning(Site site, Vector2 doorPt, Vector2 outward, float yaw, MapBuilding b, System.Random rng)
        {
            const float width = 2.0f, depth = 0.6f, aboveDoor = 1.2f, thickness = 0.08f;
            const float bracket = 0.05f, lantern = 0.12f;
            const float footboardHeight = 0.07f, footboardDepth = 0.25f;
            const float matThickness = 0.015f, matLift = 0.002f;

            Vector3 awning = To3(doorPt + outward * (depth * 0.5f), DoorHeight + aboveDoor);
            Surf wood = DecorMaterials.WoodFloor(AwningColour(b));
            // Up in the visible roof batch: shown from outside, and kept out of the NavMesh bake.
            site.Roofs.At(awning).Box(awning, new Vector3(width, thickness, depth), yaw, wood);

            Vector3 footboard = To3(doorPt + outward * (depth + footboardDepth * 0.5f), footboardHeight * 0.5f);
            var footboardSize = new Vector3(width, footboardHeight, footboardDepth);
            site.Town.Out.Solid(footboard).Box(footboard, footboardSize, 0f, wood);
            site.Town.PropCollider(footboard, footboardSize, 0f);

            Surf mat = DecorPalette.Matte(MatColour(rng));
            Vector3 matCentre = footboard + new Vector3(0f, footboardHeight * 0.5f + matLift + matThickness * 0.5f, 0f);
            site.Town.Out.Solid(matCentre).Box(matCentre, new Vector3(width * 0.78f, matThickness, footboardDepth - 0.02f), 0f, mat);

            // The supports either side, each with a jute rope tied up it.
            const float supportWidth = 0.07f;
            const float supportHeight = aboveDoor + 0.5f;
            var supportSize = new Vector3(supportWidth, supportHeight, supportWidth);
            var ropeSize = new Vector3(0.02f, supportHeight, 0.02f);
            Surf rope = DecorPalette.Matte(new Color(0.545f, 0.435f, 0.255f));
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 support = awning + Vector3.right * ((width * 0.5f - supportWidth * 0.5f) * side);
                site.Town.Out.Solid(support).Box(support, supportSize, 0f, wood);
                site.Town.PropCollider(support, supportSize, 0f);
                site.Town.Out.Solid(support).Box(support, ropeSize, 0f, rope);
                site.Town.PropCollider(support, ropeSize, 0f);
            }

            const int slats = 4;
            const float slatThickness = 0.02f;
            float slatY = awning.y - thickness * 0.5f + slatThickness * 0.5f;
            for (int i = 0; i < slats; i++)
            {
                var slat = new Vector3(awning.x, slatY + i * (aboveDoor / (slats + 1)), awning.z);
                site.Roofs.At(slat).Box(slat, new Vector3(width * 0.95f, slatThickness, depth * 0.9f), yaw, wood);
            }

            Surf iron = DecorPalette.Metal(new Color(0.45f, 0.45f, 0.48f));
            var bracketSize = new Vector3(bracket, bracket, bracket);
            for (int k = 0; k < 4; k++)
            {
                Vector3 corner = awning + new Vector3((k % 2 == 0 ? -0.5f : 0.5f) * width, aboveDoor, (k < 2 ? -0.5f : 0.5f) * depth);
                site.Roofs.At(corner).Box(corner, bracketSize, 0f, iron);
                site.Town.PropCollider(corner, bracketSize, 0f);
            }

            // The sign: a post to the right of the door, a board on it facing the street, a painted panel and a lantern.
            const float postHeight = 1.0f, postSize = 0.05f;
            const float boardWidth = 0.6f, boardHeight = 0.15f, boardDepth = 0.02f;
            Vector3 right = Vector3.Cross(Vector3.up, new Vector3(outward.x, 0f, outward.y));
            Vector3 foot = To3(doorPt + outward * 0.2f + new Vector2(right.x, right.z) * 0.6f, 0f);
            Vector3 post = foot + new Vector3(0f, postHeight * 0.5f, 0f);
            var postBox = new Vector3(postSize, postHeight, postSize);
            site.Town.Out.Solid(post).Box(post, postBox, 0f, DecorMaterials.WoodFloor(new Color(0.48f, 0.38f, 0.28f)));
            site.Town.PropCollider(post, postBox, 0f);
            Vector3 board = foot + new Vector3(0f, postHeight + boardHeight * 0.5f, 0f);
            var boardBox = new Vector3(boardWidth, boardHeight, boardDepth);
            site.Town.Out.Solid(board).Box(board, boardBox, yaw, DecorPalette.Matte(new Color(0.90f, 0.84f, 0.70f)));
            site.Town.PropCollider(board, boardBox, yaw);
            Vector3 paint = board + new Vector3(0f, 0f, boardDepth * 0.51f);
            var paintBox = new Vector3(boardWidth * 0.9f, boardHeight * 0.8f, 0.01f);
            site.Town.Out.Solid(paint).Box(paint, paintBox, yaw, DecorPalette.Matte(new Color(0.75f, 0.20f, 0.20f)));
            site.Town.PropCollider(paint, paintBox, yaw);
            Vector3 lamp = post + new Vector3(0f, postHeight * 0.5f + lantern * 0.5f + 0.05f, 0f);
            var lampBox = new Vector3(lantern, lantern, lantern);
            site.Town.Out.Solid(lamp).Box(lamp, lampBox, 0f, DecorPalette.Glow(new Color(1.00f, 0.85f, 0.60f)));
            site.Town.PropCollider(lamp, lampBox, 0f);

            const float bannerHeight = 0.38f, bannerDepth = 0.02f;
            var banner = new Vector3(awning.x, awning.y - thickness * 0.5f + bannerHeight * 0.5f, awning.z + depth * 0.5f - bannerDepth * 0.5f);
            var bannerBox = new Vector3(width * 0.78f, bannerHeight, bannerDepth);
            site.Town.Out.Solid(banner).Box(banner, bannerBox, yaw, DecorPalette.Matte(BannerColour(b, rng)));
            site.Town.PropCollider(banner, bannerBox, yaw);

            site.Take(doorPt + outward * (depth + footboardDepth + 0.7f), width * 0.5f + 0.7f);
        }

        private static Color AwningColour(MapBuilding b) => AwningStains[MapStyle.Pick(b.id, SaltAwning, AwningStains.Length)];

        // The banner: the awning's stain a little brighter, as canvas, its hue nudged.
        private static Color BannerColour(MapBuilding b, System.Random rng)
        {
            Color.RGBToHSV(AwningColour(b), out float h, out float s, out float v);
            h += (float)rng.NextDouble() * 0.02f - 0.01f;
            v = Mathf.Clamp01(v + 0.12f);
            return Color.HSVToRGB(h, s * 0.9f, v);
        }

        // A muted beige, nudged a little from shop to shop.
        private static Color MatColour(System.Random rng)
        {
            float h = 0.12f + (float)rng.NextDouble() * 0.02f - 0.01f;
            float s = 0.25f + (float)rng.NextDouble() * 0.05f - 0.025f;
            float v = 0.85f + (float)rng.NextDouble() * 0.04f - 0.02f;
            return Color.HSVToRGB(Mathf.Clamp01(h), Mathf.Clamp01(s), Mathf.Clamp01(v));
        }
        #endregion

        #region Homes
        // A wooden porch at the door of a house or trailer: a deck, two steps, two posts with a rail and balusters, and,
        // at three in ten, a swing hung between the posts.
        private static void Porch(Site site, Vector2 doorPoint, Vector2 doorOutward, float doorYaw, System.Random rng)
        {
            Surf wood = DecorMaterials.WoodFloor(Stain);
            Vector3 deck = To3(doorPoint + doorOutward * 0.4f, 0.1f);
            var deckSize = new Vector3(1.5f, 0.2f, 0.8f);   // across the door, up, out from it
            site.Town.Out.Solid(deck).Box(deck, deckSize, doorYaw, wood);

            const float stepDepth = 0.2f, stepHeight = 0.05f;
            Vector3 outward = new Vector3(doorOutward.x, 0f, doorOutward.y);
            var stepSize = new Vector3(deckSize.x, stepHeight, stepDepth);
            Vector3 step = deck + outward * (-(deckSize.z * 0.5f + stepDepth * 0.5f));
            step.y = stepHeight * 0.5f;
            site.Town.Out.Solid(step).Box(step, stepSize, doorYaw, wood);
            step = deck + outward * (-(deckSize.z * 0.5f + 1.5f * stepDepth));
            step.y = stepHeight * 1.5f;
            site.Town.Out.Solid(step).Box(step, stepSize, doorYaw, wood);

            const float postWidth = 0.05f, postHeight = 0.40f;
            var postSize = new Vector3(postWidth, postHeight, postWidth);
            Vector3 right = Vector3.Cross(Vector3.up, outward);
            Vector3 front = outward * (deckSize.z * 0.5f);
            Vector3 left = deck + front - right * (deckSize.x * 0.5f - postWidth * 0.5f);
            left.y = deckSize.y * 0.5f + postHeight * 0.5f;
            Vector3 rightPost = deck + front + right * (deckSize.x * 0.5f - postWidth * 0.5f);
            rightPost.y = left.y;
            site.Town.Out.Solid(left).Box(left, postSize, doorYaw, wood);
            site.Town.Out.Solid(rightPost).Box(rightPost, postSize, doorYaw, wood);

            const float railHeight = 0.04f, railDepth = 0.04f;
            Vector3 rail = (left + rightPost) * 0.5f;
            rail.y = left.y + postHeight * 0.5f - railHeight * 0.5f;
            site.Town.Out.Solid(rail).Box(rail, new Vector3(deckSize.x - 2f * postWidth, railHeight, railDepth), doorYaw, wood);
            int balusters = Mathf.Max(2, Mathf.FloorToInt(deckSize.x / 0.20f));
            float spacing = deckSize.x / (balusters + 1);
            var balusterSize = new Vector3(0.02f, postHeight * 0.8f, 0.02f);
            for (int i = 1; i <= balusters; i++)
            {
                Vector3 baluster = left + right * (spacing * i);
                baluster.y = left.y - postHeight * 0.5f + balusterSize.y * 0.5f;
                site.Town.Out.Solid(baluster).Box(baluster, balusterSize, doorYaw, wood);
                site.Town.PropCollider(baluster, balusterSize, doorYaw);
            }
            Swing(site, left, rightPost, doorYaw, rng);
        }

        // A wooden swing on ropes between the porch's posts, with metal brackets, a seat and a back, and the fixture to
        // sit on it.
        private static void Swing(Site site, Vector3 leftPost, Vector3 rightPost, float yaw, System.Random rng)
        {
            if (rng.NextDouble() >= 0.30) return;
            Vector3 mid = (leftPost + rightPost) * 0.5f;
            float ropeLength = 0.85f + (float)(rng.NextDouble() * 0.2 - 0.1);
            const float ropeThickness = 0.02f, knotRadius = 0.03f;
            Surf wood = DecorMaterials.WoodFloor(Stain);
            Surf metal = DecorMaterials.Metal(Color.gray);

            Vector3 rope = mid - new Vector3(0f, ropeLength * 0.5f, 0f);
            site.Town.Out.Solid(rope).Box(rope, new Vector3(ropeThickness, ropeLength, ropeThickness), yaw,
                DecorMaterials.Rope(Color.Lerp(Stain, Color.black, 0.5f)));
            Vector3 knot = mid - new Vector3(0f, ropeLength, 0f) + new Vector3(0f, knotRadius, 0f);
            site.Town.Out.Solid(knot).Box(knot, new Vector3(knotRadius * 2f, knotRadius * 2f, knotRadius * 2f), yaw, metal);

            const float bracketThickness = 0.04f;
            var bracketSize = new Vector3(bracketThickness, bracketThickness, 0.02f);
            Vector3 along = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * 0.06f;
            Vector3 bracket = leftPost + along - new Vector3(0f, bracketThickness * 0.5f, 0f);
            site.Town.Out.Solid(bracket).Box(bracket, bracketSize, yaw, metal);
            bracket = rightPost - along - new Vector3(0f, bracketThickness * 0.5f, 0f);
            site.Town.Out.Solid(bracket).Box(bracket, bracketSize, yaw, metal);

            var seatSize = new Vector3(0.60f, 0.08f, 0.30f);
            Vector3 seat = mid - new Vector3(0f, ropeLength, 0f);
            site.Town.Out.Solid(seat).Box(seat, seatSize, yaw, wood);
            site.Town.PropCollider(seat, seatSize, yaw);
            var backSize = new Vector3(0.60f, 0.40f, 0.08f);
            Vector3 back = seat + new Vector3(0f, (seatSize.y + backSize.y) * 0.5f, -(seatSize.z * 0.5f + backSize.z * 0.5f));
            site.Town.Out.Solid(back).Box(back, backSize, yaw, wood);
            site.Town.PropCollider(back, backSize, yaw);

            // Under the site's world root, so it stands where the swing does on a streamed map's piece and goes with it.
            var fixture = new GameObject("SwingInteractable");
            fixture.transform.SetParent(site.Town.World, false);
            fixture.transform.position = seat;
            fixture.AddComponent<SwingInteractable>();
            site.Take(new Vector2(seat.x, seat.z), Mathf.Max(seatSize.x, seatSize.z, backSize.x, backSize.z) * 0.5f);
        }

        // A short concrete path out from a home's front door.
        private static void Path(Site site, Vector2 doorPoint, Vector2 doorOutward, float doorYaw)
        {
            const float length = 1.0f;
            Vector3 centre = To3(doorPoint + doorOutward * (0.4f + length * 0.5f), 0.01f);
            var size = new Vector3(0.3f, 0.02f, length);
            site.Town.Out.Solid(centre).Box(centre, size, doorYaw, DecorMaterials.Concrete);
            site.Town.PropCollider(centre, size, doorYaw);
        }

        // A lamp over the front door of two homes in five, lit after dark (PorchLamp).
        private static void PorchLight(Site site, MapBuilding b, Vector2 doorPoint, Vector2 doorOutward)
        {
            if (MapStyle.Unit(b.id, SaltPorchLamp) >= 0.4f) return;
            PorchLamp.Spawn(site.Town.World, To3(doorPoint + doorOutward * 0.3f, DoorHeight + 0.3f),
                MapStyle.Unit(b.id, SaltPorchLamp + 1) * 1000f);
        }

        // A low wooden rail along a home's wall, just outside it.
        private static void WallRail(Site site, Vector2 a, Vector2 d, Vector2 outward, float len, float yaw)
        {
            const float height = 0.08f, thickness = 0.04f;
            Vector3 centre = To3(a + d * (len * 0.5f), height * 0.5f)
                + new Vector3(outward.x, 0f, outward.y) * (Thick * 0.5f + thickness * 0.5f);
            var size = new Vector3(len, height, thickness);
            site.Town.Out.Solid(centre).Box(centre, size, yaw, DecorMaterials.WoodFloor(Stain));
            site.Town.PropCollider(centre, size, yaw);
        }

        // A mailbox on a post for three homes in ten, in the house's trim colour with a red, orange or white flag. It
        // stands a metre from the footprint's middle, turned to face away from its first wall, and is drawn with the
        // overhead batches.
        private static void Mailbox(Site site, Footprint f)
        {
            if (MapStyle.Unit(f.Data.id, SaltMailbox) >= 0.3f) return;
            const float bodyWidth = 0.25f, bodyHeight = 0.40f, bodyDepth = 0.15f;
            const float poleRadius = 0.025f, poleHeight = 0.60f;
            Vector2 edge = f.Pts[1] - f.Pts[0];
            Vector3 forward = new Vector3(-edge.y, 0f, edge.x).normalized;
            Vector3 at = new Vector3(f.Middle.x, 0f, f.Middle.y) + forward;
            System.Random rng = MapStyle.Rng(f.Data.id, SaltMailboxLook);
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + (float)(rng.NextDouble() * 20.0 - 10.0);
            Color flag = FlagColours[rng.Next(FlagColours.Length)];
            Color trim = TrimColour(f.Data);

            RaidMesh m = site.Overhead.At(at);
            m.SetFrame(at, yaw);
            m.Box(Vector3.zero, new Vector3(bodyWidth, bodyHeight, bodyDepth), DecorPalette.Matte(trim));
            Color flap = trim * 0.85f;
            flap.a = 1f;
            m.Box(new Vector3(0f, bodyHeight * 0.5f, bodyDepth * 0.5f + 0.01f), new Vector3(bodyWidth * 0.6f, bodyHeight * 0.7f, 0.02f),
                DecorPalette.Matte(flap));
            var pole = new Vector3(0f, poleHeight * 0.5f, -bodyDepth * 0.5f - poleRadius);
            m.Box(pole, new Vector3(poleRadius * 2f, poleHeight, poleRadius * 2f), DecorPalette.Matte(new Color(0.45f, 0.33f, 0.22f)));
            m.Box(pole + new Vector3(0f, poleHeight * 0.9f, 0f), new Vector3(0.02f, 0.12f, 0.02f), DecorPalette.Matte(flag));
            m.ResetFrame();
            site.Town.PropCollider(at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, bodyHeight * 0.5f, -bodyDepth * 0.5f),
                new Vector3(bodyWidth, bodyHeight, bodyDepth + poleRadius * 2f), yaw);
        }

        // A building's trim: its tint (a warm hue, by building) a little desaturated.
        private static Color TrimColour(MapBuilding b)
        {
            float hue = MapStyle.Unit(b.id, SaltTint) * 0.1f;
            float sat = 0.6f + MapStyle.Unit(b.id, SaltTint + 1) * 0.2f;
            float val = 0.7f + MapStyle.Unit(b.id, SaltTint + 2) * 0.2f;
            Color trim = Color.HSVToRGB(hue, sat * 0.75f, val);
            trim.a = 1f;
            return trim;
        }

        // Fence posts at the corners of a footprint: with the walls inside the square, as scenery past its edge.
        private static void CornerPosts(Site site, Footprint f, bool inside)
        {
            Surf wood = DecorMaterials.WoodFloor(Stain);
            var size = new Vector3(0.1f, 1f, 0.1f);
            foreach (Vector2 corner in f.Pts)
            {
                var centre = new Vector3(corner.x, 0.5f, corner.y);
                RaidMesh m = inside ? site.Town.Out.Solid(centre) : site.Town.Far.Solid(centre);
                m.Box(centre, size, wood);
            }
        }
        #endregion
    }
}
