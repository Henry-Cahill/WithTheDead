using UnityEngine;

namespace CloneZ
{
    /// <summary>Small shared helpers for the home's lights, switches, wall decor and yard fixtures.</summary>
    internal static class LightingFixtures
    {
        /// <summary>True when the survivor stands on the side of a wall a fixture faces — in front of the plane through
        /// <paramref name="face"/> with the outward <paramref name="normal"/> (or when there's no survivor). Belt and
        /// braces with the interactor's sight line, so a switch on a thin panel can't be worked from the next room.</summary>
        public static bool SurvivorInFront(Vector3 face, Vector3 normal)
        {
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null) return true;
            Vector3 d = pi.transform.position - face;
            d.y = 0f;
            return Vector3.Dot(d, normal) > 0f;
        }

        /// <summary>"Nothing happens — the power's out" (or the breaker's off), or null when the house has power.</summary>
        public static string NothingHappens()
        {
            string reason = PowerGrid.NoPowerReason;
            return reason == null ? null : "Nothing happens \u2014 " + char.ToLowerInvariant(reason[0]) + reason.Substring(1);
        }

        /// <summary>As <see cref="NothingHappens()"/>, where <paramref name="at"/> is (its building's own breaker counts too).</summary>
        public static string NothingHappens(Component at)
        {
            string reason = PowerGrid.NoPowerReasonAt(at);
            return reason == null ? null : "Nothing happens \u2014 " + char.ToLowerInvariant(reason[0]) + reason.Substring(1);
        }

        /// <summary>Eases the survivor's panic by <paramref name="amount"/> (0..100 scale).</summary>
        public static void Calm(InteractContext ctx, float amount)
        {
            if (ctx.Stats != null) ctx.Stats.Panic = Mathf.Max(0f, ctx.Stats.Panic - amount);
        }
    }
}
