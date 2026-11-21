namespace CloneZ
{
    /// <summary>
    /// Anything the build tool's Demolish mode can tear down — player-built structures
    /// (barricades, house pieces) and pre-placed world objects (bed, stove, farm plots,
    /// water sources, loot containers, cover blocks). Implementers are always
    /// <see cref="UnityEngine.MonoBehaviour"/>s, so the builder reaches their transform and
    /// renderers by casting.
    /// </summary>
    public interface IDemolishable
    {
        /// <summary>A short lower-case name for the prompt ("wall", "bed", "crate"…).</summary>
        string DemolishName { get; }

        /// <summary>Whether it can be dismantled right now; <paramref name="reason"/> explains a refusal.</summary>
        bool CanDemolish(out string reason);

        /// <summary>Materials handed back on dismantle (already reduced/rounded — the final amounts).</summary>
        void GetSalvage(out int planks, out int nails);

        /// <summary>True when the object was baked into the NavMesh, so removing it needs a rebuild.</summary>
        bool CarvesNavMesh { get; }

        /// <summary>Removes the object from the world (and its static registry).</summary>
        void Dismantle();
    }
}
