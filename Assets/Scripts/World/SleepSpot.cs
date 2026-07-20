using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A bed the player can sleep in to pass the night and shed fatigue. Keeps a
    /// static registry (mirroring <see cref="LootContainer"/>) so PlayerSleep can
    /// find the nearest one. Built entirely from code by the bootstrapper.
    /// </summary>
    public class SleepSpot : MonoBehaviour
    {
        public static readonly List<SleepSpot> All = new List<SleepSpot>();
        public string DisplayName = "Bed";

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        public static SleepSpot Nearest(Vector3 p, float range, out float distance)
        {
            SleepSpot best = null;
            float bestSqr = range * range;
            distance = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var s = All[i];
                if (s == null) continue;
                float d = (s.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = s; distance = Mathf.Sqrt(d); }
            }
            return best;
        }

        /// <summary>Builds a simple bed (frame, mattress, pillow) and returns its SleepSpot.</summary>
        public static SleepSpot Spawn(Transform parent, Vector3 position)
        {
            var root = new GameObject("Bed");
            root.transform.SetParent(parent);
            root.transform.position = position;

            // Visual only — no colliders, so the bed never blocks the baked navmesh.
            Strip(CloneZUtil.Box("Frame", position + new Vector3(0f, 0.15f, 0f), new Vector3(1.55f, 0.3f, 2.55f), new Color(0.35f, 0.24f, 0.16f), root.transform));
            Strip(CloneZUtil.Box("Mattress", position + new Vector3(0f, 0.36f, 0f), new Vector3(1.4f, 0.3f, 2.4f), new Color(0.70f, 0.71f, 0.78f), root.transform));
            Strip(CloneZUtil.Box("Pillow", position + new Vector3(0f, 0.53f, -0.85f), new Vector3(1.1f, 0.18f, 0.5f), new Color(0.9f, 0.9f, 0.92f), root.transform));

            return root.AddComponent<SleepSpot>();
        }

        private static void Strip(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
        }
    }
}
