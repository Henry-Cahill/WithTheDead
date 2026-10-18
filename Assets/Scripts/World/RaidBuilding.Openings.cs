using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    internal sealed partial class RaidBuilding
    {
        private const int RollUpBands = 10;        // the steel curtain's bands, each sliding up into the drum
        private const float RollUpDrumY = 2.45f;   // the drum's axis
        private const float RollUpAcross = 0.09f;  // the curtain's distance in from the wall's inside face

        /// <summary>A roll-up door in the (leafless) doorway at local (x, z): the drum over the inside of the doorway with its
        /// hood and guide rails, and a working steel curtain (<see cref="RollUpShutter"/>) that rolls up into it.</summary>
        public void RollUpDoor(float lx, float lz)
        {
            Vector3 p = W(lx, 0f, lz);
            foreach (Opening o in _openings)
            {
                if (o.Kind != OpeningKind.Door) continue;
                Vector3 c = RunPoint(o.Run, o.Pos, 0f, 0f);
                if ((c - p).sqrMagnitude > 1f) continue;
                int outside = OutsideSide(o.Run, o.Pos);
                int inside = outside == 0 ? 1 : -outside;
                Vector3 at = RunPoint(o.Run, o.Pos, RollUpDrumY, inside * (Half + 0.2f));
                RaidMesh m = WallMeshAt(RunPoint(o.Run, o.Pos, 2.5f, 0f));
                m.CylinderX(at, 0.2f, o.Width + 0.2f, 10, DecorPalette.Metal(new Color(0.45f, 0.46f, 0.44f)), o.Run.AlongX ? 0f : 90f);
                m.Box(RunPoint(o.Run, o.Pos, 2.27f, inside * (Half + 0.05f)), RunSize(o.Run, o.Width + 0.1f, 0.12f, 0.04f), DecorPalette.Metal(new Color(0.40f, 0.41f, 0.40f)));
                // The guide rails the curtain runs in, either side of the doorway.
                Surf rail = DecorPalette.Metal(new Color(0.36f, 0.37f, 0.36f));
                for (int e = -1; e <= 1; e += 2)
                {
                    float along = e < 0 ? o.A - 0.04f : o.B + 0.04f;
                    m.Box(RunPoint(o.Run, along, 1.15f, inside * (Half + RollUpAcross + 0.01f)), RunSize(o.Run, 0.05f, 2.3f, 0.05f), rail);
                }
                Opening door = o;
                Later(() => HangRollUp(door, inside));
                return;
            }
            Town.Warn(Name + ": no doorway for a roll-up door");
        }

        // The steel curtain, once the doorway's segment is an object: bands drawn down across the doorway on its `inside`
        // face, each sliding up by just enough to end inside the drum (the bottom one furthest), so the curtain gathers into
        // it as it rises; and the shutter that works them, starting up or down at random.
        private void HangRollUp(Opening o, int inside)
        {
            WallRun r = o.Run;
            var l = new OpeningLine(r.AlongX, r.Coord);
            Transform segment = OpeningObject(o);
            var root = new GameObject("RollUpDoor");
            root.transform.SetParent(segment, false);
            root.transform.SetPositionAndRotation(RunPoint(r, o.Pos, 0f, 0f), Quaternion.identity);
            root.AddComponent<NavMeshModifier>().ignoreFromBuild = true;

            float top = DoorHeight + 0.1f, band = top / RollUpBands, len = o.Width + 0.08f;
            var bands = new FixturePart[RollUpBands];
            for (int k = 0; k < RollUpBands; k++)
            {
                float y0 = k * band, y1 = y0 + band, cy = (y0 + y1) * 0.5f;
                float across = inside * (Half + RollUpAcross + k * 0.002f);   // a hair apart, so gathered bands never fight
                var m = new RaidMesh();
                FurnitureBuilder.ShutterBand(m, l, o.Pos, len, y0 + 0.004f, y1, across, k == 0);
                bands[k] = FixturePart.Slide(root.transform, "Band", m, l.P(o.Pos, cy, across), Vector3.up * (RollUpDrumY - cy))
                    .WithSpeed(CloneZConfig.RollUpRollSeconds);
            }

            var setup = new RollUpShutter.Setup
            {
                Name = "Roll-up door",
                Bands = bands,
                Point = RunPoint(r, o.Pos, 1.0f, 0f),
                Solid = new Bounds(RunPoint(r, o.Pos, DoorHeight * 0.5f, 0f), RunSize(r, o.Width, DoorHeight, Half * 2f)),
                Gap = new Bounds(RunPoint(r, o.Pos, WallH * 0.5f, 0f), RunSize(r, o.Width, WallH, Half * 2f)),
                Block = new Bounds(RunPoint(r, o.Pos, (0.25f + DoorHeight) * 0.5f, inside * 0.05f), RunSize(r, o.Width - 0.1f, DoorHeight - 0.25f, Half * 2f + 0.3f)),
                Open = Rng.NextDouble() < CloneZConfig.RollUpOpenChance,
            };
            RollUpShutter.Attach(root, setup);
        }
    }
}
