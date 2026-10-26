using System;

namespace CloneZ
{
    // The music's one-shots: stingers for the moments that matter (the raid beginning, the survivor dying, getting
    // out or not, an emission coming, something found in an anomaly). Flat, stereo, on the Music bus (mus_ ids). The
    // score itself (layers, title theme, jukebox records) is rendered by MusicDirector from MusicScore/MusicRecords.
    public sealed partial class SfxSynth
    {
        partial void RegisterMusic()
        {
            AddStereo(SfxIds.MusRaidStart, g => MusicStingers.RaidStart(g), 1, BedRate).Warm(20);
            AddStereo(SfxIds.MusDeath, g => MusicStingers.Death(g), 1, BedRate).Warm(20);
            AddStereo(SfxIds.MusExtracted, g => MusicStingers.Extracted(g), 1, BedRate).Warm(20);
            AddStereo(SfxIds.MusRaidFailed, g => MusicStingers.RaidFailed(g), 1, BedRate).Warm(20);
            AddStereo(SfxIds.MusEmission, g => MusicStingers.Emission(g), 1, BedRate).Warm(20);
            AddStereo(SfxIds.MusDiscovery, g => MusicStingers.Discovery(g), 1, BedRate).Warm(20);
        }

        private static class MusicStingers
        {
            private static float Hz(float midi) => Dsp.Midi(midi);

            private static int At(SynthGen g, float seconds) => (int)(seconds * g.Rate);

            // Evened out, a room round the stinger, then a long fade over its last stretch so it dies away rather than stopping.
            private static StereoBuf Close(StereoBuf s, SynthGen g, float rt60, float wet, float fade)
            {
                float k = 1f / Math.Max(1e-6f, Math.Max(Dsp.Peak(s.L), Dsp.Peak(s.R)));
                Dsp.Gain(s.L, k);
                Dsp.Gain(s.R, k);
                MusicKit.Compress(s, g.Rate, -16f, 2.2f, 6f, 300f);
                MusicKit.Space(s, g.Rate, rt60, 4000f, wet, 0.14f, 0.02f);
                int n = Math.Min(s.Length, At(g, fade));
                for (int i = 0; i < n; i++)
                {
                    float w = i / (float)n;
                    int j = s.Length - 1 - i;
                    s.L[j] *= w * w;
                    s.R[j] *= w * w;
                }
                return s;
            }

            /// <summary>Stepping out into the Zone: a deep drum, a low cello swelling, a banjo climbing D–A–D–E–F and
            /// leaving the minor third hanging, a high harmonic on a fiddle far off.</summary>
            internal static StereoBuf RaidStart(SynthGen g)
            {
                int rate = g.Rate;
                var s = new StereoBuf(At(g, 7.5f));
                var low = new float[s.Length];
                MusicKit.Drum(low, 0, rate, 0.45f, 105f, 44f, 0.05f, 0.75f, 0.35f, 220f, 1f, 0.08f, 0.12f, g);
                MusicKit.Bow(low, At(g, 0.02f), rate, Hz(38), 0.15f, 5.4f, MusicKit.Cello, g);
                Dsp.LP(low, 900f, rate);
                Dsp.MixPan(s, low, 0, 1f, 0f);

                var banjo = new float[s.Length];
                int[] figure = { 50, 57, 62, 64, 65 };
                for (int i = 0; i < figure.Length; i++)
                    MusicKit.Pluck(banjo, At(g, 0.03f + i * 0.24f), rate, Hz(figure[i]), 0.6f * (i == figure.Length - 1 ? 1.1f : 0.9f), MusicKit.Banjo, g);
                MusicKit.Pluck(banjo, At(g, 2.2f), rate, Hz(62), 0.42f, MusicKit.Banjo, g);
                Dsp.HP(banjo, 120f, rate);
                Dsp.PeakEq(banjo, 650f, 1.8f, 4f, rate);
                Dsp.MixPan(s, banjo, 0, 1f, -0.25f);

                var fid = new float[s.Length];
                MusicKit.Bow(fid, At(g, 1.1f), rate, Hz(81), 0.07f, 4.6f, MusicKit.FiddleSwell, g);
                Dsp.HP(fid, 300f, rate);
                Dsp.MixPan(s, fid, 0, 1f, 0.4f);
                return Close(s, g, 3.2f, 0.42f, 1.6f);
            }

            /// <summary>The survivor is gone: a low blow and a string cluster sinking, a dulcimer falling A–F–E–D and left
            /// ringing, a single voice under it.</summary>
            internal static StereoBuf Death(SynthGen g)
            {
                int rate = g.Rate;
                var s = new StereoBuf(At(g, 7.8f));
                var low = new float[s.Length];
                MusicKit.Taiko(low, 0, rate, 0.42f, 1.25f, g);
                MusicKit.Pluck(low, 0, rate, Hz(38), 0.4f, MusicKit.Bass, g);
                var sink = new BowTone(1.2f, 2.2f, 0.0028f, 5.1f, 0.06f, 2400f, 2, 13f);
                foreach (int m in new[] { 38, 39, 45 })
                    MusicKit.Bow(low, At(g, 0.03f), rate, Hz(m), 0.1f, 5f, sink, g, 0f, 0.1f, Hz(m) * 0.972f);
                Dsp.LP(low, 1400f, rate);
                Dsp.MixPan(s, low, 0, 1f, 0f);

                var line = new float[s.Length];
                int[] fall = { 69, 65, 64, 62 };
                float[] when = { 0.7f, 1.35f, 2f, 2.9f };
                for (int i = 0; i < fall.Length; i++) MusicKit.Pluck(line, At(g, when[i]), rate, Hz(fall[i]), 0.55f, MusicKit.Dulcimer, g);
                MusicKit.Harmonic(line, At(g, 3.6f), rate, Hz(74), 0.11f, 2.4f, g);
                Dsp.HP(line, 90f, rate);
                Dsp.PeakEq(line, 1900f, 2f, 2f, rate);
                Dsp.MixPan(s, line, 0, 1f, -0.2f);

                var voice = new float[s.Length];
                MusicKit.Choir(voice, At(g, 0.9f), rate, Hz(50), 0.16f, 5.6f, g, -20f);
                Dsp.HP(voice, 110f, rate);
                Dsp.MixPan(s, voice, 0, 1f, 0.3f);
                return Close(s, g, 4f, 0.5f, 2.2f);
            }

            /// <summary>Out: the one bright chord in the score, D major picked slowly over a warm cello fifth, a fiddle
            /// singing over it, a harmonic at the end.</summary>
            internal static StereoBuf Extracted(SynthGen g)
            {
                int rate = g.Rate;
                var s = new StereoBuf(At(g, 7.5f));
                var gtr = new float[s.Length];
                int[] chord = { 38, 50, 57, 62, 66, 69 };
                for (int i = 0; i < chord.Length; i++)
                    MusicKit.Pluck(gtr, At(g, i * 0.13f), rate, Hz(chord[i]), 0.42f * (1f - 0.05f * i), MusicKit.Guitar, g);
                MusicKit.Pluck(gtr, At(g, 1.6f), rate, Hz(74), 0.3f, MusicKit.Guitar, g);
                MusicKit.Harmonic(gtr, At(g, 2.6f), rate, Hz(86), 0.07f, 2.6f, g);
                Dsp.HP(gtr, 65f, rate);
                Dsp.PeakEq(gtr, 105f, 1.3f, 3f, rate);
                Dsp.LP(gtr, 6500f, rate);
                Dsp.MixPan(s, gtr, 0, 1f, -0.15f);

                var str = new float[s.Length];
                var cello = new BowTone(1.3f, 1.7f, 0.0032f, 4.8f, 0.05f, 1500f);
                MusicKit.Bow(str, At(g, 0.05f), rate, Hz(38), 0.15f, 5.2f, cello, g);
                MusicKit.Bow(str, At(g, 0.05f), rate, Hz(45), 0.1f, 5.2f, cello, g);
                MusicKit.Bow(str, At(g, 0.9f), rate, Hz(78), 0.09f, 1.6f, MusicKit.FiddleSwell, g);
                MusicKit.Bow(str, At(g, 2.3f), rate, Hz(74), 0.09f, 3.4f, MusicKit.FiddleSwell, g, Hz(78), 0.18f);
                Dsp.HP(str, 60f, rate);
                Dsp.PeakEq(str, 2700f, 1.1f, 2f, rate);
                Dsp.MixPan(s, str, 0, 1f, 0.3f);
                return Close(s, g, 3f, 0.4f, 1.8f);
            }

            /// <summary>Lost in the Zone (out of time, or walked away): a muffled blow, the bass stepping down D–C–Bb–A
            /// with a cello, ending on the open A with nothing to resolve it.</summary>
            internal static StereoBuf RaidFailed(SynthGen g)
            {
                int rate = g.Rate;
                var s = new StereoBuf(At(g, 7f));
                var low = new float[s.Length];
                MusicKit.Taiko(low, 0, rate, 0.35f, 1.15f, g);
                int[] steps = { 50, 48, 46, 45 };
                for (int i = 0; i < steps.Length; i++)
                {
                    int at = At(g, 0.02f + i * 0.7f);
                    MusicKit.Pluck(low, at, rate, Hz(steps[i]), 0.52f, MusicKit.Guitar, g, 0.75f, 0.3f);
                    MusicKit.Bow(low, at, rate, Hz(steps[i] - 12), 0.11f, i == steps.Length - 1 ? 3.6f : 0.85f, MusicKit.Cello, g);
                }
                Dsp.HP(low, 40f, rate);
                Dsp.LP(low, 2500f, rate);
                Dsp.MixPan(s, low, 0, 1f, 0f);
                var hi = new float[s.Length];
                MusicKit.Harmonic(hi, At(g, 2.2f), rate, Hz(76), 0.06f, 2.4f, g);
                MusicKit.Bow(hi, At(g, 2.1f), rate, Hz(64), 0.05f, 3.4f, MusicKit.FiddleSwell, g);
                Dsp.MixPan(s, hi, 0, 1f, 0.35f);
                return Close(s, g, 3.4f, 0.45f, 1.8f);
            }

            /// <summary>An emission coming: a deep blow and a string cluster struck hard near the bridge, falling back and
            /// swelling again, a choir under it and a thin whine climbing.</summary>
            internal static StereoBuf Emission(SynthGen g)
            {
                int rate = g.Rate;
                var s = new StereoBuf(At(g, 6.8f));
                var low = new float[s.Length];
                MusicKit.Boom(low, 0, rate, 0.5f, g);
                MusicKit.Taiko(low, 0, rate, 0.35f, 1.1f, g);
                Dsp.LP(low, 600f, rate);
                Dsp.MixPan(s, low, 0, 1f, 0f);

                var left = new float[s.Length];
                var right = new float[s.Length];
                var stab = new BowTone(0.03f, 1.2f, 0.002f, 5f, 0.14f, 6000f, 1, 0f, 0.55f, 11f);
                var held = new BowTone(1.6f, 1.8f, 0.0015f, 5f, 0.13f, 6200f, 2, 16f, 0.6f, 10.5f);
                int[] cluster = { 62, 63, 68 };
                for (int i = 0; i < cluster.Length; i++)
                {
                    int m = cluster[i];
                    float[] side = (i & 1) == 0 ? left : right;
                    MusicKit.Bow(side, At(g, 0.01f), rate, Hz(m), 0.1f, 1.2f, stab, g);
                    MusicKit.Bow(side, At(g, 1.1f), rate, Hz(m), 0.06f, 3.9f, held, g, 0f, 0.1f, Hz(m) * 1.03f);
                }
                foreach (float[] side in new[] { left, right })
                {
                    Dsp.HP(side, 250f, rate);
                    Dsp.PeakEq(side, 3800f, 1f, 3f, rate);
                }
                Dsp.MixPan(s, left, 0, 1f, -0.45f);
                Dsp.MixPan(s, right, 0, 1f, 0.45f);

                var voice = new float[s.Length];
                MusicKit.Choir(voice, At(g, 0.7f), rate, Hz(50), 0.12f, 5f, g, 40f);
                MusicKit.Choir(voice, At(g, 1.2f), rate, Hz(56), 0.1f, 4.6f, g, 40f);
                Dsp.HP(voice, 110f, rate);
                Dsp.MixPan(s, voice, 0, 1f, -0.25f);

                var whine = new float[s.Length];
                MusicKit.Bow(whine, At(g, 0.6f), rate, Hz(81), 0.03f, 5.2f, new BowTone(1.5f, 1.5f, 0.006f, 4.2f, 0f, 1200f), g, 0f, 0.1f, Hz(86));
                Dsp.MixPan(s, whine, 0, 1f, 0.3f);
                return Close(s, g, 4.5f, 0.5f, 1.8f);
            }

            /// <summary>Something found in an anomaly: a dulcimer climbing a bright, strange scale (D E F# G# A), a glassy
            /// fiddle harmonic held over it and a soft low bell.</summary>
            internal static StereoBuf Discovery(SynthGen g)
            {
                int rate = g.Rate;
                var s = new StereoBuf(At(g, 5.5f));
                var pl = new float[s.Length];
                int[] rise = { 74, 76, 78, 80, 81 };
                for (int i = 0; i < rise.Length; i++)
                    MusicKit.Pluck(pl, At(g, i * 0.12f), rate, Hz(rise[i]), 0.34f * (1f - 0.06f * i), MusicKit.Dulcimer, g);
                MusicKit.Harmonic(pl, 0, rate, Hz(50), 0.1f, 2.2f, g);
                Dsp.HP(pl, 80f, rate);
                Dsp.PeakEq(pl, 1900f, 2f, 2f, rate);
                Dsp.MixPan(s, pl, 0, 1f, -0.2f);

                var hi = new float[s.Length];
                MusicKit.Bow(hi, At(g, 0.45f), rate, Hz(86), 0.035f, 3.6f, new BowTone(0.9f, 1.6f, 0.004f, 5f, 0.02f, 2500f, 2, 6f), g);
                MusicKit.Bow(hi, At(g, 0.6f), rate, Hz(81), 0.03f, 3.4f, new BowTone(0.9f, 1.6f, 0.004f, 5f, 0.02f, 2500f, 2, 6f), g);
                Dsp.MixPan(s, hi, 0, 1f, 0.35f);
                return Close(s, g, 3.6f, 0.48f, 1.4f);
            }
        }
    }

    public static partial class SfxIds
    {
        /// <summary>The score's stingers (flat, on the Music bus).</summary>
        public const string MusRaidStart = "mus_raid_start", MusDeath = "mus_death", MusExtracted = "mus_extracted",
            MusRaidFailed = "mus_raid_failed", MusEmission = "mus_emission", MusDiscovery = "mus_discovery";
    }
}
