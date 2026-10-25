using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>How an item sits in the right hand.</summary>
    public enum HoldStyle : byte { None, Melee, Firearm, Carry }

    /// <summary>
    /// A procedurally built humanoid: a 17-bone skeleton and one skinned mesh generated from a
    /// <see cref="HumanoidLook"/>, plus whatever item is in the right hand. It holds no behaviour —
    /// <see cref="HumanoidAnimator"/> poses the bones — so the same rig serves the survivor and the dead.
    ///
    /// The whole body is a single renderer with one submesh per colour region, and the mesh is shared
    /// by every character of the same shape, so a crowd costs a handful of draw calls per body.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HumanoidRig : MonoBehaviour
    {
        /// <summary>All bone transforms of the humanoid skeleton.</summary>
        public Transform[] Bones { get; private set; }
        /// <summary>The mesh renderer that draws the character's body.</summary>
        public SkinnedMeshRenderer Body { get; private set; }
        /// <summary>The centre of the right fist; held items hang off it.</summary>
        public Transform GripR { get; private set; }
        /// <summary>The visual look data defining shape and colours.</summary>
        public HumanoidLook Look { get; private set; }
        /// <summary>The proportion data for the character's body shape.</summary>
        public HumanoidProportions Proportions { get; private set; }

        /// <summary>Definition of the item currently held in the right hand.</summary>
        public ItemDef HeldDef { get; private set; }
        /// <summary>How the held item is being used (none, melee, firearm, carry).</summary>
        public HoldStyle HeldStyle { get; private set; }
        /// <summary>Transform of the instantiated held item.</summary>
        public Transform Held { get; private set; }
        /// <summary>Where the off hand takes hold of the held item, in <see cref="GripR"/> space.</summary>
        public Vector3 HeldSupport { get; private set; }
        /// <summary>The butt of the held item (the stock of a long gun), in <see cref="GripR"/> space.</summary>
        public Vector3 HeldButt { get; private set; }
        /// <summary>Length of the held item along its long axis, in metres.</summary>
        public float HeldLength { get; private set; }

        private HumanoidMesh _mesh;
        private Material[] _materials;

        /// <summary>Grip centre relative to the wrist of a right hand (mirror X for the left).</summary>
        public static readonly Vector3 GripOffset = new Vector3(-0.022f, -0.085f, 0.012f);

        /// <summary>Factory method that creates a new HumanoidRig under a parent.</summary>
        public static HumanoidRig Create(Transform parent, HumanoidLook look, string name = "Visual")
        {
            var go = new GameObject(name);
            go.layer = parent != null ? parent.gameObject.layer : 0;
            go.transform.SetParent(parent, false);
            var rig = go.AddComponent<HumanoidRig>();
            rig.ApplyLook(look);
            return rig;
        }

        /// <summary>Gets the transform for a specific bone.</summary>
        public Transform Bone(HumanBone bone) => Bones[(int)bone];

        /// <summary>Constructs the bone hierarchy, grip transform and body renderer the first time the rig is needed.</summary>
        private void EnsureBuilt()
        {
            if (Bones != null) return;

            int count = HumanoidProportions.BoneCount;
            Bones = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                var b = new GameObject(HumanoidProportions.Names[i]).transform;
                b.gameObject.layer = gameObject.layer;
                int parent = HumanoidProportions.Parent[i];
                b.SetParent(parent < 0 ? transform : Bones[parent], false);
                Bones[i] = b;
            }

            GripR = new GameObject("Grip.R").transform;
            GripR.gameObject.layer = gameObject.layer;
            GripR.SetParent(Bones[(int)HumanBone.HandR], false);

            var bodyGo = new GameObject("Body");
            bodyGo.layer = gameObject.layer;
            bodyGo.transform.SetParent(transform, false);
            Body = bodyGo.AddComponent<SkinnedMeshRenderer>();
            Body.bones = Bones;
            Body.rootBone = Bones[(int)HumanBone.Hips];
            Body.quality = SkinQuality.Bone2;
            Body.updateWhenOffscreen = false;
            Body.shadowCastingMode = ShadowCastingMode.On;
            // Bounds around the hips that hold any pose, lying down included.
            Body.localBounds = new Bounds(Vector3.zero, new Vector3(2.6f, 2.6f, 2.6f));
        }

        /// <summary>Returns every bone to the bind pose (standing straight, arms hanging).</summary>
        public void ResetPose()
        {
            EnsureBuilt();
            if (Proportions == null) return; // Guard against null when no look applied yet
            for (int i = 0; i < Bones.Length; i++)
            {
                Bones[i].localPosition = Proportions.LocalBind(i);
                Bones[i].localRotation = Quaternion.identity;
            }
        }

        /// <summary>Rebuilds the body for a new look. Only a change of shape swaps the mesh; a change of
        /// colour just picks different shared materials.</summary>
        public void ApplyLook(HumanoidLook look)
        {
            if (look == null) return;
            EnsureBuilt();
            Look = look;

            // Resolve shape‑specific body proportions from the look's shape definition.
            HumanoidProportions p = HumanoidProportions.For(look.Shape.Build);
            // If the underlying shape changed, rebuild the skeleton pose and update grip offset.
            if (p != Proportions)
            {
                // Store the new proportions, reset all bones to the bind pose, and resize the hand grip.
                Proportions = p;
                ResetPose();
                GripR.localPosition = GripOffset * p.Hand;
            }

            // Acquire (or reuse) the mesh matching the new shape and release any previously held mesh.
            HumanoidMesh next = HumanoidMeshFactory.Acquire(look.Shape);
            HumanoidMeshFactory.Release(_mesh);
            _mesh = next;
            // Assign the newly acquired mesh to the body renderer.
            Body.sharedMesh = next.Mesh;

            // Ensure the material array matches the number of mesh slots; allocate if needed.
            if (_materials == null || _materials.Length != next.Slots.Length) _materials = new Material[next.Slots.Length];
            // Populate each material slot with a colour+gloss combination from the look.
            for (int i = 0; i < next.Slots.Length; i++)
            {
                HumanoidSlot slot = next.Slots[i];
                _materials[i] = CloneZUtil.Mat(look.ColorOf(slot), look.GlossOf(slot));
            }
            // Apply the prepared material array to the renderer.
            Body.sharedMaterials = _materials;
            // Scale the whole rig uniformly, clamping to a minimum size to avoid degenerate meshes.
            transform.localScale = Vector3.one * Mathf.Max(0.1f, look.Scale);
        }

        /// <summary>
        /// Puts an item in the right hand, drawn with its own ground model. Every item model lies along
        /// X with its business end (head, blade, muzzle) at +X, so one rule per hold seats them all:
        /// a melee weapon's handle runs through the fist with the head forward, a gun's barrel runs on
        /// from the forearm with the top of the gun up, and a carried thing hangs from its top.
        /// </summary>
        public void SetHeldItem(ItemDef def, HoldStyle style)
        {
            EnsureBuilt();
            if (def == null) style = HoldStyle.None;
            if (def == HeldDef && style == HeldStyle && Held != null) return;

            if (Held != null)
            {
                if (Application.isPlaying) Destroy(Held.gameObject);
                else DestroyImmediate(Held.gameObject);
            }
            Held = null;
            HeldDef = null;
            HeldStyle = HoldStyle.None;
            HeldSupport = HeldButt = Vector3.zero;
            HeldLength = 0f;
            if (style == HoldStyle.None) return;

            ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, ItemMeshBaker.Lod.Near);
            if (baked == null || baked.Mesh == null) return;

            Bounds b = baked.Bounds;
            Vector3 min = b.min, size = b.size, c = b.center;
            float len = size.x;
            Quaternion q;
            Vector3 grip, support, butt;
            switch (style)
            {
                case HoldStyle.Firearm:
                {
                    // Barrel (item +X) along the forearm (hand -Y), top of the gun (item +Z) up the thumb side.
                    q = Quaternion.LookRotation(Vector3.forward, Vector3.right);
                    bool pistol = len < 0.42f;
                    grip = new Vector3(min.x + (pistol ? 0.2f : 0.3f) * len, c.y, min.z + (pistol ? 0.45f : 0.4f) * size.z);
                    support = pistol ? grip + new Vector3(0.012f, 0f, -0.03f) : new Vector3(min.x + 0.66f * len, c.y, c.z - 0.1f * size.z);
                    butt = new Vector3(min.x, c.y, c.z);
                    break;
                }

                case HoldStyle.Carry:
                    // Hangs upright from its top, spout or front (item +X) facing forward.
                    q = Quaternion.Euler(0f, -90f, 0f);
                    grip = new Vector3(c.x, b.max.y - 0.015f, c.z);
                    support = butt = grip;
                    break;

                default:
                {
                    // Handle through the fist (item +X along hand +Z), the head's spine side up the arm.
                    q = Quaternion.LookRotation(Vector3.up, Vector3.right);
                    float g = Mathf.Clamp(len * 0.12f, 0.045f, 0.14f);
                    grip = new Vector3(min.x + g, c.y, c.z);
                    support = grip + new Vector3(Mathf.Clamp(len * 0.17f, 0.09f, 0.17f), 0f, 0f);
                    butt = new Vector3(min.x, c.y, c.z);
                    break;
                }
            }

            var go = new GameObject("Held_" + def.Id);
            go.layer = gameObject.layer;
            Transform t = go.transform;
            t.SetParent(GripR, false);
            t.localRotation = q;
            t.localPosition = -(q * grip);
            go.AddComponent<MeshFilter>().sharedMesh = baked.Mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = baked.Materials;
            mr.enabled = Body.enabled;   // follow the body, which the camera hides in first person

            Held = t;
            HeldDef = def;
            HeldStyle = style;
            HeldSupport = t.localPosition + q * support;
            HeldButt = t.localPosition + q * butt;
            HeldLength = len;
        }

        /// <summary>Shows or hides the body together with whatever it is holding.</summary>
        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            Body.enabled = visible;
            if (Held != null)
            {
                var mr = Held.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = visible;
            }
        }

        private void OnDestroy()
        {
            HumanoidMeshFactory.Release(_mesh);
            _mesh = null;
        }
    }
}
