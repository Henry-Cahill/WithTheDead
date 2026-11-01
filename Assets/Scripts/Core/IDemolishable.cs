using UnityEngine;

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

    /// <summary>
    /// Something the build tool's Demolish takes apart only where it's aimed (a stretch of a building's own wall, which is
    /// broken through rather than taken down whole): told where the cursor meets it, it decides what that spot would take
    /// (<see cref="IDemolishable.CanDemolish"/> says whether it can, <see cref="IDemolishable.Dismantle"/> takes it) and
    /// shows it with <see cref="AimBox"/>.
    /// </summary>
    public interface IDemolishAim
    {
        /// <summary>The point (world) the cursor meets it at.</summary>
        void AimAt(Vector3 point);

        /// <summary>The box (world centre, turn and size) the aimed spot would take out; false when it can't be taken.</summary>
        bool AimBox(out Vector3 centre, out Quaternion rotation, out Vector3 size);
    }
}
