using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>A kitchen fitting's local frame, as the furniture builders draw it (front toward +Z, origin on the floor at
    /// its middle), turned by <see cref="Yaw"/> degrees and standing at <see cref="Origin"/> in the world.</summary>
    internal readonly struct KitchenFrame
    {
        public readonly Vector3 Origin;
        public readonly float Yaw;
        public readonly Quaternion Rotation;

        public KitchenFrame(Vector3 origin, float yaw)
        {
            Origin = origin;
            Yaw = yaw;
            Rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>A point of the frame, in the world.</summary>
        public Vector3 P(Vector3 local) => Origin + Rotation * local;

        public Vector3 P(float x, float y, float z) => P(new Vector3(x, y, z));

        /// <summary>A direction of the frame, in the world.</summary>
        public Vector3 D(Vector3 local) => Rotation * local;
    }

    /// <summary>
    /// The kitchen fixtures' shared building blocks: hinged doors and sliding drawers (<see cref="FixturePart"/>s drawn
    /// shut in a <see cref="KitchenFrame"/>), the small meshes that show only now and then — a burner's flame, a lamp's
    /// lens, the water running from the tap — and the water's glassy, see-through look.
    /// </summary>
    internal static class KitchenParts
    {
        private static readonly Color WaterTint = new Color(0.62f, 0.78f, 0.92f, 0.42f);
        private static Material s_Water;

        /// <summary>A door drawn shut by <paramref name="draw"/> in <paramref name="f"/>, turning about the edge through
        /// <paramref name="hinge"/>: opening swings its free edge (the way <paramref name="toFree"/> points from the hinge)
        /// out toward <paramref name="outward"/> by <paramref name="angle"/> degrees. Points and directions are frame-local,
        /// so a side door (toFree along X) turns about the vertical and an oven door (toFree up) drops open.</summary>
        public static FixturePart Door(Transform parent, string name, in KitchenFrame f, Action<RaidMesh> draw,
            Vector3 hinge, Vector3 toFree, Vector3 outward, float angle)
        {
            RaidMesh m = Draw(f, draw);
            Vector3 axis = Vector3.Cross(f.D(toFree), f.D(outward));
            return FixturePart.Hinge(parent, name, m, f.P(hinge), axis, angle);
        }

        /// <summary>A drawer drawn shut by <paramref name="draw"/> in <paramref name="f"/>, pulled out by <paramref name="travel"/>;
        /// <paramref name="pivot"/> is any point on it (frame-local).</summary>
        public static FixturePart Drawer(Transform parent, string name, in KitchenFrame f, Action<RaidMesh> draw, Vector3 pivot, Vector3 travel)
        {
            RaidMesh m = Draw(f, draw);
            return FixturePart.Slide(parent, name, m, f.P(pivot), f.D(travel));
        }

        /// <summary>A small mesh <paramref name="draw"/> draws round its own origin, placed at <paramref name="pivot"/> (frame-local)
        /// on an object of its own, so it can be shown, hidden, moved and scaled (see <see cref="Finish"/>).</summary>
        public static MeshRenderer PieceAt(Transform parent, string name, in KitchenFrame f, Vector3 pivot, Action<RaidMesh> draw)
        {
            Vector3 at = f.P(pivot);
            var m = new RaidMesh();
            m.SetFrame(at, f.Yaw);
            draw(m);
            m.ResetFrame();
            return Finish(m.Emit(name, parent, at, ShadowCastingMode.Off));
        }

        /// <summary>A small mesh <paramref name="draw"/> draws in the fitting's own frame (like its body), on an object of its
        /// own whose pivot is <paramref name="pivot"/> (frame-local) — e.g. a glow laid over a door, riding on the door.</summary>
        public static MeshRenderer PieceInFrame(Transform parent, string name, in KitchenFrame f, Vector3 pivot, Action<RaidMesh> draw)
            => Finish(Draw(f, draw).Emit(name, parent, f.P(pivot), ShadowCastingMode.Off));

        // An emitted piece in world axes, casting no shadow, kept out of the navmesh bake, hidden until it's wanted.
        private static MeshRenderer Finish(GameObject go)
        {
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var r = go.GetComponent<MeshRenderer>();
            r.receiveShadows = false;
            r.enabled = false;
            DecorPalette.Apply();
            return r;
        }

        private static RaidMesh Draw(in KitchenFrame f, Action<RaidMesh> draw)
        {
            var m = new RaidMesh();
            m.SetFrame(f.Origin, f.Yaw);
            draw(m);
            m.ResetFrame();
            return m;
        }

        /// <summary>Water: a pale, glassy blue you can see through (URP Lit, alpha-blended), or a glossy blue where that
        /// shader can't be had.</summary>
        public static Surf Water
        {
            get
            {
                if (s_Water == null) s_Water = MakeWater();
                return s_Water != null ? new Surf(s_Water, Vector2.zero, 0f) : DecorPalette.Gloss(WaterTint);
            }
        }

        // The same render state URP's material inspector writes for Surface = Transparent, Blend = Alpha.
        private static Material MakeWater()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            var m = new Material(shader) { name = "KitchenWater", hideFlags = HideFlags.DontUnloadUnusedAsset };
            Set(m, "_Surface", 1f);
            Set(m, "_Blend", 0f);
            Set(m, "_SrcBlend", (float)BlendMode.SrcAlpha);
            Set(m, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            Set(m, "_SrcBlendAlpha", (float)BlendMode.One);
            Set(m, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            Set(m, "_ZWrite", 0f);
            Set(m, "_Smoothness", 0.95f);
            Set(m, "_Metallic", 0f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", WaterTint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", WaterTint);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        private static void Set(Material m, string property, float value)
        {
            if (m.HasProperty(property)) m.SetFloat(property, value);
        }

        /// <summary>Tells the survivor <paramref name="message"/> if they're within <paramref name="range"/> (flat) of
        /// <paramref name="p"/> — for things that happen by themselves (a tap running dry, a sink brimming over).</summary>
        public static void NotifyNear(Vector3 p, float range, string message)
        {
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null) return;
            Vector3 d = pi.transform.position - p;
            d.y = 0f;
            if (d.sqrMagnitude <= range * range) pi.Notify(message);
        }

        /// <summary>A stable seed from a fitting's place (to the centimetre) and a salt, for its contents and its clutter.</summary>
        public static int SeedAt(Vector3 p, int salt)
            => unchecked(Mathf.RoundToInt(p.x * 100f) * 73856093 ^ Mathf.RoundToInt(p.z * 100f) * 19349663 ^ (salt + 1) * 83492791) & 0x7FFFFFFF;
    }
}
