using System;

namespace CloneZ
{
    /// <summary>
    /// The Kettle's jukebox: four old records — a honky-tonk shuffle, a bluegrass breakdown, a country blues and a waltz —
    /// composed and played in code (engine-free: rendered on a background thread). A record keeps its tunes, its form and
    /// its band; every play is a fresh take (the band's timing, fills, slides, double stops), heard through a worn record
    /// and a small speaker: band-limited, a little saturated, wow, crackle, the needle dropping and lifting.
    /// </summary>
    internal static class MusicRecords
    {
        public const int Rate = 32000;
        /// <summary>How many records the jukebox holds.</summary>
        public const int Count = 4;

        private static readonly string[] s_Titles = { "Bardstown Shuffle", "Cumberland Breakdown", "Coal Town Blues", "Kentucky Snow Waltz" };

        /// <summary>A record's title.</summary>
        public static string Title(int record) => s_Titles[((record % Count) + Count) % Count];

        /// <summary>One take of a record: mono samples at <see cref="Rate"/>, needle drop to run-out, peak 0.8.</summary>
        public static float[] Render(int record, uint take)
        {
            switch (((record % Count) + Count) % Count)
            {
                case 0: return Shuffle(take);
                case 1: return Breakdown(take);
                case 2: return Blues(take);
                default: return Waltz(take);
            }
        }

        #region The band
        private static readonly int[] s_Major = { 0, 2, 4, 5, 7, 9, 11 };
        private static readonly int[] s_Blues = { 0, 3, 5, 6, 7, 10 };
        private const int Maj = 0, Dom = 1, Min = 2;
        private const float LeadIn = 0.9f;   // seconds of needle before the band comes in

        // A chord: its root in semitones over the key and its quality.
        private static int Ch(int root, int quality = Maj) => root + 100 * quality;
        private static int RootOf(int chord) => chord % 100;
        private static int[] TonesOf(int chord)
        {
            switch (chord / 100)
            {
                case Dom: return s_DomTones;
                case Min: return s_MinTones;
                default: return s_MajTones;
            }
        }
        private static readonly int[] s_MajTones = { 0, 4, 7 }, s_DomTones = { 0, 4, 7, 10 }, s_MinTones = { 0, 3, 7 };

        // One take: its tempo and swing, the key, the two buses the band plays into (the band, and the lead fiddle or
        // harmonica), and its randomness (the tunes come from the record's own fixed generators; the take's varies).
        private sealed class Take
        {
            public SynthGen G;
            public float Beat, Swing;
            public int Meter, Key;
            public float[] Band, Lead;

            public Take(int record, uint take, float bpm, int meter, float swing, int key, int bars)
            {
                G = MusicKit.Gen(Rate, take, record, 0x7A4Eu);
                Beat = 60f / bpm * G.Range(0.985f, 1.015f);
                Meter = meter;
                Swing = swing;
                Key = key;
                int n = (int)((LeadIn + bars * meter * Beat + 4.5f) * Rate);
                Band = new float[n];
                Lead = new float[n];
            }

            // The sample at a beat (counted from the band's first downbeat), swing applied to the off-beats.
            public int At(float beat, float jitter = 0.006f)
            {
                float whole = MathF.Floor(beat), f = beat - whole;
                f = f <= 0.5f ? f * 2f * Swing : Swing + (f - 0.5f) * 2f * (1f - Swing);
                return (int)((LeadIn + (whole + f) * Beat + G.Bipolar() * jitter) * Rate);
            }

            public float Seconds(float beats) => beats * Beat;
        }

        private static float Hz(float midi) => Dsp.Midi(midi);

        // The nearest chord tone to `midi`.
        private static int Snap(int midi, int key, int chord)
        {
            int[] tones = TonesOf(chord);
            int root = key + RootOf(chord), best = midi, bestD = 99;
            for (int o = -1; o <= 1; o++)
                for (int i = 0; i < tones.Length; i++)
                {
                    int m = root + tones[i] + 12 * ((midi - root) / 12 + o);
                    int d = Math.Abs(m - midi);
                    if (d < bestD) { bestD = d; best = m; }
                }
            return best;
        }

        // The nearest root or third of the chord: where a phrase comes to rest.
        private static int SnapHome(int midi, int key, int chord)
        {
            int root = key + RootOf(chord), third = TonesOf(chord)[1], best = midi, bestD = 99;
            for (int o = -2; o <= 2; o++)
            {
                int a = root + 12 * o, b = root + third + 12 * o;
                if (Math.Abs(a - midi) < bestD) { bestD = Math.Abs(a - midi); best = a; }
                if (Math.Abs(b - midi) < bestD) { bestD = Math.Abs(b - midi); best = b; }
            }
            return best;
        }

        // `steps` scale degrees from `midi` (it is first nudged onto the scale).
        private static int Step(int midi, int steps, int key, int[] scale)
        {
            int m = midi;
            while (!InScale(m, key, scale)) m--;
            int dir = Math.Sign(steps);
            for (int k = 0; k < Math.Abs(steps); k++)
            {
                do m += dir; while (!InScale(m, key, scale));
            }
            return m;
        }

        private static bool InScale(int midi, int key, int[] scale)
        {
            int pc = ((midi - key) % 12 + 12) % 12;
            for (int i = 0; i < scale.Length; i++) if (scale[i] == pc) return true;
            return false;
        }

        private static int Fold(int midi, int lo, int hi)
        {
            while (midi < lo) midi += 12;
            while (midi > hi) midi -= 12;
            return midi;
        }

        // The rhythm guitar: a bass note on the strong beats and a muted chord strum ("chick") between, boom-chick.
        private static void BoomChick(Take t, int[] chart, int bar0, int bars, float amp)
        {
            SynthGen g = t.G;
            for (int b = 0; b < bars; b++)
            {
                int chord = chart[b], root = t.Key + RootOf(chord);
                int[] tones = TonesOf(chord);
                var strum = new float[4];
                for (int i = 0; i < 4; i++) strum[i] = Hz(Fold(root + tones[i % tones.Length] + (i >= 3 ? 12 : 0), 52, 67));
                Array.Sort(strum);
                for (int k = 0; k < t.Meter; k++)
                {
                    float beat = (bar0 + b) * t.Meter + k;
                    bool boom = k == 0 || (t.Meter == 4 && k == 2);
                    if (boom)
                    {
                        int bass = Fold(root + (k == 0 ? 0 : 7), 40, 51);
                        MusicKit.Pluck(t.Band, t.At(beat), Rate, Hz(bass), amp * g.Range(0.9f, 1.02f), MusicKit.Rhythm, g, t.Seconds(1.8f), 0.12f);
                    }
                    else
                    {
                        MusicKit.Strum(t.Band, t.At(beat), Rate, strum, amp * 0.62f * g.Range(0.85f, 1f), MusicKit.Rhythm, g, true, 0.009f, 0.24f, 0.07f);
                        if (t.Meter == 4 && g.Chance(0.3f))
                            MusicKit.Strum(t.Band, t.At(beat + 0.5f), Rate, strum, amp * 0.4f, MusicKit.Rhythm, g, false, 0.008f, 0.16f, 0.06f);
                    }
                }
            }
        }

        // The upright bass: root and fifth on the strong beats, walking up into a change of chord.
        private static void UprightBass(Take t, int[] chart, int bar0, int bars, float amp, bool walkEveryBar = false)
        {
            SynthGen g = t.G;
            for (int b = 0; b < bars; b++)
            {
                int chord = chart[b], root = Fold(t.Key + RootOf(chord), 33, 44);
                int next = b + 1 < bars ? Fold(t.Key + RootOf(chart[b + 1]), 33, 44) : root;
                bool walk = walkEveryBar || (next != root && g.Chance(0.6f));
                for (int k = 0; k < t.Meter; k++)
                {
                    float beat = (bar0 + b) * t.Meter + k;
                    int note;
                    if (t.Meter == 3) { if (k != 0 && !(walk && k == 2)) continue; note = k == 0 ? root : next - 1; }
                    else if (walk) note = k == 0 ? root : k == 1 ? root + (TonesOf(chord)[1]) : k == 2 ? root + 7 : (next > root + 7 ? next - 1 : next + 1);
                    else
                    {
                        if (k == 1 || k == 3) continue;
                        note = k == 0 ? root : root + 7 > 44 ? root - 5 : root + 7;
                    }
                    float ring = t.Seconds(walk || t.Meter == 3 ? 0.9f : 1.8f);
                    MusicKit.Pluck(t.Band, t.At(beat, 0.004f), Rate, Hz(note), amp * (k == 0 ? 1f : 0.85f) * g.Range(0.9f, 1.05f), MusicKit.Bass, g, ring, 0.08f);
                }
            }
        }

        // Brushes on a snare: a swish on the backbeat and taps between (on the downbeat and taps in a waltz).
        private static void Brushes(Take t, int bar0, int bars, float amp)
        {
            SynthGen g = t.G;
            for (int b = 0; b < bars; b++)
            {
                for (int k = 0; k < t.Meter; k++)
                {
                    float beat = (bar0 + b) * t.Meter + k;
                    bool back = t.Meter == 3 ? k == 0 : (k & 1) == 1;
                    if (back) MusicKit.Brush(t.Band, t.At(beat, 0.004f), Rate, amp, t.Seconds(t.Meter == 3 ? 0.9f : 0.35f), 0.7f, g);
                    else MusicKit.Brush(t.Band, t.At(beat, 0.004f), Rate, amp * 0.45f, 0f, 0.6f, g);
                    if (t.Meter == 4 && g.Chance(0.65f)) MusicKit.Brush(t.Band, t.At(beat + 0.5f, 0.004f), Rate, amp * 0.32f, 0f, 0.5f, g);
                }
            }
        }

        // A tune for a stretch of the chart, four bars to a phrase: the record's own motif for each phrase (from its
        // tune generator), strong beats on chord tones, the last note of a phrase on the root or the third; a fiddle
        // (slides into long notes, a double stop now and then) or a harmonica (blues bends).
        private static void Tune(Take t, int[] chart, int bar0, int bars, int lo, int hi, float[][] cells, bool harp, float amp, float ornate, int salt)
        {
            SynthGen g = t.G;
            var tune = new SynthGen(Rate, MusicKit.Hash(0x7E11u, salt, 3u), 0);
            int[] scale = harp ? s_Blues : s_Major;
            int prev = Fold(t.Key + 7, lo, hi);
            for (int p = 0; p < bars; p += 4)
            {
                int[] cellOf = { tune.Int(0, cells.Length - 1), tune.Int(0, cells.Length - 1), tune.Int(0, cells.Length - 1), cells.Length - 1 };
                if (tune.Chance(0.4f)) cellOf[2] = cellOf[0];
                var steps = new int[16];
                for (int i = 0; i < steps.Length; i++) steps[i] = tune.Int(-2, 3) + (tune.Chance(0.15f) ? (tune.Chance(0.5f) ? 2 : -2) : 0);
                int si = 0;
                for (int b = p; b < Math.Min(bars, p + 4); b++)
                {
                    int chord = chart[b];
                    float[] cell = cells[cellOf[b - p]];
                    for (int i = 0; i + 1 < cell.Length; i += 2)
                    {
                        float pos = cell[i], len = cell[i + 1];
                        if (len <= 0f) continue;
                        int m = Step(prev, steps[si++ & 15], t.Key, scale);
                        bool strong = pos == 0f || (t.Meter == 4 && pos == 2f) || len >= 1.5f;
                        bool last = b == p + 3 && i + 2 >= cell.Length;
                        if (last) m = SnapHome(m, t.Key, chord);
                        else if (strong) m = Snap(m, t.Key, chord);
                        if (m > hi) m -= 12;
                        if (m < lo) m += 12;
                        prev = m;
                        float beat = (bar0 + b) * t.Meter + pos;
                        float seconds = t.Seconds(len) * 0.95f + 0.06f;
                        float a = amp * (strong ? 1f : 0.85f) * g.Range(0.9f, 1.05f);
                        if (harp)
                        {
                            bool bend = len >= 0.5f && g.Chance(0.35f);
                            MusicKit.Reed(t.Lead, t.At(beat), Rate, Hz(m), a, seconds, bend ? -g.Range(80f, 180f) : 0f, Math.Min(0.18f, seconds * 0.5f), g);
                        }
                        else
                        {
                            float from = len >= 1f && g.Chance(ornate) ? Hz(m - (g.Chance(0.5f) ? 1 : 2)) : 0f;
                            MusicKit.Bow(t.Lead, t.At(beat), Rate, Hz(m), a, seconds, MusicKit.Fiddle, g, from, 0.07f);
                            if (len >= 1.5f && g.Chance(ornate * 0.8f))
                                MusicKit.Bow(t.Lead, t.At(beat), Rate, Hz(Snap(m - 4, t.Key, chord)), a * 0.55f, seconds, MusicKit.Fiddle, g);
                        }
                    }
                }
            }
        }

        // The last chord of the record, rung out (a strum, the bass, a long note on top).
        private static void Ending(Take t, int bar, float amp, int top)
        {
            SynthGen g = t.G;
            float beat = bar * t.Meter;
            int root = t.Key;
            float[] chord = { Hz(Fold(root, 40, 51)), Hz(Fold(root + 7, 45, 56)), Hz(Fold(root + 12, 50, 62)), Hz(Fold(root + 16, 55, 66)), Hz(Fold(root + 19, 60, 70)) };
            MusicKit.Strum(t.Band, t.At(beat), Rate, chord, amp, MusicKit.Rhythm, g, true, 0.03f, 30f, 0.1f);
            MusicKit.Pluck(t.Band, t.At(beat), Rate, Hz(Fold(root, 33, 44)), 0.6f, MusicKit.Bass, g);
            MusicKit.Bow(t.Lead, t.At(beat), Rate, Hz(top), 0.3f, t.Seconds(t.Meter * 1.5f), MusicKit.Fiddle, g);
            MusicKit.Brush(t.Band, t.At(beat), Rate, 0.5f, t.Seconds(1.5f), 0.8f, g);
        }
        #endregion

        #region Records
        // One bar's rhythm for a tune: (beat, beats) pairs. The last cell of each set is a phrase's resting bar.
        private static readonly float[][] s_Country =
        {
            new[] { 0f, 1f, 1f, 0.5f, 1.5f, 0.5f, 2f, 1f, 3f, 1f }, new[] { 0f, 0.5f, 0.5f, 0.5f, 1f, 0.5f, 1.5f, 0.5f, 2f, 2f },
            new[] { 0f, 1.5f, 1.5f, 0.5f, 2f, 2f }, new[] { 0f, 2f, 2f, 1f, 3f, 1f }, new[] { 0f, 0.5f, 0.5f, 0.5f, 1f, 1f, 2f, 0.5f, 2.5f, 0.5f, 3f, 1f },
            new[] { 0f, 3f, 3f, 1f }, new[] { 0f, 4f },
        };
        private static readonly float[][] s_Fast =
        {
            new[] { 0f, 0.5f, 0.5f, 0.5f, 1f, 0.5f, 1.5f, 0.5f, 2f, 0.5f, 2.5f, 0.5f, 3f, 0.5f, 3.5f, 0.5f },
            new[] { 0f, 1f, 1f, 0.5f, 1.5f, 0.5f, 2f, 0.5f, 2.5f, 0.5f, 3f, 1f }, new[] { 0f, 0.5f, 0.5f, 0.5f, 1f, 1f, 2f, 0.5f, 2.5f, 0.5f, 3f, 0.5f, 3.5f, 0.5f },
            new[] { 0f, 1.5f, 1.5f, 0.5f, 2f, 0.5f, 2.5f, 0.5f, 3f, 1f }, new[] { 0f, 2f, 2f, 1f, 3f, 1f },
        };
        private static readonly float[][] s_Harp =
        {
            new[] { 0f, 1f, 1.5f, 0.5f, 2f, 2f }, new[] { 0.5f, 0.5f, 1f, 1f, 2f, 1.5f }, new[] { 0f, 0.5f, 0.5f, 0.5f, 1f, 1f },
            new[] { 0f, 2.5f, 3f, 0.5f, 3.5f, 0.5f }, new[] { 1f, 0.5f, 1.5f, 0.5f, 2f, 2f }, new[] { 0f, 3f },
        };
        private static readonly float[][] s_Waltz =
        {
            new[] { 0f, 2f, 2f, 1f }, new[] { 0f, 1f, 1f, 1f, 2f, 1f }, new[] { 0f, 1.5f, 1.5f, 0.5f, 2f, 1f },
            new[] { 0f, 1f, 1f, 0.5f, 1.5f, 0.5f, 2f, 1f }, new[] { 0f, 3f },
        };

        private static int[] Join(params int[][] parts)
        {
            int n = 0;
            foreach (int[] p in parts) n += p.Length;
            var o = new int[n];
            int at = 0;
            foreach (int[] p in parts)
            {
                Array.Copy(p, 0, o, at, p.Length);
                at += p.Length;
            }
            return o;
        }

        private static int[] Part(int[] chart, int from, int count)
        {
            var o = new int[count];
            Array.Copy(chart, from, o, 0, count);
            return o;
        }

        // "Bardstown Shuffle": honky-tonk in G, a shuffle — boom-chick guitar, upright bass walking into the changes,
        // brushes on the backbeat, a fiddle kicking off and taking the tune, the second half higher and fancier.
        private static float[] Shuffle(uint take)
        {
            int I = Ch(0), IV = Ch(5), V7 = Ch(7, Dom);
            int[] verse = { I, I, IV, IV, I, I, V7, V7, I, I, IV, IV, I, V7, I, I };
            int[] chart = Join(new[] { I, I, V7, V7 }, verse, Part(verse, 8, 8), new[] { V7, V7 });
            var t = new Take(0, take, 116f, 4, 0.62f, 55, chart.Length + 1);
            BoomChick(t, chart, 0, chart.Length, 0.42f);
            UprightBass(t, chart, 0, chart.Length, 0.6f);
            Brushes(t, 0, chart.Length, 0.3f);
            Tune(t, Part(chart, 2, 2), 2, 2, 62, 79, s_Country, false, 0.2f, 0.3f, 10);
            Tune(t, verse, 4, 16, 62, 79, s_Country, false, 0.22f, 0.3f, 11);
            Tune(t, Part(verse, 8, 8), 20, 8, 67, 84, s_Country, false, 0.22f, 0.6f, 12);
            Ending(t, chart.Length, 0.45f, 71);
            return Press(t, chart.Length);
        }

        // "Cumberland Breakdown": bluegrass in A — the fiddle kicks it off and takes the first time through, then the
        // banjo takes a break in rolls while the fiddle chops the backbeat; guitar boom-chick and bass on one and three.
        private static float[] Breakdown(uint take)
        {
            int I = Ch(0), IV = Ch(5), V = Ch(7);
            int[] verse = { I, I, IV, I, I, I, V, V, I, I, IV, I, V, V, I, I };
            int[] chart = Join(new[] { V, V }, verse, verse, new[] { V, V });
            var t = new Take(1, take, 128f, 4, 0.52f, 57, chart.Length + 1);
            BoomChick(t, chart, 0, chart.Length, 0.38f);
            UprightBass(t, chart, 0, chart.Length, 0.58f);
            Tune(t, Part(chart, 0, 2), 0, 2, 64, 81, s_Fast, false, 0.2f, 0.2f, 20);
            Tune(t, verse, 2, 16, 64, 81, s_Fast, false, 0.21f, 0.25f, 21);
            Rolls(t, verse, 2, 16, 0.1f, false);
            Rolls(t, verse, 18, 16, 0.27f, true);
            Chops(t, verse, 18, 16, 0.1f);
            Tune(t, new[] { V, V }, 34, 2, 64, 81, s_Fast, false, 0.2f, 0.3f, 22);
            Ending(t, chart.Length, 0.42f, 69);
            return Press(t, chart.Length);
        }

        // "Coal Town Blues": a slow shuffle in E — the guitar's boogie on the low strings, a walking bass, brushes,
        // a harmonica bending its way through two choruses.
        private static float[] Blues(uint take)
        {
            int I = Ch(0, Dom), IV = Ch(5, Dom), V = Ch(7, Dom);
            int[] chorus = { I, I, I, I, IV, IV, I, I, V, IV, I, V };
            int[] chart = Join(new[] { I, V }, chorus, chorus);
            var t = new Take(2, take, 92f, 4, 0.66f, 52, chart.Length + 1);
            Boogie(t, chart, 0, chart.Length, 0.36f);
            UprightBass(t, chart, 0, chart.Length, 0.58f, true);
            Brushes(t, 0, chart.Length, 0.28f);
            Tune(t, chorus, 2, 12, 64, 79, s_Harp, true, 0.2f, 0f, 30);
            Tune(t, chorus, 14, 12, 64, 83, s_Harp, true, 0.22f, 0f, 31);
            Ending(t, chart.Length, 0.4f, 68);
            return Press(t, chart.Length);
        }

        // "Kentucky Snow Waltz": a waltz in D — boom-chick-chick, the bass on the downbeat, a brush swirl, the fiddle
        // singing the tune and then again higher with double stops.
        private static float[] Waltz(uint take)
        {
            int I = Ch(0), IV = Ch(5), V7 = Ch(7, Dom);
            int[] verse = { I, I, IV, I, I, I, V7, V7, I, I, IV, IV, I, V7, I, I };
            int[] chart = Join(new[] { I, IV, V7, V7 }, verse, verse, new[] { V7, V7 });
            var t = new Take(3, take, 104f, 3, 0.5f, 50, chart.Length + 1);
            BoomChick(t, chart, 0, chart.Length, 0.4f);
            UprightBass(t, chart, 0, chart.Length, 0.58f);
            Brushes(t, 0, chart.Length, 0.26f);
            Tune(t, Part(chart, 0, 4), 0, 4, 62, 76, s_Waltz, false, 0.16f, 0.3f, 40);
            Tune(t, verse, 4, 16, 62, 79, s_Waltz, false, 0.22f, 0.4f, 41);
            Tune(t, verse, 20, 16, 66, 83, s_Waltz, false, 0.22f, 0.75f, 42);
            Ending(t, chart.Length, 0.42f, 66);
            return Press(t, chart.Length);
        }

        // The blues guitar's boogie: palm-muted root-and-fifth, root-and-sixth on the swung eighths.
        private static void Boogie(Take t, int[] chart, int bar0, int bars, float amp)
        {
            SynthGen g = t.G;
            for (int b = 0; b < bars; b++)
            {
                int root = Fold(t.Key + RootOf(chart[b]), 40, 47);
                for (int k = 0; k < 8; k++)
                {
                    float beat = (bar0 + b) * 4 + k * 0.5f;
                    int upper = root + ((k >> 1) % 2 == 0 ? 7 : 9);
                    float a = amp * ((k & 1) == 0 ? 1f : 0.7f) * g.Range(0.9f, 1.05f);
                    int at = t.At(beat, 0.004f);
                    MusicKit.Pluck(t.Band, at, Rate, Hz(root), a, MusicKit.Rhythm, g, 0.2f, 0.05f);
                    MusicKit.Pluck(t.Band, at + (int)(0.006f * Rate), Rate, Hz(upper), a * 0.8f, MusicKit.Rhythm, g, 0.2f, 0.05f);
                }
            }
        }

        // The banjo: forward rolls over the chord, the thumb coming back to the high drone string; in a break the top
        // notes carry a tune.
        private static void Rolls(Take t, int[] chart, int bar0, int bars, float amp, bool lead)
        {
            SynthGen g = t.G;
            int drone = Fold(t.Key + 12, 64, 71), top = Fold(t.Key + 16, 67, 74);
            int[] fingers = { 0, 1, 2, 3, 1, 2, 3, 2 };
            for (int b = 0; b < bars; b++)
            {
                int chord = chart[b], root = t.Key + RootOf(chord);
                int[] tones = TonesOf(chord);
                int low = Fold(root + tones[2 % tones.Length], 55, 62), mid = Fold(root + tones[1], 62, 68);
                if (lead) top = Snap(Step(top, g.Int(-2, 3), t.Key, s_Major), t.Key, chord);
                else top = Fold(root + tones[0] + 12, 67, 74);
                if (top > 79) top -= 12;
                if (top < 64) top += 12;
                for (int k = 0; k < 8; k++)
                {
                    int f = fingers[k];
                    int note = f == 0 ? low : f == 1 ? mid : f == 2 ? top : drone;
                    float a = amp * (k == 0 || k == 4 ? 1f : 0.82f) * g.Range(0.88f, 1.05f);
                    MusicKit.Pluck(t.Band, t.At((bar0 + b) * 4 + k * 0.5f, 0.004f), Rate, Hz(note), a, MusicKit.Banjo, g, 0.7f, 0.25f);
                }
            }
        }

        // The fiddle's backup chop: a scrape of the bow on two and four.
        private static void Chops(Take t, int[] chart, int bar0, int bars, float amp)
        {
            SynthGen g = t.G;
            var chop = new BowTone(0.004f, 0.04f, 0f, 5f, 0.45f, 3000f, 1);
            for (int b = 0; b < bars; b++)
            {
                int root = t.Key + RootOf(chart[b]);
                for (int k = 1; k < 4; k += 2)
                {
                    int at = t.At((bar0 + b) * 4 + k, 0.004f);
                    MusicKit.Bow(t.Lead, at, Rate, Hz(Fold(root + 7, 62, 69)), amp, 0.08f, chop, g);
                    MusicKit.Bow(t.Lead, at, Rate, Hz(Fold(root + 12, 64, 72)), amp * 0.8f, 0.08f, chop, g);
                }
            }
        }

        // The pressing and the playing: the band and the lead mixed in a small room, then a worn record through a small
        // speaker — band-limited, a little driven, wow, surface noise and crackle, the needle dropping and lifting.
        private static float[] Press(Take t, int endBar)
        {
            SynthGen g = t.G;
            float[] band = t.Band, lead = t.Lead;
            Dsp.HP(lead, 200f, Rate);
            Dsp.PeakEq(lead, 290f, 1.4f, 3f, Rate);
            Dsp.PeakEq(lead, 2750f, 1.1f, 3f, Rate);
            for (int i = 0; i < band.Length; i++) band[i] += lead[i];
            Room(band, g);

            Dsp.HP(band, 110f, Rate);
            Dsp.HP(band, 110f, Rate);
            Dsp.LP(band, 4800f, Rate);
            Dsp.LP(band, 5400f, Rate);
            Dsp.PeakEq(band, 1700f, 0.9f, 3f, Rate);
            Dsp.Norm(band);
            Dsp.Saturate(band, 1.5f);

            // Wow (the record a touch off-centre, about 45 rpm) and a little flutter, written into the lead's buffer.
            float[] o = lead;
            float wowHz = g.Range(0.7f, 0.8f), wow = g.Range(10f, 16f), flutter = g.Range(0.4f, 0.8f);
            float wp = g.Next() * MusicKit.TwoPi;
            for (int i = 0; i < o.Length; i++)
            {
                float time = i / (float)Rate;
                float pos = i + wow * MathF.Sin(MusicKit.TwoPi * wowHz * time + wp) + flutter * MathF.Sin(MusicKit.TwoPi * 6.3f * time);
                o[i] = Dsp.ReadLerp(band, pos);
            }

            int drop = (int)(0.25f * Rate), music = (int)(LeadIn * Rate);
            int end = Math.Min(o.Length, t.At(endBar * t.Meter, 0f) + (int)(3.6f * Rate));
            int lift = Math.Min(o.Length - 1, end + (int)(1.6f * Rate));
            Surface(o, g, drop, lift);
            for (int i = 0; i < music && i < o.Length; i++) o[i] *= Dsp.SmoothStep((i - drop) / (float)(music - drop));
            // The band's last chord fades under the run-out; then the needle lifts and the rest is silence.
            for (int i = end; i < o.Length; i++)
            {
                float k = i < lift ? 1f - (i - end) / (float)(lift - end) : 0f;
                o[i] = o[i] * k * k + (i < lift ? RunOut(g, i - end) : 0f);
            }
            MusicKit.Drum(o, drop, Rate, 0.18f, 70f, 40f, 0.01f, 0.03f, 0.3f, 900f, 0.8f, 0.02f, 0.5f, g);
            MusicKit.Drum(o, lift, Rate, 0.1f, 90f, 50f, 0.01f, 0.02f, 0.3f, 1200f, 0.8f, 0.015f, 0.4f, g);
            int len = Math.Min(o.Length, lift + (int)(0.4f * Rate));
            var outBuf = new float[len];
            Array.Copy(o, outBuf, len);
            Dsp.RemoveMean(outBuf);
            Dsp.Norm(outBuf, 0.8f);
            return outBuf;
        }

        // A record's small room: four damped delay lines, in place.
        private static void Room(float[] x, SynthGen g)
        {
            var s = new StereoBuf(x, x);
            MusicKit.Space(s, Rate, 0.8f, 3800f, 0.12f, 0.05f, 0.004f, 0.5f);
        }

        // Surface noise between the needle's drop and its lift: a soft hiss and crackle.
        private static void Surface(float[] x, SynthGen g, int from, int to)
        {
            uint rs = g.NextUInt() | 1u;
            float z = 0f, k = MathF.Exp(-MusicKit.TwoPi * 3500f / Rate);
            for (int i = from; i < to && i < x.Length; i++)
            {
                rs ^= rs << 13;
                rs ^= rs >> 17;
                rs ^= rs << 5;
                float w = (int)rs * 4.656613e-10f;
                z = w + (z - w) * k;
                x[i] += z * 0.012f;
            }
            int clicks = (int)((to - from) / (float)Rate * g.Range(4f, 7f));
            for (int c = 0; c < clicks; c++)
            {
                int at = g.Int(from, Math.Max(from + 1, to));
                float a = 0.25f * MathF.Pow(g.Next(), 3f) + 0.01f, d = MathF.Exp(-1f / (g.Range(0.0002f, 0.0012f) * Rate));
                for (int i = 0; i < Rate / 200 && at + i < x.Length; i++)
                {
                    x[at + i] += g.Bipolar() * a;
                    a *= d;
                }
            }
        }

        // The run-out groove: a soft swish and tick once a turn (45 rpm).
        private static float RunOut(SynthGen g, int i)
        {
            float time = i / (float)Rate, turn = time * 0.75f;
            float ph = turn - MathF.Floor(turn);
            return g.Bipolar() * 0.02f * MathF.Exp(-ph * 40f) + g.Bipolar() * 0.004f;
        }
        #endregion
    }
}
