using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A bed the player can sleep in to pass the night and shed fatigue. Keeps a
    /// static registry (mirroring <see cref="LootContainer"/>) so PlayerSleep can
    /// find the nearest one. Built entirely from code by the bootstrapper.
    /// </summary>
    public class SleepSpot : MonoBehaviour, IDemolishable
    {
        public static readonly List<SleepSpot> All = new List<SleepSpot>();
        public string DisplayName = "Bed";

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region IDemolishable
        public string DemolishName => "bed";
        public bool CanDemolish(out string reason) { reason = null; return true; }
        public void GetSalvage(out int planks, out int nails) { planks = CloneZConfig.SalvageBedPlanks; nails = CloneZConfig.SalvageBedNails; }
        public bool CarvesNavMesh => true;   // rebuild the navmesh so the bed's footprint frees up
        public void Dismantle() { All.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }   // out of the rebuild that follows
        #endregion

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

        /// <summary>Builds a bed — a wooden frame with headboard, made up with a quilt and pillows, head
        /// toward -Z — standing on the floor at <paramref name="position"/>, and returns its SleepSpot.</summary>
        public static SleepSpot Spawn(Transform parent, Vector3 position)
        {
            // Stand on the house floor where there is one: the sleep pose lies on the mattress 0.51 m above
            // the bed's own position, so the position itself has to rise with the floor.
            position.y = BuildPiece.FloorTopAt(position);
            var root = new GameObject("Bed");
            root.transform.SetParent(parent);
            root.transform.position = position;

            // Visual only — no collider, so the survivor can always reach the mattress to lie down.
            var m = new RaidMesh();
            m.SetFrame(position, 0f);
            FurnitureBuilder.Bed(m, new System.Random(1993), 1.4f, 2.0f, Quilt, FurnitureBuilder.Walnut);
            m.ResetFrame();
            m.ApplyTo(root, position);
            DecorPalette.Apply();

            return root.AddComponent<SleepSpot>();
        }

        private static readonly Color Quilt = new Color(0.55f, 0.18f, 0.15f);
    }
}
