using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Builds a gun's attachments onto the model in a character's hands (<see cref="HumanoidRig.SetHeldMods"/>): a red dot or
    /// a scope on top, a suppressor, compensator or barrel extension at the muzzle, a light or laser on the rail (under a
    /// handgun's frame, on a long gun's right side), a foregrip, a stock's cheek rest and pad, an extended magazine. Each is
    /// placed on the gun as it's drawn, from its measured outline (<see cref="GunShape"/>), so every version of a generated gun
    /// model gets them where its own top, muzzle and underside are.
    ///
    /// An attachment is described as primitive parts in the gun's space (<see cref="GunShape"/>: X along the bore to the muzzle,
    /// Z the top of the gun, Y its right side) and baked into one mesh with a submesh per surface, kept per attachment and gun model: a crowd
    /// with the same kit shares them. A light's lens and a laser's emitter get a submesh of their own, swapped to a glowing
    /// material while the device is on (<see cref="Fitted.SetLightLit"/>, <see cref="Fitted.SetLaserLit"/>).
    /// </summary>
    public static class WeaponAttachments
    {
        private enum Surface : byte { Lit, Glow, Lens, Emitter }

        private readonly struct Piece
        {
            public readonly Part Part;
            public readonly Surface Surface;
            public Piece(Part part, Surface surface) { Part = part; Surface = surface; }
        }

        // One attachment for one gun model, baked.
        private sealed class Model
        {
            public Mesh Mesh;
            public Material[] Materials;
            public int LensSub = -1, EmitterSub = -1;
            public bool HasLens, HasEmitter, HasSupport;
            public Vector3 Lens, Emitter, Support;   // item space; the beam leaves along +X
            public float MuzzleGain;                 // how far a barrel extension moves the muzzle forward
        }

        /// <summary>What's fitted to one held gun: the attachments' renderers, where a light's beam and a laser leave the gun,
        /// and where the off hand holds a foregrip.</summary>
        public sealed class Fitted
        {
            private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>(4);
            private MeshRenderer _light, _laser;
            private int _lightSub = -1, _laserSub = -1;
            private bool _lightLit, _laserLit;

            /// <summary>The object the attachments hang under (a child of the held item).</summary>
            public Transform Root { get; internal set; }
            /// <summary>A weapon light's lens (null without one): forward is the beam, up the top of the gun.</summary>
            public Transform LightLens { get; internal set; }
            /// <summary>A laser's emitter (null without one): forward is the beam, up the top of the gun.</summary>
            public Transform LaserEmitter { get; internal set; }
            /// <summary>Where the off hand takes a foregrip, in the held item's space (null without one).</summary>
            public Vector3? Support { get; internal set; }
            /// <summary>The attachments' renderers.</summary>
            public IReadOnlyList<MeshRenderer> Renderers => _renderers;

            internal void Add(MeshRenderer r, int lensSub, int emitterSub)
            {
                _renderers.Add(r);
                if (lensSub >= 0) { _light = r; _lightSub = lensSub; }
                if (emitterSub >= 0) { _laser = r; _laserSub = emitterSub; }
            }

            /// <summary>Shows or hides the attachments along with the gun (the camera hides the body in first person).</summary>
            public void SetVisible(bool visible)
            {
                for (int i = 0; i < _renderers.Count; i++) if (_renderers[i] != null) _renderers[i].enabled = visible;
            }

            /// <summary>Lights the weapon light's lens (or puts it out).</summary>
            public void SetLightLit(bool lit)
            {
                if (lit == _lightLit || _light == null) return;
                _lightLit = lit;
                Swap(_light, _lightSub, lit ? Glow(LensLitColor) : LensGlass);
            }

            /// <summary>Lights the laser's emitter (or puts it out).</summary>
            public void SetLaserLit(bool lit)
            {
                if (lit == _laserLit || _laser == null) return;
                _laserLit = lit;
                Swap(_laser, _laserSub, lit ? Glow(LaserColor) : EmitterGlass);
            }

            private static void Swap(MeshRenderer r, int sub, Material m)
            {
                Material[] mats = r.sharedMaterials;
                if (sub < 0 || sub >= mats.Length) return;
                mats[sub] = m;
                r.sharedMaterials = mats;
            }
        }

        /// <summary>The colour of a laser's dot and emitter.</summary>
        public static readonly Color LaserColor = new Color(1f, 0.1f, 0.06f);
        private static readonly Color LensLitColor = new Color(1f, 0.97f, 0.9f);
        private static readonly Color ReticleColor = new Color(1f, 0.16f, 0.1f);

        private static readonly PartMat Anodised = PartMat.Of(new Color(0.075f, 0.075f, 0.08f), 0.45f, 0.6f);
        private static readonly PartMat Polymer = PartMat.Of(new Color(0.095f, 0.095f, 0.1f), 0.25f);
        private static readonly PartMat Steel = PartMat.Of(new Color(0.3f, 0.31f, 0.33f), 0.5f, 0.85f);
        private static readonly PartMat DarkSteel = PartMat.Of(new Color(0.15f, 0.15f, 0.16f), 0.45f, 0.8f);
        private static readonly PartMat Rubber = PartMat.Of(new Color(0.05f, 0.05f, 0.05f), 0.12f);
        private static readonly PartMat Bore = PartMat.Of(new Color(0.02f, 0.02f, 0.02f), 0.1f);
        private static readonly PartMat Optic = PartMat.Of(new Color(0.11f, 0.17f, 0.21f), 0.95f, 0.1f);

        private static readonly Dictionary<string, Model> s_Models = new Dictionary<string, Model>(32);
        private static readonly Dictionary<int, Material> s_Glow = new Dictionary<int, Material>(4);
        private static Material s_LensGlass, s_EmitterGlass;

        private static Material LensGlass => s_LensGlass != null ? s_LensGlass : s_LensGlass = CloneZUtil.Mat(new Color(0.72f, 0.75f, 0.77f), 0.95f);
        private static Material EmitterGlass => s_EmitterGlass != null ? s_EmitterGlass : s_EmitterGlass = CloneZUtil.Mat(new Color(0.32f, 0.04f, 0.04f), 0.9f);

        // Meshes and materials made in a play session are destroyed when it ends; statics outlive it in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetLastPlaySession()
        {
            s_Models.Clear();
            s_Glow.Clear();
            s_LensGlass = s_EmitterGlass = null;
        }

        /// <summary>An unlit material of <paramref name="color"/>, shared.</summary>
        internal static Material Glow(Color color)
        {
            int key = (Mathf.RoundToInt(color.r * 255f) << 16) | (Mathf.RoundToInt(color.g * 255f) << 8) | Mathf.RoundToInt(color.b * 255f);
            if (s_Glow.TryGetValue(key, out Material m) && m != null) return m;
            m = CloneZUtil.UnlitMat(color);
            m.name = "AttachmentGlow";
            m.enableInstancing = true;
            s_Glow[key] = m;
            return m;
        }

        /// <summary>
        /// Builds <paramref name="mods"/> onto <paramref name="held"/>, the model of <paramref name="gun"/> drawn with
        /// <paramref name="gunMesh"/> (mods the gun has no slot for are skipped). Null when none of them shows.
        /// </summary>
        public static Fitted Fit(Transform held, ItemDef gun, Mesh gunMesh, IReadOnlyList<string> mods, bool visible)
        {
            if (held == null || gun == null || gunMesh == null || mods == null || mods.Count == 0) return null;
            GunShape shape = GunShape.Of(gun, gunMesh);
            if (shape == null) return null;

            // A barrel extension moves the muzzle (and whatever goes on it) forward, so it's built first.
            string barrel = null;
            for (int i = 0; i < mods.Count; i++)
            {
                ModStats m = Ballistics.Mod(mods[i]);
                if (m != null && m.Slot == ModSlot.Barrel && Ballistics.ModFits(gun, m)) barrel = m.Id;
            }
            float gain = barrel != null ? ModelFor(barrel, null, gun, gunMesh, shape, 0f).MuzzleGain : 0f;

            Fitted fitted = null;
            int slots = 0;   // the slots already dressed: a second mod rolled into one (an NPC's two sights) shows only the first
            for (int i = 0; i < mods.Count; i++)
            {
                ModStats m = Ballistics.Mod(mods[i]);
                if (m == null || !Ballistics.ModFits(gun, m) || (slots & (1 << (int)m.Slot)) != 0) continue;
                slots |= 1 << (int)m.Slot;
                Model model = ModelFor(m.Id, m.Slot == ModSlot.Muzzle ? barrel : null, gun, gunMesh, shape, m.Slot == ModSlot.Muzzle ? gain : 0f);
                if (model == null || model.Mesh == null) continue;
                if (fitted == null)
                {
                    var rootGo = new GameObject("Attachments");
                    rootGo.layer = held.gameObject.layer;
                    rootGo.transform.SetParent(held, false);
                    // The attachments are built in gun space: turned back into the model's own for a model made end for end.
                    rootGo.transform.localRotation = Quaternion.Inverse(shape.ToGun);
                    fitted = new Fitted { Root = rootGo.transform };
                }

                var go = new GameObject("Attachment_" + m.Id);
                go.layer = held.gameObject.layer;
                go.transform.SetParent(fitted.Root, false);
                go.AddComponent<MeshFilter>().sharedMesh = model.Mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterials = (Material[])model.Materials.Clone();
                mr.enabled = visible;
                fitted.Add(mr, model.LensSub, model.EmitterSub);

                if (model.HasLens) fitted.LightLens = Marker(go.transform, "LightLens", model.Lens);
                if (model.HasEmitter) fitted.LaserEmitter = Marker(go.transform, "LaserEmitter", model.Emitter);
                if (model.HasSupport) fitted.Support = Quaternion.Inverse(shape.ToGun) * model.Support;
            }
            return fitted;
        }

        // An empty child at an item-space point, its forward along the bore and its up the top of the gun.
        private static Transform Marker(Transform parent, string name, Vector3 at)
        {
            var t = new GameObject(name).transform;
            t.gameObject.layer = parent.gameObject.layer;
            t.SetParent(parent, false);
            t.localPosition = at;
            t.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.forward);
            return t;
        }

        private static Model ModelFor(string id, string barrel, ItemDef gun, Mesh gunMesh, GunShape s, float gain)
        {
            string key = id + "|" + gunMesh.name + "|" + gun.Id + "|" + barrel;
            if (s_Models.TryGetValue(key, out Model cached) && (cached.Mesh != null || cached.Materials == null)) return cached;

            var parts = new List<Piece>(24);
            var model = new Model();
            Describe(id, gun, s, gain, parts, model);
            if (parts.Count > 0) Bake("Attachment_" + id + "_" + gun.Id, parts, model);
            s_Models[key] = model;
            return model;
        }

        #region The attachments
        private static void Describe(string id, ItemDef gun, GunShape s, float gain, List<Piece> p, Model model)
        {
            Vector3 muzzle = s.Muzzle + Vector3.right * gain;
            switch (id)
            {
                case "mod_suppressor9": Can(p, muzzle, s.Pistol ? 0.14f : 0.16f, 0.034f); break;
                case "mod_suppressor545": Can(p, muzzle, 0.19f, 0.038f); break;
                case "mod_compensator": Compensator(p, muzzle, s.Pistol ? 0.022f : 0.027f); break;
                case "mod_barrel_long": model.MuzzleGain = LongBarrel(p, s); break;
                case "mod_barrel_short": model.MuzzleGain = BarrelNut(p, s); break;
                case "mod_reddot": if (s.Pistol) PistolDot(p, s); else RifleDot(p, s); break;
                case "mod_scope4x": Scope(p, s); break;
                case "mod_laser": model.Emitter = Laser(p, s); model.HasEmitter = true; break;
                case "mod_weaponlight": model.Lens = WeaponLight(p, s); model.HasLens = true; break;
                case "mod_foregrip": model.Support = Foregrip(p, s); model.HasSupport = true; break;
                case "mod_stock_tactical": TacticalStock(p, s); break;
                case "mod_stock_folding": FoldingStock(p, s); break;
                case "mod_mag_extended": ExtendedMagazine(p, s, Ballistics.CaliberOfGun(gun) == "545"); break;
                default: Generic(p, s, Ballistics.Mod(id)); break;
            }
        }

        // A suppressor: a collar threaded onto the muzzle and the can, with a couple of grooves and a front cap.
        private static void Can(List<Piece> p, Vector3 m, float length, float diameter)
        {
            RodX(p, m + new Vector3(0.002f, 0f, 0f), 0.024f, 0.022f, DarkSteel);   // collar, sunk into the muzzle
            float start = m.x + 0.012f;
            RodX(p, new Vector3(start + length * 0.5f, m.y, m.z), length, diameter, Anodised);
            for (int i = 0; i < 2; i++)
                RodX(p, new Vector3(start + 0.014f + i * 0.01f, m.y, m.z), 0.003f, diameter + 0.0015f, DarkSteel);
            RodX(p, new Vector3(start + length - 0.004f, m.y, m.z), 0.01f, diameter + 0.001f, DarkSteel);
            RodX(p, new Vector3(start + length + 0.0012f, m.y, m.z), 0.0025f, 0.01f, Bore);
        }

        // A compensator: a short block on the muzzle with ports cut across its top.
        private static void Compensator(List<Piece> p, Vector3 m, float diameter)
        {
            const float length = 0.055f;
            RodX(p, new Vector3(m.x + length * 0.5f - 0.006f, m.y, m.z), length, diameter, DarkSteel);
            for (int i = 0; i < 3; i++)
                Box(p, new Vector3(m.x + 0.012f + i * 0.012f, m.y, m.z + diameter * 0.5f - 0.0015f), new Vector3(0.006f, diameter * 0.7f, 0.004f), Bore);
            RodX(p, new Vector3(m.x + length - 0.005f, m.y, m.z), 0.002f, diameter * 0.45f, Bore);
        }

        // A long barrel: a steel extension carrying the bore forward, with a front sight post near its end.
        private static float LongBarrel(List<Piece> p, GunShape s)
        {
            Vector3 m = s.Muzzle;
            float length = s.Pistol ? 0.05f : 0.12f;
            float d = Mathf.Clamp(s.BoreRadius * 1.8f, 0.016f, 0.026f);
            RodX(p, new Vector3(m.x + length * 0.5f - 0.01f, m.y, m.z), length + 0.02f, d, Steel);
            RodX(p, new Vector3(m.x + length - 0.006f, m.y, m.z), 0.012f, d + 0.004f, DarkSteel);
            Box(p, new Vector3(m.x + length - 0.02f, m.y, m.z + d * 0.5f + 0.005f), new Vector3(0.006f, 0.004f, 0.012f), DarkSteel);
            RodX(p, new Vector3(m.x + length + 0.0012f, m.y, m.z), 0.0025f, d * 0.55f, Bore);
            return length;
        }

        // A short barrel can't take metal off the model; a stubby thread protector at the muzzle says it's been cut down.
        private static float BarrelNut(List<Piece> p, GunShape s)
        {
            Vector3 m = s.Muzzle;
            float d = Mathf.Clamp(s.BoreRadius * 2.1f, 0.016f, 0.03f);
            RodX(p, new Vector3(m.x + 0.006f, m.y, m.z), 0.024f, d, DarkSteel);
            for (int i = 0; i < 3; i++) RodX(p, new Vector3(m.x + 0.002f + i * 0.006f, m.y, m.z), 0.002f, d + 0.002f, Steel);
            RodX(p, new Vector3(m.x + 0.0185f, m.y, m.z), 0.002f, d * 0.5f, Bore);
            return 0.018f;
        }

        // A handgun's reflex sight on the back of the slide: a low housing, a hooded window and a red dot on its glass.
        private static void PistolDot(List<Piece> p, GunShape s)
        {
            float x = s.X(0.27f);
            float z = s.TopAt(x - 0.022f, x + 0.022f, out float y);
            Box(p, new Vector3(x, y, z + 0.003f), new Vector3(0.044f, 0.026f, 0.01f), Anodised);
            Box(p, new Vector3(x - 0.016f, y, z + 0.013f), new Vector3(0.012f, 0.026f, 0.014f), Anodised);
            for (int side = -1; side <= 1; side += 2)
                Box(p, new Vector3(x + 0.006f, y + side * 0.0115f, z + 0.015f), new Vector3(0.026f, 0.003f, 0.018f), Anodised);
            Box(p, new Vector3(x + 0.006f, y, z + 0.0255f), new Vector3(0.026f, 0.026f, 0.004f), Anodised);
            Box(p, new Vector3(x + 0.012f, y, z + 0.0155f), new Vector3(0.0015f, 0.02f, 0.015f), Optic);
            Ball(p, new Vector3(x + 0.0105f, y, z + 0.016f), 0.0022f, Surface.Glow);
        }

        // A long gun's red dot: a tube sight on a riser over the receiver, glass at both ends and the dot at the back.
        private static void RifleDot(List<Piece> p, GunShape s)
        {
            float x = s.X(0.42f);
            float z = s.TopAt(x - 0.03f, x + 0.03f, out float y);
            Box(p, new Vector3(x, y, z + 0.003f), new Vector3(0.05f, 0.03f, 0.01f), Anodised);
            Box(p, new Vector3(x, y, z + 0.015f), new Vector3(0.032f, 0.02f, 0.016f), Anodised);
            float axis = z + 0.04f;
            RodX(p, new Vector3(x, y, axis), 0.075f, 0.034f, Anodised);
            RodX(p, new Vector3(x + 0.04f, y, axis), 0.008f, 0.038f, Anodised);
            RodX(p, new Vector3(x + 0.0445f, y, axis), 0.0015f, 0.029f, Optic);
            RodX(p, new Vector3(x - 0.0385f, y, axis), 0.0015f, 0.029f, Optic);
            Ball(p, new Vector3(x - 0.036f, y, axis), 0.0024f, Surface.Glow);
            Rod(p, new Vector3(x, y, axis + 0.021f), Vector3.forward, 0.01f, 0.016f, Anodised);
            Rod(p, new Vector3(x, y + 0.021f, axis), Vector3.up, 0.01f, 0.016f, Anodised);
        }

        // A 4x scope in two rings over the receiver (shorter on a handgun), its tube high enough for the bell to clear the gun.
        private static void Scope(List<Piece> p, GunShape s)
        {
            bool pistol = s.Pistol;
            float x = s.X(pistol ? 0.45f : 0.4f);
            float tube = pistol ? 0.16f : 0.26f, ring = pistol ? 0.04f : 0.06f;
            float bell = pistol ? 0.032f : 0.04f, eye = pistol ? 0.03f : 0.036f, d = 0.026f;
            float rear = s.TopAt(x - ring - 0.008f, x - ring + 0.008f, out float yr);
            float front = s.TopAt(x + ring - 0.008f, x + ring + 0.008f, out float yf);
            float y = (yr + yf) * 0.5f;
            float underBell = s.TopAt(x + tube * 0.5f - 0.06f, x + tube * 0.5f, out _);
            float underEye = s.TopAt(x - tube * 0.5f, x - tube * 0.5f + 0.05f, out _);
            float axis = Mathf.Max(Mathf.Max(rear, front) + 0.012f + d * 0.5f, Mathf.Max(underBell + bell * 0.5f, underEye + eye * 0.5f) + 0.004f);

            RodX(p, new Vector3(x, y, axis), tube - 0.08f, d, Anodised);
            RodX(p, new Vector3(x + tube * 0.5f - 0.03f, y, axis), 0.06f, bell, Anodised);
            RodX(p, new Vector3(x + tube * 0.5f - 0.068f, y, axis), 0.016f, (bell + d) * 0.5f, Anodised);
            RodX(p, new Vector3(x - tube * 0.5f + 0.025f, y, axis), 0.05f, eye, Anodised);
            RodX(p, new Vector3(x + tube * 0.5f + 0.0008f, y, axis), 0.0015f, bell - 0.006f, Optic);
            RodX(p, new Vector3(x - tube * 0.5f - 0.0008f, y, axis), 0.0015f, eye - 0.006f, Optic);
            Box(p, new Vector3(x, y, axis), new Vector3(0.04f, 0.032f, 0.03f), Anodised);
            Rod(p, new Vector3(x, y, axis + 0.021f), Vector3.forward, 0.012f, 0.018f, Anodised);
            Rod(p, new Vector3(x, y + 0.021f, axis), Vector3.up, 0.012f, 0.018f, Anodised);
            foreach ((float rx, float top) in new[] { (x - ring, rear), (x + ring, front) })
            {
                RodX(p, new Vector3(rx, y, axis), 0.014f, d + 0.007f, DarkSteel);
                float low = top - 0.002f, high = axis - d * 0.4f;
                Box(p, new Vector3(rx, y, (low + high) * 0.5f), new Vector3(0.016f, 0.024f, Mathf.Max(0.004f, high - low)), DarkSteel);
            }
        }

        // A laser module: under a handgun's frame, or clamped to a long gun's right side. Returns where the beam leaves.
        private static Vector3 Laser(List<Piece> p, GunShape s)
        {
            if (s.Pistol)
            {
                float front = s.Muzzle.x - 0.014f, x = front - 0.02f;
                float z = s.BottomAt(front - 0.04f, front, out float y);
                Box(p, new Vector3(x, y, z - 0.0005f), new Vector3(0.03f, 0.02f, 0.006f), DarkSteel);
                Box(p, new Vector3(x, y, z - 0.013f), new Vector3(0.04f, 0.026f, 0.022f), Polymer);
                var emitter = new Vector3(front + 0.0012f, y, z - 0.017f);
                Disc(p, emitter, 0.0025f, 0.008f, Surface.Emitter);
                return emitter + Vector3.right * 0.0015f;
            }
            else
            {
                float x = s.X(0.8f), side = s.RightAt(x), z = s.Muzzle.z - s.BoreRadius * 0.2f;
                Box(p, new Vector3(x, side + 0.002f, z), new Vector3(0.03f, 0.01f, 0.014f), DarkSteel);
                Box(p, new Vector3(x, side + 0.017f, z), new Vector3(0.045f, 0.022f, 0.026f), Polymer);
                var emitter = new Vector3(x + 0.0235f, side + 0.017f, z - 0.005f);
                Disc(p, emitter, 0.0025f, 0.008f, Surface.Emitter);
                return emitter + Vector3.right * 0.0015f;
            }
        }

        // A weapon light: a square-bodied light under a handgun's frame, a tube light on a long gun's right side. Returns the
        // middle of its lens.
        private static Vector3 WeaponLight(List<Piece> p, GunShape s)
        {
            if (s.Pistol)
            {
                float front = s.Muzzle.x - 0.006f;
                float z = s.BottomAt(front - 0.055f, front, out float y);
                float x = front - 0.032f;
                Box(p, new Vector3(x + 0.004f, y, z - 0.0005f), new Vector3(0.04f, 0.02f, 0.006f), DarkSteel);
                Box(p, new Vector3(x, y, z - 0.015f), new Vector3(0.05f, 0.028f, 0.026f), Polymer);
                RodX(p, new Vector3(front - 0.006f, y, z - 0.016f), 0.012f, 0.031f, Anodised);
                for (int side = -1; side <= 1; side += 2)
                    Box(p, new Vector3(x - 0.022f, y + side * 0.0155f, z - 0.02f), new Vector3(0.008f, 0.004f, 0.01f), Rubber);
                var lens = new Vector3(front + 0.0008f, y, z - 0.016f);
                Disc(p, lens, 0.0015f, 0.026f, Surface.Lens);
                return lens + Vector3.right * 0.001f;
            }
            else
            {
                float front = Mathf.Min(s.X(0.86f), s.Muzzle.x - 0.015f);
                float x = front - 0.06f, side = s.RightAt(x), z = s.Muzzle.z - s.BoreRadius * 0.2f;
                float y = side + 0.016f;
                Box(p, new Vector3(x, side + 0.002f, z), new Vector3(0.035f, 0.012f, 0.016f), DarkSteel);
                RodX(p, new Vector3(x - 0.005f, y, z), 0.09f, 0.025f, Anodised);
                RodX(p, new Vector3(front - 0.016f, y, z), 0.032f, 0.032f, Anodised);
                RodX(p, new Vector3(x - 0.055f, y, z), 0.012f, 0.022f, Rubber);
                var lens = new Vector3(front + 0.0008f, y, z);
                Disc(p, lens, 0.0015f, 0.028f, Surface.Lens);
                return lens + Vector3.right * 0.001f;
            }
        }

        // A vertical foregrip under the handguard. Returns where the off hand closes on it.
        private static Vector3 Foregrip(List<Piece> p, GunShape s)
        {
            float x = s.X(0.62f);
            float z = s.BottomAt(x - 0.02f, x + 0.02f, out float y);
            Box(p, new Vector3(x, y, z - 0.002f), new Vector3(0.04f, 0.024f, 0.012f), DarkSteel);
            Rod(p, new Vector3(x, y, z - 0.05f), Vector3.forward, 0.08f, 0.03f, Polymer);
            for (int i = 0; i < 3; i++) Rod(p, new Vector3(x, y, z - 0.028f - i * 0.018f), Vector3.forward, 0.004f, 0.032f, Polymer);
            Rod(p, new Vector3(x, y, z - 0.092f), Vector3.forward, 0.006f, 0.034f, Rubber);
            return new Vector3(x, y, z - 0.05f);
        }

        // A tactical stock: a polymer cheek rest along the comb (tilted with it) and a thick recoil pad on the butt.
        private static void TacticalStock(List<Piece> p, GunShape s)
        {
            float x0 = s.Box.min.x;
            float a = s.X(0.03f), b = s.X(0.17f);
            float ta = s.TopAt(a, a + 0.03f, out float yRear), tb = s.TopAt(b - 0.03f, b, out float yFront);
            float y = (yRear + yFront) * 0.5f;
            Quaternion tilt = Quaternion.Euler(0f, -Mathf.Atan2(tb - ta, b - a) * Mathf.Rad2Deg, 0f);
            Vector3 mid = new Vector3((a + b) * 0.5f, y, (ta + tb) * 0.5f);
            float run = Mathf.Sqrt((b - a) * (b - a) + (tb - ta) * (tb - ta));
            p.Add(new Piece(new Part(PartShape.Cube, mid + tilt * new Vector3(0f, 0f, 0.006f), tilt, new Vector3(run, 0.032f, 0.016f), Polymer), Surface.Lit));
            p.Add(new Piece(new Part(PartShape.Cube, mid + tilt * new Vector3(0f, 0f, 0.0145f), tilt, new Vector3(run - 0.01f, 0.026f, 0.003f), Rubber), Surface.Lit));
            float bottom = s.BottomAt(x0, x0 + 0.03f, out _);
            float buttTop = s.TopAt(x0, x0 + 0.03f, out float yb);
            float h = Mathf.Max(0.04f, buttTop - bottom + 0.006f);
            Box(p, new Vector3(x0 - 0.008f, yb, (buttTop + bottom) * 0.5f), new Vector3(0.022f, 0.036f, h), Rubber);
        }

        // A folding stock, shown folded: a steel hinge at the wrist and a skeleton frame laid along the right of the stock.
        private static void FoldingStock(List<Piece> p, GunShape s)
        {
            float x0 = s.Box.min.x;
            float hinge = s.X(0.3f), rear = x0 + 0.02f;
            float top = s.TopAt(rear, hinge, out _);
            float bottom = s.BottomAt(rear, hinge, out _);
            float side = Mathf.Max(s.RightAt(hinge), s.RightAt((rear + hinge) * 0.5f), s.RightAt(rear)) + 0.004f;
            float hi = Mathf.Lerp(bottom, top, 0.78f), lo = Mathf.Lerp(bottom, top, 0.28f);
            Rod(p, new Vector3(hinge, side - 0.004f, (hi + lo) * 0.5f), Vector3.up, 0.014f, 0.022f, DarkSteel);
            foreach (float z in new[] { hi, lo })
                RodX(p, new Vector3((hinge + rear) * 0.5f, side, z), hinge - rear, 0.007f, Steel);
            Box(p, new Vector3(rear, side, (hi + lo) * 0.5f), new Vector3(0.008f, 0.008f, hi - lo + 0.014f), Steel);
        }

        // An extended magazine: a longer base under a handgun's grip, a longer magazine under a long gun's receiver (curved on
        // an assault rifle, straight on anything else).
        private static void ExtendedMagazine(List<Piece> p, GunShape s, bool curved)
        {
            if (s.Pistol)
            {
                Vector3 g = s.GripBase();
                Box(p, new Vector3(g.x, g.y, g.z - 0.012f), new Vector3(0.034f, 0.024f, 0.032f), Polymer);
                Box(p, new Vector3(g.x + 0.002f, g.y, g.z - 0.031f), new Vector3(0.042f, 0.028f, 0.007f), Polymer);
                return;
            }
            float x = s.X(0.46f);
            float z = s.BottomAt(x - 0.03f, x + 0.03f, out float y);
            if (curved)
            {
                Box(p, new Vector3(x, y, z - 0.045f), new Vector3(0.058f, 0.022f, 0.11f), DarkSteel);
                var lower = new Part(PartShape.Cube, new Vector3(x + 0.022f, y, z - 0.14f), Quaternion.Euler(0f, -22f, 0f), new Vector3(0.054f, 0.022f, 0.1f), DarkSteel);
                p.Add(new Piece(lower, Surface.Lit));
                p.Add(new Piece(new Part(PartShape.Cube, new Vector3(x + 0.043f, y, z - 0.19f), Quaternion.Euler(0f, -22f, 0f), new Vector3(0.06f, 0.026f, 0.008f), Polymer), Surface.Lit));
            }
            else
            {
                Box(p, new Vector3(x, y, z - 0.075f), new Vector3(0.034f, 0.022f, 0.16f), DarkSteel);
                Box(p, new Vector3(x, y, z - 0.157f), new Vector3(0.04f, 0.026f, 0.008f), Polymer);
            }
        }

        // An attachment with no model of its own yet: a small block where its slot is, so fitting it still shows.
        private static void Generic(List<Piece> p, GunShape s, ModStats m)
        {
            if (m == null) return;
            switch (m.Slot)
            {
                case ModSlot.Muzzle:
                case ModSlot.Barrel:
                    RodX(p, s.Muzzle + new Vector3(0.02f, 0f, 0f), 0.05f, 0.024f, DarkSteel);
                    break;
                case ModSlot.Sight:
                {
                    float x = s.X(s.Pistol ? 0.27f : 0.42f);
                    float z = s.TopAt(x - 0.02f, x + 0.02f, out float y);
                    Box(p, new Vector3(x, y, z + 0.01f), new Vector3(0.04f, 0.024f, 0.024f), Anodised);
                    break;
                }
                default:
                {
                    float x = s.X(0.6f);
                    float z = s.BottomAt(x - 0.02f, x + 0.02f, out float y);
                    Box(p, new Vector3(x, y, z - 0.01f), new Vector3(0.04f, 0.024f, 0.024f), Polymer);
                    break;
                }
            }
        }
        #endregion

        #region Parts
        private static void Box(List<Piece> p, Vector3 at, Vector3 size, PartMat mat) =>
            p.Add(new Piece(new Part(PartShape.Cube, at, Quaternion.identity, size, mat), Surface.Lit));

        private static void Ball(List<Piece> p, Vector3 at, float diameter, Surface surface) =>
            p.Add(new Piece(new Part(PartShape.Sphere, at, Quaternion.identity, Vector3.one * diameter, PartMat.Of(ReticleColor)), surface));

        // A cylinder of `length` along the bore (item X), centred at `at`.
        private static void RodX(List<Piece> p, Vector3 at, float length, float diameter, PartMat mat) =>
            p.Add(new Piece(new Part(PartShape.Cylinder, at, Quaternion.Euler(0f, 0f, 90f), new Vector3(diameter, length * 0.5f, diameter), mat), Surface.Lit));

        // A lens or an emitter: a thin disc across the bore, drawn with the device's own swappable surface.
        private static void Disc(List<Piece> p, Vector3 at, float length, float diameter, Surface surface) =>
            p.Add(new Piece(new Part(PartShape.Cylinder, at, Quaternion.Euler(0f, 0f, 90f), new Vector3(diameter, length * 0.5f, diameter), Optic), surface));

        // A cylinder of `length` along `axis` (item Y or Z), centred at `at`.
        private static void Rod(List<Piece> p, Vector3 at, Vector3 axis, float length, float diameter, PartMat mat)
        {
            Quaternion q = axis == Vector3.up ? Quaternion.identity : Quaternion.FromToRotation(Vector3.up, axis);
            p.Add(new Piece(new Part(PartShape.Cylinder, at, q, new Vector3(diameter, length * 0.5f, diameter), mat), Surface.Lit));
        }
        #endregion

        #region Baking
        // One mesh with a submesh per surface: the lit ones share CloneZUtil's materials, the glows ours.
        private static void Bake(string name, List<Piece> parts, Model model)
        {
            var groups = new List<(int key, Surface surface, PartMat mat, List<Part> parts)>(8);
            for (int i = 0; i < parts.Count; i++)
            {
                Piece piece = parts[i];
                int key = piece.Surface == Surface.Lit ? SurfaceKey(piece.Part.Mat) : -1 - (int)piece.Surface;
                int g = groups.FindIndex(e => e.key == key);
                if (g < 0)
                {
                    groups.Add((key, piece.Surface, piece.Part.Mat, new List<Part>(8)));
                    g = groups.Count - 1;
                }
                groups[g].parts.Add(piece.Part);
            }

            var verts = new List<Vector3>(512);
            var norms = new List<Vector3>(512);
            var subs = new List<int[]>(groups.Count);
            var mats = new Material[groups.Count];
            for (int g = 0; g < groups.Count; g++)
            {
                var tris = new List<int>(768);
                foreach (Part part in groups[g].parts)
                {
                    Matrix4x4 m = Matrix4x4.TRS(part.Position, part.Rotation, part.Scale);
                    Matrix4x4 nm = Matrix4x4.Transpose(m.inverse);
                    PrimitiveMesh src = PrimitiveMesh.For(part.Shape, ItemMeshBaker.Lod.Near);
                    int first = verts.Count;
                    for (int v = 0; v < src.Verts.Length; v++)
                    {
                        verts.Add(m.MultiplyPoint3x4(src.Verts[v]));
                        norms.Add(nm.MultiplyVector(src.Norms[v]).normalized);
                    }
                    for (int t = 0; t < src.Tris.Length; t++) tris.Add(first + src.Tris[t]);
                }
                subs.Add(tris.ToArray());
                PartMat pm = groups[g].mat;
                switch (groups[g].surface)
                {
                    case Surface.Glow: mats[g] = Glow(ReticleColor); break;
                    case Surface.Lens: mats[g] = LensGlass; model.LensSub = g; break;
                    case Surface.Emitter: mats[g] = EmitterGlass; model.EmitterSub = g; break;
                    default: mats[g] = CloneZUtil.Mat(pm.Color, pm.Smoothness, pm.Metallic); break;
                }
            }

            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s, calculateBounds: false);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(markNoLongerReadable: true);
            model.Mesh = mesh;
            model.Materials = mats;
        }

        private static int SurfaceKey(in PartMat m)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(m.Color.r * 63f), 0, 63);
            int g = Mathf.Clamp(Mathf.RoundToInt(m.Color.g * 63f), 0, 63);
            int b = Mathf.Clamp(Mathf.RoundToInt(m.Color.b * 63f), 0, 63);
            int s = Mathf.Clamp(Mathf.RoundToInt(m.Smoothness * 15f), 0, 15);
            int t = Mathf.Clamp(Mathf.RoundToInt(m.Metallic * 7f), 0, 7);
            return (r << 19) | (g << 13) | (b << 7) | (s << 3) | t;
        }
        #endregion
    }
}
