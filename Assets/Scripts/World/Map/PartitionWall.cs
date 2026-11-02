using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A plain stretch of partition in a real building's interior (an <see cref="InteriorBuild"/>'s wall piece with no doorway in
    /// it), breakable as <see cref="BreakableWall"/> has it: a hole is cut out of the piece's own mesh and the piece's colliders
    /// are made again round it; the dead batter one only from inside the building; what hangs on it (a switch, the breaker
    /// panel, cupboards) keeps a hole off, but a picture or a poster comes down with the plaster, and a blow nearby can knock one
    /// askew; a hole's parts go with the piece when the isometric camera cuts it away, and with its level.
    /// </summary>
    internal sealed class PartitionWall : BreakableWall
    {
        private static readonly List<Vector2> s_Standing = new List<Vector2>(4);

        private InteriorBuild _build;
        private BoxCollider _box;
        private readonly List<BoxCollider> _extra = new List<BoxCollider>(2);

        internal override float HalfThick => RaidBuilding.Half;
        protected override float CutDepth => 0.16f;
        /// <summary>The meshes made for its holes go with the building's (see InteriorBuild.Discard).</summary>
        internal override List<Mesh> Meshes => _build != null ? _build.Meshes : null;
        protected override Transform MeshesRoot => _build != null && _build.Root != null ? _build.Root.transform : null;

        /// <summary>Makes <paramref name="piece"/> (a plain stretch of partition, its mesh drawn) breakable, with the holes made
        /// in it while its building last stood put back; null for a doorway's head, or a stretch too short ever to hold a hole.</summary>
        internal static PartitionWall Fit(InteriorBuild build, InteriorPass.Piece piece, float height)
        {
            GameObject go = piece.Object;
            if (go == null || piece.Door != null) return null;
            var box = go.GetComponent<BoxCollider>();
            float len = piece.E - piece.S;
            if (box == null || go.GetComponent<MeshFilter>() == null || len < Width + 2f * EndMargin) return null;
            var w = go.AddComponent<PartitionWall>();
            w._build = build;
            w._box = box;
            w._alongX = piece.AlongX;
            w._lo = -len * 0.5f;
            w._hi = len * 0.5f;
            w._height = height;
            w._key = Interactable.KeyFor("partition", go.transform.position);
            w.Begin();
            return w;
        }

        // The dead batter a partition only from inside its building (in one of its rooms, on its floor).
        protected override bool CanBatter(Vector3 from, Vector3 towards) => _build != null && _build.Plan.RoomAt(_build.LocalOf(from)) >= 0;

        protected override void CutMeshes(List<MeshFilter> into)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null) into.Add(mf);
        }

        // A box to each stretch left standing between its holes, and one over each hole that stops short of the top.
        protected override void Shape()
        {
            for (int i = 0; i < _extra.Count; i++) InteriorPass.Kill(_extra[i]);
            _extra.Clear();
            Stretches(s_Standing);
            float thick = HalfThick * 2f;
            bool first = true;
            foreach (Vector2 s in s_Standing)
            {
                BoxCollider bc = first ? _box : gameObject.AddComponent<BoxCollider>();
                if (!first) _extra.Add(bc);
                first = false;
                bc.center = Local((s.x + s.y) * 0.5f, _height * 0.5f, 0f);
                bc.size = Size(s.y - s.x, _height, thick);
            }
            s_Standing.Clear();
            _box.enabled = !first;
            foreach (WallBreach h in Holes)
            {
                if (h == null || h.State == WallBreach.Intact || h.Top >= _height - 0.01f) continue;
                var over = gameObject.AddComponent<BoxCollider>();
                _extra.Add(over);
                over.center = Local(h.Centre, (h.Top + _height) * 0.5f, 0f);
                over.size = Size(h.B - h.A, _height - h.Top, thick);
            }
        }

        // Everything under its object but its holes: a switch, the breaker panel, cupboards, a clock. A picture or a poster
        // doesn't hold a wall up: it comes down with it (see Fell).
        protected override void Hangings()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform c = transform.GetChild(i);
                if (c.GetComponent<WallBreach>() == null && c.GetComponent<PictureFixture>() == null) Hung(c);
            }
        }

        // The building's furniture, litter and other walls.
        protected override void NearMeshes(List<MeshFilter> into)
        {
            into.Clear();
            if (_build != null && _build.Root != null) _build.Root.GetComponentsInChildren(false, into);
        }

        // The hole's parts hide with the piece when the isometric camera cuts it away, and go with its level.
        protected override void PartsChanged(WallBreach hole)
        {
            var marker = GetComponent<CutawayOccluder>();
            if (marker != null) marker.PartsChanged();
            if (_build == null || _build.Root == null) return;
            hole.GetComponentsInChildren(true, s_Renderers);
            MapCutaway.InteriorPartsAdded(_build.Plan.Id, _build.Root, s_Renderers);
            s_Renderers.Clear();
        }

        // The pictures and posters hanging across a hole, gone with the wall they hung on.
        protected override void Fell(WallBreach hole)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform c = transform.GetChild(i);
                if (c.GetComponent<PictureFixture>() == null) continue;
                float lo = float.MaxValue, hi = float.MinValue;
                Spread(transform.InverseTransformPoint(c.position), ref lo, ref hi);
                c.GetComponentsInChildren(true, s_Renderers);
                for (int k = 0; k < s_Renderers.Count; k++)
                    if (s_Renderers[k] != null && s_Renderers[k].bounds.size != Vector3.zero) Corners(s_Renderers[k].bounds, ref lo, ref hi);
                s_Renderers.Clear();
                if (hi > hole.A && lo < hole.B) InteriorPass.Kill(c.gameObject);
            }
        }

        // The pictures near a blow may be knocked askew, as on the starter house's walls.
        internal override void Jolted(WallBreach hole)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var picture = transform.GetChild(i).GetComponent<PictureFixture>();
                if (picture == null) continue;
                float along = AlongOf(transform.InverseTransformPoint(picture.transform.position));
                if (Mathf.Abs(along - hole.Centre) < Width + 1f) picture.Jolt();
            }
        }
    }
}
