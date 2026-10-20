using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Draws every item lying on the ground near the player.
    ///
    /// Ground items have no scene objects of their own — they are records in
    /// <see cref="GroundItemStore"/> — so something has to put them on screen. This gathers the items
    /// in the chunks around the camera, sorts them by item type and level of detail, and issues one
    /// instanced draw per group. The cost therefore scales with how many <em>kinds</em> of thing are
    /// in view rather than how many things: a thousand tin cans on the floor is one draw call, not a
    /// thousand, and certainly not the three hundred thousand the old GameObject visuals would have
    /// needed.
    ///
    /// The gentle spin and bob every dropped item has is folded into the instance matrix here, which
    /// is why the items need no per-object update of their own.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class GroundItemRenderer : MonoBehaviour
    {
        /// <summary>Singleton instance of the renderer for easy access.</summary>
        public static GroundItemRenderer Instance { get; private set; }

        [Tooltip("Items beyond this range are not drawn at all.")]

        /// <summary>Maximum distance at which items are drawn; beyond this they are invisible.</summary>
        public float DrawRadius = 60f;

        [Tooltip("Past this range items drop to the cheap silhouette mesh.")]
        /// <summary>Distance at which items switch to a cheap silhouette mesh.</summary>
        public float NearRadius = 14f;

        [Tooltip("Items spin and bob so they catch the eye, as they always have.")]
        /// <summary>Whether dropped items animate with a spin and bob to catch the player's eye.</summary>
        public bool Animate = true;

        /// <summary>Number of items drawn in the most recent frame.</summary>
        public int DrawnLastFrame { get; private set; }
        /// <summary>Number of draw batches submitted in the most recent frame.</summary>
        public int BatchesLastFrame { get; private set; }

        // Unity's instanced draw path takes at most 1023 matrices in one go.
        private const int BatchLimit = 1023;

        private readonly List<GroundItemStore.Chunk> _chunks = new List<GroundItemStore.Chunk>(64);

        // Grouped instance transforms, keyed by item type and level of detail. Kept between frames so
        // the per-frame work is filling them, not allocating them.
        private readonly Dictionary<int, List<Matrix4x4>> _groups = new Dictionary<int, List<Matrix4x4>>(64);
        private readonly List<int> _liveGroups = new List<int>(64);

        private readonly Plane[] _frustum = new Plane[6];
        private Matrix4x4[] _batch = new Matrix4x4[BatchLimit];
        private Vector3 _eye;
        private Camera _cachedMain;

        private void OnEnable()
            {
                Instance = this;
                _cachedMain = Camera.main;
            }
        private void OnDisable() { if (Instance == this) Instance = null; }

        private void LateUpdate()
        {
            if (_cachedMain == null || _cachedMain != Camera.main) _cachedMain = Camera.main;
            Camera cam = _cachedMain;

            // The isometric camera hangs well back from the survivor, so detail and draw range are
            // measured from the point it looks at; the shoulder / first-person cameras are close enough.
            _eye = ViewState.Current == CameraView.Isometric && IsoCameraFollow.Instance != null
                ? IsoCameraFollow.FocusPoint
                : cam.transform.position;
            GeometryUtility.CalculateFrustumPlanes(cam, _frustum);

            Gather(_eye);
            Draw(cam);
        }

        /// <summary>Walks the nearby chunks and files every visible item under its type and detail level.</summary>
        private void Gather(Vector3 eye)
        {
            for (int i = 0; i < _liveGroups.Count; i++) _groups[_liveGroups[i]].Clear();
            _liveGroups.Clear();
            DrawnLastFrame = 0;
            _chunks.Clear();

            GroundItemStore.ChunksInRange(eye, DrawRadius, _chunks);

            float drawSqr = DrawRadius * DrawRadius;
            float nearSqr = NearRadius * NearRadius;
            float t = Time.time;

            for (int c = 0; c < _chunks.Count; c++)
            {
                List<GroundItemStore.Item> items = _chunks[c].Items;

                for (int i = 0; i < items.Count; i++)
                {
                    GroundItemStore.Item item = items[i];

                    float dx = item.X - eye.x;
                    float dz = item.Z - eye.z;
                    float d2 = dx * dx + dz * dz;
                    if (d2 > drawSqr) continue;

                    ItemDef def = GroundItemStore.TypeAt(item.TypeIndex);
                    if (def == null) continue;

                    ItemMeshBaker.Lod lod = d2 <= nearSqr ? ItemMeshBaker.Lod.Near : ItemMeshBaker.Lod.Far;
                    ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, lod);
                    if (baked == null) continue;   // no bespoke model; nothing to draw here yet

                    float height = baked.BaseHeight;
                    float yaw = item.YawDegrees;

                    if (Animate)
                    {
                        // The same spin and bob every dropped item used to do in its own Update, only
                        // now it is a few instructions while filling the instance matrix.
                        float phase = item.Id * 0.61803f;   // spread the bob so a pile does not pulse as one
                        yaw += t * 60f;
                        height += Mathf.Sin((t + phase) * 2f) * 0.06f;
                    }

                    var centre = new Vector3(item.X, height, item.Z);

                    // Cull against the view before paying for the matrix.
                    float radius = baked.Bounds.extents.magnitude;
                    if (!InView(centre, radius)) continue;

                    var trs = Matrix4x4.TRS(centre, Quaternion.Euler(0f, yaw, 0f), Vector3.one);

                    int key = (item.TypeIndex << 1) | (int)lod;
                    if (!_groups.TryGetValue(key, out List<Matrix4x4> list))
                    {
                        list = new List<Matrix4x4>(64);
                        _groups[key] = list;
                    }
                    if (list.Count == 0) _liveGroups.Add(key);

                    list.Add(trs);
                    DrawnLastFrame++;
                }
            }
        }

        private bool InView(Vector3 centre, float radius)
        {
            for (int p = 0; p < 6; p++)
            {
                if (_frustum[p].GetDistanceToPoint(centre) < -radius) return false;
            }
            return true;
        }

        /// <summary>Issues one instanced draw per group, per submesh.</summary>
        private void Draw(Camera cam)
        {
            BatchesLastFrame = 0;

            for (int g = 0; g < _liveGroups.Count; g++)
            {
                int key = _liveGroups[g];
                List<Matrix4x4> list = _groups[key];
                if (list.Count == 0) continue;

                var lod = (ItemMeshBaker.Lod)(key & 1);
                ItemDef def = GroundItemStore.TypeAt(key >> 1);
                ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, lod);
                if (baked == null) continue;

                for (int sub = 0; sub < baked.Materials.Length; sub++)
                {
                    var rp = new RenderParams(baked.Materials[sub])
                    {
                        camera = cam,
                        receiveShadows = true,
                        // Instanced draws are culled against this as a whole, so it has to cover
                        // everywhere the batch might put an item, not the mesh's own extents.
                        worldBounds = new Bounds(_eye, Vector3.one * (DrawRadius * 2f + 10f)),
                        shadowCastingMode = lod == ItemMeshBaker.Lod.Near
                            ? UnityEngine.Rendering.ShadowCastingMode.On
                            : UnityEngine.Rendering.ShadowCastingMode.Off,
                    };

                    for (int start = 0; start < list.Count; start += BatchLimit)
                    {
                        int n = Mathf.Min(BatchLimit, list.Count - start);
                        list.CopyTo(start, _batch, 0, n);
                        Graphics.RenderMeshInstanced(rp, baked.Mesh, sub, _batch, n);
                        BatchesLastFrame++;
                    }
                }
            }
        }
    }
}
