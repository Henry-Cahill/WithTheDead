using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The handcrafted anchors of each zone (Metro's set pieces, used sparingly): a crashed helicopter, a drowned
    /// chapel, a hunters' hide, the cemetery the Returned walk home from, a cooling tower, the Hollow Choir's
    /// sanctum. Each is built from primitives at clear ground found by the site map, carries good loot, a light
    /// and a visit trigger (quests and map knowledge use these), and publishes its points for the AI. This also
    /// publishes the raid's landing zones (N, E, S, W), vehicle spots and warfare outposts.
    /// </summary>
    internal static class ZoneLandmarks
    {
        public static string LastSummary { get; private set; } = "";

        public static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "crashed_helicopter", "the crashed helicopter" }, { "warden_checkpoint", "the Warden checkpoint" },
            { "drowned_chapel", "the drowned chapel" }, { "hunters_hide", "the hunters' hide" },
            { "returned_cemetery", "the cemetery of the Returned" }, { "cooling_tower", "the cooling tower" },
            { "choir_sanctum", "the Choir's sanctum" }, { "lodestar_site", "the Lodestar's basin" },
            { "quarantine_tent", "the quarantine tent" },
        };

        public static string NameOf(string id) => id != null && Names.TryGetValue(id, out string n) ? n : id;

        public static void Build(Transform world, RaidMapInfo map, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            int built = 0;
            foreach (string id in zone.Landmarks)
            {
                if (id == "lodestar_site" || id == "quarantine_tent") continue;
                if (id == "warden_checkpoint" && ZoneCordon.HasGate) { built++; continue; }
                Vector2 size = SizeOf(id);
                float yaw = rng.Next(4) * 90f;
                float edge = id == "cooling_tower" ? 18f : 10f;
                if (!site.TryFind(rng, size, yaw, 2f, ZoneSiteMap.Hard | SiteCell.Edge, edge,
                        p => Vector3.Distance(p, map.PlayerSpawn) < 30f ? -1f : (float)rng.NextDouble() + Vector3.Distance(p, map.PlayerSpawn) / 140f,
                        120, out Vector3 at))
                    continue;
                var sb = new SiteBuilder(world, "Landmark_" + id, at, yaw, rng);
                switch (id)
                {
                    case "crashed_helicopter": CrashedHelicopter(sb); break;
                    case "warden_checkpoint": Checkpoint(sb); break;
                    case "drowned_chapel": DrownedChapel(sb); break;
                    case "hunters_hide": HuntersHide(sb); break;
                    case "returned_cemetery": Cemetery(sb); break;
                    case "cooling_tower": CoolingTower(sb); break;
                    case "choir_sanctum": ChoirSanctum(sb); break;
                    default: continue;
                }
                sb.Finish();
                site.Claim(at, size, yaw, 1f);
                WorldMarkers.Add(WorldMarkers.Landmark(id), at);
                LocationVisitTrigger.Attach(sb.Root.gameObject, id, Mathf.Max(size.x, size.y) * 0.6f + 4f);
                built++;
            }
            LastSummary = built + " landmarks";
        }

        private static Vector2 SizeOf(string id)
        {
            switch (id)
            {
                case "cooling_tower": return new Vector2(20f, 20f);
                case "returned_cemetery": return new Vector2(16f, 14f);
                case "drowned_chapel": return new Vector2(12f, 14f);
                case "choir_sanctum": return new Vector2(14f, 14f);
                default: return new Vector2(12f, 10f);
            }
        }

        #region Set pieces
        private static readonly Color Olive = new Color(0.27f, 0.31f, 0.22f);
        private static readonly Color Scorch = new Color(0.06f, 0.05f, 0.05f);

        private static void CrashedHelicopter(SiteBuilder sb)
        {
            System.Random r = sb.Rng;
            sb.Flat.FlatBlob(new Vector3(0f, 0.02f, 0f), 6f, 4.5f, 0f, 0.4f, r, ZoneKit.M(Scorch));
            sb.At(sb.Solid, new Vector3(0f, 0f, 0f), 18f, m =>
            {
                m.Ellipsoid(new Vector3(0f, 1.3f, 0f), new Vector3(2.6f, 2.4f, 7.5f), Quaternion.Euler(0f, 0f, 24f), ZoneKit.Mt(Olive), 10, 6);
                m.Ellipsoid(new Vector3(0f, 1.5f, 3f), new Vector3(1.8f, 1.5f, 1.8f), Quaternion.Euler(0f, 0f, 24f), ZoneKit.Gs(new Color(0.2f, 0.25f, 0.28f)), 8, 5);
                m.CylinderX(new Vector3(0.6f, 0.9f, -5.6f), 0.35f, 4.5f, 8, ZoneKit.Mt(Olive), 90f);
            });
            sb.Collider(new Vector3(0f, 1.2f, 0f), new Vector3(2.8f, 2.4f, 8f), 18f);
            for (int i = 0; i < 3; i++)
            {
                float yaw = (float)r.NextDouble() * 360f;
                Vector3 p = new Vector3(ZoneKit.R(r, -4f, 4f), 0.05f, ZoneKit.R(r, -4f, 4f));
                sb.At(sb.Flat, p, yaw, m => m.Box(new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.05f, 5.5f), ZoneKit.Mt(new Color(0.15f, 0.16f, 0.14f))));
            }
            ZoneKit.Plume(sb.Root, sb.W(0.5f, 1.8f, -1f), true, 2.2f, 7f, 3f, new Color(0.18f, 0.17f, 0.16f, 0.55f));
            ZoneKit.Fire(sb.Root, sb.W(-1.2f, 0.2f, -2.5f), 0.8f);
            ZoneFlameLight.Spawn(sb.Root, sb.W(-1.2f, 0.9f, -2.5f), new Color(1f, 0.5f, 0.2f), 11f, 2.2f, 0.6f);
            sb.Storage("Crash crate", "lm_crash_crate", new Vector3(3f, 0.5f, 1.5f), ContainerType.GunLocker, 5);
            sb.Storage("Medical case", "lm_crash_med", new Vector3(-3f, 0.5f, 2f), ContainerType.MedCabinet, 4);
            sb.Solid.Box(new Vector3(3f, 0.35f, 1.5f), new Vector3(1.2f, 0.7f, 0.8f), ZoneKit.M(Olive));
            sb.Collider(new Vector3(3f, 0.35f, 1.5f), new Vector3(1.2f, 0.7f, 0.8f));
        }

        private static void Checkpoint(SiteBuilder sb)
        {
            for (int i = 0; i < 6; i++)
            {
                sb.Solid.Box(new Vector3(-3f + i * 1.2f, 0.45f, -3f), new Vector3(1.1f, 0.9f, 0.6f), ZoneKit.M(new Color(0.48f, 0.43f, 0.33f)));
                sb.Solid.Box(new Vector3(-3f + i * 1.2f, 0.45f, 3f), new Vector3(1.1f, 0.9f, 0.6f), ZoneKit.M(new Color(0.48f, 0.43f, 0.33f)));
            }
            sb.Collider(new Vector3(0f, 0.45f, -3f), new Vector3(7.2f, 0.9f, 0.6f));
            sb.Collider(new Vector3(0f, 0.45f, 3f), new Vector3(7.2f, 0.9f, 0.6f));
            sb.Solid.Box(new Vector3(4.5f, 1.2f, 0f), new Vector3(2.2f, 2.4f, 2.2f), ZoneKit.M(ZoneKit.WardenOlive));
            sb.Collider(new Vector3(4.5f, 1.2f, 0f), new Vector3(2.2f, 2.4f, 2.2f));
            sb.Storage("Warden locker", "lm_cp_locker", new Vector3(4.5f, 1f, -1.3f), ContainerType.Locker, 4);
            ZoneFlameLight.Spawn(sb.Root, sb.W(4.5f, 2.6f, -1.3f), new Color(1f, 0.85f, 0.6f), 9f, 1f, 0.03f);
        }

        private static void DrownedChapel(SiteBuilder sb)
        {
            Color stone = new Color(0.44f, 0.44f, 0.42f);
            sb.Flat.Box(new Vector3(0f, 0.03f, 0f), new Vector3(9f, 0.04f, 11f), ZoneKit.G(new Color(0.06f, 0.08f, 0.08f)));
            sb.Solid.Box(new Vector3(-4.5f, 2f, 0f), new Vector3(0.5f, 4f, 11f), ZoneKit.M(stone));
            sb.Solid.Box(new Vector3(4.5f, 2f, 0f), new Vector3(0.5f, 4f, 11f), ZoneKit.M(stone));
            sb.Solid.Box(new Vector3(0f, 2f, 5.5f), new Vector3(9.5f, 4f, 0.5f), ZoneKit.M(stone));
            sb.Solid.Box(new Vector3(-3f, 2f, -5.5f), new Vector3(3.5f, 4f, 0.5f), ZoneKit.M(stone));
            sb.Solid.Box(new Vector3(3f, 2f, -5.5f), new Vector3(3.5f, 4f, 0.5f), ZoneKit.M(stone));
            sb.Collider(new Vector3(-4.5f, 2f, 0f), new Vector3(0.5f, 4f, 11f));
            sb.Collider(new Vector3(4.5f, 2f, 0f), new Vector3(0.5f, 4f, 11f));
            sb.Collider(new Vector3(0f, 2f, 5.5f), new Vector3(9.5f, 4f, 0.5f));
            sb.Collider(new Vector3(-3f, 2f, -5.5f), new Vector3(3.5f, 4f, 0.5f));
            sb.Collider(new Vector3(3f, 2f, -5.5f), new Vector3(3.5f, 4f, 0.5f));
            sb.Upper.Box(new Vector3(0f, 6f, -5.2f), new Vector3(1.6f, 4f, 1.6f), ZoneKit.M(stone));
            sb.Roof.Box(new Vector3(0f, 4.2f, 0f), new Vector3(9.6f, 0.3f, 11.4f), ZoneKit.M(ZoneKit.WetWood));
            for (int i = 0; i < 4; i++)
            {
                sb.Solid.Box(new Vector3(-2f, 0.4f, -2.5f + i * 1.8f), new Vector3(2.2f, 0.8f, 0.5f), ZoneKit.M(ZoneKit.WetWood));
                sb.Solid.Box(new Vector3(2f, 0.4f, -2.5f + i * 1.8f), new Vector3(2.2f, 0.8f, 0.5f), ZoneKit.M(ZoneKit.WetWood));
            }
            sb.Solid.Box(new Vector3(0f, 0.6f, 4.2f), new Vector3(2f, 1.2f, 0.9f), ZoneKit.M(stone));
            sb.Collider(new Vector3(0f, 0.6f, 4.2f), new Vector3(2f, 1.2f, 0.9f));
            sb.Storage("Altar", "lm_chapel_altar", new Vector3(0f, 1.2f, 3.6f), ContainerType.Desk, 4);
            sb.Storage("Sacristy chest", "lm_chapel_chest", new Vector3(3.6f, 0.5f, 4.6f), ContainerType.Crate, 4);
            for (int i = 0; i < 4; i++) ZoneFlameLight.Spawn(sb.Root, sb.W(-1.2f + i * 0.8f, 1.35f, 4.2f), new Color(1f, 0.7f, 0.35f), 5f, 0.6f, 0.5f);
            WorldMarkers.Add(WorldMarkers.MutantLair, sb.W(0f, 0f, 0f));
        }

        private static void HuntersHide(SiteBuilder sb)
        {
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1.2f : 1.2f), z = (i < 2 ? -1.2f : 1.2f);
                sb.Solid.Box(new Vector3(x + 3f, 1.6f, z), new Vector3(0.2f, 3.2f, 0.2f), ZoneKit.M(ZoneKit.WetWood));
                sb.Collider(new Vector3(x + 3f, 1.6f, z), new Vector3(0.2f, 3.2f, 0.2f));
            }
            sb.Upper.Box(new Vector3(3f, 3.2f, 0f), new Vector3(2.8f, 0.15f, 2.8f), ZoneKit.M(ZoneKit.WetWood));
            sb.Roof.Box(new Vector3(3f, 4.8f, 0f), new Vector3(3f, 0.1f, 3f), ZoneKit.M(new Color(0.25f, 0.30f, 0.20f)));
            // A canvas tent and the fire.
            sb.At(sb.Solid, new Vector3(-3f, 0f, 0f), 0f, m =>
            {
                m.Tri(new Vector3(-1.2f, 0f, -1.5f), new Vector3(0f, 1.6f, -1.5f), new Vector3(1.2f, 0f, -1.5f), ZoneKit.M(new Color(0.40f, 0.42f, 0.30f)));
                m.Box(new Vector3(-0.6f, 0.8f, 0f), new Vector3(0.05f, 1.8f, 3f), Quaternion.Euler(0f, 0f, -53f), ZoneKit.M(new Color(0.40f, 0.42f, 0.30f)));
                m.Box(new Vector3(0.6f, 0.8f, 0f), new Vector3(0.05f, 1.8f, 3f), Quaternion.Euler(0f, 0f, 53f), ZoneKit.M(new Color(0.40f, 0.42f, 0.30f)));
            });
            sb.Collider(new Vector3(-3f, 0.7f, 0f), new Vector3(2.4f, 1.4f, 3f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                sb.Solid.Box(new Vector3(Mathf.Cos(a) * 0.6f, 0.1f, Mathf.Sin(a) * 0.6f + 3f), new Vector3(0.3f, 0.2f, 0.25f), ZoneKit.M(ZoneKit.ConcreteDark));
            }
            ZoneKit.Fire(sb.Root, sb.W(0f, 0.1f, 3f), 0.6f);
            ZoneFlameLight.Spawn(sb.Root, sb.W(0f, 0.8f, 3f), new Color(1f, 0.55f, 0.22f), 9f, 1.8f, 0.6f);
            sb.Storage("Food sack", "lm_hide_food", new Vector3(-3f, 0.4f, 1.2f), ContainerType.Pantry, 4);
            sb.Storage("Rifle case", "lm_hide_rifle", new Vector3(3f, 3.4f, 0.8f), ContainerType.GunLocker, 3);
            WorldMarkers.Add(WorldMarkers.HumanCamp, sb.W(0f, 0f, 1.5f));
        }

        private static void Cemetery(SiteBuilder sb)
        {
            System.Random r = sb.Rng;
            Color stone = new Color(0.52f, 0.52f, 0.50f);
            for (float x = -7f; x <= 7f; x += 0.5f)
            {
                sb.Upper.Box(new Vector3(x, 0.5f, -7f), new Vector3(0.05f, 1f, 0.05f), ZoneKit.Mt(new Color(0.12f, 0.12f, 0.12f)));
                sb.Upper.Box(new Vector3(x, 0.5f, 7f), new Vector3(0.05f, 1f, 0.05f), ZoneKit.Mt(new Color(0.12f, 0.12f, 0.12f)));
            }
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 6; col++)
                {
                    if (r.NextDouble() < 0.15) continue;
                    var p = new Vector3(-5.5f + col * 2.2f, 0f, -4.5f + row * 2.6f);
                    float tilt = ZoneKit.R(r, -12f, 12f);
                    sb.At(sb.Solid, p, tilt, m => m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.15f), ZoneKit.M(ZoneKit.V(stone, r))));
                    // Open, empty graves: the Returned walked home from them.
                    if (r.NextDouble() < 0.4) sb.Flat.Box(p + new Vector3(0f, 0.02f, -0.9f), new Vector3(0.8f, 0.02f, 1.6f), ZoneKit.M(new Color(0.12f, 0.09f, 0.07f)));
                }
            sb.Solid.Box(new Vector3(0f, 1.4f, 5.4f), new Vector3(4f, 2.8f, 2.4f), ZoneKit.M(stone));
            sb.Collider(new Vector3(0f, 1.4f, 5.4f), new Vector3(4f, 2.8f, 2.4f));
            sb.Storage("Crypt niche", "lm_cemetery_crypt", new Vector3(0f, 1f, 4.1f), ContainerType.Desk, 3);
            ZoneFlameLight.Spawn(sb.Root, sb.W(0.8f, 1.4f, 4f), new Color(0.6f, 0.75f, 1f), 6f, 0.7f, 0.4f);
            WorldMarkers.Add(WorldMarkers.Cemetery, sb.W(0f, 0f, 0f));
        }

        private static void CoolingTower(SiteBuilder sb)
        {
            const int sides = 20;
            Color conc = new Color(0.58f, 0.57f, 0.54f);
            for (int ring = 0; ring < 6; ring++)
            {
                float y0 = ring * 4f;
                float t = ring / 5f;
                float rad = Mathf.Lerp(9f, 6.2f, Mathf.Sin(t * Mathf.PI * 0.75f));
                RaidMesh m = ring == 0 ? sb.Solid : sb.Upper;
                for (int s = 0; s < sides; s++)
                {
                    if (ring == 0 && (s == 0 || s == 10)) continue;   // doorways
                    float a = (s + 0.5f) / sides * Mathf.PI * 2f;
                    var c = new Vector3(Mathf.Cos(a) * rad, y0 + 2f, Mathf.Sin(a) * rad);
                    float yaw = -a * Mathf.Rad2Deg + 90f;
                    sb.At(m, c, yaw, mm => mm.Box(Vector3.zero, new Vector3(rad * 2f * Mathf.PI / sides + 0.1f, 4f, 0.5f), ZoneKit.M(ZoneKit.V(conc, sb.Rng, 0.04f))));
                    if (ring == 0) sb.Collider(c, new Vector3(rad * 2f * Mathf.PI / sides + 0.1f, 4f, 0.5f), yaw);
                }
            }
            ZoneKit.Plume(sb.Root, sb.W(0f, 23f, 0f), false, 5f, 4f, 3f, new Color(0.92f, 0.92f, 0.9f, 0.45f));
            sb.Storage("Maintenance locker", "lm_tower_locker", new Vector3(0f, 1f, 5.6f), ContainerType.ToolBench, 5);
            sb.Storage("Control cabinet", "lm_tower_cabinet", new Vector3(5.6f, 1f, 0f), ContainerType.Locker, 4);
            ZoneFlameLight.Spawn(sb.Root, sb.W(0f, 3f, 0f), new Color(0.8f, 0.85f, 1f), 12f, 0.9f, 0.1f);
            WorldMarkers.Add(WorldMarkers.MutantLair, sb.W(0f, 0f, 0f));
        }

        private static void ChoirSanctum(SiteBuilder sb)
        {
            Color stone = new Color(0.30f, 0.28f, 0.32f);
            for (int i = 0; i < 9; i++)
            {
                float a = i / 9f * Mathf.PI * 2f;
                var c = new Vector3(Mathf.Cos(a) * 5.5f, 0f, Mathf.Sin(a) * 5.5f);
                float h = ZoneKit.R(sb.Rng, 2.4f, 3.6f);
                sb.Solid.Box(c + Vector3.up * h * 0.5f, new Vector3(0.7f, h, 0.5f), ZoneKit.M(ZoneKit.V(stone, sb.Rng)));
                sb.Collider(c + Vector3.up * h * 0.5f, new Vector3(0.7f, h, 0.5f));
            }
            sb.Solid.Cylinder(Vector3.zero, 2f, 0.4f, 16, ZoneKit.M(stone));
            sb.Flat.FlatBlob(new Vector3(0f, 0.42f, 0f), 1.2f, 1.2f, 0f, 0.1f, sb.Rng, ZoneKit.Gl(new Color(0.55f, 0.25f, 0.8f)));
            for (int i = 0; i < 6; i++) ZoneFlameLight.Spawn(sb.Root, sb.W(Mathf.Cos(i) * 2.4f, 0.4f, Mathf.Sin(i) * 2.4f), new Color(0.7f, 0.4f, 1f), 5f, 0.7f, 0.6f);
            sb.Storage("Offerings", "lm_choir_offer", new Vector3(0f, 0.6f, 1.6f), ContainerType.Desk, 4);
            sb.Storage("Choir cache", "lm_choir_cache", new Vector3(0f, 0.5f, -6.5f), ContainerType.GunLocker, 4);
            WorldMarkers.Add(WorldMarkers.HumanCamp, sb.W(0f, 0f, 3f));
        }
        #endregion

        #region Published points
        public static void PublishPoints(Transform world, RaidMapInfo map, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            float half = site.Half;
            Vector3[] sides = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
            for (int s = 0; s < 4; s++)
            {
                Vector3 target = sides[s] * (half - (zone.HasCordon ? CordonRules.BeltWidth + 6f : 16f));
                Vector3 pad = target;
                if (site.TryFind(rng, new Vector2(10f, 10f), 0f, 1f, ZoneSiteMap.Hard | SiteCell.Edge, 4f,
                        p => -Vector3.Distance(p, target), 160, out Vector3 found) && Vector3.Distance(found, target) < 26f)
                    pad = found;
                site.Claim(pad, new Vector2(10f, 10f), 0f);
                WorldMarkers.Add(WorldMarkers.Landing, pad);
                ZoneWorld.LandingZones.Add(pad);
            }

            int vehicles = 0;
            for (int i = 0; i < 400 && vehicles < 4; i++)
            {
                var p = new Vector3(ZoneKit.R(rng, -half + 10f, half - 10f), 0f, ZoneKit.R(rng, -half + 10f, half - 10f));
                if ((site.At(p) & SiteCell.Paved) == 0 || site.Any(p, 3f, ZoneSiteMap.Hard)) continue;
                site.MarkCircle(p, 3f, SiteCell.Taken);
                WorldMarkers.Add(WorldMarkers.VehicleSpot, p);
                vehicles++;
            }

            for (int i = 0; i < 2; i++)
            {
                if (!site.TryFind(rng, new Vector2(12f, 12f), 0f, 2f, ZoneSiteMap.Hard | SiteCell.Edge, 12f,
                        p => Vector3.Distance(p, map.PlayerSpawn) < 35f ? -1f : (float)rng.NextDouble(), 80, out Vector3 at))
                    continue;
                site.Claim(at, new Vector2(12f, 12f), 0f);
                WorldMarkers.Add(WorldMarkers.Outpost, at);
            }
        }
        #endregion
    }

    /// <summary>Raises <see cref="GameEvents.LocationVisited"/> once per raid when the local survivor reaches a landmark.</summary>
    public sealed class LocationVisitTrigger : MonoBehaviour
    {
        private static readonly HashSet<string> s_Visited = new HashSet<string>();
        public string Id;
        public float Radius = 10f;
        private float _next;

        public static void ResetForRaid() => s_Visited.Clear();

        public static LocationVisitTrigger Attach(GameObject go, string id, float radius)
        {
            var t = go.AddComponent<LocationVisitTrigger>();
            t.Id = id;
            t.Radius = radius;
            return t;
        }

        private void Update()
        {
            if (Time.time < _next || string.IsNullOrEmpty(Id) || s_Visited.Contains(Id)) return;
            _next = Time.time + 0.5f;
            Actor me = Actor.LocalPlayer;
            if (me == null || !me.IsAlive) return;
            Vector3 d = me.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > Radius * Radius) return;
            s_Visited.Add(Id);
            AccountProfile a = ProfileStore.Account;
            bool first = a != null && a.Learn("landmark:" + Id);
            GameEvents.RaiseLocationVisited(Id);
            GameEvents.RaiseToast((first ? "Discovered " : "Reached ") + ZoneLandmarks.NameOf(Id) + ".");
            if (first) ProfileStore.Save();
        }
    }
}
