using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A stove the survivor can cook raw ingredients at. Keeps a static registry
    /// (mirroring <see cref="SleepSpot"/> and <see cref="LootContainer"/>) so the
    /// inventory panel can tell when the player is close enough to cook. Built
    /// entirely from code by the bootstrapper; visual only, so it never disturbs
    /// the baked navmesh.
    /// </summary>
    public class CookingStation : MonoBehaviour, IDemolishable
    {
        public static readonly List<CookingStation> All = new List<CookingStation>();
        public string DisplayName = "Stove";

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region IDemolishable
        public string DemolishName => "stove";
        public bool CanDemolish(out string reason) { reason = null; return true; }
        public void GetSalvage(out int planks, out int nails) { planks = CloneZConfig.SalvageStovePlanks; nails = CloneZConfig.SalvageStoveNails; }
        public bool CarvesNavMesh => true;
        public void Dismantle() { All.Remove(this); Destroy(gameObject); }
        #endregion

        /// <summary>Nearest stove within <paramref name="range"/> of a point, or null.</summary>
        public static CookingStation Nearest(Vector3 p, float range)
        {
            CookingStation best = null;
            float bestSqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                var s = All[i];
                if (s == null) continue;
                float d = (s.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = s; }
            }
            return best;
        }

        /// <summary>True when a stove is within reach of the given point.</summary>
        public static bool InRange(Vector3 p, float range) => Nearest(p, range) != null;

        /// <summary>Builds a simple stove (body, cooktop, two burners) and returns its CookingStation.</summary>
        public static CookingStation Spawn(Transform parent, Vector3 position)
        {
            var root = new GameObject("Stove");
            root.transform.SetParent(parent);
            root.transform.position = position;

            var body = new Color(0.72f, 0.73f, 0.76f);   // enamelled steel
            var top = new Color(0.18f, 0.18f, 0.20f);     // dark cooktop
            var burner = new Color(0.85f, 0.28f, 0.18f);  // warm element

            // Visual only — colliders stripped so the stove never blocks the baked navmesh.
            Strip(CloneZUtil.Box("Body", position + new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.8f), body, root.transform));
            Strip(CloneZUtil.Box("Cooktop", position + new Vector3(0f, 0.92f, 0f), new Vector3(0.94f, 0.06f, 0.84f), top, root.transform));
            Strip(CloneZUtil.Box("BurnerL", position + new Vector3(-0.2f, 0.97f, 0.16f), new Vector3(0.28f, 0.04f, 0.28f), burner, root.transform));
            Strip(CloneZUtil.Box("BurnerR", position + new Vector3(0.2f, 0.97f, -0.16f), new Vector3(0.28f, 0.04f, 0.28f), burner, root.transform));

            return root.AddComponent<CookingStation>();
        }

        private static void Strip(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
    }
}
