using UnityEngine;

namespace CloneZ
{
    // The map edge and what lies past it (the exit roads it opens onto are defined in RaidMapBuilder.Town.cs): the
    // barrier around the clamp line — runs of rusty corrugated sheet, plank fence, wire fence and banked snow, broken
    // only where the four exit roads leave town and closed at the east and west gaps by a low barrier — then the
    // never-baked scenery beyond (the snowfields, the pine woods, the frozen river and dock, the truss bridge, the
    // roads and rails running on out), closing with the survivor's in-room spawn.
    public static partial class RaidMapBuilder
    {
        #region Map edge
        // A barrier just outside the clamp line (PlayerController keeps the survivor within ±HalfSize):
        // runs of rusty corrugated sheet, plank fence, wire fence and banked snow, broken only where the
        // four exit roads leave town (each blocked further out by its own barricade).
        private static void BuildEdge(RaidTown town)
        {
            System.Random rng = town.Rng;
            for (int side = 0; side < 4; side++)
            {
                float t = -EdgeLine;
                while (t < EdgeLine - 0.5f)
                {
                    float len = R(rng, 4f, 7.5f);
                    float a = t, b = Mathf.Min(EdgeLine, t + len);
                    t = b;
                    // Leave the exit road open.
                    if (b > -6.6f && a < 6.6f)
                    {
                        if (a < -6.6f) EdgeSegment(town, side, a, -6.6f, rng);
                        if (b > 6.6f) EdgeSegment(town, side, 6.6f, b, rng);
                        continue;
                    }
                    EdgeSegment(town, side, a, b, rng);
                }
            }
            // The east and west gaps get a low barrier at the line (north and south have their barricades).
            Prop(town, new Vector3(EdgeLine - 0.3f, 0f, 0f), 90f, new Vector3(8f, 0.9f, 0.6f), true, (m, r) =>
            {
                Vector3 o = m.FrameOrigin;
                Quaternion q = m.FrameRotation;
                for (int i = -1; i <= 1; i++)
                {
                    m.SetFrame(o + q * new Vector3(i * 2.7f, 0f, 0f), q);
                    DecorProps.JerseyBarrier(m, r, 2.6f, true);
                }
            });
            Prop(town, new Vector3(-EdgeLine + 0.3f, 0f, 0f), 90f, new Vector3(8f, 1.1f, 0.3f), true, (m, r) =>
            {
                Surf wood = DecorPalette.Matte(new Color(0.40f, 0.32f, 0.22f));
                for (int i = -4; i <= 4; i++)
{
    m.Box(new Vector3(i * 1.0f, 0.55f, 0f), new Vector3(0.12f, 1.1f, 0.12f), wood);
    // Detailed visual upgrade for the centre post of the wooden low‑barrier – metal cap, rust, rope loop,
    // paint streaks, and a faded sign to give a lived‑in, winter‑weathered look.
    if (i == 0)
    {
        // 1️⃣ Metal cap on top of the centre post (0.14 m × 0.02 m × 0.14 m)
        Surf metalCap = DecorPalette.Metal(new Color(0.50f, 0.45f, 0.40f));
        m.Box(new Vector3(0f, 1.11f, 0f), new Vector3(0.14f, 0.02f, 0.14f), metalCap);
        // 2️⃣ Small rust patch at the base of the centre post (0.20 m × 0.02 m × 0.02 m)
        Surf rust = DecorPalette.Matte(new Color(0.55f, 0.30f, 0.20f));
        m.Box(new Vector3(0f, 0.01f, 0f), new Vector3(0.20f, 0.02f, 0.02f), rust);
        // 3️⃣ Thin rope‑loop around the post at mid‑height (cylinder, radius 0.04 m, height 0.04 m)
        Surf rope = DecorPalette.Matte(new Color(0.28f, 0.22f, 0.18f));
        m.Cylinder(new Vector3(0f, 0.70f, 0f), 0.04f, 0.04f, 6, rope);
        // 4️⃣ Two weathered paint streaks on the lower horizontal beam (each 0.02 m × 0.005 m × 0.08 m)
        Surf streak = DecorPalette.Matte(new Color(0.35f, 0.28f, 0.20f));
        // left streak
        m.Box(new Vector3(-0.30f, 0.96f, 0f), new Vector3(0.02f, 0.005f, 0.08f), streak);
        // right streak
        m.Box(new Vector3(0.30f, 0.96f, 0f), new Vector3(0.02f, 0.005f, 0.08f), streak);
        // 5️⃣ A faded wooden “hand‑sign” hanging from the centre post (0.60 m × 0.80 m × 0.02 m)
        Surf signWood = DecorPalette.Matte(new Color(0.38f, 0.32f, 0.24f));
        m.Box(new Vector3(0.35f, 1.25f, 0f), new Vector3(0.60f, 0.80f, 0.02f), signWood);
    }
}
                m.Box(new Vector3(0f, 0.95f, 0f), new Vector3(8.2f, 0.1f, 0.08f), wood);
                m.Box(new Vector3(0f, 0.55f, 0f), new Vector3(8.2f, 0.1f, 0.08f), wood);
                m.Box(new Vector3(0f, 1.03f, 0f), new Vector3(8.0f, 0.05f, 0.1f), DecorPalette.Matte(DecorProps.SnowWhite));
            });
        }

        // One barrier run along a side: 0 = north (z+), 1 = east (x+), 2 = south (z-), 3 = west (x-).
        private static void EdgeSegment(RaidTown town, int side, float a, float b, System.Random rng)
        {
            float len = b - a;
            if (len < 0.5f) return;
            float mid = (a + b) * 0.5f;
            Vector3 c;
            float yaw;
            switch (side)
            {
                case 0: c = new Vector3(mid, 0f, EdgeLine); yaw = 180f; break;
                case 1: c = new Vector3(EdgeLine, 0f, mid); yaw = 270f; break;
                case 2: c = new Vector3(mid, 0f, -EdgeLine); yaw = 0f; break;
                default: c = new Vector3(-EdgeLine, 0f, mid); yaw = 90f; break;
            }
            int kind = rng.Next(4);
            RaidMesh m = town.Out.Solid(c);
            m.SetFrame(c, yaw);
            float h;
            switch (kind)
            {
                case 0:
                {
                    h = 2.4f;
                    Surf post = DecorPalette.Matte(new Color(0.30f, 0.24f, 0.18f));
                    Surf sheet = DecorMaterials.Corrugated(new Color(0.46f, 0.47f, 0.46f));
                    for (float x = -len * 0.5f; x <= len * 0.5f + 0.01f; x += 2.4f)
                        m.Box(new Vector3(x, 1.2f, -0.08f), new Vector3(0.12f, 2.4f, 0.12f), post);
                    for (float x = -len * 0.5f + 0.6f; x < len * 0.5f; x += 1.1f)
                    {
                        float sh = R(rng, 2.0f, 2.45f);
                        m.Box(new Vector3(x, sh * 0.5f, 0f), new Vector3(1.2f, sh, 0.03f), Quaternion.Euler(R(rng, -3f, 3f), 0f, R(rng, -2f, 2f)), sheet);
                    }
                    // occasional rust spots on the corrugated sheet
                    Surf rust = DecorPalette.Matte(new Color(0.55f, 0.30f, 0.20f));
for (float x = -len * 0.5f + 0.3f; x < len * 0.5f; x += R(rng, 2f, 4f))
{
    float w = R(rng, 0.2f, 0.4f);
    // small matte box placed flush on sheet surface to simulate rust stain
    m.Box(new Vector3(x, 0.01f, 0f), new Vector3(w, 0.02f, 0.02f), rust);
}
// occasional snow drifts sitting on the corrugated sheet surface
for (float x = -len * 0.5f + 0.8f; x < len * 0.5f; x += R(rng, 3f, 5f))
{
    float w = R(rng, 0.3f, 0.6f);
    m.Box(new Vector3(x, 2.45f, 0f), new Vector3(w, 0.05f, 0.3f), DecorMaterials.Snow);
}
                    break;
                }
                case 1:
                {
                    h = 2.0f;
                    Surf post = DecorPalette.Matte(new Color(0.32f, 0.25f, 0.17f));
                    for (float x = -len * 0.5f; x <= len * 0.5f + 0.01f; x += 2.2f)
                        {
                            m.Box(new Vector3(x, 1.05f, -0.06f), new Vector3(0.12f, 2.1f, 0.12f), post);
                            // metal cap on top of post
m.Cylinder(new Vector3(x, 2.6f, -0.06f), 0.04f, 0.05f, 6, DecorPalette.Metal(new Color(0.50f, 0.45f, 0.40f)));
                             // occasional rust stain above cap (30% chance)
                             if (rng.NextDouble() < 0.3)
                             {
                                 float w = R(rng, 0.05f, 0.08f);
                                 m.Box(new Vector3(x, 2.62f, -0.06f), new Vector3(w, 0.01f, 0.01f), DecorPalette.Matte(new Color(0.55f, 0.30f, 0.20f)));
                             }
                             // thin wooden rope‑tie between posts
                             Surf rope = DecorPalette.Matte(new Color(0.28f, 0.22f, 0.18f));
                             m.Box(new Vector3(x, 0.5f, -0.06f), new Vector3(0.04f, 0.4f, 0.04f), rope);
                        }
                    m.Box(new Vector3(0f, 1.0f, 0f), new Vector3(len, 2.0f, 0.05f), DecorMaterials.Planks(new Color(0.46f, 0.38f, 0.28f)));
                    m.Box(new Vector3(0f, 2.03f, 0f), new Vector3(len, 0.05f, 0.1f), DecorPalette.Matte(DecorProps.SnowWhite));
                    break;
                }
                case 2:
                {
                    h = 2.2f;
                    Surf steel = DecorPalette.Metal(new Color(0.48f, 0.50f, 0.52f));
                    for (float x = -len * 0.5f; x <= len * 0.5f + 0.01f; x += 2.5f)
                        { m.Cylinder(new Vector3(x, 0f, 0f), 0.04f, 2.2f, 6, steel); if (rng.NextDouble() < 0.4) { Surf bolt = DecorPalette.Metal(new Color(0.55f, 0.53f, 0.48f)); m.Cylinder(new Vector3(x, 2.2f, 0f), 0.015f, 0.06f, 6, bolt); } }
                    m.Box(new Vector3(0f, 2.15f, 0f), new Vector3(len, 0.05f, 0.05f), steel);
                    for (int w = 0; w < 8; w++)
                        m.Box(new Vector3(0f, 0.2f + w * 0.26f, 0f), new Vector3(len, 0.015f, 0.015f), steel);
                    for (float x = -len * 0.5f; x < len * 0.5f - 0.3f; x += 0.3f)
                        m.Rod(new Vector3(x, 2.2f, 0f), new Vector3(x + 0.3f, 2.4f, R(rng, -0.12f, 0.12f)), 0.012f, steel);
                    break;
                }
                default:
                {
                    h = 1.9f;
                    for (float x = -len * 0.5f; x < len * 0.5f; x += R(rng, 1.4f, 2.2f))
                        m.Ellipsoid(new Vector3(x, 0f, R(rng, -0.2f, 0.4f)), new Vector3(R(rng, 2.2f, 3.2f), R(rng, 3.0f, 4.4f), R(rng, 1.4f, 2.0f)),
                            Quaternion.Euler(0f, R(rng, -20f, 20f), 0f), DecorPalette.Matte(rng.NextDouble() < 0.5 ? DecorProps.SnowWhite : DecorProps.SnowShade), 8, 4);
                    break;
                }
            }
            m.ResetFrame();
            town.PropCollider(c + Vector3.up * 1.25f, new Vector3(len, Mathf.Max(2.5f, h), 0.5f), yaw);
        }
        #endregion

        #region Scenery past the edge
        // Never baked, never collided with: the snowfields, pine woods, the frozen river and dock to the
        // west, the bridge to the east, and the roads running on out of town.
        private static void BuildScenery(RaidTown town)
        {
            System.Random rng = town.Rng;
            const float far = 130f;
            Surf snow = DecorMaterials.Snow;
            RaidMesh g = new RaidMesh();
            g.Box(new Vector3(0f, -0.5f, (GroundHalf + far) * 0.5f), new Vector3(far * 2f, 1f, far - GroundHalf), snow);
            g.Box(new Vector3(0f, -0.5f, -(GroundHalf + far) * 0.5f), new Vector3(far * 2f, 1f, far - GroundHalf), snow);
            g.Box(new Vector3((GroundHalf + far) * 0.5f, -0.5f, 0f), new Vector3(far - GroundHalf, 1f, GroundHalf * 2f), snow);
            // West: a snowy bank, then the frozen river.
            g.Box(new Vector3(-GroundHalf - 1.6f, -0.5f, 0f), new Vector3(3.2f, 1f, GroundHalf * 2f), snow);
            g.Box(new Vector3(-(GroundHalf + 3.2f + far) * 0.5f, -0.8f, 0f), new Vector3(far - GroundHalf - 3.2f, 1f, GroundHalf * 2f), DecorPalette.Glass(new Color(0.26f, 0.31f, 0.36f)));
            g.Emit("FarGround", town.Scenery, Vector3.zero, UnityEngine.Rendering.ShadowCastingMode.Off);

            // The roads run on out of town.
            Surf asphalt = DecorMaterials.Asphalt;
            for (float z = GroundHalf; z < far; z += 12f)
            {
                var p = new Vector3(0f, 0.012f, z + 6f);
                town.Far.Flat(p).Flat(p, 8f, 12f, 0f, asphalt);
                var q = new Vector3(0f, 0.012f, -z - 6f);
                town.Far.Flat(q).Flat(q, 8f, 12f, 0f, asphalt);
            }
            for (float x = GroundHalf; x < far; x += 12f)
            {
                var p = new Vector3(x + 6f, 0.012f, 0f);
                town.Far.Flat(p).Flat(p, 12f, 8f, 0f, asphalt);
            }
            // Rails north and south.
            for (float z = GroundHalf; z < far; z += 10f)
                for (int s = -1; s <= 1; s += 2)
                {
                    var p = new Vector3(66f, 0f, s * (z + 5f));
                    RaidMesh m = town.Far.Solid(p);
                    m.Box(p + new Vector3(0f, 0.02f, 0f), new Vector3(3.8f, 0.04f, 10f), DecorMaterials.Gravel);
                    m.Box(p + new Vector3(-0.72f, 0.09f, 0f), new Vector3(0.08f, 0.14f, 10f), DecorPalette.Metal(new Color(0.36f, 0.30f, 0.26f)));
                    m.Box(p + new Vector3(0.72f, 0.09f, 0f), new Vector3(0.08f, 0.14f, 10f), DecorPalette.Metal(new Color(0.36f, 0.30f, 0.26f)));
                }

            // East: a steel truss bridge carries Depot Rd away.
            Surf steel = DecorPalette.Metal(new Color(0.26f, 0.28f, 0.30f));
            for (int s = -1; s <= 1; s += 2)
            {
                float z = s * 4.6f;
                var c = new Vector3(90f, 0f, z);
                RaidMesh m = town.Far.Solid(c);
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
                town.Far.Solid(new Vector3(x, 0f, 0f)).Box(new Vector3(x, 5.0f, 0f), new Vector3(0.3f, 0.3f, 9.2f), steel);

            // West: the dock running out onto the ice, and a rowboat frozen in.
            Surf plank = DecorMaterials.Planks(new Color(0.42f, 0.34f, 0.24f));
            RaidMesh dock = town.Far.Solid(new Vector3(-80f, 0f, 0f));
            for (float x = -GroundHalf; x > -86f; x -= 0.35f)
                dock.Box(new Vector3(x, 0.05f, 0f), new Vector3(0.3f, 0.06f, 3.0f), plank);
            for (float x = -72f; x > -86f; x -= 3f)
                for (int s = -1; s <= 1; s += 2)
                    dock.Cylinder(new Vector3(x, -0.8f, s * 1.55f), 0.12f, 1.3f, 6, DecorPalette.Matte(new Color(0.30f, 0.24f, 0.17f)));
            dock.SetFrame(new Vector3(-79f, -0.35f, 4.2f), 70f);
            dock.Box(new Vector3(0f, 0.25f, 0f), new Vector3(1.3f, 0.5f, 3.6f), DecorPalette.Matte(new Color(0.24f, 0.32f, 0.30f)));
            dock.Box(new Vector3(0f, 0.48f, 0f), new Vector3(1.1f, 0.06f, 3.3f), DecorPalette.Matte(DecorProps.SnowWhite));
            dock.ResetFrame();

            // Pine woods and a few bare trees all round.
            int pines = 0;
            for (int attempt = 0; attempt < 600 && pines < 110; attempt++)
            {
                var p = new Vector3(R(rng, -far + 10f, far - 10f), 0f, R(rng, -far + 10f, far - 10f));
                if (Mathf.Abs(p.x) < 74f && Mathf.Abs(p.z) < 74f) continue;
                if (p.x < -74f) continue;                                 // the river
                if (Mathf.Abs(p.x) < 8f || Mathf.Abs(p.z) < 8f) continue;  // the roads out
                if (Mathf.Abs(p.x - 66f) < 4f) continue;                   // the rails
                RaidMesh m = town.Far.Solid(p);
                m.SetFrame(p, R(rng, 0f, 360f));
                if (rng.NextDouble() < 0.8) DecorProps.Pine(m, rng, R(rng, 7f, 13f));
                else DecorProps.BareTree(m, rng, R(rng, 5f, 8f));
                m.ResetFrame();
                pines++;
            }
        }
        #endregion

        #region Survivor spawn
        // The survivor starts in one of the reference-photo rooms (the Guard post's operations room or
        // the county office floor), standing clear of the furniture, at least 35 m from every exit.
        private static void PickSpawn(RaidTown town)
        {
            var valid = new System.Collections.Generic.List<int>();
            for (int i = 0; i < town.SpawnCandidates.Count; i++)
            {
                Vector3 p = town.SpawnCandidates[i].pos;
                bool ok = true;
                foreach (RaidExit e in town.Info.Exits)
                {
                    Vector3 d = e.Position - p;
                    d.y = 0f;
                    if (d.magnitude < 35f) ok = false;
                }
                if (ok) valid.Add(i);
            }
            if (town.SpawnCandidates.Count == 0)
            {
                town.Warn("no spawn candidates; starting on Main St");
                town.Info.PlayerSpawn = new Vector3(-6f, 0.6f, 20f);
                return;
            }
            if (valid.Count == 0)
            {
                town.Warn("no valid spawn far enough from exits; using default");
                town.Info.PlayerSpawn = new Vector3(-6f, 0.6f, 20f);
                town.Info.PlayerSpawnYaw = 0f;
                return;
            }
            int pick = valid[town.Rng.Next(valid.Count)];
            var c = town.SpawnCandidates[pick];
            foreach (Rect f in c.room.Furniture)
            {
                float dx = Mathf.Max(f.xMin - c.pos.x, 0f, c.pos.x - f.xMax);
                float dz = Mathf.Max(f.yMin - c.pos.z, 0f, c.pos.z - f.yMax);
                if (dx * dx + dz * dz < 1f) town.Warn("spawn point in " + c.room.Name + " is within 1 m of furniture");
            }
            town.Info.PlayerSpawn = new Vector3(c.pos.x, 0.6f, c.pos.z);
            town.Info.PlayerSpawnYaw = c.yaw;
        }
        #endregion
    }
}
