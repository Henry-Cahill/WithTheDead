using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Lightweight world sound system. Actions that make noise (footsteps,
    /// weapon swings, gunshots) emit a pulse; zombies poll for the loudest
    /// nearby pulse to investigate. Mirrors Project Zomboid's "soundAttract"
    /// mechanic where sound draws the horde.
    /// </summary>
    public static class NoiseSystem
    {
        private struct Pulse
        {
            public Vector3 Position;
            public float Radius;
            public float Time;
        }

        private const float PulseLifetime = 0.4f;
        private static readonly List<Pulse> s_Pulses = new List<Pulse>(64);

        public static void Emit(Vector3 position, float radius)
        {
            if (radius <= 0f) return;
            s_Pulses.Add(new Pulse { Position = position, Radius = radius, Time = Time.time });
        }

        /// <summary>
        /// Finds the most attractive active noise a listener can hear.
        /// Strength scales with the noise's loudness and how close it is.
        /// </summary>
        public static bool TryHear(Vector3 listener, float hearingRadius, out Vector3 sourcePos, out float strength)
        {
            sourcePos = default;
            strength = 0f;
            float now = Time.time;

            for (int i = s_Pulses.Count - 1; i >= 0; i--)
            {
                Pulse p = s_Pulses[i];
                if (now - p.Time > PulseLifetime)
                {
                    s_Pulses.RemoveAt(i);
                    continue;
                }

                float reach = p.Radius + hearingRadius;
                float dist = Vector3.Distance(listener, p.Position);
                if (dist > reach) continue;

                float s = (reach - dist) / reach * p.Radius;
                if (s > strength)
                {
                    strength = s;
                    sourcePos = p.Position;
                }
            }

            return strength > 0f;
        }

        public static void Clear() => s_Pulses.Clear();
    }
}
