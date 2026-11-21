using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The bathroom's loose furniture (see the per-area hooks in HomeFurniture.cs): the toilet, the pedestal basin and
    // the shower stall, drawn in china, glass and chrome (FurnitureBuilder.Bathroom.cs), with the parts that move and
    // the water kept apart for each piece's fixture (ToiletFixture, BasinFixture, ShowerFixture).
    public partial class HomeFurniture
    {
        #region Bathroom
        // The bathroom's furniture — the toilet, the pedestal basin and the shower stall — drawn in china, glass and chrome
        // (FurnitureBuilder.Bathroom.cs), with the parts that move and the water kept apart for each piece's fixture
        // (ToiletFixture, BasinFixture, ShowerFixture).

        partial void DrawBathroom(RaidMesh m, System.Random rng, ref bool drawn)
        {
            switch (Kind)
            {
                case HomeFurnitureKind.Toilet: FurnitureBuilder.ToiletBody(m); break;
                case HomeFurnitureKind.Basin: FurnitureBuilder.PedestalBasin(m, rng); break;
                case HomeFurnitureKind.Shower: FurnitureBuilder.ShowerStall(m, rng); break;
                default: return;
            }
            drawn = true;
        }

        partial void AttachBathroom()
        {
            switch (Kind)
            {
                case HomeFurnitureKind.Toilet: AttachToilet(); break;
                case HomeFurnitureKind.Basin: AttachBasin(); break;
                case HomeFurnitureKind.Shower: AttachShower(); break;
            }
        }

        // The seat and lid lift on one hinge behind the seat, the cistern lid stands up against the wall on its back edge,
        // the flush lever dips; the cistern's water sinks as it empties.
        private void AttachToilet()
        {
            Transform t = transform;
            Vector3 lift = BathroomFixtures.Direction(this, Vector3.left);   // turns a part's front edge up and back
            Vector3 hinge = BathroomFixtures.Point(this, FurnitureBuilder.ToiletSeatHinge);
            FixturePart seat = FixturePart.Hinge(t, "Seat", BathDraw(FurnitureBuilder.ToiletSeat), hinge, lift, CloneZConfig.ToiletSeatOpenAngle);
            FixturePart lid = FixturePart.Hinge(t, "Lid", BathDraw(FurnitureBuilder.ToiletLid), hinge, lift, CloneZConfig.ToiletSeatOpenAngle);
            FixturePart cisternLid = FixturePart.Hinge(t, "CisternLid", BathDraw(FurnitureBuilder.ToiletCisternLid),
                BathroomFixtures.Point(this, FurnitureBuilder.ToiletCisternLidHinge), lift, CloneZConfig.ToiletCisternLidAngle);
            FixturePart lever = FixturePart.Hinge(t, "FlushLever", BathDraw(FurnitureBuilder.ToiletLever),
                BathroomFixtures.Point(this, FurnitureBuilder.ToiletLeverPivot), BathroomFixtures.Direction(this, Vector3.right),
                CloneZConfig.ToiletLeverAngle, ShadowCastingMode.Off).WithSpeed(0.12f);
            MeshRenderer water = BathroomFixtures.Emit(BathDraw(FurnitureBuilder.ToiletCisternSheet), "CisternWater", t,
                BathroomFixtures.Point(this, FurnitureBuilder.ToiletCisternWater), false, true);
            ToiletFixture.Create(this).SetParts(seat, lid, cisternLid, lever, water);
        }

        // The mixer's lever lifts at the front; the stream, the water in the bowl and the plug (in the drain or out on the
        // deck) are shown and hidden.
        private void AttachBasin()
        {
            Transform t = transform;
            FixturePart lever = FixturePart.Hinge(t, "TapLever", BathDraw(FurnitureBuilder.BasinLever),
                BathroomFixtures.Point(this, FurnitureBuilder.BasinLeverPivot), BathroomFixtures.Direction(this, Vector3.left),
                CloneZConfig.BasinLeverAngle, ShadowCastingMode.Off).WithSpeed(0.15f);
            Vector3 surface = BathroomFixtures.Point(this, FurnitureBuilder.BasinWaterTop);
            MeshRenderer stream = BathroomFixtures.Emit(BathDraw(FurnitureBuilder.BasinStream), "TapStream", t, surface, false, false);
            MeshRenderer water = BathroomFixtures.Emit(BathDraw(FurnitureBuilder.BasinWaterSheet), "BasinWater", t, surface, false, false);
            MeshRenderer plugIn = BathroomFixtures.Emit(BathDraw(m => FurnitureBuilder.BasinPlug(m, true)), "PlugIn", t, surface, false, false);
            MeshRenderer plugOut = BathroomFixtures.Emit(BathDraw(m => FurnitureBuilder.BasinPlug(m, false)), "PlugOut", t, surface, false, true);
            BasinFixture.Create(this).SetParts(lever, stream, water, plugIn, plugOut);
        }

        // The glass door swings out about its edge by the corner post, the mixer's lever turns a quarter round, and the
        // spray's two looks take turns while it runs.
        private void AttachShower()
        {
            Transform t = transform;
            FixturePart door = FixturePart.Hinge(t, "ShowerDoor", BathDraw(FurnitureBuilder.ShowerDoor),
                BathroomFixtures.Point(this, FurnitureBuilder.ShowerDoorHinge), Vector3.down, CloneZConfig.ShowerDoorAngle, ShadowCastingMode.Off);
            FixturePart lever = FixturePart.Hinge(t, "MixerLever", BathDraw(FurnitureBuilder.ShowerLever),
                BathroomFixtures.Point(this, FurnitureBuilder.ShowerLeverPivot), BathroomFixtures.Direction(this, Vector3.forward), 90f,
                ShadowCastingMode.Off).WithSpeed(0.2f);
            var rng = new System.Random(Seed ^ 0x5157);
            Vector3 rose = BathroomFixtures.Point(this, FurnitureBuilder.ShowerRose);
            MeshRenderer sprayA = BathroomFixtures.Emit(BathDraw(m => FurnitureBuilder.ShowerSpray(m, rng)), "SprayA", t, rose, false, false);
            MeshRenderer sprayB = BathroomFixtures.Emit(BathDraw(m => FurnitureBuilder.ShowerSpray(m, rng)), "SprayB", t, rose, false, false);
            ShowerFixture.Create(this).SetParts(door, lever, sprayA, sprayB);
        }

        // A part drawn in this piece's local frame, its vertices landing in world space.
        private RaidMesh BathDraw(System.Action<RaidMesh> draw)
        {
            RaidMesh m = BathroomFixtures.LocalMesh(this);
            draw(m);
            m.ResetFrame();
            return m;
        }
        #endregion
    }
}
