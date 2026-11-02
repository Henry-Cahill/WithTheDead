using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A building's own main breaker on the statewide map. The furnished floors of a building carry its id on their roots, and
    /// while its breaker is off (thrown at its <see cref="BreakerPanel"/>) every light and appliance in it is dead, whatever the
    /// grid does (<see cref="PowerGrid.PoweredAt"/>). The starter home's breaker is the grid's own
    /// (<see cref="PowerGrid.MainBreakerOn"/>). Which breakers are off is kept for the session, so a building taken down and
    /// furnished again is as it was left.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildingPower : MonoBehaviour
    {
        private static readonly HashSet<long> s_Off = new HashSet<long>();

        /// <summary>The building's id (its floors share it).</summary>
        public long Id;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_Off.Clear();

        /// <summary>True while building <paramref name="id"/>'s main breaker is on.</summary>
        public static bool On(long id) => !s_Off.Contains(id);

        /// <summary>Throws building <paramref name="id"/>'s main breaker; its lamps go dark (or light) at once.</summary>
        public static void Set(long id, bool on)
        {
            bool changed = on ? s_Off.Remove(id) : s_Off.Add(id);
            if (changed) HomeLamp.RefreshAll();
        }

        /// <summary>True unless <paramref name="at"/> stands in a building whose main breaker is off (nothing is looked up
        /// while every breaker is on).</summary>
        public static bool OnAt(Component at)
        {
            if (s_Off.Count == 0 || at == null) return true;
            BuildingPower b = at.GetComponentInParent<BuildingPower>(true);
            return b == null || !s_Off.Contains(b.Id);
        }

        /// <summary>The building <paramref name="at"/> stands in, or null.</summary>
        public static BuildingPower Of(Component at) => at != null ? at.GetComponentInParent<BuildingPower>(true) : null;
    }
}
