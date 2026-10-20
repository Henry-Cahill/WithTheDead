using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Mirelock's woods: hundreds of snow-dusted conifers and bare birches crowding every open patch of ground,
    /// undergrowth, fallen logs. Trunks are solid (they block movement, sight and bullets, and carve the navmesh);
    /// canopies are drawn but kept out of the bake and hidden from the isometric view so the ground stays readable.
    /// Merged per chunk, so the whole forest costs a handful of renderers.
    /// </summary>
    internal static class ZoneForest
    {
        public static int TreeCount { get; private set; }
        private static readonly List<Vector3> s_Trees = new List<Vector3>(512);

        /// <summary>Tree positions of the current raid (the stingers and the foragers read them).</summary>
        public static IReadOnlyList<Vector3> Trees => s_Trees;

        public static void Reset()
        {
            TreeCount = 0;
            s_Trees.Clear();
        }

        /// <summary>How deep in the woods a point is, 0 (open) .. 1 (dense).</summary>
        public static float Density(Vector3 p)
        {
            if (s_Trees.Count == 0) return 0f;
            int n = 0;
            for (int i = 0; i < s_Trees.Count; i++)
            {
                float dx = s_Trees[i].x - p.x, dz = s_Trees[i].z - p.z;
                if (dx * dx + dz * dz < 144f) n++;
            }
            return Mathf.Clamp01(n / 14f);
        }

        public static void Build(Transform world, RaidMapInfo map, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            var trunks = new ZoneChunks(24f);
            var canopies = new ZoneChunks(24f);
            var ground = new ZoneChunks(24f);
            Transform colliders = ZoneKit.Child(world, "TreeColliders", Vector3.zero);
            Transform unbaked = ZoneWorld.Unbaked != null ? ZoneWorld.Unbaked : ZoneKit.Child(null, "ZoneUnbaked", Vector3.zero);

            int want = Mathf.RoundToInt(430f * zone.ForestDensity);
            SiteCell mask = ZoneSiteMap.Hard | SiteCell.Paved | SiteCell.Edge;
            float reach = site.Half - 4f;
            Color needles = new Color(0.13f, 0.20f, 0.14f);
            Color snowCap = new Color(0.86f, 0.89f, 0.93f);
            Color birch = new Color(0.78f, 0.77f, 0.72f);

            for (int i = 0; i < want * 6 && TreeCount < want; i++)
            {
                var p = new Vector3(ZoneKit.R(rng, -reach, reach), 0f, ZoneKit.R(rng, -reach, reach));
                if (site.Any(p, 1.7f, mask)) continue;
                if ((p - map.PlayerSpawn).sqrMagnitude < 100f) continue;
                bool conifer = rng.NextDouble() < 0.72;
                float h = conifer ? ZoneKit.R(rng, 7f, 13f) : ZoneKit.R(rng, 6f, 10f);
                float r = conifer ? ZoneKit.R(rng, 0.18f, 0.3f) : ZoneKit.R(rng, 0.12f, 0.18f);
                RaidMesh tm = trunks.At(p);
                tm.Cylinder(p + Vector3.down * 0.1f, r, h * (conifer ? 0.45f : 0.9f), 7, ZoneKit.M(conifer ? ZoneKit.V(ZoneKit.Bark, rng) : ZoneKit.V(birch, rng, 0.05f)));
                RaidMesh cm = canopies.At(p);
                if (conifer)
                {
                    int tiers = 3 + rng.Next(2);
                    for (int t = 0; t < tiers; t++)
                    {
                        float f = t / (float)tiers;
                        float y = h * (0.28f + f * 0.62f);
                        float rad = Mathf.Lerp(2.2f, 0.7f, f) * ZoneKit.R(rng, 0.85f, 1.15f);
                        cm.Ellipsoid(p + Vector3.up * y, new Vector3(rad * 2f, h * 0.22f, rad * 2f), ZoneKit.M(ZoneKit.V(needles, rng, 0.05f)), 7, 4);
                        cm.Ellipsoid(p + Vector3.up * (y + h * 0.07f), new Vector3(rad * 1.5f, h * 0.06f, rad * 1.5f), ZoneKit.M(snowCap), 6, 3);
                    }
                }
                else
                {
                    for (int b = 0; b < 5; b++)
                    {
                        float yaw = (float)rng.NextDouble() * 360f;
                        Vector3 dir = Quaternion.Euler(-35f - (float)rng.NextDouble() * 25f, yaw, 0f) * Vector3.forward;
                        Vector3 a = p + Vector3.up * ZoneKit.R(rng, h * 0.45f, h * 0.8f);
                        cm.Rod(a, a + dir * ZoneKit.R(rng, 1.2f, 2.4f), 0.05f, ZoneKit.M(ZoneKit.V(birch, rng, 0.06f)), 4);
                    }
                }
                ZoneKit.Solid(colliders, p + Vector3.up * 2f, new Vector3(r * 2.2f, 4f, r * 2.2f), Quaternion.identity);
                site.MarkCircle(p, 1.2f, SiteCell.Solid);
                s_Trees.Add(p);
                TreeCount++;
            }

            // Undergrowth, ferns poking through the snow, and fallen logs.
            for (int i = 0; i < want; i++)
            {
                var p = new Vector3(ZoneKit.R(rng, -reach, reach), 0f, ZoneKit.R(rng, -reach, reach));
                if (site.Any(p, 0.6f, ZoneSiteMap.Hard | SiteCell.Paved)) continue;
                RaidMesh g = ground.At(p);
                if (rng.NextDouble() < 0.8)
                    g.Ellipsoid(p + Vector3.up * 0.25f, new Vector3(ZoneKit.R(rng, 0.8f, 1.8f), ZoneKit.R(rng, 0.4f, 0.8f), ZoneKit.R(rng, 0.8f, 1.8f)), ZoneKit.M(ZoneKit.V(new Color(0.16f, 0.22f, 0.15f), rng)), 6, 3);
                else
                    g.FlatBlob(p + Vector3.up * 0.01f, ZoneKit.R(rng, 1f, 2.5f), ZoneKit.R(rng, 1f, 2f), (float)rng.NextDouble() * 360f, 0.3f, rng, ZoneKit.M(new Color(0.20f, 0.24f, 0.18f)));
            }
            int logs = Mathf.RoundToInt(22f * zone.ForestDensity);
            for (int i = 0; i < logs * 4 && logs > 0; i++)
            {
                var p = new Vector3(ZoneKit.R(rng, -reach, reach), 0f, ZoneKit.R(rng, -reach, reach));
                if (site.Any(p, 2.2f, mask)) continue;
                float yaw = (float)rng.NextDouble() * 180f;
                float len = ZoneKit.R(rng, 3f, 6f);
                trunks.At(p).CylinderX(p + Vector3.up * 0.28f, 0.28f, len, 7, ZoneKit.M(ZoneKit.V(ZoneKit.Bark, rng)), yaw);
                ZoneKit.Solid(colliders, p + Vector3.up * 0.28f, new Vector3(len, 0.56f, 0.56f), Quaternion.Euler(0f, yaw, 0f));
                site.MarkCircle(p, len * 0.5f, SiteCell.Solid);
                logs--;
            }

            trunks.Emit(world, "ForestTrunks", ShadowCastingMode.On);
            List<MeshRenderer> tops = canopies.Emit(unbaked, "ForestCanopy", ShadowCastingMode.On);
            for (int i = 0; i < tops.Count; i++) CeilingVisibility.Register(tops[i]);   // seen-through from above, like roofs
            ground.Emit(unbaked, "ForestFloor", ShadowCastingMode.Off);
        }
    }

    /// <summary>
    /// The Spillway's industrial heart: pipe runs on trestles, tanks, rusted shipping containers, a section of the old
    /// dam wall along the north edge, hazard paint and steam venting from broken lines.
    /// </summary>
    internal static class ZoneIndustry
    {
        public static bool Wants(ZoneDef zone) => zone != null && zone.Id == "spillway";

        public static void Build(Transform world, RaidMapInfo map, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            Transform unbaked = ZoneWorld.Unbaked;
            float half = site.Half;

            // The dam wall: a concrete cliff just inside the north edge, broken by the spillway channel.
            var wall = new SiteBuilder(world, "DamWall", new Vector3(0f, 0f, half - 3f), 0f, rng, cutaway: false);
            for (float x = -half + 4f; x < half - 4f; x += 8f)
            {
                if (Mathf.Abs(x) < 6f) continue;
                Vector3 c = new Vector3(x, 0f, half - 3f);
                if (site.Any(c, 3f, SiteCell.Reserved)) continue;
                wall.At(wall.Solid, new Vector3(x, 0f, 0f), 0f, m => m.Box(new Vector3(0f, 6f, 0f), new Vector3(8.1f, 12f, 2.2f), ZoneKit.M(ZoneKit.V(ZoneKit.ConcreteDark, rng))));
                // Add a few rust‑stained bolt heads on the concrete block (1‑4 bolts per segment).
                int boltCount = rng.Next(1, 5);
                for (int b = 0; b < boltCount; b++)
                {
                    // Random offset within the block footprint (≈ half‑size of the concrete segment).
                    float offsetX = (float)rng.NextDouble() * 7f - 3.5f; // -3.5 .. 3.5
                    float offsetZ = (float)rng.NextDouble() * 1.8f - 0.9f; // -0.9 .. 0.9 (depth half)
                    Vector3 boltPos = new Vector3(x + offsetX, 0.065f, offsetZ);
                    wall.At(wall.Solid, boltPos, 0f,
                        m => m.Cylinder(Vector3.zero, 0.015f, 0.02f, 8, ZoneKit.Mt(ZoneKit.RustDark)));
                }
                wall.Collider(new Vector3(x, 6f, 0f), new Vector3(8f, 12f, 2.2f));
                wall.At(wall.Flat, new Vector3(x, 0f, -1.2f), 0f, m => m.Box(new Vector3(0f, 2f, 0f), new Vector3(8f, 0.3f, 0.02f), DecorMaterials.Hazard));
            }
            wall.Finish();

            // Pipe runs on trestles.
            for (int run = 0; run < 4; run++)
            {
                float len = ZoneKit.R(rng, 18f, 34f);
                float yaw = rng.NextDouble() < 0.5 ? 0f : 90f;
                if (!site.TryFind(rng, new Vector2(len, 3f), yaw, 1f, ZoneSiteMap.Hard | SiteCell.Edge, 8f, null, 60, out Vector3 at)) continue;
                var sb = new SiteBuilder(world, "PipeRun", at, yaw, rng, cutaway: false);
                float h = ZoneKit.R(rng, 2.6f, 3.4f);
                for (float x = -len * 0.5f; x <= len * 0.5f; x += 4f)
                {
                    sb.Solid.Box(new Vector3(x, h * 0.5f, 0f), new Vector3(0.25f, h, 0.25f), ZoneKit.Mt(ZoneKit.RustDark));
                    sb.Collider(new Vector3(x, h * 0.5f, 0f), new Vector3(0.25f, h, 0.25f));
                }
                sb.Upper.CylinderX(new Vector3(0f, h + 0.35f, -0.4f), 0.35f, len, 10, ZoneKit.Mt(ZoneKit.V(ZoneKit.Rust, rng)));
                sb.Upper.CylinderX(new Vector3(0f, h + 0.25f, 0.45f), 0.25f, len, 8, ZoneKit.Mt(ZoneKit.Galvanised));
                sb.Finish();
                site.Claim(at, new Vector2(len, 3f), yaw, 0.5f);
                if (rng.NextDouble() < 0.7) ZoneKit.Plume(unbaked, sb.W(ZoneKit.R(rng, -len * 0.4f, len * 0.4f), h + 0.5f, -0.4f), false, 1.4f, 6f, 2.5f, new Color(0.85f, 0.85f, 0.82f, 0.5f));
            }

            // Tanks and containers.
            for (int i = 0; i < 7; i++)
            {
                bool tank = rng.NextDouble() < 0.45;
                Vector2 size = tank ? new Vector2(7f, 7f) : new Vector2(6.1f, 2.5f);
                float yaw = rng.Next(4) * 90f;
                if (!site.TryFind(rng, size, yaw, 1.5f, ZoneSiteMap.Hard | SiteCell.Paved | SiteCell.Edge, 6f, null, 50, out Vector3 at)) continue;
                var sb = new SiteBuilder(world, tank ? "Tank" : "Container", at, yaw, rng, cutaway: false);
                if (tank)
                {
                    float r = ZoneKit.R(rng, 2.6f, 3.3f), h = ZoneKit.R(rng, 5f, 8f);
                    sb.Solid.Cylinder(Vector3.zero, r, h, 16, ZoneKit.Mt(ZoneKit.V(new Color(0.55f, 0.55f, 0.50f), rng)));
                    sb.Solid.Cylinder(new Vector3(0f, h, 0f), r * 0.98f, 0.4f, 16, ZoneKit.Mt(ZoneKit.RustDark));
                    sb.Flat.Box(new Vector3(0f, h * 0.4f, -r - 0.01f), new Vector3(r * 1.2f, 0.5f, 0.02f), DecorMaterials.Hazard);
                    sb.Collider(new Vector3(0f, h * 0.5f, 0f), new Vector3(r * 1.7f, h, r * 1.7f));
                    if (rng.NextDouble() < 0.5) sb.Storage("Valve house locker", "spill_locker", new Vector3(r + 0.6f, 1f, 0f), ContainerType.ToolBench, 4);
                }
                else
                {
                    Color c = ZoneKit.V(rng.NextDouble() < 0.5 ? new Color(0.35f, 0.17f, 0.12f) : new Color(0.20f, 0.30f, 0.38f), rng);
                    sb.Solid.Box(new Vector3(0f, 1.3f, 0f), new Vector3(6f, 2.6f, 2.44f), ZoneKit.Mt(c));
                    sb.Collider(new Vector3(0f, 1.3f, 0f), new Vector3(6f, 2.6f, 2.44f));
                    if (rng.NextDouble() < 0.6) sb.Storage("Shipping container", "spill_container", new Vector3(3.2f, 1f, 0f), ContainerType.Crate, 5);
                }
                sb.Finish();
                site.Claim(at, size, yaw, 0.5f);
            }

            // Steam from the broken lines and a few sodium lamps.
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(ZoneKit.R(rng, -half + 8f, half - 8f), 0f, ZoneKit.R(rng, -half + 8f, half - 8f));
                if (site.Any(p, 2f, ZoneSiteMap.Hard)) continue;
                ZoneKit.Plume(unbaked, p + Vector3.up * 0.2f, false, 1.1f, 5f, 2f, new Color(0.9f, 0.9f, 0.86f, 0.45f));
                if (i % 2 == 0) ZoneFlameLight.Spawn(unbaked, p + Vector3.up * 4.5f, new Color(1f, 0.62f, 0.25f), 14f, 1.4f, 0.05f);
            }
        }
    }
}
