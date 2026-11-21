using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The character's level: experience from kills, extractions, found loot, quests and exploration. Levels gate
    /// trader loyalty, the flea market and the deeper zones; they survive death and reset only with a wipe.
    /// (Skills level separately, by use — see <see cref="PlayerSkills"/>.)
    /// </summary>
    public static class Progression
    {
        public const int MaxLevel = 50;

        public static event System.Action<int> LevelUp;

        /// <summary>Experience needed to go from <paramref name="level"/> to the next.</summary>
        public static float XpToNext(int level) => 1000f + (level - 1) * 650f + (level - 1) * (level - 1) * 35f;

        /// <summary>Total experience at which a level starts.</summary>
        public static float XpAtLevel(int level)
        {
            float t = 0f;
            for (int l = 1; l < level; l++) t += XpToNext(l);
            return t;
        }

        public static void AddXp(float amount, string reason = null)
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || amount <= 0f || GameSession.IsWaveSurvival) return;
            amount *= PlayerModifiers.Current.XpMult;
            c.Xp += amount;
            while (c.Level < MaxLevel && c.Xp >= XpAtLevel(c.Level + 1))
            {
                c.Level++;
                GameEvents.RaiseToast("Level " + c.Level + "!");
                LevelUp?.Invoke(c.Level);
            }
        }

        /// <summary>Progress 0..1 through the current level.</summary>
        public static float LevelProgress(CharacterProfile c)
        {
            if (c == null) return 0f;
            float start = XpAtLevel(c.Level);
            return Mathf.Clamp01((c.Xp - start) / XpToNext(c.Level));
        }
    }
}
