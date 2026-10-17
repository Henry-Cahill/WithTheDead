using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Generates the skinned humanoid mesh for one <see cref="HumanoidShape"/>.
    ///
    /// Every surface is a loft: a stack of superellipse rings skinned to one bone (or blended across
    /// two, down the torso), closed off with rounded or flat ends. Limbs are tapered capsules with
    /// muscle bulges, the torso follows a measured profile from crotch to neck, and clothing is the
    /// same surfaces laid a little further out — so a hem is simply where a garment's loft stops and
    /// the skin's starts. Nothing hidden under clothing is generated at all.
    /// This file holds the machinery; HumanoidBodyBuilder.Parts.cs places the parts.
    /// </summary>
    internal sealed partial class HumanoidBodyBuilder
    {
        private const int SlotCount = (int)HumanoidSlot.Gore + 1;

        private struct Ring
        {
            public Vector3 C, R, F;      // centre, and the two axes spanning the ring
            public float Rx, Rz, Exp;    // radii along R and F, superellipse exponent (2 = ellipse)
            public int B0, B1;           // bones, blended by W1 (0 = all B0)
            public float W1;
        }

        /// <summary>A limb profile key: position along the limb (0..1), side and front radii, and how
        /// far the section is pushed forward.</summary>
        private readonly struct Key
        {
            public readonly float T, Rx, Rz, Dz;
            public Key(float t, float rx, float rz, float dz = 0f) { T = t; Rx = rx; Rz = rz; Dz = dz; }
        }

        /// <summary>The outermost surface laid over a limb, remembered so blood can be splashed on it.</summary>
        private struct LimbSurface
        {
            public int Bone;
            public Vector3 A, B;
            public Key[] Keys;
            public float CoverTo, Cover;   // garment inflation over t in [0, CoverTo]
        }

        private readonly HumanoidShape _s;
        private readonly HumanoidProportions _p;
        private readonly List<Vector3> _v = new List<Vector3>(8192);
        private readonly List<Vector3> _n = new List<Vector3>(8192);
        private readonly List<BoneWeight> _w = new List<BoneWeight>(8192);
        private readonly List<int>[] _t = new List<int>[SlotCount];
        private readonly List<Ring> _rings = new List<Ring>(64);
        private readonly List<LimbSurface> _limbs = new List<LimbSurface>(16);

        public HumanoidBodyBuilder(HumanoidShape shape)
        {
            _s = shape;
            _p = HumanoidProportions.For(shape.Build);
            InitTorsoProfile();
        }

        public void Build(HumanoidMesh into)
        {
            BuildHead();
            BuildTorso();
            BuildArm(-1f);
            BuildArm(1f);
            BuildLeg(-1f);
            BuildLeg(1f);
            BuildFoot(-1f);
            BuildFoot(1f);
            BuildHeadwear();
            if (_s.Zombie && _s.Gore != 0) BuildGore();
            ScaleHead();

            var mesh = new Mesh { name = "Humanoid_" + _s };
            if (_v.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.boneWeights = _w.ToArray();

            var slots = new List<HumanoidSlot>(SlotCount);
            for (int s = 0; s < SlotCount; s++)
                if (_t[s] != null && _t[s].Count > 0) slots.Add((HumanoidSlot)s);
            mesh.subMeshCount = slots.Count;
            for (int i = 0; i < slots.Count; i++) mesh.SetTriangles(_t[(int)slots[i]], i, false);

            // Bones bind with no rotation, so each bind pose is just the inverse of the joint position.
            var bind = new Matrix4x4[HumanoidProportions.BoneCount];
            for (int i = 0; i < bind.Length; i++) bind[i] = Matrix4x4.Translate(-_p.Bind[i]);
            mesh.bindposes = bind;
            mesh.RecalculateBounds();

            into.Mesh = mesh;
            into.Slots = slots.ToArray();
        }

        // ── Loft machinery ───────────────────────────────────────────────────────────────────────────

        /// <summary>A touch larger than life, so the face and hair read from the high game camera.</summary>
        private const float HeadScale = 1.1f;

        /// <summary>Grows everything on the head bone — face, hair, hats — about the base of the skull.</summary>
        private void ScaleHead()
        {
            const int head = (int)HumanBone.Head;
            Vector3 pivot = _p.Bind[head];
            for (int i = 0; i < _v.Count; i++)
            {
                BoneWeight w = _w[i];
                if (w.boneIndex0 == head && w.weight0 > 0.999f) _v[i] = pivot + (_v[i] - pivot) * HeadScale;
            }
        }

        private List<int> Tris(HumanoidSlot slot) => _t[(int)slot] ??= new List<int>(1024);

        private static BoneWeight Weight(in Ring r)
        {
            float w1 = Mathf.Clamp01(r.W1);
            if (r.B1 == r.B0 || w1 <= 0.001f) return new BoneWeight { boneIndex0 = r.B0, weight0 = 1f };
            if (w1 >= 0.999f) return new BoneWeight { boneIndex0 = r.B1, weight0 = 1f };
            if (w1 > 0.5f) return new BoneWeight { boneIndex0 = r.B1, weight0 = w1, boneIndex1 = r.B0, weight1 = 1f - w1 };
            return new BoneWeight { boneIndex0 = r.B0, weight0 = 1f - w1, boneIndex1 = r.B1, weight1 = w1 };
        }

        /// <summary>Skins a surface through the rings in <see cref="_rings"/>, optionally closing either
        /// end with a flat cap (rings that shrink to nothing close themselves).</summary>
        private void Loft(HumanoidSlot slot, int seg, bool capStart, bool capEnd)
        {
            int n = _rings.Count;
            if (n < 2) return;
            List<int> tris = Tris(slot);
            int start = _v.Count;

            for (int i = 0; i < n; i++)
            {
                Ring r = _rings[i];
                BoneWeight bw = Weight(r);
                float e = 2f / Mathf.Max(0.5f, r.Exp);
                for (int j = 0; j < seg; j++)
                {
                    float a = (float)j / seg * Mathf.PI * 2f;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    float x = Mathf.Sign(c) * Mathf.Pow(Mathf.Abs(c), e) * r.Rx;
                    float z = Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), e) * r.Rz;
                    _v.Add(r.C + r.R * x + r.F * z);
                    _n.Add(Vector3.zero);
                    _w.Add(bw);
                }
            }

            for (int i = 0; i < n - 1; i++)
            {
                Vector3 inside = (_rings[i].C + _rings[i + 1].C) * 0.5f;
                int r0 = start + i * seg, r1 = r0 + seg;
                for (int j = 0; j < seg; j++)
                {
                    int j1 = (j + 1) % seg;
                    Tri(tris, r0 + j, r0 + j1, r1 + j1, inside, true);
                    Tri(tris, r0 + j, r1 + j1, r1 + j, inside, true);
                }
            }

            for (int i = 0; i < n; i++)
            {
                Ring r = _rings[i];
                bool pole = r.Rx < 1e-5f && r.Rz < 1e-5f;
                Vector3 axis = AxisAt(i);
                Vector3 poleNormal = i == 0 ? -axis : axis;
                for (int j = 0; j < seg; j++)
                {
                    int v = start + i * seg + j;
                    Vector3 nrm = _n[v];
                    _n[v] = pole || nrm.sqrMagnitude < 1e-14f ? poleNormal : nrm.normalized;
                }
            }

            if (capStart) Cap(tris, start, seg, _rings[0], -AxisAt(0));
            if (capEnd) Cap(tris, start + (n - 1) * seg, seg, _rings[n - 1], AxisAt(n - 1));
        }

        private Vector3 AxisAt(int i)
        {
            int n = _rings.Count;
            Vector3 d = _rings[Mathf.Min(n - 1, i + 1)].C - _rings[Mathf.Max(0, i - 1)].C;
            if (d.sqrMagnitude < 1e-12f) d = Vector3.Cross(_rings[i].F, _rings[i].R);
            return d.normalized;
        }

        /// <summary>Adds a triangle wound to face away from <paramref name="inside"/> (Unity treats
        /// clockwise as front-facing); degenerate slivers at closed poles are dropped.</summary>
        private void Tri(List<int> tris, int a, int b, int c, Vector3 inside, bool accumulate)
        {
            Vector3 pa = _v[a], pb = _v[b], pc = _v[c];
            Vector3 n = Vector3.Cross(pb - pa, pc - pa);
            if (n.sqrMagnitude < 1e-16f) return;
            if (Vector3.Dot(n, (pa + pb + pc) / 3f - inside) < 0f) { int t = b; b = c; c = t; n = -n; }
            tris.Add(a); tris.Add(b); tris.Add(c);
            if (!accumulate) return;
            _n[a] += n; _n[b] += n; _n[c] += n;
        }

        private void Cap(List<int> tris, int ringStart, int seg, in Ring r, Vector3 outward)
        {
            BoneWeight bw = Weight(r);
            int centre = _v.Count;
            _v.Add(r.C); _n.Add(outward); _w.Add(bw);
            int first = _v.Count;
            for (int j = 0; j < seg; j++) { _v.Add(_v[ringStart + j]); _n.Add(outward); _w.Add(bw); }
            Vector3 inside = r.C - outward * 0.01f;
            for (int j = 0; j < seg; j++) Tri(tris, centre, first + j, first + (j + 1) % seg, inside, false);
        }

        // ── Primitives ───────────────────────────────────────────────────────────────────────────────

        /// <summary>An ellipsoid (or a superellipsoid, for rounded boxes), optionally only the band
        /// between two polar angles — 0° is the top pole, 180° the bottom — capped flat where cut.</summary>
        private void Ellipsoid(HumanoidSlot slot, int bone, Vector3 c, Vector3 radii, Quaternion rot,
            int seg = 12, int rings = 8, float exp = 2f, float vexp = 2f, float fromDeg = 0f, float toDeg = 180f)
        {
            _rings.Clear();
            Vector3 up = rot * Vector3.up, right = rot * Vector3.right, fwd = rot * Vector3.forward;
            float e = 2f / vexp;
            for (int i = 0; i <= rings; i++)
            {
                float th = Mathf.Lerp(fromDeg, toDeg, (float)i / rings) * Mathf.Deg2Rad;
                float cy = Mathf.Cos(th), sy = Mathf.Sin(th);
                float y = Mathf.Sign(cy) * Mathf.Pow(Mathf.Abs(cy), e);
                float r = Mathf.Pow(Mathf.Abs(sy), e);
                if (r < 1e-4f) r = 0f;
                _rings.Add(new Ring
                {
                    C = c + up * (y * radii.y), R = right, F = fwd,
                    Rx = r * radii.x, Rz = r * radii.z, Exp = exp, B0 = bone, B1 = bone,
                });
            }
            Loft(slot, seg, fromDeg > 0.5f, toDeg < 179.5f);
        }

        private static void SampleKeys(Key[] k, float t, out float rx, out float rz, out float dz)
        {
            int last = k.Length - 1;
            if (t <= k[0].T) { rx = k[0].Rx; rz = k[0].Rz; dz = k[0].Dz; return; }
            if (t >= k[last].T) { rx = k[last].Rx; rz = k[last].Rz; dz = k[last].Dz; return; }
            int i = 0;
            while (i < last - 1 && t > k[i + 1].T) i++;
            float u = Mathf.SmoothStep(0f, 1f, (t - k[i].T) / (k[i + 1].T - k[i].T));
            rx = Mathf.Lerp(k[i].Rx, k[i + 1].Rx, u);
            rz = Mathf.Lerp(k[i].Rz, k[i + 1].Rz, u);
            dz = Mathf.Lerp(k[i].Dz, k[i + 1].Dz, u);
        }

        private static void LimbFrame(Vector3 a, Vector3 b, Vector3 front, out Vector3 axis, out float len, out Vector3 r, out Vector3 f)
        {
            axis = b - a;
            len = axis.magnitude;
            axis = len > 1e-6f ? axis / len : Vector3.down;
            f = Vector3.ProjectOnPlane(front, axis);
            if (f.sqrMagnitude < 1e-8f) f = Vector3.ProjectOnPlane(Vector3.up, axis);
            f.Normalize();
            r = Vector3.Cross(axis, f).normalized;
        }

        /// <summary>
        /// A tapered limb from <paramref name="a"/> to <paramref name="b"/> following a radius profile,
        /// covering t in [t0, t1]. Each end is rounded (a joint, tucked into its neighbour) or cut flat
        /// (a hem). Clothing passes an <paramref name="inflate"/> so it sits outside the skin, and a
        /// <paramref name="flare"/> that widens it toward t1.
        /// </summary>
        private void Limb(HumanoidSlot slot, int bone, Vector3 a, Vector3 b, Vector3 front, Key[] keys,
            float inflate = 0f, float t0 = 0f, float t1 = 1f, bool roundStart = true, bool roundEnd = true,
            int seg = 12, float flare = 0f)
        {
            LimbFrame(a, b, front, out Vector3 axis, out float len, out Vector3 R, out Vector3 F);
            if (len < 1e-5f) return;
            _rings.Clear();
            const int capRings = 4;

            if (roundStart) AddRoundEnd(keys, a, axis, len, R, F, t0, -1f, inflate, bone, capRings);

            int body = Mathf.Max(2, Mathf.CeilToInt((t1 - t0) * len / 0.03f));
            for (int i = 0; i <= body; i++)
            {
                float t = Mathf.Lerp(t0, t1, (float)i / body);
                float extra = inflate + flare * (t1 > t0 ? (t - t0) / (t1 - t0) : 0f);
                SampleKeys(keys, t, out float rx, out float rz, out float dz);
                _rings.Add(new Ring
                {
                    C = a + axis * (t * len) + F * dz, R = R, F = F,
                    Rx = rx + extra, Rz = rz + extra, Exp = 2f, B0 = bone, B1 = bone,
                });
            }

            if (roundEnd) AddRoundEnd(keys, a, axis, len, R, F, t1, 1f, inflate + flare, bone, capRings);
            Loft(slot, seg, !roundStart, !roundEnd);
        }

        private void AddRoundEnd(Key[] keys, Vector3 a, Vector3 axis, float len, Vector3 R, Vector3 F,
            float t, float dir, float inflate, int bone, int capRings)
        {
            SampleKeys(keys, t, out float rx, out float rz, out float dz);
            rx += inflate; rz += inflate;
            Vector3 c = a + axis * (t * len) + F * dz;
            float reach = (rx + rz) * 0.5f;

            // Start caps run pole -> equator, end caps equator -> pole, so the ring order stays continuous.
            for (int k = 0; k < capRings; k++)
            {
                int step = dir < 0f ? capRings - k : k + 1;
                float phi = (float)step / capRings * Mathf.PI * 0.5f;
                float cos = step == capRings ? 0f : Mathf.Cos(phi);
                _rings.Add(new Ring
                {
                    C = c + axis * (dir * reach * Mathf.Sin(phi)), R = R, F = F,
                    Rx = rx * cos, Rz = rz * cos, Exp = 2f, B0 = bone, B1 = bone,
                });
            }
        }

        // ── Torso profile ────────────────────────────────────────────────────────────────────────────

        // Torso cross-sections from crotch to neck: height, half width, half depth, forward offset and
        // superellipse exponent (rounder at the hips, squarer across the shoulders).
        private float[] _ty, _tw, _td, _tz, _te;

        private void InitTorsoProfile()
        {
            HumanoidProportions p = _p;
            _ty = new[] { 0.800f, 0.815f, 0.845f, 0.885f, 0.925f, 0.975f, 1.030f, 1.080f, 1.140f, 1.210f, 1.290f, 1.360f, 1.410f, 1.450f, 1.480f, 1.500f, 1.512f };
            _tw = new[] { 0f, 0.075f, 0.128f, p.HipW * 0.96f, p.HipW, p.HipW * 0.965f, Mathf.Lerp(p.HipW, p.WaistW, 0.65f), p.WaistW,
                          Mathf.Lerp(p.WaistW, p.ChestW, 0.5f), p.ChestW * 0.97f, p.ChestW, p.ChestW * 1.01f, p.ShoulderW * 1.02f, p.ShoulderW * 0.92f, p.ShoulderW * 0.62f, p.Neck + 0.02f, 0f };
            _td = new[] { 0f, 0.060f, 0.088f, p.HipD * 0.98f, p.HipD, p.HipD * 0.97f, Mathf.Lerp(p.HipD, p.WaistD, 0.6f), p.WaistD,
                          Mathf.Lerp(p.WaistD, p.ChestD, 0.5f), p.ChestD * 0.97f, p.ChestD, p.ChestD * 0.94f, p.ChestD * 0.82f, p.ChestD * 0.70f, p.ChestD * 0.55f, p.Neck + 0.01f, 0f };
            _tz = new[] { 0f, 0f, 0f, -0.006f, -0.010f, -0.004f, p.Belly * 0.6f, p.Belly, p.Belly * 0.5f + 0.008f, 0.016f, 0.020f, 0.014f, 0f, -0.008f, -0.012f, -0.014f, -0.014f };
            _te = new[] { 2f, 2.2f, 2.4f, 2.6f, 2.6f, 2.7f, 2.8f, 2.8f, 2.9f, 3.0f, 3.0f, 3.1f, 3.3f, 3.0f, 2.6f, 2.2f, 2f };
        }

        // Cubic Hermite through the keys, so the profile curves smoothly from hips to waist to chest.
        private static float Profile(float[] ys, float[] vs, int i, float y)
        {
            int n = ys.Length;
            float h = ys[i + 1] - ys[i];
            float u = (y - ys[i]) / h;
            float m0 = i > 0 ? (vs[i + 1] - vs[i - 1]) / (ys[i + 1] - ys[i - 1]) : (vs[i + 1] - vs[i]) / h;
            float m1 = i + 2 < n ? (vs[i + 2] - vs[i]) / (ys[i + 2] - ys[i]) : (vs[i + 1] - vs[i]) / h;
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * vs[i] + (u3 - 2f * u2 + u) * h * m0
                 + (-2f * u3 + 3f * u2) * vs[i + 1] + (u3 - u2) * h * m1;
        }

        private void SampleTorso(float y, out float w, out float d, out float z, out float e)
        {
            int last = _ty.Length - 1;
            y = Mathf.Clamp(y, _ty[0], _ty[last]);
            int i = 0;
            while (i < last - 1 && y > _ty[i + 1]) i++;
            w = Mathf.Max(0f, Profile(_ty, _tw, i, y));
            d = Mathf.Max(0f, Profile(_ty, _td, i, y));
            z = Profile(_ty, _tz, i, y);
            e = Mathf.Lerp(_te[i], _te[i + 1], (y - _ty[i]) / (_ty[i + 1] - _ty[i]));
        }

        private Ring TorsoRing(float y, float inflate)
        {
            SampleTorso(y, out float w, out float d, out float z, out float e);
            var r = new Ring
            {
                C = new Vector3(0f, y, z), R = Vector3.right, F = Vector3.forward,
                Rx = w > 1e-4f ? w + inflate : 0f, Rz = d > 1e-4f ? d + inflate : 0f, Exp = e,
            };

            // The torso bends smoothly: hips, then a blend into the spine, then into the chest.
            const int hips = (int)HumanBone.Hips, spine = (int)HumanBone.Spine, chest = (int)HumanBone.Chest;
            if (y < 0.99f) { r.B0 = r.B1 = hips; }
            else if (y < 1.10f) { r.B0 = hips; r.B1 = spine; r.W1 = Mathf.SmoothStep(0f, 1f, (y - 0.99f) / 0.11f); }
            else if (y < 1.17f) { r.B0 = r.B1 = spine; }
            else if (y < 1.28f) { r.B0 = spine; r.B1 = chest; r.W1 = Mathf.SmoothStep(0f, 1f, (y - 1.17f) / 0.11f); }
            else { r.B0 = r.B1 = chest; }
            return r;
        }

        /// <summary>The bone a rigid detail (a button, a pocket) at this height should ride on.</summary>
        private static int TorsoBone(float y) =>
            y < 1.045f ? (int)HumanBone.Hips : y < 1.225f ? (int)HumanBone.Spine : (int)HumanBone.Chest;

        private void TorsoSection(HumanoidSlot slot, float y0, float y1, float inflate, bool capBottom, bool capTop)
        {
            _rings.Clear();
            int steps = Mathf.Max(2, Mathf.CeilToInt((y1 - y0) / 0.028f));
            for (int i = 0; i <= steps; i++) _rings.Add(TorsoRing(Mathf.Lerp(y0, y1, (float)i / steps), inflate));
            Loft(slot, 18, capBottom, capTop);
        }

        /// <summary>Forward (or backward) surface of the torso at height y and sideways offset x.</summary>
        private float SurfaceZ(float y, float x, float inflate, bool front)
        {
            SampleTorso(y, out float w, out float d, out float z, out float e);
            w += inflate; d += inflate;
            float ax = Mathf.Clamp01(Mathf.Abs(x) / Mathf.Max(w, 1e-4f));
            float k = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(ax, e)), 1f / e);
            return z + (front ? d : -d) * k;
        }
    }
}
