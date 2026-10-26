using System;
using System.Collections.Generic;

namespace CloneZ
{
    // The world around the survivor, all of it synthesised: the weather (wind, rain, the roof overhead, thunder), the
    // woods by day and night (birdsong, cicadas, crickets and katydids, frogs, the hush of a December night), wildlife
    // calling far off, a fire in the stove, and the roadhouse in the hub with its guitarist. Beds are flat stereo loops
    // that AmbienceDirector fades with the weather, the hour, the season, the place and the roof; calls are mono and
    // placed round the listener.
    public sealed partial class SfxSynth
    {
        partial void RegisterAmbience()
        {
            // ── Beds: flat stereo loops at the bed rate ─────────────────────────────────────────────────────────────
            AddStereo(SfxIds.WindAmbience, g => AmbienceKit.Wind(g, 16f)).Loop().Peak(0.5f);
            AddStereo(SfxIds.WindHighAmbience, g => AmbienceKit.WindHigh(g, 12f)).Loop().Peak(0.55f);
            AddStereo(SfxIds.ForestAmbience, g => AmbienceKit.Forest(g, 16f)).Loop().Peak(0.45f);
            AddStereo(SfxIds.WinterAmbience, g => AmbienceKit.Winter(g, 14f)).Loop().Peak(0.45f);
            AddStereo(SfxIds.NightAmbience, g => AmbienceKit.Night(g, 12f)).Loop().Peak(0.45f);
            AddStereo(SfxIds.CicadaAmbience, g => AmbienceKit.Cicadas(g, 12f)).Loop().Peak(0.45f);
            AddStereo(SfxIds.FrogAmbience, g => AmbienceKit.Frogs(g, 12f)).Loop().Peak(0.45f);
            AddStereo(SfxIds.RainAmbience, g => AmbienceKit.Rain(g, 12f, false)).Loop().Peak(0.5f);
            AddStereo(SfxIds.RainHeavyAmbience, g => AmbienceKit.Rain(g, 12f, true)).Loop().Peak(0.55f);
            AddStereo(SfxIds.RainRoofAmbience, g => AmbienceKit.RainRoof(g, 12f)).Loop().Peak(0.5f);
            AddStereo(SfxIds.BarAmbience, g => AmbienceKit.Bar(g, 16f)).Loop().Peak(0.45f);

            // ── Weather one-shots ────────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.ThunderNear, g => AmbienceKit.Thunder(g, true), 3, BedRate)
                .Bus(SfxBus.Ambience).Peak(0.95f).Priority(40).Range(150f).Spread(110f).Reverb(0.25f);
            Add(SfxIds.ThunderFar, g => AmbienceKit.Thunder(g, false), 2, BedRate)
                .Bus(SfxBus.Ambience).Peak(0.85f).Priority(60).Range(150f).Spread(140f).Reverb(0.2f);
            AddStereo(SfxIds.WindGust, g => AmbienceKit.Gust(g), 2).Bus(SfxBus.Ambience).Peak(0.6f).Priority(110);

            // ── Wildlife: mono calls the director places 25–120 m round the listener ───────────────────────────────
            Add(SfxIds.BirdCardinal, g => AmbienceCalls.Cardinal(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.7f).Priority(180).Reverb(0.5f);
            Add(SfxIds.BirdWren, g => AmbienceCalls.WrenSong(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.7f).Priority(180).Reverb(0.5f);
            Add(SfxIds.BirdChickadee, g => AmbienceCalls.ChickadeeSong(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.65f).Priority(180).Reverb(0.5f);
            Add(SfxIds.BirdTitmouse, g => AmbienceCalls.TitmouseSong(g), 1, BedRate).Bus(SfxBus.Ambience).Peak(0.65f).Priority(180).Reverb(0.5f);
            Add(SfxIds.BirdJay, g => AmbienceCalls.JayCall(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.7f).Priority(170).Reverb(0.5f);
            Add(SfxIds.WoodpeckerDrum, g => AmbienceCalls.Woodpecker(g), 3, BedRate).Bus(SfxBus.Ambience).Peak(0.75f).Priority(170).Reverb(0.5f);
            Add(SfxIds.CrowCall, g => AmbienceCalls.Crow(g), 3, BedRate).Bus(SfxBus.Ambience).Peak(0.75f).Priority(160).Reverb(0.5f);
            Add(SfxIds.OwlCall, g => AmbienceCalls.Owl(g), 3, BedRate).Bus(SfxBus.Ambience).Peak(0.8f).Priority(150).Reverb(0.5f);
            Add(SfxIds.WhippoorwillCall, g => AmbienceCalls.Whippoorwill(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.75f).Priority(160).Reverb(0.5f);
            Add(SfxIds.CoyoteYip, g => AmbienceCalls.Coyotes(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.8f).Priority(150).Reverb(0.3f);
            Add(SfxIds.DogBarkFar, g => AmbienceCalls.Dogs(g), 3, BedRate).Bus(SfxBus.Ambience).Peak(0.8f).Priority(150).Reverb(0.3f);
            Add(SfxIds.BranchCreak, g => AmbienceCalls.BranchCreak(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.7f).Priority(170).Reverb(0.5f);
            Add(SfxIds.TreeCrack, g => AmbienceCalls.TreeCrack(g), 2, BedRate).Bus(SfxBus.Ambience).Peak(0.85f).Priority(140).Reverb(0.3f);

            // ── Positional loops: the stove's fire, the kerosene heater, the hub's guitarist ─────────────────────────
            Add(SfxIds.FireCrackle, g => AmbienceKit.Fire(g, 10f), 1, BedRate).Loop().Bus(SfxBus.Ambience).Peak(0.7f).Priority(90);
            Add(SfxIds.HeaterHiss, g => AmbienceKit.Heater(g, 6f), 1, BedRate).Loop().Bus(SfxBus.Ambience).Peak(0.5f).Priority(120);
            Add(SfxIds.Guitar, g => AmbienceGuitar.Tune(g), 2, BedRate).Loop().Peak(0.6f).Priority(60);
        }

        // Scaffolding for the beds (seamless stereo loops with their events laid on exactly), and the beds themselves.
        private static class AmbienceKit
        {
            private const float TwoPi = Dsp.TwoPi;

            #region Scaffolding
            // A stereo bed `seconds` long. The texture is rendered half a second longer and the overhang cross-faded into
            // the head, so noise and filters wrap without a seam; the events are then laid on the exact loop (wrapping
            // round its end), and the loop is turned to start in its quietest stretch.
            internal static StereoBuf Bed(SynthGen g, float seconds, Action<StereoBuf> texture, Action<StereoBuf> events)
            {
                int len = g.N(seconds);
                var raw = new StereoBuf(len + g.N(0.5f));
                texture?.Invoke(raw);
                var s = new StereoBuf(Dsp.CrossfadeLoop(raw.L, len), Dsp.CrossfadeLoop(raw.R, len));
                events?.Invoke(s);
                RotateQuiet(s, g.Rate);
                return s;
            }

            // The mono version, for positional loops (a fire, a heater).
            internal static float[] MonoBed(SynthGen g, float seconds, Action<float[]> texture, Action<float[]> events)
            {
                int len = g.N(seconds);
                var raw = new float[len + g.N(0.5f)];
                texture?.Invoke(raw);
                float[] x = Dsp.CrossfadeLoop(raw, len);
                events?.Invoke(x);
                Dsp.RotateToQuiet(x, g.Rate);
                return x;
            }

            // Turns a stereo loop (both sides alike) so it starts in the middle of its quietest tenth of a second.
            private static void RotateQuiet(StereoBuf s, int rate)
            {
                int n = s.Length;
                if (n < 64) return;
                int win = Math.Max(16, Math.Min(n / 8, rate / 10));
                double e = 0;
                for (int i = 0; i < win; i++) e += s.L[i] * s.L[i] + s.R[i] * s.R[i];
                double best = e;
                int bestAt = 0;
                for (int i = 1; i < n; i++)
                {
                    int a = (i + win - 1) % n, d = i - 1;
                    e += s.L[a] * s.L[a] + s.R[a] * s.R[a] - s.L[d] * s.L[d] - s.R[d] * s.R[d];
                    if (e < best)
                    {
                        best = e;
                        bestAt = i;
                    }
                }
                int start = (bestAt + win / 2) % n;
                if (start == 0) return;
                Rotate(s.L, start);
                Rotate(s.R, start);
            }

            private static void Rotate(float[] x, int start)
            {
                var t = new float[x.Length];
                Array.Copy(x, start, t, 0, x.Length - start);
                Array.Copy(x, 0, t, x.Length - start, start);
                Array.Copy(t, x, x.Length);
            }

            // A control curve (a function of seconds) sampled every 32 samples and joined by straight lines.
            internal static float[] Curve(int n, int rate, Func<float, float> f)
            {
                const int Step = 32;
                var c = new float[n];
                float a = f(0f);
                for (int i = 0; i < n; i += Step)
                {
                    float b = f((i + Step) / (float)rate);
                    int m = Math.Min(Step, n - i);
                    float k = (b - a) / Step;
                    for (int j = 0; j < m; j++) c[i + j] = a + k * j;
                    a = b;
                }
                return c;
            }

            // Two sides of noise sharing `common` of their power (0 = unrelated, 1 = identical), pink or white.
            internal static StereoBuf NoisePair(SynthGen g, int n, float common, bool pink)
            {
                float[] c = pink ? Dsp.Pink(g, n) : Dsp.White(g, n);
                float[] l = pink ? Dsp.Pink(g, n) : Dsp.White(g, n);
                float[] r = pink ? Dsp.Pink(g, n) : Dsp.White(g, n);
                float a = MathF.Sqrt(Dsp.Clamp01(common)), b = MathF.Sqrt(1f - Dsp.Clamp01(common));
                for (int i = 0; i < n; i++)
                {
                    l[i] = a * c[i] + b * l[i];
                    r[i] = a * c[i] + b * r[i];
                }
                return new StereoBuf(l, r);
            }

            // Scatters tiny decaying noise grains (leaf flutter, patter, grit), kept where `density` (0..1 per sample) is
            // high, their level following it too. Wraps round the end.
            internal static void Rustle(float[] x, SynthGen g, int count, float gain, float tauMin, float tauMax, float[] density)
            {
                int n = x.Length, rate = g.Rate;
                for (int c = 0; c < count; c++)
                {
                    int at = g.Int(0, n);
                    float d = density != null ? density[at] : 1f;
                    if (g.Next() > d) continue;
                    float tau = g.Range(tauMin, tauMax);
                    int len = Math.Max(2, (int)(tau * rate * 5f));
                    float e = gain * (0.2f + 0.8f * g.Next() * g.Next()) * (0.4f + 0.6f * d);
                    float fall = MathF.Exp(-1f / (tau * rate));
                    for (int i = 0, k = at; i < len; i++, e *= fall)
                    {
                        x[k] += g.Bipolar() * e;
                        if (++k >= n) k = 0;
                    }
                }
            }

            // Adds a full-length mono layer into both sides at a fixed pan.
            internal static void Layer(StereoBuf s, float[] x, float gain, float pan)
            {
                Dsp.PanGains(pan, out float l, out float r);
                int n = Math.Min(s.Length, x.Length);
                for (int i = 0; i < n; i++)
                {
                    s.L[i] += x[i] * gain * l;
                    s.R[i] += x[i] * gain * r;
                }
            }

            // 0 outside u = 0..1, easing in over `attack` and out over `release` (fractions of the span).
            internal static float Swell(float u, float attack, float release)
            {
                if (u <= 0f || u >= 1f) return 0f;
                float a = attack > 0f ? Dsp.SmoothStep(u / attack) : 1f;
                float r = release > 0f ? Dsp.SmoothStep((1f - u) / release) : 1f;
                return a * r;
            }

            // A period nudged so a whole number of them fills the loop (a rhythm that wraps without a stumble).
            internal static float Fit(float period, float loop) => loop / Math.Max(1f, MathF.Round(loop / period));

            // A call heard from further off (d = 0 near .. 1 far): its highs soaked up by the air and the leaves.
            internal static float[] Far(float[] x, int rate, float d, float nearHz, float farHz)
            {
                Dsp.LP(x, Dsp.Lerp(nearHz, farHz, Dsp.Clamp01(d)), rate);
                return x;
            }

            // Silences the last `seconds` of a one-shot smoothly (a baked tail never stops short).
            internal static float[] EndFade(float[] x, int rate, float seconds)
            {
                int n = Math.Min(x.Length, Math.Max(1, (int)(seconds * rate)));
                for (int i = 0; i < n; i++) x[x.Length - 1 - i] *= Dsp.SmoothStep(i / (float)n);
                return x;
            }

            // A loop's events and the reverb they share: each is laid on dry and, by its distance, into the send.
            internal sealed class Scene
            {
                public readonly StereoBuf Dry, Send;
                private readonly int _rate;

                public Scene(StereoBuf dry, int rate)
                {
                    Dry = dry;
                    Send = new StereoBuf(dry.Length);
                    _rate = rate;
                }

                public void Put(float[] ev, float atSeconds, float gain, float pan, float send)
                {
                    int at = (int)(atSeconds * _rate);
                    Dsp.MixWrapPan(Dry, ev, at, gain, pan);
                    if (send > 0f) Dsp.MixWrapPan(Send, ev, at, gain * send, pan);
                }

                // The space round them all (wrapped round the loop), added under the dry events.
                public void Space(float size, float damp, float wet, float predelay)
                {
                    if (Dsp.Peak(Send.L) < 1e-9f) return;
                    Dsp.StereoReverb(Send, _rate, size, damp, 1f, 0f, predelay, true);
                    for (int i = 0; i < Dry.Length; i++)
                    {
                        Dry.L[i] += Send.L[i] * wet;
                        Dry.R[i] += Send.R[i] * wet;
                    }
                }
            }
            #endregion

            #region Wind
            // Gusty wind over open ground and through trees: a pink-noise body whose band opens and climbs with each gust
            // (reaching one side a moment before the other), a low rumble, and dry leaves rattling as the gusts peak.
            internal static StereoBuf Wind(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    Func<float, float> front = Dsp.Gusts(g, seconds, 2, 5, 4), eddy = Dsp.Gusts(g, seconds, 4, 9, 3);
                    Func<float, float> left = t => Dsp.Clamp01(0.8f * front(t) + 0.2f * eddy(t));
                    Func<float, float> right = t => Dsp.Clamp01(0.8f * front(t - 0.7f) + 0.2f * eddy(t + 2.3f));
                    float[] gl = Curve(n, rate, left), gr = Curve(n, rate, right);
                    StereoBuf body = NoisePair(g, n, 0.45f, true);
                    Dsp.SweepBP(body.L, rate, 0.6f, t => { float v = left(t); return 180f + 620f * v * v; });
                    Dsp.SweepBP(body.R, rate, 0.6f, t => { float v = right(t); return 180f + 620f * v * v; });
                    StereoBuf low = NoisePair(g, n, 0.7f, true);
                    Dsp.LP(low.L, 140f, rate);
                    Dsp.LP(low.R, 140f, rate);
                    Dsp.HP(low.L, 28f, rate);
                    Dsp.HP(low.R, 28f, rate);
                    var dl = new float[n];
                    var dr = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        dl[i] = Dsp.SmoothStep((gl[i] - 0.35f) / 0.55f);
                        dr[i] = Dsp.SmoothStep((gr[i] - 0.35f) / 0.55f);
                    }
                    var lvL = new float[n];
                    var lvR = new float[n];
                    Rustle(lvL, g, (int)(seconds * 1600f), 1f, 0.0003f, 0.0015f, dl);
                    Rustle(lvR, g, (int)(seconds * 1600f), 1f, 0.0003f, 0.0015f, dr);
                    Dsp.HP(lvL, 1500f, rate);
                    Dsp.HP(lvR, 1500f, rate);
                    Dsp.LP(lvL, 7000f, rate);
                    Dsp.LP(lvR, 7000f, rate);
                    for (int i = 0; i < n; i++)
                    {
                        float a = gl[i], b = gr[i];
                        s.L[i] = body.L[i] * (0.15f + 0.85f * a * a) * 2.2f + low.L[i] * (0.3f + 0.7f * a) * 0.7f + lvL[i] * 0.5f;
                        s.R[i] = body.R[i] * (0.15f + 0.85f * b * b) * 2.2f + low.R[i] * (0.3f + 0.7f * b) * 0.7f + lvR[i] * 0.5f;
                    }
                }, null);
            }

            // A gale: a roaring body flung open by every gust, ground rumble, whistles where the wind finds corners and
            // wires (narrow resonances sliding up as it presses), and leaves and grit torn past.
            internal static StereoBuf WindHigh(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    Func<float, float> front = Dsp.Gusts(g, seconds, 2, 5, 4);
                    Func<float, float> right = t => front(t - 0.5f);
                    float[] gl = Curve(n, rate, front), gr = Curve(n, rate, right);
                    StereoBuf roar = NoisePair(g, n, 0.4f, true);
                    Dsp.SweepLP(roar.L, rate, 0.9f, t => { float v = front(t); return 320f + 1700f * v * v; });
                    Dsp.SweepLP(roar.R, rate, 0.9f, t => { float v = right(t); return 320f + 1700f * v * v; });
                    Dsp.HP(roar.L, 60f, rate);
                    Dsp.HP(roar.R, 60f, rate);
                    StereoBuf low = NoisePair(g, n, 0.75f, true);
                    Dsp.LP(low.L, 110f, rate);
                    Dsp.LP(low.R, 110f, rate);
                    Dsp.HP(low.L, 26f, rate);
                    Dsp.HP(low.R, 26f, rate);
                    var whistle = new StereoBuf(n);
                    for (int w = 0; w < 3; w++)
                    {
                        float f0 = g.Range(480f, 1350f), q = g.Range(16f, 28f), pan = g.Range(-0.85f, 0.85f);
                        Func<float, float> press = Dsp.Gusts(g, seconds, 2, 6, 3);
                        Func<float, float> hz = t => f0 * (0.8f + 0.45f * press(t));
                        float[] wn = Dsp.White(g, n);
                        Dsp.SweepBP(wn, rate, q, hz);
                        Dsp.SweepBP(wn, rate, q, hz);
                        Dsp.Norm(wn);
                        float[] env = Curve(n, rate, t => { float v = Dsp.Clamp01(0.6f * press(t) + 0.6f * front(t) - 0.45f); return v * v; });
                        for (int i = 0; i < n; i++) wn[i] *= env[i];
                        Layer(whistle, wn, 1f, pan);
                    }
                    var dl = new float[n];
                    var dr = new float[n];
                    for (int i = 0; i < n; i++)
                    {
                        dl[i] = gl[i] * gl[i];
                        dr[i] = gr[i] * gr[i];
                    }
                    var lvL = new float[n];
                    var lvR = new float[n];
                    Rustle(lvL, g, (int)(seconds * 2200f), 1f, 0.0002f, 0.0012f, dl);
                    Rustle(lvR, g, (int)(seconds * 2200f), 1f, 0.0002f, 0.0012f, dr);
                    Dsp.HP(lvL, 1800f, rate);
                    Dsp.HP(lvR, 1800f, rate);
                    for (int i = 0; i < n; i++)
                    {
                        float a = gl[i], b = gr[i];
                        s.L[i] = roar.L[i] * (0.35f + 0.65f * a) * 1.3f + low.L[i] * (0.4f + 0.6f * a) * 1.5f + whistle.L[i] * 0.16f + lvL[i] * 0.25f;
                        s.R[i] = roar.R[i] * (0.35f + 0.65f * b) * 1.3f + low.R[i] * (0.4f + 0.6f * b) * 1.5f + whistle.R[i] * 0.16f + lvR[i] * 0.25f;
                    }
                }, null);
            }

            // One gust passing (flat, stereo): a swell of wind sweeping from one side to the other, leaves rattling at its
            // height and a thin whistle riding it.
            internal static StereoBuf Gust(SynthGen g)
            {
                int rate = g.Rate;
                float len = g.Range(3.6f, 4.6f), top = g.Range(0.35f, 0.5f);
                int n = g.N(len);
                Func<float, float> env = t =>
                {
                    float u = t / len;
                    if (u <= 0f || u >= 1f) return 0f;
                    return u < top ? Dsp.SmoothStep(u / top) : MathF.Pow(1f - Dsp.SmoothStep((u - top) / (1f - top)), 1.6f);
                };
                float[] e = Curve(n, rate, t => Math.Max(env(t), 0.3f * (1f - Dsp.SmoothStep(t / (len * top)))));
                StereoBuf s = NoisePair(g, n, 0.5f, true);
                Dsp.SweepBP(s.L, rate, 0.7f, t => 240f + 900f * env(t));
                Dsp.SweepBP(s.R, rate, 0.7f, t => 240f + 900f * env(t));
                var dens = new float[n];
                for (int i = 0; i < n; i++) dens[i] = e[i] * e[i];
                var lv = new float[n];
                Rustle(lv, g, (int)(len * 2600f), 1f, 0.0003f, 0.0014f, dens);
                Dsp.HP(lv, 1500f, rate);
                Dsp.LP(lv, 8000f, rate);
                float wf = g.Range(650f, 1150f);
                float[] wh = Dsp.White(g, n);
                Dsp.SweepBP(wh, rate, 18f, t => wf * (0.85f + 0.4f * env(t)));
                Dsp.SweepBP(wh, rate, 18f, t => wf * (0.85f + 0.4f * env(t)));
                Dsp.Norm(wh);
                float from = g.Range(0.3f, 0.8f) * (g.Chance(0.5f) ? 1f : -1f), to = -from * g.Range(0.6f, 1f);
                for (int i = 0; i < n; i++)
                {
                    Dsp.PanGains(Dsp.Lerp(from, to, Dsp.SmoothStep(i / (float)n)), out float l, out float r);
                    float body = e[i] * 1.6f, extra = lv[i] * 0.3f + wh[i] * e[i] * e[i] * 0.08f;
                    s.L[i] = (s.L[i] * body + extra) * l * 1.41f;
                    s.R[i] = (s.R[i] * body + extra) * r * 1.41f;
                }
                return s;
            }
            #endregion

            #region Woods
            // Daytime Kentucky woods: leaves hissing and fluttering in a breeze (a different tree each side), a soft low
            // air, and birds about the canopy near and far — cardinals, a Carolina wren, a chickadee, a titmouse, a blue
            // jay, a woodpecker drumming — in the wood's own reverberation.
            internal static StereoBuf Forest(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    float[] ga = Curve(n, rate, Dsp.Gusts(g, seconds, 1, 3, 3)), gb = Curve(n, rate, Dsp.Gusts(g, seconds, 1, 4, 3));
                    StereoBuf hiss = NoisePair(g, n, 0.15f, true);
                    Dsp.HP(hiss.L, 1800f, rate);
                    Dsp.HP(hiss.R, 1800f, rate);
                    Dsp.LP(hiss.L, 7000f, rate);
                    Dsp.LP(hiss.R, 7000f, rate);
                    var rl = new float[n];
                    var rr = new float[n];
                    Rustle(rl, g, (int)(seconds * 900f), 1f, 0.0003f, 0.0013f, ga);
                    Rustle(rr, g, (int)(seconds * 900f), 1f, 0.0003f, 0.0013f, gb);
                    Dsp.HP(rl, 2000f, rate);
                    Dsp.HP(rr, 2000f, rate);
                    StereoBuf air = NoisePair(g, n, 0.6f, true);
                    Dsp.LP(air.L, 380f, rate);
                    Dsp.LP(air.R, 380f, rate);
                    Dsp.HP(air.L, 35f, rate);
                    Dsp.HP(air.R, 35f, rate);
                    for (int i = 0; i < n; i++)
                    {
                        float a = 0.3f + 0.7f * ga[i], b = 0.3f + 0.7f * gb[i];
                        s.L[i] = hiss.L[i] * a * 0.5f + rl[i] * 0.12f + air.L[i] * 0.18f;
                        s.R[i] = hiss.R[i] * b * 0.5f + rr[i] * 0.12f + air.R[i] * 0.18f;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    var calls = new List<float[]>
                    {
                        AmbienceCalls.CardinalSong(g, 0), AmbienceCalls.Wren(g), AmbienceCalls.Chickadee(g, false), AmbienceCalls.Titmouse(g),
                        AmbienceCalls.CardinalSong(g, 1), AmbienceCalls.Jay(g, false), AmbienceCalls.Drumming(g, g.Int(0, 3)),
                    };
                    float slot = seconds / calls.Count;
                    for (int i = 0; i < calls.Count; i++)
                    {
                        float d = g.Range(0.35f, 1f);
                        float[] c = Far(calls[i], rate, d, 12000f, 4500f);
                        scene.Put(c, slot * (i + g.Range(0.05f, 0.55f)), Dsp.Lerp(0.8f, 0.28f, d), g.Range(-0.8f, 0.8f), Dsp.Lerp(0.35f, 0.9f, d));
                    }
                    scene.Space(0.7f, 0.45f, 0.5f, 0.025f);
                });
            }

            // A December night in the woods: a thin, whispering air through bare twigs that comes and goes, a deep far
            // murmur, faint whistles where the wind finds a gap; a tree creaking somewhere, then another; ice ticking in the
            // branches; snow sliding off a bough.
            internal static StereoBuf Winter(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    Func<float, float> breath = Dsp.Gusts(g, seconds, 1, 3, 3);
                    float[] ga = Curve(n, rate, breath), gb = Curve(n, rate, t => breath(t - 0.9f));
                    StereoBuf air = NoisePair(g, n, 0.35f, true);
                    Dsp.BP(air.L, 950f, 0.55f, rate);
                    Dsp.BP(air.R, 950f, 0.55f, rate);
                    StereoBuf twig = NoisePair(g, n, 0.15f, false);
                    Dsp.HP(twig.L, 2600f, rate);
                    Dsp.HP(twig.R, 2600f, rate);
                    Dsp.LP(twig.L, 7500f, rate);
                    Dsp.LP(twig.R, 7500f, rate);
                    StereoBuf low = NoisePair(g, n, 0.8f, true);
                    Dsp.LP(low.L, 110f, rate);
                    Dsp.LP(low.R, 110f, rate);
                    Dsp.HP(low.L, 25f, rate);
                    Dsp.HP(low.R, 25f, rate);
                    var whistle = new StereoBuf(n);
                    for (int w = 0; w < 2; w++)
                    {
                        float f0 = g.Range(850f, 1700f), pan = g.Range(-0.8f, 0.8f), shift = w * 3.1f;
                        Func<float, float> hz = t => f0 * (0.92f + 0.16f * breath(t + shift));
                        float[] wn = Dsp.White(g, n);
                        Dsp.SweepBP(wn, rate, 24f, hz);
                        Dsp.SweepBP(wn, rate, 24f, hz);
                        Dsp.Norm(wn);
                        float[] env = Curve(n, rate, t => { float v = Dsp.Clamp01((breath(t + shift) - 0.55f) / 0.45f); return v * v; });
                        for (int i = 0; i < n; i++) wn[i] *= env[i];
                        Layer(whistle, wn, 1f, pan);
                    }
                    for (int i = 0; i < n; i++)
                    {
                        float a = ga[i], b = gb[i];
                        s.L[i] = air.L[i] * (0.2f + 0.8f * a * a) * 0.5f + twig.L[i] * a * a * a * 0.1f + low.L[i] * 0.22f + whistle.L[i] * 0.03f;
                        s.R[i] = air.R[i] * (0.2f + 0.8f * b * b) * 0.5f + twig.R[i] * b * b * b * 0.1f + low.R[i] * 0.22f + whistle.R[i] * 0.03f;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    scene.Put(Far(AmbienceCalls.Creak(g, true), rate, g.Range(0.4f, 0.8f), 7000f, 2500f), g.Range(0.5f, seconds * 0.4f), 1.1f, g.Range(-0.8f, -0.2f), 0.8f);
                    scene.Put(Far(AmbienceCalls.Creak(g, false), rate, g.Range(0.5f, 0.9f), 7000f, 2500f), g.Range(seconds * 0.55f, seconds * 0.9f), 0.9f, g.Range(0.2f, 0.8f), 0.9f);
                    int ticks = g.Int(6, 11);
                    for (int i = 0; i < ticks; i++) scene.Put(IceTick(g), g.Range(0f, seconds), g.Range(0.1f, 0.3f), g.Range(-0.9f, 0.9f), 0.6f);
                    scene.Put(SnowSlide(g), g.Range(seconds * 0.15f, seconds * 0.85f), 0.9f, g.Range(-0.7f, 0.7f), 0.9f);
                    scene.Space(0.85f, 0.6f, 0.5f, 0.03f);
                });
            }

            // A frozen twig or a crust of ice ticking.
            private static float[] IceTick(SynthGen g)
            {
                var x = new float[g.N(0.05f)];
                float f = g.Range(3200f, 6500f);
                Dsp.Modes(x, g.Rate, 0f, 0.7f, new[] { f, f * g.Range(1.4f, 1.9f) }, new[] { 1f, 0.5f }, new[] { g.Range(0.003f, 0.008f), 0.003f });
                Dsp.AddBurst(x, g, 0f, 0.4f, 0.0001f, 0.0004f, 0f, 2500f);
                return x;
            }

            // Snow sliding off a bough: a soft rushing hiss, then the muffled whump of it landing.
            private static float[] SnowSlide(SynthGen g)
            {
                int rate = g.Rate;
                float slide = g.Range(0.35f, 0.7f);
                var x = new float[g.N(slide + 0.6f)];
                float[] hiss = Dsp.White(g, g.N(slide));
                Dsp.BP(hiss, g.Range(1500f, 2600f), 0.8f, rate);
                Dsp.Shape(hiss, rate, t => Swell(t / slide, 0.6f, 0.15f) * 0.35f);
                Dsp.Mix(x, hiss, 0);
                Dsp.AddBurst(x, g, slide, 0.9f, 0.01f, 0.09f, 420f, 0f);
                Dsp.Thump(x, rate, slide, 0.6f, 85f, 55f, 0.05f, 0.008f, 0.08f);
                Dsp.LP(x, 3500f, rate);
                return x;
            }
            #endregion

            #region Warm nights
            // A warm night: field crickets chirping each on its own steady beat, snowy tree crickets far off pulsing in
            // step, katydids rasping "ka-ty-did" back and forth, a ground cricket's endless high trill, still night air.
            internal static StereoBuf Night(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    StereoBuf air = NoisePair(g, n, 0.4f, true);
                    Dsp.LP(air.L, 1200f, rate);
                    Dsp.LP(air.R, 1200f, rate);
                    Dsp.HP(air.L, 50f, rate);
                    Dsp.HP(air.R, 50f, rate);
                    float f1 = g.Range(6200f, 6600f), f2 = f1 * g.Range(1.03f, 1.06f), a1 = g.Range(26f, 32f), a2 = g.Range(30f, 36f);
                    float p1 = 0f, p2 = 0f, q1 = 0f, q2 = 0f, dt = 1f / rate;
                    for (int i = 0; i < n; i++)
                    {
                        p1 += f1 * dt; p1 -= MathF.Floor(p1);
                        p2 += f2 * dt; p2 -= MathF.Floor(p2);
                        q1 += a1 * dt; q1 -= MathF.Floor(q1);
                        q2 += a2 * dt; q2 -= MathF.Floor(q2);
                        float t1 = MathF.Sin(TwoPi * p1) * (q1 < 0.55f ? MathF.Sin(Dsp.Pi * q1 / 0.55f) : 0f);
                        float t2 = MathF.Sin(TwoPi * p2) * (q2 < 0.55f ? MathF.Sin(Dsp.Pi * q2 / 0.55f) : 0f);
                        s.L[i] = air.L[i] * 0.25f + t1 * 0.022f + t2 * 0.008f;
                        s.R[i] = air.R[i] * 0.25f + t1 * 0.008f + t2 * 0.02f;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    float loop = seconds;
                    int crickets = g.Int(5, 8);
                    for (int c = 0; c < crickets; c++)
                    {
                        float period = Fit(g.Range(0.34f, 0.62f), loop), d = g.Range(0.15f, 1f), pan = g.Range(-0.9f, 0.9f), phase = g.Range(0f, period);
                        float[] chirp = Far(Chirp(g, g.Range(4200f, 5200f), g.Int(3, 6)), rate, d, 14000f, 5500f);
                        int count = (int)MathF.Round(loop / period);
                        for (int k = 0; k < count; k++)
                            scene.Put(chirp, phase + k * period + g.Range(-0.004f, 0.004f), Dsp.Lerp(0.75f, 0.18f, d) * g.Range(0.85f, 1.05f), pan, Dsp.Lerp(0.3f, 0.85f, d));
                    }
                    float beat = Fit(g.Range(0.45f, 0.6f), loop);
                    int members = g.Int(4, 7), pulses = (int)MathF.Round(loop / beat);
                    for (int m = 0; m < members; m++)
                    {
                        float d = g.Range(0.55f, 1f), pan = g.Range(-0.95f, 0.95f), lag = g.Range(-0.012f, 0.012f);
                        float[] treet = Far(Treet(g, g.Range(2650f, 2950f)), rate, d, 12000f, 4500f);
                        for (int k = 0; k < pulses; k++) scene.Put(treet, k * beat + lag + g.Range(-0.004f, 0.004f), Dsp.Lerp(0.5f, 0.15f, d), pan, 0.8f);
                    }
                    int katydids = g.Int(2, 4);
                    float call = Fit(g.Range(1f, 1.35f), loop);
                    int calls = (int)MathF.Round(loop / call);
                    for (int k = 0; k < katydids; k++)
                    {
                        float d = g.Range(0.2f, 0.9f), pan = g.Range(-0.85f, 0.85f), offset = call * k / katydids + g.Range(-0.05f, 0.05f);
                        float[] kd = Far(Katydid(g, g.Int(2, 5), g.Range(4200f, 5400f)), rate, d, 14000f, 5500f);
                        for (int j = 0; j < calls; j++)
                            if (!g.Chance(0.12f)) scene.Put(kd, offset + j * call, Dsp.Lerp(0.7f, 0.22f, d) * g.Range(0.85f, 1.05f), pan, Dsp.Lerp(0.3f, 0.8f, d));
                    }
                    scene.Space(0.75f, 0.5f, 0.35f, 0.02f);
                });
            }

            // A field cricket's chirp: three to five short, almost pure pulses round its carrier.
            private static float[] Chirp(SynthGen g, float carrier, int pulses)
            {
                int rate = g.Rate;
                float pulse = g.Range(0.011f, 0.016f), step = pulse + g.Range(0.006f, 0.01f);
                var x = new float[(int)((pulses * step + 0.03f) * rate)];
                for (int p = 0; p < pulses; p++)
                    AmbienceCalls.Whistle(x, rate, 0.002f + p * step, pulse, u => carrier * (1.01f - 0.03f * u), u => Swell(u, 0.2f, 0.45f), p == 0 ? 0.7f : 1f, 0.06f, 0.02f);
                return x;
            }

            // A snowy tree cricket's "treet": a soft, rapidly pulsed tone.
            private static float[] Treet(SynthGen g, float carrier)
            {
                int rate = g.Rate;
                float len = g.Range(0.07f, 0.1f), am = g.Range(45f, 60f);
                var x = new float[(int)((len + 0.02f) * rate)];
                AmbienceCalls.Whistle(x, rate, 0f, len, u => carrier, u => Swell(u, 0.15f, 0.3f) * (0.55f + 0.45f * MathF.Sin(TwoPi * am * u * len)), 1f, 0.03f, 0f);
                return x;
            }

            // A true katydid's call: two to five rasps, "ka-ty-did".
            private static float[] Katydid(SynthGen g, int rasps, float centre)
            {
                int rate = g.Rate;
                float gap = g.Range(0.11f, 0.15f);
                var x = new float[(int)((rasps * gap + 0.12f) * rate)];
                for (int r = 0; r < rasps; r++)
                    Dsp.Mix(x, Rasp(g, g.Range(0.035f, 0.06f), centre * g.Range(0.97f, 1.03f)), (int)((0.005f + r * gap) * rate), r == rasps - 1 ? 0.85f : 1f);
                return x;
            }

            // One rasp: the wing's file of teeth struck several hundred times a second, ringing two broad resonances.
            private static float[] Rasp(SynthGen g, float dur, float centre)
            {
                int rate = g.Rate, n = (int)((dur + 0.02f) * rate);
                var x = new float[n];
                float strike = g.Range(650f, 1000f);
                for (float t = 0f; t < dur; t += g.Range(0.8f, 1.2f) / strike)
                {
                    int k = (int)(t * rate);
                    if (k < n) x[k] += (0.6f + 0.4f * g.Next()) * Swell(t / dur, 0.25f, 0.3f);
                }
                var y = (float[])x.Clone();
                Dsp.BP(x, centre, 3f, rate);
                Dsp.BP(y, Math.Min(centre * 1.9f, rate * 0.42f), 2.5f, rate);
                for (int i = 0; i < n; i++) x[i] += 0.5f * y[i];
                Dsp.Norm(x);
                return x;
            }

            // A summer afternoon: the whole wood's chorus of dog-day cicadas (a shimmering band round 5 kHz breathing slowly,
            // throbbing with the tymbals of hundreds) and single cicadas nearer by, each swelling to a pulsing buzz and
            // dying away with a falling pitch.
            internal static StereoBuf Cicadas(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    StereoBuf band = NoisePair(g, n, 0.3f, false);
                    Dsp.BP(band.L, 5100f, 1.6f, rate);
                    Dsp.BP(band.L, 5100f, 1.6f, rate);
                    Dsp.BP(band.R, 4900f, 1.6f, rate);
                    Dsp.BP(band.R, 4900f, 1.6f, rate);
                    Func<float, float> breathe = Dsp.Gusts(g, seconds, 1, 3, 3);
                    float[] bl = Curve(n, rate, breathe), br = Curve(n, rate, t => breathe(t + 1.7f));
                    float ph = 0f, f = g.Range(130f, 160f), dt = 1f / rate;
                    for (int i = 0; i < n; i++)
                    {
                        ph += f * dt;
                        ph -= MathF.Floor(ph);
                        float throb = 0.7f + 0.3f * MathF.Sin(TwoPi * ph);
                        s.L[i] = band.L[i] * (0.2f + 0.8f * bl[i] * bl[i]) * throb;
                        s.R[i] = band.R[i] * (0.2f + 0.8f * br[i] * br[i]) * throb;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    int calls = g.Int(4, 6);
                    for (int c = 0; c < calls; c++)
                    {
                        float dur = g.Range(4.5f, 8f), d = c < 2 ? g.Range(0.1f, 0.4f) : g.Range(0.5f, 1f);
                        float[] call = Far(Cicada(g, dur, g.Range(4300f, 6200f), g.Range(110f, 210f), g.Range(4f, 11f)), rate, d, 15000f, 6500f);
                        scene.Put(call, c * seconds / calls + g.Range(0f, 1.5f), Dsp.Lerp(1.9f, 0.6f, d), g.Range(-0.85f, 0.85f), 0.5f);
                    }
                    scene.Space(0.7f, 0.5f, 0.25f, 0.02f);
                });
            }

            // One cicada: tymbal clicks (noise pulses) ringing its body's resonance, swelling in, throbbing, dying away.
            private static float[] Cicada(SynthGen g, float dur, float centre, float clickHz, float throbHz)
            {
                int rate = g.Rate, n = (int)(dur * rate);
                var x = new float[n];
                float ph = 0f, ph2 = g.Next(), attack = g.Range(0.2f, 0.35f), release = g.Range(0.15f, 0.25f);
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)n;
                    ph += clickHz / rate;
                    ph -= MathF.Floor(ph);
                    ph2 += throbHz / rate;
                    ph2 -= MathF.Floor(ph2);
                    float click = ph < 0.35f ? MathF.Sin(Dsp.Pi * ph / 0.35f) : 0f;
                    x[i] = g.Bipolar() * click * (0.6f + 0.4f * MathF.Sin(TwoPi * ph2)) * Swell(u, attack, release);
                }
                Dsp.SweepBP(x, rate, 4f, t => centre * (1f - 0.14f * Dsp.SmoothStep((t / dur - 0.78f) / 0.22f)));
                Dsp.Norm(x);
                return x;
            }

            // A wet spring night by the water: spring peepers (a chorus of clear rising peeps, each frog on its own beat),
            // chorus frogs running a thumbnail along a comb, a gray treefrog's musical trill, a green frog's banjo "gunk".
            internal static StereoBuf Frogs(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    StereoBuf air = NoisePair(g, n, 0.4f, true);
                    Dsp.LP(air.L, 900f, rate);
                    Dsp.LP(air.R, 900f, rate);
                    Dsp.HP(air.L, 50f, rate);
                    Dsp.HP(air.R, 50f, rate);
                    for (int i = 0; i < n; i++)
                    {
                        s.L[i] = air.L[i] * 0.2f;
                        s.R[i] = air.R[i] * 0.2f;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    float loop = seconds;
                    int peepers = g.Int(14, 20);
                    for (int p = 0; p < peepers; p++)
                    {
                        float period = Fit(g.Range(0.75f, 1.5f), loop), d = g.Range(0.1f, 1f), pan = g.Range(-0.95f, 0.95f), phase = g.Range(0f, period);
                        float[] peep = Far(Peep(g, g.Range(2650f, 3250f)), rate, d, 13000f, 4500f);
                        int count = (int)MathF.Round(loop / period);
                        for (int k = 0; k < count; k++)
                            if (!g.Chance(0.08f)) scene.Put(peep, phase + k * period + g.Range(-0.03f, 0.03f), Dsp.Lerp(0.7f, 0.12f, d) * g.Range(0.8f, 1.05f), pan, Dsp.Lerp(0.25f, 0.8f, d));
                    }
                    int chorus = g.Int(3, 6);
                    for (int c = 0; c < chorus; c++)
                    {
                        float d = g.Range(0.3f, 1f);
                        scene.Put(Far(ChorusFrog(g), rate, d, 12000f, 4000f), g.Range(0f, loop), Dsp.Lerp(0.6f, 0.2f, d), g.Range(-0.8f, 0.8f), 0.6f);
                    }
                    int trills = g.Int(2, 4);
                    for (int c = 0; c < trills; c++)
                    {
                        float d = g.Range(0.2f, 0.8f);
                        scene.Put(Far(Treefrog(g), rate, d, 12000f, 4000f), g.Range(0f, loop), Dsp.Lerp(0.55f, 0.2f, d), g.Range(-0.8f, 0.8f), 0.6f);
                    }
                    scene.Put(Gunk(g), g.Range(0f, loop), 0.35f, g.Range(-0.6f, 0.6f), 0.6f);
                    scene.Space(0.65f, 0.5f, 0.4f, 0.02f);
                });
            }

            // A spring peeper's peep: a clear whistle sliding up.
            private static float[] Peep(SynthGen g, float hz)
            {
                int rate = g.Rate;
                float d = g.Range(0.09f, 0.14f);
                var x = new float[(int)((d + 0.02f) * rate)];
                AmbienceCalls.Whistle(x, rate, 0f, d, u => hz * (0.92f + 0.12f * u), u => Swell(u, 0.1f, 0.25f), 1f, 0.035f, 0.01f);
                return x;
            }

            // An upland chorus frog: ten to eighteen pulses quickening and rising, "crreeek".
            private static float[] ChorusFrog(SynthGen g)
            {
                int rate = g.Rate;
                int pulses = g.Int(10, 18);
                var x = new float[g.N(0.9f)];
                float t = 0.005f, f = g.Range(2300f, 2800f);
                for (int p = 0; p < pulses; p++)
                {
                    float u = p / (float)(pulses - 1), hz = f * (0.95f + 0.1f * u);
                    AmbienceCalls.Whistle(x, rate, t, 0.008f, v => hz, v => Swell(v, 0.2f, 0.5f), 0.6f + 0.4f * u, 0.3f, 0.1f);
                    t += Dsp.Lerp(0.045f, 0.028f, u);
                }
                return x;
            }

            // A gray treefrog's trill: fast pulses on two bands an octave apart.
            private static float[] Treefrog(SynthGen g)
            {
                int rate = g.Rate;
                float dur = g.Range(0.5f, 1f), step = 1f / g.Range(32f, 48f), f = g.Range(2000f, 2400f);
                var x = new float[g.N(dur + 0.05f)];
                for (float t = 0f; t < dur; t += step)
                {
                    float a = Swell(t / dur, 0.15f, 0.2f);
                    AmbienceCalls.Whistle(x, rate, t, 0.012f, v => f, v => Swell(v, 0.2f, 0.5f), a, 0f, 0f);
                    AmbienceCalls.Whistle(x, rate, t, 0.012f, v => f * 0.5f, v => Swell(v, 0.2f, 0.5f), a * 0.6f, 0f, 0f);
                }
                return x;
            }

            // A green frog's "gunk": a loose banjo string plucked once.
            private static float[] Gunk(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(0.35f)];
                float f = g.Range(220f, 320f);
                Dsp.Modes(x, rate, 0.005f, 1f, new[] { f, f * 2f, f * 3.01f, f * 4.02f }, new[] { 1f, 0.7f, 0.45f, 0.25f }, new[] { 0.08f, 0.05f, 0.03f, 0.02f });
                Dsp.AddBurst(x, g, 0f, 0.3f, 0.002f, 0.01f, 2000f, 200f);
                return x;
            }
            #endregion

            #region Rain
            // Rain outdoors: the wash of countless drops all round (and, when heavy, the fizz of spray and the low roar of
            // water everywhere), surging in sheets; over it single drops — a few close and loud, most small and far —
            // ticking on hard ground, tapping leaves, plinking into puddles, and fat drips falling from the trees.
            internal static StereoBuf Rain(SynthGen g, float seconds, bool heavy)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    float[] sheet = Curve(n, rate, Dsp.Gusts(g, seconds, 1, heavy ? 4 : 3, 3));
                    StereoBuf wash = NoisePair(g, n, 0.2f, true);
                    Dsp.HP(wash.L, heavy ? 250f : 450f, rate);
                    Dsp.HP(wash.R, heavy ? 250f : 450f, rate);
                    Dsp.LP(wash.L, heavy ? 11000f : 8000f, rate);
                    Dsp.LP(wash.R, heavy ? 11000f : 8000f, rate);
                    StereoBuf fizz = null, roar = null;
                    if (heavy)
                    {
                        fizz = NoisePair(g, n, 0.1f, false);
                        Dsp.HP(fizz.L, 4500f, rate);
                        Dsp.HP(fizz.R, 4500f, rate);
                        roar = NoisePair(g, n, 0.6f, true);
                        Dsp.LP(roar.L, 350f, rate);
                        Dsp.LP(roar.R, 350f, rate);
                        Dsp.HP(roar.L, 40f, rate);
                        Dsp.HP(roar.R, 40f, rate);
                    }
                    for (int i = 0; i < n; i++)
                    {
                        float k = heavy ? 0.72f + 0.28f * sheet[i] : 0.85f + 0.15f * sheet[i];
                        float l = wash.L[i] * (heavy ? 1f : 0.35f), r = wash.R[i] * (heavy ? 1f : 0.35f);
                        if (heavy)
                        {
                            l += fizz.L[i] * 0.12f + roar.L[i] * 0.55f;
                            r += fizz.R[i] * 0.12f + roar.R[i] * 0.55f;
                        }
                        s.L[i] = l * k;
                        s.R[i] = r * k;
                    }
                }, s =>
                {
                    int n = s.Length;
                    int drops = (int)(seconds * (heavy ? 240f : 75f));
                    for (int d = 0; d < drops; d++)
                    {
                        float u = g.Next(), kind = g.Next();
                        float gain = (heavy ? 0.05f : 0.06f) + (heavy ? 0.3f : 0.7f) * u * u * u * u;
                        float[] ev = kind < 0.45f ? DropTick(g) : kind < 0.78f ? DropLeaf(g) : DropPlink(g);
                        Dsp.MixWrapPan(s, ev, g.Int(0, n), gain, g.Range(-0.95f, 0.95f));
                    }
                    if (heavy) return;
                    int drips = g.Int(6, 12);
                    for (int d = 0; d < drips; d++) Dsp.MixWrapPan(s, DropPlop(g), g.Int(0, n), g.Range(0.15f, 0.35f), g.Range(-0.9f, 0.9f));
                });
            }

            private static float[] DropTick(SynthGen g) => Dsp.Burst(g, 0.0002f, g.Range(0.0004f, 0.0014f), 9000f, g.Range(1800f, 3200f));

            private static float[] DropLeaf(SynthGen g)
            {
                var x = new float[g.N(0.03f)];
                float f = g.Range(900f, 2400f);
                Dsp.Modes(x, g.Rate, 0f, 0.6f, new[] { f, f * g.Range(1.6f, 2.3f) }, new[] { 1f, 0.5f }, new[] { g.Range(0.002f, 0.005f), 0.002f });
                Dsp.AddBurst(x, g, 0f, 0.5f, 0.0002f, 0.0006f, 7000f, 1200f);
                return x;
            }

            private static float[] DropPlink(SynthGen g)
            {
                float[] b = Dsp.Bubble(g.Rate, g.Range(1100f, 3200f), g.Range(0.008f, 0.025f), g.Range(0.4f, 1.4f));
                var x = new float[b.Length + g.N(0.004f)];
                Dsp.AddBurst(x, g, 0f, 0.35f, 0.0002f, 0.0005f, 8000f, 2000f);
                Dsp.Mix(x, b, g.N(0.0015f), 0.6f);
                return x;
            }

            // A fat drip from a branch or an eave into a puddle: a splash and a low, rising plop.
            private static float[] DropPlop(SynthGen g)
            {
                float[] b = Dsp.Bubble(g.Rate, g.Range(450f, 900f), g.Range(0.03f, 0.05f), g.Range(0.6f, 1.2f));
                var x = new float[b.Length + g.N(0.006f)];
                Dsp.AddBurst(x, g, 0f, 0.4f, 0.0003f, 0.0012f, 7000f, 900f);
                Dsp.Mix(x, b, g.N(0.002f), 0.8f);
                return x;
            }

            // Rain heard from indoors: the roof drumming overhead (a dull, dense patter through the boards) with louder
            // single taps and the odd tink on the flashing, rain ticking on the window panes either side, the gutter
            // overflowing in two unsteady drips, and the downspout gurgling to itself; a small room round it all.
            internal static StereoBuf RainRoof(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    float[] sheet = Curve(n, rate, Dsp.Gusts(g, seconds, 1, 3, 3));
                    StereoBuf drum = NoisePair(g, n, 0.65f, true);
                    Dsp.LP(drum.L, 1000f, rate);
                    Dsp.LP(drum.R, 1000f, rate);
                    Dsp.HP(drum.L, 90f, rate);
                    Dsp.HP(drum.R, 90f, rate);
                    var pl = new float[n];
                    var pr = new float[n];
                    Rustle(pl, g, (int)(seconds * 450f), 1f, 0.0008f, 0.003f, null);
                    Rustle(pr, g, (int)(seconds * 450f), 1f, 0.0008f, 0.003f, null);
                    Dsp.LP(pl, 1800f, rate);
                    Dsp.LP(pr, 1800f, rate);
                    Dsp.HP(pl, 150f, rate);
                    Dsp.HP(pr, 150f, rate);
                    for (int i = 0; i < n; i++)
                    {
                        float k = 0.8f + 0.2f * sheet[i];
                        s.L[i] = (drum.L[i] * 0.5f + pl[i] * 0.3f) * k;
                        s.R[i] = (drum.R[i] * 0.5f + pr[i] * 0.3f) * k;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    float loop = seconds;
                    int taps = (int)(loop * 14f);
                    for (int i = 0; i < taps; i++)
                    {
                        float u = g.Next();
                        scene.Put(RoofTap(g), g.Range(0f, loop), 0.08f + 0.4f * u * u * u * u, g.Range(-0.8f, 0.8f), 0.4f);
                    }
                    int tinks = (int)(loop * 1.5f);
                    for (int i = 0; i < tinks; i++) scene.Put(Tink(g), g.Range(0f, loop), g.Range(0.04f, 0.12f), g.Range(-0.7f, 0.7f), 0.5f);
                    for (int w = 0; w < 2; w++)
                    {
                        float pan = w == 0 ? -0.75f : 0.7f;
                        int ticks = (int)(loop * g.Range(4f, 7f));
                        for (int i = 0; i < ticks; i++) scene.Put(GlassTick(g), g.Range(0f, loop), g.Range(0.03f, 0.12f), pan + g.Range(-0.1f, 0.1f), 0.3f);
                    }
                    for (int d = 0; d < 2; d++)
                    {
                        float period = Fit(d == 0 ? g.Range(0.75f, 1f) : g.Range(1.2f, 1.7f), loop), pan = d == 0 ? g.Range(-0.6f, -0.3f) : g.Range(0.3f, 0.6f);
                        float hz = g.Range(700f, 1300f);
                        int count = (int)MathF.Round(loop / period);
                        for (int k = 0; k < count; k++)
                            if (!g.Chance(0.1f)) scene.Put(GutterDrip(g, hz * g.Range(0.95f, 1.05f)), k * period + g.Range(-0.05f, 0.05f), g.Range(0.5f, 0.75f), pan, 0.5f);
                    }
                    float spout = g.Range(0.55f, 0.85f) * (g.Chance(0.5f) ? 1f : -1f);
                    int bubbles = (int)(loop * 22f);
                    for (int i = 0; i < bubbles; i++)
                        scene.Put(Dsp.Bubble(rate, g.Range(380f, 1100f), g.Range(0.01f, 0.04f), g.Range(0.3f, 1f)), g.Range(0f, loop), g.Range(0.02f, 0.07f), spout, 0.5f);
                    scene.Space(0.3f, 0.6f, 0.3f, 0.008f);
                });
            }

            // One drop on the shingles above: a dull wooden knock through the boards.
            private static float[] RoofTap(SynthGen g)
            {
                var x = new float[g.N(0.04f)];
                float f = g.Range(260f, 900f);
                Dsp.Modes(x, g.Rate, 0f, 0.8f, new[] { f, f * g.Range(1.7f, 2.4f) }, new[] { 1f, 0.4f }, new[] { g.Range(0.004f, 0.01f), 0.004f });
                Dsp.AddBurst(x, g, 0f, 0.3f, 0.0003f, 0.0015f, 2000f, 100f);
                return x;
            }

            // A drop on the tin flashing.
            private static float[] Tink(SynthGen g)
            {
                var x = new float[g.N(0.08f)];
                float f = g.Range(2400f, 4200f);
                Dsp.Modes(x, g.Rate, 0f, 0.6f, new[] { f, f * 1.52f, f * 2.37f }, new[] { 1f, 0.5f, 0.3f }, new[] { g.Range(0.012f, 0.03f), 0.01f, 0.006f });
                return x;
            }

            // A drop on the window: a tiny tick and the pane's faint ring.
            private static float[] GlassTick(SynthGen g)
            {
                var x = new float[g.N(0.03f)];
                float f = g.Range(2800f, 5200f);
                Dsp.Modes(x, g.Rate, 0f, 0.5f, new[] { f, f * 1.73f }, new[] { 1f, 0.4f }, new[] { g.Range(0.003f, 0.008f), 0.003f });
                Dsp.AddBurst(x, g, 0f, 0.5f, 0.0001f, 0.0004f, 0f, 2500f);
                return x;
            }

            // The gutter dripping into a puddle below: a splash and a rising plip.
            private static float[] GutterDrip(SynthGen g, float hz)
            {
                float[] b = Dsp.Bubble(g.Rate, hz, g.Range(0.025f, 0.04f), g.Range(0.6f, 1.2f));
                var x = new float[b.Length + g.N(0.005f)];
                Dsp.AddBurst(x, g, 0f, 0.35f, 0.0003f, 0.001f, 6000f, 800f);
                Dsp.Mix(x, b, g.N(0.0015f), 0.9f);
                return x;
            }
            #endregion

            #region The Kettle
            // The roadhouse on a busy night: the babble of the whole room (speech-shaped noise flickering with syllables)
            // under a score of patrons talking at the tables — men and women, near and far, indistinct — a big laugh and a
            // giggle, glasses clinking, bottles set down on the bar, pool balls clacking, a chair scraped back, a stool
            // creaking; all in the room's warm wooden reverberation.
            internal static StereoBuf Bar(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return Bed(g, seconds, s =>
                {
                    int n = s.Length;
                    StereoBuf b = NoisePair(g, n, 0.3f, true);
                    Dsp.BP(b.L, 520f, 0.8f, rate);
                    Dsp.BP(b.R, 540f, 0.8f, rate);
                    Dsp.PeakEq(b.L, 1700f, 1f, 5f, rate);
                    Dsp.PeakEq(b.R, 1700f, 1f, 5f, rate);
                    var fr = new float[6];
                    var ph = new float[6];
                    for (int k = 0; k < 6; k++)
                    {
                        fr[k] = g.Range(2.5f, 6.5f);
                        ph[k] = g.Next() * TwoPi;
                    }
                    float[] mod = Curve(n, rate, t =>
                    {
                        float v = 0f;
                        for (int k = 0; k < 6; k++) v += MathF.Sin(TwoPi * fr[k] * t + ph[k]);
                        return 0.55f + 0.45f * Math.Max(-1f, Math.Min(1f, v / 2.4f));
                    });
                    StereoBuf tone = NoisePair(g, n, 0.8f, true);
                    Dsp.LP(tone.L, 120f, rate);
                    Dsp.LP(tone.R, 120f, rate);
                    Dsp.HP(tone.L, 30f, rate);
                    Dsp.HP(tone.R, 30f, rate);
                    for (int i = 0; i < n; i++)
                    {
                        s.L[i] = b.L[i] * mod[i] * 0.55f + tone.L[i] * 0.08f;
                        s.R[i] = b.R[i] * mod[i] * 0.55f + tone.R[i] * 0.08f;
                    }
                }, s =>
                {
                    var scene = new Scene(s, rate);
                    float loop = seconds;
                    const int Talkers = 16;
                    for (int i = 0; i < Talkers; i++)
                    {
                        bool female = g.Chance(0.38f);
                        float f0 = female ? g.Range(175f, 240f) : g.Range(92f, 145f), dur = g.Range(0.8f, 2.4f), d = g.Range(0.25f, 1f);
                        float[] v = Far(Talk(g, dur, f0, female), rate, d, 7000f, 2200f);
                        scene.Put(v, (i + g.Next()) * loop / Talkers, Dsp.Lerp(0.85f, 0.3f, d), g.Range(-0.85f, 0.85f), Dsp.Lerp(0.5f, 1f, d));
                    }
                    scene.Put(Far(Laugh(g, false), rate, g.Range(0.3f, 0.6f), 7000f, 2500f), g.Range(1f, loop * 0.45f), 0.6f, g.Range(-0.7f, 0.7f), 0.7f);
                    scene.Put(Far(Laugh(g, true), rate, g.Range(0.4f, 0.8f), 7000f, 2500f), g.Range(loop * 0.55f, loop - 1f), 0.45f, g.Range(-0.7f, 0.7f), 0.8f);
                    int clinks = g.Int(6, 10);
                    for (int i = 0; i < clinks; i++) scene.Put(Clink(g), g.Range(0f, loop), g.Range(0.1f, 0.25f), g.Range(-0.8f, 0.8f), 0.6f);
                    int bottles = g.Int(2, 4);
                    for (int i = 0; i < bottles; i++) scene.Put(Bottle(g), g.Range(0f, loop), g.Range(0.2f, 0.32f), g.Range(-0.5f, 0.3f), 0.5f);
                    for (int i = 0; i < 2; i++) scene.Put(PoolClack(g), g.Range(0f, loop), g.Range(0.15f, 0.25f), g.Range(0.4f, 0.8f), 0.6f);
                    for (int i = 0; i < 2; i++) scene.Put(ChairScrape(g), g.Range(0f, loop), g.Range(0.15f, 0.25f), g.Range(-0.8f, 0.8f), 0.6f);
                    scene.Put(Far(AmbienceCalls.Creak(g, false), rate, 0.3f, 6000f, 3000f), g.Range(0f, loop), 0.12f, g.Range(-0.6f, 0.6f), 0.5f);
                    scene.Space(0.45f, 0.55f, 0.6f, 0.012f);
                });
            }

            // One patron's phrase: syllables of vowels on a falling, accented pitch, with a hiss or a tick between some.
            private static float[] Talk(SynthGen g, float seconds, float f0, bool female)
            {
                int syl = Math.Max(2, (int)(seconds * g.Range(3.8f, 5.2f)));
                var vowels = new Vowel[syl + 1];
                var acc = new float[syl + 1];
                for (int i = 0; i <= syl; i++)
                {
                    vowels[i] = Vowel.Speech[g.Int(0, Vowel.Speech.Length)];
                    acc[i] = g.Range(0.45f, 1f);
                }
                float scale = female ? 1.17f : 1f;
                float[] v = Dsp.Voice(g, seconds, new VoiceParams
                {
                    F0 = u => f0 * (1.1f - 0.2f * u) * (0.93f + 0.14f * acc[Math.Min(syl, (int)(u * syl))]),
                    Formants = u =>
                    {
                        float p = u * syl;
                        int j = Math.Min(syl - 1, (int)p);
                        return Vowel.Lerp(vowels[j], vowels[j + 1], Dsp.SmoothStep((p - j - 0.5f) * 2f)).Scale(scale);
                    },
                    Amp = u =>
                    {
                        float p = u * syl;
                        int j = Math.Min(syl - 1, (int)p);
                        float f = p - j;
                        float e = f < 0.85f ? MathF.Sin(Dsp.Pi * f / 0.85f) : 0f;
                        return acc[j] * e * Swell(u, 0.04f, 0.1f);
                    },
                    Breath = 0.16f, Jitter = 0.02f, Tilt = female ? 2800f : 2200f, Direct = 0.04f,
                });
                for (int i = 1; i < syl; i++)
                    if (g.Chance(0.4f)) Dsp.AddBurst(v, g, i * seconds / syl, g.Range(0.03f, 0.1f), 0.004f, g.Range(0.008f, 0.03f), 8000f, 2500f);
                return v;
            }

            // A laugh: breathy "ha" pulses, falling in pitch.
            private static float[] Laugh(SynthGen g, bool female)
            {
                int pulses = g.Int(4, 8);
                float pace = g.Range(4.2f, 5.5f), dur = pulses / pace + 0.25f;
                float f0 = female ? g.Range(280f, 380f) : g.Range(150f, 220f);
                return Dsp.Voice(g, dur, new VoiceParams
                {
                    F0 = u => f0 * (1.15f - 0.3f * u),
                    Formants = u => female ? Vowel.Ae.Scale(1.12f) : Vowel.A,
                    Amp = u =>
                    {
                        float p = u * dur * pace;
                        if (p >= pulses) return 0f;
                        float f = p - (int)p;
                        float e = f < 0.55f ? MathF.Sin(Dsp.Pi * f / 0.55f) : 0f;
                        return e * (1f - 0.5f * p / pulses);
                    },
                    Breath = 0.45f, Jitter = 0.03f, Rough = 0.15f, Tilt = 2500f,
                });
            }

            // Two glasses touching: bright, inharmonic rings a hair apart in pitch, beating.
            private static float[] Clink(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(0.9f)];
                float f = g.Range(2000f, 3300f), f2 = f * g.Range(1.04f, 1.15f);
                Dsp.Modes(x, rate, 0.001f, 0.6f, new[] { f, f * 2.32f, f * 4.1f }, new[] { 1f, 0.45f, 0.2f }, new[] { g.Range(0.15f, 0.35f), 0.08f, 0.04f });
                Dsp.Modes(x, rate, 0.001f + g.Range(0f, 0.003f), 0.45f, new[] { f2, f2 * 2.28f, f2 * 4.05f }, new[] { 1f, 0.45f, 0.2f }, new[] { g.Range(0.12f, 0.3f), 0.07f, 0.035f });
                Dsp.AddBurst(x, g, 0f, 0.25f, 0.0002f, 0.0008f, 0f, 3000f);
                return x;
            }

            // A bottle set down on the bar top: a wooden knock with a short glassy ring.
            private static float[] Bottle(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(0.5f)];
                Dsp.Modes(x, rate, 0f, 0.8f, new[] { g.Range(160f, 240f), g.Range(380f, 520f), g.Range(900f, 1200f) }, new[] { 1f, 0.6f, 0.3f }, new[] { 0.03f, 0.02f, 0.012f });
                float f = g.Range(900f, 1500f);
                Dsp.Modes(x, rate, 0.001f, 0.25f, new[] { f, f * 2.7f, f * 5.1f }, new[] { 1f, 0.4f, 0.2f }, new[] { 0.12f, 0.05f, 0.02f });
                Dsp.AddBurst(x, g, 0f, 0.3f, 0.0003f, 0.002f, 4000f, 200f);
                return x;
            }

            // Pool balls: a hard, bright clack, sometimes a second ball touched after it.
            private static float[] PoolClack(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(0.4f)];
                int hits = g.Chance(0.5f) ? 2 : 1;
                float at = 0f;
                for (int h = 0; h < hits; h++)
                {
                    float f = g.Range(2600f, 3600f), a = h == 0 ? 1f : 0.5f;
                    Dsp.Modes(x, rate, at, a, new[] { f, f * 1.48f, f * 2.6f }, new[] { 1f, 0.5f, 0.25f }, new[] { 0.012f, 0.008f, 0.004f });
                    Dsp.AddBurst(x, g, at, 0.6f * a, 0.0001f, 0.0006f, 0f, 1500f);
                    at += g.Range(0.08f, 0.25f);
                }
                return x;
            }

            // A chair dragged back on the plank floor: stick-slip judder through its legs.
            private static float[] ChairScrape(SynthGen g)
            {
                int rate = g.Rate;
                float dur = g.Range(0.35f, 0.7f);
                float[] c = Dsp.Creak(g, dur, u => 35f + 50f * u, u => Swell(u, 0.15f, 0.3f),
                    new[] { g.Range(250f, 400f), g.Range(700f, 1100f), g.Range(1800f, 2600f) }, new[] { 5f, 4f, 3f }, 0.35f);
                float[] nz = Dsp.White(g, c.Length);
                Dsp.BP(nz, 1200f, 0.9f, rate);
                for (int i = 0; i < c.Length; i++) c[i] += nz[i] * Swell(i / (float)c.Length, 0.15f, 0.3f) * 0.6f;
                return c;
            }
            #endregion

            #region Thunder
            // Thunder. Near: the bolt's ripping run of cracks and its crack (a sharp broadband report with a blast and a deep
            // boom), splitting into a rolling rumble — overlapping swells as sound arrives from further and further along the
            // bolt — with echoes of the crack off hills and treelines. Far: only the low, long roll, swelling in.
            internal static float[] Thunder(SynthGen g, bool near)
            {
                int rate = g.Rate;
                float len = near ? g.Range(5.6f, 6.6f) : g.Range(6.2f, 7.6f);
                int n = g.N(len);
                var env = new float[n];
                int rolls = near ? g.Int(7, 11) : g.Int(5, 9);
                float first = near ? 0.16f : g.Range(0f, 0.03f), span = len - first - 1.8f;
                for (int r = 0; r < rolls; r++)
                {
                    float at = r == 0 ? first : first + span * MathF.Pow(Dsp.Clamp01((r + g.Range(0f, 0.8f)) / rolls), 1.35f);
                    float amp = MathF.Exp(-(at - first) / (near ? 1.9f : 2.8f)) * (r == 0 ? g.Range(0.75f, 1f) : g.Range(0.45f, 1f));
                    float atk = r == 0 ? g.Range(0.03f, 0.07f) : g.Range(0.05f, near ? 0.2f : 0.45f), dec = g.Range(0.35f, 1.1f);
                    int i0 = (int)(at * rate);
                    for (int i = Math.Max(0, i0); i < n; i++)
                    {
                        float t = (i - i0) / (float)rate;
                        if (t > atk + dec * 8f) break;
                        env[i] += amp * Dsp.AD(t, atk, dec);
                    }
                }
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    float bed = 0.12f * MathF.Exp(-t / 2.5f) * Dsp.SmoothStep((t - first + 0.1f) / 0.4f);
                    env[i] = MathF.Tanh((env[i] + bed) * 1.2f) * Dsp.SmoothStep((len - t) / 0.9f);
                }
                float[] rum = Dsp.Brown(g, n);
                Dsp.SweepLP(rum, rate, 0.75f, t => (near ? 340f : 210f) * MathF.Exp(-t / (near ? 2.6f : 3.5f)) + (near ? 95f : 80f));
                float[] mid = Dsp.Pink(g, n);
                Dsp.BP(mid, near ? 520f : 360f, 0.7f, rate);
                var x = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)rate;
                    x[i] = rum[i] * env[i] + mid[i] * env[i] * (near ? 0.5f : 0.22f) * MathF.Exp(-t / (near ? 1.4f : 2f));
                }
                if (near)
                {
                    float rip = g.Range(0.06f, 0.16f);
                    var crack = new float[g.N(0.7f)];
                    int cracks = g.Int(25, 55);
                    for (int c = 0; c < cracks; c++)
                    {
                        float u = c / (float)cracks;
                        Dsp.AddBurst(crack, g, rip * MathF.Pow(u, 0.6f) * g.Range(0.9f, 1.05f), 0.12f + 0.45f * u * u, 0.0002f, g.Range(0.0004f, 0.0025f), 9000f, 600f);
                    }
                    Dsp.AddBurst(crack, g, rip, 1.3f, 0.0003f, 0.007f, 11000f, 250f);
                    Dsp.AddBurst(crack, g, rip + 0.004f, 0.8f, 0.002f, 0.05f, 2500f, 90f);
                    Dsp.Thump(crack, rate, rip, 1.1f, 90f, 36f, 0.18f, 0.006f, 0.35f);
                    Dsp.Mix(x, crack, 0);
                    float[] echo = (float[])crack.Clone();
                    Dsp.LP(echo, 1400f, rate);
                    Dsp.Mix(x, echo, g.N(g.Range(0.35f, 0.6f)), 0.32f);
                    Dsp.Mix(x, echo, g.N(g.Range(0.8f, 1.2f)), 0.22f);
                    Dsp.Mix(x, echo, g.N(g.Range(1.5f, 2.2f)), 0.12f);
                }
                Dsp.HP(x, 26f, rate);
                Dsp.Norm(x);
                Dsp.Saturate(x, near ? 2.2f : 1.6f);
                return x;
            }
            #endregion

            #region Fire and heater
            // A wood fire (the woodstove, a hearth): the draught's low breathy roar swelling and sinking with the flames, a
            // flutter of flame, clusters of tiny sharp crackles (many quiet, a few loud), pops of sap bursting with a woody
            // knock, and now and then the hiss of steam from wet wood.
            internal static float[] Fire(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return MonoBed(g, seconds, x =>
                {
                    int n = x.Length;
                    float[] flick = Curve(n, rate, Dsp.Gusts(g, seconds, 3, 9, 4));
                    float[] roar = Dsp.Brown(g, n);
                    Dsp.LP(roar, 260f, rate);
                    Dsp.HP(roar, 35f, rate);
                    float[] flame = Dsp.Pink(g, n);
                    Dsp.BP(flame, 700f, 0.7f, rate);
                    for (int i = 0; i < n; i++) x[i] = roar[i] * (0.5f + 0.5f * flick[i]) * 0.45f + flame[i] * flick[i] * flick[i] * 0.3f;
                }, x =>
                {
                    int n = x.Length;
                    int clusters = (int)(seconds * 14f);
                    for (int c = 0; c < clusters; c++)
                    {
                        int at = g.Int(0, n), ticks = g.Int(1, 7);
                        float lvl = 0.05f + 0.6f * MathF.Pow(g.Next(), 3f), hz = g.Range(1800f, 5500f);
                        for (int k = 0; k < ticks; k++)
                        {
                            float[] tk = Dsp.Burst(g, 0.0001f, g.Range(0.0002f, 0.0012f), 0f, 900f);
                            if (g.Chance(0.5f)) Dsp.PeakEq(tk, hz, 2f, 9f, rate);
                            Dsp.MixWrap(x, tk, at + g.N(g.Range(0f, 0.06f)), lvl * g.Range(0.5f, 1f));
                        }
                    }
                    int pops = g.Int(4, 8);
                    for (int p = 0; p < pops; p++)
                    {
                        var pop = new float[g.N(0.12f)];
                        Dsp.AddBurst(pop, g, 0f, 1f, 0.0001f, g.Range(0.001f, 0.003f), 8000f, 600f);
                        Dsp.Modes(pop, rate, 0f, 0.4f, new[] { g.Range(300f, 600f), g.Range(900f, 1500f) }, new[] { 1f, 0.5f }, new[] { 0.015f, 0.008f });
                        Dsp.MixWrap(x, pop, g.Int(0, n), g.Range(0.5f, 1f));
                    }
                    int hisses = g.Int(1, 3);
                    for (int h = 0; h < hisses; h++)
                    {
                        float dur = g.Range(0.3f, 0.9f);
                        float[] hs = Dsp.White(g, g.N(dur));
                        Dsp.BP(hs, g.Range(3000f, 4500f), 1.5f, rate);
                        Dsp.Shape(hs, rate, t => Swell(t / dur, 0.2f, 0.4f));
                        Dsp.MixWrap(x, hs, g.Int(0, n), 0.1f);
                    }
                });
            }

            // A kerosene heater: the wick's steady flame, a soft even rush with the faintest flutter, and the steel ticking
            // as it expands.
            internal static float[] Heater(SynthGen g, float seconds)
            {
                int rate = g.Rate;
                return MonoBed(g, seconds, x =>
                {
                    int n = x.Length;
                    float[] a = Dsp.Pink(g, n);
                    Dsp.BP(a, 1100f, 0.6f, rate);
                    Dsp.LP(a, 5000f, rate);
                    float[] low = Dsp.Brown(g, n);
                    Dsp.LP(low, 180f, rate);
                    Dsp.HP(low, 35f, rate);
                    float[] flutter = Curve(n, rate, Dsp.Gusts(g, seconds, 4, 12, 3));
                    for (int i = 0; i < n; i++) x[i] = a[i] * (0.85f + 0.15f * flutter[i]) * 0.5f + low[i] * 0.2f;
                }, x =>
                {
                    for (int k = 0; k < 3; k++)
                    {
                        var tk = new float[g.N(0.1f)];
                        Dsp.Modes(tk, rate, 0f, 1f, new[] { g.Range(2200f, 3800f), g.Range(5000f, 7000f) }, new[] { 1f, 0.4f }, new[] { 0.02f, 0.01f });
                        Dsp.AddBurst(tk, g, 0f, 0.5f, 0.0001f, 0.0005f, 0f, 2000f);
                        Dsp.MixWrap(x, tk, g.Int(0, x.Length), g.Range(0.15f, 0.3f));
                    }
                });
            }
            #endregion
        }

        // Wildlife, built from what the real calls look like on a spectrogram: songbirds' whistled notes with their slurs
        // and trills, owls' hoots, the crow's caw, coyotes and far-off dogs from formant voices, woodpeckers drumming on
        // dead wood, and the creak and crack of trees.
        private static class AmbienceCalls
        {
            private const float TwoPi = Dsp.TwoPi;

            // A whistled note into x at `at` seconds: a sine with a touch of 2nd and 3rd harmonic, pitch hz(u) and level
            // amp(u) over u = 0..1 of the note.
            internal static void Whistle(float[] x, int rate, float at, float dur, Func<float, float> hz, Func<float, float> amp, float gain, float h2 = 0.05f, float h3 = 0.015f)
            {
                int i0 = (int)(at * rate), n = Math.Max(2, (int)(dur * rate));
                float ph = 0f, dt = 1f / rate, inv = 1f / (n - 1);
                for (int i = 0; i < n; i++)
                {
                    int k = i0 + i;
                    if (k >= x.Length) break;
                    float u = i * inv;
                    ph += hz(u) * dt;
                    ph -= MathF.Floor(ph);
                    if (k < 0) continue;
                    float p = TwoPi * ph;
                    x[k] += gain * amp(u) * (MathF.Sin(p) + h2 * MathF.Sin(2f * p) + h3 * MathF.Sin(3f * p));
                }
            }

            private static float Swell(float u, float attack, float release) => AmbienceKit.Swell(u, attack, release);

            // Cuts a call's buffer to `seconds`.
            private static float[] Trim(float[] x, SynthGen g, float seconds)
            {
                int n = Math.Min(x.Length, g.N(seconds));
                if (n < x.Length) Array.Resize(ref x, n);
                return x;
            }

            // Room after a call for its tail.
            private static float[] Pad(float[] x, SynthGen g, float seconds)
            {
                Array.Resize(ref x, x.Length + g.N(seconds));
                return x;
            }

            // Drops the near-silence before a call's first sound (a slow creak's first slip comes a fraction of a second in).
            private static float[] Lead(float[] x, int rate)
            {
                float lim = Dsp.Peak(x) * 0.02f;
                int i = 0;
                while (i < x.Length && Math.Abs(x[i]) < lim) i++;
                i = Math.Max(0, i - rate / 200);
                if (i == 0 || i >= x.Length) return x;
                var y = new float[x.Length - i];
                Array.Copy(x, i, y, 0, y.Length);
                return y;
            }

            // The light reverberation of the woods round a call, with a clean end.
            private static float[] Air(float[] x, int rate, float wet)
            {
                Dsp.Reverb(x, rate, 0.7f, 0.5f, wet);
                return AmbienceKit.EndFade(x, rate, 0.25f);
            }

            #region Songbirds
            // Northern cardinal: clear slurred whistles. Song 0: an upslurred "what" or two, then "cheer, cheer, cheer" (each
            // a long fall from ~4.2 kHz to ~1.9 kHz), sometimes a quicker "chew-chew-chew" after; song 1: the rapid
            // "birdie-birdie-birdie", each note dipping and rising.
            internal static float[] CardinalSong(SynthGen g, int type)
            {
                int rate = g.Rate;
                var x = new float[g.N(3.2f)];
                float k = g.Range(0.93f, 1.07f), t = 0.03f;
                if (type == 0)
                {
                    int intro = g.Chance(0.65f) ? g.Int(1, 3) : 0;
                    for (int i = 0; i < intro; i++)
                    {
                        float d = g.Range(0.2f, 0.3f), lo = k * g.Range(1500f, 1900f), hi = k * g.Range(3300f, 3900f);
                        Whistle(x, rate, t, d, u => lo + (hi - lo) * u * u, u => Swell(u, 0.25f, 0.2f), 0.75f, 0.04f, 0.01f);
                        t += d + g.Range(0.08f, 0.14f);
                    }
                    int reps = g.Int(4, 8);
                    float period = g.Range(0.22f, 0.3f), d1 = period * g.Range(0.62f, 0.75f);
                    float hi1 = k * g.Range(3900f, 4500f), lo1 = k * g.Range(1700f, 2100f);
                    for (int r = 0; r < reps && t + d1 < 2.6f; r++)
                    {
                        Whistle(x, rate, t, d1, u => lo1 + (hi1 - lo1) * MathF.Pow(1f - u, 1.6f), u => Swell(u, 0.08f, 0.35f), 1f - 0.2f * r / reps, 0.04f, 0.01f);
                        t += period * g.Range(0.97f, 1.03f);
                    }
                    if (g.Chance(0.45f))
                    {
                        int chews = g.Int(3, 6);
                        float hi2 = k * g.Range(2700f, 3100f), lo2 = k * g.Range(1700f, 2000f);
                        for (int r = 0; r < chews && t < 2.9f; r++)
                        {
                            Whistle(x, rate, t, 0.08f, u => lo2 + (hi2 - lo2) * (1f - u), u => Swell(u, 0.12f, 0.35f), 0.7f, 0.04f, 0.01f);
                            t += g.Range(0.11f, 0.13f);
                        }
                    }
                }
                else
                {
                    int reps = g.Int(8, 13);
                    float period = g.Range(0.13f, 0.17f), d1 = period * 0.72f;
                    float top = k * g.Range(3300f, 3900f), bottom = k * g.Range(2000f, 2400f);
                    for (int r = 0; r < reps && t + d1 < 2.6f; r++)
                    {
                        Whistle(x, rate, t, d1, u => u < 0.4f ? top - (top - bottom) * (u / 0.4f) : bottom + (top * 1.08f - bottom) * ((u - 0.4f) / 0.6f),
                            u => Swell(u, 0.12f, 0.25f), r == 0 ? 0.75f : 1f, 0.04f, 0.01f);
                        t += period;
                    }
                }
                return Trim(x, g, t + 0.15f);
            }

            internal static float[] Cardinal(SynthGen g)
            {
                return Air(Pad(CardinalSong(g, g.Variation % 2), g, 0.6f), g.Rate, 0.18f);
            }

            // Carolina wren: a loud rolling "tea-kettle, tea-kettle, tea-kettle" — a phrase of two to four fast sweeping
            // notes (some warbled) repeated three to six times.
            internal static float[] Wren(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(2.6f)];
                int notes = g.Int(2, 5);
                var f0 = new float[notes];
                var f1 = new float[notes];
                var d = new float[notes];
                var fm = new float[notes];
                for (int i = 0; i < notes; i++)
                {
                    f0[i] = g.Range(2600f, 5600f);
                    f1[i] = f0[i] > 4000f ? f0[i] - g.Range(1400f, 2600f) : f0[i] + g.Range(1400f, 2600f);
                    d[i] = g.Range(0.045f, 0.085f);
                    fm[i] = g.Chance(0.4f) ? g.Range(40f, 90f) : 0f;
                }
                float gap = g.Range(0.035f, 0.07f), t = 0.03f;
                int reps = g.Int(3, 7);
                for (int r = 0; r < reps && t < 2.2f; r++)
                {
                    for (int i = 0; i < notes; i++)
                    {
                        float a0 = f0[i], a1 = f1[i], w = fm[i], dur = d[i];
                        Whistle(x, rate, t, dur, u => (a0 + (a1 - a0) * Dsp.SmoothStep(u)) * (1f + 0.05f * MathF.Sin(TwoPi * w * u * dur)),
                            u => Swell(u, 0.15f, 0.3f), (r == 0 ? 0.85f : 1f) * (i == 0 ? 1f : 0.85f), 0.12f, 0.04f);
                        t += dur + 0.012f;
                    }
                    t += gap;
                }
                return Trim(x, g, t + 0.1f);
            }

            internal static float[] WrenSong(SynthGen g)
            {
                return Air(Pad(Wren(g), g, 0.6f), g.Rate, 0.18f);
            }

            // Carolina chickadee: the four-note "fee-bee, fee-bay" (pure whistles, the second and fourth lower) or the
            // "chick-a-dee-dee-dee" call: two quick high slurs and buzzy "dee" notes.
            internal static float[] Chickadee(SynthGen g, bool call)
            {
                int rate = g.Rate;
                var x = new float[g.N(2.2f)];
                float k = g.Range(0.95f, 1.05f), t = 0.03f;
                if (!call)
                {
                    float[] hz = { 6300f, 4300f, 5900f, 3700f };
                    for (int i = 0; i < 4; i++)
                    {
                        float d = g.Range(0.15f, 0.21f), f = hz[i] * k * g.Range(0.98f, 1.02f);
                        Whistle(x, rate, t, d, u => f * (1.015f - 0.035f * u), u => Swell(u, 0.18f, 0.3f), i % 2 == 0 ? 0.75f : 1f, 0.02f, 0f);
                        t += d + (i == 1 ? g.Range(0.12f, 0.18f) : g.Range(0.05f, 0.08f));
                    }
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        float f = k * g.Range(7000f, 7800f);
                        Whistle(x, rate, t, 0.035f, u => f - 2600f * u, u => Swell(u, 0.2f, 0.4f), 0.65f, 0.05f, 0f);
                        t += 0.065f;
                    }
                    t += 0.03f;
                    int dees = g.Int(3, 6);
                    for (int i = 0; i < dees; i++)
                    {
                        Dee(x, g, t, g.Range(0.11f, 0.14f), k * g.Range(380f, 440f), k * 3600f);
                        t += g.Range(0.15f, 0.18f);
                    }
                }
                return Trim(x, g, t + 0.1f);
            }

            // A buzzy "dee": a harmonic-rich pulse heard through a nasal band round `centre`.
            private static void Dee(float[] x, SynthGen g, float at, float dur, float f0, float centre)
            {
                int rate = g.Rate, n = (int)(dur * rate);
                var b = new float[n];
                float ph = 0f, dt = f0 / rate;
                for (int i = 0; i < n; i++)
                {
                    ph += dt;
                    if (ph >= 1f) ph -= 1f;
                    b[i] = Dsp.Saw(ph, dt) * Swell(i / (float)n, 0.12f, 0.3f);
                }
                var hi = (float[])b.Clone();
                Dsp.BP(b, centre, 4f, rate);
                Dsp.BP(hi, centre * 1.6f, 3f, rate);
                for (int i = 0; i < n; i++) b[i] += 0.4f * hi[i];
                Dsp.Norm(b);
                Dsp.Mix(x, b, (int)(at * rate), 0.9f);
            }

            internal static float[] ChickadeeSong(SynthGen g)
            {
                return Air(Pad(Chickadee(g, g.Variation % 2 == 1), g, 0.6f), g.Rate, 0.18f);
            }

            // Tufted titmouse: "peter-peter-peter" — a higher note slurring down into a lower one, the pair repeated.
            internal static float[] Titmouse(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(2.6f)];
                float k = g.Range(0.94f, 1.06f), t = 0.03f;
                int reps = g.Int(4, 8);
                float period = g.Range(0.24f, 0.3f), hiA = k * g.Range(3300f, 3700f), loB = k * g.Range(2300f, 2600f);
                for (int r = 0; r < reps && t + period < 2.4f; r++)
                {
                    Whistle(x, rate, t, 0.07f, u => hiA * (1.03f - 0.06f * u), u => Swell(u, 0.15f, 0.3f), 0.8f, 0.04f, 0f);
                    Whistle(x, rate, t + 0.085f, 0.11f, u => loB * (1.06f - 0.1f * u), u => Swell(u, 0.12f, 0.35f), 1f, 0.04f, 0f);
                    t += period;
                }
                return Trim(x, g, t + 0.1f);
            }

            internal static float[] TitmouseSong(SynthGen g) => Air(Pad(Titmouse(g), g, 0.6f), g.Rate, 0.18f);

            // Blue jay: harsh, nasal "jay! jay!" screams (a rough pulse with an arched pitch through two bright bands), or
            // the liquid, bell-like "queedle" of rising and falling notes.
            internal static float[] Jay(SynthGen g, bool queedle)
            {
                int rate = g.Rate;
                var x = new float[g.N(2.4f)];
                float t = 0.03f;
                if (!queedle)
                {
                    int calls = g.Int(2, 5);
                    float f0 = g.Range(1150f, 1450f);
                    for (int c = 0; c < calls && t < 1.9f; c++)
                    {
                        float dur = g.Range(0.24f, 0.36f);
                        Screech(x, g, t, dur, f0 * g.Range(0.96f, 1.04f), 1f - 0.12f * c);
                        t += dur + g.Range(0.12f, 0.22f);
                    }
                }
                else
                {
                    int notes = g.Int(2, 4);
                    float f = g.Range(1600f, 2000f);
                    for (int c = 0; c < notes; c++)
                    {
                        Whistle(x, rate, t, 0.16f, u => f + 900f * MathF.Sin(Dsp.Pi * u), u => Swell(u, 0.2f, 0.3f), 1f, 0.3f, 0.15f);
                        t += g.Range(0.3f, 0.4f);
                    }
                }
                return Trim(x, g, t + 0.1f);
            }

            private static void Screech(float[] x, SynthGen g, float at, float dur, float f0, float gain)
            {
                int rate = g.Rate, n = (int)(dur * rate);
                var b = new float[n];
                float ph = 0f;
                OnePole jit = OnePole.At(60f, rate);
                float jk = jit.NoiseNormaliser;
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)n;
                    float f = f0 * (0.9f + 0.18f * MathF.Sin(Dsp.Pi * Math.Min(1f, u * 1.4f))) * (1f + 0.025f * jk * jit.Lp(g.Bipolar()));
                    float dt = f / rate;
                    ph += dt;
                    if (ph >= 1f) ph -= 1f;
                    b[i] = (Dsp.Saw(ph, dt) + 0.35f * g.Bipolar()) * Swell(u, 0.08f, 0.35f);
                }
                var hi = (float[])b.Clone();
                Dsp.BP(b, 2600f, 2.2f, rate);
                Dsp.BP(hi, 4300f, 2.5f, rate);
                for (int i = 0; i < n; i++) b[i] += 0.6f * hi[i];
                Dsp.Norm(b);
                Dsp.Mix(x, b, (int)(at * rate), gain);
            }

            internal static float[] JayCall(SynthGen g)
            {
                return Air(Pad(Jay(g, g.Variation % 2 == 1), g, 0.6f), g.Rate, 0.2f);
            }
            #endregion

            #region Woodpeckers, trees
            // Drumming on a resonant dead limb, building in then fading: kind 0 a pileated woodpecker's big, slowing roll;
            // 1 a hairy woodpecker's fast, even burst; 2 a downy's short roll after a "pik" or two.
            internal static float[] Drumming(SynthGen g, int kind)
            {
                var x = new float[g.N(2.8f)];
                float end;
                switch (kind)
                {
                    case 0: end = Drum(x, g, 0.03f, g.Range(13f, 16f), g.Int(22, 30), g.Range(330f, 430f), 1f, 0.25f); break;
                    case 1: end = Drum(x, g, 0.03f, g.Range(24f, 27f), g.Int(20, 28), g.Range(650f, 800f), 0.8f, 0.05f); break;
                    default:
                        Pik(x, g, 0.03f);
                        Pik(x, g, g.Range(0.35f, 0.5f));
                        end = Drum(x, g, g.Range(0.8f, 1f), g.Range(16f, 18f), g.Int(12, 17), g.Range(800f, 1000f), 0.75f, 0.05f);
                        break;
                }
                return Trim(x, g, end + 0.1f);
            }

            // Strikes on wood (a beak's click and the limb's modes); returns when the roll ends.
            private static float Drum(float[] x, SynthGen g, float at, float perSecond, int strikes, float hz, float gain, float slowing)
            {
                int rate = g.Rate;
                float t = at;
                for (int i = 0; i < strikes; i++)
                {
                    float u = strikes > 1 ? i / (float)(strikes - 1) : 0f;
                    float a = gain * Math.Min(1f, 0.35f + u * 5f) * (1f - 0.55f * u * u);
                    float f = hz * g.Range(0.97f, 1.03f);
                    Dsp.Modes(x, rate, t, a, new[] { f, f * 1.9f, f * 3.1f, f * 4.6f }, new[] { 1f, 0.55f, 0.3f, 0.15f }, new[] { 0.014f, 0.009f, 0.006f, 0.004f });
                    Dsp.AddBurst(x, g, t, a * 0.5f, 0.0001f, 0.0006f, 0f, 1500f);
                    t += (1f + slowing * u) / perSecond * g.Range(0.98f, 1.02f);
                }
                return t;
            }

            // A downy woodpecker's "pik": a short, sharp high note.
            private static void Pik(float[] x, SynthGen g, float at)
            {
                float f = g.Range(4200f, 5000f);
                Whistle(x, g.Rate, at, 0.03f, u => f * (1.05f - 0.15f * u), u => Swell(u, 0.15f, 0.5f), 0.6f, 0.2f, 0.05f);
            }

            internal static float[] Woodpecker(SynthGen g)
            {
                return Air(Pad(Drumming(g, g.Variation % 3), g, 0.7f), g.Rate, 0.25f);
            }

            // A tree creaking in the wind: a stick-slip pulse train (its rate swelling and easing) through a trunk's or a
            // limb's narrow resonances — low and slow for a big trunk, higher and quicker for a limb.
            internal static float[] Creak(SynthGen g, bool low)
            {
                float dur = g.Range(1.2f, 2.2f);
                float r0 = low ? g.Range(4f, 7f) : g.Range(12f, 20f), r1 = r0 * g.Range(1.8f, 3f);
                float[] res = low ? new[] { g.Range(160f, 220f), g.Range(380f, 480f), g.Range(800f, 1000f) }
                                  : new[] { g.Range(300f, 420f), g.Range(700f, 900f), g.Range(1400f, 1800f) };
                float[] c = Dsp.Creak(g, dur, u => r0 + (r1 - r0) * MathF.Sin(Dsp.Pi * u), u => Swell(u, 0.06f, 0.3f), res, new[] { 7f, 9f, 6f }, 0.3f);
                Dsp.LP(c, 3500f, g.Rate);
                return c;
            }

            internal static float[] BranchCreak(SynthGen g)
            {
                return Air(Pad(Lead(Creak(g, g.Variation % 2 == 0), g.Rate), g, 0.6f), g.Rate, 0.25f);
            }

            // A tree cracking far off — frost splitting a trunk (variation 0): a sharp report, the trunk's thump, a
            // splintering run; or a limb giving way (1): the crack, the rush of it falling through the branches, the crash.
            internal static float[] TreeCrack(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(3.6f)];
                Dsp.AddBurst(x, g, 0.02f, 1f, 0.0002f, 0.004f, 9000f, 200f);
                Dsp.Modes(x, rate, 0.02f, 0.6f, new[] { g.Range(120f, 180f), g.Range(300f, 420f), g.Range(700f, 950f) }, new[] { 1f, 0.6f, 0.3f }, new[] { 0.08f, 0.05f, 0.025f });
                Dsp.Grains(x, g, 0.03f, 0.45f, 60, 0.35f, 0.0015f, u => 1f - u);
                float end = 0.6f;
                if (g.Variation % 2 == 1)
                {
                    float fall = g.Range(0.9f, 1.4f), land = 0.3f + fall;
                    float[] sw = Dsp.White(g, g.N(fall));
                    Dsp.SweepBP(sw, rate, 1.2f, t => 600f + 1800f * t / fall);
                    Dsp.Shape(sw, rate, t => Swell(t / fall, 0.7f, 0.1f) * 0.35f);
                    Dsp.Mix(x, sw, g.N(0.3f));
                    Dsp.Thump(x, rate, land, 0.9f, 90f, 45f, 0.05f, 0.003f, 0.2f);
                    Dsp.Grains(x, g, land, land + 0.6f, 120, 0.4f, 0.002f, u => 1f - u);
                    end = land + 0.7f;
                }
                Dsp.LP(x, 5000f, rate);
                Dsp.Reverb(x, rate, 0.95f, 0.45f, 0.5f);
                return AmbienceKit.EndFade(Trim(x, g, end + 1.4f), rate, 0.6f);
            }
            #endregion

            #region Night callers and far animals
            // American crow: two to five harsh, nasal caws, each a rough voice bending down at its end.
            internal static float[] Crow(SynthGen g)
            {
                int rate = g.Rate;
                int caws = g.Int(2, 6);
                var x = new float[g.N(caws * 0.7f + 0.8f)];
                float f0 = g.Range(480f, 600f), t = 0.03f;
                var throat = new Vowel(1050f, 1600f, 2600f, 160f, 220f, 300f);
                for (int c = 0; c < caws; c++)
                {
                    float dur = g.Range(0.26f, 0.4f), f = f0 * g.Range(0.95f, 1.05f);
                    float[] v = Dsp.Voice(g, dur, new VoiceParams
                    {
                        F0 = u => f * (1f + 0.06f * MathF.Sin(Dsp.Pi * u) - 0.1f * u),
                        Formants = u => throat,
                        Amp = u => Swell(u, 0.06f, 0.3f),
                        Breath = 0.3f, Rough = 0.8f, Jitter = 0.03f, Tilt = 3500f, G1 = 1f, G2 = 0.9f, G3 = 0.5f, Direct = 0.12f,
                    });
                    Dsp.Mix(x, v, (int)(t * rate), 1f - 0.1f * c);
                    t += dur + g.Range(0.14f, 0.3f);
                }
                return Air(Trim(x, g, t + 0.6f), rate, 0.2f);
            }

            // Barred owl: "who cooks for you, who cooks for you-all" — rounded, slightly buzzy hoots round 400 Hz, the
            // last drawn out and falling away with a quaver (variations 0 and 1). Variation 2: a great horned owl's deep,
            // soft "hoo, h'hoo — hoo — hoo".
            internal static float[] Owl(SynthGen g)
            {
                int rate = g.Rate;
                var x = new float[g.N(4.6f)];
                float t = 0.04f;
                if (g.Variation % 3 == 2)
                {
                    float f = g.Range(260f, 320f);
                    Hoot(x, g, t, 0.38f, f, 0.03f, 0.05f, 0.9f, 0f);
                    t += 0.62f;
                    Hoot(x, g, t, 0.12f, f * 1.04f, 0.02f, 0.04f, 0.6f, 0f);
                    t += 0.16f;
                    Hoot(x, g, t, 0.36f, f * 1.06f, 0.03f, 0.05f, 1f, 0f);
                    t += 0.68f;
                    Hoot(x, g, t, 0.42f, f * 1.02f, 0.02f, 0.06f, 0.9f, 0f);
                    t += 0.62f;
                    Hoot(x, g, t, 0.45f, f * 0.98f, 0.02f, 0.08f, 0.8f, 0f);
                    t += 0.45f;
                }
                else
                {
                    float f = g.Range(370f, 430f);
                    bool full = g.Variation % 3 == 0 || g.Chance(0.5f);
                    t = Cooks(x, g, t, f, !full);
                    if (full) t = Cooks(x, g, t + g.Range(0.45f, 0.65f), f * g.Range(0.98f, 1.02f), true);
                }
                return Air(Trim(x, g, t + 0.7f), rate, 0.22f);
            }

            // "Who cooks for you" (and, with `all`, the drawn-out falling "-all" after it). Returns when it ends.
            private static float Cooks(float[] x, SynthGen g, float t, float f, bool all)
            {
                Hoot(x, g, t, 0.22f, f, 0.04f, 0.06f, 0.8f, 0f);
                t += 0.29f;
                Hoot(x, g, t, 0.17f, f * 1.08f, 0.03f, 0.05f, 0.75f, 0f);
                t += 0.21f;
                Hoot(x, g, t, 0.15f, f * 1.03f, 0.03f, 0.05f, 0.7f, 0f);
                t += 0.19f;
                if (!all)
                {
                    Hoot(x, g, t, 0.42f, f * 1.12f, 0.06f, 0.12f, 1f, 0f);
                    return t + 0.42f;
                }
                Hoot(x, g, t, 0.28f, f * 1.14f, 0.05f, 0.04f, 1f, 0f);
                t += 0.3f;
                Hoot(x, g, t, 0.85f, f * 1.18f, 0.03f, 0.32f, 0.95f, 0.02f);
                return t + 0.85f;
            }

            // One hoot: a rounded tone (strong low harmonics) whose pitch rises a touch and falls away, a breathy edge.
            private static void Hoot(float[] x, SynthGen g, float at, float dur, float f0, float rise, float fall, float gain, float quaver)
            {
                int rate = g.Rate;
                Whistle(x, rate, at, dur,
                    u => f0 * (1f + rise * MathF.Sin(Dsp.Pi * Math.Min(1f, u * 1.6f))) * (1f - fall * Dsp.SmoothStep((u - 0.55f) / 0.45f)) * (1f + quaver * MathF.Sin(TwoPi * 6f * u * dur)),
                    u => Swell(u, 0.18f, 0.35f), gain, 0.3f, 0.12f);
                float[] b = Dsp.White(g, Math.Max(8, (int)(dur * rate)));
                Dsp.BP(b, f0 * 2.2f, 2f, rate);
                Dsp.Shape(b, rate, t => Swell(t / dur, 0.1f, 0.5f));
                Dsp.Mix(x, b, (int)(at * rate), gain * 0.08f);
            }

            // Eastern whip-poor-will: "WHIP-poor-WILL" three times over — a quick rising note, a short dip, then a long sweep
            // up to ~3 kHz that drops sharply at its end. The director chains these for a bird's long run of calls.
            internal static float[] Whippoorwill(SynthGen g)
            {
                int rate = g.Rate;
                float period = g.Range(1.08f, 1.18f), k = g.Range(0.94f, 1.06f);
                var x = new float[g.N(3f * period + 0.6f)];
                for (int p = 0; p < 3; p++)
                {
                    float t = 0.05f + p * period, a = p == 0 ? 0.9f : 1f;
                    Whistle(x, rate, t, 0.075f, u => k * (1350f + 650f * u), u => Swell(u, 0.2f, 0.35f), 0.8f * a, 0.1f, 0.03f);
                    Whistle(x, rate, t + 0.12f, 0.07f, u => k * (1650f - 400f * u), u => Swell(u, 0.2f, 0.4f), 0.55f * a, 0.1f, 0.03f);
                    Whistle(x, rate, t + 0.21f, 0.32f, u => k * (u < 0.82f ? 1250f + 1750f * MathF.Pow(u / 0.82f, 1.4f) : 3000f - 900f * ((u - 0.82f) / 0.18f)),
                        u => Swell(u, 0.1f, 0.12f), a, 0.05f, 0.015f);
                }
                return Air(x, rate, 0.2f);
            }

            // Coyotes across open ground: two to four voices yipping in runs and breaking into wavering howls, far off —
            // dark and echoing.
            internal static float[] Coyotes(SynthGen g)
            {
                int rate = g.Rate;
                const float Len = 4f;
                var x = new float[g.N(Len + 0.9f)];
                int voices = g.Int(2, 5);
                for (int v = 0; v < voices; v++)
                {
                    float t = v == 0 ? 0.02f : g.Range(0f, 0.8f), pitch = g.Range(0.85f, 1.2f);
                    while (t < Len - 0.7f)
                    {
                        if (!(v == 0 && t < 0.05f) && g.Chance(0.3f))
                        {
                            float dur = g.Range(0.6f, 1.3f);
                            float[] h = Dsp.Voice(g, dur, new VoiceParams
                            {
                                F0 = u => pitch * (620f + 520f * MathF.Sin(Dsp.Pi * Math.Min(1f, u * 1.3f))),
                                Formants = u => Vowel.Lerp(Vowel.U, Vowel.A, u).Scale(1.6f),
                                Amp = u => Swell(u, 0.15f, 0.3f),
                                VibratoHz = 7f, VibratoDepth = 0.025f, Breath = 0.2f, Jitter = 0.02f, Tilt = 3000f,
                            });
                            Dsp.Mix(x, h, (int)(t * rate), 0.8f);
                            t += dur + g.Range(0.1f, 0.3f);
                        }
                        else
                        {
                            int yips = g.Int(2, 6);
                            for (int y = 0; y < yips; y++)
                            {
                                float dur = g.Range(0.06f, 0.12f), f = pitch * g.Range(850f, 1250f);
                                float[] yp = Dsp.Voice(g, dur, new VoiceParams
                                {
                                    F0 = u => f * (1f + 0.35f * MathF.Sin(Dsp.Pi * u)),
                                    Formants = u => Vowel.I.Scale(1.4f),
                                    Amp = u => Swell(u, 0.15f, 0.4f),
                                    Breath = 0.25f, Rough = 0.2f, Tilt = 3200f,
                                });
                                Dsp.Mix(x, yp, (int)(t * rate), 0.7f);
                                t += dur + g.Range(0.05f, 0.11f);
                            }
                            t += g.Range(0.15f, 0.4f);
                        }
                    }
                }
                Dsp.LP(x, 4500f, rate);
                Dsp.HP(x, 200f, rate);
                Dsp.Reverb(x, rate, 0.9f, 0.5f, 0.45f);
                return AmbienceKit.EndFade(x, rate, 0.5f);
            }

            // Abandoned dogs barking a long way off, between houses: a medium dog (variation 0), a big one (1) or a small
            // yappy one (2) — barks, some doubled, dark with distance, with dispersed slap-backs off the walls.
            internal static float[] Dogs(SynthGen g)
            {
                int rate = g.Rate, type = g.Variation % 3;
                float f0 = type == 1 ? g.Range(170f, 230f) : type == 2 ? g.Range(480f, 620f) : g.Range(280f, 380f);
                float size = type == 1 ? 0.85f : type == 2 ? 1.35f : 1.05f;
                int barks = type == 2 ? g.Int(4, 9) : g.Int(2, 6);
                var x = new float[g.N(3.4f)];
                float t = 0.03f;
                for (int b = 0; b < barks && t < 2.3f; b++)
                {
                    float dur = (type == 2 ? 0.09f : 0.15f) * g.Range(0.8f, 1.25f), f = f0 * g.Range(0.95f, 1.05f);
                    float[] v = Dsp.Voice(g, dur, new VoiceParams
                    {
                        F0 = u => f * (1.1f - 0.25f * u),
                        Formants = u => Vowel.Lerp(Vowel.A, Vowel.Uh, u).Scale(size),
                        Amp = u => u < 0.08f ? u / 0.08f : MathF.Exp(-(u - 0.08f) * 4f),
                        Breath = 0.35f, Rough = 0.7f, Jitter = 0.04f, Tilt = 2400f, Direct = 0.1f,
                    });
                    Dsp.Mix(x, v, (int)(t * rate), g.Range(0.75f, 1f));
                    t += dur + (g.Chance(0.35f) ? g.Range(0.08f, 0.14f) : g.Range(0.35f, 0.8f));
                }
                Dsp.LP(x, 2600f, rate);
                Dsp.HP(x, 150f, rate);
                var echo = (float[])x.Clone();
                Dsp.LP(echo, 1500f, rate);
                Dsp.Mix(x, echo, g.N(g.Range(0.08f, 0.11f)), 0.3f);
                Dsp.Mix(x, echo, g.N(g.Range(0.15f, 0.19f)), 0.22f);
                Dsp.Mix(x, echo, g.N(g.Range(0.25f, 0.3f)), 0.15f);
                Dsp.Reverb(x, rate, 0.8f, 0.55f, 0.35f);
                return AmbienceKit.EndFade(Trim(x, g, t + 0.9f), rate, 0.4f);
            }
            #endregion
        }

        // The hub guitarist's tunes: fingerpicked steel strings (extended Karplus–Strong: a pick-shaped noise burst circulating
        // in a tuned delay line that loses its highs first) through a small guitar body's resonances. Two original tunes,
        // a loop each: Travis-picked in G (alternating palm-muted thumb bass under a pinched melody, lazily swung) and a
        // slow waltz in D.
        private static class AmbienceGuitar
        {
            private struct Note
            {
                public float At, Ring, Bright, Vel;
                public int Midi, String;
            }

            internal static float[] Tune(SynthGen g)
            {
                var notes = new List<Note>();
                float loop = g.Variation % 2 == 0 ? Travis(notes, g) : Waltz(notes, g);
                int rate = g.Rate;
                var x = new float[g.N(loop)];
                notes.Sort((a, b) => a.At.CompareTo(b.At));
                for (int i = 0; i < notes.Count; i++)
                {
                    Note nt = notes[i];
                    // The next note on the same string damps this one (round the loop).
                    float next = loop;
                    for (int j = 1; j < notes.Count; j++)
                    {
                        Note o = notes[(i + j) % notes.Count];
                        if (o.String != nt.String) continue;
                        float gap = o.At - nt.At;
                        if (gap <= 0f) gap += loop;
                        next = gap;
                        break;
                    }
                    float hold = Math.Min(nt.Ring * 1.2f, next + 0.015f);
                    float[] p = Pluck(g, Dsp.Midi(nt.Midi), hold, nt.Ring, nt.Bright, g.Range(0.12f, 0.2f));
                    Dsp.MixWrap(x, p, (int)MathF.Round((nt.At + 0.06f) * rate), nt.Vel);
                }
                Body(x, rate);
                return x;
            }

            // One plucked string: a period of noise shaped by the pick (soft thumb .. bright nail) and by where along the
            // string it was plucked, circulating in a delay line tuned by a fractional all-pass, its loop losing a little
            // each pass (highs fastest) so it rings `t60` seconds to silence; cut with a short fade after `seconds`.
            private static float[] Pluck(SynthGen g, float hz, float seconds, float t60, float bright, float pos)
            {
                int rate = g.Rate;
                int n = Math.Max(32, (int)(seconds * rate));
                var y = new float[n];
                float period = rate / hz;
                int delay = Math.Max(2, (int)(period - 1f));
                float frac = period - 0.5f - delay;
                float c = (1f - frac) / (1f + frac);
                float rho = MathF.Pow(10f, -3f / (Math.Max(0.05f, t60) * hz));
                var ex = new float[delay + 1];
                OnePole lp = OnePole.At(Dsp.Lerp(900f, 7000f, bright), rate);
                for (int i = 0; i < ex.Length; i++) ex[i] = lp.Lp(g.Bipolar());
                int d = Math.Max(1, (int)(pos * delay));
                for (int i = ex.Length - 1; i >= d; i--) ex[i] -= ex[i - d];
                float apIn = 0f, apOut = 0f;
                for (int i = 0; i < n; i++)
                {
                    float a = i - delay >= 0 ? y[i - delay] : 0f;
                    float b = i - delay - 1 >= 0 ? y[i - delay - 1] : 0f;
                    float v = rho * 0.5f * (a + b);
                    float ap = c * v + apIn - c * apOut;
                    apIn = v;
                    apOut = ap;
                    y[i] = (i < ex.Length ? ex[i] : 0f) + ap;
                }
                Dsp.RemoveMean(y);
                Dsp.Norm(y);
                Dsp.FadeIn(y, 4);
                Dsp.FadeOut(y, Math.Min(n / 4, (int)(0.012f * rate)));
                return y;
            }

            // The body: the air in the box (~100 Hz) and the top's first modes (~200, ~410 Hz) added to the strings, a little
            // bite in the upper mids, nothing under 60 Hz. Loop filters, so the tune wraps.
            private static void Body(float[] x, int rate)
            {
                float[] air = (float[])x.Clone(), top = (float[])x.Clone(), top2 = (float[])x.Clone();
                Dsp.LoopBP(air, 100f, 5f, rate);
                Dsp.LoopBP(top, 200f, 7f, rate);
                Dsp.LoopBP(top2, 410f, 4f, rate);
                for (int i = 0; i < x.Length; i++) x[i] = 0.8f * x[i] + 1.1f * air[i] + 0.9f * top[i] + 0.5f * top2[i];
                Dsp.LoopPeakEq(x, 2800f, 1.2f, 2.5f, rate);
                Dsp.LoopHP(x, 60f, rate);
            }

            // Travis picking in G: the thumb alternates bass strings (palm-muted), the fingers pinch the melody on the beat
            // and fill between; eight bars of 4/4 (G, G7, C, C, G, D, G, D and round again), lazily swung.
            private static float Travis(List<Note> notes, SynthGen g)
            {
                float beat = 60f / g.Range(100f, 108f), bar = beat * 4f, swing = g.Range(0.55f, 0.6f);
                // per bar: the root bass, the other bass, a string-3 and a string-2 chord tone
                int[,] chord = { { 43, 50, 55, 59 }, { 43, 50, 55, 59 }, { 48, 52, 55, 60 }, { 48, 52, 55, 60 }, { 43, 50, 55, 59 }, { 50, 45, 57, 62 }, { 43, 50, 55, 59 }, { 50, 45, 57, 62 } };
                // the melody, four notes a bar (on the one, the "and" of two, the three, the "and" of four)
                int[,] tune = { { 67, 62, 64, 62 }, { 65, 62, 59, 62 }, { 64, 60, 64, 67 }, { 67, 64, 60, 62 }, { 59, 62, 67, 66 }, { 66, 62, 64, 66 }, { 67, 62, 59, 55 }, { 62, 66, 64, 66 } };
                for (int b = 0; b < 8; b++)
                {
                    for (int e = 0; e < 8; e++)
                    {
                        float at = b * bar + (e / 2) * beat + (e % 2 == 1 ? swing * beat : 0f) + g.Range(-0.008f, 0.008f);
                        if (e % 2 == 0) Add(notes, at, e % 4 == 0 ? chord[b, 0] : chord[b, 1], e % 4 == 0 ? 6 : 5, g.Range(0.55f, 0.65f), 0.45f, 0.15f);
                        int m = e == 0 ? 0 : e == 3 ? 1 : e == 4 ? 2 : e == 7 ? 3 : -1;
                        if (m >= 0)
                        {
                            int note = tune[b, m];
                            Add(notes, at + g.Range(0f, 0.006f), note, note >= 64 ? 1 : note >= 59 ? 2 : 3, (m == 0 ? 0.95f : 0.82f) * g.Range(0.9f, 1.05f), 2.2f, 0.65f);
                        }
                        else if (e == 1 || e == 5) Add(notes, at, chord[b, e == 1 ? 2 : 3], e == 1 ? 3 : 2, g.Range(0.4f, 0.5f), 1.5f, 0.45f);
                    }
                }
                return bar * 8f;
            }

            // A slow waltz in D: the bass on the one, the chord pinched on two and three, a singing melody on top
            // (D, G, D, A, D, G, A7, D).
            private static float Waltz(List<Note> notes, SynthGen g)
            {
                float beat = 60f / g.Range(84f, 90f), bar = beat * 3f;
                int[,] chord = { { 50, 57, 62 }, { 43, 55, 59 }, { 50, 57, 62 }, { 45, 57, 61 }, { 50, 57, 62 }, { 43, 55, 59 }, { 45, 55, 61 }, { 50, 57, 62 } };
                int[,] tune = { { 66, 69 }, { 71, 67 }, { 66, 62 }, { 64, 61 }, { 62, 66 }, { 67, 71 }, { 69, 67 }, { 66, 62 } };
                for (int b = 0; b < 8; b++)
                {
                    float t0 = b * bar;
                    Add(notes, t0 + g.Range(-0.006f, 0.006f), chord[b, 0], 6, g.Range(0.62f, 0.72f), 1.6f, 0.3f);
                    Add(notes, t0 + g.Range(0f, 0.008f), tune[b, 0], tune[b, 0] >= 64 ? 1 : 2, g.Range(0.9f, 1f), 2.6f, 0.6f);
                    for (int k = 1; k <= 2; k++)
                    {
                        float at = t0 + k * beat + g.Range(-0.008f, 0.008f);
                        Add(notes, at, chord[b, 1], 3, g.Range(0.38f, 0.46f), 1.1f, 0.4f);
                        Add(notes, at + g.Range(0.004f, 0.012f), chord[b, 2], 2, g.Range(0.36f, 0.44f), 1.1f, 0.45f);
                    }
                    if (tune[b, 1] != tune[b, 0])
                        Add(notes, t0 + 1.5f * beat + g.Range(-0.008f, 0.008f), tune[b, 1], tune[b, 1] >= 64 ? 1 : 2, g.Range(0.75f, 0.85f), 1.8f, 0.6f);
                }
                return bar * 8f;
            }

            private static void Add(List<Note> notes, float at, int midi, int str, float vel, float ring, float bright)
                => notes.Add(new Note { At = at, Midi = midi, String = str, Vel = vel, Ring = ring, Bright = bright });
        }
    }

    public static partial class SfxIds
    {
        // Ambience beds (flat stereo loops, Ambience bus).
        public const string WindHighAmbience = "amb_wind_high", RainAmbience = "amb_rain", RainHeavyAmbience = "amb_rain_heavy",
            RainRoofAmbience = "amb_rain_roof", NightAmbience = "amb_night", CicadaAmbience = "amb_cicadas", FrogAmbience = "amb_frogs",
            WinterAmbience = "amb_winter";
        // Weather one-shots: thunder (positional, placed toward the strike), a gust passing (flat).
        public const string ThunderNear = "thunder_near", ThunderFar = "thunder_far", WindGust = "wind_gust";
        // Wildlife and the trees (mono, placed round the listener by AmbienceDirector).
        public const string BirdCardinal = "bird_cardinal", BirdWren = "bird_wren", BirdChickadee = "bird_chickadee",
            BirdTitmouse = "bird_titmouse", BirdJay = "bird_jay", WoodpeckerDrum = "woodpecker", CrowCall = "crow", OwlCall = "owl",
            WhippoorwillCall = "whippoorwill", CoyoteYip = "coyote_yip", DogBarkFar = "dog_bark_far", BranchCreak = "branch_creak",
            TreeCrack = "tree_crack";
        // Positional loops: a wood fire, a kerosene heater.
        public const string FireCrackle = "fire_crackle", HeaterHiss = "heater_hiss";
    }
}
