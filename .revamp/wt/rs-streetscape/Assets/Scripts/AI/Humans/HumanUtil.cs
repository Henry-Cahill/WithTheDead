using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// Small helpers shared by the Zone's people (stalkers, A-Life squads, outposts, wardens, companions):
    /// navmesh sampling, the map's edge band, compass words, distances to the nearest player and a coroutine host
    /// that lives exactly as long as the current world.
    /// </summary>
    public static class HumanUtil
    {
        private static readonly List<Actor> s_Players = new List<Actor>(8);
        private static readonly List<KeyValuePair<string, Vector3>> s_Landmarks = new List<KeyValuePair<string, Vector3>>(16);
        private static readonly string[] s_Compass = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };

        /// <summary>Gets half the size of the arena, clamped to a minimum of 10 units.</summary>
        public static float MapHalf => Mathf.Max(10f, CloneZConfig.ArenaHalfSize);

        /// <summary>Calculates the flat (XZ) squared distance between two points, ignoring height.</summary>
        public static float FlatSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Computes the flat (XZ) distance between two points.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b) => Mathf.Sqrt(FlatSqr(a, b));

        /// <summary>Returns a copy of the vector with zero vertical component.</summary>
        public static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        /// <summary>Samples the NavMesh around a point within radius, returning the nearest valid position.</summary>
        public static bool SampleNav(Vector3 p, float radius, out Vector3 result)
        {
            if (NavMesh.SamplePosition(p, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
            result = p;
            return false;
        }

        /// <summary>Flat distance from a point to the nearest living survivor (float.MaxValue when there is none).</summary>
        public static float NearestPlayerDistance(Vector3 p)
        {
            NearestPlayer(p, out float d);
            return d;
        }

        /// <summary>The living survivor nearest a point (null when none).</summary>
        public static Actor NearestPlayer(Vector3 p, out float distance)
        {
            Actor.Players(s_Players);
            Actor best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < s_Players.Count; i++)
            {
                float d = FlatSqr(p, s_Players[i].transform.position);
                if (d < bestSqr) { bestSqr = d; best = s_Players[i]; }
            }
            distance = best != null ? Mathf.Sqrt(bestSqr) : float.MaxValue;
            return best;
        }

        /// <summary>Clamps a point to stay within the map bounds, leaving a margin from the edge.</summary>
        public static Vector3 ClampToMap(Vector3 p, float margin) => CloneZConfig.ClampToArena(p, Mathf.Min(margin, MapHalf - 1f));


        /// <summary>Generates a random point on the map's edge band, inset from the fence by a given distance.</summary>
        public static Vector3 EdgePoint(System.Random rng, float inset)
        {
            float h = Mathf.Max(1f, MapHalf - inset);
            float t = (float)(rng.NextDouble() * 2.0 - 1.0) * h;
            Vector3 c = CloneZConfig.ArenaCenter;
            switch (rng.Next(4))
            {
                case 0: return new Vector3(c.x + t, 0f, c.z + h);
                case 1: return new Vector3(c.x + t, 0f, c.z - h);
                case 2: return new Vector3(c.x + h, 0f, c.z + t);
                default: return new Vector3(c.x - h, 0f, c.z + t);
            }
        }

        /// <summary>The point of the edge band (inset from the fence) nearest <paramref name="p"/>.</summary>
        /// <summary>Finds the nearest point on the edge band (inset from fence) to a given position.</summary>
        public static Vector3 NearestEdge(Vector3 p, float inset)
        {
            float h = Mathf.Max(1f, MapHalf - inset);
            Vector3 c = CloneZConfig.ArenaCenter;
            p = ClampToMap(p, inset);
            float rx = p.x - c.x, rz = p.z - c.z;
            float dx = h - Mathf.Abs(rx), dz = h - Mathf.Abs(rz);
            if (dx < dz) p.x = c.x + (rx < 0f ? -1f : 1f) * h;
            else p.z = c.z + (rz < 0f ? -1f : 1f) * h;
            p.y = 0f;
            return p;
        }

        /// <summary>Creates a random point anywhere on the map, keeping a margin off the fence.</summary>
        public static Vector3 RandomMapPoint(System.Random rng, float margin) => CloneZConfig.RandomArenaPoint(rng, margin);

        /// <summary>A random ground-level navmesh point anywhere on the map (kept <paramref name="margin"/> off the fence).</summary>
        /// <summary>Generates a random ground-level NavMesh point on the map, respecting a margin from the fence.</summary>
        public static bool RandomNavPoint(System.Random rng, float margin, out Vector3 result, int tries = 20)
        {
            for (int i = 0; i < tries; i++)
            {
                if (SampleNav(RandomMapPoint(rng, margin), 3f, out result) && result.y < 0.6f) return true;
            }
            result = Vector3.zero;
            return false;
        }

        /// <summary>A navmesh point within <paramref name="radius"/> of <paramref name="center"/>.</summary>
        /// <summary>Attempts to find a NavMesh point within a radius of a center point, trying random offsets.</summary>
        public static bool NavPointNear(Vector3 center, float radius, System.Random rng, out Vector3 result, int tries = 10)
        {
            for (int i = 0; i < tries; i++)
            {
                float a = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float r = radius * Mathf.Sqrt((float)rng.NextDouble());
                if (SampleNav(center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), 2.5f, out result)) return true;
            }
            if (SampleNav(center, radius + 2f, out result) && result.y < 0.6f) return true;
            result = Vector3.zero;
            return false;
        }

        /// <summary>"north", "south-east"... for a flat direction (+Z is north, +X east).</summary>
        /// <summary>Converts a flat direction vector into a compass word like "north" or "south-east".</summary>
        public static string Compass(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return "north";
            float ang = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (ang < 0f) ang += 360f;
            return s_Compass[Mathf.RoundToInt(ang / 45f) % 8];
        }

        /// <summary>"crashed_helicopter" → "crashed helicopter"; a real landmark's id (see <see cref="MapGameplay"/>)
        /// → its name ("Bardstown courthouse").</summary>
        public static string Pretty(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            return MapGameplay.TryLandmarkName(id, out string name) ? name : id.Replace('_', ' ');
        }

        // A distance in words: metres to the hundred under 2 km, kilometres past it.
        private static string Far(float d) => d >= 2000f ? Mathf.RoundToInt(d / 1000f) + " km" : Mathf.Max(100, Mathf.RoundToInt(d / 100f) * 100) + " m";

        /// <summary>Formats a float to a short string with invariant culture.</summary>
        public static string Inv(float f) => f.ToString("0.#", CultureInfo.InvariantCulture);

        /// <summary>Tries to parse a string into a float using invariant culture.</summary>
        public static bool TryParse(string s, out float f) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f);

        /// <summary>A hash that is the same on every run (string.GetHashCode is not).</summary>
        /// <summary>Computes a stable hash code for a string that is consistent across runs.</summary>
        public static int StableHash(string s)
        {
            unchecked
            {
                int h = 23;
                if (s != null)
                    for (int i = 0; i < s.Length; i++) h = h * 31 + s[i];
                return h;
            }
        }

        /// <summary>Picks a random element from a list, or default if empty.</summary>
        public static T Pick<T>(System.Random rng, IList<T> list) => list == null || list.Count == 0 ? default : list[rng.Next(list.Count)];

        /// <summary>True when the game time indicates nighttime.</summary>
        public static bool IsNight => TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight;

        public static float LightLevel => TimeOfDay.Instance != null ? TimeOfDay.Instance.LightLevel : 1f;

        /// <summary>Runs a coroutine on a helper object that is destroyed with the scene.</summary>
        public static Coroutine Run(IEnumerator routine) => routine != null ? HumanRunner.Instance.StartCoroutine(routine) : null;

        /// <summary>Every landmark the current map published ("landmark:&lt;id&gt;"), as (id, position). On the streamed map
        /// only those around the country built near the survivors (the state's far landmarks are no goal for anyone here);
        /// of the real map's landmarks only the most important of any within <see cref="RealLandmarkSpacing"/> of each
        /// other (the courthouse, not the bank across the street), so the Zone map's pins stay readable downtown. The
        /// returned list is a new instance; callers may modify it without affecting future calls.</summary>
        public static List<KeyValuePair<string, Vector3>> Landmarks()
        {
            var result = new List<KeyValuePair<string, Vector3>>(16);
            const string prefix = "landmark:";
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            float reach = streamer != null ? streamer.BuiltReach + 160f : float.MaxValue;
            Vector3 centre = CloneZConfig.ArenaCenter;
            s_Real.Clear();
            foreach (string kind in WorldMarkers.Kinds)
            {
                if (kind == null || !kind.StartsWith(prefix, System.StringComparison.Ordinal)) continue;
                IReadOnlyList<Vector3> pts = WorldMarkers.Get(kind);
                string id = kind.Substring(prefix.Length);
                bool real = MapGameplay.TryLandmark(id, out _, out int rank);
                for (int i = 0; i < pts.Count; i++)
                {
                    if (streamer != null && (Mathf.Abs(pts[i].x - centre.x) > reach || Mathf.Abs(pts[i].z - centre.z) > reach)) continue;
                    if (real) s_Real.Add((id, pts[i], rank));
                    else result.Add(new KeyValuePair<string, Vector3>(id, pts[i]));
                }
            }
            if (s_Real.Count == 0) return result;
            s_Real.Sort((a, b) => a.rank != b.rank ? b.rank.CompareTo(a.rank) : string.CompareOrdinal(a.id, b.id));
            int first = result.Count;
            const float spacing2 = RealLandmarkSpacing * RealLandmarkSpacing;
            foreach (var l in s_Real)
            {
                bool crowded = false;
                for (int k = first; k < result.Count && !crowded; k++) crowded = FlatSqr(result[k].Value, l.at) < spacing2;
                if (!crowded) result.Add(new KeyValuePair<string, Vector3>(l.id, l.at));
            }
            s_Real.Clear();
            return result;
        }

        /// <summary>The least distance between two real landmarks listed by <see cref="Landmarks"/>, in metres.</summary>
        public const float RealLandmarkSpacing = 45f;

        private static readonly List<(string id, Vector3 at, int rank)> s_Real = new List<(string, Vector3, int)>(64);

        /// <summary>Every published marker whose kind mentions <paramref name="word"/> (anomaly fields, camps...).</summary>
        public static void MarkersMatching(string word, List<Vector3> into)
        {
            into.Clear();
            if (string.IsNullOrEmpty(word)) return;
            foreach (string kind in WorldMarkers.Kinds)
            {
                if (kind == null || kind.IndexOf(word, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                IReadOnlyList<Vector3> pts = WorldMarkers.Get(kind);
                for (int i = 0; i < pts.Count; i++) into.Add(pts[i]);
            }
        }

        /// <summary>Where a point lies, in words: so many metres and which way from the nearest landmark, else which
        /// part of the map.</summary>
        public static string DescribeLocation(Vector3 p, string zoneName)
        {
            List<KeyValuePair<string, Vector3>> marks = Landmarks();
            float best = float.MaxValue;
            int bi = -1;
            for (int i = 0; i < marks.Count; i++)
            {
                float d = FlatSqr(p, marks[i].Value);
                if (d < best) { best = d; bi = i; }
            }
            string inZone = string.IsNullOrEmpty(zoneName) ? "" : " in " + zoneName;
            if (bi >= 0 && best < 60f * 60f)
            {
                float d = Mathf.Sqrt(best);
                string what = "the " + Pretty(marks[bi].Key);
                if (d < 5f) return "right by " + what + inZone;
                return Mathf.RoundToInt(d) + " m " + Compass(p - marks[bi].Value) + " of " + what + inZone;
            }
            return DescribeRegion(p, zoneName);
        }

        /// <summary>"the north-west part of Kestrel Flats, by the fence", from the position alone.</summary>
        public static string DescribeRegion(Vector3 p, string zoneName)
        {
            string of = " of " + (string.IsNullOrEmpty(zoneName) ? "the map" : zoneName);
            ZoneDef zone = ZoneCatalog.Current;
            if (KentuckyStreamer.Active != null && zone != null && !string.IsNullOrEmpty(zone.RealPlace))
            {
                // The whole state: the start town's square has its parts; elsewhere the nearest town says where (in it,
                // on its edge, or so far and which way from it), else so far and which way from the start town.
                float d = Mathf.Sqrt(p.x * p.x + p.z * p.z), start = zone.RealHalfSize;
                string tail = string.IsNullOrEmpty(zoneName) ? "" : ", " + zoneName;
                if (Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)) < start)
                    return (d < start * 0.3f ? "the middle" : "the " + Compass(p) + " part") + " of " + zone.RealPlace;
                if (MapGameplay.StrongestPlace(p, out MapPlace inside, out float din) > 0.05f && MapGameplay.Within(inside, din))
                    return (din < 400f ? "the middle of " : "the " + Compass(p - MapGameplay.WorldOf(inside)) + " side of ") + inside.name + tail;
                if (MapGameplay.NearestPlace(p, 300, out MapPlace town, out float dn, out Vector3 at) && dn < 25000f)
                    return Far(dn) + " " + Compass(p - at) + " of " + town.name + tail;
                return Far(d) + " " + Compass(p) + " of " + zone.RealPlace + tail;
            }
            float h = MapHalf;
            float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z));
            string where = edge < h * 0.3f ? "the middle" : "the " + Compass(p) + " part";
            string near = edge > h - 14f ? ", by the fence" : "";
            return where + of + near;
        }
    }

    /// <summary>Hosts coroutines for the static systems of the Zone's people; destroyed with the scene.</summary>
    [DisallowMultipleComponent]
    public sealed class HumanRunner : MonoBehaviour
    {
        private static HumanRunner s_Instance;

        public static HumanRunner Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = UnityEngine.Object.FindAnyObjectByType<HumanRunner>();
                    if (s_Instance == null)
                        s_Instance = new GameObject("[ZonePeople]").AddComponent<HumanRunner>();
                }
                return s_Instance;
            }
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }
    }
}
