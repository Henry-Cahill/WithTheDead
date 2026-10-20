using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The Zone's cost on the body, across runs: radiation taken, emissions lived through and artifacts worn add to a
    /// character's exposure, and each whole point of it leaves a mark — a lasting change with a gift and a price.
    /// Marks belong to the body, so they outlive a lost raid (a wipe or a new character starts clean).
    /// </summary>
    public static class ZoneExposure
    {
        public sealed class Mark
        {
            public string Id, Name, Description;
            public System.Action<StatMods> Apply;
        }

        public static readonly List<Mark> Marks = new List<Mark>();

        static ZoneExposure()
        {
            M("night_eyes", "Night Eyes", "Your pupils never quite close now. You see further in the dark; bright light leaves you exposed.",
                m => { m.NightVision += 0.25f; m.VisibilityMult *= 1.08f; });
            M("cold_blood", "Cold Blood", "You barely feel the cold any more. You eat like two men.",
                m => { m.ColdResist += 0.3f; m.HungerRate *= 1.15f; });
            M("quiet_heart", "Quiet Heart", "Your heart barely changes pace, whatever is coming. Wounds close slower.",
                m => { m.PanicRate *= 0.7f; m.HealRate *= 0.9f; });
            M("lead_skin", "Lead Skin", "Your skin has gone grey and thick. It shrugs off radiation, and every step is heavier.",
                m => { m.Resist.Radiation += 0.2f; m.MoveSpeed *= 0.97f; });
            M("zone_sense", "Zone Sense", "You feel anomalies before you see them. Something else feels you back.",
                m => { m.Resist.Psi -= 0.1f; m.LootLuck *= 1.05f; });
            M("hollow_bones", "Hollow Bones", "You are light on your feet and tire slowly. You bruise at a touch.",
                m => { m.StaminaDrain *= 0.85f; m.Resist.Bullet -= 0.05f; });
            M("returned_touch", "The Returned's Touch", "The dead are slower to notice you. The living find you hard to like.",
                m => { m.VisibilityMult *= 0.92f; m.XpMult *= 0.95f; });
            M("still_hands", "Still Hands", "Your hands no longer shake. Your gut has never been right since.",
                m => { m.AimSpread *= 0.9f; m.ThirstRate *= 1.12f; });
            foreach (Mark k in Marks)
            {
                ZoneMarkRegistry.Names[k.Id] = k.Name;
                ZoneMarkRegistry.Descriptions[k.Id] = k.Description;
            }
        }

        private static void M(string id, string name, string desc, System.Action<StatMods> apply)
            => Marks.Add(new Mark { Id = id, Name = name, Description = desc, Apply = apply });

        public static Mark Get(string id)
        {
            for (int i = 0; i < Marks.Count; i++) if (Marks[i].Id == id) return Marks[i];
            return null;
        }

        /// <summary>Adds exposure to the active character; crossing a whole point leaves a new mark.</summary>
        public static void Add(float amount)
        {
            if (amount <= 0f || GameSession.IsBorrowedBody) return;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null) return;
            int before = Mathf.FloorToInt(c.Exposure);
            c.Exposure += amount;
            int after = Mathf.FloorToInt(c.Exposure);
            for (int n = before + 1; n <= after; n++) GrantMark(c, n);
        }

        private static void GrantMark(CharacterProfile c, int n)
        {
            var pool = new List<Mark>();
            foreach (Mark m in Marks) if (!c.ZoneMarks.Contains(m.Id)) pool.Add(m);
            if (pool.Count == 0) return;
            var rng = new System.Random((c.Id ?? c.Name).GetHashCode() ^ (n * 7919));
            Mark pick = pool[rng.Next(pool.Count)];
            c.ZoneMarks.Add(pick.Id);
            GameEvents.RaiseToast("The Zone has left a mark: " + pick.Name);
            GameEvents.RaiseSubtitle("", pick.Description);
        }
    }

    /// <summary>Applies the active character's Zone marks.</summary>
    public sealed class ZoneMarkEffects : MonoBehaviour, IStatModSource
    {
        public void ApplyMods(StatMods mods)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || c.ZoneMarks == null || GameSession.IsBorrowedBody) return;
            for (int i = 0; i < c.ZoneMarks.Count; i++) ZoneExposure.Get(c.ZoneMarks[i])?.Apply?.Invoke(mods);
        }
    }

    /// <summary>
    /// Lays the Zone's hazards over a raid map: clusters of anomalies (anomaly fields), radiation hot spots near the
    /// industrial landmarks, and the artifacts inside them. Clear of the exits, the insertion points and the streets'
    /// buildings; deterministic from the raid seed.
    /// </summary>
    internal static class AnomalyField
    {
        public static void Build(Transform world, RaidMapInfo map, ZoneDef zone, ZoneSiteMap site, System.Random rng)
        {
            if (zone == null || map == null || zone.Anomalies.Length == 0) return;
            var root = new GameObject("Anomalies").transform;
            root.SetParent(world, false);
            ZoneKit.NoBake(root.gameObject);

            float area = map.HalfSize * map.HalfSize * 4f;
            int target = Mathf.RoundToInt(area / 1200f * zone.AnomalyDensity * SandboxSettings.Current.AnomalyDensity);
            int placed = 0, clusters = 0, tollgates = 0;
            var keepClear = new List<Vector3>(ZoneWorld.LandingZones) { map.PlayerSpawn };
            foreach (RaidExit e in map.Exits) keepClear.Add(e.Position);
            IReadOnlyList<Vector3> marks = WorldMarkers.Get(WorldMarkers.Landing);
            for (int i = 0; i < marks.Count; i++) keepClear.Add(marks[i]);

            for (int attempt = 0; attempt < 60 && placed < target; attempt++)
            {
                Vector3 centre;
                if (site != null)
                {
                    if (!site.TryFind(rng, new Vector2(8f, 8f), 0f, 1f, SiteCell.Building | SiteCell.Reserved | SiteCell.Taken | SiteCell.Hazard, 6f,
                        p => Clear(p, keepClear, 22f) ? (float)rng.NextDouble() : -1f, 30, out centre)) continue;
                }
                else
                {
                    float r = map.HalfSize - 8f;
                    centre = new Vector3(ZoneKit.R(rng, -r, r), 0f, ZoneKit.R(rng, -r, r));
                    if (!Clear(centre, keepClear, 22f)) continue;
                }
                clusters++;
                int size = 2 + rng.Next(4);
                for (int k = 0; k < size && placed < target; k++)
                {
                    string kind = zone.Anomalies[rng.Next(zone.Anomalies.Length)];
                    if (kind == "tollgate" && (tollgates >= 2 || rng.NextDouble() < 0.5)) kind = zone.Anomalies[0];
                    AnomalyDef def = AnomalyCatalog.Get(kind);
                    if (def == null) continue;
                    Vector3 p = centre + Quaternion.Euler(0f, ZoneKit.R(rng, 0f, 360f), 0f) * new Vector3(ZoneKit.R(rng, 0f, 7f), 0f, 0f);
                    p.y = GroundY(p);
                    if (site != null && site.Any(p, def.Radius + 0.5f, SiteCell.Building | SiteCell.Reserved | SiteCell.Hazard)) continue;
                    if (!Clear(p, keepClear, 18f)) continue;
                    Anomaly.Spawn(root, def, p, Mathf.Lerp(0.85f, 1.15f, (float)rng.NextDouble()) * (0.8f + zone.Depth * 0.15f), rng);
                    site?.MarkCircle(p, def.Radius + 0.5f, SiteCell.Hazard);
                    if (kind == "tollgate") tollgates++;
                    placed++;
                }
            }

            // Radiation hot spots: by the industrial landmarks, then a few loose in the open.
            int spots = Mathf.RoundToInt(1f + zone.Radiation * 600f);
            foreach (string kind in WorldMarkers.Kinds)
            {
                if (spots <= 0) break;
                if (!kind.StartsWith("landmark:")) continue;
                IReadOnlyList<Vector3> pts = WorldMarkers.Get(kind);
                if (pts.Count == 0 || rng.NextDouble() < 0.5) continue;
                Vector3 at = pts[0] + new Vector3(ZoneKit.R(rng, -10f, 10f), 0f, ZoneKit.R(rng, -10f, 10f));
                if (!Clear(at, keepClear, 25f)) continue;
                RadiationSpot.Spawn(root, at, ZoneKit.R(rng, 7f, 12f), ZoneKit.R(rng, 0.0012f, 0.003f) * (0.6f + zone.Depth * 0.4f), rng);
                spots--;
            }
            for (int i = 0; i < spots; i++)
            {
                float r = map.HalfSize - 10f;
                var at = new Vector3(ZoneKit.R(rng, -r, r), 0f, ZoneKit.R(rng, -r, r));
                if (!Clear(at, keepClear, 25f)) continue;
                RadiationSpot.Spawn(root, at, ZoneKit.R(rng, 6f, 10f), ZoneKit.R(rng, 0.0008f, 0.002f) * (0.6f + zone.Depth * 0.4f), rng);
            }
            Debug.Log("[CloneZ] Zone hazards: " + placed + " anomalies in " + clusters + " fields, radiation spots " + RadiationSpot.All.Count);
        }

        private static bool Clear(Vector3 p, List<Vector3> keep, float radius)
        {
            for (int i = 0; i < keep.Count; i++)
            {
                Vector3 k = keep[i];
                if ((new Vector2(k.x - p.x, k.z - p.z)).sqrMagnitude < radius * radius) return false;
            }
            return true;
        }

        private static float GroundY(Vector3 p)
        {
            return Physics.Raycast(new Vector3(p.x, 20f, p.z), Vector3.down, out RaycastHit hit, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && hit.point.y < 1.5f
                ? hit.point.y : 0f;
        }
    }

    /// <summary>Wires the Zone's hazards into the raid: anomalies, artifacts, radiation, detectors, bolts, emissions, marks.</summary>
    public sealed class ZoneHazardsModule : IGameModule
    {
        public int Order => 45;
        private static int s_Seed;

        public void Init()
        {
            PlayerRadiation.RegisterItems();
            DetectorDevice.RegisterItemVerbs();
            ArtifactBelt.RegisterItemVerbs();
            ItemActions.RegisterUse(BoltThrower.BoltId, (inv, stack) =>
            {
                var t = inv.GetComponent<BoltThrower>();
                if (t != null) t.TryThrow();
                return true;
            });

            WorldHooks.RaidMapBuilt += (world, map, zone, rng) =>
            {
                s_Seed = rng != null ? rng.Next() : GameSession.RaidSeed;
                if (zone == null) zone = ZoneCatalog.Current;
                var hazardRng = new System.Random(s_Seed);
                AnomalyField.Build(world, map, zone, ZoneWorld.Site, hazardRng);
                ArtifactField.Build(world, zone, s_Seed);
            };
            WorldHooks.RaidStarted += (world, map, zone) =>
            {
                if (NetAuthority.IsServer) EmissionDirector.Begin(zone ?? ZoneCatalog.Current, s_Seed);
                else EmissionDirector.BeginFollower(zone ?? ZoneCatalog.Current, s_Seed);
                ArtifactField.AnnouncePrize();
            };
            WorldHooks.HubBuilt += _ => EndRaid();
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                GameObject go = p.gameObject;
                if (go.GetComponent<BoltThrower>() == null) go.AddComponent<BoltThrower>();
                if (go.GetComponent<DetectorDevice>() == null) go.AddComponent<DetectorDevice>();
                if (go.GetComponent<ArtifactBelt>() == null) go.AddComponent<ArtifactBelt>();
                if (go.GetComponent<PlayerRadiation>() == null) go.AddComponent<PlayerRadiation>();
                if (go.GetComponent<ZoneMarkEffects>() == null) go.AddComponent<ZoneMarkEffects>();
                if (Object.FindAnyObjectByType<HudRadiation>() == null) HudRadiation.Create();
            };
            GameEvents.RaidEnded += (o, e) => EndRaid();
        }

        private static void EndRaid()
        {
            if (EmissionDirector.Instance != null) Object.Destroy(EmissionDirector.Instance.gameObject);
            ArtifactField.Clear();
            ZoneAtmosphere.SkyOverrideStrength = 0f;
        }
    }
}
