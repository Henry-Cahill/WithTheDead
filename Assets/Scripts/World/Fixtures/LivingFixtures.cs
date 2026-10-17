using UnityEngine;

namespace CloneZ
{
    /// <summary>Small shared helpers for the living room's and bedroom's fixtures.</summary>
    internal static class LivingFixtures
    {
        /// <summary>Eases the survivor's panic by <paramref name="amount"/> (0..100 scale).</summary>
        public static void Calm(InteractContext ctx, float amount)
        {
            if (ctx.Stats != null && amount > 0f) ctx.Stats.Panic = Mathf.Max(0f, ctx.Stats.Panic - amount);
        }

        /// <summary>Flat (XZ) distance between two points.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>The furniture of <paramref name="kind"/> whose footprint holds <paramref name="position"/> — the
        /// dresser under the TV, the counter under the radio — or null.</summary>
        public static HomeFurniture Under(Vector3 position, HomeFurnitureKind kind)
        {
            var at = new Vector2(position.x, position.z);
            for (int i = 0; i < HomeFurniture.All.Count; i++)
            {
                HomeFurniture f = HomeFurniture.All[i];
                if (f != null && f.Kind == kind && HomeFurniture.Footprint(f.Kind, f.transform.position, f.Yaw).Contains(at)) return f;
            }
            return null;
        }

        /// <summary>"The power's out" → "the power's out" (for the middle of a sentence).</summary>
        public static string Lower(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
    }
}
