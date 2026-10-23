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
        public static Vector3 ClampToMap(Vector3 p, float margin)
        {
            float h = Mathf.Max(1f, MapHalf - margin);
            p.x = Mathf.Clamp(p.x, -h, h);
            p.z = Mathf.Clamp(p.z, -h, h);
            return p;
        }


        /// <summary>Generates a random point on the map's edge band, inset from the fence by a given distance.</summary>
        public static Vector3 EdgePoint(System.Random rng, float inset)
        {
            float h = Mathf.Max(1f, MapHalf - inset);
            float t = (float)(rng.NextDouble() * 2.0 - 1.0) * h;
            switch (rng.Next(4))
            {
                case 0: return new Vector3(t, 0f, h);
                case 1: return new Vector3(t, 0f, -h);
                case 2: return new Vector3(h, 0f, t);
                default: return new Vector3(-h, 0f, t);
            }
        }

        /// <summary>The point of the edge band (inset from the fence) nearest <paramref name="p"/>.</summary>
        /// <summary>Finds the nearest point on the edge band (inset from fence) to a given position.</summary>
        public static Vector3 NearestEdge(Vector3 p, float inset)
        {
            float h = Mathf.Max(1f, MapHalf - inset);
            p = ClampToMap(p, inset);
            float dx = h - Mathf.Abs(p.x), dz = h - Mathf.Abs(p.z);
            if (dx < dz) p.x = (p.x < 0f ? -1f : 1f) * h;
            else p.z = (p.z < 0f ? -1f : 1f) * h;
            p.y = 0f;
            return p;
        }

        /// <summary>Creates a random point anywhere on the map, keeping a margin off the fence.</summary>
        public static Vector3 RandomMapPoint(System.Random rng, float margin)
        {
            float h = Mathf.Max(1f, MapHalf - margin);
            return new Vector3((float)(rng.NextDouble() * 2.0 - 1.0) * h, 0f, (float)(rng.NextDouble() * 2.0 - 1.0) * h);
        }

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

        /// <summary>"crashed_helicopter" → "crashed helicopter".</summary>
        /// <summary>Makes a readable string from an identifier by replacing underscores with spaces.</summary>
        public static string Pretty(string id) => string.IsNullOrEmpty(id) ? "" : id.Replace('_', ' ');

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

/// <summary>Every landmark the current map published ("landmark:&lt;id&gt;"), as (id, position). The returned list is a new instance; callers may modify it without affecting future calls.</summary>
        public static List<KeyValuePair<string, Vector3>> Landmarks()
        {
            var result = new List<KeyValuePair<string, Vector3>>(16);
            const string prefix = "landmark:";
            foreach (string kind in WorldMarkers.Kinds)
            {
                if (kind == null || !kind.StartsWith(prefix)) continue;
                IReadOnlyList<Vector3> pts = WorldMarkers.Get(kind);
                string id = kind.Substring(prefix.Length);
                for (int i = 0; i < pts.Count; i++) result.Add(new KeyValuePair<string, Vector3>(id, pts[i]));
            }
            return result;
        }

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
            float h = MapHalf;
            float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z));
            string where = edge < h * 0.3f ? "the middle" : "the " + Compass(p) + " part";
            string near = edge > h - 14f ? ", by the fence" : "";
            return where + " of " + (string.IsNullOrEmpty(zoneName) ? "the map" : zoneName) + near;
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
                    s_Instance = UnityEngine.Object.FindObjectOfType<HumanRunner>();
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
