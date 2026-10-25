using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The interiors of the current world — boxes of space under a roof. The raid map registers each
    /// building's interior as it builds it; the body-temperature model treats a survivor inside one as
    /// sheltered from the wind and snow. Cleared whenever a new world is built. A streamed map registers a
    /// piece's interiors under that piece (<see cref="Owner"/>) and forgets them when the piece is unloaded.
    /// </summary>
    public static class Shelter
    {
        private static readonly List<Bounds> s_Interiors = new List<Bounds>();
        private static readonly List<object> s_Owners = new List<object>();

        /// <summary>Whose interiors are being registered (a streamed map piece while it's built); null otherwise.</summary>
        public static object Owner;

        public static int Count => s_Interiors.Count;

        /// <summary>Bumped whenever the interiors change (one registered, a piece's forgotten, all cleared).</summary>
        public static int Version { get; private set; }

        /// <summary>Registers an interior volume (world space), under the current <see cref="Owner"/>.</summary>
        public static void Register(Bounds interior)
        {
            s_Interiors.Add(interior);
            s_Owners.Add(Owner);
            Version++;
        }

        /// <summary>Forgets the interiors registered under <paramref name="owner"/>.</summary>
        public static void Forget(object owner)
        {
            if (owner == null) return;
            for (int i = s_Interiors.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(s_Owners[i], owner)) continue;
                s_Interiors.RemoveAt(i);
                s_Owners.RemoveAt(i);
            }
            Version++;
        }

        /// <summary>Forgets every interior (called before a world is built).</summary>
        public static void Clear()
        {
            s_Interiors.Clear();
            s_Owners.Clear();
            Owner = null;
            Version++;
        }

        /// <summary>True when the position lies inside a registered interior.</summary>
        public static bool IsSheltered(Vector3 position)
        {
            for (int i = 0; i < s_Interiors.Count; i++)
                if (s_Interiors[i].Contains(position)) return true;
            return false;
        }

        /// <summary>Collects the interiors overlapping <paramref name="area"/> (world space) into <paramref name="into"/>
        /// (cleared first).</summary>
        public static void Overlapping(Bounds area, List<Bounds> into)
        {
            into.Clear();
            for (int i = 0; i < s_Interiors.Count; i++)
                if (s_Interiors[i].Intersects(area)) into.Add(s_Interiors[i]);
        }
    }

    /// <summary>
    /// Which ground of one patch lies under a roof: the columns of the registered interiors there (each grown by a pad),
    /// laid on a one-metre grid, so falling rain and snow can test thousands of points a second without walking every
    /// interior of the map each time. Read again only when the patch moves on or the interiors change.
    /// </summary>
    public sealed class RoofMap
    {
        private const float Cell = 1f, Slack = 4f;
        private readonly List<Bounds> _boxes = new List<Bounds>();
        private bool[] _cells = new bool[0];
        private int _nx, _nz, _version = -1;
        private float _x0, _z0, _x1, _z1, _pad = -1f;
        private bool _any;

        /// <summary>Makes sure the map covers <paramref name="area"/> (world x, z), with every interior grown by
        /// <paramref name="pad"/> round its sides.</summary>
        public void Cover(Rect area, float pad)
        {
            if (_version == Shelter.Version && Mathf.Approximately(_pad, pad)
                && area.xMin >= _x0 && area.yMin >= _z0 && area.xMax <= _x1 && area.yMax <= _z1) return;
            _version = Shelter.Version;
            _pad = pad;
            _x0 = area.xMin - Slack;
            _z0 = area.yMin - Slack;
            _x1 = area.xMax + Slack;
            _z1 = area.yMax + Slack;
            // Every interior over the patch, whatever its height: rooms are on the ground, and nothing falls into one.
            Shelter.Overlapping(new Bounds(new Vector3((_x0 + _x1) * 0.5f, 0f, (_z0 + _z1) * 0.5f),
                new Vector3(_x1 - _x0 + pad * 2f, 1e6f, _z1 - _z0 + pad * 2f)), _boxes);
            _any = _boxes.Count > 0;
            if (!_any) return;
            _nx = Mathf.CeilToInt((_x1 - _x0) / Cell);
            _nz = Mathf.CeilToInt((_z1 - _z0) / Cell);
            int n = _nx * _nz;
            if (_cells.Length < n) _cells = new bool[n];
            else System.Array.Clear(_cells, 0, n);
            for (int i = 0; i < _boxes.Count; i++)
            {
                Vector3 lo = _boxes[i].min, hi = _boxes[i].max;
                int ix0 = Mathf.Max(0, Mathf.FloorToInt((lo.x - pad - _x0) / Cell)), ix1 = Mathf.Min(_nx - 1, Mathf.FloorToInt((hi.x + pad - _x0) / Cell));
                int iz0 = Mathf.Max(0, Mathf.FloorToInt((lo.z - pad - _z0) / Cell)), iz1 = Mathf.Min(_nz - 1, Mathf.FloorToInt((hi.z + pad - _z0) / Cell));
                for (int z = iz0; z <= iz1; z++)
                    for (int x = ix0; x <= ix1; x++) _cells[z * _nx + x] = true;
            }
        }

        /// <summary>True when the column through (<paramref name="x"/>, <paramref name="z"/>) passes through a room (or
        /// within the pad of one). Ground off the patch last covered counts as open.</summary>
        public bool Covered(float x, float z)
        {
            if (!_any) return false;
            int ix = Mathf.FloorToInt((x - _x0) / Cell), iz = Mathf.FloorToInt((z - _z0) / Cell);
            return ix >= 0 && iz >= 0 && ix < _nx && iz < _nz && _cells[iz * _nx + ix];
        }

        /// <summary>True when something drifting from (<paramref name="x"/>, <paramref name="z"/>) by
        /// (<paramref name="dx"/>, <paramref name="dz"/>) as it falls passes over a room anywhere on the way.</summary>
        public bool CoveredAlong(float x, float z, float dx, float dz)
        {
            if (!_any) return false;
            int steps = 1 + Mathf.CeilToInt(Mathf.Sqrt(dx * dx + dz * dz) / Cell);
            for (int k = 0; k <= steps; k++)
            {
                float t = (float)k / steps;
                if (Covered(x + dx * t, z + dz * t)) return true;
            }
            return false;
        }
    }
}
