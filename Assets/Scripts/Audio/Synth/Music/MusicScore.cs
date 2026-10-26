using System;

namespace CloneZ
{
    /// <summary>
    /// The layers of the score. Explore, Tension, Combat and Dread share one grid and one harmony and play together,
    /// each at the volume the danger calls for; the Theme is the title screen's piece, on a grid of its own.
    /// </summary>
    internal enum MusicPart : byte { Explore, Tension, Combat, Dread, Theme }

    /// <summary>What to render: one part of one segment of a score.</summary>
    internal struct MusicCue
    {
        /// <summary>Which layer.</summary>
        public MusicPart Part;
        /// <summary>The score: every part rendered with the same seed shares its harmony, drones and motifs.</summary>
        public uint Seed;
        /// <summary>The segment's place on the grid (segment k starts k segment-lengths after segment 0).</summary>
        public long Index;
        /// <summary>Darker colours, lower drones, sparser picking, darker modes.</summary>
        public bool Night;
        /// <summary>Explore: segments since its phrase began (the phrase's motif and instrument hold for its length).</summary>
        public int PhrasePos;
        /// <summary>Explore: the phrase ends with this segment (a cadence, and the drone lets go).</summary>
        public bool Last;
    }

    /// <summary>
    /// The score, composed and rendered in code (engine-free: the director renders it on background threads and the
    /// offline tools render it too). Post-apocalyptic Kentucky: a modal D (Dorian, Aeolian, Phrygian at night) over a
    /// bowed open-string drone, with fingerpicked guitar, banjo or mountain dulcimer and a fiddle — and under danger a
    /// heartbeat, a ticking muted string and tremolo strings near the bridge; in a fight taiko-like drums, toms, a low
    /// spiccato gallop and fiddle chops; in an emission a creeping string cluster, a wordless choir and deep booms.
    /// <para>
    /// The music is cut into segments of four bars at 80 bpm (12 s). Every part of segment k is a pure function of the
    /// score's seed and k, rendered with a ring-out tail that overlaps the next segment: notes that start in a segment
    /// finish in it, drones are phase-coherent with absolute time and hand over through complementary windows, and the
    /// room is linear, so consecutive segments played back to back add up seamlessly. Parts of the same segment share
    /// its chords (a progression drawn from the seed and the index), so any mix of layers is in harmony.
    /// </para>
    /// </summary>
    internal static class MusicScore
    {
        public const int Rate = 32000;
        /// <summary>The score's beat: 80 bpm.</summary>
        public const int Beat = 24000;
        public const int Bar = Beat * 4;
        /// <summary>Four bars: 12 s.</summary>
        public const int Segment = Bar * 4;
        /// <summary>The ring-out past the segment's end, overlapping the next one.</summary>
        public const int Tail = Rate * 3;
        /// <summary>The theme's beat: 64 bpm.</summary>
        public const int ThemeBeat = 30000;
        public const int ThemeBar = ThemeBeat * 4;
        public const int ThemeSegment = ThemeBar * 4;
        public const int ThemeTail = Rate * 4;
        /// <summary>Intro, A, A', B, A'', outro, and a breath before the next telling.</summary>
        public const int ThemeSections = 7;

        // Seconds over which a drone hands over to the next segment's (its windows sum to one).
        private const float Overlap = 2f;
        // D4: degree 0 of every scale.
        private const int Tonic = 62;

        /// <summary>Samples from one segment's start to the next's.</summary>
        public static int SegmentSamples(MusicPart part) => part == MusicPart.Theme ? ThemeSegment : Segment;

        /// <summary>Samples in one rendered segment (with its ring-out).</summary>
        public static int LengthSamples(MusicPart part) => part == MusicPart.Theme ? ThemeSegment + ThemeTail : Segment + Tail;

        /// <summary>Renders one part of one segment (stereo, <see cref="LengthSamples"/> long).</summary>
        public static StereoBuf Render(MusicCue cue)
        {
            switch (cue.Part)
            {
                case MusicPart.Explore: return Explore(cue);
                case MusicPart.Tension: return Tension(cue);
                case MusicPart.Combat: return Combat(cue);
                case MusicPart.Dread: return Dread(cue);
                default: return Theme(cue);
            }
        }

        /// <summary>Renders one part of one segment, interleaved for an AudioClip.</summary>
        public static float[] RenderInterleaved(MusicCue cue) => MusicKit.Interleave(Render(cue));

        #region Harmony
        private static readonly int[][] s_Scales =
        {
            new[] { 0, 2, 3, 5, 7, 9, 10 },   // Dorian
            new[] { 0, 2, 3, 5, 7, 8, 10 },   // Aeolian
            new[] { 0, 1, 3, 5, 7, 8, 10 },   // Phrygian
        };

        // Four bars each, as the scale degrees of the chords' roots (0 = i), with the mode their chords belong to:
        // i i VII i | i VI VII i | i IV i VII | i VII VI VII | VI VII i i | i III VII i | i bII i i | i iv i v | i i IV i | i v VI VII
        private static readonly int[][] s_Progressions =
        {
            new[] { 0, 0, 6, 0 }, new[] { 0, 5, 6, 0 }, new[] { 0, 3, 0, 6 }, new[] { 0, 6, 5, 6 }, new[] { 5, 6, 0, 0 },
            new[] { 0, 2, 6, 0 }, new[] { 0, 1, 0, 0 }, new[] { 0, 3, 0, 4 }, new[] { 0, 0, 3, 0 }, new[] { 0, 4, 5, 6 },
        };
        private static readonly int[] s_ProgressionMode = { 0, 1, 0, 1, 1, 0, 2, 1, 0, 1 };
        private static readonly float[] s_DayWeight = { 3f, 2f, 3f, 1.5f, 1.2f, 2.5f, 0.3f, 1f, 2.5f, 1.5f };
        private static readonly float[] s_NightWeight = { 2f, 2f, 1f, 2f, 1.2f, 1f, 2.5f, 2.5f, 1f, 2f };

        private sealed class Plan
        {
            public int[] Scale, Roots;
        }

        private static int Progression(uint seed, long index, bool night)
        {
            var g = new SynthGen(Rate, MusicKit.Hash(seed, index, 0xA11CEu), 0);
            float[] w = night ? s_NightWeight : s_DayWeight;
            float total = 0f;
            for (int i = 0; i < w.Length; i++) total += w[i];
            float r = g.Next() * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r < 0f) return i;
            }
            return 0;
        }

        // The chords of segment `index` (never the same progression twice running).
        private static Plan PlanFor(uint seed, long index, bool night)
        {
            int p = Progression(seed, index, night);
            if (p == Progression(seed, index - 1, night)) p = Progression(seed, index + 104729, night);
            return new Plan { Scale = s_Scales[s_ProgressionMode[p]], Roots = s_Progressions[p] };
        }

        // The MIDI note of a scale degree (0 = D4; any octave).
        private static int Midi(int[] scale, int degree)
        {
            int o = degree >= 0 ? degree / 7 : -((-degree + 6) / 7);
            return Tonic + 12 * o + scale[degree - o * 7];
        }

        private static bool IsChordTone(Plan p, int bar, int degree)
        {
            int d = ((degree - p.Roots[bar]) % 7 + 7) % 7;
            return d == 0 || d == 2 || d == 4;
        }

        // The nearest chord tone (chord tones are a third apart, so a neighbour always is one).
        private static int Snap(Plan p, int bar, int degree)
        {
            if (IsChordTone(p, bar, degree)) return degree;
            return IsChordTone(p, bar, degree - 1) ? degree - 1 : degree + 1;
        }

        // A chord tone of the bar (offset in degrees above its root) as a bass note, D2 .. C#3.
        private static int BassMidi(Plan p, int bar, int offset)
        {
            int m = Midi(p.Scale, p.Roots[bar] + offset) - 24;
            while (m < 38) m += 12;
            while (m >= 50) m -= 12;
            return m;
        }

        // The bar's triad voiced between lo and hi (ascending).
        private static int[] Voicing(Plan p, int bar, int lo, int hi)
        {
            var t = new int[3];
            for (int i = 0; i < 3; i++)
            {
                int m = Midi(p.Scale, p.Roots[bar] + 2 * i);
                while (m < lo) m += 12;
                while (m > hi) m -= 12;
                t[i] = m;
            }
            Array.Sort(t);
            return t;
        }

        private static float Hz(float midi) => Dsp.Midi(midi);

        // A player's timing: a few milliseconds either way.
        private static int Human(SynthGen g, float seconds = 0.008f) => (int)(g.Bipolar() * seconds * Rate);
        #endregion

        #region Finishing
        // Level, a soft ceiling, and the ring-out's cut-off end faded.
        private static StereoBuf Finish(StereoBuf s, float gain)
        {
            Dsp.Gain(s.L, gain);
            Dsp.Gain(s.R, gain);
            MusicKit.SoftLimit(s.L, 0.85f);
            MusicKit.SoftLimit(s.R, 0.85f);
            MusicKit.FadeEnd(s, Rate, 0.4f);
            return s;
        }

        // The body of a plucked instrument (0 guitar, 1 banjo, 2 dulcimer).
        private static void StringBody(float[] x, int instrument, bool night)
        {
            switch (instrument)
            {
                case 1:
                    Dsp.HP(x, 130f, Rate);
                    Dsp.PeakEq(x, 650f, 1.8f, 5f, Rate);
                    Dsp.PeakEq(x, 2400f, 1.4f, 2.5f, Rate);
                    Dsp.LP(x, 7800f, Rate);
                    break;
                case 2:
                    Dsp.HP(x, 90f, Rate);
                    Dsp.PeakEq(x, 420f, 1.2f, 3f, Rate);
                    Dsp.PeakEq(x, 1900f, 2f, 2.5f, Rate);
                    Dsp.LP(x, 7000f, Rate);
                    break;
                default:
                    Dsp.HP(x, 65f, Rate);
                    Dsp.PeakEq(x, 105f, 1.3f, 3.5f, Rate);
                    Dsp.PeakEq(x, 230f, 1.6f, 2f, Rate);
                    Dsp.PeakEq(x, 2800f, 0.9f, -3f, Rate);
                    Dsp.LP(x, 6500f, Rate);
                    break;
            }
            if (night) Dsp.LP(x, 3600f, Rate);
        }

        // The body of a fiddle or cello: the air and wood resonances and the bridge's brightness.
        private static void FiddleBody(float[] x, bool low)
        {
            Dsp.HP(x, low ? 70f : 170f, Rate);
            Dsp.PeakEq(x, low ? 200f : 290f, 1.4f, 3f, Rate);
            Dsp.PeakEq(x, low ? 800f : 1150f, 1.2f, -2.5f, Rate);
            Dsp.PeakEq(x, low ? 1900f : 2750f, 1.1f, 4f, Rate);
            Dsp.LP(x, low ? 4500f : 7000f, Rate);
        }

        // Two high sines a few hertz apart, beating, swelling and fading: glass in the dark.
        private static void Glass(StereoBuf s, int at, float hz, float seconds, float amp, SynthGen g)
        {
            int len = Math.Min(s.Length - at, (int)(seconds * Rate));
            float beat = g.Range(1.5f, 4f);
            double p1 = g.Next(), p2 = g.Next(), i1 = hz / (double)Rate, i2 = (hz + beat) / (double)Rate;
            Dsp.PanGains(g.Range(-0.6f, 0.6f), out float gl, out float gr);
            for (int i = 0; i < len; i++)
            {
                p1 += i1;
                if (p1 >= 1.0) p1 -= 1.0;
                p2 += i2;
                if (p2 >= 1.0) p2 -= 1.0;
                float v = (MathF.Sin(MusicKit.TwoPi * (float)p1) + MathF.Sin(MusicKit.TwoPi * (float)p2)) * 0.5f * amp * Dsp.Bell(i / (float)Rate, seconds, 0.45f);
                s.L[at + i] += v * gl;
                s.R[at + i] += v * gr;
            }
        }
        #endregion

        #region Explore
        private enum Texture { Rest, Bass, Roll, Pick, Melody, Cadence }

        // One bar's rhythm for a tune: note starts and lengths in beats.
        private static readonly float[][] s_CellAt =
        {
            new[] { 0f, 1.5f, 2f }, new[] { 0f, 1f, 2f }, new[] { 0f, 2f, 3f }, new[] { 0.5f, 1f, 2f, 3f },
            new[] { 0f, 3f }, new[] { 0f, 1f, 1.5f, 2f }, new[] { 1f, 2f }, new[] { 0f },
        };
        private static readonly float[][] s_CellLen =
        {
            new[] { 1.5f, 0.5f, 2f }, new[] { 1f, 1f, 2f }, new[] { 2f, 1f, 1f }, new[] { 0.5f, 1f, 1f, 1f },
            new[] { 3f, 1f }, new[] { 1f, 0.5f, 0.5f, 2f }, new[] { 1f, 2f }, new[] { 4f },
        };
        // Melodic steps (scale degrees), mostly stepwise.
        private static readonly int[] s_Steps = { -2, -1, -1, -1, 0, 1, 1, 1, 2, 2, 3, -3, 4 };

        // Two bars at a time: what the picker does (day / night), with weights.
        private static readonly Texture[][] s_DayPairs =
        {
            new[] { Texture.Melody, Texture.Melody }, new[] { Texture.Pick, Texture.Melody }, new[] { Texture.Pick, Texture.Pick },
            new[] { Texture.Roll, Texture.Melody }, new[] { Texture.Melody, Texture.Roll }, new[] { Texture.Roll, Texture.Rest },
            new[] { Texture.Bass, Texture.Rest }, new[] { Texture.Pick, Texture.Roll },
        };
        private static readonly float[] s_DayPairWeight = { 0.22f, 0.08f, 0.04f, 0.14f, 0.12f, 0.16f, 0.14f, 0.1f };
        private static readonly Texture[][] s_NightPairs =
        {
            new[] { Texture.Bass, Texture.Rest }, new[] { Texture.Roll, Texture.Rest }, new[] { Texture.Melody, Texture.Rest },
            new[] { Texture.Bass, Texture.Melody }, new[] { Texture.Rest, Texture.Roll }, new[] { Texture.Melody, Texture.Melody },
            new[] { Texture.Pick, Texture.Roll }, new[] { Texture.Roll, Texture.Melody },
        };
        private static readonly float[] s_NightPairWeight = { 0.2f, 0.15f, 0.15f, 0.15f, 0.1f, 0.1f, 0.08f, 0.07f };
        // Which chord tone (0 root, 1 third, 2 fifth) the fingers take on each off-beat of a Travis pattern.
        private static readonly int[] s_PickOrder = { 1, 2, 1, 2 };

        private static int Pick(SynthGen g, float[] weights)
        {
            float total = 0f;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            float r = g.Next() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                r -= weights[i];
                if (r < 0f) return i;
            }
            return 0;
        }

        // The phrase's tune: a rhythm and a contour for an opening bar and an answering one.
        private sealed class Motif
        {
            public int CellA, CellB, Start;
            public int[] StepsA, StepsB;
        }

        private static Motif MakeMotif(SynthGen g)
        {
            var m = new Motif { CellA = g.Int(0, 6), CellB = g.Int(0, 8), Start = g.Chance(0.5f) ? 4 : 2 };
            m.StepsA = Steps(g, s_CellAt[m.CellA].Length);
            m.StepsB = Steps(g, s_CellAt[m.CellB].Length);
            return m;
        }

        private static int[] Steps(SynthGen g, int n)
        {
            var s = new int[n];
            for (int i = 0; i < n; i++) s[i] = s_Steps[g.Int(0, s_Steps.Length)];
            return s;
        }

        // Exploring: a bowed drone on open D under sparse fingerpicking (guitar, banjo or dulcimer), the phrase's tune
        // coming and going, now and then a fiddle swelling in; at night lower, darker, slower, with harmonics like bells.
        private static StereoBuf Explore(MusicCue cue)
        {
            int n = Segment + Tail;
            var s = new StereoBuf(n);
            bool night = cue.Night;
            var g = MusicKit.Gen(Rate, cue.Seed, cue.Index, 0xE0u);
            var pg = MusicKit.Gen(Rate, cue.Seed, cue.Index - cue.PhrasePos, 0xE1u);
            var sg = MusicKit.Gen(Rate, cue.Seed, 0, 0xE2u);
            Plan plan = PlanFor(cue.Seed, cue.Index, night);
            double t0 = cue.Index * (double)Segment / Rate;
            float seg = Segment / (float)Rate;

            // The drone: an open D and its fifth, bowed softly; an octave down and darker at night.
            float hold = cue.Last ? seg - 2.5f : seg, release = cue.Last ? 5f : Overlap;
            float droneHz = Hz(night ? 38 : 50), swellHz = sg.Range(0.035f, 0.06f), swellPh = sg.Next();
            MusicKit.Drone(s, Rate, t0, droneHz, 0.05f, night ? 520f : 750f, 3, 9f, 0.55f, swellHz, swellPh, 0.5f, Overlap, hold, release);
            MusicKit.Drone(s, Rate, t0, droneHz * 1.5f, 0.025f, night ? 650f : 950f, 2, 7f, 0.7f, swellHz * 1.3f, swellPh + 0.5f, 0.6f, Overlap, hold, release);
            MusicKit.DroneAir(s, Rate, g, 0.003f, night ? 700f : 1100f, 0.9f, Overlap, hold, release);

            // The phrase's instrument and tune.
            float r = pg.Next();
            int instrument = night ? (r < 0.7f ? 0 : 2) : (r < 0.55f ? 0 : r < 0.78f ? 1 : 2);
            StringTone tone = instrument == 1 ? MusicKit.Banjo : instrument == 2 ? MusicKit.Dulcimer : MusicKit.Guitar;
            int up = instrument == 1 ? 12 : 0;
            Motif motif = MakeMotif(pg);
            int deg = motif.Start + (g.Chance(0.3f) ? (g.Chance(0.5f) ? 2 : -2) : 0);
            float amp = night ? 0.34f : 0.4f;

            // Two bars at a time.
            var tex = new Texture[4];
            for (int half = 0; half < 2; half++)
            {
                Texture[] pair = night ? s_NightPairs[Pick(g, s_NightPairWeight)] : s_DayPairs[Pick(g, s_DayPairWeight)];
                tex[half * 2] = pair[0];
                tex[half * 2 + 1] = pair[1];
            }
            if (cue.PhrasePos == 0 && (tex[0] == Texture.Pick || tex[0] == Texture.Melody)) tex[0] = night ? Texture.Bass : Texture.Roll;
            if (cue.Last)
            {
                tex[2] = night ? Texture.Bass : Texture.Melody;
                tex[3] = Texture.Cadence;
            }

            var pluck = new float[n];
            var bells = new float[n];
            for (int bar = 0; bar < 4; bar++)
            {
                switch (tex[bar])
                {
                    case Texture.Bass:
                        BassBar(pluck, g, plan, bar, tone, amp, night);
                        if (night && g.Chance(0.5f)) MusicKit.Harmonic(bells, bar * Bar + 3 * Beat + Human(g), Rate, Hz(g.Chance(0.5f) ? 74 : 81), 0.12f, 2.4f, g);
                        break;
                    case Texture.Roll:
                        RollBar(pluck, g, plan, bar, tone, amp, up);
                        break;
                    case Texture.Pick:
                        PickBar(pluck, g, plan, bar, tone, amp, night, up);
                        break;
                    case Texture.Melody:
                        bool answer = (bar & 1) == 1 && tex[bar - 1] == Texture.Melody;
                        int cell = answer ? motif.CellB : motif.CellA;
                        int[] steps = answer ? motif.StepsB : motif.StepsA;
                        MelodyBar(pluck, g, plan, bar, cell, steps, ref deg, tone, amp, up);
                        MusicKit.Pluck(pluck, bar * Bar + Human(g), Rate, Hz(BassMidi(plan, bar, 0)), amp * 0.8f, tone, g, 1.9f, 0.3f);
                        break;
                    case Texture.Cadence:
                        Cadence(pluck, bells, g, plan, bar, tone, amp, up);
                        break;
                    default:
                        if (night && g.Chance(0.4f)) MusicKit.Harmonic(bells, bar * Bar + g.Int(0, 3) * Beat + Human(g), Rate, Hz(g.Chance(0.5f) ? 74 : 69), 0.1f, 2.6f, g);
                        break;
                }
            }
            StringBody(pluck, instrument, night);
            Dsp.MixPan(s, pluck, 0, 1f, -0.15f);
            Dsp.LP(bells, 6000f, Rate);
            Dsp.MixPan(s, bells, 0, 1f, 0.35f);

            // A fiddle swelling in on a chord tone, sometimes sighing down a step (a cello at night) — in the bells' bus.
            float[] fiddle = bells;
            Array.Clear(fiddle, 0, fiddle.Length);
            int swells = night ? (g.Chance(0.45f) ? 1 : 0) : g.Int(0, 3);
            for (int k = 0; k < swells; k++)
            {
                int bar = g.Int(0, 4);
                if (tex[bar] == Texture.Melody || tex[bar] == Texture.Pick) bar = (bar + 1) & 3;
                float start = (bar * Bar + g.Int(0, 3) * Beat) / (float)Rate;
                float len = Math.Min(g.Range(2.6f, 5f), n / (float)Rate - start - 0.5f);
                if (len < 1.5f) continue;
                int d = Snap(plan, bar, night ? g.Int(-5, 2) : g.Int(2, 9));
                int midi = Midi(plan.Scale, d) - (night ? 12 : 0);
                BowTone bow = night ? MusicKit.Cello : MusicKit.FiddleSwell;
                int at = (int)(start * Rate);
                if (g.Chance(0.35f) && len > 3f)
                {
                    float first = len * 0.55f;
                    int lower = Midi(plan.Scale, d - 1) - (night ? 12 : 0);
                    MusicKit.Bow(fiddle, at, Rate, Hz(midi), 0.15f, first + 0.3f, bow, g);
                    MusicKit.Bow(fiddle, at + (int)(first * Rate), Rate, Hz(lower), 0.13f, len - first, new BowTone(0.25f, bow.Release, bow.Vibrato, bow.VibratoHz, bow.Noise, bow.Bright, bow.Voices, bow.Detune), g, Hz(midi), 0.16f);
                }
                else MusicKit.Bow(fiddle, at, Rate, Hz(midi), 0.15f, len, bow, g);
            }
            FiddleBody(fiddle, night);
            Dsp.MixPan(s, fiddle, 0, 1f, 0.3f);

            MusicKit.Space(s, Rate, night ? 3.4f : 2.6f, night ? 3200f : 4800f, night ? 0.42f : 0.32f, 0.16f, 0.022f);
            return Finish(s, night ? 1.75f : 1.5f);
        }

        // A bass note on the downbeat, maybe its fifth on the third beat, let ring.
        private static void BassBar(float[] x, SynthGen g, Plan p, int bar, in StringTone tone, float amp, bool night)
        {
            MusicKit.Pluck(x, bar * Bar + Human(g), Rate, Hz(BassMidi(p, bar, 0)), amp * 1.05f, tone, g, 1.9f, 0.4f);
            if (g.Chance(night ? 0.4f : 0.6f)) MusicKit.Pluck(x, bar * Bar + 2 * Beat + Human(g), Rate, Hz(BassMidi(p, bar, 4)), amp * 0.8f, tone, g);
        }

        // The chord rolled up slowly from the bass and left to ring; sometimes a top note on the third beat.
        private static void RollBar(float[] x, SynthGen g, Plan p, int bar, in StringTone tone, float amp, int up)
        {
            int at = bar * Bar + Human(g);
            MusicKit.Pluck(x, at, Rate, Hz(BassMidi(p, bar, 0)), amp, tone, g);
            int[] v = Voicing(p, bar, 55 + up, 69 + up);
            float gap = g.Range(0.09f, 0.16f);
            for (int i = 0; i < v.Length; i++)
                MusicKit.Pluck(x, at + (int)((i + 1) * gap * Rate), Rate, Hz(v[i]), amp * (0.78f - 0.06f * i), tone, g);
            if (g.Chance(0.5f)) MusicKit.Pluck(x, at + 2 * Beat + Human(g), Rate, Hz(v[2] + (g.Chance(0.5f) ? 0 : 5)), amp * 0.62f, tone, g);
        }

        // Travis picking: the thumb alternating root and fifth on the beats, the fingers on chord tones between.
        private static void PickBar(float[] x, SynthGen g, Plan p, int bar, in StringTone tone, float amp, bool night, int up)
        {
            int root = BassMidi(p, bar, 0), alt = BassMidi(p, bar, g.Chance(0.75f) ? 4 : 2);
            int[] v = Voicing(p, bar, 55 + up, 69 + up);
            float drop = night ? 0.45f : 0.3f;
            for (int e = 0; e < 8; e++)
            {
                int at = bar * Bar + e * (Beat / 2) + ((e & 1) == 1 ? (int)(0.02f * Beat) : 0) + Human(g);
                if ((e & 1) == 0)
                {
                    int note = e == 0 || e == 4 ? root : alt;
                    MusicKit.Pluck(x, at, Rate, Hz(note), amp * (e == 0 ? 0.95f : 0.8f) * g.Range(0.9f, 1.05f), tone, g, 0.72f, 0.18f);
                }
                else
                {
                    if (g.Chance(drop)) continue;
                    MusicKit.Pluck(x, at, Rate, Hz(v[s_PickOrder[e >> 1]]), amp * 0.62f * g.Range(0.85f, 1.05f), tone, g, 1.4f, 0.35f);
                }
            }
            MusicKit.Pluck(x, bar * Bar + Human(g), Rate, Hz(v[2]), amp * 0.72f, tone, g, 2.4f, 0.4f);
        }

        // A bar of the phrase's tune, its strong beats landing on chord tones, now and then hammered on from below.
        private static void MelodyBar(float[] x, SynthGen g, Plan p, int bar, int cell, int[] steps, ref int deg, in StringTone tone, float amp, int up)
        {
            float[] at = s_CellAt[cell], len = s_CellLen[cell];
            for (int i = 0; i < at.Length; i++)
            {
                int d = deg + steps[i];
                if (d > 9) d = 18 - d;
                if (d < -3) d = -6 - d;
                bool strong = at[i] == 0f || at[i] == 2f;
                if (strong || len[i] >= 2f) d = Snap(p, bar, d);
                deg = d;
                int midi = Midi(p.Scale, d) + up;
                int t = bar * Bar + (int)(at[i] * Beat) + Human(g);
                float a = amp * (strong ? 1f : 0.82f) * g.Range(0.88f, 1.05f);
                float ring = Math.Min(4f, len[i] * 0.75f + 1.4f);
                if (len[i] >= 1f && g.Chance(0.18f))
                {
                    int grace = t - (int)(0.075f * Rate);
                    MusicKit.Pluck(x, grace, Rate, Hz(Midi(p.Scale, d - 1) + up), a * 0.7f, tone, g, 0.075f, 0.02f);
                    a *= 0.8f;
                }
                MusicKit.Pluck(x, t, Rate, Hz(midi), a, tone, g, ring, 0.25f);
            }
        }

        // The phrase comes home: a low D and the D minor chord rolled, a harmonic ringing over it.
        private static void Cadence(float[] x, float[] bells, SynthGen g, Plan p, int bar, in StringTone tone, float amp, int up)
        {
            int at = bar * Bar + Human(g);
            MusicKit.Pluck(x, at, Rate, Hz(38), amp * 1.1f, tone, g);
            int[] v = { 50 + up, 53 + up, 57 + up, 62 + up };
            float gap = g.Range(0.12f, 0.18f);
            for (int i = 0; i < v.Length; i++) MusicKit.Pluck(x, at + (int)((i + 1) * gap * Rate), Rate, Hz(v[i]), amp * (0.75f - 0.07f * i), tone, g);
            MusicKit.Harmonic(bells, at + 2 * Beat + Human(g), Rate, Hz(74), 0.13f, 2.6f, g);
        }
        #endregion

        #region Tension
        // Eighth-note masks for the ticking muted string: 1 the root, 2 the flat second above it.
        private static readonly int[][] s_Ticks =
        {
            new[] { 1, 0, 1, 1, 1, 0, 1, 0 }, new[] { 1, 1, 0, 1, 1, 0, 1, 2 }, new[] { 1, 0, 0, 1, 0, 0, 1, 0 },
            new[] { 1, 0, 1, 0, 1, 0, 1, 2 }, new[] { 1, 1, 1, 0, 1, 1, 0, 2 },
        };
        // Clusters for the strings near the bridge (semitones over the bar's root): seconds, tritones, a flat ninth.
        private static readonly int[][] s_Clusters =
        {
            new[] { 0, 1, 6 }, new[] { 0, 1, 7, 13 }, new[] { 0, 6, 11 }, new[] { 0, 1, 5 }, new[] { -1, 0, 6, 7 },
        };

        // Something has found you: a heartbeat on every beat over a low drone, a muted string ticking the bar's root
        // and its flat second, strings bowed near the bridge with a nervous tremolo, and glass ringing high up.
        private static StereoBuf Tension(MusicCue cue)
        {
            int n = Segment + Tail;
            var s = new StereoBuf(n);
            var g = MusicKit.Gen(Rate, cue.Seed, cue.Index, 0x7E0u);
            var sg = MusicKit.Gen(Rate, cue.Seed, 0, 0x7E1u);
            Plan plan = PlanFor(cue.Seed, cue.Index, cue.Night);
            double t0 = cue.Index * (double)Segment / Rate;
            float seg = Segment / (float)Rate;

            MusicKit.Drone(s, Rate, t0, Hz(26), 0.09f, 220f, 2, 6f, 0.25f, sg.Range(0.07f, 0.11f), sg.Next(), 0.45f, Overlap, seg, Overlap);
            MusicKit.Drone(s, Rate, t0, Hz(38), 0.08f, 420f, 2, 11f, 0.5f, sg.Range(0.05f, 0.08f), sg.Next(), 0.6f, Overlap, seg, Overlap);

            var a = new float[n];
            var b2 = new float[n];
            for (int b = 0; b < 16; b++) MusicKit.Heart(a, b * Beat + Human(g, 0.006f), Rate, (b % 4 == 0 ? 0.9f : 0.72f) * g.Range(0.92f, 1.05f), g);
            Dsp.LP(a, 240f, Rate);
            Dsp.MixPan(s, a, 0, 0.75f, 0f);

            Array.Clear(a, 0, n);
            int[] mask = s_Ticks[g.Int(0, s_Ticks.Length)];
            bool sparse = g.Chance(0.35f);
            for (int bar = 0; bar < 4; bar++)
            {
                if (sparse && (bar & 1) == 0) continue;
                int root = BassMidi(plan, bar, 0);
                for (int e = 0; e < 8; e++)
                {
                    if (mask[e] == 0) continue;
                    int at = bar * Bar + e * (Beat / 2) + Human(g, 0.004f);
                    float amp = (e == 0 ? 0.55f : (e & 1) == 0 ? 0.44f : 0.36f) * g.Range(0.9f, 1.05f);
                    MusicKit.Pluck(a, at, Rate, Hz(mask[e] == 2 ? root + 1 : root), amp, MusicKit.Muted, g, 0.17f, 0.05f);
                }
            }
            Dsp.HP(a, 55f, Rate);
            Dsp.LP(a, 1500f, Rate);
            Dsp.MixPan(s, a, 0, 0.8f, -0.12f);

            // The strings, the cluster's notes alternating between the two sides.
            Array.Clear(a, 0, n);
            int swells = g.Int(1, 3);
            for (int k = 0; k < swells; k++)
            {
                int bar = k == 0 ? g.Int(0, 2) : g.Int(2, 4);
                float start = (bar * Bar + g.Int(0, 4) * Beat) / (float)Rate;
                float len = Math.Min(g.Range(4.5f, 8f), n / (float)Rate - start - 0.4f);
                if (len < 2f) continue;
                int baseNote = Midi(plan.Scale, plan.Roots[bar]) - (g.Chance(0.5f) ? 12 : 0);
                int[] cluster = s_Clusters[g.Int(0, s_Clusters.Length)];
                for (int i = 0; i < cluster.Length; i++)
                {
                    int at = (int)((start + i * g.Range(0.1f, 0.5f)) * Rate);
                    MusicKit.Bow(((i + k) & 1) == 0 ? a : b2, at, Rate, Hz(baseNote + cluster[i]), 0.05f, len - i * 0.3f, MusicKit.Ponticello, g);
                }
            }
            foreach (float[] side in new[] { a, b2 })
            {
                Dsp.HP(side, 280f, Rate);
                Dsp.PeakEq(side, 4200f, 1f, 3f, Rate);
                Dsp.LP(side, 9000f, Rate);
            }
            Dsp.MixPan(s, a, 0, 0.9f, -0.55f);
            Dsp.MixPan(s, b2, 0, 0.9f, 0.55f);

            if (g.Chance(0.6f))
            {
                float at = g.Range(1f, seg - 5f);
                Glass(s, (int)(at * Rate), Hz(Midi(plan.Scale, plan.Roots[g.Int(0, 4)] + 7) + 12), g.Range(4f, 7f), 0.022f, g);
            }

            MusicKit.Space(s, Rate, 3.2f, 2800f, 0.34f, 0.12f, 0.03f);
            return Finish(s, 1f);
        }
        #endregion

        #region Combat
        // Sixteen steps a bar: B a big drum, m a smaller one, h a high tom, l a low tom.
        private static readonly string[] s_DrumBars =
        {
            "B..m..m.B..m.m..", "B...l.l.B.h.B.h.", "B.hhl...B.l.m.h.", "B..mB..mB.hlB.lh", "B.m.l.m.B.m.l.hh",
        };
        private static readonly string[] s_Fills = { "B...B...llhhBlhh", "B..mB..mlhlhBBhh", "B.l.B.l.hlhlBhhh" };
        // The low strings' gallop: x a note, o the octave, . a rest.
        private static readonly string[] s_Gallops = { "x.xxx.xxx.xxx.xx", "x.xox.xxx.xox.xx", "xxx.xxx.xxx.xoxo", "x..xx..xx.xxx.xx" };

        // A fight: taiko-like drums and toms, a stick ticking eighths, low strings galloping in sixteenths on the bar's
        // root, fiddle chops on two and four, a banjo roll now and then, and a riser into the next downbeat.
        private static StereoBuf Combat(MusicCue cue)
        {
            int n = Segment + Tail;
            var s = new StereoBuf(n);
            var g = MusicKit.Gen(Rate, cue.Seed, cue.Index, 0xC00u);
            Plan plan = PlanFor(cue.Seed, cue.Index, cue.Night);
            int step = Beat / 4;

            var drums = new float[n];
            var hi = new float[n];
            var lo = new float[n];
            var ticks = new float[n];
            string a = s_DrumBars[g.Int(0, s_DrumBars.Length)], b = s_DrumBars[g.Int(0, s_DrumBars.Length)];
            string fill = s_Fills[g.Int(0, s_Fills.Length)];
            for (int bar = 0; bar < 4; bar++)
            {
                string pat = bar == 3 ? fill : (bar & 1) == 0 ? a : b;
                for (int k = 0; k < 16; k++)
                {
                    int at = bar * Bar + k * step + Human(g, 0.003f);
                    float accent = k == 0 ? 1f : (k & 3) == 0 ? 0.88f : 0.74f;
                    switch (pat[k])
                    {
                        case 'B': MusicKit.Taiko(drums, at, Rate, 0.8f * accent, 1f, g); break;
                        case 'm': MusicKit.Taiko(drums, at, Rate, 0.5f * accent, 0.72f, g); break;
                        case 'h': MusicKit.Tom(hi, at, Rate, 0.42f * accent, 1.4f, g); break;
                        case 'l': MusicKit.Tom(lo, at, Rate, 0.48f * accent, 1f, g); break;
                    }
                    if ((k & 1) == 0) MusicKit.Tick(ticks, at, Rate, (k & 3) == 2 ? 0.5f : 0.3f, g);
                }
            }
            Dsp.HP(drums, 32f, Rate);
            Dsp.Saturate(drums, 1.3f);
            Dsp.MixPan(s, drums, 0, 1f, 0f);
            Dsp.MixPan(s, hi, 0, 0.9f, 0.35f);
            Dsp.MixPan(s, lo, 0, 0.9f, -0.3f);
            Dsp.HP(ticks, 900f, Rate);
            Dsp.MixPan(s, ticks, 0, 0.5f, 0.25f);

            // The buses are reused from here on: the drums are in the mix.
            float[] bass = drums;
            Array.Clear(bass, 0, n);
            string gallop = s_Gallops[g.Int(0, s_Gallops.Length)];
            for (int bar = 0; bar < 4; bar++)
            {
                int root = BassMidi(plan, bar, 0);
                for (int k = 0; k < 16; k++)
                {
                    char c = gallop[k];
                    if (c == '.') continue;
                    int note = c == 'o' ? root + 12 : root;
                    int at = bar * Bar + k * step + Human(g, 0.003f);
                    float acc = (k & 3) == 0 ? 1f : 0.72f;
                    MusicKit.Bow(bass, at, Rate, Hz(note), 0.3f * acc, 0.15f, MusicKit.Spiccato, g);
                    MusicKit.Pluck(bass, at, Rate, Hz(note), 0.32f * acc, MusicKit.Muted, g, 0.12f, 0.04f);
                }
            }
            Dsp.HP(bass, 40f, Rate);
            Dsp.Saturate(bass, 1.8f);
            Dsp.LP(bass, 1300f, Rate);
            Dsp.MixPan(s, bass, 0, 0.85f, 0f);

            // Fiddle chops: a scrape of the bow on two and four, pitched on the chord.
            float[] chops = hi;
            Array.Clear(chops, 0, n);
            var chop = new BowTone(0.004f, 0.05f, 0f, 5f, 0.5f, 3200f, 2, 18f);
            for (int bar = 0; bar < 4; bar++)
            {
                int[] v = Voicing(plan, bar, 57, 70);
                for (int k = 4; k < 16; k += 8)
                {
                    int at = bar * Bar + k * step + Human(g, 0.004f);
                    MusicKit.Bow(chops, at, Rate, Hz(v[0]), 0.18f, 0.09f, chop, g);
                    MusicKit.Bow(chops, at, Rate, Hz(v[2]), 0.14f, 0.09f, chop, g);
                }
            }
            Dsp.HP(chops, 380f, Rate);
            Dsp.PeakEq(chops, 2600f, 1.2f, 4f, Rate);
            Dsp.MixPan(s, chops, 0, 0.7f, 0.4f);

            // A banjo roll racing through a bar.
            if (g.Chance(0.55f))
            {
                float[] banjo = lo;
                Array.Clear(banjo, 0, n);
                int bar = g.Int(1, 3);
                int[] v = Voicing(plan, bar, 62, 76);
                int[] roll = { 0, 1, 2, 0, 1, 2, 0, 2 };
                for (int k = 0; k < 16; k++)
                {
                    int note = v[roll[k & 7]];
                    MusicKit.Pluck(banjo, bar * Bar + k * step + Human(g, 0.004f), Rate, Hz(note), ((k & 3) == 0 ? 0.32f : 0.24f) * g.Range(0.9f, 1.05f), MusicKit.Banjo, g, 0.5f, 0.3f);
                }
                StringBody(banjo, 1, false);
                Dsp.MixPan(s, banjo, 0, 0.8f, -0.45f);
            }

            float[] fx = ticks;
            Array.Clear(fx, 0, n);
            MusicKit.Riser(fx, Segment - (int)(1.5f * Rate), Rate, 0.12f, 1.5f, g);
            Dsp.MixPan(s, fx, 0, 1f, 0.1f);

            MusicKit.Space(s, Rate, 1.9f, 4200f, 0.2f, 0.18f, 0.012f);
            return Finish(s, 0.85f);
        }
        #endregion

        #region Dread
        // An emission: the Zone convulsing. A low cluster drone, booms like far blasts, a string section swelling on a
        // cluster and creeping sharp, a wordless choir, and a thin whine rising over it all.
        private static StereoBuf Dread(MusicCue cue)
        {
            int n = Segment + Tail;
            var s = new StereoBuf(n);
            var g = MusicKit.Gen(Rate, cue.Seed, cue.Index, 0xD00u);
            var sg = MusicKit.Gen(Rate, cue.Seed, 0, 0xD01u);
            double t0 = cue.Index * (double)Segment / Rate;
            float seg = Segment / (float)Rate;

            MusicKit.Drone(s, Rate, t0, Hz(26), 0.13f, 300f, 2, 8f, 0.3f, sg.Range(0.05f, 0.08f), sg.Next(), 0.5f, Overlap, seg, Overlap);
            MusicKit.Drone(s, Rate, t0, Hz(33), 0.06f, 380f, 2, 10f, 0.5f, sg.Range(0.06f, 0.09f), sg.Next(), 0.6f, Overlap, seg, Overlap);
            MusicKit.Drone(s, Rate, t0, Hz(39), 0.05f, 450f, 2, 14f, 0.7f, sg.Range(0.07f, 0.1f), sg.Next(), 0.7f, Overlap, seg, Overlap);

            var a = new float[n];
            var b = new float[n];
            MusicKit.Boom(a, Human(g, 0.01f), Rate, 0.85f, g);
            MusicKit.Boom(a, 2 * Bar + g.Int(0, 2) * Beat, Rate, 0.6f, g);
            Dsp.LP(a, 380f, Rate);
            Dsp.MixPan(s, a, 0, 1f, 0f);

            // The section, its notes alternating between the sides.
            Array.Clear(a, 0, n);
            int baseNote = g.Chance(0.5f) ? 62 : 50;
            int[] cluster = g.Chance(0.5f) ? new[] { 0, 1, 6 } : new[] { 0, 1, 6, 7 };
            for (int i = 0; i < cluster.Length; i++)
            {
                float start = g.Range(0f, 1.5f) + i * 0.4f;
                float len = seg - start + g.Range(0.6f, 2f);
                float hz = Hz(baseNote + cluster[i]);
                MusicKit.Bow((i & 1) == 0 ? a : b, (int)(start * Rate), Rate, hz, 0.05f, len, MusicKit.Section, g, 0f, 0.1f, hz * MathF.Pow(2f, g.Range(30f, 60f) / 1200f));
            }
            foreach (float[] side in new[] { a, b })
            {
                Dsp.HP(side, 150f, Rate);
                Dsp.PeakEq(side, 2500f, 1f, 2f, Rate);
            }
            Dsp.MixPan(s, a, 0, 1f, -0.5f);
            Dsp.MixPan(s, b, 0, 1f, 0.5f);

            // The choir, the same way.
            Array.Clear(a, 0, n);
            Array.Clear(b, 0, n);
            int[] voices = { 50, 56, 63 };
            for (int i = 0; i < voices.Length; i++)
            {
                if (i > 0 && g.Chance(0.25f)) continue;
                float start = g.Range(0.5f, 3f) + i * 0.8f;
                float len = Math.Min(g.Range(6f, 9f), n / (float)Rate - start - 0.5f);
                MusicKit.Choir((i & 1) == 0 ? b : a, (int)(start * Rate), Rate, Hz(voices[i]), 0.16f, len, g, g.Range(15f, 40f));
            }
            Dsp.HP(a, 120f, Rate);
            Dsp.HP(b, 120f, Rate);
            Dsp.MixPan(s, a, 0, 1f, -0.35f);
            Dsp.MixPan(s, b, 0, 1f, 0.35f);

            Whine(s, (int)(g.Range(0.5f, 2f) * Rate), Hz(g.Chance(0.5f) ? 81 : 80), Hz(86), seg - 1f, 0.025f, g);

            MusicKit.Space(s, Rate, 5.5f, 2400f, 0.5f, 0.08f, 0.045f);
            return Finish(s, 0.75f);
        }

        // A thin sine climbing from hz0 to hz1, wavering, swelling in and out.
        private static void Whine(StereoBuf s, int at, float hz0, float hz1, float seconds, float amp, SynthGen g)
        {
            int len = Math.Min(s.Length - at, (int)(seconds * Rate));
            double ph = g.Next();
            float lnRatio = MathF.Log(hz1 / hz0), vib = g.Range(3.6f, 5f), pan = g.Range(-0.4f, 0.4f);
            Dsp.PanGains(pan, out float gl, out float gr);
            for (int i = 0; i < len; i++)
            {
                float u = i / (float)len, t = i / (float)Rate;
                float f = hz0 * MathF.Exp(lnRatio * u) * (1f + 0.006f * MathF.Sin(MusicKit.TwoPi * vib * t));
                ph += f / Rate;
                if (ph >= 1.0) ph -= 1.0;
                float v = MathF.Sin(MusicKit.TwoPi * (float)ph) * amp * Dsp.Bell(t, seconds, 0.7f);
                s.L[at + i] += v * gl;
                s.R[at + i] += v * gr;
            }
        }
        #endregion

        #region Theme
        // A chord of the theme: its bass, the alternate bass, and its tones from the fourth string up (MIDI, ascending).
        private sealed class TChord
        {
            public readonly int Bass, Alt;
            public readonly int[] Tones;

            public TChord(int bass, int alt, params int[] tones)
            {
                Bass = bass;
                Alt = alt;
                Tones = tones;
            }
        }

        private static readonly TChord s_Dm = new TChord(38, 45, 50, 53, 57, 62, 65);
        private static readonly TChord s_F = new TChord(41, 48, 53, 57, 60, 65, 69);
        private static readonly TChord s_C = new TChord(48, 43, 52, 55, 60, 64, 67);
        private static readonly TChord s_Bb = new TChord(46, 41, 50, 53, 58, 62, 65);
        private static readonly TChord s_A = new TChord(45, 40, 52, 57, 61, 64, 69);

        // Intro, A, A', B, A'', outro, breath.
        private static readonly TChord[][] s_ThemeChart =
        {
            new[] { s_Dm, s_Dm, s_C, s_Dm }, new[] { s_Dm, s_F, s_C, s_Dm }, new[] { s_Dm, s_Bb, s_C, s_A }, new[] { s_F, s_C, s_Bb, s_A },
            new[] { s_Dm, s_F, s_C, s_Dm }, new[] { s_Bb, s_C, s_Dm, s_Dm }, new[] { s_Dm, s_Dm, s_Dm, s_Dm },
        };

        // The tune ("WithTheDead"), per section: (MIDI, beat, beats) triples, beats counted over the section's four bars.
        private static readonly float[][] s_ThemeTune =
        {
            new float[0],
            new[] { 57f, 0f, 1f, 62f, 1f, 1f, 64f, 2f, 0.5f, 65f, 2.5f, 1.5f, 64f, 4f, 1f, 62f, 5f, 1f, 60f, 6f, 2f, 62f, 8f, 1.5f, 65f, 9.5f, 0.5f, 64f, 10f, 1f, 60f, 11f, 1f, 62f, 12f, 4f },
            new[] { 57f, 0f, 1f, 62f, 1f, 1f, 64f, 2f, 0.5f, 65f, 2.5f, 1.5f, 67f, 4f, 1f, 65f, 5f, 1f, 62f, 6f, 2f, 64f, 8f, 1.5f, 65f, 9.5f, 0.5f, 67f, 10f, 1f, 64f, 11f, 1f, 64f, 12f, 1f, 61f, 13f, 1f, 57f, 14f, 2f },
            new[] { 72f, 0f, 1.5f, 69f, 1.5f, 0.5f, 65f, 2f, 2f, 67f, 4f, 1f, 64f, 5f, 1f, 60f, 6f, 2f, 62f, 8f, 1f, 65f, 9f, 1f, 70f, 10f, 1f, 69f, 11f, 1f, 69f, 12f, 1f, 67f, 13f, 0.5f, 65f, 13.5f, 0.5f, 64f, 14f, 2f },
            new[] { 57f, 0f, 1f, 62f, 1f, 1f, 64f, 2f, 0.5f, 65f, 2.5f, 1.5f, 64f, 4f, 1f, 62f, 5f, 1f, 60f, 6f, 2f, 62f, 8f, 1.5f, 65f, 9.5f, 0.5f, 64f, 10f, 1f, 60f, 11f, 1f, 62f, 12f, 2f, 69f, 14f, 2f },
            new[] { 65f, 0f, 2f, 62f, 2f, 2f, 64f, 4f, 2f, 60f, 6f, 2f, 62f, 8f, 4f },
            new float[0],
        };
        // The fiddle's long counter-line over A'', and the cello's roots under B.
        private static readonly float[] s_ThemeFiddle = { 69f, 0f, 4f, 72f, 4f, 2f, 69f, 6f, 2f, 67f, 8f, 4f, 65f, 12f, 2f, 69f, 14f, 2f };
        private static readonly float[] s_ThemeCello = { 53f, 0f, 3.7f, 48f, 4f, 3.7f, 46f, 8f, 3.7f, 45f, 12f, 3.7f };

        // The title theme: a lone guitar fingerpicking the tune over its own alternating bass, a cello drone on low D
        // and A, a fiddle's counter-line on the last return. Each telling (seven sections, about 105 s with its breath)
        // is played afresh: the tune holds, the picking, ornaments, harmonics and the strings' parts change.
        private static StereoBuf Theme(MusicCue cue)
        {
            int n = ThemeSegment + ThemeTail;
            var s = new StereoBuf(n);
            long index = Math.Max(0L, cue.Index);
            int section = (int)(index % ThemeSections);
            long pass = index / ThemeSections;
            uint seed = MusicKit.Hash(cue.Seed, pass, 0x7E3Eu);
            var g = MusicKit.Gen(Rate, seed, section, 0x70u);
            var pg = MusicKit.Gen(Rate, seed, 0, 0x72u);
            var sg = MusicKit.Gen(Rate, cue.Seed, 0, 0x71u);
            double t0 = index * (double)ThemeSegment / Rate;
            float seg = ThemeSegment / (float)Rate;
            const int beat = ThemeBeat, bar = ThemeBar;
            TChord[] chart = s_ThemeChart[section];
            float[] tune = s_ThemeTune[section];
            bool harmonics = pg.Chance(0.75f), fiddle = pg.Chance(0.7f), cello = pg.Chance(0.65f);
            float ornament = pg.Range(0.1f, 0.3f);

            if (section != 6)
            {
                float fadeIn = section == 0 ? 4f : Overlap;
                float hold = section == 5 ? seg - 3f : seg, release = section == 5 ? 6f : Overlap;
                float swellHz = sg.Range(0.04f, 0.06f), swellPh = sg.Next();
                MusicKit.Drone(s, Rate, t0, Hz(38), 0.045f, 800f, 3, 8f, 0.5f, swellHz, swellPh, 0.45f, fadeIn, hold, release);
                MusicKit.Drone(s, Rate, t0, Hz(45), 0.022f, 1000f, 2, 6f, 0.6f, swellHz * 1.4f, swellPh + 0.3f, 0.5f, fadeIn, hold, release);
            }

            StringTone tone = MusicKit.Guitar;
            var gtr = new float[n];
            var bells = new float[n];
            if (section == 0)
            {
                MusicKit.Pluck(gtr, Human(g), Rate, Hz(38), 0.44f, tone, g);
                if (harmonics) MusicKit.Harmonic(bells, 2 * beat + Human(g), Rate, Hz(74), 0.14f, 3f, g);
                ThemeRoll(gtr, g, s_Dm, bar + Human(g), tone, 0.36f, 0.18f);
                if (harmonics && g.Chance(0.6f)) MusicKit.Harmonic(bells, bar + 3 * beat + Human(g), Rate, Hz(81), 0.1f, 3f, g);
                MusicKit.Pluck(gtr, 2 * bar + Human(g), Rate, Hz(48), 0.38f, tone, g);
                ThemeRoll(gtr, g, s_C, 2 * bar + beat / 2 + Human(g), tone, 0.32f, 0.15f);
                MusicKit.Pluck(gtr, 3 * bar + Human(g), Rate, Hz(38), 0.4f, tone, g);
                MusicKit.Pluck(gtr, 3 * bar + 2 * beat + Human(g), Rate, Hz(62), 0.3f, tone, g);
            }
            else if (section == 6)
            {
                int count = g.Int(1, 3);
                for (int i = 0; i < count; i++)
                    MusicKit.Harmonic(bells, bar + g.Int(0, 8) * beat + Human(g), Rate, Hz(i == 0 ? 74 : 81), 0.09f, 3.2f, g);
            }
            else
            {
                bool full = section == 2 || section == 4;
                for (int i = 0; i + 2 < tune.Length; i += 3)
                {
                    int midi = (int)tune[i];
                    float len = tune[i + 2];
                    int at = (int)(tune[i + 1] * beat) + Human(g, 0.012f);
                    float a = 0.46f * g.Range(0.92f, 1.04f);
                    if (len >= 1f && g.Chance(ornament))
                    {
                        MusicKit.Pluck(gtr, at - (int)(0.08f * Rate), Rate, Hz(midi - (midi % 12 == 2 || midi % 12 == 9 ? 2 : 1)), a * 0.7f, tone, g, 0.08f, 0.02f);
                        a *= 0.82f;
                    }
                    MusicKit.Pluck(gtr, at, Rate, Hz(midi), a, tone, g, len * beat / (float)Rate + 0.4f, 0.3f);
                }
                int bars = section == 5 ? 2 : 4;
                for (int b = 0; b < bars; b++)
                {
                    TChord c = chart[b];
                    for (int e = 0; e < 8; e++)
                    {
                        float pos = b * 4 + e * 0.5f;
                        int at = b * bar + e * (beat / 2) + ((e & 1) == 1 ? (int)(0.03f * beat) : 0) + Human(g, 0.008f);
                        if ((e & 1) == 0)
                        {
                            if (!full && (e == 2 || e == 6) && g.Chance(0.55f)) continue;
                            MusicKit.Pluck(gtr, at, Rate, Hz(e == 0 || e == 4 ? c.Bass : c.Alt), 0.34f * (e == 0 ? 1f : 0.85f), tone, g, 0.9f, 0.2f);
                        }
                        else
                        {
                            if (TuneStartsAt(tune, pos) || g.Chance(full ? 0.15f : 0.35f)) continue;
                            int note = Below(c.Tones, TuneAt(tune, pos) - 2, e >> 1);
                            if (note > 0) MusicKit.Pluck(gtr, at, Rate, Hz(note), 0.24f * g.Range(0.85f, 1.05f), tone, g, 1.2f, 0.3f);
                        }
                    }
                }
                if (section == 5)
                {
                    ThemeRoll(gtr, g, s_Dm, 2 * bar + Human(g), tone, 0.34f, 0.2f);
                    MusicKit.Pluck(gtr, 3 * bar + Human(g), Rate, Hz(38), 0.42f, tone, g);
                    if (harmonics) MusicKit.Harmonic(bells, 3 * bar + beat + Human(g), Rate, Hz(74), 0.13f, 3.4f, g);
                    MusicKit.Harmonic(bells, 3 * bar + 5 * beat / 2 + Human(g), Rate, Hz(81), 0.09f, 3.4f, g);
                }
            }
            StringBody(gtr, 0, false);
            Dsp.MixPan(s, gtr, 0, 1f, -0.1f);
            Dsp.LP(bells, 6500f, Rate);
            Dsp.MixPan(s, bells, 0, 1f, 0.3f);

            float[] strings = bells;
            Array.Clear(strings, 0, n);
            bool low = false;
            if (section == 4 && fiddle) ThemeLine(strings, g, s_ThemeFiddle, MusicKit.FiddleSwell, 0.11f, 0);
            else if (section == 3 && cello)
            {
                ThemeLine(strings, g, s_ThemeCello, MusicKit.Cello, 0.13f, 0);
                low = true;
            }
            FiddleBody(strings, low);
            Dsp.MixPan(s, strings, 0, 1f, 0.35f);

            MusicKit.Space(s, Rate, 2.8f, 4600f, 0.34f, 0.16f, 0.02f);
            return Finish(s, 1.5f);
        }

        // A chord rolled slowly up from the fourth string and left to ring.
        private static void ThemeRoll(float[] x, SynthGen g, TChord c, int at, in StringTone tone, float amp, float gap)
        {
            for (int i = 0; i < 4 && i < c.Tones.Length; i++)
                MusicKit.Pluck(x, at + (int)(i * gap * g.Range(0.85f, 1.15f) * Rate), Rate, Hz(c.Tones[i]), amp * (1f - 0.08f * i), tone, g);
        }

        // A bowed line from (MIDI, beat, beats) triples on the theme's grid.
        private static void ThemeLine(float[] x, SynthGen g, float[] line, in BowTone tone, float amp, int transpose)
        {
            for (int i = 0; i + 2 < line.Length; i += 3)
            {
                int at = (int)(line[i + 1] * ThemeBeat) + Human(g, 0.02f);
                float seconds = line[i + 2] * ThemeBeat / (float)Rate + 0.5f;
                MusicKit.Bow(x, at, Rate, Hz(line[i] + transpose), amp * g.Range(0.9f, 1.05f), seconds, tone, g);
            }
        }

        private static bool TuneStartsAt(float[] tune, float beat)
        {
            for (int i = 0; i + 2 < tune.Length; i += 3)
                if (Math.Abs(tune[i + 1] - beat) < 0.01f) return true;
            return false;
        }

        // The tune's note sounding at a beat (or the one it last left off on).
        private static int TuneAt(float[] tune, float beat)
        {
            int note = 69;
            for (int i = 0; i + 2 < tune.Length; i += 3)
                if (tune[i + 1] <= beat + 0.01f) note = (int)tune[i];
            return note;
        }

        // A chord tone at or below `limit` — the highest, or the next one down on alternate beats.
        private static int Below(int[] tones, int limit, int alternate)
        {
            int best = -1, second = -1;
            for (int i = 0; i < tones.Length; i++)
            {
                if (tones[i] > limit) break;
                second = best;
                best = tones[i];
            }
            return (alternate & 1) == 1 && second > 0 ? second : best;
        }
        #endregion
    }
}
