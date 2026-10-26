using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace CloneZ
{
    /// <summary>
    /// Offline previews of the score for the sound tools (never called by the game): each layer over a few segments at
    /// day and at night, the score held at a few intensities (calm, hunted, a fight, an emission), an explore → tension →
    /// combat → explore sequence mixed the way the director mixes it, the title theme, every jukebox record and every
    /// stinger — as WAVs, with a spectrogram beside each when the tools provide a painter, and a table of levels, render
    /// times and seam checks (music.md), after a few measured checks of the kit (string tuning, seamless drones and room).
    /// <c>Bench</c> times the kit's pieces.
    /// </summary>
    internal static class MusicPreview
    {
        // The offline tools' spectrogram painter (static void Draw(string path, float[] mono, int rate)), when present.
        private static readonly MethodInfo s_Spectro =
            typeof(MusicPreview).Assembly.GetType("Spectro")?.GetMethod("Draw", BindingFlags.Public | BindingFlags.Static);

        /// <summary>Writes preview WAVs of the score into <paramref name="outDir"/>.</summary>
        internal static void Render(string outDir)
        {
            Directory.CreateDirectory(outDir);
            var table = new StringBuilder();
            table.AppendLine("| preview | seconds | peak dB | rms dB | loudest 1 s dB | worst render ms/segment | real-time % | seam |");
            table.AppendLine("|---|---|---|---|---|---|---|---|");
            const uint seed = 0x5EEDCAFEu;

            Checks(table);
            Layer(outDir, table, "explore_day", MusicPart.Explore, seed, 40, 5, false);
            Layer(outDir, table, "explore_night", MusicPart.Explore, seed + 1, 80, 5, true);
            Layer(outDir, table, "tension_day", MusicPart.Tension, seed, 40, 3, false);
            Layer(outDir, table, "tension_night", MusicPart.Tension, seed + 1, 80, 3, true);
            Layer(outDir, table, "combat", MusicPart.Combat, seed, 40, 3, false);
            Layer(outDir, table, "dread", MusicPart.Dread, seed, 40, 3, false);
            Layer(outDir, table, "theme", MusicPart.Theme, seed, 0, MusicScore.ThemeSections, false);
            Sequence(outDir, table, seed + 7);
            Intensity(outDir, table, "mix_calm", seed + 11, 1f, 0f, 0f, 0f);
            Intensity(outDir, table, "mix_hunted", seed + 11, 0.4f, 1f, 0f, 0f);
            Intensity(outDir, table, "mix_fight", seed + 11, 0.4f, 1f, 1f, 0f);
            Intensity(outDir, table, "mix_emission", seed + 11, 0.3f, 0.3f, 0f, 1f);
            for (int r = 0; r < MusicRecords.Count; r++)
            {
                var sw = Stopwatch.StartNew();
                float[] record = MusicRecords.Render(r, 0xA5u + (uint)r);
                double ms = sw.Elapsed.TotalMilliseconds;
                string name = "record_" + r + "_" + MusicRecords.Title(r).ToLowerInvariant().Replace(' ', '_');
                Write(outDir, table, name, new StereoBuf(record, record), MusicRecords.Rate, ms, record.Length, 1);
            }
            var synth = new SfxSynth();
            foreach (string id in synth.Ids)
            {
                if (!id.StartsWith("mus_", StringComparison.Ordinal)) continue;
                var sw = Stopwatch.StartNew();
                float[] data = synth.RenderSamples(id, 0, out int ch, out int rate);
                double ms = sw.Elapsed.TotalMilliseconds;
                var s = new StereoBuf(data.Length / ch);
                for (int i = 0; i < s.Length; i++)
                {
                    s.L[i] = data[i * ch];
                    s.R[i] = data[i * ch + ch - 1];
                }
                Write(outDir, table, "stinger_" + id.Substring(4), s, rate, ms, s.Length, 1);
            }

            File.WriteAllText(Path.Combine(outDir, "music.md"), table.ToString());
            Console.WriteLine(table.ToString());
        }

        // Consecutive segments of one part overlap-added, exactly as the director schedules them back to back.
        private static void Layer(string dir, StringBuilder table, string name, MusicPart part, uint seed, long first, int count, bool night)
        {
            int step = MusicScore.SegmentSamples(part), len = MusicScore.LengthSamples(part);
            var mix = new StereoBuf(step * (count - 1) + len);
            double worst = 0;
            for (int k = 0; k < count; k++)
            {
                var cue = new MusicCue { Part = part, Seed = seed, Index = first + k, Night = night, PhrasePos = k, Last = part == MusicPart.Explore && k == count - 1 };
                var sw = Stopwatch.StartNew();
                StereoBuf s = MusicScore.Render(cue);
                worst = Math.Max(worst, sw.Elapsed.TotalMilliseconds);
                Dsp.Mix(mix.L, s.L, k * step);
                Dsp.Mix(mix.R, s.R, k * step);
            }
            Write(dir, table, name, mix, MusicScore.Rate, worst, step, count);
        }

        // Explore, then something finds you, then a fight, then it fades back to exploring — with the director's fades.
        private static void Sequence(string dir, StringBuilder table, uint seed)
        {
            const int count = 10;
            int step = MusicScore.Segment, len = MusicScore.LengthSamples(MusicPart.Explore), rate = MusicScore.Rate;
            var lanes = new StereoBuf[3];
            for (int l = 0; l < 3; l++) lanes[l] = new StereoBuf(step * (count - 1) + len);
            double worst = 0;
            for (int k = 0; k < count; k++)
            {
                for (int l = 0; l < 3; l++)
                {
                    var cue = new MusicCue { Part = (MusicPart)l, Seed = seed, Index = 300 + k, PhrasePos = k, Last = l == 0 && k == count - 1 };
                    var sw = Stopwatch.StartNew();
                    StereoBuf s = MusicScore.Render(cue);
                    worst = Math.Max(worst, sw.Elapsed.TotalMilliseconds);
                    Dsp.Mix(lanes[l].L, s.L, k * step);
                    Dsp.Mix(lanes[l].R, s.R, k * step);
                }
            }
            var mix = new StereoBuf(lanes[0].Length);
            float explore = 1f, tension = 0f, combat = 0f, dt = 1f / rate;
            for (int i = 0; i < mix.Length; i++)
            {
                float t = i * dt;
                float threat = t > 40f && t < 84f ? 1f : 0f, fight = t > 52f && t < 72f ? 1f : 0f;
                tension = Approach(tension, Math.Max(threat, fight * 0.9f), 1.2f, 0.07f, dt);
                combat = Approach(combat, fight, 1.4f, 0.08f, dt);
                explore = Approach(explore, 1f - 0.65f * Math.Max(tension, combat), 0.5f, 0.6f, dt);
                mix.L[i] = 0.72f * explore * lanes[0].L[i] + 0.7f * tension * lanes[1].L[i] + 0.7f * combat * lanes[2].L[i];
                mix.R[i] = 0.72f * explore * lanes[0].R[i] + 0.7f * tension * lanes[1].R[i] + 0.7f * combat * lanes[2].R[i];
            }
            Write(dir, table, "sequence", mix, rate, worst, step, count);
        }

        private static float Approach(float v, float target, float up, float down, float dt)
            => target > v ? Math.Min(target, v + up * dt) : Math.Max(target, v - down * dt);

        // Three segments of the score held at one intensity: each layer at the gain the director gives it there
        // (times the director's per-layer volume).
        private static void Intensity(string dir, StringBuilder table, string name, uint seed, float explore, float tension, float combat, float dread)
        {
            const int count = 3;
            int step = MusicScore.Segment, len = MusicScore.LengthSamples(MusicPart.Explore);
            var mix = new StereoBuf(step * (count - 1) + len);
            float[] gains = { explore * 0.72f, tension * 0.7f, combat * 0.7f, dread * 0.72f };
            double worst = 0;
            for (int k = 0; k < count; k++)
            {
                for (int l = 0; l < 4; l++)
                {
                    if (gains[l] <= 0f) continue;
                    var cue = new MusicCue { Part = (MusicPart)l, Seed = seed, Index = 500 + k, PhrasePos = k + 1 };
                    var sw = Stopwatch.StartNew();
                    StereoBuf s = MusicScore.Render(cue);
                    worst = Math.Max(worst, sw.Elapsed.TotalMilliseconds);
                    Dsp.Mix(mix.L, s.L, k * step, gains[l]);
                    Dsp.Mix(mix.R, s.R, k * step, gains[l]);
                }
            }
            Write(dir, table, name, mix, MusicScore.Rate, worst, step, count);
        }

        private static void Write(string dir, StringBuilder table, string name, StereoBuf s, int rate, double worstMs, int step, int count)
        {
            WavFile.Write(Path.Combine(dir, name + ".wav"), MusicKit.Interleave(s), 2, rate);
            var mono = new float[s.Length];
            for (int i = 0; i < mono.Length; i++) mono[i] = 0.5f * (s.L[i] + s.R[i]);
            s_Spectro?.Invoke(null, new object[] { Path.Combine(dir, name + ".png"), mono, rate });
            double seam = SeamJump(mono, step, count);
            float peak = Math.Max(Dsp.Peak(s.L), Dsp.Peak(s.R));
            double realtime = step > 0 ? worstMs / (step * 1000.0 / rate) * 100.0 : 0.0;
            table.AppendLine($"| {name} | {s.Length / (double)rate:0.0} | {Db(peak):0.0} | {Db(Dsp.Rms(mono)):0.0} | {Db(Loudest(mono, rate)):0.0} | {worstMs:0} | {realtime:0.0} | {seam:0.0} |");
        }

        // The worst sample-to-sample step within 3 ms of a segment boundary (or a ring-out's end), relative to the
        // signal's typical step: a click at a seam stands out as a big number.
        private static double SeamJump(float[] x, int step, int count)
        {
            double typical = 0;
            for (int i = 1; i < x.Length; i++) typical += Math.Abs(x[i] - x[i - 1]);
            typical /= Math.Max(1, x.Length - 1);
            if (typical < 1e-9) return 0;
            int w = 96;
            double worst = 0;
            for (int k = 1; k <= count; k++)
            {
                foreach (int at in new[] { k * step, k * step + MusicScore.Tail })
                {
                    for (int i = Math.Max(1, at - w); i < Math.Min(x.Length, at + w); i++)
                        worst = Math.Max(worst, Math.Abs(x[i] - x[i - 1]));
                }
            }
            return worst / typical;
        }

        private static float Loudest(float[] x, int rate)
        {
            float best = 0f;
            for (int at = 0; at + rate <= x.Length; at += rate / 4)
            {
                double s = 0;
                for (int i = 0; i < rate; i++) s += x[at + i] * x[at + i];
                best = Math.Max(best, (float)Math.Sqrt(s / rate));
            }
            return best;
        }

        private static double Db(double v) => 20.0 * Math.Log10(Math.Max(v, 1e-9));

        /// <summary>Times the kit's pieces (for tuning the render budget).</summary>
        internal static void Bench(string outDir)
        {
            int rate = MusicScore.Rate;
            var g = new SynthGen(rate, 99u, 0);
            var x = new float[rate * 9];
            var s = new StereoBuf(rate * 9);
            for (int pass = 0; pass < 2; pass++)
            {
                var sw = Stopwatch.StartNew();
                MusicKit.Bow(x, 0, rate, 146.8f, 0.1f, 5.8f, MusicKit.Section, g);
                double bow = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                MusicKit.Bow(x, 0, rate, 146.8f, 0.1f, 5.8f, MusicKit.Fiddle, g);
                double fiddle = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                MusicKit.Choir(x, 0, rate, 146.8f, 0.1f, 5.6f, g, 20f);
                double choir = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                MusicKit.Space(s, rate, 3f, 4000f, 0.4f, 0.14f, 0.02f);
                double space = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                for (int i = 0; i < 10; i++) MusicKit.Pluck(x, i * 1000, rate, 220f, 0.4f, MusicKit.Guitar, g);
                double pluck = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                MusicKit.Taiko(x, 0, rate, 0.8f, 1.2f, g);
                double taiko = sw.Elapsed.TotalMilliseconds;
                sw.Restart();
                MusicKit.Drone(s, rate, 100.0, 146.8f, 0.1f, 800f, 3, 9f, 0.5f, 0.05f, 0.2f, 0.5f, 2f, 7f, 2f);
                double drone = sw.Elapsed.TotalMilliseconds;
                Console.WriteLine($"bow(section 3v 5.8s) {bow:0.0} ms, bow(fiddle 1v) {fiddle:0.0}, choir 5.6s {choir:0.0}, space 9s {space:0.0}, 10 plucks {pluck:0.0}, taiko {taiko:0.0}, drone 3v 9s {drone:0.0}");
            }
        }

        // The kit's promises, measured: plucked strings in tune, a drone and the room seamless across segments, and
        // how loud the room's wet signal comes out.
        private static void Checks(StringBuilder table)
        {
            int rate = MusicScore.Rate;
            var sb = new StringBuilder("Checks:");
            var g = new SynthGen(rate, 12345u, 0);
            double worstCents = 0;
            string worstAt = "";
            foreach (int midi in new[] { 38, 45, 50, 57, 62, 69, 74, 81, 86 })
            {
                foreach (StringTone tone in new[] { MusicKit.Guitar, MusicKit.Banjo, MusicKit.Dulcimer })
                {
                    var x = new float[rate];
                    float hz = Dsp.Midi(midi);
                    MusicKit.Pluck(x, 0, rate, hz, 0.5f, tone, g);
                    double c = Math.Abs(Cents(x, rate, hz));
                    if (c > worstCents)
                    {
                        worstCents = c;
                        worstAt = " (MIDI " + midi + ")";
                    }
                }
            }
            sb.Append($" worst pluck tuning {worstCents:0.0} cents{worstAt};");

            // A drone over two segments, rendered whole and as two windowed halves (each through the room), compared.
            int seg = MusicScore.Segment;
            float segS = seg / (float)rate, overlap = 2f;
            int len = seg + 3 * rate;
            var whole = new StereoBuf(seg * 2 + 3 * rate);
            MusicKit.Drone(whole, rate, 0.0, 146.83f, 0.2f, 1000f, 3, 9f, 0.5f, 0.05f, 0.3f, 0.5f, overlap, 2f * segS, overlap);
            var a = new StereoBuf(len);
            var b = new StereoBuf(len);
            MusicKit.Drone(a, rate, 0.0, 146.83f, 0.2f, 1000f, 3, 9f, 0.5f, 0.05f, 0.3f, 0.5f, overlap, segS, overlap);
            MusicKit.Drone(b, rate, segS, 146.83f, 0.2f, 1000f, 3, 9f, 0.5f, 0.05f, 0.3f, 0.5f, overlap, segS, overlap);
            sb.Append($" drone seam error {Db(SeamError(whole, a, b, seg, rate)):0} dB;");
            MusicKit.Space(whole, rate, 2.6f, 4800f, 0.32f, 0.16f, 0.022f);
            MusicKit.Space(a, rate, 2.6f, 4800f, 0.32f, 0.16f, 0.022f);
            MusicKit.Space(b, rate, 2.6f, 4800f, 0.32f, 0.16f, 0.022f);
            sb.Append($" with the room {Db(SeamError(whole, a, b, seg, rate)):0} dB;");

            // The room's wet level for noise in (dB re the dry signal).
            var noise = new StereoBuf(Dsp.White(g, rate * 4), null);
            var dry = (float[])noise.L.Clone();
            MusicKit.Space(noise, rate, 2.6f, 4800f, 0.32f, 0f, 0.022f);
            for (int i = 0; i < dry.Length; i++) noise.L[i] -= dry[i];
            sb.Append($" room wet at 0.32: {Db(Dsp.Rms(noise.L) / Math.Max(1e-9f, Dsp.Rms(dry))):0.0} dB.");
            table.AppendLine(sb.ToString()).AppendLine();
        }

        // The largest difference between one long render and two segments overlap-added, after the first fade-in and
        // before the last fade-out, relative to the signal's peak.
        private static double SeamError(StereoBuf whole, StereoBuf a, StereoBuf b, int seg, int rate)
        {
            double worst = 0, peak = 1e-9;
            for (int i = 2 * rate; i < 2 * seg - rate; i++)
            {
                float l = (i < a.Length ? a.L[i] : 0f) + (i >= seg && i - seg < b.Length ? b.L[i - seg] : 0f);
                worst = Math.Max(worst, Math.Abs(l - whole.L[i]));
                peak = Math.Max(peak, Math.Abs(whole.L[i]));
            }
            return worst / peak;
        }

        // How far a tone sits from hz, in cents (the strongest of a fine Goertzel scan ±60 cents).
        private static double Cents(float[] x, int rate, float hz)
        {
            double best = -1, bestC = 0;
            for (int c = -60; c <= 60; c++)
            {
                double f = hz * Math.Pow(2.0, c / 1200.0), w = 2 * Math.PI * f / rate, k = 2 * Math.Cos(w), s1 = 0, s2 = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    double s0 = x[i] + k * s1 - s2;
                    s2 = s1;
                    s1 = s0;
                }
                double p = s1 * s1 + s2 * s2 - k * s1 * s2;
                if (p > best) { best = p; bestC = c; }
            }
            return bestC;
        }
    }
}
