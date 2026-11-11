using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>How an item sits in the right hand.</summary>
    public enum HoldStyle : byte { None, Melee, Firearm, Carry }

    /// <summary>
    /// A procedurally built humanoid: a 17-bone skeleton and one skinned mesh generated from a
    /// <see cref="HumanoidLook"/>, plus whatever item is in the right hand (a gun with its attachments, see
    /// <see cref="SetHeldMods"/>). It holds no behaviour —
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
        /// <summary>The attachments built onto the held gun (null with none fitted; see <see cref="SetHeldMods"/>).</summary>
        public WeaponAttachments.Fitted HeldAttachments { get; private set; }

        private HumanoidMesh _mesh;
        private readonly List<string> _heldMods = new List<string>(4);
        private Vector3 _plainSupport;   // HeldSupport before a foregrip moved it

        // The held item's model, held (ItemMeshBaker.Hold) while it's in the hand so the generated meshes' cache keeps it.
        private ItemMeshBaker.Baked _heldModel;

        // Picks which look of a generated item mesh this character holds (see SetHeldItem); 0 until first needed.
        private static int s_NextLookSeed;
        private uint _lookSeed;
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
            ItemMeshBaker.Release(_heldModel);
            _heldModel = null;
            Held = null;
            HeldDef = null;
            HeldStyle = HoldStyle.None;
            HeldSupport = HeldButt = Vector3.zero;
            HeldLength = 0f;
            HeldAttachments = null;   // they went with the old item
            _heldMods.Clear();
            if (style == HoldStyle.None) return;

            // Characters holding the same kind of item show different looks of it, each keeping its own.
            int looks = ItemMeshBaker.VariantCount(def);
            if (_lookSeed == 0) _lookSeed = (uint)System.Threading.Interlocked.Increment(ref s_NextLookSeed) * 0x9E3779B1u | 1u;
            uint pick = 2166136261u;
            foreach (char ch in def.Id) pick = (pick ^ ch) * 16777619u;
            pick ^= _lookSeed;
            ItemMeshBaker.Baked baked = ItemMeshBaker.Get(def, ItemMeshBaker.Lod.Near,
                looks > 1 ? (int)((pick ^ (pick >> 16)) % (uint)looks) : 0);
            if (baked == null || baked.Mesh == null) return;

            Bounds b = baked.Bounds;
            Vector3 min = b.min, size = b.size, c = b.center;
            float len = size.x;
            Quaternion q, turn = Quaternion.identity;
            Vector3 grip, support, butt;
            switch (style)
            {
                case HoldStyle.Firearm:
                {
                    // Barrel (item +X) along the forearm (hand -Y), top of the gun (item +Z) up the thumb side. A model made
                    // end for end (some generated ones are) is turned round first, and held by its own outline (GunShape).
                    // The fist closes on the gun's grip and the off hand goes on its forend, pump or handguard, where the
                    // gun's own hold puts them (GunStats.Hold, shares of its box), else at the usual places for its length; a
                    // share that lands beside the gun (a look's bipod or sling stretches its box) goes onto the outline there.
                    q = Quaternion.LookRotation(Vector3.forward, Vector3.right);
                    GunShape shape = GunShape.Of(def, baked.Mesh);
                    if (shape != null && shape.Reversed)
                    {
                        turn = shape.ToGun;
                        min = shape.Box.min;
                        c = shape.Box.center;
                    }
                    bool pistol = len < GunShape.PistolLength;
                    GunStats stats = Ballistics.Gun(def.Id);
                    GunHold hold = stats != null && stats.Hold.IsSet ? stats.Hold : GunHold.Usual(pistol);
                    grip = OntoGun(shape, new Vector3(min.x + hold.GripX * len, c.y, min.z + hold.GripZ * size.z));
                    support = pistol ? grip + new Vector3(0.012f, 0f, -0.03f)
                        : OntoGun(shape, new Vector3(min.x + hold.SupportX * len, c.y, min.z + hold.SupportZ * size.z));
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
            // The grip, support and butt are in gun space; a turned model is drawn turned (q * turn) so they land the same.
            t.localRotation = q * turn;
            t.localPosition = -(q * grip);
            go.AddComponent<MeshFilter>().sharedMesh = baked.Mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = baked.Materials;
            mr.enabled = Body.enabled;   // follow the body, which the camera hides in first person
            ItemMeshBaker.Hold(baked);
            _heldModel = baked;

            Held = t;
            HeldDef = def;
            HeldStyle = style;
            HeldSupport = t.localPosition + q * support;
            _plainSupport = HeldSupport;
            HeldButt = t.localPosition + q * butt;
            HeldLength = len;
        }

        // A look whose box isn't the hand-built model's (a bipod's legs, a sling, a longer magazine) can put a share of the box
        // beside the gun: a hand that would close on air there goes up or down onto the gun's outline (gun space).
        private static Vector3 OntoGun(GunShape shape, Vector3 p)
        {
            if (shape == null) return p;
            float top = shape.TopAt(p.x - 0.015f, p.x + 0.015f, out _), bottom = shape.BottomAt(p.x - 0.015f, p.x + 0.015f, out _);
            float inset = Mathf.Min(0.01f, (top - bottom) * 0.5f);
            p.z = Mathf.Clamp(p.z, bottom + inset, top - inset);
            return p;
        }

        /// <summary>
        /// Builds the attachments in <paramref name="mods"/> (a gun's <see cref="ItemExtra.Mods"/>) onto the gun put in the
        /// hand with <see cref="SetHeldItem"/>; null or empty takes them off. Cheap to call every frame: nothing is rebuilt
        /// until the list changes. A foregrip moves where the off hand holds the gun (<see cref="HeldSupport"/>).
        /// </summary>
        public void SetHeldMods(IReadOnlyList<string> mods)
        {
            int n = mods != null && Held != null && HeldStyle == HoldStyle.Firearm ? mods.Count : 0;
            if (n == _heldMods.Count)
            {
                bool same = true;
                for (int i = 0; i < n && same; i++) same = mods[i] == _heldMods[i];
                if (same) return;
            }

            if (HeldAttachments != null && HeldAttachments.Root != null)
            {
                if (Application.isPlaying) Destroy(HeldAttachments.Root.gameObject);
                else DestroyImmediate(HeldAttachments.Root.gameObject);
            }
            HeldAttachments = null;
            _heldMods.Clear();
            if (Held != null) HeldSupport = _plainSupport;
            if (n == 0) return;

            for (int i = 0; i < n; i++) _heldMods.Add(mods[i]);
            MeshFilter mf = Held.GetComponent<MeshFilter>();
            HeldAttachments = WeaponAttachments.Fit(Held, HeldDef, mf != null ? mf.sharedMesh : null, _heldMods, Body.enabled);
            if (HeldAttachments != null && HeldAttachments.Support.HasValue)
                HeldSupport = Held.localPosition + Held.localRotation * HeldAttachments.Support.Value;
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
            HeldAttachments?.SetVisible(visible);
        }

        private void OnDestroy()
        {
            HumanoidMeshFactory.Release(_mesh);
            _mesh = null;
            ItemMeshBaker.Release(_heldModel);
            _heldModel = null;
        }
    }
}
