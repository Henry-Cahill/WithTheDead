using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    // The working windows fitted in a building's outside walls (the interiors' RaidWindows, objects of their own) hide with
    // the wall they're in, as its doors do (MapCutaway.Doors.cs): sashes left standing in a wall cut down to its stub would
    // float. Each is registered by the interior that fitted it, with its level and the facing of its wall, and hidden (by
    // forceRenderingOff, which nothing else of a window touches) while that facing of its storey is lowered or cut whole, or
    // while its level goes with the roof. Each storey's stand-in glass, there till the floor is furnished, hides whenever
    // anything of the building is cut.
    internal static partial class MapCutaway
    {
        /// <summary>A working window fitted in an outside wall: its renderers, hidden while its wall or its level is cut.</summary>
        private sealed class FittedWindow
        {
            public int Level, Group;
            public GameObject Root;
            public readonly List<Renderer> Renderers = new List<Renderer>(10);
            public bool Hidden;
        }

        private static readonly Dictionary<long, List<FittedWindow>> s_Windows = new Dictionary<long, List<FittedWindow>>(64);
        /// <summary>The buildings registered now, by id (the windows' interiors find their building's cut by it).</summary>
        private static readonly Dictionary<long, Building> s_ById = new Dictionary<long, Building>(512);

        /// <summary>Working windows registered now (for the harness).</summary>
        internal static int WindowCount
        {
            get
            {
                int n = 0;
                foreach (List<FittedWindow> list in s_Windows.Values) n += list.Count;
                return n;
            }
        }

        private static void ResetWindowStatics()
        {
            s_Windows.Clear();
            s_ById.Clear();
        }

        /// <summary>A working window fitted in building <paramref name="id"/>'s outside wall on <paramref name="level"/>, its
        /// wall's outward face turned toward <paramref name="facing"/> (<see cref="KentuckyMap.BuildingShell.Facing"/>): its
        /// renderers under <paramref name="root"/> go with the wall when it's cut.</summary>
        internal static void AddWindow(long id, int level, int facing, GameObject root)
        {
            if (root == null) return;
            var w = new FittedWindow { Level = level, Group = facing, Root = root };
            root.GetComponentsInChildren(true, w.Renderers);
            if (!s_Windows.TryGetValue(id, out List<FittedWindow> list)) s_Windows[id] = list = new List<FittedWindow>(8);
            list.Add(w);
            if (s_ById.TryGetValue(id, out Building b)) ShowWindows(b);
        }

        /// <summary>A window's parts changed (its boards nailed on for the first time): its renderers gathered afresh and hidden
        /// as its wall is now.</summary>
        internal static void WindowPartsChanged(GameObject root)
        {
            if (root == null) return;
            foreach (List<FittedWindow> list in s_Windows.Values)
                foreach (FittedWindow w in list)
                {
                    if (!ReferenceEquals(w.Root, root)) continue;
                    w.Renderers.Clear();
                    root.GetComponentsInChildren(true, w.Renderers);
                    for (int k = 0; k < w.Renderers.Count; k++)
                        if (w.Renderers[k] != null) w.Renderers[k].forceRenderingOff = w.Hidden;
                    return;
                }
        }

        /// <summary>The windows fitted under <paramref name="interior"/> taken down with it.</summary>
        internal static void RemoveWindows(long id, GameObject interior)
        {
            if (!s_Windows.TryGetValue(id, out List<FittedWindow> list)) return;
            Transform under = interior != null ? interior.transform : null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                GameObject root = list[i].Root;
                if (root == null || (under != null && root.transform.IsChildOf(under))) list.RemoveAt(i);
            }
            if (list.Count == 0) s_Windows.Remove(id);
        }

        /// <summary>True when the ground storey of building <paramref name="id"/>'s walls facing <paramref name="facing"/> is cut
        /// away now (lowered on its stand-in, or the walls cut whole), as its windows there are hidden.</summary>
        internal static bool FacingCut(long id, int facing)
            => s_ById.TryGetValue(id, out Building b) && b.Site != null && WindowCut(b, 0, facing);

        // Each of the building's windows shown or hidden as its wall and its level are now.
        private static void ShowWindows(Building b)
        {
            if (!s_Windows.TryGetValue(b.Id, out List<FittedWindow> list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                FittedWindow w = list[i];
                if (w.Root == null)
                {
                    list.RemoveAt(i);
                    continue;
                }
                bool hide = b.Site != null && WindowCut(b, w.Level, w.Group);
                if (hide == w.Hidden) continue;
                w.Hidden = hide;
                List<Renderer> rs = w.Renderers;
                for (int k = 0; k < rs.Count; k++)
                    if (rs[k] != null) rs[k].forceRenderingOff = hide;
            }
            if (list.Count == 0) s_Windows.Remove(b.Id);
        }

        // True when a window on `level` in a wall facing `group` is cut away now: a ground-storey one with its wall lowered on
        // the stand-in or the walls cut whole; one up a storey with its floor lifted with the roof (or, without storeys of its
        // own, the upper part), or with the walls of the survivor's own floor lowered on its side.
        private static bool WindowCut(Building b, int level, int group)
        {
            int bit = 1 << group;
            if (level <= 0) return b.WallState == WallsNone || (b.WallState == WallsStandIn && (b.Applied & bit) != 0);
            if (b.Levels == null || b.Bands == null)
                return b.Upper != null && (b.Applied & UpperBit) != 0;   // a shared roof batch never goes, and its walls with it
            if (b.Shown == int.MaxValue || level < b.Shown) return false;
            if (level > b.Shown) return true;
            Building walls = level < b.BandWalls.Length ? b.BandWalls[level] : null;
            if (walls == null || walls.Site == null) return b.ShownFacing != 0;
            return walls.WallState == WallsNone || (walls.WallState == WallsStandIn && (walls.Applied & bit) != 0);
        }

        // The stand-in glass hidden while anything of the building is cut (its real windows are fitted by then, all but always).
        private static void ShowPanes(Building b)
        {
            MeshRenderer[] panes = b.Panes;
            if (panes == null) return;
            bool hide = b.Applied != 0;
            for (int k = 0; k < panes.Length; k++)
                if (panes[k] != null) panes[k].forceRenderingOff = hide;
        }
    }
}
