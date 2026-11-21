using System.Collections.Generic;
using static CloneZ.KentuckyMap;

namespace CloneZ
{
    /// <summary>
    /// The insides of the real-world map's enterable buildings: what <see cref="BuildingPass"/> lists in
    /// <see cref="Site.Enterables"/> (footprint, storeys, doors, windows), furnished. Nothing yet: the buildings are
    /// empty shells with their loot (<see cref="MapGameplay.Loot"/>).
    /// </summary>
    internal static class InteriorPass
    {
        internal static IEnumerable<string> Build(Site site)
        {
            yield break;
        }
    }
}
