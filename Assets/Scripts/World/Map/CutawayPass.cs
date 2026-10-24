using System.Collections.Generic;

namespace CloneZ
{
    /// <summary>
    /// Emits the per-building shells (<see cref="KentuckyMap.BuildingShell"/>) of a Full site as objects the isometric
    /// cutaway can hide one building at a time (<see cref="MapCutaway"/>). Runs after the buildings and their
    /// insides. Shared (batched) shells are emitted with the site's batches instead.
    /// </summary>
    internal static class CutawayPass
    {
        internal static IEnumerable<string> Build(KentuckyMap.Site site)
        {
            yield break;
        }
    }
}
