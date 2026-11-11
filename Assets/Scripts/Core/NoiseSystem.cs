using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Lightweight world sound system. Actions that make noise (footsteps,
    /// weapon swings, gunshots) emit a pulse; zombies poll for the loudest
    /// nearby pulse to investigate. Mirrors Project Zomboid's "soundAttract"
    /// mechanic where sound draws the horde. A lure's pulses (an alarm clock
    /// ringing, firecrackers, a radio left playing: <see cref="EmitLure"/>)
    /// are heard like any other, and can also pull one of the dead off a chase
    /// it has lost sight of (<see cref="TryHearLure"/>).
    /// </summary>
    public static class NoiseSystem
    {
        private struct Pulse
        {
            public Vector3 Position;
            public float Radius;
            public float Time;
            public bool Lure;
        }

        private const float PulseLifetime = 0.4f;
        private static readonly List<Pulse> s_Pulses = new List<Pulse>(64);

        public static void Emit(Vector3 position, float radius) => Add(position, radius, false);

        /// <summary>A lure's call: heard as any noise is, and loud enough to carry one of the dead off a chase it has
        /// lost sight of its prey in (<see cref="TryHearLure"/>).</summary>
        public static void EmitLure(Vector3 position, float radius) => Add(position, radius, true);

        private static void Add(Vector3 position, float radius, bool lure)
        {
            if (radius <= 0f) return;
            s_Pulses.Add(new Pulse { Position = position, Radius = radius, Time = Time.time, Lure = lure });
        }

        /// <summary>
        /// Finds the most attractive active noise a listener can hear.
        /// Strength scales with the noise's loudness and how close it is.
        /// </summary>
        public static bool TryHear(Vector3 listener, float hearingRadius, out Vector3 sourcePos, out float strength)
            => Hear(listener, hearingRadius, false, out sourcePos, out strength);

        /// <summary>The most attractive lure (<see cref="EmitLure"/>) a listener can hear, ignoring every other noise.</summary>
        public static bool TryHearLure(Vector3 listener, float hearingRadius, out Vector3 sourcePos)
            => Hear(listener, hearingRadius, true, out sourcePos, out _);

        private static bool Hear(Vector3 listener, float hearingRadius, bool luresOnly, out Vector3 sourcePos, out float strength)
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
                if (luresOnly && !p.Lure) continue;

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
