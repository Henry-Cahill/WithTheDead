using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Named points the current world publishes for other systems: landmarks ("landmark:crashed_helicopter"),
    /// the cordon's towers and gate, outposts, the Lodestar site, cemeteries. Builders add them while the map is
    /// built; spawners, quests and A-Life read them once the raid starts. Cleared for every new world.
    /// </summary>
    public static class WorldMarkers
    {
        public const string CordonTower = "cordon_tower", CordonGate = "cordon_gate", Outpost = "outpost",
            LodestarSite = "lodestar_site", Cemetery = "cemetery", HumanCamp = "human_camp", MutantLair = "mutant_lair",
            VehicleSpot = "vehicle_spot", Landing = "landing_zone";

        private static readonly Dictionary<string, List<Vector3>> s_Points = new Dictionary<string, List<Vector3>>();

        public static void Clear() => s_Points.Clear();

        public static void Add(string kind, Vector3 position)
        {
            if (string.IsNullOrEmpty(kind)) return;
            if (!s_Points.TryGetValue(kind, out List<Vector3> list)) s_Points[kind] = list = new List<Vector3>();
            list.Add(position);
        }

        /// <summary>Every point of a kind (an empty list when none).</summary>
        public static IReadOnlyList<Vector3> Get(string kind)
            => kind != null && s_Points.TryGetValue(kind, out List<Vector3> list) ? list : (IReadOnlyList<Vector3>)s_Empty;

        public static bool TryGetFirst(string kind, out Vector3 position)
        {
            IReadOnlyList<Vector3> l = Get(kind);
            position = l.Count > 0 ? l[0] : Vector3.zero;
            return l.Count > 0;
        }

        /// <summary>Landmark positions are published as "landmark:&lt;id&gt;".</summary>
        public static string Landmark(string id) => "landmark:" + id;

        public static IEnumerable<string> Kinds => s_Points.Keys;

        private static readonly List<Vector3> s_Empty = new List<Vector3>();
    }

    /// <summary>Names and descriptions of the Zone marks a body can carry (filled by the exposure system).</summary>
    public static class ZoneMarkRegistry
    {
        public static readonly Dictionary<string, string> Names = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>();

        public static string NameOf(string id) => id != null && Names.TryGetValue(id, out string n) ? n : id;
        public static string Describe(string id) => id != null && Descriptions.TryGetValue(id, out string d) ? d : "";
    }
}
