using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A scattered arena cover block — a solid box that breaks line of sight and carves the
    /// baked NavMesh. Unlike the collider-less furniture, cover is baked into the NavMesh, so
    /// demolishing one asks the <see cref="WorldNavMesh"/> to rebuild. Keeps a static registry
    /// so the run can be captured/restored (and, as a bonus, so gameplay could query it later).
    /// </summary>
    public class Cover : MonoBehaviour, IDemolishable
    {
        public static readonly List<Cover> All = new List<Cover>();

        /// <summary>The block's box dimensions (persisted so a saved run rebuilds it exactly).</summary>
        public Vector3 Size { get; private set; }

        private static readonly Color CoverColor = new Color(0.30f, 0.27f, 0.24f);

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        /// <summary>Builds a cover block. <paramref name="center"/> is the box centre (its y is half
        /// the height, so the block rests on the ground).</summary>
        public static Cover Spawn(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject go = CloneZUtil.Box("Cover", center, size, CoverColor, parent);
            var c = go.AddComponent<Cover>();
            c.Size = size;
            return c;
        }

        #region IDemolishable
        public string DemolishName => "cover";
        public bool CanDemolish(out string reason) { reason = null; return true; }
        public void GetSalvage(out int planks, out int nails)
        {
            planks = CloneZConfig.SalvageCoverPlanks;
            nails = CloneZConfig.SalvageCoverNails;
        }
        public bool CarvesNavMesh => true;   // baked into the NavMesh -> removing it needs a rebuild

        public void Dismantle()
        {
            All.Remove(this);
            gameObject.SetActive(false);   // drop out of the NavMesh bake before the rebuild runs
            Destroy(gameObject);
        }
        #endregion
    }
}
