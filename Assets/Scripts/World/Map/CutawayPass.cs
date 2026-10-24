using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// Emits the per-building shells (<see cref="BuildingShell"/>) of a Full site as objects the isometric cutaway can
    /// hide one building at a time (<see cref="MapCutaway"/>): for each building its ground-storey walls under the site's
    /// world root (a NavMesh source like every wall) and everything above them under its overhead root (never baked),
    /// both standing on the footprint's middle; then the site's list of them, for the runtime, on a
    /// <see cref="MapCutawaySite"/> on the walls' parent. Runs after the buildings and their insides (the interiors line
    /// the outside walls from inside, into the shells); the shells point at the shared batches from then on. Shared
    /// (batched) shells are emitted with the site's batches instead.
    /// <para>
    /// Light on the main thread: two objects a building, their meshes queued on the site's bake (<see cref="Deferred"/>)
    /// or baked at once; nothing is read back from them here (what the runtime needs from the meshes, their true heights,
    /// it reads a slice at a time once they're baked). A streamed piece's steps end every couple of milliseconds
    /// (<see cref="Site.StepDue"/>).
    /// </para>
    /// </summary>
    internal static class CutawayPass
    {
        private const float DefaultWallThickness = 0.22f;
        /// <summary>A shell whose walls stop lower than this has no walls worth cutting (a canopy's posts).</summary>
        private const float MinWalls = 0.05f;

        /// <summary>Queue the shells' meshes on the site's bake (<see cref="Site.Bake"/>: a streamed piece packs it on a worker
        /// thread with the piece's other meshes, the fixed square bakes it with its outdoor meshes) rather than bake each
        /// here, on the main thread. Off only for the harness's comparison.</summary>
        internal static bool Deferred = true;

        /// <summary>Running totals over the process (the harness reads them around a build): buildings emitted and the time
        /// that took, in stopwatch ticks.</summary>
        internal static long Emitted, EmitTicks;

        internal static IEnumerable<string> Build(Site site)
        {
            List<BuildingShell> shells = site.Shells;
            int own = 0;
            for (int i = 0; i < shells.Count; i++)
                if (!shells[i].Batched) own++;
            if (own == 0) yield break;
            // On a streamed piece the interiors' plans and linings end without a step of their own: close it here, so the
            // harness and the streamer's longest-step tally put that time where it belongs.
            yield return "interior plans";
            site.BeginStep();
            var thickness = new Dictionary<Footprint, float>();
            foreach (EnterableBuilding e in site.Enterables)
                if (e.Footprint != null && e.WallThickness > 0.01f) thickness[e.Footprint] = e.WallThickness;
            DecorPalette.Matte(MapCutaway.CapColour);   // the cut walls' cap colour joins the palette with the build's
            Transform walls = Container("Building shells", site.Town.World), upper = Container("Building shells", site.Town.Overhead);
            var buildings = new List<MapCutaway.Building>(own);
            for (int i = 0; i < shells.Count; i++)
            {
                BuildingShell shell = shells[i];
                if (shell.Batched) continue;
                long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                MapCutaway.Building b = Emit(site, shell, walls, upper, thickness);
                MapCutaway.Retire(site, shell);
                if (b != null) buildings.Add(b);
                Emitted++;
                EmitTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
                if (!site.StepDue) continue;
                yield return "cutaway";
                site.BeginStep();
            }
            MapCutawaySite.Attach(walls.gameObject, upper, buildings);
            yield return "cutaway";
        }

        private static Transform Container(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        private static MapCutaway.Building Emit(Site site, BuildingShell shell, Transform wallsRoot, Transform upperRoot,
            Dictionary<Footprint, float> thickness)
        {
            Footprint f = shell.Footprint;
            Vector3 origin = To3(f.Middle, 0f);
            string id = f.Data != null ? f.Data.id.ToString(CultureInfo.InvariantCulture) : "";
            RaidMesh w = shell.Walls[0], u = shell.Upper;
            GameObject wallsGo = Place(site, w, "Walls " + id, wallsRoot, origin);
            GameObject upperGo = u != w ? Place(site, u, "Upper " + id, upperRoot, origin) : null;
            if ((wallsGo == null && upperGo == null) || f.Pts.Count < 3) return null;

            var b = new MapCutaway.Building
            {
                Id = f.Data != null ? f.Data.id : 0,
                Kind = f.Data != null ? f.Data.kind : null,
                Middle = f.Middle,
                Local = f.Pts.ToArray(),
                WallThickness = thickness.TryGetValue(f, out float t) ? t : DefaultWallThickness,
                Walls = wallsGo != null ? wallsGo.GetComponent<MeshRenderer>() : null,
                Upper = upperGo != null ? upperGo.GetComponent<MeshRenderer>() : null,
                WallsMesh = wallsGo != null ? wallsGo.GetComponent<MeshFilter>().sharedMesh : null,
                UpperMesh = upperGo != null ? upperGo.GetComponent<MeshFilter>().sharedMesh : null,
            };
            bool cuttable = wallsGo != null && shell.WallTop >= MinWalls;
            b.WallTop = cuttable ? shell.WallTop : 0f;
            b.ClipTop = b.WallTop;
            b.Top = Mathf.Max(shell.Top, b.WallTop);
            int n = b.Local.Length;
            b.EdgeGroup = new byte[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = b.Local[(i + 1) % n] - b.Local[i];
                b.EdgeGroup[i] = (byte)BuildingShell.Facing(new Vector2(d.y, -d.x));   // the footprint runs anticlockwise
                if (cuttable && d.sqrMagnitude > 0.0025f) b.Groups |= 1 << b.EdgeGroup[i];
            }
            return b;
        }

        // A shell's mesh as an object of its own: baked now (and its arrays given back to the pool), or queued on the
        // site's bake, which gives them back once it's applied.
        private static GameObject Place(Site site, RaidMesh m, string name, Transform parent, Vector3 origin)
        {
            if (m == null || m.IsEmpty) return null;
            if (Deferred) return m.EmitLater(name, parent, origin, ShadowCastingMode.On, site.Bake);
            GameObject go = m.Emit(name, parent, origin, ShadowCastingMode.On);
            m.Release();
            return go;
        }
    }
}
