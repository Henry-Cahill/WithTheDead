using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A bed the player can sleep in to pass the night and shed fatigue. Keeps a
    /// static registry (mirroring <see cref="LootContainer"/>) so PlayerSleep can
    /// find the nearest one. Built entirely from code by the bootstrapper, with its
    /// <see cref="BedFixture"/> (sleep, lie down, sit on the edge, make the bed).
    /// </summary>
    public class SleepSpot : MonoBehaviour, IDemolishable
    {
        public static readonly List<SleepSpot> All = new List<SleepSpot>();
        public string DisplayName = "Bed";

        /// <summary>The bed's size: width along X, length along Z (the head at -Z).</summary>
        public const float Width = 1.4f, Length = 2.0f;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region IDemolishable
        public string DemolishName => "bed";
        public bool CanDemolish(out string reason) { reason = null; return true; }
        public void GetSalvage(out int planks, out int nails) { planks = CloneZConfig.SalvageBedPlanks; nails = CloneZConfig.SalvageBedNails; }
        public bool CarvesNavMesh => true;   // rebuild the navmesh so the bed's footprint frees up
        public void Dismantle() { All.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }   // out of the rebuild that follows
        #endregion

        /// <summary>Finds the nearest SleepSpot to a point within a range, returning the distance.</summary>
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
        /// toward -Z (turned by <paramref name="yaw"/> degrees) — standing on the floor at
        /// <paramref name="position"/>, and returns its SleepSpot. <paramref name="quilt"/> colours the quilt
        /// (the starter home's red by default).</summary>
        public static SleepSpot Spawn(Transform parent, Vector3 position, float yaw = 0f, Color? quilt = null)
        {
            // Stand on the house floor where there is one: the sleep pose lies on the mattress 0.51 m above
            // the bed's own position, so the position itself has to rise with the floor.
            position.y = BuildPiece.FloorTopAt(position);
            var root = new GameObject("Bed");
            root.transform.SetParent(parent);
            // The frame and bedding are drawn unturned round the position; the root's turn (which the sleep pose
            // and the bed's fixture both follow) turns them with it.
            root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            // Visual only — no collider, so the survivor can always reach the mattress to lie down.
            var m = new RaidMesh();
            m.SetFrame(position, 0f);
            FurnitureBuilder.BedFrame(m, new System.Random(1993), Width, Length, FurnitureBuilder.Walnut);
            m.ResetFrame();
            m.ApplyTo(root, position);

            // The bedding, made and rumpled: two renderers the bed's fixture swaps between (kept out of the navmesh).
            Color cover = quilt ?? Quilt;
            Renderer made = Bedding(root.transform, position, true, cover);
            Renderer rumpled = Bedding(root.transform, position, false, cover);
            DecorPalette.Apply();

            var spot = root.AddComponent<SleepSpot>();
            root.AddComponent<BedFixture>().Bind(spot, made, rumpled);
            return spot;
        }

        private static Renderer Bedding(Transform root, Vector3 position, bool made, Color quilt)
        {
            var m = new RaidMesh();
            m.SetFrame(position, 0f);
            FurnitureBuilder.BedQuilt(m, new System.Random(made ? 1994 : 1995), Width, Length, quilt, made);
            m.ResetFrame();
            GameObject go = m.Emit(made ? "QuiltMade" : "QuiltRumpled", root, position);
            if (go == null) return null;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var r = go.GetComponent<Renderer>();
            r.enabled = made;
            return r;
        }

        private static readonly Color Quilt = new Color(0.55f, 0.18f, 0.15f);
    }
}
