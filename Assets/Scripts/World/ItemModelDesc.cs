using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Which primitive mesh a part is cut from. Mirrors the shapes the item models are
    /// assembled out of, without binding a part to a Unity object.</summary>
    public enum PartShape : byte
    {
        Cube = 0,
        Sphere = 1,
        Cylinder = 2,
    }

    /// <summary>
    /// The surface of a part, held as plain numbers rather than a <see cref="Material"/>.
    ///
    /// This exists because the old model code called <c>CloneZUtil.Mat</c> while building, which
    /// allocated a fresh <see cref="Material"/> per call — roughly ten per dropped item, none of them
    /// shared and none of them reclaimed by ordinary garbage collection. At the item counts this world
    /// carries that is untenable. Colour now travels baked into the mesh's vertex colours instead, so
    /// every item in the game can be drawn with one shared material and batched together.
    /// </summary>
    public readonly struct PartMat
    {
        public readonly Color Color;
        public readonly float Smoothness;
        public readonly float Metallic;

        public PartMat(Color color, float smoothness, float metallic)
        {
            Color = color;
            Smoothness = smoothness;
            Metallic = metallic;
        }

        /// <summary>Drop-in replacement for the old <c>CloneZUtil.Mat</c>, but allocation-free.</summary>
        public static PartMat Of(Color color, float smoothness = 0.1f, float metallic = 0f)
            => new PartMat(color, smoothness, metallic);
    }

    /// <summary>One primitive placed in a model, already flattened into model space.</summary>
    public readonly struct Part
    {
        public readonly PartShape Shape;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Vector3 Scale;
        public readonly PartMat Mat;

        public Part(PartShape shape, Vector3 position, Quaternion rotation, Vector3 scale, PartMat mat)
        {
            Shape = shape;
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Mat = mat;
        }

        /// <summary>Rough on-screen weight of the part, used to decide what survives into a lower
        /// level of detail — a fray thread and a skirt panel should not be treated alike.</summary>
        public float Volume => Mathf.Abs(Scale.x * Scale.y * Scale.z);
    }

    /// <summary>
    /// A handle to a part that has already been placed, so the caller can keep adjusting it.
    ///
    /// The model builders very often create a piece and then turn it — <c>var seam = Block(...);
    /// seam.localEulerAngles = ...</c> — which only worked because every piece used to be a real
    /// Transform. A ref points back at the part's slot in its node and writes through to it, so that
    /// idiom keeps working now that parts are values in a list.
    /// </summary>
    public readonly struct PartRef
    {
        private readonly PartNode _node;
        private readonly int _index;

        internal PartRef(PartNode node, int index) { _node = node; _index = index; }

        private Part Get => _node.Parts[_index];
        private void Set(in Part p) { _node.Parts[_index] = p; }

        // Named to match Transform, since that is what these call sites were written against.
        public Vector3 localPosition
        {
            get => Get.Position;
            set { Part p = Get; Set(new Part(p.Shape, value, p.Rotation, p.Scale, p.Mat)); }
        }

        public Vector3 localEulerAngles
        {
            get => Get.Rotation.eulerAngles;
            set { Part p = Get; Set(new Part(p.Shape, p.Position, Quaternion.Euler(value), p.Scale, p.Mat)); }
        }

        public Vector3 localScale
        {
            get => Get.Scale;
            set { Part p = Get; Set(new Part(p.Shape, p.Position, p.Rotation, value, p.Mat)); }
        }

        /// <summary>Accepted so the old call sites still compile; a baked part carries no name.</summary>
        public string name { get => null; set { } }
    }

    /// <summary>
    /// A stand-in for <see cref="Transform"/> while a model is being described.
    ///
    /// The model builders nest parts under named nodes — a sleeve, a hood, a bow — and lean on the
    /// parent's placement to position everything inside it. They used to do that with real
    /// GameObjects, which meant describing a model and instantiating one were the same act, and a
    /// model could not be examined without several hundred scene objects existing first. A node
    /// carries only the handful of members the builders actually touch, deliberately spelled the same
    /// way <see cref="Transform"/> spells them, so the builder bodies read exactly as before while
    /// costing nothing but a little memory.
    /// </summary>
    public sealed class PartNode
    {
        public string name;
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localEulerAngles = Vector3.zero;

        internal PartNode Parent;
        internal readonly List<Part> Parts = new List<Part>();
        internal readonly List<PartNode> Children = new List<PartNode>();

        public PartNode(string name = "Node") { this.name = name; }

        /// <summary>Matches the <see cref="Transform.SetParent(Transform, bool)"/> shape the builders
        /// already call; the bool is accepted and ignored, since local placement is always kept.</summary>
        public void SetParent(PartNode parent, bool worldPositionStays = false)
        {
            if (parent == this) return;
            Parent?.Children.Remove(this);
            Parent = parent;
            parent?.Children.Add(this);
        }

        internal PartRef Add(in Part part)
        {
            Parts.Add(part);
            return new PartRef(this, Parts.Count - 1);
        }

        /// <summary>Walks this node and everything under it, flattening every part into model space.</summary>
        internal void Flatten(Matrix4x4 parentMatrix, List<Part> into)
        {
            Matrix4x4 m = parentMatrix * Matrix4x4.TRS(localPosition, Quaternion.Euler(localEulerAngles), Vector3.one);

            for (int i = 0; i < Parts.Count; i++)
            {
                Part p = Parts[i];
                Matrix4x4 pm = m * Matrix4x4.TRS(p.Position, p.Rotation, Vector3.one);
                into.Add(new Part(p.Shape, pm.GetColumn(3), pm.rotation, p.Scale, p.Mat));
            }

            for (int i = 0; i < Children.Count; i++) Children[i].Flatten(m, into);
        }
    }

    /// <summary>
    /// A finished description of one item's ground model: a flat list of placed primitives and the
    /// height it should hover at. It holds no scene objects and no materials, so it can be built once
    /// per item type, kept for the life of the session, and baked into whatever form is wanted —
    /// a combined mesh for the millions of things lying on the ground, or real GameObjects for the
    /// rare close-up.
    /// </summary>
    public sealed class ItemModelDesc
    {
        public readonly List<Part> Parts;
        public readonly float BaseHeight;
        public readonly Bounds Bounds;

        public ItemModelDesc(List<Part> parts, float baseHeight)
        {
            Parts = parts;
            BaseHeight = baseHeight;

            if (parts.Count == 0) { Bounds = new Bounds(Vector3.zero, Vector3.one * 0.1f); return; }

            var b = new Bounds(parts[0].Position, Vector3.zero);
            for (int i = 0; i < parts.Count; i++) b.Encapsulate(new Bounds(parts[i].Position, parts[i].Scale));
            Bounds = b;
        }

        /// <summary>Collapses a built node tree into a description.</summary>
        public static ItemModelDesc FromRoot(PartNode root, float baseHeight)
        {
            var flat = new List<Part>(256);
            root.Flatten(Matrix4x4.identity, flat);
            return new ItemModelDesc(flat, baseHeight);
        }

        /// <summary>
        /// Builds the model as real GameObjects, the way the game used to draw every dropped item.
        /// This is no longer how the ground is rendered — it costs a few hundred objects per item —
        /// but it is still the simplest way to show a single model up close, so it is kept for
        /// inspection views and for comparing the instanced path against something known good.
        /// </summary>
        public Transform ToGameObject(Transform parent, string name = "Visual")
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);

            for (int i = 0; i < Parts.Count; i++)
            {
                Part p = Parts[i];
                PrimitiveType prim = p.Shape == PartShape.Cube ? PrimitiveType.Cube
                                   : p.Shape == PartShape.Sphere ? PrimitiveType.Sphere
                                   : PrimitiveType.Cylinder;

                var go = GameObject.CreatePrimitive(prim);
                go.name = "Part" + i;

                Collider col = go.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);   // ground items never shove the player or carve navmesh

                go.transform.SetParent(root, false);
                go.transform.localPosition = p.Position;
                go.transform.localRotation = p.Rotation;
                go.transform.localScale = p.Scale;
                go.GetComponent<Renderer>().sharedMaterial =
                    CloneZUtil.Mat(p.Mat.Color, p.Mat.Smoothness, p.Mat.Metallic);
            }

            return root;
        }
    }
}
