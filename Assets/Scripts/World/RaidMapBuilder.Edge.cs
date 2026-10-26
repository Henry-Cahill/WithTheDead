using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The map's edge and what lies past it: the four exits where the roads leave town (the police road block, the Guard
    // checkpoint, the rail crossing and the dock), the chain-link fence round the rest, the rail line running north–south
    // past the depot, then the never-baked scenery beyond (the snowfields, the pine woods, the frozen river and the dock to
    // the west, the truss bridge to the east, the roads and rails running on out), closing with the survivor's spawn.
    public static partial class RaidMapBuilder
    {
        private sealed partial class Builder
        {
            #region Exits
            // The four roads out; two or three are open each raid.
            /// <summary>
            /// Adds the four road exits, picks which two or three are open at random, and reserves their zones against clutter.
            /// </summary>
            public void Exits()
            {
                const float d = ExitReach;
                List<RaidExit> exits = _town.Info.Exits;
                exits.Add(new RaidExit { Name = "Road Block", Position = new Vector3(0f, 0f, d), Radius = 4.5f, ExtractSeconds = 8f, Hint = "Police barricade, north end of Main St" });
                exits.Add(new RaidExit { Name = "Rail Bridge", Position = new Vector3(d, 0f, 0f), Radius = 4.5f, ExtractSeconds = 8f, Hint = "Rail crossing, east end of Depot Rd" });
                exits.Add(new RaidExit { Name = "Army Checkpoint", Position = new Vector3(0f, 0f, -d), Radius = 4.5f, ExtractSeconds = 8f, Hint = "Guard checkpoint, south end of Main St" });
                exits.Add(new RaidExit { Name = "Boat Dock", Position = new Vector3(-d, 0f, 0f), Radius = 4.5f, ExtractSeconds = 8f, Hint = "River dock, west end of Depot Rd" });

                int open = 2 + _rng.Next(2);
                var order = new[] { 0, 1, 2, 3 };
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (order[i], order[j]) = (order[j], order[i]);
                }
                for (int i = 0; i < order.Length; i++) exits[order[i]].Open = i < open;

                // Nothing solid may stand in (or crowd) an exit zone.
                foreach (RaidExit e in exits) Reserve(Around(e.Position, e.Radius + 2f, e.Radius + 2f));
            }
            #endregion

            #region Edge
            // The fence just outside the clamp line (the survivor is kept within ±Half), broken where the roads leave town,
            // each road blocked further out by its exit's own barriers; and the rail line through it.
            /// <summary>
            /// Builds the perimeter fence, road blocks, rail line and dock barrier to close the map edge around the playable square.
            /// </summary>
            public void Edge()
            {
                float e = EdgeLine, gap = MainSt.Outer + 0.2f;
                // Counter-clockwise, so the banked snow lies outside.
                FenceSide(new Vector3(e, 0f, e), new Vector3(-e, 0f, e), gap);
                FenceSide(new Vector3(-e, 0f, e), new Vector3(-e, 0f, -e), gap);
                FenceSide(new Vector3(-e, 0f, -e), new Vector3(e, 0f, -e), gap);
                FenceSide(new Vector3(e, 0f, -e), new Vector3(e, 0f, e), gap);
                RoadBlock();
                Checkpoint();
                RailLine();
                DockEnd();
            }

            // One side of the fence, less the road's gap at its middle.
            private void FenceSide(Vector3 a, Vector3 b, float gap)
            {
                Vector3 mid = (a + b) * 0.5f, dir = (b - a).normalized;
                Fence(a, mid - dir * gap, FenceKind.ChainLink, default);
                Fence(mid + dir * gap, b, FenceKind.ChainLink, default);
            }

            // North exit: the police road block across Main St beyond the exit zone, sawhorses on the sidewalks, the cruiser
            // on the verge and the ROAD CLOSED sign facing town.
            private void RoadBlock()
            {
                float z = EdgeLine - 1.1f;
                for (int i = -1; i <= 1; i++)
                    Prop(_town, new Vector3(i * 2.7f, 0f, z), 0f, new Vector3(2.6f, 0.9f, 0.6f), true, (m, r) => DecorProps.JerseyBarrier(m, r, 2.6f, false));
                for (int s = -1; s <= 1; s += 2)
                    Prop(_town, new Vector3(s * 5.3f, 0f, z - 0.9f), s * 8f, new Vector3(1.6f, 1.05f, 0.5f), true, (m, r) => DecorProps.Sawhorse(m, r, 1.6f));
                ParkedCar(_town, new Vector3(9.6f, 0f, 66f), 200f, DecorProps.CarKind.Police, new Color(0.10f, 0.12f, 0.16f));
                Sign(new Vector3(-7.8f, 0f, 66.4f), 180f, "ROAD CLOSED", new Color(0.86f, 0.84f, 0.78f), new Color(0.12f, 0.12f, 0.12f), 0.4f, 1.2f);
            }

            // South exit: the Guard checkpoint on Main St — sandbags either side, striped barriers across the road beyond the
            // zone, the guard booth, an army truck and a floodlight still burning.
            private void Checkpoint()
            {
                for (int s = -1; s <= 1; s += 2)
                    Prop(_town, new Vector3(s * 7.6f, 0f, -59.5f), 90f, new Vector3(4f, 0.8f, 0.5f), true, (m, r) => DecorProps.SandbagWall(m, r, 4f, 5));
                for (int i = -1; i <= 1; i++)
                    Prop(_town, new Vector3(i * 2.7f, 0f, -EdgeLine + 1.1f), 0f, new Vector3(2.6f, 0.9f, 0.6f), true, (m, r) => DecorProps.JerseyBarrier(m, r, 2.6f, true));
                GuardBoothProp(_town, new Vector3(9.8f, 0f, -63.5f), 270f);
                ParkedCar(_town, new Vector3(-10.6f, 0f, -57f), 0f, DecorProps.CarKind.ArmyTruck, Color.white);
                var tower = new Vector3(8.6f, 0f, -54f);
                Prop(_town, tower, 0f, new Vector3(0.4f, 5f, 0.4f), true, (m, r) =>
                {
                    Surf steel = DecorPalette.Metal(new Color(0.3f, 0.32f, 0.28f));
                    m.Box(new Vector3(0f, 0.1f, 0f), new Vector3(0.9f, 0.2f, 0.9f), DecorMaterials.Concrete);
                    m.Cylinder(new Vector3(0f, 0.2f, 0f), 0.08f, 4.8f, 8, steel);
                    m.Box(new Vector3(0f, 5.0f, 0f), new Vector3(0.9f, 0.1f, 0.2f), steel);
                    for (int k = -1; k <= 1; k += 2)
                    {
                        m.Box(new Vector3(k * 0.32f, 5.15f, 0.05f), new Vector3(0.3f, 0.26f, 0.22f), Quaternion.Euler(25f, 0f, 0f), DecorPalette.Matte(new Color(0.2f, 0.22f, 0.18f)));
                        m.Box(new Vector3(k * 0.32f, 5.1f, 0.17f), new Vector3(0.24f, 0.2f, 0.01f), Quaternion.Euler(25f, 0f, 0f), DecorPalette.Glow(new Color(0.92f, 0.95f, 1f)));
                    }
                });
                _town.Light(_town.World, tower + new Vector3(0f, 4.9f, 0.6f), new Color(0.88f, 0.92f, 1f), 16f, 2.4f, false, true);
                Sign(new Vector3(-7.2f, 0f, -52.4f), 0f, "CHECKPOINT", new Color(0.20f, 0.26f, 0.16f), new Color(0.88f, 0.86f, 0.74f), 0.42f, 1.3f);
            }

            // The rail line on its ballast past the depot and over Depot Rd at the Rail Bridge exit (the ballast and sleepers
            // stop at the road, the rails run on across it), a crossbuck either side of the road, and striped barriers
            // across the road at the fence.
            private void RailLine()
            {
                Surf sleeper = DecorMaterials.Planks(new Color(0.30f, 0.22f, 0.15f));
                Surf rail = DecorPalette.Metal(new Color(0.36f, 0.30f, 0.26f));
                float road = DepotRd.Outer + 0.2f;
                for (float z = -GroundHalf; z < GroundHalf - 0.1f; z += 10f)
                {
                    float z1 = Mathf.Min(GroundHalf, z + 10f);
                    if (z < -road) Pave(Rect.MinMaxRect(RailX - 1.9f, z, RailX + 1.9f, Mathf.Min(z1, -road)), 0.02f, DecorMaterials.Gravel);
                    if (z1 > road) Pave(Rect.MinMaxRect(RailX - 1.9f, Mathf.Max(z, road), RailX + 1.9f, z1), 0.02f, DecorMaterials.Gravel);
                    RaidMesh m = _town.Out.Solid(new Vector3(RailX, 0f, (z + z1) * 0.5f));
                    for (int k = -1; k <= 1; k += 2)
                        m.Box(new Vector3(RailX + k * 0.72f, 0.09f, (z + z1) * 0.5f), new Vector3(0.08f, 0.14f, z1 - z), rail);
                    for (float s = z + 0.3f; s < z1; s += 0.65f)
                        if (Mathf.Abs(s) > road) m.Box(new Vector3(RailX, 0.045f, s), new Vector3(2.5f, 0.09f, 0.22f), sleeper);
                }
                Reserve(Rect.MinMaxRect(RailX - 2.2f, -GroundHalf, RailX + 2.2f, GroundHalf));
                for (int s = -1; s <= 1; s += 2)
                    Prop(_town, new Vector3(RailX - 4.6f, 0f, s * (road + 0.7f)), s > 0 ? 180f : 0f, new Vector3(0.3f, 3f, 0.3f), true, (m, r) => Crossbuck(m));
                for (int i = -1; i <= 1; i++)
                    Prop(_town, new Vector3(EdgeLine - 0.4f, 0f, i * 2.7f), 90f, new Vector3(2.6f, 0.9f, 0.6f), true, (m, r) => DecorProps.JerseyBarrier(m, r, 2.6f, true));
            }

            // West exit: Depot Rd runs down to the boat dock: a timber barrier across the road at the fence, the dock's first
            // planks inside it, and the dock's sign.
            private void DockEnd()
            {
                float len = DepotRd.HalfWidth * 2f + 0.4f;
                Prop(_town, new Vector3(-EdgeLine + 0.4f, 0f, 0f), 90f, new Vector3(len, 1.1f, 0.3f), true, (m, r) =>
                {
                    Surf wood = DecorPalette.Matte(new Color(0.40f, 0.32f, 0.22f));
                    for (int i = 0; i <= 3; i++) m.Box(new Vector3(-len * 0.5f + len * i / 3f, 0.55f, 0f), new Vector3(0.14f, 1.1f, 0.14f), wood);
                    m.Box(new Vector3(0f, 0.95f, 0f), new Vector3(len, 0.14f, 0.08f), wood);
                    m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(len, 0.14f, 0.08f), wood);
                    m.Box(new Vector3(0f, 1.04f, 0f), new Vector3(len - 0.1f, 0.05f, 0.1f), DecorPalette.Matte(DecorProps.SnowWhite));
                });
                Sign(new Vector3(-65f, 0f, -DepotRd.Outer - 1.2f), 0f, "BOAT DOCK", new Color(0.16f, 0.24f, 0.30f), new Color(0.86f, 0.86f, 0.80f), 0.4f, 1.2f);
                RaidMesh d = _town.Out.Solid(new Vector3(-68f, 0f, 0f));
                Surf plank = DecorMaterials.Planks(new Color(0.42f, 0.34f, 0.24f));
                for (float x = -EdgeLine + 0.15f; x < -65f; x += 0.35f) d.Box(new Vector3(x, 0.05f, 0f), new Vector3(0.3f, 0.06f, 3.0f), plank);
            }

            private static void Crossbuck(RaidMesh m)
            {
                m.Cylinder(Vector3.zero, 0.06f, 3.0f, 8, DecorPalette.Metal(new Color(0.6f, 0.6f, 0.6f)));
                Surf board = DecorPalette.Matte(new Color(0.86f, 0.86f, 0.82f));
                m.Box(new Vector3(0f, 2.6f, 0.06f), new Vector3(1.2f, 0.2f, 0.03f), Quaternion.Euler(0f, 0f, 35f), board);
                m.Box(new Vector3(0f, 2.6f, 0.06f), new Vector3(1.2f, 0.2f, 0.03f), Quaternion.Euler(0f, 0f, -35f), board);
                Surf hood = DecorPalette.Matte(new Color(0.12f, 0.12f, 0.12f));
                Surf lens = DecorPalette.Glass(new Color(0.35f, 0.06f, 0.05f));
                for (int s = -1; s <= 1; s += 2)
                {
                    m.Box(new Vector3(s * 0.2f, 2.0f, 0.08f), new Vector3(0.22f, 0.22f, 0.12f), hood);
                    m.Box(new Vector3(s * 0.2f, 2.0f, 0.141f), new Vector3(0.14f, 0.14f, 0.01f), lens);
                }
            }
            #endregion

            #region Scenery past the edge
            // Never baked, never collided with: the snowfields, the frozen river and the dock to the west, the truss bridge to
            // the east, the roads and rails running on out of town, and the pine woods all round.
            /// <summary>
            /// Adds non-colliding far scenery — snowfields, frozen river, out-roads and rails, truss bridge, dock and pine woods.
            /// </summary>
            public void Scenery()
            {
                const float far = 130f;
                Surf snow = DecorMaterials.Snow;
                var g = new RaidMesh();
                g.Box(new Vector3(0f, -0.5f, (GroundHalf + far) * 0.5f), new Vector3(far * 2f, 1f, far - GroundHalf), snow);
                g.Box(new Vector3(0f, -0.5f, -(GroundHalf + far) * 0.5f), new Vector3(far * 2f, 1f, far - GroundHalf), snow);
                g.Box(new Vector3((GroundHalf + far) * 0.5f, -0.5f, 0f), new Vector3(far - GroundHalf, 1f, GroundHalf * 2f), snow);
                // West: a snowy bank, then the frozen river.
                g.Box(new Vector3(-GroundHalf - 1.6f, -0.5f, 0f), new Vector3(3.2f, 1f, GroundHalf * 2f), snow);
                g.Box(new Vector3(-(GroundHalf + 3.2f + far) * 0.5f, -0.8f, 0f), new Vector3(far - GroundHalf - 3.2f, 1f, GroundHalf * 2f),
                    DecorPalette.Glass(new Color(0.26f, 0.31f, 0.36f)));
                g.Emit("FarGround", _town.Scenery, Vector3.zero, ShadowCastingMode.Off);

                // The roads run on out of town.
                Surf asphalt = DecorMaterials.DryAsphalt;
                for (float t = GroundHalf; t < far; t += 12f)
                {
                    foreach (Vector3 p in new[] { new Vector3(0f, RoadY, t + 6f), new Vector3(0f, RoadY, -t - 6f) })
                        _town.Far.Flat(p).Flat(p, MainSt.HalfWidth * 2f, 12f, 0f, asphalt);
                    var q = new Vector3(t + 6f, RoadY, 0f);
                    _town.Far.Flat(q).Flat(q, 12f, DepotRd.HalfWidth * 2f, 0f, asphalt);
                }
                // The rails north and south.
                Surf rail = DecorPalette.Metal(new Color(0.36f, 0.30f, 0.26f));
                for (float t = GroundHalf; t < far; t += 10f)
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var p = new Vector3(RailX, 0f, s * (t + 5f));
                        RaidMesh m = _town.Far.Solid(p);
                        m.Box(p + new Vector3(0f, 0.02f, 0f), new Vector3(3.8f, 0.04f, 10f), DecorMaterials.Gravel);
                        for (int k = -1; k <= 1; k += 2) m.Box(p + new Vector3(k * 0.72f, 0.09f, 0f), new Vector3(0.08f, 0.14f, 10f), rail);
                    }

                // East: the steel truss bridge carrying Depot Rd away.
                Surf steel = DecorPalette.Metal(new Color(0.26f, 0.28f, 0.30f));
                for (int s = -1; s <= 1; s += 2)
                {
                    float z = s * 4.6f;
                    RaidMesh m = _town.Far.Solid(new Vector3(90f, 0f, z));
                    m.Box(new Vector3(90f, 0.35f, z), new Vector3(34f, 0.5f, 0.4f), steel);
                    m.Box(new Vector3(90f, 5.0f, z), new Vector3(30f, 0.4f, 0.4f), steel);
                    for (float x = 74f; x <= 106f; x += 4f)
                    {
                        m.Box(new Vector3(x, 2.7f, z), new Vector3(0.3f, 4.6f, 0.3f), steel);
                        if (x < 106f) m.Rod(new Vector3(x, 0.5f, z), new Vector3(x + 4f, 4.9f, z), 0.22f, steel);
                    }
                    m.Box(new Vector3(90f, 5.25f, z), new Vector3(29f, 0.06f, 0.35f), DecorPalette.Matte(DecorProps.SnowWhite));
                }
                for (float x = 76f; x < 106f; x += 6f)
                    _town.Far.Solid(new Vector3(x, 0f, 0f)).Box(new Vector3(x, 5.0f, 0f), new Vector3(0.3f, 0.3f, 9.2f), steel);

                // West: the dock running out onto the ice, and a rowboat frozen in.
                Surf plank = DecorMaterials.Planks(new Color(0.42f, 0.34f, 0.24f));
                RaidMesh dock = _town.Far.Solid(new Vector3(-80f, 0f, 0f));
                for (float x = -GroundHalf; x > -86f; x -= 0.35f)
                    dock.Box(new Vector3(x, 0.05f, 0f), new Vector3(0.3f, 0.06f, 3.0f), plank);
                for (float x = -72f; x > -86f; x -= 3f)
                    for (int s = -1; s <= 1; s += 2)
                        dock.Cylinder(new Vector3(x, -0.8f, s * 1.55f), 0.12f, 1.3f, 6, DecorPalette.Matte(new Color(0.30f, 0.24f, 0.17f)));
                dock.SetFrame(new Vector3(-79f, -0.35f, 4.2f), 70f);
                dock.Box(new Vector3(0f, 0.25f, 0f), new Vector3(1.3f, 0.5f, 3.6f), DecorPalette.Matte(new Color(0.24f, 0.32f, 0.30f)));
                dock.Box(new Vector3(0f, 0.48f, 0f), new Vector3(1.1f, 0.06f, 3.3f), DecorPalette.Matte(DecorProps.SnowWhite));
                dock.ResetFrame();

                // Pine woods, and a few bare trees, all round.
                int pines = 0;
                for (int attempt = 0; attempt < 600 && pines < 110; attempt++)
                {
                    var p = new Vector3(R(_rng, -far + 10f, far - 10f), 0f, R(_rng, -far + 10f, far - 10f));
                    if (Mathf.Abs(p.x) < 74f && Mathf.Abs(p.z) < 74f) continue;
                    if (p.x < -74f) continue;                                 // the river
                    if (Mathf.Abs(p.x) < 8f || Mathf.Abs(p.z) < 8f) continue;  // the roads out
                    if (Mathf.Abs(p.x - RailX) < 4f) continue;                 // the rails
                    RaidMesh m = _town.Far.Solid(p);
                    m.SetFrame(p, R(_rng, 0f, 360f));
                    if (Chance(_rng, 0.8)) DecorProps.Pine(m, _rng, R(_rng, 7f, 13f));
                    else DecorProps.BareTree(m, _rng, R(_rng, 5f, 8f));
                    m.ResetFrame();
                    pines++;
                }
            }
            #endregion

            #region Survivor spawn
            // The survivor starts in one of the reference-photo rooms (the Guard post's operations room or the county office
            // floor), clear of the furniture, at least 35 m from every exit.
            /// <summary>
            /// Picks a valid spawn point from prepared candidates, ensuring distance from exits and warning if too close to furniture.
            /// </summary>
            public void Spawn()
            {
                RaidMapInfo info = _town.Info;
                var valid = new List<int>();
                for (int i = 0; i < _town.SpawnCandidates.Count; i++)
                {
                    Vector3 p = _town.SpawnCandidates[i].pos;
                    bool ok = true;
                    foreach (RaidExit e in info.Exits)
                    {
                        Vector3 d = e.Position - p;
                        d.y = 0f;
                        if (d.magnitude < 35f) ok = false;
                    }
                    if (ok) valid.Add(i);
                }
                if (valid.Count == 0)
                {
                    _town.Warn(_town.SpawnCandidates.Count == 0 ? "no spawn candidates; starting on Main St" : "no spawn far enough from the exits; starting on Main St");
                    info.PlayerSpawn = MainSt.At(20f, -(MainSt.Outer - 1f), 0.6f);
                    info.PlayerSpawnYaw = 0f;
                    return;
                }
                var c = _town.SpawnCandidates[valid[_rng.Next(valid.Count)]];
                foreach (Rect f in c.room.Furniture)
                {
                    float dx = Mathf.Max(f.xMin - c.pos.x, 0f, c.pos.x - f.xMax);
                    float dz = Mathf.Max(f.yMin - c.pos.z, 0f, c.pos.z - f.yMax);
                    if (dx * dx + dz * dz < 1f) _town.Warn("spawn point in " + c.room.Name + " is within 1 m of furniture");
                }
                info.PlayerSpawn = new Vector3(c.pos.x, 0.6f, c.pos.z);
                info.PlayerSpawnYaw = c.yaw;
            }
            #endregion
        }
    }
}
