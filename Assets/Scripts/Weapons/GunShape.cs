using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The outline of a gun model as it is held, measured once per model from its vertices, so attachments sit on the
    /// gun that's drawn rather than on a guess at it: the generated meshes (Tools/AssetPipeline/mesh) differ a lot from one
    /// version of a gun to the next, one shotgun's barrel runs five centimetres off the middle, and another was made end for
    /// end. Everything is in gun space: the item's space (see <see cref="HumanoidRig.SetHeldItem"/>), with the muzzle at +X,
    /// the top of the gun at +Z and its right side at +Y, a <see cref="Reversed"/> model turned round to match.
    ///
    /// The model is cut into <see cref="Slices"/> slices along X, and each keeps its highest and lowest point and how far it
    /// reaches either side; <see cref="Muzzle"/> is the middle of the bore's end. A generated mesh is read again from its file
    /// for this (the one drawn is on the graphics card only); a primitive model is measured from its parts, with points along
    /// their edges so a long part counts in every slice it crosses. Measured once per model and kept (plain numbers, nothing
    /// to destroy).
    /// </summary>
    public sealed class GunShape
    {
        /// <summary>How many slices the model is cut into along its length.</summary>
        public const int Slices = 32;

        /// <summary>Shorter than this (metres) the gun is held and dressed as a handgun, as <see cref="HumanoidRig"/> holds it.</summary>
        public const float PistolLength = 0.42f;

        private const uint Magic = 0x324D5A43;   // GeneratedItemMeshes' "CZM2"
        private const int FormatVersion = 3, HeaderSize = 28, VertexStride = 18;

        private static readonly Dictionary<string, GunShape> s_Cache = new Dictionary<string, GunShape>(16);

        private readonly float[] _top = new float[Slices], _bottom = new float[Slices];
        private readonly float[] _right = new float[Slices], _left = new float[Slices];
        private readonly float[] _topY = new float[Slices], _bottomY = new float[Slices];
        private readonly float _x0, _length;

        /// <summary>The model's box, in gun space (see <see cref="Reversed"/>).</summary>
        public Bounds Box { get; }
        /// <summary>True when the model was made end for end, its muzzle at item -X (some generated meshes are): everything
        /// here is measured in gun space, the model turned half round its up axis (<see cref="EndForEnd"/>) so the muzzle is
        /// at +X, and whatever holds the model turns it the same way.</summary>
        public bool Reversed { get; }
        /// <summary>Gun space from the model's own, for a <see cref="Reversed"/> model (half a turn round item Z; its own
        /// inverse).</summary>
        public static readonly Quaternion EndForEnd = Quaternion.Euler(0f, 0f, 180f);
        /// <summary>From the model's space to gun space: <see cref="EndForEnd"/> for a reversed model, else none.</summary>
        public Quaternion ToGun => Reversed ? EndForEnd : Quaternion.identity;
        /// <summary>True for a handgun (shorter than <see cref="PistolLength"/>).</summary>
        public bool Pistol => _length < PistolLength;
        /// <summary>Length along the bore, metres.</summary>
        public float Length => _length;
        /// <summary>The middle of the bore's end: the top of what the muzzle face shows, where the barrel is.</summary>
        public Vector3 Muzzle { get; }
        /// <summary>Roughly the barrel's radius at the muzzle, metres.</summary>
        public float BoreRadius { get; }

        private GunShape(List<Vector3> v, bool reversed)
        {
            Reversed = reversed;
            var b = new Bounds(v[0], Vector3.zero);
            for (int i = 1; i < v.Count; i++) b.Encapsulate(v[i]);
            Box = b;
            _x0 = b.min.x;
            _length = Mathf.Max(1e-4f, b.size.x);

            for (int k = 0; k < Slices; k++)
            {
                _top[k] = _right[k] = float.NegativeInfinity;
                _bottom[k] = _left[k] = float.PositiveInfinity;
            }
            for (int i = 0; i < v.Count; i++)
            {
                Vector3 p = v[i];
                int k = Slice(p.x);
                if (p.z > _top[k]) _top[k] = p.z;
                if (p.z < _bottom[k]) _bottom[k] = p.z;
                if (p.y > _right[k]) _right[k] = p.y;
                if (p.y < _left[k]) _left[k] = p.y;
            }

            // Where across the gun its top and its underside are (the middle of what lies within 4 mm of each).
            var topLo = new float[Slices]; var topHi = new float[Slices];
            var botLo = new float[Slices]; var botHi = new float[Slices];
            for (int k = 0; k < Slices; k++)
            {
                topLo[k] = botLo[k] = float.PositiveInfinity;
                topHi[k] = botHi[k] = float.NegativeInfinity;
            }
            for (int i = 0; i < v.Count; i++)
            {
                Vector3 p = v[i];
                int k = Slice(p.x);
                if (p.z > _top[k] - 0.004f) { topLo[k] = Mathf.Min(topLo[k], p.y); topHi[k] = Mathf.Max(topHi[k], p.y); }
                if (p.z < _bottom[k] + 0.004f) { botLo[k] = Mathf.Min(botLo[k], p.y); botHi[k] = Mathf.Max(botHi[k], p.y); }
            }
            for (int k = 0; k < Slices; k++)
            {
                _topY[k] = float.IsInfinity(topLo[k]) ? float.NaN : (topLo[k] + topHi[k]) * 0.5f;
                _bottomY[k] = float.IsInfinity(botLo[k]) ? float.NaN : (botLo[k] + botHi[k]) * 0.5f;
            }
            FillGaps(_top); FillGaps(_bottom); FillGaps(_right); FillGaps(_left); FillGaps(_topY); FillGaps(_bottomY);

            // The muzzle face: what lies within a thin slab of the front end. A magazine tube or a frame hangs below the
            // bore, so the bore is taken from the top of the face.
            float slab = Mathf.Max(0.004f, 0.012f * _length);
            float z0 = float.PositiveInfinity, z1 = float.NegativeInfinity, y0 = float.PositiveInfinity, y1 = float.NegativeInfinity;
            for (int i = 0; i < v.Count; i++)
            {
                Vector3 p = v[i];
                if (p.x < b.max.x - slab) continue;
                z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
                y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y);
            }
            float r = Mathf.Clamp(0.5f * Mathf.Min(z1 - z0, y1 - y0), 0.005f, 0.03f);
            BoreRadius = r;
            Muzzle = new Vector3(b.max.x, (y0 + y1) * 0.5f, Mathf.Max((z0 + z1) * 0.5f, z1 - r * 1.15f));
        }

        /// <summary>The measured outline of the model drawn for <paramref name="def"/> (<paramref name="mesh"/>, as
        /// <see cref="ItemMeshBaker.Get(ItemDef, ItemMeshBaker.Lod, int)"/> handed it out), or null when it can't be read.</summary>
        public static GunShape Of(ItemDef def, Mesh mesh)
        {
            if (def == null || mesh == null) return null;
            string key = mesh.name + "|" + def.Id;
            if (s_Cache.TryGetValue(key, out GunShape cached)) return cached;
            GunShape shape = null;
            try
            {
                List<Vector3> v = Vertices(def, mesh.name, out bool generated);
                if (v == null || v.Count < 4) v = Corners(mesh.bounds);
                shape = new GunShape(v, false);
                // A generated mesh can come out end for end. The butt of a stock or the grip of a handgun stands much taller
                // than a muzzle, so a model whose front end does is turned round.
                if (generated && shape.EndHeight(true) > shape.EndHeight(false) * 1.25f)
                {
                    for (int i = 0; i < v.Count; i++) v[i] = EndForEnd * v[i];
                    shape = new GunShape(v, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Couldn't measure the " + def.Id + " model for its attachments (" + e.Message + "); using its box.");
                shape = new GunShape(Corners(mesh.bounds), false);
            }
            s_Cache[key] = shape;
            return shape;
        }

        // How tall the gun stands over its last (front) or first (back) tenth.
        private float EndHeight(bool front)
        {
            int n = Mathf.Max(1, Slices / 10);
            float sum = 0f;
            for (int i = 0; i < n; i++)
            {
                int k = front ? Slices - 1 - i : i;
                sum += _top[k] - _bottom[k];
            }
            return sum / n;
        }

        // Generated meshes and the primitive models can change between play sessions in the editor (a new bake): measure afresh.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetLastPlaySession() => s_Cache.Clear();

        #region Queries
        /// <summary>The point <paramref name="u"/> (0 the butt, 1 the muzzle) of the way along the gun, as item X.</summary>
        public float X(float u) => _x0 + u * _length;

        /// <summary>The top of the gun between <paramref name="xFrom"/> and <paramref name="xTo"/> (the median of the slices'
        /// highest points, so a sight post or a hammer doesn't lift what sits there), and in <paramref name="y"/> the middle of
        /// the top across the gun.</summary>
        public float TopAt(float xFrom, float xTo, out float y) => Median(_top, _topY, xFrom, xTo, out y);

        /// <summary>The underside of the gun between <paramref name="xFrom"/> and <paramref name="xTo"/> (the median of the
        /// slices' lowest points), and in <paramref name="y"/> the middle of the underside across the gun.</summary>
        public float BottomAt(float xFrom, float xTo, out float y) => Median(_bottom, _bottomY, xFrom, xTo, out y);

        /// <summary>The gun's right side (item +Y) at <paramref name="x"/>.</summary>
        public float RightAt(float x) => _right[Slice(x)];

        /// <summary>The middle of the gun's lowest stretch in its back half: a handgun's grip, where its magazine goes in.</summary>
        public Vector3 GripBase()
        {
            float lowest = Box.min.z;
            float sum = 0f, ySum = 0f;
            int n = 0;
            for (int k = 0; k < Slices / 2; k++)
            {
                if (_bottom[k] > lowest + 0.006f) continue;
                sum += k + 0.5f;
                ySum += _bottomY[k];
                n++;
            }
            if (n == 0) return new Vector3(X(0.25f), Box.center.y, lowest);
            return new Vector3(X(sum / n / Slices), ySum / n, lowest);
        }
        #endregion

        #region Measuring
        private int Slice(float x) => Mathf.Clamp(Mathf.FloorToInt((x - _x0) / _length * Slices), 0, Slices - 1);

        private float Median(float[] value, float[] across, float xFrom, float xTo, out float y)
        {
            int a = Slice(Mathf.Min(xFrom, xTo)), b = Slice(Mathf.Max(xFrom, xTo));
            int n = b - a + 1;
            var vs = new float[n];
            var ys = new float[n];
            for (int k = a; k <= b; k++)
            {
                vs[k - a] = value[k];
                ys[k - a] = across[k];
            }
            Array.Sort(vs);
            Array.Sort(ys);
            y = ys[n / 2];
            return vs[n / 2];
        }

        // An empty slice (a gap in the model) takes its nearest neighbours' values.
        private static void FillGaps(float[] a)
        {
            for (int k = 0; k < a.Length; k++)
            {
                if (!float.IsInfinity(a[k]) && !float.IsNaN(a[k])) continue;
                float before = float.NaN, after = float.NaN;
                int db = 0, da = 0;
                for (int j = k - 1; j >= 0; j--) if (!float.IsInfinity(a[j]) && !float.IsNaN(a[j])) { before = a[j]; db = k - j; break; }
                for (int j = k + 1; j < a.Length; j++) if (!float.IsInfinity(a[j]) && !float.IsNaN(a[j])) { after = a[j]; da = j - k; break; }
                if (float.IsNaN(before)) a[k] = float.IsNaN(after) ? 0f : after;
                else if (float.IsNaN(after)) a[k] = before;
                else a[k] = Mathf.Lerp(before, after, db / (float)(db + da));
            }
        }

        private static List<Vector3> Corners(Bounds b)
        {
            var v = new List<Vector3>(8);
            for (int i = 0; i < 8; i++)
                v.Add(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
            return v;
        }

        // The model's vertices in item space: a generated mesh's from its file (its name carries the file's stem), else the
        // parts of the primitive model ItemMeshBaker baked (the same description it chose).
        private static List<Vector3> Vertices(ItemDef def, string meshName, out bool generatedMesh)
        {
            const string generated = "ItemMesh_";
            generatedMesh = false;
            if (meshName != null && meshName.StartsWith(generated, StringComparison.Ordinal))
            {
                int end = meshName.LastIndexOf('_');
                if (end > generated.Length)
                {
                    List<Vector3> v = ReadGenerated(meshName.Substring(generated.Length, end - generated.Length));
                    if (v != null)
                    {
                        generatedMesh = true;
                        return v;
                    }
                }
            }
            ItemModelDesc desc = ItemModels.Describe(def) ?? GeneratedItemModels.Describe(def) ?? FallbackItemModel.Describe(def);
            if (desc == null || desc.Parts.Count == 0) return null;
            var verts = new List<Vector3>(desc.Parts.Count * 24);
            var starts = new int[desc.Parts.Count];
            for (int i = 0; i < desc.Parts.Count; i++)
            {
                Part part = desc.Parts[i];
                Matrix4x4 m = Matrix4x4.TRS(part.Position, part.Rotation, part.Scale);
                Vector3[] src = PrimitiveMesh.For(part.Shape, ItemMeshBaker.Lod.Near).Verts;
                starts[i] = verts.Count;
                for (int j = 0; j < src.Length; j++) verts.Add(m.MultiplyPoint3x4(src[j]));
            }

            // A primitive has corners only at its ends, so a long part (a forend, a stock) would show only in the slices at
            // its ends, and a small part between them (a barrel band, a sight) would stand for the whole gun there. Points
            // along every edge, under half a slice apart, put each part in every slice it crosses.
            float x0 = float.PositiveInfinity, x1 = float.NegativeInfinity;
            for (int i = 0; i < verts.Count; i++) { x0 = Mathf.Min(x0, verts[i].x); x1 = Mathf.Max(x1, verts[i].x); }
            float step = Mathf.Max(1e-3f, (x1 - x0) / (Slices * 2));
            for (int i = 0; i < desc.Parts.Count; i++)
            {
                int[] tris = PrimitiveMesh.For(desc.Parts[i].Shape, ItemMeshBaker.Lod.Near).Tris;
                for (int t = 0; t < tris.Length; t += 3)
                    for (int e = 0; e < 3; e++)
                    {
                        Vector3 a = verts[starts[i] + tris[t + e]], b = verts[starts[i] + tris[t + (e + 1) % 3]];
                        int n = Mathf.FloorToInt(Mathf.Abs(b.x - a.x) / step);
                        for (int k = 1; k <= n; k++) verts.Add(Vector3.Lerp(a, b, k / (n + 1f)));
                    }
            }
            return verts;
        }

        // The near level's vertex positions from a generated mesh's file, read as GeneratedItemMeshes reads it: a 28-byte
        // header, then the (deflated) body, whose first level is its vertex and index counts, the quantisation box and the
        // vertices (three 16-bit position steps, then a normal, tangent and UV this doesn't need).
        private static List<Vector3> ReadGenerated(string stem)
        {
            TextAsset ta = Resources.Load<TextAsset>(GeneratedItemMeshes.ResourceFolder + "/" + stem);
            if (ta == null) return null;
            byte[] data = ta.bytes;
            Resources.UnloadAsset(ta);
            if (data.Length < HeaderSize || BitConverter.ToUInt32(data, 0) != Magic || BitConverter.ToUInt16(data, 4) != FormatVersion)
                return null;
            uint flags = BitConverter.ToUInt32(data, 20);
            var body = new byte[checked((int)BitConverter.ToUInt32(data, 24))];
            using (Stream packed = new MemoryStream(data, HeaderSize, data.Length - HeaderSize))
            using (Stream source = (flags & 1u) != 0 ? new DeflateStream(packed, CompressionMode.Decompress) : packed)
            {
                int read = 0;
                while (read < body.Length)
                {
                    int got = source.Read(body, read, body.Length - read);
                    if (got <= 0) return null;
                    read += got;
                }
            }
            if (body.Length < 32) return null;
            int count = checked((int)BitConverter.ToUInt32(body, 0));
            if (count <= 0 || 32L + (long)count * VertexStride > body.Length) return null;
            var min = new Vector3(BitConverter.ToSingle(body, 8), BitConverter.ToSingle(body, 12), BitConverter.ToSingle(body, 16));
            var step = new Vector3(BitConverter.ToSingle(body, 20), BitConverter.ToSingle(body, 24), BitConverter.ToSingle(body, 28)) / 65535f;
            var v = new List<Vector3>(count);
            for (int i = 0, o = 32; i < count; i++, o += VertexStride)
                v.Add(min + Vector3.Scale(new Vector3(BitConverter.ToUInt16(body, o), BitConverter.ToUInt16(body, o + 2), BitConverter.ToUInt16(body, o + 4)), step));
            return v;
        }
        #endregion
    }
}
