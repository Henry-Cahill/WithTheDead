using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The real map's landmarks as a survivor finds them: one component (created with the first landmark worth
    /// discovering, under the raid's world) checks twice a second which of the tracked landmarks the local survivor has
    /// reached. The first time ever, the landmark is learnt for good (account knowledge, like the Zone's set pieces), the
    /// survivor is told ("Discovered the Bardstown courthouse.") and rewarded once (<see cref="GameEvents.LocationVisited"/>);
    /// after that it's simply known, so walking a town you know earns nothing. The landmarks come from
    /// <see cref="MapGameplay"/> as the map is built (each streamed piece adds its own).
    /// </summary>
    public sealed class MapGameplayRuntime : MonoBehaviour
    {
        private const float Cell = 64f;
        private const float Interval = 0.5f;

        private struct Entry
        {
            public string Id, Name;
            public Vector3 At;
            public float Radius;
        }

        private static MapGameplayRuntime s_Instance;
        private static readonly Dictionary<long, List<Entry>> s_Cells = new Dictionary<long, List<Entry>>();
        private static readonly HashSet<string> s_Tracked = new HashSet<string>();
        private static readonly HashSet<string> s_Reached = new HashSet<string>();
        private float _next;

        /// <summary>Forgets every landmark (a new raid's map).</summary>
        internal static void Reset()
        {
            s_Cells.Clear();
            s_Tracked.Clear();
            s_Reached.Clear();
        }

        /// <summary>Watches for the local survivor reaching a landmark (within <paramref name="radius"/> of
        /// <paramref name="at"/>, world metres); <paramref name="name"/> as told ("the Bardstown courthouse"). Only in
        /// play: an edit-mode build has nobody to discover anything.</summary>
        internal static void Track(string id, string name, Vector3 at, float radius, Transform host)
        {
            if (!Application.isPlaying || string.IsNullOrEmpty(id) || !s_Tracked.Add(id)) return;
            long key = Key(at);
            if (!s_Cells.TryGetValue(key, out List<Entry> list)) s_Cells[key] = list = new List<Entry>(4);
            list.Add(new Entry { Id = id, Name = name, At = at, Radius = radius });
            if (s_Instance == null && host != null)
            {
                var go = new GameObject("MapLandmarks");
                go.transform.SetParent(host, false);
                s_Instance = go.AddComponent<MapGameplayRuntime>();
            }
        }

        private static long Key(Vector3 p) => ((long)Mathf.FloorToInt(p.x / Cell) << 32) | (uint)Mathf.FloorToInt(p.z / Cell);

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + Interval;
            Actor me = Actor.LocalPlayer;
            if (me == null || !me.IsAlive) return;
            Vector3 p = me.transform.position;
            int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
            for (int z = cz - 1; z <= cz + 1; z++)
                for (int x = cx - 1; x <= cx + 1; x++)
                {
                    if (!s_Cells.TryGetValue(((long)x << 32) | (uint)z, out List<Entry> list)) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        Entry e = list[i];
                        float dx = e.At.x - p.x, dz = e.At.z - p.z;
                        if (dx * dx + dz * dz > e.Radius * e.Radius || s_Reached.Contains(e.Id)) continue;
                        Reach(e);
                    }
                }
        }

        private static void Reach(Entry e)
        {
            s_Reached.Add(e.Id);
            AccountProfile account = ProfileStore.Account;
            if (account == null || !account.Learn("landmark:" + e.Id)) return;
            GameEvents.RaiseLocationVisited(e.Id);
            GameEvents.RaiseToast("Discovered " + e.Name + ".");
            ProfileStore.Save();
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }
    }
}
