using UnityEngine;

namespace CloneZ
{
    /// <summary>The sounds of the fight that no single weapon owns: a bullet striking a body.</summary>
    public static class WeaponAudio
    {
        private static bool s_Hooked;

        /// <summary>Hooks the combat events (once per run of the game).</summary>
        public static void Ensure()
        {
            if (s_Hooked) return;
            s_Hooked = true;
            CombatResolver.ShotResolved += (req, r) =>
            {
                if (r.Hit && (r.TargetActor != null || r.Target != null))
                    Sfx.Play(SfxIds.HitFlesh, r.End, 0.6f, Random.Range(0.9f, 1.1f), 18f);
            };
        }
    }
}
