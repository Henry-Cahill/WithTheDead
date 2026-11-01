using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    // The cutaway in a building with floors above or below the ground one (KentuckyMap.Levels.cs). Whatever is over the floor
    // the survivor is on goes with the roof: the storeys of the upper part above that floor (unseen, casting their shadows
    // still, as the roof does), and the floors, stairs and stairwell walls and the furnished rooms over it (not drawn); the
    // walls of the survivor's own storey show by a stand-in with the facings turned to the camera lowered, as the ground
    // storey's do (MapCutaway.Overlay.cs). From the basement everything from the ground floor up goes. A building whose roof
    // is lifted while the survivor is outside it (it stands in their way) loses everything over its ground floor, as one
    // without floors loses its upper part.
    internal static partial class MapCutaway
    {
        /// <summary>The survivor counts as on the floor above once their feet are this close below it (on the stairs up), and
        /// stays on a floor till they're this much further down the stairs from it.</summary>
        internal static float LevelEarly = 0.6f, LevelLinger = 0.4f;

        /// <summary>Seconds between making sure the lifted floors' rooms stay hidden (the walls' cutaway puts back what it hid
        /// when it's done with it).</summary>
        private const float ReassertInterval = 1f;

        private static readonly Dictionary<long, Building> s_Leveled = new Dictionary<long, Building>(64);
        private static readonly Dictionary<long, List<LevelInterior>> s_Interiors = new Dictionary<long, List<LevelInterior>>(64);
        private static readonly List<Renderer> s_LevelScratch = new List<Renderer>(64);
        /// <summary>The facings turned to the camera at the last scan (none with the walls' cutaway off).</summary>
        private static int s_Facing;

        /// <summary>A furnished level of a building with levels: its renderers, hidden while the level is lifted.</summary>
        private sealed class LevelInterior
        {
            public int Level;
            public GameObject Root;
            public readonly List<Renderer> Renderers = new List<Renderer>(64);
            public bool Hidden;
        }

        /// <summary>Buildings with levels registered now, and furnished levels known (for the harness).</summary>
        internal static int LeveledCount => s_Leveled.Count;
        internal static int LevelInteriorCount
        {
            get
            {
                int n = 0;
                foreach (List<LevelInterior> list in s_Interiors.Values) n += list.Count;
                return n;
            }
        }

        private static void ResetLevelStatics()
        {
            s_Leveled.Clear();
            s_Interiors.Clear();
            s_LevelScratch.Clear();
            s_Facing = 0;
        }

        /// <summary>Gives a building record its levels' parts as <see cref="CutawayPass"/> emitted them: the upper part's
        /// storeys (<paramref name="bands"/>, null when it wasn't split), each level's floors and stairs and its stairwell's
        /// walls.</summary>
        internal static void SetLevels(Building b, KentuckyMap.LevelLayout levels, GameObject[] bands, GameObject[] structure, GameObject[] stairwells)
        {
            b.Levels = levels;
            if (bands != null)
            {
                int n = b.Local.Length;
                var groups = new byte[n];
                int all = 0;
                for (int i = 0; i < n; i++)
                {
                    Vector2 d = b.Local[(i + 1) % n] - b.Local[i];
                    groups[i] = (byte)KentuckyMap.BuildingShell.Facing(new Vector2(d.y, -d.x));
                    if (d.sqrMagnitude > 0.0025f) all |= 1 << groups[i];
                }
                b.Bands = new MeshRenderer[bands.Length];
                b.BandMeshes = new Mesh[bands.Length];
                b.BandWalls = new Building[bands.Length];
                for (int k = 0; k < bands.Length; k++)
                {
                    if (bands[k] == null) continue;
                    b.Bands[k] = bands[k].GetComponent<MeshRenderer>();
                    b.BandMeshes[k] = bands[k].GetComponent<MeshFilter>().sharedMesh;
                    if (k == 0) continue;
                    b.BandWalls[k] = new Building
                    {
                        Id = b.Id, Kind = b.Kind, Middle = b.Middle, Local = b.Local, EdgeGroup = groups, Groups = all,
                        WallThickness = b.WallThickness, Walls = b.Bands[k], WallsMesh = b.BandMeshes[k], StubBase = levels.FloorOf(k),
                    };
                }
            }
            if (structure != null)
            {
                b.Structure = new Renderer[structure.Length];
                for (int i = 0; i < structure.Length; i++) b.Structure[i] = structure[i] != null ? structure[i].GetComponent<Renderer>() : null;
            }
            if (stairwells != null)
            {
                b.Stairwells = new Renderer[stairwells.Length];
                for (int i = 0; i < stairwells.Length; i++)
                    b.Stairwells[i] = stairwells[i] != null ? stairwells[i].GetComponentInChildren<Renderer>(true) : null;
            }
        }

        // A building with levels comes into the world with its site: its storeys' stand-in records go with it.
        private static void RegisterLevels(Building b)
        {
            if (b.Levels == null) return;
            s_Leveled[b.Id] = b;
            b.Level = int.MinValue;
            b.Shown = int.MaxValue;
            b.ShownFacing = 0;
            if (b.BandWalls == null) return;
            foreach (Building w in b.BandWalls)
            {
                if (w == null) continue;
                w.Site = b.Site;
                w.Base = b.Base;
                w.Pts = b.Pts;
                w.Bounds = b.Bounds;
                w.Cover = b.Cover;
            }
        }

        // ... and goes with it, everything of it shown again.
        private static void ForgetLevels(Building b)
        {
            if (b.Levels == null) return;
            b.Level = int.MinValue;
            ShowLevels(b, true);
            if (b.BandWalls != null)
                foreach (Building w in b.BandWalls)
                {
                    if (w == null) continue;
                    w.Applied = 0;
                    ReleaseOverlay(w);
                    w.WallState = WallsWhole;
                    w.Site = null;
                }
            if (s_Leveled.TryGetValue(b.Id, out Building known) && ReferenceEquals(known, b)) s_Leveled.Remove(b.Id);
        }

        // The level of building b the survivor whose feet are at y (world) is on: the one whose storey they're in, the next
        // one up once they're on the stairs within reach of its floor; they stay on a floor till they're a little further
        // down the stairs from it.
        private static int LevelOf(Building b, float y)
        {
            KentuckyMap.LevelLayout L = b.Levels;
            float h = y - b.Base;
            int level = L.LevelAt(h, LevelEarly);
            int was = b.Level;
            if (was > level && was >= L.Lowest && was <= L.Top && h >= L.FloorOf(was) - LevelEarly - LevelLinger) return was;
            return level;
        }

        // What of a building with levels is cut: everything over the floor the survivor's on while they're in it with its
        // upper part lifted, everything over its ground floor while it's lifted with them outside, nothing otherwise; and
        // the walls of their own floor lowered on the camera's side. `force` puts every part right, whatever it was.
        private static void ShowLevels(Building b, bool force)
        {
            KentuckyMap.LevelLayout L = b.Levels;
            if (L == null) return;
            int shown = (b.Applied & UpperBit) == 0 ? int.MaxValue : b.Occupied && b.Level != int.MinValue ? b.Level : 0;
            int lowered = shown >= 1 && shown != int.MaxValue ? s_Facing : 0;
            float now = Time.unscaledTime;
            bool changed = force || shown != b.Shown || lowered != b.ShownFacing;
            if (!changed && (shown == int.MaxValue || now < b.Reassert)) return;
            b.Shown = shown;
            b.ShownFacing = lowered;
            b.Reassert = now + ReassertInterval;

            // The upper part's storeys: those over the survivor's floor go as the roof does; their own floor's walls show by
            // its stand-in, lowered on the camera's side (or go whole when it can't be made).
            if (b.Bands != null)
                for (int k = 0; k < b.Bands.Length; k++)
                {
                    MeshRenderer r = b.Bands[k];
                    if (r == null) continue;
                    bool here = k >= 1 && k == shown;
                    int cut = here ? lowered : 0;
                    Building walls = b.BandWalls != null ? b.BandWalls[k] : null;
                    if (walls != null && walls.Site != null)
                    {
                        if (cut != 0 && !walls.Measured && walls.WallsMesh != null && walls.WallsMesh.vertexCount > 0) walls.Measured = true;
                        if (cut != 0 && walls.Measured && !OverlayReady(walls) && !walls.OverlayFailed) MakeOverlayNow(walls);
                        if (walls.Applied != cut || force)
                        {
                            walls.Applied = cut;
                            ShowWalls(walls);
                        }
                        if (cut != 0 && walls.WallState == WallsStandIn) continue;
                    }
                    bool lifted = shown != int.MaxValue && (k == 0 ? shown < 1 : k > shown || cut != 0);
                    r.shadowCastingMode = lifted ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
                }

            // Each level's floors and stairs (unseen, casting their shadows), its stairwell's walls and its rooms (not drawn).
            for (int level = L.Lowest; level <= L.Top; level++)
            {
                bool hide = level > shown;
                int i = level - L.Lowest;
                Renderer s = b.Structure != null && i < b.Structure.Length ? b.Structure[i] : null;
                if (s != null) s.shadowCastingMode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
                Renderer w = b.Stairwells != null && i < b.Stairwells.Length ? b.Stairwells[i] : null;
                if (w != null) w.forceRenderingOff = hide;
            }
            HideInteriors(b.Id, shown);
            ShowWindows(b);
        }

        private static void HideInteriors(long id, int shown)
        {
            if (!s_Interiors.TryGetValue(id, out List<LevelInterior> list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                LevelInterior li = list[i];
                if (li.Root == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
                bool hide = li.Level > shown;
                // Shown and staying so: nothing to do. Hidden: made sure of every time (a piece the walls' cutaway hid and
                // put back would show).
                if (!hide && !li.Hidden) continue;
                li.Hidden = hide;
                List<Renderer> rs = li.Renderers;
                for (int k = 0; k < rs.Count; k++)
                    if (rs[k] != null) rs[k].forceRenderingOff = hide;
            }
            if (list.Count == 0) s_Interiors.Remove(id);
        }

        /// <summary>A furnished level of a building with levels (its builder calls this once it's finished): its renderers,
        /// under <paramref name="root"/> and <paramref name="overhead"/>, go with the level while it's lifted (but those under
        /// <paramref name="windows"/> and <paramref name="yard"/>: its working windows, and its front door's fittings outside,
        /// the plate of which goes with its wall, MapCutaway.Windows.cs).</summary>
        internal static void AddInterior(long id, int level, GameObject root, Transform overhead, Transform windows = null, Transform yard = null)
        {
            if (root == null) return;
            var li = new LevelInterior { Level = level, Root = root };
            root.GetComponentsInChildren(true, li.Renderers);
            if (windows != null || yard != null)
                for (int i = li.Renderers.Count - 1; i >= 0; i--)
                {
                    Transform t = li.Renderers[i] != null ? li.Renderers[i].transform : null;
                    if (t != null && ((windows != null && t.IsChildOf(windows)) || (yard != null && t.IsChildOf(yard)))) li.Renderers.RemoveAt(i);
                }
            if (overhead != null)
            {
                overhead.GetComponentsInChildren(true, s_LevelScratch);
                li.Renderers.AddRange(s_LevelScratch);
                s_LevelScratch.Clear();
            }
            if (!s_Interiors.TryGetValue(id, out List<LevelInterior> list)) s_Interiors[id] = list = new List<LevelInterior>(2);
            list.Add(li);
            if (!s_Leveled.TryGetValue(id, out Building b) || level <= b.Shown) return;
            li.Hidden = true;
            foreach (Renderer r in li.Renderers)
                if (r != null) r.forceRenderingOff = true;
        }

        /// <summary>A furnished level taken down (its builder's <see cref="InteriorBuild.Discard"/>).</summary>
        internal static void RemoveInterior(long id, GameObject root)
        {
            if (!s_Interiors.TryGetValue(id, out List<LevelInterior> list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].Root == null || ReferenceEquals(list[i].Root, root)) list.RemoveAt(i);
            if (list.Count == 0) s_Interiors.Remove(id);
        }

        /// <summary>Renderers made in a furnished level after it was registered (a hole battered through one of its walls, the
        /// boards nailed across it): from now on they go with the level as the rest of it does.</summary>
        internal static void InteriorPartsAdded(long id, GameObject root, List<Renderer> added)
        {
            if (added == null || added.Count == 0 || !s_Interiors.TryGetValue(id, out List<LevelInterior> list)) return;
            foreach (LevelInterior li in list)
            {
                if (!ReferenceEquals(li.Root, root)) continue;
                li.Renderers.RemoveAll(r => r == null);
                foreach (Renderer r in added)
                {
                    if (r == null || li.Renderers.Contains(r)) continue;
                    li.Renderers.Add(r);
                    if (li.Hidden) r.forceRenderingOff = true;
                }
                return;
            }
        }

        /// <summary>For the harness: the level building <paramref name="id"/> is cut over now (<see cref="int.MaxValue"/>
        /// when nothing of it is; null when it has no levels registered).</summary>
        internal static int? ShownLevel(long id) => s_Leveled.TryGetValue(id, out Building b) ? b.Shown : (int?)null;
    }
}
