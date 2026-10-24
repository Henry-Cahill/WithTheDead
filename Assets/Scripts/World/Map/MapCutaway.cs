using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The Kentucky map's isometric cutaway, Project Zomboid-style: roofs and upper storeys show from outside, and the
    /// building the survivor is in, and whatever stands between the camera and them, is cut away (see
    /// <see cref="KentuckyMap.BuildingShell"/>). For now every shell hands out the site's shared batches, so roofs
    /// show and nothing is cut; per-building shells on Full sites and the runtime that cuts them come next.
    /// </summary>
    internal static class MapCutaway
    {
        /// <summary>A building's shell on <paramref name="site"/> (see <see cref="KentuckyMap.Site.Shell"/>).</summary>
        internal static KentuckyMap.BuildingShell ShellFor(KentuckyMap.Site site, KentuckyMap.Footprint footprint, bool scenery)
        {
            var shell = new KentuckyMap.BuildingShell { Footprint = footprint, Batched = true };
            Vector3 middle = KentuckyMap.To3(footprint.Middle, 0f);
            RaidMesh walls = scenery ? site.Town.Far.Solid(middle) : site.Town.Out.Solid(middle);
            for (int i = 0; i < shell.Walls.Length; i++) shell.Walls[i] = walls;
            shell.Upper = site.Roofs.At(middle);
            site.Shells.Add(shell);
            return shell;
        }
    }
}
