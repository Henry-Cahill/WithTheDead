using System;

namespace CloneZ
{
    // What's thrown to make a din (Weapons/NoiseLure.cs): a wind-up alarm clock's bells, a firecracker's bang and a transistor
    // radio playing to itself. The loops are periodic by construction (strikes and notes wrapped round the end, filters run
    // round the seam), so none of them needs a cross-fade.
    public sealed partial class SfxSynth
    {
        partial void RegisterThrowables()
        {
            // A ringing alarm clock carries down the street (a lure: NoiseLure); two looks, so two clocks don't ring as one.
            Add(SfxIds.AlarmBell, g => ThrowKit.AlarmBell(g), 2).Loop().Peak(0.8f).Range(60f).Reverb(0.6f).Priority(40);
            // A firecracker: a sharp crack and the street slapping it back. Heard as far as a pistol shot.
            Add(SfxIds.Firecracker, g => ThrowKit.Firecracker(g), 6).Peak(0.9f).Range(120f).Reverb(0.5f).Priority(45);
            // A transistor radio blaring a tune through its little speaker.
            Add(SfxIds.RadioPlay, g => ThrowKit.RadioSong(g), 2, BedRate).Loop().Peak(0.6f).Range(45f).Reverb(0.7f).Priority(60);
        }

        private static class ThrowKit
        {
            /// <summary>
            /// A twin-bell alarm clock ringing: the hammer flicking between two thin steel cups eighteen times a second, each
            /// strike a tick and an inharmonic ring cut short by the next, the two bells a little apart in pitch, and the tin
            /// case and the movement buzzing along with them.
            /// </summary>
            internal static float[] AlarmBell(SynthGen g)
            {
                g = WeaponKit.Fresh(g);
                int rate = g.Rate;
                const float loop = 1.2f;
                const int strikes = 22;
                float[] x = g.Buffer(loop);
                float fa = g.Range(2200f, 2450f), fb = fa * g.Range(1.07f, 1.13f);
                float[] ratios = { 1f, 2.76f, 5.4f, 8.93f };
                for (int s = 0; s < strikes; s++)
                {
                    float gain = g.Range(0.75f, 1f);
                    var strike = new float[g.N(0.45f)];
                    Dsp.Ring(strike, rate, 0f, gain, ((s & 1) == 0 ? fa : fb) * g.Range(0.998f, 1.002f), ratios, g.Range(0.09f, 0.14f), 0.6f, 0.55f);
                    WeaponKit.Noise(strike, g, 0f, gain * 0.3f, 0.00005f, 0.0008f, 0f, 3500f);
                    float at = s * loop / strikes + g.Range(-0.0015f, 0.0015f);
                    Dsp.MixWrap(x, strike, (int)(at * rate), 1f);
                }
                var buzz = new float[x.Length];
                for (int i = 0; i < buzz.Length; i++) buzz[i] = g.Bipolar();
                Dsp.LoopBP(buzz, g.Range(850f, 1100f), 2.5f, rate);
                Dsp.Norm(buzz);
                Dsp.Shape(buzz, rate, t => 0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * strikes / loop * t));
                Dsp.Mix(x, buzz, 0, WeaponKit.Db(-18f));
                Dsp.LoopHP(x, 300f, rate);
                return x;
            }

            /// <summary>One firecracker: the paper tube bursting (a hard crack and a spit of noise), a little body, and the
            /// street slapping it back.</summary>
            internal static float[] Firecracker(SynthGen g)
            {
                g = WeaponKit.Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.8f);
                const float t0 = WeaponKit.T0;
                WeaponKit.NWave(x, rate, t0, 1f, g.Range(0.0003f, 0.0006f));
                WeaponKit.Noise(x, g, t0, g.Range(0.6f, 0.9f), 0.0002f, g.Range(0.006f, 0.012f), 9000f, 400f);
                WeaponKit.Noise(x, g, t0 + 0.002f, 0.35f, 0.001f, g.Range(0.02f, 0.04f), 2500f, 150f);
                Dsp.Thump(x, rate, t0, 0.3f, g.Range(180f, 240f), 90f, 0.01f, 0.0005f, 0.025f);
                int slaps = g.Int(2, 4);
                for (int i = 0; i < slaps; i++)
                    WeaponKit.Noise(x, g, t0 + g.Range(0.03f, 0.2f), WeaponKit.Db(g.Range(-20f, -13f)), 0.002f, g.Range(0.03f, 0.06f), 3000f, 200f);
                Dsp.HP(x, 120f, rate);
                return x;
            }

            /// <summary>
            /// A transistor radio left playing: a bright little tune — a walking bass, organ stabs on the off-beats, a
            /// pentatonic lead and a drum machine, eight seconds round — through a three-inch speaker (no lows, no highs,
            /// pushed into distortion), with the station's hiss and crackle under it and its reception swelling and sagging.
            /// </summary>
            internal static float[] RadioSong(SynthGen g)
            {
                g = WeaponKit.Fresh(g);
                int rate = g.Rate;
                const float beat = 0.5f;
                const int bars = 4;
                float loop = bars * 4 * beat;
                float[] x = g.Buffer(loop);
                int key = 52 + g.Int(0, 8);
                int[] roots = { 0, 5, 7, 0 };
                int[] scale = { 0, 2, 4, 7, 9, 12, 14, 16 };
                float[] bass = { 1f, 0.5f, 0.25f, 0.12f }, organ = { 1f, 0.6f, 0.45f, 0.3f, 0.2f }, lead = { 1f, 0f, 0.4f, 0f, 0.2f };
                for (int bar = 0; bar < bars; bar++)
                {
                    float b0 = bar * 4 * beat;
                    int root = key + roots[bar];
                    for (int b = 0; b < 4; b++)
                    {
                        Note(x, rate, b0 + b * beat, Dsp.Midi(root - 12 + ((b & 1) == 1 ? 7 : 0)), beat * 0.9f, 0.55f, bass, 0.25f);
                        foreach (int n in new[] { 0, 4, 7 })
                            Note(x, rate, b0 + (b + 0.5f) * beat, Dsp.Midi(root + n), beat * 0.35f, 0.14f, organ, 0.4f);
                        Kick(x, g, b0 + b * beat, (b & 1) == 0);
                    }
                    for (int e = 0; e < 8; e++)
                    {
                        if (g.Chance(0.25f)) continue;
                        Note(x, rate, b0 + e * beat * 0.5f, Dsp.Midi(key + 12 + scale[g.Int(0, scale.Length)]), beat * 0.45f, 0.3f, lead, 0.3f);
                    }
                }
                // The little speaker: no lows, no highs, driven hard.
                Dsp.LoopHP(x, 380f, rate);
                Dsp.LoopHP(x, 380f, rate);
                Dsp.LoopLP(x, 3200f, rate);
                Dsp.LoopLP(x, 3200f, rate);
                Dsp.LoopPeakEq(x, 1200f, 1.2f, 5f, rate);
                Dsp.Drive(x, 2.2f);
                // The reception swelling and sagging, twice round, and the station's hiss and crackle under it.
                Dsp.Shape(x, rate, t => 0.82f + 0.18f * MathF.Sin(Dsp.TwoPi * 2f * t / loop));
                var hiss = new float[x.Length];
                for (int i = 0; i < hiss.Length; i++) hiss[i] = g.Bipolar();
                Dsp.LoopBP(hiss, 2400f, 0.8f, rate);
                Dsp.Norm(hiss);
                Dsp.Mix(x, hiss, 0, WeaponKit.Db(-24f));
                Dsp.GrainsWrap(x, g, 40, 0.25f, 0.0006f);
                return x;
            }

            // A note of a few harmonics (`amps`, from the fundamental) under a quick attack and a decay of `tau` seconds, cut
            // off at `len` — wrapped round the loop's end.
            private static void Note(float[] loop, int rate, float at, float hz, float len, float gain, float[] amps, float tau)
            {
                int n = (int)((len + 0.02f) * rate);
                var b = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    float env = Math.Min(1f, t / 0.005f) * MathF.Exp(-t / tau) * (t < len ? 1f : Math.Max(0f, 1f - (t - len) / 0.02f));
                    float s = 0f;
                    for (int h = 0; h < amps.Length; h++)
                        if (amps[h] != 0f) s += amps[h] * MathF.Sin(Dsp.TwoPi * hz * (h + 1) * t);
                    b[i] = s * env * gain;
                }
                Dsp.MixWrap(loop, b, (int)(at * rate), 1f);
            }

            // The drum machine: a kick on the downbeat, a snare on the backbeat, a hat on every one.
            private static void Kick(float[] loop, SynthGen g, float at, bool down)
            {
                int rate = g.Rate;
                var b = new float[g.N(0.3f)];
                if (down) Dsp.Thump(b, rate, 0f, 0.8f, 150f, 55f, 0.03f, 0.002f, 0.08f);
                else WeaponKit.Noise(b, g, 0f, 0.45f, 0.001f, 0.06f, 0f, 0f, 1900f, 0.9f);
                WeaponKit.Noise(b, g, 0.25f, 0.12f, 0.0005f, 0.015f, 0f, 6000f);
                Dsp.MixWrap(loop, b, (int)(at * rate), 1f);
            }
        }
    }

    public static partial class SfxIds
    {
        /// <summary>The noise lures (<see cref="NoiseLure"/>): an alarm clock ringing, a firecracker going off, a radio playing.</summary>
        public const string AlarmBell = "alarm_bell", Firecracker = "firecracker", RadioPlay = "radio_play";
    }
}
