using System;

namespace CloneZ
{
    // Doors, gear, the body, the interface and small actions: tactile foley built from physical layers — contact
    // transients, modal rings (steel, wood, plastic, glass, bells), stick-slip friction, cloth and paper noise, formant
    // breath — at 44.1 kHz, the breathing loops at 32 kHz. The interface is analog and quiet: switch clicks, a ledger's
    // paper, a rubber stamp, a desk bell.
    public sealed partial class SfxSynth
    {
        partial void RegisterFoley()
        {
            Recipe Foley(string id, Func<SynthGen, float[]> make, int variations, int rate = HiRate) => Add(id, make, variations, rate);

            // ── Doors and gear ──────────────────────────────────────────────────────────────────────
            Foley(SfxIds.DoorOpen, FoleyKit.DoorOpen, 3).Peak(0.7f).Priority(100);
            Foley(SfxIds.DoorClose, FoleyKit.DoorClose, 3).Peak(0.85f).Priority(100).Reverb(0.7f);
            Foley(SfxIds.DoorBash, FoleyKit.DoorBash, 4).Peak(0.9f).Priority(72).Reverb(0.7f);
            Foley(SfxIds.DoorKnock, FoleyKit.DoorKnock, 2).Priority(100).Reverb(0.8f);
            Foley(SfxIds.DoorLocked, FoleyKit.DoorLocked, 2).Peak(0.75f).Priority(110);
            Foley(SfxIds.DoorBolt, FoleyKit.DoorBolt, 2).Peak(0.75f).Priority(110);
            Foley(SfxIds.SwitchHeavy, FoleyKit.SwitchHeavy, 2).Peak(0.85f).Priority(90);
            Foley(SfxIds.GearRustle, FoleyKit.GearRustle, 4).Peak(0.6f);

            // ── The body ────────────────────────────────────────────────────────────────────────────
            Foley(SfxIds.BreathMask, g => FoleyKit.MaskBreathing(g, false), 1, BedRate).Loop().Peak(0.55f).Inside().Priority(24);
            Foley(SfxIds.BreathMaskHard, g => FoleyKit.MaskBreathing(g, true), 1, BedRate).Loop().Peak(0.7f).Inside().Priority(24);
            Foley(SfxIds.BreathHeavy, FoleyKit.HeavyBreathing, 1, BedRate).Loop().Peak(0.55f).Inside().Priority(24);
            Foley(SfxIds.MaskCrack, FoleyKit.MaskCrack, 2).Peak(0.85f).Inside();
            Foley(SfxIds.VisorWipe, FoleyKit.VisorWipe, 2).Peak(0.7f).Inside();
            Foley(SfxIds.Heartbeat, FoleyKit.Heartbeat, 2).Peak(0.85f).Inside().Priority(40);
            Foley(SfxIds.Sneeze, FoleyKit.Sneeze, 2).Peak(0.85f);
            Foley(SfxIds.Cough, FoleyKit.Cough, 4);

            // ── Interface ───────────────────────────────────────────────────────────────────────────
            Foley(SfxIds.UiClick, FoleyKit.UiClick, 3).Peak(0.5f).Warm(10);
            Foley(SfxIds.UiHover, FoleyKit.UiHover, 3).Peak(0.25f).Warm(10);
            Foley(SfxIds.UiOpen, FoleyKit.UiOpen, 2).Peak(0.4f).Warm(10);
            Foley(SfxIds.UiClose, FoleyKit.UiClose, 2).Peak(0.4f).Warm(10);
            Foley(SfxIds.UiBuy, FoleyKit.UiBuy, 2).Peak(0.5f).Warm(10);
            Foley(SfxIds.UiError, FoleyKit.UiError, 1).Peak(0.55f).Warm(10);
            Foley(SfxIds.Cash, FoleyKit.Cash, 2).Peak(0.42f).Bus(SfxBus.Interface).Inside().Dry().Priority(16);

            // ── Small actions ───────────────────────────────────────────────────────────────────────
            Foley(SfxIds.Pickup, FoleyKit.Pickup, 5).Peak(0.45f);
            Foley(SfxIds.Eat, FoleyKit.Eat, 3).Peak(0.5f);
            Foley(SfxIds.Drink, FoleyKit.Drink, 3).Peak(0.5f);
            Foley(SfxIds.Bandage, FoleyKit.Bandage, 2).Peak(0.5f);
            Foley(SfxIds.Injector, FoleyKit.Injector, 2).Peak(0.55f);
            Foley(SfxIds.PageTurn, FoleyKit.PageTurn, 3).Peak(0.45f);
            Foley(SfxIds.Splash, FoleyKit.Splash, 3);
            Foley(SfxIds.Crank, FoleyKit.Crank, 2).Peak(0.7f);
            Foley(SfxIds.Lighter, FoleyKit.Lighter, 2).Peak(0.5f);
            Foley(SfxIds.Handcuffs, FoleyKit.Handcuffs, 2).Peak(0.75f);
        }

        /// <summary>The foley family's building blocks and recipes (pure DSP on the generator; thread-safe).</summary>
        private static class FoleyKit
        {
            // Mode ratios and relative levels of the small things foley is made of.
            private static readonly float[] SteelR = { 1f, 2.76f, 5.40f, 8.93f }, SteelA = { 1f, 0.55f, 0.32f, 0.18f };
            private static readonly float[] PanelR = { 1f, 1.58f, 2.23f, 3.11f, 4.27f, 5.83f }, PanelA = { 1f, 0.75f, 0.55f, 0.4f, 0.28f, 0.18f };
            private static readonly float[] ShellR = { 1f, 1.47f, 2.09f, 2.56f, 3.3f }, ShellA = { 1f, 0.6f, 0.4f, 0.25f, 0.15f };
            private static readonly float[] GlassR = { 1f, 2.14f, 3.47f, 5.08f, 6.9f }, GlassA = { 1f, 0.7f, 0.5f, 0.35f, 0.2f };
            private static readonly float[] BellR = { 1f, 2.32f, 4.25f, 6.63f, 9.38f }, BellA = { 1f, 0.45f, 0.25f, 0.12f, 0.06f };
            private static readonly float[] CoinR = { 1f, 1.73f, 2.33f, 3.6f }, CoinA = { 1f, 0.6f, 0.45f, 0.25f };
            private static readonly float[] CanR = { 1f, 1.38f, 1.81f, 2.46f, 3.12f }, CanA = { 1f, 0.7f, 0.5f, 0.35f, 0.2f };
            private static readonly float[] ZippoR = { 1f, 1.48f, 2.17f, 2.93f, 4.1f }, ZippoA = { 1f, 0.6f, 0.5f, 0.3f, 0.2f };

            #region Building blocks
            private static int At(SynthGen g, float seconds) => (int)(seconds * g.Rate);

            /// <summary>A contact transient: a band-limited noise burst at <paramref name="at"/> seconds, peaking at <paramref name="gain"/>.</summary>
            private static void Tick(float[] x, SynthGen g, float at, float gain, float tau, float hp, float lp)
            {
                float[] b = Dsp.Burst(g, 0.00012f, tau, lp, hp);
                Dsp.Norm(b);
                Dsp.Mix(x, b, At(g, at), gain);
            }

            /// <summary>Decaying modes at hz × ratios, each nudged by ±spread and given a random level; the k-th lasts tau × fall^k.</summary>
            private static void Ring(float[] x, SynthGen g, float at, float gain, float hz, float[] ratios, float[] amps, float tau, float fall, float spread)
            {
                int n = ratios.Length;
                var f = new float[n];
                var a = new float[n];
                var t = new float[n];
                float tt = tau;
                for (int i = 0; i < n; i++)
                {
                    f[i] = hz * ratios[i] * (1f + g.Bipolar() * spread);
                    a[i] = amps[Math.Min(i, amps.Length - 1)] * (0.7f + 0.6f * g.Next());
                    t[i] = tt;
                    tt *= fall;
                }
                Dsp.Modes(x, g.Rate, at, gain, f, a, t);
            }

            /// <summary>Small steel parts meeting (a latch, a pawl, a lid): the click of contact and a short inharmonic ring.</summary>
            private static void Metal(float[] x, SynthGen g, float at, float gain, float hz, float tau, float click = 0.6f)
            {
                Ring(x, g, at, gain * 0.5f, hz, SteelR, SteelA, tau, 0.55f, 0.04f);
                Tick(x, g, at, gain * click, 0.0005f, Math.Min(0.7f * hz, 5000f), 14000f);
            }

            /// <summary>A blow on a wooden panel: the contact (soft to hard), the panel's modes and the slab's low thump.</summary>
            private static void Wood(float[] x, SynthGen g, float at, float gain, float hz, float tau, float hard)
            {
                Ring(x, g, at, gain * 0.55f, hz, PanelR, PanelA, tau, 0.72f, 0.07f);
                Tick(x, g, at, gain * (0.2f + 0.5f * hard), 0.0006f + 0.0025f * (1f - hard), 120f, 1200f + 5000f * hard);
                Dsp.Thump(x, g.Rate, at, gain * 0.5f, hz * 0.7f, hz * 0.5f, 0.015f, 0.001f, tau * 0.7f);
            }

            /// <summary>A plastic part snapping or knocking: hollow, short, few modes.</summary>
            private static void Plastic(float[] x, SynthGen g, float at, float gain, float hz, float tau = 0.006f)
            {
                Ring(x, g, at, gain * 0.5f, hz, ShellR, ShellA, tau, 0.7f, 0.05f);
                Tick(x, g, at, gain * 0.6f, 0.0004f, 1500f, 11000f);
            }

            /// <summary>A coin (or a washer) knocking: a thin plate's high, short, inharmonic ring.</summary>
            private static void AddCoin(float[] x, SynthGen g, float at, float gain)
            {
                Ring(x, g, at, gain * 0.6f, g.Range(3600f, 6000f), CoinR, CoinA, g.Range(0.05f, 0.12f), 0.7f, 0.03f);
                Tick(x, g, at, gain * 0.4f, 0.0003f, 3500f, 15000f);
            }

            /// <summary>
            /// Stick-slip friction (a hinge, a strap through a buckle, a glove on a visor): slips whose rate follows the
            /// sliding speed — separate ticks when slow, a groan or a squeal when fast — with jittered, alternating
            /// (period-doubled) slips and a chattering grip, ringing narrow resonances. speed(u) is 0..1 over the sound.
            /// Returned at unit peak.
            /// </summary>
            private static float[] Creak(SynthGen g, float seconds, Func<float, float> speed, float slowHz, float fastHz,
                float[] resHz, float[] resQ, float[] resGain, float jitter, float rough, float chatter)
            {
                int rate = g.Rate, n = g.N(seconds);
                var exc = new float[n];
                OnePole wob = OnePole.At(5f, rate), grip = OnePole.At(16f, rate), soft = OnePole.At(6500f, rate);
                float wobK = wob.NoiseNormaliser, gripK = grip.NoiseNormaliser;
                float ph = g.Next();
                bool alt = false;
                for (int i = 0; i < n; i++)
                {
                    float v = Dsp.Clamp01(speed(i / (float)n));
                    float hz = (slowHz + (fastHz - slowHz) * v * v) * (1f + 0.05f * wobK * wob.Lp(g.Bipolar()));
                    ph += Math.Max(0f, hz) / rate;
                    float e = 0f;
                    if (ph >= 1f)
                    {
                        // Sliding fast the slips lock into a steady squeal; slow, they come ragged.
                        ph -= 1f + jitter * (1.5f - v) * g.Bipolar();
                        alt = !alt;
                        e = (0.55f + 0.45f * g.Next()) * (alt ? 1f : 1f - rough);
                    }
                    float hold = Math.Max(0f, 1f + chatter * gripK * grip.Lp(g.Bipolar()));
                    exc[i] = soft.Lp(e * (0.2f + 0.8f * v) * hold);
                }
                var y = new float[n];
                for (int r = 0; r < resHz.Length; r++)
                {
                    Biquad bq = Biquad.BP(resHz[r], resQ[Math.Min(r, resQ.Length - 1)], rate);
                    float w = resGain[Math.Min(r, resGain.Length - 1)];
                    for (int i = 0; i < n; i++) y[i] += bq.Run(exc[i]) * w;
                }
                Dsp.Norm(y);
                return y;
            }

            /// <summary>The level of cloth or paper on the move, 0..1: a dense run of tiny decaying bursts (folds and fibres
            /// catching and letting go), thinned where env(t seconds) is low. grain 0 = soft and smooth, 1 = crisp nylon or paper.</summary>
            private static float[] Crinkle(SynthGen g, int n, float grain, Func<float, float> env)
            {
                int rate = g.Rate;
                var e = new float[n];
                float perSecond = 90f + 330f * grain, tauLo = (0.006f - 0.0045f * grain) * rate, tauHi = (0.016f - 0.01f * grain) * rate;
                int count = Math.Max(1, (int)(n / (float)rate * perSecond));
                for (int k = 0; k < count; k++)
                {
                    int at = g.Int(0, n);
                    float a = 0.3f + 0.7f * g.Next();
                    float tau = tauLo + (tauHi - tauLo) * g.Next();
                    if (g.Next() > Dsp.Clamp01(env(at / (float)rate)) * 1.3f) continue;
                    int len = Math.Min(n - at, (int)(tau * 5f));
                    for (int i = 0; i < len; i++) e[at + i] += a * MathF.Exp(-i / tau);
                }
                Dsp.Norm(e);
                return e;
            }

            /// <summary>Cloth, nylon or paper moving: noise through a band wandering between lo and hi Hz, crinkling (grain 0..1)
            /// and following env(t seconds), with sparse sharp crackles of creases. Added at <paramref name="gain"/> peak.</summary>
            private static void Rustle(float[] x, SynthGen g, float at, float len, float gain, float lo, float hi, float q, float crackle, Func<float, float> env, float grain = 0.6f)
            {
                int rate = g.Rate, n = g.N(len);
                float[] w = Dsp.White(g, n);
                float[] band = Dsp.Wander(g, n, rate, 7f);
                float[] fold = Crinkle(g, n, grain, env);
                float depth = 0.45f + 0.5f * grain;
                var f = new Biquad();
                float lr = MathF.Log(lo), hr = MathF.Log(hi);
                for (int i = 0; i < n; i++)
                {
                    if ((i & 15) == 0) f.SetBP(MathF.Exp(lr + (hr - lr) * band[i]), q, rate);
                    w[i] = f.Run(w[i]) * env(i / (float)rate) * (1f - depth + depth * fold[i]);
                }
                Dsp.Norm(w);
                if (crackle > 0f)
                {
                    var c = new float[n];
                    Dsp.Grains(c, g, 0f, len, Math.Max(1, (int)(len * 160f)), 1f, 0.0004f, u => env(u * len));
                    Dsp.HP(c, Math.Max(lo, 1500f), rate);
                    Dsp.Norm(c);
                    for (int i = 0; i < n; i++) w[i] += c[i] * crackle;
                    Dsp.Norm(w);
                }
                Dsp.Mix(x, w, At(g, at), gain);
            }

            /// <summary>Sharp little crackles between t0 and t1 seconds — paper creases, splinters, fibres tearing, a crunch —
            /// band-limited to hp..lp Hz, their density following density(u) (null: even). Added at <paramref name="gain"/> peak.</summary>
            private static void Crackle(float[] x, SynthGen g, float t0, float t1, int count, float gain, float tau, float hp, float lp, Func<float, float> density)
            {
                int rate = g.Rate;
                var c = new float[g.N(Math.Max(0.001f, t1 - t0) + tau * 8f) + 8];
                Dsp.Grains(c, g, 0f, Math.Max(0.001f, t1 - t0), Math.Max(1, count), 1f, tau, density);
                if (hp > 0f) Dsp.HP(c, hp, rate);
                if (lp > 0f) Dsp.LP(c, lp, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, At(g, t0), gain);
            }

            /// <summary>Band-passed noise with an envelope env(t seconds), at unit peak: air, a hiss, a swish.</summary>
            private static float[] Air(SynthGen g, float len, float hp, float lp, Func<float, float> env)
            {
                float[] w = Dsp.White(g, g.N(len));
                if (hp > 0f) Dsp.HP(w, hp, g.Rate);
                if (lp > 0f) Dsp.LP(w, lp, g.Rate);
                Dsp.Shape(w, g.Rate, env);
                Dsp.Norm(w);
                return w;
            }

            /// <summary>A room's answer: a few dispersed, darkened copies over 4–45 ms (never one flutter echo), then
            /// optionally a faint short diffuse tail.</summary>
            private static void Room(float[] x, SynthGen g, float size, float early, float tail)
            {
                int rate = g.Rate, n = x.Length;
                var dark = (float[])x.Clone();
                Dsp.LP(dark, 2800f, rate);
                Dsp.HP(dark, 100f, rate);
                var y = new float[n];
                const int taps = 7;
                for (int k = 0; k < taps; k++)
                {
                    float d = 0.004f + size * 0.042f * (k + 0.3f + 0.7f * g.Next()) / taps;
                    int di = Math.Max(1, (int)(d * rate));
                    float a = early * MathF.Pow(0.78f, k) * (g.Chance(0.5f) ? 1f : -1f);
                    for (int i = di; i < n; i++) y[i] += dark[i - di] * a;
                }
                for (int i = 0; i < n; i++) x[i] += y[i];
                if (tail > 0f) Dsp.Reverb(x, rate, 0.12f + 0.2f * size, 0.6f, tail);
            }

            /// <summary>Smooth edges (seconds) on a layer about to be mixed in.</summary>
            private static void Fade(float[] x, int rate, float inSec, float outSec)
            {
                Dsp.FadeIn(x, (int)(inSec * rate));
                Dsp.FadeOut(x, (int)(outSec * rate));
            }

            /// <summary>Drops the trailing near-silence (keeping a short tail for the finishing fade), so clips stay small.</summary>
            private static float[] Trim(float[] x, SynthGen g, float floorDb = -60f)
            {
                int n = Dsp.AudibleLength(x, MathF.Pow(10f, floorDb / 20f)) + g.N(0.03f);
                if (n >= x.Length) return x;
                var y = new float[n];
                Array.Copy(x, y, n);
                return y;
            }

            /// <summary>The speed of a swinging leaf over its swing, 0..1: a push, a peak, a slowing, maybe a hitch on the way.</summary>
            private static float Swing(float u, float peakAt, float hitch, float hitchAt)
            {
                float v = MathF.Sqrt(Dsp.Bell(u, 1f, peakAt));
                return v * (1f - hitch * Dsp.Bell(u - hitchAt + 0.1f, 0.2f, 0.5f));
            }
            #endregion

            #region Doors and gear
            /// <summary>An old wooden door opening: the handle's spring, the latch clacking free of the strike plate, the
            /// leaf knocking in its frame, then dry hinges creaking (stick-slip, different on every door).</summary>
            internal static float[] DoorOpen(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.1f);
                float th = g.Range(0.003f, 0.008f);
                Metal(x, g, th, 0.28f, g.Range(2900f, 3600f), 0.004f, 0.5f);
                float tl = th + g.Range(0.03f, 0.06f);
                Metal(x, g, tl, 0.75f, g.Range(1700f, 2300f), 0.01f, 0.7f);
                Wood(x, g, tl + 0.003f, 0.45f, g.Range(170f, 240f), 0.035f, 0.35f);

                // The leaf swings for about 0.45 s (the doors' animation); the hinges creak with it and a little after.
                float t0 = tl + g.Range(0.01f, 0.04f);
                float dur = g.Range(0.45f, 0.62f);
                float peakAt = g.Range(0.3f, 0.5f), hitch = g.Range(0f, 0.6f), hitchAt = g.Range(0.25f, 0.75f);
                float[] res = { g.Range(180f, 280f), g.Range(650f, 1100f), g.Range(1400f, 2300f), g.Range(2600f, 3900f) };
                float[] q = { 2.5f, g.Range(7f, 12f), g.Range(8f, 14f), g.Range(9f, 16f) };
                float[] w = { g.Range(0.4f, 0.9f), 1f, g.Range(0.5f, 0.9f), g.Range(0.25f, 0.6f) };
                float[] c = Creak(g, dur, u => Swing(u, peakAt, hitch, hitchAt), g.Range(18f, 40f), g.Range(150f, 480f), res, q, w,
                    g.Range(0.04f, 0.12f), g.Range(0f, 0.45f), g.Range(0.25f, 0.5f));
                Fade(c, rate, 0.01f, 0.06f);
                Dsp.Mix(x, c, At(g, t0), g.Range(0.45f, 0.65f));
                Dsp.Mix(x, Air(g, dur, 80f, 500f, t => Dsp.Bell(t, dur, peakAt)), At(g, t0), 0.05f);

                Room(x, g, g.Range(0.4f, 0.8f), 0.18f, 0f);
                Dsp.HP(x, 30f, rate);
                return Trim(x, g);
            }

            /// <summary>An old wooden door shutting: the swing (moving air, sometimes a short creak) and, as the leaf meets
            /// the frame at the end of its swing, the slab's thud, its panel ringing, the latch snapping home and the leaf
            /// shuddering against the stop, with the room's answer.</summary>
            internal static float[] DoorClose(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.15f);
                // The leaf meets the frame at the end of its 0.45 s swing (the doors' animation eases in, so a little early).
                float ti = g.Range(0.36f, 0.41f);
                Dsp.Mix(x, Air(g, ti + 0.04f, 100f, 650f, t => Dsp.Bell(t, ti + 0.04f, 0.8f)), 0, 0.08f);
                float cs = g.Range(0.02f, 0.05f), cd = ti - cs - 0.02f;
                float pk = g.Range(0.3f, 0.7f);
                float[] c = Creak(g, cd, u => MathF.Sqrt(Dsp.Bell(u, 1f, pk)), g.Range(20f, 40f), g.Range(160f, 420f),
                    new[] { g.Range(180f, 260f), g.Range(700f, 1100f), g.Range(1500f, 2400f) }, new[] { 2.5f, 9f, 11f },
                    new[] { 0.7f, 1f, 0.6f }, 0.06f, g.Range(0f, 0.4f), 0.35f);
                Fade(c, rate, 0.01f, 0.03f);
                Dsp.Mix(x, c, At(g, cs), g.Range(0.14f, 0.24f));

                // The slab: its thud and panel, driven a little for weight.
                var body = new float[x.Length];
                Dsp.Thump(body, rate, ti, 1f, g.Range(105f, 135f), g.Range(62f, 75f), 0.02f, 0.0015f, g.Range(0.06f, 0.09f));
                Wood(body, g, ti, 0.9f, g.Range(150f, 210f), g.Range(0.05f, 0.08f), 0.45f);
                Dsp.Norm(body);
                Dsp.Saturate(body, 1.6f);
                Dsp.Norm(body);
                Dsp.Mix(x, body, 0, 1f);
                Tick(x, g, ti, 0.3f, 0.004f, 200f, 3000f);
                float tl = ti + g.Range(0.006f, 0.018f);
                Metal(x, g, tl, 0.4f, g.Range(1800f, 2600f), 0.012f, 0.6f);
                int k = g.Int(1, 4);
                float tr = tl;
                for (int i = 0; i < k; i++)
                {
                    tr += g.Range(0.02f, 0.045f);
                    float a = 0.22f * MathF.Pow(0.55f, i);
                    if (g.Chance(0.5f)) Metal(x, g, tr, a * 0.7f, g.Range(2000f, 3200f), 0.006f, 0.5f);
                    else Wood(x, g, tr, a, g.Range(200f, 300f), 0.02f, 0.5f);
                }

                Room(x, g, g.Range(0.5f, 1f), 0.28f, 0.1f);
                Dsp.HP(x, 30f, rate);
                Dsp.Compress(x, rate, -18f, 3f, 0.3f, 90f);
                return Trim(x, g);
            }

            /// <summary>A violent blow on a wooden door (a shoulder, a boot, the dead): the slab's boom and the panel flexing,
            /// a splintering crack, the latch and hinges rattling in the frame, bits falling. Each variation gives way differently.</summary>
            internal static float[] DoorBash(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.15f);
                float split = Dsp.Clamp01(0.15f + 0.27f * g.Variation + g.Range(-0.08f, 0.08f));
                float t0 = 0.004f;
                var body = new float[x.Length];
                Dsp.Thump(body, rate, t0, 1f, g.Range(85f, 105f), g.Range(45f, 55f), 0.025f, 0.0015f, g.Range(0.1f, 0.14f));
                Dsp.Thump(body, rate, t0 + 0.002f, 0.5f, g.Range(150f, 190f), g.Range(95f, 115f), 0.015f, 0.001f, 0.05f);
                Wood(body, g, t0, 1f, g.Range(120f, 165f), g.Range(0.07f, 0.1f), 0.7f);
                Dsp.Norm(body);
                Dsp.Saturate(body, 2.2f);
                Dsp.Norm(body);
                Dsp.Mix(x, body, 0, 1f);
                Tick(x, g, t0, 0.5f, 0.003f, 100f, 2500f);

                Tick(x, g, t0 + g.Range(0f, 0.004f), 0.2f + 0.5f * split, 0.0012f, 1500f, 12000f);
                Crackle(x, g, t0 + 0.003f, t0 + 0.06f + 0.25f * split, (int)(30 + 150 * split), 0.15f + 0.35f * split, 0.0006f, 1400f, 9000f,
                    u => MathF.Pow(1f - u, 1.6f));

                int k = g.Int(5, 11);
                float tr = t0 + g.Range(0.02f, 0.035f);
                float a = 0.4f;
                for (int i = 0; i < k; i++)
                {
                    if (g.Chance(0.6f)) Metal(x, g, tr, a, g.Range(1600f, 3200f), g.Range(0.004f, 0.009f), 0.5f);
                    else Wood(x, g, tr, a * 0.9f, g.Range(220f, 380f), 0.018f, 0.6f);
                    tr += g.Range(0.012f, 0.035f) * (1f + i * 0.12f);
                    a *= g.Range(0.68f, 0.85f);
                }
                if (split > 0.5f)
                {
                    int bits = g.Int(2, 5);
                    for (int i = 0; i < bits; i++) Wood(x, g, g.Range(0.2f, 0.6f), g.Range(0.05f, 0.12f), g.Range(700f, 1300f), 0.01f, 0.9f);
                }

                Room(x, g, g.Range(0.6f, 1f), 0.3f, 0.12f);
                Dsp.HP(x, 30f, rate);
                Dsp.Norm(x);
                Dsp.Saturate(x, 1.4f);
                Dsp.Compress(x, rate, -20f, 4f, 0.2f, 120f);
                return Trim(x, g);
            }

            /// <summary>Knuckles rapping three times on a wooden door: each rap the knuckle's tick and the panel's hollow knock.</summary>
            internal static float[] DoorKnock(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.05f);
                float hz = g.Range(150f, 200f), tau = g.Range(0.035f, 0.05f);
                float gap = g.Range(0.16f, 0.21f);
                float t = 0.006f;
                for (int i = 0; i < 3; i++)
                {
                    float a = i == 0 ? 1f : g.Range(0.8f, 0.97f);
                    Wood(x, g, t, a, hz * g.Range(0.96f, 1.04f), tau, g.Range(0.55f, 0.75f));
                    Tick(x, g, t, 0.25f * a, 0.0015f, 600f, 3500f);
                    t += gap * g.Range(0.9f, 1.12f);
                }
                Room(x, g, 0.7f, 0.25f, 0.08f);
                Dsp.HP(x, 35f, rate);
                return Trim(x, g);
            }

            /// <summary>Trying the handle of a locked door: the handle goes down on its spring, the leaf is pulled twice against
            /// the bolt (a knock and the latch rattling in its keep), the handle springs back.</summary>
            internal static float[] DoorLocked(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.95f);
                Metal(x, g, 0.005f, 0.3f, g.Range(2600f, 3300f), 0.005f, 0.4f);
                float t = g.Range(0.07f, 0.1f);
                for (int i = 0; i < 2; i++)
                {
                    float a = i == 0 ? 1f : g.Range(0.75f, 0.95f);
                    Wood(x, g, t, 0.75f * a, g.Range(170f, 230f), 0.04f, 0.5f);
                    Metal(x, g, t + g.Range(0.002f, 0.008f), 0.6f * a, g.Range(1500f, 2100f), 0.009f, 0.6f);
                    Metal(x, g, t + g.Range(0.025f, 0.045f), 0.22f * a, g.Range(1900f, 2800f), 0.006f, 0.4f);
                    t += g.Range(0.18f, 0.26f);
                }
                Metal(x, g, t + g.Range(0.02f, 0.06f), 0.35f, g.Range(2400f, 3100f), 0.006f, 0.5f);
                Room(x, g, 0.6f, 0.2f, 0f);
                Dsp.HP(x, 35f, rate);
                return Trim(x, g);
            }

            /// <summary>A bolt shot or drawn: steel sliding on steel (a little grit), then the bolt's solid clack in its keep,
            /// felt in the leaf. Lower pitch for a deadbolt, higher for a slide bolt.</summary>
            internal static float[] DoorBolt(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.5f);
                float slide = g.Range(0.05f, 0.09f);
                float[] s = Dsp.White(g, g.N(slide));
                Dsp.BP(s, g.Range(1800f, 2600f), 1.2f, rate);
                Dsp.Shape(s, rate, t => Dsp.Bell(t, slide, 0.7f));
                Dsp.Norm(s);
                Dsp.Mix(x, s, At(g, 0.004f), 0.18f);
                Crackle(x, g, 0.004f, 0.004f + slide, 10, 0.15f, 0.0003f, 2500f, 9000f, null);

                float tc = 0.004f + slide;
                Metal(x, g, tc, 1f, g.Range(1200f, 1700f), 0.014f, 0.7f);
                Wood(x, g, tc + 0.001f, 0.45f, g.Range(200f, 260f), 0.03f, 0.6f);
                Metal(x, g, tc + g.Range(0.03f, 0.05f), 0.15f, g.Range(2200f, 3000f), 0.005f, 0.4f);
                Dsp.HP(x, 40f, rate);
                return Trim(x, g);
            }

            /// <summary>A heavy knife switch or breaker thrown: the handle taken up, the blade driven home into its jaws with a
            /// steel clunk and the cabinet's boom, a crack of arc, and the contactor behind the panel answering.</summary>
            internal static float[] SwitchHeavy(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.8f);
                Metal(x, g, 0.004f, 0.2f, g.Range(2400f, 3000f), 0.005f, 0.4f);
                float sl = g.Range(0.06f, 0.1f);
                Rustle(x, g, 0.01f, sl, 0.12f, 1500f, 4000f, 1.5f, 0f, tt => Dsp.Bell(tt, sl, 0.7f), 0.3f);
                float tc = 0.01f + sl;
                Ring(x, g, tc, 0.5f, g.Range(320f, 420f), PanelR, PanelA, 0.06f, 0.75f, 0.05f);
                Metal(x, g, tc, 0.8f, g.Range(900f, 1300f), 0.02f, 0.8f);
                Dsp.Thump(x, rate, tc, 0.6f, 160f, 90f, 0.015f, 0.001f, 0.05f);
                Crackle(x, g, tc, tc + g.Range(0.03f, 0.06f), g.Int(15, 30), 0.3f, 0.0003f, 2500f, 12000f, u => 1f - u);
                float tk = tc + g.Range(0.08f, 0.14f);
                Metal(x, g, tk, 0.35f, g.Range(700f, 1000f), 0.012f, 0.5f);
                Dsp.Thump(x, rate, tk, 0.3f, 140f, 100f, 0.01f, 0.001f, 0.03f);
                Room(x, g, 0.6f, 0.2f, 0.06f);
                Dsp.HP(x, 40f, rate);
                Dsp.Compress(x, rate, -16f, 3f, 0.3f, 80f);
                return Trim(x, g);
            }

            /// <summary>Gear shifting: swishes of nylon and canvas, sometimes a webbing strap drawn through its buckle, and a
            /// plastic buckle snapping or a D-ring knocking.</summary>
            internal static float[] GearRustle(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.7f);
                int moves = g.Int(2, 4);
                float t = 0.003f;
                for (int i = 0; i < moves; i++)
                {
                    float len = g.Range(0.1f, 0.22f);
                    bool nylon = g.Chance(0.55f);
                    float pk = g.Range(0.25f, 0.6f);
                    Rustle(x, g, t, len, g.Range(0.6f, 1f) * (i == 0 ? 1f : 0.8f), nylon ? 2000f : 700f, nylon ? 7500f : 3500f,
                        nylon ? 1.6f : 1.1f, nylon ? 0.2f : 0.45f, tt => Dsp.Bell(tt, len, pk));
                    t += len * g.Range(0.45f, 0.8f);
                }
                if (g.Chance(0.6f))
                {
                    float sl = g.Range(0.05f, 0.09f);
                    float[] z = Creak(g, sl, u => Dsp.Bell(u, 1f, 0.4f), 60f, g.Range(180f, 300f),
                        new[] { g.Range(1800f, 2600f), g.Range(3500f, 5000f) }, new[] { 3f, 4f }, new[] { 1f, 0.6f }, 0.25f, 0.4f, 0.3f);
                    Fade(z, rate, 0.005f, 0.01f);
                    Dsp.Mix(x, z, At(g, g.Range(0.08f, 0.3f)), 0.22f);
                }
                float tb = g.Range(0.12f, 0.4f);
                if (g.Chance(0.5f))
                {
                    Plastic(x, g, tb, 0.5f, g.Range(2200f, 3000f));
                    Plastic(x, g, tb + g.Range(0.012f, 0.025f), 0.35f, g.Range(2600f, 3400f));
                }
                else Metal(x, g, tb, 0.3f, g.Range(2600f, 4200f), g.Range(0.02f, 0.05f), 0.4f);
                Dsp.HP(x, 120f, rate);
                return Trim(x, g);
            }
            #endregion

            #region The body
            /// <summary>
            /// Breathing in a respirator, a seamless loop of whole cycles: drawn in through the filter (the inlet valve's
            /// soft tick, a hollow canister hiss swelling with the flow), out through the flutter valve (a rubbery buzz
            /// while the flow holds the flap open, its slap as it shuts), coloured by the facepiece. Hard: fast and forceful.
            /// </summary>
            internal static float[] MaskBreathing(SynthGen g, bool hard)
            {
                int rate = g.Rate;
                int cycles = hard ? 4 : 2;
                var ins = new float[cycles];
                var gap = new float[cycles];
                var outs = new float[cycles];
                var rest = new float[cycles];
                float total = 0f;
                for (int c = 0; c < cycles; c++)
                {
                    ins[c] = hard ? g.Range(0.55f, 0.65f) : g.Range(0.95f, 1.1f);
                    gap[c] = hard ? g.Range(0.04f, 0.08f) : g.Range(0.1f, 0.2f);
                    outs[c] = hard ? g.Range(0.65f, 0.78f) : g.Range(1.1f, 1.3f);
                    rest[c] = hard ? g.Range(0.14f, 0.24f) : g.Range(0.35f, 0.55f);
                    total += ins[c] + gap[c] + outs[c] + rest[c];
                }
                var x = new float[g.N(total)];
                float t = rest[cycles - 1] * 0.5f;   // the loop begins and ends mid-pause
                for (int c = 0; c < cycles; c++)
                {
                    float effort = hard ? g.Range(0.85f, 1f) : g.Range(0.8f, 1f);
                    MaskInhale(x, g, t, ins[c], effort, hard);
                    t += ins[c] + gap[c];
                    MaskExhale(x, g, t, outs[c], effort, hard);
                    t += outs[c] + rest[c];
                }
                Dsp.LoopPeakEq(x, 400f, 1.1f, 3f, rate);
                Dsp.LoopHP(x, 50f, rate);
                return x;
            }

            private static void MaskInhale(float[] loop, SynthGen g, float at, float len, float effort, bool hard)
            {
                int rate = g.Rate, n = g.N(len);
                float peakAt = g.Range(0.3f, 0.45f);
                float[] w = Dsp.White(g, n);
                var hollow = (float[])w.Clone();
                Dsp.BP(hollow, g.Range(900f, 1250f), hard ? 1.3f : 1.7f, rate);
                var hiss = (float[])w.Clone();
                Dsp.HP(hiss, 2800f, rate);
                Dsp.LP(hiss, hard ? 9000f : 7000f, rate);
                Dsp.BP(w, g.Range(320f, 450f), 1.4f, rate);
                Dsp.Norm(hollow);
                Dsp.Norm(hiss);
                Dsp.Norm(w);
                var y = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float f = Dsp.Bell(i / (float)rate, len, peakAt) * effort;
                    y[i] = hollow[i] * f * MathF.Sqrt(f) + hiss[i] * f * f * (hard ? 0.55f : 0.35f) + w[i] * f * 0.45f;
                }
                Dsp.Norm(y);
                Dsp.MixWrap(loop, y, At(g, at), hard ? 1f : 0.8f);

                var v = new float[g.N(0.03f)];
                Ring(v, g, 0.001f, 0.6f, g.Range(550f, 800f), ShellR, ShellA, 0.004f, 0.6f, 0.05f);
                Tick(v, g, 0.001f, 0.3f, 0.0006f, 400f, 3000f);
                Dsp.MixWrap(loop, v, At(g, at + g.Range(0.02f, 0.05f)), hard ? 0.35f : 0.25f);
            }

            private static void MaskExhale(float[] loop, SynthGen g, float at, float len, float effort, bool hard)
            {
                int rate = g.Rate, n = g.N(len);
                float peakAt = g.Range(0.2f, 0.3f);
                float[] breath = Dsp.White(g, n);
                var air = (float[])breath.Clone();
                Dsp.BP(breath, g.Range(480f, 620f), 1.3f, rate);
                Dsp.BP(air, g.Range(1300f, 1800f), 1.6f, rate);
                Dsp.Norm(breath);
                Dsp.Norm(air);
                float flap = hard ? g.Range(38f, 52f) : g.Range(26f, 38f);
                OnePole wob = OnePole.At(4f, rate);
                float wobK = wob.NoiseNormaliser;
                float ph = 0f;
                var buzz = new float[n];
                var y = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float f = Dsp.Bell(i / (float)rate, len, peakAt) * effort;
                    float open = Dsp.SmoothStep((f - 0.3f) / 0.35f);
                    ph += flap * (0.85f + 0.3f * f) * (1f + 0.08f * wobK * wob.Lp(g.Bipolar())) / rate;
                    if (ph >= 1f) ph -= 1f;
                    float s = MathF.Sin(Dsp.TwoPi * ph);
                    float pulse = s > 0f ? s * s * s : 0f;
                    float am = 1f - 0.75f * open + 0.75f * open * pulse * 2.2f;
                    y[i] = (breath[i] + 0.5f * air[i]) * f * am;
                    buzz[i] = pulse * open * f;
                }
                Dsp.BP(buzz, g.Range(180f, 260f), 2.5f, rate);
                Dsp.Norm(buzz);
                Dsp.Norm(y);
                for (int i = 0; i < n; i++) y[i] += buzz[i] * (hard ? 0.45f : 0.35f);
                Dsp.LP(y, hard ? 5000f : 3800f, rate);
                Dsp.Norm(y);
                Dsp.MixWrap(loop, y, At(g, at), hard ? 1f : 0.85f);

                var v = new float[g.N(0.03f)];
                Ring(v, g, 0.001f, 0.6f, g.Range(420f, 650f), ShellR, ShellA, 0.005f, 0.6f, 0.05f);
                Tick(v, g, 0.001f, 0.25f, 0.0008f, 300f, 2200f);
                Dsp.MixWrap(loop, v, At(g, at + len * g.Range(0.82f, 0.92f)), hard ? 0.3f : 0.22f);
            }

            /// <summary>Winded, bare-faced breathing, a seamless loop: quick ragged gasps in through the mouth and harder
            /// breaths out with a ghost of voice, irregular in depth and timing.</summary>
            internal static float[] HeavyBreathing(SynthGen g)
            {
                int rate = g.Rate;
                const int cycles = 5;
                var ins = new float[cycles];
                var gap = new float[cycles];
                var outs = new float[cycles];
                var rest = new float[cycles];
                float total = 0f;
                for (int c = 0; c < cycles; c++)
                {
                    ins[c] = g.Range(0.36f, 0.48f);
                    gap[c] = g.Range(0.02f, 0.06f);
                    outs[c] = g.Range(0.48f, 0.64f);
                    rest[c] = g.Range(0.06f, 0.2f);
                    total += ins[c] + gap[c] + outs[c] + rest[c];
                }
                var x = new float[g.N(total)];
                float t = rest[cycles - 1] * 0.5f;   // the loop begins and ends mid-pause
                float throat = g.Range(0.95f, 1.08f);
                for (int c = 0; c < cycles; c++)
                {
                    float depth = g.Range(0.7f, 1f);
                    Gasp(x, g, t, ins[c], depth, throat);
                    t += ins[c] + gap[c];
                    Pant(x, g, t, outs[c], depth * g.Range(0.85f, 1f), throat);
                    t += outs[c] + rest[c];
                }
                Dsp.LoopLP(x, 5500f, rate);
                Dsp.LoopHP(x, 80f, rate);
                return x;
            }

            private static void Gasp(float[] loop, SynthGen g, float at, float len, float depth, float throat)
            {
                Vowel a = Vowel.Uh.Scale(throat, 1.6f), b = Vowel.O.Scale(throat, 1.6f);
                float pk = g.Range(0.45f, 0.65f);
                float[] v = Dsp.Voice(g, len, new VoiceParams
                {
                    Whisper = true,
                    Breath = 1f,
                    Formants = u => Vowel.Lerp(a, b, u),
                    Amp = u => Dsp.Bell(u, 1f, pk),
                    G1 = 0.6f, G2 = 0.8f, G3 = 0.6f, Direct = 0.2f,
                });
                Dsp.HP(v, 500f, g.Rate);
                Dsp.Norm(v);
                Dsp.MixWrap(loop, v, At(g, at), 0.55f * depth);
            }

            private static void Pant(float[] loop, SynthGen g, float at, float len, float depth, float throat)
            {
                Vowel a = Vowel.A.Scale(throat, 1.4f), b = Vowel.Uh.Scale(throat, 1.4f);
                float pk = g.Range(0.2f, 0.32f);
                float f0 = g.Range(105f, 135f) / throat;
                float[] h = Dsp.Voice(g, len, new VoiceParams
                {
                    Whisper = true,
                    Breath = 1f,
                    Formants = u => Vowel.Lerp(a, b, u),
                    Amp = u => Dsp.Bell(u, 1f, pk),
                    G1 = 1f, G2 = 0.7f, G3 = 0.4f, Direct = 0.1f,
                });
                float[] v = Dsp.Voice(g, len, new VoiceParams
                {
                    F0 = u => f0 * (1.05f - 0.12f * u),
                    Formants = u => Vowel.Lerp(a, b, u),
                    Amp = u => Dsp.Bell(u, 1f, pk),
                    Breath = 0.6f, Rough = 0.3f, Tilt = 1200f, Jitter = 0.03f, Direct = 0.05f,
                });
                Dsp.Norm(h);
                Dsp.Norm(v);
                float voice = g.Range(0.08f, 0.2f);
                for (int i = 0; i < h.Length; i++) h[i] += v[i] * voice;
                Dsp.Norm(h);
                Dsp.MixWrap(loop, h, At(g, at), depth);
            }

            /// <summary>The visor cracking an inch from the ear: a hard bright snap with a glassy ring, the crack running in
            /// a scatter of ticks, and the visor flexing against the face.</summary>
            internal static float[] MaskCrack(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.7f);
                float t0 = 0.004f;
                Tick(x, g, t0, 1f, 0.0004f, 2500f, 16000f);
                Ring(x, g, t0, 0.5f, g.Range(2700f, 3500f), GlassR, GlassA, 0.035f, 0.6f, 0.04f);
                int k = g.Int(14, 26);
                float t = t0 + g.Range(0.006f, 0.014f);
                for (int i = 0; i < k; i++)
                {
                    float a = g.Range(0.15f, 0.5f) * MathF.Pow(0.93f, i);
                    Tick(x, g, t, a, 0.0003f, 3000f, 15000f);
                    if (g.Chance(0.4f)) Ring(x, g, t, a * 0.25f, g.Range(4000f, 8000f), GlassR, GlassA, 0.012f, 0.6f, 0.1f);
                    t += g.Range(0.002f, 0.008f) * (1f + i * 0.1f);
                }
                Dsp.Thump(x, rate, t0, 0.3f, 240f, 160f, 0.01f, 0.001f, 0.02f);
                Dsp.HP(x, 60f, rate);
                return Trim(x, g);
            }

            /// <summary>A gloved hand wiping the visor: a rubbing hiss with a chattering squeak, once or back and forth.</summary>
            internal static float[] VisorWipe(SynthGen g)
            {
                int rate = g.Rate;
                bool twice = g.Variation % 2 == 1;
                float[] x = g.Buffer(twice ? 0.85f : 0.6f);
                int strokes = twice ? 2 : 1;
                float t = 0.005f;
                for (int s = 0; s < strokes; s++)
                {
                    float sl = twice ? g.Range(0.28f, 0.34f) : g.Range(0.38f, 0.46f);
                    float pk = g.Range(0.35f, 0.55f);
                    Rustle(x, g, t, sl, 0.45f, 1500f, 6000f, 1.2f, 0f, tt => Dsp.Bell(tt, sl, pk), 0.4f);
                    float[] sq = Creak(g, sl, u => Dsp.Bell(u, 1f, pk), 120f, g.Range(650f, 1100f),
                        new[] { g.Range(1300f, 1800f), g.Range(2500f, 3300f), g.Range(4200f, 5200f) }, new[] { 8f, 10f, 10f },
                        new[] { 1f, 0.7f, 0.35f }, 0.04f, 0.2f, 0.4f);
                    Fade(sq, rate, 0.01f, 0.02f);
                    Dsp.Mix(x, sq, At(g, t), g.Range(0.45f, 0.65f));
                    Dsp.Mix(x, Air(g, sl, 120f, 500f, tt => Dsp.Bell(tt, sl, pk)), At(g, t), 0.12f);
                    t += sl + g.Range(0.02f, 0.06f);
                }
                Dsp.HP(x, 100f, rate);
                return Trim(x, g);
            }

            /// <summary>One heartbeat felt in the chest: the deep lub, then the shorter, higher dub; harmonics so it carries on
            /// small speakers.</summary>
            internal static float[] Heartbeat(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.85f);
                float t1 = 0.012f, t2 = t1 + g.Range(0.26f, 0.31f);
                Beat(x, g, t1, 1f, g.Range(55f, 64f), 0.055f);
                Beat(x, g, t2, g.Range(0.6f, 0.72f), g.Range(68f, 78f), 0.04f);
                Dsp.Norm(x);
                Dsp.Saturate(x, 1.8f);
                Dsp.LP(x, 900f, rate);
                Dsp.HP(x, 22f, rate);
                return Trim(x, g);
            }

            private static void Beat(float[] x, SynthGen g, float at, float gain, float hz, float tau)
            {
                int rate = g.Rate;
                Dsp.Thump(x, rate, at, gain, hz * 1.25f, hz * 0.8f, 0.03f, 0.008f, tau);
                Dsp.Thump(x, rate, at + 0.004f, gain * 0.3f, hz * 2.4f, hz * 1.7f, 0.02f, 0.004f, tau * 0.5f);
                float[] b = Dsp.Burst(g, 0.004f, 0.018f, 260f, 30f);
                Dsp.Norm(b);
                Dsp.Mix(x, b, At(g, at), gain * 0.35f);
            }

            /// <summary>A human sneeze: one or two catching gasps ("ah... ah..."), then the explosive "tshoo" — a burst of
            /// air and spray and a yelping voice that drops away.</summary>
            internal static float[] Sneeze(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.4f);
                float pitch = g.Range(0.85f, 1.2f);
                int gasps = g.Variation == 0 ? 2 : 1;
                float t = 0.01f;
                for (int i = 0; i < gasps; i++)
                {
                    float gl = g.Range(0.2f, 0.28f);
                    float f0 = 200f * pitch * (1f + 0.15f * i);
                    float[] v = Dsp.Voice(g, gl, new VoiceParams
                    {
                        F0 = u => f0 * (1f + 0.25f * u),
                        Formants = u => Vowel.Lerp(Vowel.A, Vowel.Ae, u),
                        Amp = u => Dsp.Bell(u, 1f, 0.6f),
                        Breath = 0.9f, Direct = 0.05f, Tilt = 2500f, Jitter = 0.02f,
                    });
                    Dsp.Norm(v);
                    Dsp.Mix(x, v, At(g, t), 0.28f + 0.07f * i);
                    t += gl + g.Range(0.12f, 0.22f);
                }
                t += g.Range(0.02f, 0.06f);

                Tick(x, g, t, 0.9f, 0.006f, 1500f, 9000f);
                Dsp.Mix(x, Air(g, 0.5f, 1800f, 7000f, tt => Dsp.AD(tt, 0.002f, 0.07f)), At(g, t), 0.7f);
                Dsp.Mix(x, Air(g, 0.3f, 2500f, 8000f, tt => Dsp.AD(tt, 0.004f, 0.05f)), At(g, t), 0.45f);
                float vl = g.Range(0.28f, 0.36f);
                float f1 = 330f * pitch;
                float[] ch = Dsp.Voice(g, vl, new VoiceParams
                {
                    F0 = u => f1 * (1f - 0.35f * u),
                    Formants = u => Vowel.Lerp(Vowel.Uh, Vowel.U, Dsp.SmoothStep(u * 2f)),
                    Amp = u => Dsp.AD(u, 0.03f, 0.3f),
                    Breath = 1.1f, Rough = 0.25f, Direct = 0.1f, Tilt = 3000f, Jitter = 0.03f,
                });
                Dsp.Norm(ch);
                Dsp.Mix(x, ch, At(g, t + 0.004f), 0.9f);
                Dsp.HP(x, 80f, rate);
                return Trim(x, g);
            }

            /// <summary>A human cough: a few barks (the glottis bursting open, a rough breathy voice, the chest behind it),
            /// fading, sometimes wet, sometimes ending in a wheezing breath in.</summary>
            internal static float[] Cough(SynthGen g)
            {
                int rate = g.Rate;
                int pattern = g.Variation % 4;
                float[] x = g.Buffer(1.45f);
                float pitch = g.Range(0.85f, 1.15f);
                float wet = g.Range(0.1f, 0.5f);
                int count = pattern == 0 ? 2 : pattern == 1 ? 3 : pattern == 2 ? 1 : 4;
                float t = 0.008f;
                float a = 1f;
                for (int i = 0; i < count; i++)
                {
                    OneCough(x, g, t, a, pitch * (1f - 0.04f * i), wet);
                    t += g.Range(0.2f, 0.32f) * (i == 0 ? 1f : 0.9f);
                    a *= g.Range(0.7f, 0.88f);
                }
                if (pattern >= 2)
                {
                    float wl = g.Range(0.25f, 0.4f);
                    float[] w = Dsp.Voice(g, wl, new VoiceParams
                    {
                        Whisper = true,
                        Breath = 1f,
                        Formants = u => Vowel.Lerp(Vowel.I, Vowel.E, u).Scale(1.1f, 0.7f),
                        Amp = u => Dsp.Bell(u, 1f, 0.6f),
                        Direct = 0.2f,
                    });
                    Dsp.Norm(w);
                    Dsp.Mix(x, w, At(g, t + 0.02f), 0.22f);
                }
                Dsp.HP(x, 70f, rate);
                return Trim(x, g);
            }

            private static void OneCough(float[] x, SynthGen g, float at, float gain, float pitch, float wet)
            {
                int rate = g.Rate;
                Tick(x, g, at, 0.5f * gain, 0.004f, 300f, 5000f);
                float len = g.Range(0.16f, 0.24f);
                float f0 = g.Range(115f, 150f) * pitch;
                float k = g.Range(0.95f, 1.08f);
                float[] v = Dsp.Voice(g, len, new VoiceParams
                {
                    F0 = u => f0 * (1.15f - 0.3f * u),
                    Formants = u => Vowel.Lerp(Vowel.Uh, Vowel.Aw, u).Scale(k, 1.3f),
                    Amp = u => Dsp.AD(u, 0.04f, 0.28f),
                    Breath = 1.4f, Rough = 0.7f, Gurgle = wet, Direct = 0.12f, Tilt = 2600f, Jitter = 0.04f,
                });
                Dsp.Norm(v);
                Dsp.Mix(x, v, At(g, at + 0.002f), gain);
                Dsp.Thump(x, rate, at, 0.35f * gain, 150f * pitch, 95f * pitch, 0.02f, 0.003f, 0.04f);
            }
            #endregion

            #region Interface
            /// <summary>A soft tactile switch: a faint pre-click as it travels, the snap and the housing's little thock.</summary>
            internal static float[] UiClick(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.07f);
                float t = 0.0025f;
                Tick(x, g, t, 0.12f, 0.0004f, 2500f, 9000f);
                float ts = t + g.Range(0.004f, 0.007f);
                Ring(x, g, ts, 0.7f, g.Range(2300f, 2700f), ShellR, ShellA, 0.0035f, 0.7f, 0.04f);
                Tick(x, g, ts, 0.5f, 0.0005f, 1800f, 8500f);
                Ring(x, g, ts, 0.3f, g.Range(700f, 900f), ShellR, ShellA, 0.008f, 0.6f, 0.04f);
                Dsp.HP(x, 200f, rate);
                Dsp.LP(x, 10000f, rate);
                return x;
            }

            /// <summary>The lightest tick: a fingertip settling on a key.</summary>
            internal static float[] UiHover(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.04f);
                float t = 0.002f;
                Ring(x, g, t, 0.5f, g.Range(3000f, 3600f), ShellR, ShellA, 0.0018f, 0.7f, 0.05f);
                Tick(x, g, t, 0.35f, 0.00035f, 2500f, 8000f);
                Ring(x, g, t, 0.2f, g.Range(1100f, 1400f), ShellR, ShellA, 0.004f, 0.6f, 0.05f);
                Dsp.HP(x, 400f, rate);
                Dsp.LP(x, 9000f, rate);
                return x;
            }

            /// <summary>A ledger opened: its clasp gives softly, the cover lifts and the pages fan, the cover settles.</summary>
            internal static float[] UiOpen(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.32f);
                Metal(x, g, 0.003f, 0.35f, g.Range(2600f, 3200f), 0.004f, 0.4f);
                Rustle(x, g, 0.02f, 0.16f, 0.6f, 1200f, 5500f, 1f, 0.4f, t => Dsp.Bell(t, 0.16f, 0.35f));
                float tc = g.Range(0.15f, 0.19f);
                Dsp.Thump(x, rate, tc, 0.4f, 210f, 150f, 0.01f, 0.002f, 0.02f);
                Tick(x, g, tc, 0.15f, 0.003f, 150f, 1200f);
                Dsp.HP(x, 120f, rate);
                return Trim(x, g);
            }

            /// <summary>A ledger shut: a short swish of pages, the cover's soft thup, the clasp catching.</summary>
            internal static float[] UiClose(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.26f);
                Rustle(x, g, 0.003f, 0.09f, 0.45f, 1000f, 4500f, 1f, 0.3f, t => Dsp.Bell(t, 0.09f, 0.6f));
                float tc = g.Range(0.07f, 0.09f);
                Dsp.Thump(x, rate, tc, 0.7f, 190f, 125f, 0.01f, 0.0015f, 0.025f);
                Tick(x, g, tc, 0.25f, 0.003f, 120f, 1500f);
                Metal(x, g, tc + 0.012f, 0.18f, g.Range(2300f, 2600f), 0.004f, 0.3f);
                Dsp.HP(x, 100f, rate);
                return Trim(x, g);
            }

            /// <summary>Done: a rubber stamp comes down on the ledger (the handle's click, the thump on paper), and a small
            /// desk bell answers.</summary>
            internal static float[] UiBuy(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.7f);
                Metal(x, g, 0.003f, 0.2f, g.Range(2200f, 2800f), 0.004f, 0.3f);
                float ts = g.Range(0.02f, 0.03f);
                Dsp.Thump(x, rate, ts, 1f, 230f, 150f, 0.008f, 0.0015f, 0.035f);
                Tick(x, g, ts, 0.45f, 0.004f, 200f, 2500f);
                Crackle(x, g, ts, ts + 0.02f, 8, 0.12f, 0.0004f, 2000f, 8000f, null);
                float tb = ts + g.Range(0.05f, 0.07f);
                Ring(x, g, tb, 0.12f, g.Range(2000f, 2150f), BellR, BellA, 0.1f, 0.6f, 0.005f);
                Tick(x, g, tb, 0.04f, 0.0003f, 3000f, 10000f);
                Dsp.HP(x, 120f, rate);
                return Trim(x, g);
            }

            /// <summary>No: a locked drawer jerked twice — two dull clunks, the second lower.</summary>
            internal static float[] UiError(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.32f);
                float t = 0.003f;
                for (int i = 0; i < 2; i++)
                {
                    float a = i == 0 ? 1f : 0.85f;
                    float hz = (i == 0 ? 1f : 0.84f) * g.Range(190f, 210f);
                    Dsp.Thump(x, rate, t, a * 0.8f, hz * 1.2f, hz, 0.008f, 0.0015f, 0.03f);
                    Ring(x, g, t, a * 0.3f, hz * 2.1f, PanelR, PanelA, 0.018f, 0.65f, 0.03f);
                    Metal(x, g, t + 0.003f, a * 0.18f, g.Range(1200f, 1500f) * (i == 0 ? 1f : 0.9f), 0.006f, 0.25f);
                    t += g.Range(0.1f, 0.12f);
                }
                Dsp.HP(x, 90f, rate);
                Dsp.LP(x, 6000f, rate);
                return Trim(x, g);
            }

            /// <summary>An old cash register: the key and the mechanism turning over, the bell, the drawer running out on its
            /// rollers to its stop, and the coins in the tray.</summary>
            internal static float[] Cash(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.45f);
                float t = 0.004f;
                Metal(x, g, t, 0.35f, g.Range(1700f, 2200f), 0.006f, 0.5f);
                Metal(x, g, t + g.Range(0.025f, 0.035f), 0.45f, g.Range(1300f, 1700f), 0.008f, 0.5f);
                Crackle(x, g, t + 0.01f, t + 0.06f, 12, 0.12f, 0.0004f, 1500f, 8000f, null);
                float tb = t + g.Range(0.05f, 0.07f);
                Ring(x, g, tb, 0.6f, g.Range(2550f, 2700f), BellR, BellA, 0.28f, 0.55f, 0.004f);
                Tick(x, g, tb, 0.1f, 0.0004f, 3000f, 12000f);
                float td = tb + g.Range(0.02f, 0.04f);
                float run = g.Range(0.14f, 0.2f);
                Dsp.Mix(x, Air(g, run, 120f, 900f, tt => Dsp.Bell(tt, run, 0.6f)), At(g, td), 0.18f);
                Crackle(x, g, td, td + run, 20, 0.1f, 0.0004f, 2500f, 9000f, null);
                float ts = td + run;
                Wood(x, g, ts, 0.5f, g.Range(250f, 320f), 0.03f, 0.6f);
                Metal(x, g, ts + 0.003f, 0.3f, g.Range(1500f, 2000f), 0.01f, 0.5f);
                int coins = g.Int(5, 10);
                for (int i = 0; i < coins; i++)
                    AddCoin(x, g, ts + g.Range(0.005f, 0.2f) * (i / (float)coins + 0.2f), g.Range(0.08f, 0.22f) * (1f - 0.5f * i / coins));
                Dsp.HP(x, 80f, rate);
                return Trim(x, g);
            }
            #endregion

            #region Small actions
            /// <summary>Taking an item: the glove closing on it, then what it is — cloth, a plastic case, metal odds and ends,
            /// a box, a tin.</summary>
            internal static float[] Pickup(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.4f);
                Rustle(x, g, 0.003f, g.Range(0.06f, 0.1f), 0.45f, 900f, 4000f, 1f, 0.3f, tt => Dsp.Bell(tt, 0.08f, 0.3f));
                float t = g.Range(0.04f, 0.07f);
                switch (g.Variation % 5)
                {
                    case 0:
                    {
                        float len = g.Range(0.12f, 0.18f);
                        Rustle(x, g, t, len, 0.8f, 700f, 3500f, 1f, 0.4f, tt => Dsp.Bell(tt, len, 0.35f));
                        Dsp.Thump(x, rate, t, 0.25f, 180f, 120f, 0.01f, 0.003f, 0.02f);
                        break;
                    }
                    case 1:
                        Plastic(x, g, t, 0.9f, g.Range(800f, 1200f), 0.012f);
                        Plastic(x, g, t + g.Range(0.04f, 0.07f), 0.4f, g.Range(900f, 1400f), 0.01f);
                        break;
                    case 2:
                    {
                        int k = g.Int(3, 6);
                        float a = 0.6f;
                        for (int i = 0; i < k; i++)
                        {
                            Metal(x, g, t, a, g.Range(2500f, 5500f), g.Range(0.01f, 0.03f), 0.4f);
                            t += g.Range(0.015f, 0.03f);
                            a *= 0.7f;
                        }
                        break;
                    }
                    case 3:
                        Crackle(x, g, t, t + 0.1f, 25, 0.35f, 0.0005f, 1800f, 8000f, u => 1f - u);
                        Dsp.Thump(x, rate, t, 0.35f, 220f, 160f, 0.01f, 0.002f, 0.015f);
                        Tick(x, g, t, 0.2f, 0.002f, 200f, 1500f);
                        break;
                    default:
                        Ring(x, g, t, 0.5f, g.Range(1400f, 2000f), CanR, CanA, 0.05f, 0.6f, 0.03f);
                        Tick(x, g, t, 0.3f, 0.0005f, 1500f, 9000f);
                        Metal(x, g, t + g.Range(0.03f, 0.06f), 0.15f, g.Range(2500f, 3500f), 0.01f, 0.3f);
                        break;
                }
                Dsp.HP(x, 100f, rate);
                return Trim(x, g);
            }

            /// <summary>Eating: a wrapper torn open, a spoon in a tin, or straight in — then a bite and chewing (a crunch or a
            /// wet squelch) and a swallow.</summary>
            internal static float[] Eat(SynthGen g)
            {
                int rate = g.Rate;
                int kind = g.Variation % 3;
                float[] x = g.Buffer(1.3f);
                float t = 0.004f;
                if (kind == 0)
                {
                    Crackle(x, g, t, t + 0.16f, 60, 0.6f, 0.0005f, 2000f, 10000f, u => Dsp.Bell(u, 1f, 0.4f));
                    Rustle(x, g, t, 0.18f, 0.35f, 2000f, 7000f, 1.2f, 0f, tt => Dsp.Bell(tt, 0.18f, 0.4f));
                    t += 0.24f;
                }
                else if (kind == 1)
                {
                    Ring(x, g, t, 0.4f, g.Range(1500f, 1900f), CanR, CanA, 0.06f, 0.6f, 0.03f);
                    Metal(x, g, t, 0.3f, g.Range(3500f, 4500f), 0.02f, 0.4f);
                    Ring(x, g, t + 0.13f, 0.25f, g.Range(1500f, 1900f), CanR, CanA, 0.05f, 0.6f, 0.03f);
                    Metal(x, g, t + 0.13f, 0.2f, g.Range(3500f, 4500f), 0.015f, 0.4f);
                    t += 0.3f;
                }
                else t += 0.03f;

                bool crunchy = kind != 1;
                int chews = g.Int(3, 5);
                for (int i = 0; i < chews && t < 1f; i++)
                {
                    float a = i == 0 ? 1f : g.Range(0.5f, 0.8f) * MathF.Pow(0.85f, i);
                    Chew(x, g, t, a, crunchy && i < 3);
                    t += g.Range(0.22f, 0.3f);
                }
                if (t < 1.1f) Gulp(x, g, t + 0.03f, 0.35f);
                Dsp.HP(x, 60f, rate);
                Dsp.Compress(x, rate, -20f, 3f, 0.5f, 60f);
                return Trim(x, g);
            }

            private static void Chew(float[] x, SynthGen g, float at, float gain, bool crunch)
            {
                int rate = g.Rate;
                Dsp.Thump(x, rate, at, gain * 0.3f, 150f, 95f, 0.012f, 0.006f, 0.03f);
                if (crunch)
                {
                    Crackle(x, g, at, at + g.Range(0.05f, 0.09f), g.Int(18, 40), gain * 0.9f, 0.0006f, 1100f, 7000f, u => MathF.Pow(1f - u, 1.4f));
                    return;
                }
                float len = g.Range(0.07f, 0.11f);
                float[] s = Dsp.White(g, g.N(len));
                float[] am = Dsp.Wander(g, s.Length, rate, 70f);
                Dsp.BP(s, g.Range(700f, 1100f), 1.1f, rate);
                for (int i = 0; i < s.Length; i++) s[i] *= Dsp.Bell(i / (float)rate, len, 0.35f) * (0.2f + 0.8f * am[i] * am[i]);
                Dsp.Norm(s);
                Dsp.Mix(x, s, At(g, at), gain * 0.5f);
                for (int k = 0; k < 2; k++) Tick(x, g, at + g.Range(0.01f, len), gain * g.Range(0.1f, 0.25f), 0.0008f, 900f, 3500f);
            }

            /// <summary>A swallow: the tongue's click, then the throat's resonant "glk" sliding down, felt low.</summary>
            private static void Gulp(float[] x, SynthGen g, float at, float gain)
            {
                int rate = g.Rate;
                Tick(x, g, at, gain * 0.3f, 0.0015f, 700f, 2600f);
                Dsp.Thump(x, rate, at + 0.012f, gain, g.Range(330f, 400f), g.Range(170f, 220f), 0.025f, 0.004f, 0.03f);
                Dsp.Thump(x, rate, at + 0.016f, gain * 0.6f, 120f, 85f, 0.02f, 0.006f, 0.045f);
            }

            /// <summary>Drinking from a bottle or a canteen: two or three gulps, air glugging into the bottle between them,
            /// sometimes a breath out after.</summary>
            internal static float[] Drink(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.4f);
                int gulps = g.Int(2, 4);
                float t = 0.01f;
                for (int i = 0; i < gulps; i++)
                {
                    int nb = g.Int(2, 4);
                    for (int b = 0; b < nb; b++)
                    {
                        float[] bub = Dsp.Bubble(rate, g.Range(380f, 720f), g.Range(0.05f, 0.09f), g.Range(0.5f, 1.4f));
                        Dsp.Mix(x, bub, At(g, t + g.Range(0f, 0.18f)), g.Range(0.2f, 0.45f));
                    }
                    Gulp(x, g, t + g.Range(0.12f, 0.2f), i == 0 ? 1f : g.Range(0.75f, 0.95f));
                    t += g.Range(0.32f, 0.42f);
                }
                if (g.Chance(0.5f))
                {
                    float[] ah = Dsp.Voice(g, 0.25f, new VoiceParams
                    {
                        Whisper = true,
                        Breath = 1f,
                        Formants = u => Vowel.A,
                        Amp = u => Dsp.Bell(u, 1f, 0.3f),
                        Direct = 0.15f,
                    });
                    Dsp.Norm(ah);
                    Dsp.Mix(x, ah, At(g, t + 0.05f), 0.2f);
                }
                Dsp.HP(x, 60f, rate);
                return Trim(x, g);
            }

            /// <summary>Dressing a wound: the gauze torn (fibres popping in a quick run over a coarse swish), then wound round
            /// in a few soft passes.</summary>
            internal static float[] Bandage(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.3f);
                float tl = g.Range(0.22f, 0.34f);
                Crackle(x, g, 0.005f, 0.005f + tl, g.Int(180, 280), 0.75f, 0.00045f, 1300f, 8000f,
                    u => 0.4f + 0.6f * Dsp.SmoothStep(u * 3f) * (1f - Dsp.SmoothStep((u - 0.85f) / 0.15f)));
                Rustle(x, g, 0.005f, tl, 0.4f, 1500f, 4500f, 1.3f, 0f, tt => Dsp.Bell(tt, tl, 0.6f));
                // The weave giving way thread by thread: a ragged rasp that speeds up as the tear runs.
                float[] rip = Creak(g, tl, u => Dsp.SmoothStep(u * 2.5f) * (1f - Dsp.SmoothStep((u - 0.8f) / 0.2f)), 50f, g.Range(140f, 220f),
                    new[] { g.Range(1800f, 2500f), g.Range(3400f, 4400f), g.Range(5500f, 7000f) }, new[] { 2f, 3f, 3f }, new[] { 1f, 0.8f, 0.5f },
                    0.3f, 0.5f, 0.6f);
                Fade(rip, rate, 0.005f, 0.02f);
                Dsp.Mix(x, rip, At(g, 0.005f), 0.35f);
                float t = 0.005f + tl + g.Range(0.1f, 0.16f);
                int passes = g.Int(2, 4);
                for (int i = 0; i < passes && t < 1.05f; i++)
                {
                    float pl = g.Range(0.14f, 0.22f);
                    Rustle(x, g, t, pl, 0.35f, 600f, 2800f, 0.9f, 0.15f, tt => Dsp.Bell(tt, pl, 0.5f));
                    t += pl + g.Range(0.02f, 0.06f);
                }
                Dsp.HP(x, 120f, rate);
                return Trim(x, g);
            }

            /// <summary>An autoinjector: the safety cap pops, the spring fires with a hard clack and a ringing spring, and the
            /// dose goes in with a hiss.</summary>
            internal static float[] Injector(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.8f);
                Plastic(x, g, 0.004f, 0.4f, g.Range(1500f, 2000f));
                float tc = g.Range(0.13f, 0.19f);
                Metal(x, g, tc, 1f, g.Range(2600f, 3200f), 0.008f, 0.8f);
                Plastic(x, g, tc, 0.5f, g.Range(900f, 1200f));
                Ring(x, g, tc + 0.002f, 0.15f, g.Range(3800f, 4600f), SteelR, SteelA, 0.06f, 0.6f, 0.02f);
                float hl = g.Range(0.3f, 0.45f);
                Dsp.Mix(x, Air(g, hl, 3500f, 11000f, tt => Dsp.AD(tt, 0.012f, hl * 0.3f)), At(g, tc + 0.008f), 0.35f);
                Dsp.HP(x, 150f, rate);
                return Trim(x, g);
            }

            /// <summary>A page turned: the corner lifts with a crinkle, the sheet sweeps over cutting the air (crackling with
            /// its stiffness), and lands flat.</summary>
            internal static float[] PageTurn(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.5f);
                float len = g.Range(0.24f, 0.32f);
                Crackle(x, g, 0.004f, 0.05f, 10, 0.25f, 0.0004f, 2500f, 9000f, null);
                float[] w = Dsp.White(g, g.N(len));
                float f0 = g.Range(900f, 1400f), f1 = g.Range(2600f, 4200f);
                Dsp.SweepBP(w, rate, 0.9f, tt => Dsp.Lerp(f0, f1, Dsp.SmoothStep(tt / len)));
                float[] crinkle = Crinkle(g, w.Length, 0.8f, tt => Dsp.Bell(tt, len, 0.55f));
                for (int i = 0; i < w.Length; i++) w[i] *= Dsp.Bell(i / (float)rate, len, 0.55f) * (0.45f + 0.55f * crinkle[i]);
                Dsp.Norm(w);
                Dsp.Mix(x, w, At(g, 0.01f), 0.7f);
                Crackle(x, g, 0.03f, 0.01f + len, g.Int(20, 40), 0.3f, 0.0004f, 2000f, 10000f, u => Dsp.Bell(u, 1f, 0.6f));
                float tl = 0.01f + len * 0.9f;
                Dsp.Thump(x, rate, tl, 0.25f, 220f, 150f, 0.01f, 0.002f, 0.015f);
                Tick(x, g, tl, 0.18f, 0.002f, 300f, 2500f);
                Dsp.HP(x, 150f, rate);
                return Trim(x, g);
            }

            /// <summary>Something going into water: the cavity's low plop, a throw of spray, drops falling back and a slosh.</summary>
            internal static float[] Splash(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1.2f);
                float t0 = 0.004f;
                Dsp.Mix(x, Dsp.Bubble(rate, g.Range(220f, 380f), g.Range(0.08f, 0.12f), g.Range(0.8f, 1.6f)), At(g, t0 + 0.004f), 0.7f);
                Dsp.Thump(x, rate, t0, 0.35f, 120f, 70f, 0.02f, 0.002f, 0.03f);
                Dsp.Mix(x, Air(g, 0.6f, 400f, 7000f, tt => Dsp.AD(tt, 0.004f, 0.06f) + 0.35f * Dsp.AD(tt - 0.03f, 0.03f, 0.16f)), At(g, t0), 0.75f);
                int drops = g.Int(12, 22);
                for (int i = 0; i < drops; i++)
                {
                    float u = g.Next();
                    float td = t0 + 0.06f + 0.6f * u * u;
                    Dsp.Mix(x, Dsp.Bubble(rate, g.Range(700f, 2400f), g.Range(0.015f, 0.04f), g.Range(0.4f, 1.6f)), At(g, td), g.Range(0.05f, 0.2f) * (1f - 0.6f * u));
                    if (g.Chance(0.4f)) Tick(x, g, td, 0.06f * (1f - u), 0.001f, 1500f, 8000f);
                }
                int slosh = g.Int(2, 4);
                for (int i = 0; i < slosh; i++)
                    Dsp.Mix(x, Dsp.Bubble(rate, g.Range(400f, 700f), g.Range(0.05f, 0.09f), g.Range(0.3f, 1f)), At(g, t0 + g.Range(0.05f, 0.25f)), g.Range(0.2f, 0.35f));
                Dsp.HP(x, 50f, rate);
                return Trim(x, g);
            }

            /// <summary>A hand crank wound: the pawl clicking over the ratchet with every tooth, the little dynamo whirring up
            /// to speed with the turns, slowing on the last one.</summary>
            internal static float[] Crank(SynthGen g)
            {
                int rate = g.Rate;
                const float len = 1.3f;
                float[] x = g.Buffer(len + 0.1f);
                float rev = g.Range(1.8f, 2.4f), teeth = g.Range(6f, 9f);
                Func<float, float> speed = tt => (0.5f + 0.5f * Dsp.SmoothStep(tt / 0.3f)) * (1f - 0.4f * Dsp.SmoothStep((tt - (len - 0.3f)) / 0.3f));
                float t = 0.01f;
                int k = 0;
                while (t < len)
                {
                    float s = Math.Max(0.15f, speed(t));
                    Metal(x, g, t, 0.55f * (0.6f + 0.4f * s), g.Range(1800f, 2400f) * (k % 2 == 0 ? 1f : 1.06f), 0.006f, 0.5f);
                    Tick(x, g, t + 0.002f, 0.12f, 0.0003f, 3500f, 12000f);
                    t += 1f / (rev * teeth * s) * g.Range(0.92f, 1.08f);
                    k++;
                }
                var whir = new float[g.N(len)];
                float[] mesh = Dsp.White(g, whir.Length);
                float ph = 0f;
                for (int i = 0; i < whir.Length; i++)
                {
                    float tt = i / (float)rate;
                    float s = speed(tt);
                    float hz = 70f + 150f * s;
                    ph += hz / rate;
                    if (ph >= 1f) ph -= 1f;
                    float turn = s * (0.7f + 0.3f * MathF.Sin(Dsp.TwoPi * rev * tt));
                    whir[i] = Dsp.Saw(ph, hz / rate) * turn;
                    mesh[i] *= turn * (ph < 0.25f ? 1f : 0.35f);   // the gear teeth meshing
                }
                Dsp.BP(whir, 650f, 1.1f, rate);
                Dsp.Norm(whir);
                Dsp.BP(mesh, 2400f, 1.2f, rate);
                Dsp.Norm(mesh);
                for (int i = 0; i < whir.Length; i++) whir[i] += 0.4f * mesh[i];
                Fade(whir, rate, 0.02f, 0.05f);
                Dsp.Mix(x, whir, 0, 0.07f);
                Dsp.HP(x, 80f, rate);
                Dsp.Compress(x, rate, -16f, 2.5f, 0.3f, 60f);
                return Trim(x, g);
            }

            /// <summary>A zippo: the lid flicked open with its ringing clink, the thumb rolling the flint wheel (a gritty rasp
            /// and sparks), and the wick taking with a soft whump.</summary>
            internal static float[] Lighter(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(1f);
                float t0 = 0.004f;
                Ring(x, g, t0, 0.4f, g.Range(2900f, 3400f), ZippoR, ZippoA, 0.09f, 0.65f, 0.02f);
                Tick(x, g, t0, 0.35f, 0.0004f, 2500f, 14000f);
                Metal(x, g, t0 + g.Range(0.012f, 0.02f), 0.2f, g.Range(1800f, 2300f), 0.01f, 0.3f);
                float ts = t0 + g.Range(0.2f, 0.28f);
                float sl = g.Range(0.025f, 0.045f);
                Rustle(x, g, ts, sl, 0.5f, 2500f, 7000f, 1.5f, 0f, tt => Dsp.Bell(tt, sl, 0.4f));
                Crackle(x, g, ts, ts + sl + 0.03f, g.Int(12, 24), 0.35f, 0.0003f, 3000f, 14000f, u => 1f - u);
                float ti = ts + sl * 0.7f;
                Dsp.Mix(x, Air(g, 0.6f, 120f, 1400f, tt => Dsp.AD(tt, 0.012f, 0.06f) + 0.25f * Dsp.AD(tt - 0.02f, 0.05f, 0.25f)), At(g, ti), 0.55f);
                Dsp.Thump(x, rate, ti, 0.2f, 100f, 65f, 0.02f, 0.005f, 0.04f);
                Dsp.HP(x, 60f, rate);
                return Trim(x, g);
            }

            /// <summary>Handcuffs closing: the chain shifting, the swing arm ratcheting through the bow (a quick run of pawl
            /// clicks), the lock's heavier click and the chain settling.</summary>
            internal static float[] Handcuffs(SynthGen g)
            {
                int rate = g.Rate;
                float[] x = g.Buffer(0.85f);
                for (int i = 0; i < 3; i++) Metal(x, g, g.Range(0.005f, 0.09f), g.Range(0.08f, 0.18f), g.Range(3500f, 6000f), g.Range(0.02f, 0.04f), 0.3f);
                int teeth = g.Int(9, 15);
                float t = g.Range(0.12f, 0.16f);
                float gap = g.Range(0.011f, 0.017f);
                for (int i = 0; i < teeth; i++)
                {
                    float a = g.Range(0.45f, 0.7f) * (0.75f + 0.25f * (i % 2));
                    Metal(x, g, t, a, g.Range(3000f, 3600f) * (i % 2 == 0 ? 1f : 1.07f), 0.005f, 0.6f);
                    t += gap * g.Range(0.85f, 1.15f) * (1f + 0.03f * i);
                }
                Metal(x, g, t + 0.012f, 0.9f, g.Range(2200f, 2600f), 0.012f, 0.7f);
                for (int i = 0; i < 3; i++) Metal(x, g, t + g.Range(0.05f, 0.25f), g.Range(0.05f, 0.12f), g.Range(3500f, 6000f), 0.03f, 0.3f);
                Dsp.HP(x, 200f, rate);
                Dsp.Compress(x, rate, -16f, 2.5f, 0.3f, 50f);
                return Trim(x, g);
            }
            #endregion
        }
    }

    public static partial class SfxIds
    {
        /// <summary>Foley ids added with the reworked family (doors, breathing, interface chrome).</summary>
        public const string DoorKnock = "door_knock", DoorLocked = "door_locked", DoorBolt = "door_bolt", SwitchHeavy = "switch_heavy",
            BreathMaskHard = "breath_mask_hard", BreathHeavy = "breath_heavy",
            UiHover = "ui_hover", UiOpen = "ui_open", UiClose = "ui_close";
    }
}
