using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A stove the survivor can cook raw ingredients at. Keeps a static registry
    /// (mirroring <see cref="SleepSpot"/> and <see cref="LootContainer"/>) so the
    /// inventory panel can tell when the player is close enough to cook. Built
    /// entirely from code by the bootstrapper with the raid map's furniture builder;
    /// its mesh is baked into the navmesh, so dismantling it rebuilds the navmesh.
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
        public void Dismantle() { All.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }   // out of the rebuild that follows
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

        /// <summary>Builds a white enamel range (four burners, a knob panel and an oven door, its back toward
        /// -Z) standing on the floor at <paramref name="position"/>, and returns its CookingStation.</summary>
        public static CookingStation Spawn(Transform parent, Vector3 position)
        {
            position.y = BuildPiece.FloorTopAt(position);
            var root = new GameObject("Stove");
            root.transform.SetParent(parent);
            root.transform.position = position;

            var m = new RaidMesh();
            m.SetFrame(position, 0f);
            FurnitureBuilder.Stove(m, new System.Random(1993), new Color(0.86f, 0.85f, 0.80f));
            m.ResetFrame();
            m.ApplyTo(root, position);
            DecorPalette.Apply();

            // Solid like the kitchen counters beside it (its mesh is baked into the navmesh either way).
            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.54f, -0.02f);
            col.size = new Vector3(0.76f, 1.08f, 0.62f);

            return root.AddComponent<CookingStation>();
        }
    }
}
