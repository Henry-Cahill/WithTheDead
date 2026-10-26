using System;

namespace CloneZ
{
    // Footsteps on every surface, for the survivor and for everything else that walks: boots on snow, boards, concrete,
    // grass, gravel, earth, mud, sheet metal, carpet, tile, leaf litter and water (and the wet versions when it rains),
    // the dead (shuffles, drags, stumbles, sprints, crawls) and the Zone's beasts (paws and claws, hooves, a bear's
    // weight, a Thumper's stomp, a Shade's touch, a Puppeteer's drag). Each is built from a step's anatomy: the heel
    // strike (a click and the leg's thud), the floor answering (resonance, crunch, splash), the ball of the foot coming
    // down a beat later, and the scuff or roll of the toe-off. All mono at 44.1 kHz; FootstepAudio plays them.
    public sealed partial class SfxSynth
    {
        partial void RegisterFootsteps()
        {
            // ── Boots by surface: the survivor's, the stalkers', other runners' ─────────────────────────────────────
            Footfall(SfxIds.StepSnow, StepKit.Snow, 8).Warm(40);
            Footfall(SfxIds.StepWood, g => StepKit.Wood(g, 0f), 8).Warm(40);
            Footfall(SfxIds.StepConcrete, g => StepKit.Concrete(g, 0f), 8).Warm(40);
            Footfall(SfxIds.StepGrass, StepKit.Grass, 8).Warm(40);
            Footfall(SfxIds.StepGravel, StepKit.Gravel, 6);
            Footfall(SfxIds.StepDirt, StepKit.Dirt, 6);
            Footfall(SfxIds.StepMud, StepKit.Mud, 6);
            Footfall(SfxIds.StepMetal, g => StepKit.Metal(g, 0f), 6);
            Footfall(SfxIds.StepCarpet, StepKit.Carpet, 6);
            Footfall(SfxIds.StepTile, StepKit.Tile, 6);
            Footfall(SfxIds.StepLeaves, StepKit.Leaves, 6);
            Footfall(SfxIds.StepWater, StepKit.Water, 6);
            // Wet ground: rain on the hard surfaces (splashy asphalt, a porch, a tin roof) and slush where it falls on snow.
            Footfall(SfxIds.StepConcreteWet, g => StepKit.Concrete(g, 1f), 6);
            Footfall(SfxIds.StepWoodWet, g => StepKit.Wood(g, 1f), 4);
            Footfall(SfxIds.StepMetalWet, g => StepKit.Metal(g, 1f), 4);
            Footfall(SfxIds.StepSlush, StepKit.Slush, 6);
            // The body above the feet: kit jostling on a sprint, a drop landed.
            Footfall(SfxIds.StepGear, StepKit.Gear, 6).Priority(175);
            Footfall(SfxIds.StepLand, StepKit.Land, 3).Priority(110);

            // ── The dead ─────────────────────────────────────────────────────────────────────────────────────────
            Footfall(SfxIds.StepZombieShuffle, StepKit.Shuffle, 6).Priority(165);
            Footfall(SfxIds.StepZombieDrag, StepKit.Drag, 4).Priority(170);
            Footfall(SfxIds.StepZombieLurch, StepKit.Lurch, 5).Priority(165);
            Footfall(SfxIds.StepZombieRun, StepKit.Run, 6).Priority(150);
            Footfall(SfxIds.StepCrawl, StepKit.Crawl, 5).Priority(165);

            // ── Beasts ───────────────────────────────────────────────────────────────────────────────────────────
            Footfall(SfxIds.StepPaw, g => StepKit.Paw(g, false), 6).Priority(165);
            Footfall(SfxIds.StepPawClaw, g => StepKit.Paw(g, true), 6).Priority(165);
            Footfall(SfxIds.StepHoof, g => StepKit.Hoof(g, false), 5).Priority(165);
            Footfall(SfxIds.StepHoofHard, g => StepKit.Hoof(g, true), 5).Priority(165);
            Footfall(SfxIds.StepHeavy, StepKit.Heavy, 5).Priority(140);
            Footfall(SfxIds.StepStomp, StepKit.Stomp, 4).Priority(100);
            Footfall(SfxIds.StepShade, StepKit.ShadeTouch, 5).Priority(150);
            Footfall(SfxIds.StepPuppet, StepKit.PuppetDrag, 4).Priority(150);
        }

        // A footstep recipe: mono at 44.1 kHz, each variation on its own random stream, low priority (one of many).
        private Recipe Footfall(string id, Func<SynthGen, float[]> make, int variations)
            => Add(id, g => make(StepKit.Fresh(g)), variations).Priority(155);

        /// <summary>The footstep family's toolbox: contacts, thuds, crunch, pings, splashes, scrapes and squeaks, and the
        /// steps built from them.</summary>
        private static partial class StepKit
        {
            #region Building blocks
            /// <summary>A generator of the recipe's own for this variation (derived from its stream and the variation
            /// number, so every variation differs whether or not the core mixes the variation into the seed).</summary>
            internal static SynthGen Fresh(SynthGen g)
            {
                uint s = g.NextUInt() ^ (0x9E3779B9u * (uint)(g.Variation + 1));
                s ^= s >> 15;
                s *= 0x2C1B3C6Du;
                s ^= s >> 12;
                s *= 0x297A2D39u;
                s ^= s >> 15;
                return new SynthGen(g.Rate, s == 0u ? 0x51ED27u : s, g.Variation);
            }

            private static int At(SynthGen g, float seconds) => (int)(seconds * g.Rate);

            /// <summary>A noise burst (attack, exponential decay) band-limited to [hp, lp], peak-normalised, mixed at gain.</summary>
            internal static void Hit(float[] x, SynthGen g, float at, float gain, float tau, float hp, float lp, float attack = 0.0002f)
            {
                float[] b = Dsp.Burst(g, attack, tau, lp, hp);
                Dsp.Norm(b);
                Dsp.Mix(x, b, At(g, at), gain);
            }

            /// <summary>A band-passed burst: a knock with a pitch (a hoof, a slap, a block of wood).</summary>
            internal static void Knock(float[] x, SynthGen g, float at, float gain, float tau, float hz, float q)
                => Dsp.AddBurstBP(x, g, at, gain, 0.0003f, tau, hz, q);

            /// <summary>The weight landing: a sine thud gliding down from hz0 toward hz1, soft-edged.</summary>
            internal static void Thud(float[] x, SynthGen g, float at, float gain, float hz0, float hz1, float tau, float attack = 0.002f)
                => Dsp.Thump(x, g.Rate, at, gain, hz0, hz1, tau * 0.6f, attack, tau);

            /// <summary>
            /// Granular crunch: <paramref name="count"/> tiny decaying noise grains between t0 and t1 (their density following
            /// env(u) over that span), sizes skewed so a few big cracks stand out of many small ones, band-limited to
            /// [hp, lp]; peak-normalised, mixed at gain. Snow packing, gravel grinding, leaves crumpling, grit.
            /// </summary>
            internal static void Crunch(float[] x, SynthGen g, float t0, float t1, int count, float gain, float tau0, float tau1,
                float skew, Func<float, float> env, float hp, float lp)
            {
                int rate = g.Rate;
                var y = new float[x.Length];
                for (int c = 0; c < count; c++)
                {
                    float u = 0f, e = 1f;
                    for (int tries = 0; tries < 8; tries++)
                    {
                        u = g.Next();
                        e = env != null ? Dsp.Clamp01(env(u)) : 1f;
                        if (g.Next() <= e) break;
                    }
                    int at = (int)((t0 + (t1 - t0) * u) * rate);
                    float amp = MathF.Pow(g.Next(), skew) * (0.4f + 0.6f * e);
                    float tau = tau0 + (tau1 - tau0) * g.Next();
                    float k = MathF.Exp(-1f / (tau * rate));
                    int len = (int)(tau * rate * 6f) + 1;
                    for (int i = 0; i < len; i++)
                    {
                        int j = at + i;
                        if (j >= y.Length) break;
                        if (j >= 0) y[j] += g.Bipolar() * amp;
                        amp *= k;
                    }
                }
                if (hp > 0f) Dsp.HP(y, hp, rate);
                if (lp > 0f) Dsp.LP(y, lp, rate);
                Dsp.Norm(y);
                Dsp.Mix(x, y, 0, gain);
            }

            /// <summary>
            /// Tiny resonant ticks (stones knocking together, claws, buckles): damped sines at log-uniform pitches in
            /// [f0, f1], decaying over tau0..tau1, at times following env(u) between t0 and t1, sizes skewed; the loudest
            /// is about <paramref name="gain"/>. Each starts at a random phase, so it ticks.
            /// </summary>
            internal static void Pings(float[] x, SynthGen g, float t0, float t1, int count, float gain, float f0, float f1,
                float tau0, float tau1, float skew, Func<float, float> env)
            {
                int rate = g.Rate;
                for (int c = 0; c < count; c++)
                {
                    float u = 0f, e = 1f;
                    for (int tries = 0; tries < 8; tries++)
                    {
                        u = g.Next();
                        e = env != null ? Dsp.Clamp01(env(u)) : 1f;
                        if (g.Next() <= e) break;
                    }
                    int at = (int)((t0 + (t1 - t0) * u) * rate);
                    float f = f0 * MathF.Pow(f1 / f0, g.Next());
                    float tau = tau0 + (tau1 - tau0) * g.Next();
                    float amp = gain * MathF.Pow(g.Next(), skew) * (0.4f + 0.6f * e);
                    float w = Dsp.TwoPi * f / rate, ph = g.Next() * Dsp.TwoPi;
                    float k = MathF.Exp(-1f / (tau * rate));
                    int len = (int)(tau * rate * 6f) + 1;
                    for (int i = 0; i < len; i++)
                    {
                        int j = at + i;
                        if (j >= x.Length) break;
                        if (j >= 0) x[j] += amp * MathF.Sin(ph + w * i);
                        amp *= k;
                    }
                }
            }

            /// <summary>Droplets: small rising bubbles (f0..f1 Hz, 6–22 ms) scattered between t0 and t1, most near the start.</summary>
            internal static void Drops(float[] x, SynthGen g, float t0, float t1, int count, float gain, float f0, float f1)
            {
                for (int c = 0; c < count; c++)
                {
                    float u = g.Next();
                    float at = t0 + (t1 - t0) * u * u;
                    float f = f0 * MathF.Pow(f1 / f0, g.Next());
                    float[] b = Dsp.Bubble(g.Rate, f, g.Range(0.006f, 0.022f), g.Range(0.4f, 1.6f));
                    Dsp.Mix(x, b, At(g, at), gain * (1f - 0.65f * u) * g.Range(0.35f, 1f));
                }
            }

            /// <summary>Water flung from under a foot: the slap on the surface, a sheet of spray, the water shoved aside and
            /// droplets falling back. <paramref name="size"/> 0..1 runs from a wet film to a puddle.</summary>
            internal static void Splash(float[] x, SynthGen g, float at, float gain, float size)
            {
                Knock(x, g, at, gain * 0.55f, 0.005f + 0.01f * size, g.Range(700f, 1300f), 0.8f);
                Hit(x, g, at + 0.002f, gain * 0.75f, 0.018f + 0.05f * size, 1900f, 11000f, 0.002f);
                Hit(x, g, at + 0.004f, gain * (0.15f + 0.35f * size), 0.03f + 0.07f * size, 300f, 2500f, 0.006f);
                Drops(x, g, at + 0.012f, at + 0.1f + 0.25f * size, 3 + (int)(10f * size), gain * 0.35f, 1100f, 3800f);
            }

            /// <summary>Friction — a sole, a claw or a body sliding: noise roughened by stick-slip (it grabs and lets go
            /// about <paramref name="roughHz"/> times a second, irregularly), band-passed at hz, shaped by env(u) over len
            /// seconds; peak-normalised, mixed at gain.</summary>
            internal static void Scrape(float[] x, SynthGen g, float at, float len, float gain, float hz, float q, float roughHz, Func<float, float> env)
            {
                int rate = g.Rate;
                int n = g.N(len);
                var y = new float[n];
                OnePole slip = OnePole.At(roughHz, rate);
                float k = slip.NoiseNormaliser;
                for (int i = 0; i < n; i++)
                {
                    float m = slip.Lp(g.Bipolar()) * k;
                    float grab = Math.Max(0f, 0.55f + 0.55f * m);
                    y[i] = g.Bipolar() * env(i / (float)n) * grab * grab;
                }
                Dsp.BP(y, hz, q, rate);
                Dsp.Norm(y);
                Dsp.Mix(x, y, At(g, at), gain);
            }

            /// <summary>Wet compression (a squelch): low-passed noise bubbling under a swell-and-fall envelope.</summary>
            internal static void Squish(float[] x, SynthGen g, float at, float len, float gain, float lp)
            {
                int rate = g.Rate;
                int n = g.N(len);
                float[] y = Dsp.White(g, n);
                OnePole bub = OnePole.At(70f, rate);
                float k = bub.NoiseNormaliser;
                for (int i = 0; i < n; i++)
                {
                    float m = Math.Max(0f, 0.5f + 0.5f * bub.Lp(g.Bipolar()) * k);
                    y[i] *= Dsp.Bell(i / (float)n, 1f, 0.3f) * (0.35f + m);
                }
                Dsp.LP(y, lp, rate, 1.4f);
                Dsp.Norm(y);
                Dsp.Mix(x, y, At(g, at), gain);
            }

            /// <summary>A rubber sole squeaking on a smooth floor: a buzzy tone gliding f0 → f1 with stick-slip flutter.</summary>
            internal static void Squeak(float[] x, SynthGen g, float at, float len, float gain, float f0, float f1)
            {
                int rate = g.Rate;
                int n = g.N(len);
                var y = new float[n];
                OnePole jit = OnePole.At(60f, rate);
                float jk = jit.NoiseNormaliser, ph = 0f;
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)n;
                    float f = Dsp.Glide(f0, f1, u) * (1f + 0.03f * jit.Lp(g.Bipolar()) * jk);
                    ph += f / rate;
                    if (ph >= 1f) ph -= 1f;
                    float a = Dsp.TwoPi * ph;
                    y[i] = (MathF.Sin(a) + 0.4f * MathF.Sin(2f * a) + 0.2f * MathF.Sin(3f * a)) * Dsp.Bell(u, 1f, 0.35f);
                }
                Dsp.Norm(y);
                Dsp.Mix(x, y, At(g, at), gain);
            }

            /// <summary>A creak (a board, a joint): stick-slip pulses through narrow resonances, mixed at gain.</summary>
            internal static void Creak(float[] x, SynthGen g, float at, float len, float gain, float rate0, float rate1, float[] resHz, float[] resQ)
            {
                float[] c = Dsp.Creak(g, len, u => Dsp.Lerp(rate0, rate1, u), u => Dsp.Bell(u, 1f, 0.4f), resHz, resQ, 0.25f);
                Dsp.Mix(x, c, At(g, at), gain);
            }

            /// <summary>Thicker: normalised, then compressed with a fast attack so the body holds up against the transient.</summary>
            internal static void Punch(float[] x, int rate, float thresholdDb, float ratio)
            {
                Dsp.Norm(x);
                Dsp.Compress(x, rate, thresholdDb, ratio, 0.4f, 60f);
            }

            /// <summary>Rounds off the few-sample spikes of a click-led step (tanh, after normalising), so it is not quiet
            /// beside the others once peak-normalised: the strike keeps its edge, the body gains a few dB.</summary>
            internal static void Tame(float[] x, float drive)
            {
                Dsp.Norm(x);
                Dsp.Saturate(x, drive);
            }

            /// <summary>Clears the rumble under the step (and the DC the thuds leave), and puts a hair of silence in front: the
            /// renderer fades in the first 1.5 ms of every one-shot, which would blunt the strike.</summary>
            internal static float[] Done(float[] x, SynthGen g, float hp = 30f)
            {
                Dsp.HP(x, hp, g.Rate);
                int pre = g.N(0.002f);
                var y = new float[x.Length + pre];
                Array.Copy(x, 0, y, pre, x.Length);
                return y;
            }
            #endregion
        }
    }

    // The steps themselves.
    public sealed partial class SfxSynth
    {
        private static partial class StepKit
        {
            #region Boots
            /// <summary>Boots on concrete or asphalt: the heel's hard edge cracks on the slab with the leg's thud behind it,
            /// grit crackles under the sole, the ball of the foot comes down a beat later and the toe pushes off with a
            /// short rubber scuff. Wet (1): every contact throws a film of water — splashy asphalt.</summary>
            internal static float[] Concrete(SynthGen g, float wet)
            {
                float[] x = g.Buffer(wet > 0f ? 0.42f : 0.34f);
                float ball = g.Range(0.05f, 0.1f), heel = g.Range(0.8f, 1f), fore = g.Range(0.4f, 0.65f);
                float dry = 1f - 0.35f * wet;
                Hit(x, g, 0f, heel * dry, g.Range(0.0022f, 0.0035f), 1100f, 9500f);
                Hit(x, g, 0.0004f, heel * 0.6f, g.Range(0.008f, 0.014f), 280f, 2400f, 0.0006f);
                Thud(x, g, 0f, heel * 0.3f, g.Vary(190f, 0.12f), 90f, 0.02f);
                Crunch(x, g, 0.002f, ball + 0.05f, g.Int(10, 22), 0.25f * dry, 0.00015f, 0.0006f, 3f, u => 1f - u, 3200f, 12000f);
                Hit(x, g, ball, fore * dry, g.Range(0.0018f, 0.003f), 1400f, 8500f);
                Hit(x, g, ball + 0.0005f, fore * 0.5f, 0.009f, 250f, 2000f, 0.001f);
                Thud(x, g, ball, fore * 0.25f, 170f, 90f, 0.015f);
                if (g.Chance(0.65f))
                    Scrape(x, g, ball + g.Range(0.05f, 0.12f), g.Range(0.05f, 0.11f), g.Range(0.07f, 0.15f), g.Range(2400f, 4200f), 0.9f, 45f, u => Dsp.Bell(u, 1f, 0.3f));
                if (wet > 0f)
                {
                    Splash(x, g, 0.003f, 0.6f * wet, 0.35f);
                    Splash(x, g, ball + 0.003f, 0.4f * wet, 0.2f);
                }
                Tame(x, 3f);
                return Done(x, g);
            }

            /// <summary>Boots on boards: a woody knock, and the floor answers — boards and joists ringing hollow under the
            /// weight (low modes that change with every board), set going by the heel and again by the ball of the foot;
            /// now and then a board creaks. Wet (1): a thin splash on each contact (a porch in the rain).</summary>
            internal static float[] Wood(SynthGen g, float wet)
            {
                float[] x = g.Buffer(0.42f);
                float ball = g.Range(0.06f, 0.11f), heel = g.Range(0.8f, 1f), fore = g.Range(0.45f, 0.7f);
                // The floor's modes: a hollow board-and-joist boom, a cluster of woody mids, a knock on top. Short: boots damp it.
                float f0 = g.Range(95f, 145f);
                float[] hz =
                {
                    f0, f0 * g.Range(1.85f, 2.3f), f0 * g.Range(3.1f, 3.8f), g.Range(380f, 520f), g.Range(560f, 760f),
                    g.Range(820f, 1100f), g.Range(1250f, 1750f), g.Range(2200f, 3000f),
                };
                float[] amp = { 0.7f, 0.6f, 0.5f, 0.45f, 0.4f, 0.3f, 0.18f, 0.08f };
                float[] tau = { g.Range(0.03f, 0.055f), 0.03f, 0.022f, 0.016f, 0.013f, 0.01f, 0.007f, 0.004f };
                Dsp.Modes(x, g.Rate, 0.0008f, heel * 0.7f, hz, amp, tau);
                Dsp.Modes(x, g.Rate, ball + 0.0008f, fore * 0.55f, hz, amp, tau);
                Hit(x, g, 0f, heel * (1f - 0.3f * wet), g.Range(0.0025f, 0.004f), 600f, 6500f);
                Hit(x, g, 0.0005f, heel * 0.4f, g.Range(0.006f, 0.01f), 250f, 1600f, 0.0006f);
                Thud(x, g, 0f, heel * 0.3f, g.Vary(150f, 0.1f), 75f, 0.025f);
                Hit(x, g, ball, fore * 0.8f * (1f - 0.3f * wet), g.Range(0.002f, 0.0035f), 700f, 6000f);
                Thud(x, g, ball, fore * 0.2f, 140f, 75f, 0.02f);
                Crunch(x, g, 0.001f, ball + 0.04f, g.Int(4, 10), 0.12f, 0.0002f, 0.0006f, 3f, null, 2500f, 9000f);
                if (g.Chance(0.35f))
                {
                    float r0 = g.Range(90f, 160f);
                    Creak(x, g, ball + g.Range(0.02f, 0.08f), g.Range(0.08f, 0.16f), g.Range(0.12f, 0.22f), r0, r0 * g.Range(0.6f, 1.4f),
                        new[] { g.Range(520f, 760f), g.Range(1100f, 1500f), g.Range(1900f, 2600f) }, new[] { 9f, 12f, 14f });
                }
                if (wet > 0f)
                {
                    Splash(x, g, 0.003f, 0.45f * wet, 0.2f);
                    Splash(x, g, ball + 0.003f, 0.3f * wet, 0.12f);
                }
                Tame(x, 2f);
                return Done(x, g);
            }

            /// <summary>Boots on tile or linoleum: a sharp, bright click with a glassy chip of ring, the short thud of the slab
            /// under it and the ball's click; sometimes the rubber squeaks on the smooth floor as the foot turns.</summary>
            internal static float[] Tile(SynthGen g)
            {
                float[] x = g.Buffer(0.34f);
                float ball = g.Range(0.05f, 0.095f), heel = g.Range(0.85f, 1f), fore = g.Range(0.45f, 0.7f);
                float[] chip = { g.Range(2300f, 3000f), g.Range(3900f, 4800f), g.Range(6200f, 7400f) };
                float[] ca = { 1f, 0.6f, 0.35f }, ct = { 0.008f, 0.005f, 0.003f };
                Hit(x, g, 0f, heel, g.Range(0.0014f, 0.0024f), 1800f, 13000f);
                Dsp.Modes(x, g.Rate, 0.0003f, heel * 0.25f, chip, ca, ct);
                Hit(x, g, 0.0004f, heel * 0.5f, g.Range(0.005f, 0.008f), 350f, 3000f, 0.0005f);
                Thud(x, g, 0f, heel * 0.3f, g.Vary(210f, 0.1f), 100f, 0.014f);
                Hit(x, g, ball, fore, g.Range(0.0012f, 0.002f), 2000f, 12000f);
                Dsp.Modes(x, g.Rate, ball + 0.0003f, fore * 0.18f, chip, ca, ct);
                Hit(x, g, ball + 0.0004f, fore * 0.4f, 0.005f, 350f, 3000f, 0.0005f);
                Thud(x, g, ball, fore * 0.22f, 190f, 100f, 0.011f);
                if (g.Chance(0.3f))
                    Squeak(x, g, ball + g.Range(0.04f, 0.1f), g.Range(0.03f, 0.07f), g.Range(0.08f, 0.16f), g.Range(1700f, 2600f), g.Range(1500f, 3000f));
                else if (g.Chance(0.5f))
                    Scrape(x, g, ball + g.Range(0.05f, 0.1f), g.Range(0.04f, 0.08f), 0.08f, 3500f, 1f, 50f, u => Dsp.Bell(u, 1f, 0.3f));
                Tame(x, 3f);
                return Done(x, g);
            }

            /// <summary>Boots on carpet or a rug: the weight lands soft — a muffled thump, the pile brushing the sole, almost no click.</summary>
            internal static float[] Carpet(SynthGen g)
            {
                float[] x = g.Buffer(0.3f);
                float ball = g.Range(0.06f, 0.11f);
                Thud(x, g, 0f, 0.55f, g.Vary(140f, 0.1f), 75f, 0.026f, 0.004f);
                Hit(x, g, 0.001f, 0.6f, 0.012f, 160f, 1400f, 0.003f);
                Crunch(x, g, 0f, ball + 0.08f, g.Int(30, 60), 0.22f, 0.0006f, 0.002f, 2f, u => Dsp.Bell(u, 1f, 0.3f), 900f, 3200f);
                Thud(x, g, ball, g.Range(0.3f, 0.42f), 130f, 75f, 0.022f, 0.005f);
                Hit(x, g, ball, 0.4f, 0.01f, 160f, 1200f, 0.003f);
                if (g.Chance(0.5f))
                    Scrape(x, g, ball + g.Range(0.05f, 0.1f), g.Range(0.06f, 0.1f), 0.1f, 1300f, 0.7f, 30f, u => Dsp.Bell(u, 1f, 0.4f));
                return Done(x, g, 35f);
            }

            /// <summary>Boots on sheet metal (corrugated roofing, a grate, a car's roof): the clank of the strike, the sheet
            /// booming and ringing inharmonically (damped by the sole), loose fixings rattling; the ball of the foot sets it
            /// off again. Wet (1): a film of water splashes off it.</summary>
            internal static float[] Metal(SynthGen g, float wet)
            {
                float[] x = g.Buffer(0.7f);
                float ball = g.Range(0.06f, 0.11f), heel = g.Range(0.8f, 1f), fore = g.Range(0.5f, 0.75f);
                // A sheet's modes are dense and inharmonic (a bell's are sparse): many partials, log-spread from a low
                // flexing boom up to a tinny top, the high ones dying first.
                const int M = 16;
                float lo = g.Range(130f, 200f), hi = g.Range(4500f, 6500f), ring = g.Range(0.08f, 0.15f);
                var hz = new float[M];
                var amp = new float[M];
                var tau = new float[M];
                for (int i = 0; i < M; i++)
                {
                    hz[i] = lo * MathF.Pow(hi / lo, (i + g.Range(0f, 0.85f)) / M);
                    amp[i] = MathF.Pow(0.86f, i) * g.Range(0.45f, 1f);
                    tau[i] = ring * MathF.Pow(0.87f, i) * g.Range(0.6f, 1.2f);
                }
                Dsp.Modes(x, g.Rate, 0.0005f, heel * 0.4f, hz, amp, tau);
                for (int i = 0; i < M; i++) amp[i] *= g.Range(0.5f, 1.3f);
                Dsp.Modes(x, g.Rate, ball + 0.0005f, fore * 0.32f, hz, amp, tau);
                Thud(x, g, 0f, heel * 0.4f, g.Range(85f, 120f), 55f, 0.06f);
                Hit(x, g, 0f, heel, g.Range(0.002f, 0.0035f), 900f, 11000f);
                Hit(x, g, 0.0004f, heel * 0.45f, 0.012f, 300f, 2500f, 0.0006f);
                Hit(x, g, ball, fore * 0.8f, 0.0025f, 900f, 10000f);
                Pings(x, g, 0.01f, ball + 0.12f, g.Int(3, 8), 0.12f, 1800f, 5200f, 0.004f, 0.012f, 1.5f, u => 1f - u);
                if (wet > 0f)
                {
                    Splash(x, g, 0.003f, 0.5f * wet, 0.25f);
                    Splash(x, g, ball + 0.003f, 0.3f * wet, 0.15f);
                }
                Tame(x, 2f);
                return Done(x, g, 40f);
            }

            /// <summary>Boots in snow: the crust and crystals breaking as the snow packs under the heel, then under the ball of
            /// the foot — distinct crackles over a compressed, squeaky body around 1 kHz — with the soft "fwump" of the boot
            /// sinking. Some variations break a crust first; the rest are powder.</summary>
            internal static float[] Snow(SynthGen g)
            {
                float[] x = g.Buffer(0.4f);
                float ball = g.Range(0.07f, 0.12f), heel = g.Range(0.8f, 1f), fore = g.Range(0.55f, 0.8f);
                float h1 = ball + g.Range(0.04f, 0.08f), f1 = ball + g.Range(0.12f, 0.2f);
                bool crust = g.Chance(0.35f);
                if (crust) Hit(x, g, 0.002f, heel * 0.8f, g.Range(0.0012f, 0.002f), 1300f, 9000f);
                Thud(x, g, 0.002f, 0.28f * heel, g.Vary(120f, 0.12f), 60f, 0.032f, 0.005f);
                Hit(x, g, 0.001f, 0.35f * heel, g.Range(0.02f, 0.03f), 90f, 650f, 0.004f);
                Crunch(x, g, 0f, h1, g.Int(45, 75), heel, 0.0003f, 0.0012f, 2.6f, u => Dsp.Bell(u, 1f, 0.22f), 500f, 7500f);
                Crunch(x, g, 0f, h1, g.Int(20, 34), heel * 0.55f, 0.0015f, 0.004f, 1.8f, u => Dsp.Bell(u, 1f, 0.3f), 700f, 1600f);
                Crunch(x, g, ball, f1, g.Int(35, 60), fore, 0.0003f, 0.0011f, 2.6f, u => Dsp.Bell(u, 1f, 0.3f), 500f, 6500f);
                Crunch(x, g, ball, f1, g.Int(16, 28), fore * 0.55f, 0.0015f, 0.004f, 1.8f, u => Dsp.Bell(u, 1f, 0.35f), 650f, 1500f);
                Thud(x, g, ball, 0.2f * fore, 110f, 60f, 0.028f, 0.006f);
                return Done(x, g);
            }

            /// <summary>Boots in slush: wet snow squelching and collapsing — a soft, watery squish with a few icy crunches and bubbles.</summary>
            internal static float[] Slush(SynthGen g)
            {
                float[] x = g.Buffer(0.44f);
                float ball = g.Range(0.07f, 0.12f);
                Thud(x, g, 0.003f, 0.6f, g.Vary(95f, 0.1f), 50f, 0.04f, 0.006f);
                Squish(x, g, 0f, g.Range(0.08f, 0.13f), 1f, g.Range(700f, 1100f));
                Squish(x, g, ball, g.Range(0.1f, 0.16f), 0.7f, g.Range(600f, 1000f));
                Crunch(x, g, 0f, ball + 0.12f, g.Int(15, 30), 0.35f, 0.0003f, 0.001f, 2.5f, u => Dsp.Bell(u, 1f, 0.3f), 1500f, 7000f);
                Drops(x, g, 0.02f, ball + 0.2f, g.Int(3, 7), 0.25f, 500f, 1600f);
                return Done(x, g);
            }

            /// <summary>Boots in grass: blades swishing against the boot and snapping under it, over the soft thump of the earth.</summary>
            internal static float[] Grass(SynthGen g)
            {
                float[] x = g.Buffer(0.38f);
                float ball = g.Range(0.06f, 0.11f);
                Thud(x, g, 0.003f, 0.3f, g.Vary(110f, 0.1f), 58f, 0.03f, 0.005f);
                Hit(x, g, 0.001f, 0.3f, 0.012f, 150f, 1200f, 0.002f);
                Scrape(x, g, 0f, g.Range(0.16f, 0.24f), 0.75f, g.Range(2600f, 3800f), 0.55f, 120f, u => Dsp.Bell(u, 1f, 0.25f));
                Crunch(x, g, 0f, ball + 0.12f, g.Int(25, 45), 0.6f, 0.0002f, 0.0008f, 2.5f, u => Dsp.Bell(u, 1f, 0.3f), 2000f, 9500f);
                Scrape(x, g, ball, g.Range(0.1f, 0.16f), 0.45f, g.Range(3000f, 4500f), 0.6f, 120f, u => Dsp.Bell(u, 1f, 0.3f));
                Thud(x, g, ball, 0.2f, 100f, 58f, 0.026f, 0.006f);
                return Done(x, g);
            }

            /// <summary>Boots on gravel: the stones grind and knock against each other as they shift — bright little clicks in
            /// two waves (heel, then the ball of the foot), grit hissing between them, the weight's thud under it.</summary>
            internal static float[] Gravel(SynthGen g)
            {
                float[] x = g.Buffer(0.42f);
                float ball = g.Range(0.07f, 0.12f);
                Thud(x, g, 0.002f, 0.35f, g.Vary(130f, 0.12f), 65f, 0.028f, 0.003f);
                Hit(x, g, 0f, 0.45f, 0.005f, 400f, 4000f);
                Pings(x, g, 0f, ball + 0.06f, g.Int(25, 40), 0.55f, 1600f, 6500f, 0.0015f, 0.005f, 1.8f, u => Dsp.Bell(u, 1f, 0.2f));
                Crunch(x, g, 0f, ball + 0.08f, g.Int(40, 70), 0.6f, 0.0002f, 0.0009f, 2f, u => Dsp.Bell(u, 1f, 0.25f), 1200f, 9000f);
                Pings(x, g, ball, ball + g.Range(0.14f, 0.22f), g.Int(18, 30), 0.4f, 1500f, 6000f, 0.0015f, 0.005f, 1.8f, u => Dsp.Bell(u, 1f, 0.25f));
                Crunch(x, g, ball, ball + 0.18f, g.Int(30, 50), 0.45f, 0.0002f, 0.0009f, 2f, u => Dsp.Bell(u, 1f, 0.3f), 1200f, 8000f);
                Thud(x, g, ball, 0.22f, 120f, 65f, 0.024f, 0.004f);
                return Done(x, g);
            }

            /// <summary>Boots on bare earth: a dull, padded thud, the soil giving a little, a few grains of grit.</summary>
            internal static float[] Dirt(SynthGen g)
            {
                float[] x = g.Buffer(0.32f);
                float ball = g.Range(0.06f, 0.1f);
                Thud(x, g, 0.001f, 0.45f, g.Vary(170f, 0.12f), 85f, 0.024f, 0.003f);
                Hit(x, g, 0f, 0.8f, g.Range(0.006f, 0.012f), 180f, 1700f, 0.0008f);
                Hit(x, g, 0f, 0.25f, 0.003f, 1500f, 5000f);
                Crunch(x, g, 0.002f, ball + 0.08f, g.Int(10, 22), 0.25f, 0.0003f, 0.001f, 2.5f, u => 1f - u, 1800f, 7000f);
                Thud(x, g, ball, g.Range(0.25f, 0.38f), 150f, 85f, 0.02f, 0.003f);
                Hit(x, g, ball, 0.5f, 0.008f, 180f, 1500f, 0.001f);
                if (g.Chance(0.5f))
                    Scrape(x, g, ball + g.Range(0.05f, 0.1f), g.Range(0.05f, 0.09f), 0.12f, 1800f, 0.7f, 40f, u => Dsp.Bell(u, 1f, 0.3f));
                return Done(x, g);
            }

            /// <summary>Boots in mud: a wet slap as the sole sinks, the mud squelching round it, then the suck and pop as the
            /// foot pulls free.</summary>
            internal static float[] Mud(SynthGen g)
            {
                float[] x = g.Buffer(0.5f);
                Thud(x, g, 0.002f, 0.8f, g.Vary(100f, 0.1f), 52f, 0.04f, 0.004f);
                Knock(x, g, 0f, 0.6f, 0.012f, g.Range(500f, 800f), 0.9f);
                Squish(x, g, 0.005f, g.Range(0.1f, 0.16f), 0.8f, g.Range(800f, 1300f));
                float pull = g.Range(0.2f, 0.3f);
                Squish(x, g, pull - 0.05f, 0.09f, 0.35f, 1500f);
                float[] pop = Dsp.Bubble(g.Rate, g.Range(110f, 180f), g.Range(0.04f, 0.07f), g.Range(1.5f, 2.6f));
                Dsp.Mix(x, pop, At(g, pull), g.Range(0.35f, 0.55f));
                Drops(x, g, 0.01f, pull + 0.1f, g.Int(2, 5), 0.18f, 400f, 1100f);
                return Done(x, g, 35f);
            }

            /// <summary>Boots in leaf litter: dry leaves crackling and crumpling in a long, crisp rustle; now and then a twig snaps.</summary>
            internal static float[] Leaves(SynthGen g)
            {
                float[] x = g.Buffer(0.46f);
                float ball = g.Range(0.07f, 0.12f);
                Thud(x, g, 0.003f, 0.4f, g.Vary(95f, 0.1f), 52f, 0.03f, 0.005f);
                Crunch(x, g, 0f, ball + 0.2f, g.Int(110, 170), 1f, 0.0002f, 0.0012f, 2.4f, u => Dsp.Bell(u, 1f, 0.25f), 700f, 10000f);
                Crunch(x, g, 0f, ball + 0.12f, g.Int(15, 30), 0.5f, 0.002f, 0.006f, 1.5f, u => Dsp.Bell(u, 1f, 0.3f), 400f, 1800f);
                Scrape(x, g, 0.01f, g.Range(0.18f, 0.28f), 0.3f, g.Range(2200f, 3400f), 0.5f, 90f, u => Dsp.Bell(u, 1f, 0.3f));
                if (g.Chance(0.3f))
                {
                    float t = g.Range(0.02f, ball + 0.05f);
                    Hit(x, g, t, 0.7f, 0.0015f, 900f, 8000f);
                    Dsp.Ring(x, g.Rate, t, 0.25f, g.Range(1100f, 2100f), new[] { 1f, 2.3f, 3.9f }, 0.012f);
                }
                return Done(x, g);
            }

            /// <summary>A foot coming down in water (a puddle, a ditch, the shallows): the slap on the surface, a sheet of
            /// spray, the water shoved aside and droplets raining back; lifting out, the boot pours a little.</summary>
            internal static float[] Water(SynthGen g)
            {
                float[] x = g.Buffer(0.55f);
                Thud(x, g, 0.003f, 0.35f, g.Vary(120f, 0.1f), 65f, 0.03f, 0.004f);
                Splash(x, g, 0f, 1f, g.Range(0.7f, 1f));
                Drops(x, g, 0.18f, 0.45f, g.Int(4, 9), 0.2f, 700f, 2400f);
                Hit(x, g, g.Range(0.2f, 0.28f), 0.2f, 0.05f, 400f, 3000f, 0.02f);
                return Done(x, g, 40f);
            }
            #endregion

            #region The body over the feet
            /// <summary>Kit jostling on a running body: buckles and tags jingling, straps and nylon rustling, now and then a
            /// canteen sloshing, the pack thumping on the back.</summary>
            internal static float[] Gear(SynthGen g)
            {
                float[] x = g.Buffer(0.32f);
                Scrape(x, g, 0f, g.Range(0.1f, 0.18f), 0.55f, g.Range(1800f, 2800f), 0.7f, 70f, u => Dsp.Bell(u, 1f, 0.3f));
                // Buckles and tags: little clinks, each a pair of close inharmonic partials.
                int clinks = g.Int(3, 7);
                for (int c = 0; c < clinks; c++)
                {
                    float t = g.Range(0.005f, 0.12f), f = g.Range(2400f, 5200f), a = g.Range(0.2f, 0.5f) * (1f - 2.5f * Math.Max(0f, t - 0.06f));
                    Dsp.Modes(x, g.Rate, t, a, new[] { f, f * g.Range(1.35f, 1.6f), f * g.Range(2.4f, 2.9f) }, new[] { 1f, 0.6f, 0.3f },
                        new[] { g.Range(0.008f, 0.02f), 0.007f, 0.004f });
                    Hit(x, g, t, a * 0.5f, 0.0008f, 3000f, 12000f);
                }
                if (g.Chance(0.5f))
                {
                    float[] slosh = Dsp.Bubble(g.Rate, g.Range(260f, 420f), g.Range(0.05f, 0.09f), g.Range(0.5f, 1.2f));
                    Dsp.Mix(x, slosh, At(g, g.Range(0.02f, 0.08f)), 0.25f);
                    Squish(x, g, 0.03f, 0.08f, 0.2f, 900f);
                }
                Thud(x, g, 0f, 0.25f, 140f, 80f, 0.02f, 0.004f);
                return Done(x, g, 60f);
            }

            /// <summary>Landing from a drop: both feet slam down a hair apart, the body's weight thumping through the legs and
            /// the kit jolting on the back.</summary>
            internal static float[] Land(SynthGen g)
            {
                float[] x = g.Buffer(0.55f);
                float second = g.Range(0.02f, 0.05f);
                Thud(x, g, 0f, 1f, g.Vary(95f, 0.1f), 42f, 0.07f, 0.002f);
                Hit(x, g, 0f, 0.7f, 0.018f, 80f, 2500f, 0.0008f);
                Hit(x, g, 0f, 0.4f, 0.003f, 900f, 7000f);
                Thud(x, g, second, 0.7f, 90f, 42f, 0.06f, 0.002f);
                Hit(x, g, second, 0.45f, 0.015f, 80f, 2200f, 0.0008f);
                Pings(x, g, 0.02f, 0.2f, g.Int(4, 8), 0.3f, 2600f, 6500f, 0.012f, 0.04f, 1.5f, u => 1f - u);
                Scrape(x, g, 0.01f, 0.22f, 0.35f, 2000f, 0.6f, 60f, u => Dsp.Bell(u, 1f, 0.25f));
                Punch(x, g.Rate, -14f, 3f);
                return Done(x, g, 30f);
            }
            #endregion

            #region The dead
            /// <summary>A shambler's step: the foot scuffed forward along the ground, then planted flat and heavy — no heel
            /// strike, a dull slap of rotten soles or bare skin — sometimes wet, sometimes with the toes dragging after.</summary>
            internal static float[] Shuffle(SynthGen g)
            {
                float[] x = g.Buffer(0.46f);
                float plant = g.Range(0.05f, 0.11f);
                Scrape(x, g, 0f, plant + 0.02f, g.Range(0.45f, 0.65f), g.Range(1100f, 2000f), 0.6f, 50f, u => Dsp.Bell(u, 1f, 0.75f));
                Thud(x, g, plant, 0.6f, g.Vary(130f, 0.12f), 62f, 0.035f, 0.004f);
                Hit(x, g, plant, 0.85f, g.Range(0.006f, 0.012f), 180f, 2400f, 0.001f);
                if (g.Chance(0.35f))
                {
                    Knock(x, g, plant + 0.004f, 0.3f, 0.01f, g.Range(450f, 750f), 2f);
                    Drops(x, g, plant + 0.01f, plant + 0.06f, g.Int(1, 3), 0.15f, 220f, 480f);
                }
                if (g.Chance(0.45f))
                    Scrape(x, g, plant + g.Range(0.03f, 0.07f), g.Range(0.08f, 0.16f), g.Range(0.18f, 0.3f), g.Range(900f, 1600f), 0.6f, 40f, u => Dsp.Bell(u, 1f, 0.3f));
                return Done(x, g);
            }

            /// <summary>A dead foot dragged along the ground: a long, uneven scrape of sole, cloth and skin, grit rolling under
            /// it, slowing to a stop as the foot is finally set down.</summary>
            internal static float[] Drag(SynthGen g)
            {
                float len = g.Range(0.35f, 0.65f);
                float[] x = g.Buffer(len + 0.15f);
                float wob = g.Range(7f, 13f), wph = g.Next() * Dsp.TwoPi;
                Scrape(x, g, 0f, len, 1f, g.Range(900f, 1600f), 0.55f, g.Range(25f, 45f), u => Dsp.Bell(u, 1f, 0.35f) * (0.75f + 0.25f * MathF.Sin(u * wob + wph)));
                Scrape(x, g, 0f, len, 0.45f, g.Range(320f, 520f), 0.7f, 20f, u => Dsp.Bell(u, 1f, 0.4f));
                Crunch(x, g, 0f, len, g.Int(15, 30), 0.25f, 0.0003f, 0.001f, 2.5f, u => Dsp.Bell(u, 1f, 0.4f), 2500f, 8000f);
                Thud(x, g, len - 0.02f, 0.4f, 100f, 55f, 0.03f, 0.005f);
                Hit(x, g, len - 0.02f, 0.25f, 0.008f, 150f, 1800f, 0.001f);
                return Done(x, g);
            }

            /// <summary>A fast shambler's stumbling step: a heavy plant that half misses, a scuff, and the other foot catching
            /// the fall a beat later.</summary>
            internal static float[] Lurch(SynthGen g)
            {
                float[] x = g.Buffer(0.42f);
                float second = g.Range(0.07f, 0.14f), k = g.Range(0.55f, 0.9f);
                Thud(x, g, 0f, 0.7f, g.Vary(135f, 0.1f), 62f, 0.032f, 0.003f);
                Hit(x, g, 0f, 0.85f, g.Range(0.005f, 0.01f), 200f, 3000f, 0.0008f);
                Scrape(x, g, 0.015f, second, 0.35f, g.Range(1200f, 2200f), 0.6f, 45f, u => Dsp.Bell(u, 1f, 0.5f));
                Thud(x, g, second, k * 0.65f, g.Vary(120f, 0.1f), 62f, 0.028f, 0.003f);
                Hit(x, g, second, k * 0.75f, g.Range(0.005f, 0.01f), 200f, 2600f, 0.0008f);
                if (g.Chance(0.4f))
                    Scrape(x, g, second + 0.03f, g.Range(0.06f, 0.12f), 0.25f, 1400f, 0.6f, 40f, u => Dsp.Bell(u, 1f, 0.3f));
                return Done(x, g);
            }

            /// <summary>A sprinting corpse's footfall: a heavy, slapping impact at full tilt — the weight of a body thrown
            /// forward — grit spraying, now and then a skid.</summary>
            internal static float[] Run(SynthGen g)
            {
                float[] x = g.Buffer(0.34f);
                Thud(x, g, 0f, 0.75f, g.Vary(160f, 0.12f), 62f, 0.04f, 0.0015f);
                Hit(x, g, 0f, 1f, g.Range(0.004f, 0.007f), 350f, 4500f);
                Hit(x, g, 0.0005f, 0.7f, 0.015f, 120f, 1500f, 0.001f);
                Crunch(x, g, 0.003f, 0.07f, g.Int(10, 20), 0.3f, 0.0002f, 0.0008f, 2.5f, u => 1f - u, 2500f, 10000f);
                if (g.Chance(0.35f))
                    Scrape(x, g, 0.02f, g.Range(0.06f, 0.12f), 0.3f, g.Range(1500f, 2600f), 0.6f, 50f, u => Dsp.Bell(u, 1f, 0.25f));
                Tame(x, 2.5f);
                return Done(x, g);
            }

            /// <summary>A crawler hauling itself along: a palm slapped flat on the ground, then the body dragged up after it.</summary>
            internal static float[] Crawl(SynthGen g)
            {
                float[] x = g.Buffer(0.62f);
                Knock(x, g, 0f, 0.9f, g.Range(0.006f, 0.01f), g.Range(900f, 1500f), 0.8f);
                Thud(x, g, 0f, 0.5f, g.Vary(150f, 0.1f), 80f, 0.014f, 0.0015f);
                float pull = g.Range(0.05f, 0.1f), len = g.Range(0.3f, 0.45f);
                Scrape(x, g, pull, len, 0.75f, g.Range(500f, 900f), 0.5f, 22f, u => Dsp.Bell(u, 1f, 0.35f));
                Scrape(x, g, pull, len, 0.35f, g.Range(1800f, 2800f), 0.7f, 35f, u => Dsp.Bell(u, 1f, 0.3f));
                if (g.Chance(0.5f)) Thud(x, g, pull + g.Range(0.12f, 0.25f), 0.3f, 110f, 60f, 0.02f, 0.004f);
                Tame(x, 2f);
                return Done(x, g);
            }
            #endregion

            #region Beasts
            /// <summary>A wolf's or a hound's paws: two pads (a diagonal pair) landing light and soft; on hard ground the
            /// claws tick and now and then scratch.</summary>
            internal static float[] Paw(SynthGen g, bool hard)
            {
                float[] x = g.Buffer(0.22f);
                float second = g.Range(0.012f, 0.045f);
                for (int p = 0; p < 2; p++)
                {
                    float t = p == 0 ? 0f : second, k = p == 0 ? 1f : g.Range(0.55f, 0.85f);
                    Thud(x, g, t, k * (hard ? 0.4f : 0.55f), g.Vary(190f, 0.12f), 105f, 0.011f, 0.0015f);
                    Hit(x, g, t, k * 0.6f, g.Range(0.004f, 0.007f), 150f, hard ? 2500f : 1800f, 0.0008f);
                    if (hard) Pings(x, g, t + 0.002f, t + 0.03f, g.Int(2, 5), k * 0.7f, 3000f, 7500f, 0.0012f, 0.003f, 1.2f, null);
                }
                if (hard)
                {
                    if (g.Chance(0.35f)) Scrape(x, g, second + 0.01f, g.Range(0.02f, 0.04f), 0.25f, 5000f, 1f, 80f, u => Dsp.Bell(u, 1f, 0.3f));
                    Tame(x, 2f);
                }
                else Crunch(x, g, 0f, second + 0.06f, g.Int(8, 18), 0.35f, 0.0002f, 0.0008f, 2.5f, u => 1f - u, 1500f, 6500f);
                return Done(x, g, 50f);
            }

            /// <summary>A deer's hooves (a diagonal pair): on soft ground a dull double thud with the earth giving; on asphalt
            /// or boards a hollow, clacking clip-clop.</summary>
            internal static float[] Hoof(SynthGen g, bool hard)
            {
                float[] x = g.Buffer(0.24f);
                float second = g.Range(0.03f, 0.07f);
                float[] hz = { g.Range(650f, 900f), g.Range(1500f, 2000f), g.Range(2700f, 3400f) };
                float[] amp = { 1f, 0.6f, 0.3f }, tau = { 0.012f, 0.007f, 0.004f };
                for (int p = 0; p < 2; p++)
                {
                    float t = p == 0 ? 0f : second, k = p == 0 ? 1f : g.Range(0.6f, 0.9f);
                    if (hard)
                    {
                        Hit(x, g, t, k * 0.8f, g.Range(0.0012f, 0.002f), 800f, 7000f);
                        Dsp.Modes(x, g.Rate, t, k * 0.75f, hz, amp, tau);
                        Hit(x, g, t + 0.0003f, k * 0.35f, 0.006f, 300f, 2500f, 0.0004f);
                        Thud(x, g, t, k * 0.25f, 190f, 110f, 0.01f, 0.001f);
                    }
                    else
                    {
                        Thud(x, g, t, k * 0.6f, g.Vary(170f, 0.1f), 95f, 0.015f, 0.0015f);
                        Knock(x, g, t, k * 0.6f, 0.006f, g.Range(550f, 850f), 1.2f);
                        Crunch(x, g, t, t + 0.05f, g.Int(6, 12), k * 0.3f, 0.0002f, 0.0008f, 2.5f, u => 1f - u, 1500f, 6000f);
                    }
                }
                if (hard) Tame(x, 2.5f);
                return Done(x, g, 50f);
            }

            /// <summary>A bear's weight on its pads: a deep, padded thump (front, then hind), fur and debris brushing,
            /// sometimes a twig cracking under it.</summary>
            internal static float[] Heavy(SynthGen g)
            {
                float[] x = g.Buffer(0.46f);
                float hind = g.Range(0.08f, 0.16f);
                Thud(x, g, 0f, 0.85f, g.Vary(95f, 0.1f), 46f, 0.05f, 0.004f);
                Hit(x, g, 0f, 0.6f, 0.02f, 60f, 1400f, 0.002f);
                Thud(x, g, hind, 0.65f, g.Vary(90f, 0.1f), 46f, 0.045f, 0.004f);
                Hit(x, g, hind, 0.5f, 0.018f, 60f, 1300f, 0.002f);
                Crunch(x, g, 0f, hind + 0.15f, g.Int(20, 40), 0.3f, 0.0004f, 0.0015f, 2.2f, u => 1f - 0.7f * u, 900f, 4000f);
                Scrape(x, g, 0.01f, hind + 0.1f, 0.35f, 1400f, 0.5f, 30f, u => Dsp.Bell(u, 1f, 0.4f));
                if (g.Chance(0.25f))
                {
                    float t = g.Range(0.01f, hind);
                    Hit(x, g, t, 0.5f, 0.0015f, 900f, 7000f);
                    Dsp.Ring(x, g.Rate, t, 0.2f, g.Range(900f, 1700f), new[] { 1f, 2.4f, 4.1f }, 0.012f);
                }
                Punch(x, g.Rate, -12f, 2.5f);
                return Done(x, g, 28f);
            }

            /// <summary>A Thumper's stomp: a hoof the size of a manhole cover driven into the ground — a sub-bass blow felt
            /// as much as heard, the crack of the ground taking it, earth and grit showering down, a low rumble rolling off.</summary>
            internal static float[] Stomp(SynthGen g)
            {
                float[] x = g.Buffer(0.95f);
                Thud(x, g, 0f, 1f, g.Vary(60f, 0.08f), 30f, 0.14f, 0.004f);
                Thud(x, g, 0f, 0.5f, g.Vary(125f, 0.08f), 62f, 0.045f, 0.002f);
                Hit(x, g, 0f, 0.8f, g.Range(0.012f, 0.02f), 70f, 3000f, 0.0008f);
                Knock(x, g, 0.001f, 0.5f, 0.015f, g.Range(380f, 560f), 1.5f);
                Crunch(x, g, 0.03f, 0.55f, g.Int(60, 100), 0.35f, 0.0005f, 0.002f, 2.2f, u => MathF.Exp(-3f * u), 250f, 4500f);
                float[] rumble = Dsp.Brown(g, x.Length);
                Dsp.LP(rumble, 170f, g.Rate);
                Dsp.Shape(rumble, g.Rate, t => Dsp.AD(t, 0.012f, 0.22f));
                Dsp.Norm(rumble);
                Dsp.Mix(x, rumble, 0, 0.45f);
                Punch(x, g.Rate, -12f, 3f);
                return Done(x, g, 24f);
            }

            /// <summary>A Shade's step: almost nothing — a whisper of air drawn in ahead of it, the lightest touch, then a
            /// breath let out that isn't quite a breath, with a faint glassy shimmer in it. Wrong, if you hear it at all.</summary>
            internal static float[] ShadeTouch(SynthGen g)
            {
                float[] x = g.Buffer(0.5f);
                float swell = g.Range(0.07f, 0.11f);
                int n = g.N(swell);
                float[] air = Dsp.White(g, n);
                Dsp.BP(air, g.Range(3500f, 5500f), 1.1f, g.Rate);
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)n;
                    air[i] *= u * u * u;
                }
                Dsp.FadeOut(air, Math.Min(n / 6, g.N(0.004f)));
                Dsp.Norm(air);
                Dsp.Mix(x, air, 0, 0.45f);
                Hit(x, g, swell, 0.6f, 0.005f, 120f, 1300f, 0.001f);
                Thud(x, g, swell, 0.3f, 160f, 100f, 0.01f, 0.002f);
                // The exhalation: a whispered vowel sliding from "oo" toward "ee", pitched oddly high.
                float len = g.Range(0.2f, 0.3f), k = g.Range(1.15f, 1.4f);
                float[] breath = Dsp.Voice(g, len, new VoiceParams
                {
                    Whisper = true,
                    Breath = 1f,
                    Formants = u => Vowel.Lerp(Vowel.U, Vowel.E, u).Scale(k, 1.4f),
                    Amp = u => Dsp.Bell(u, 1f, 0.2f),
                });
                Dsp.Norm(breath);
                Dsp.Mix(x, breath, At(g, swell + 0.01f), 0.3f);
                float f = g.Range(1900f, 2900f);
                Dsp.Modes(x, g.Rate, swell, 0.05f, new[] { f, f * 1.012f, f * 2.73f }, new[] { 1f, 0.9f, 0.3f }, new[] { 0.12f, 0.13f, 0.06f });
                return Done(x, g, 60f);
            }

            /// <summary>A Puppeteer shuffling forward: a long, slow drag of a twisted foot — cloth, skin and something wet —
            /// the joints creaking, then a soft, fleshy plant.</summary>
            internal static float[] PuppetDrag(SynthGen g)
            {
                float len = g.Range(0.5f, 0.8f);
                float[] x = g.Buffer(len + 0.25f);
                // The foot peeling up off the ground (wet), then dragged.
                Knock(x, g, 0.003f, 0.35f, 0.008f, g.Range(350f, 600f), 1.4f);
                Squish(x, g, 0f, g.Range(0.05f, 0.08f), 0.3f, 900f);
                Scrape(x, g, 0f, len, 0.8f, g.Range(600f, 1100f), 0.5f, 18f, u => Dsp.Bell(u, 1f, 0.45f));
                Scrape(x, g, 0f, len, 0.4f, g.Range(1800f, 2600f), 0.8f, 30f, u => Dsp.Bell(u, 1f, 0.4f));
                Squish(x, g, len * 0.3f, len * 0.5f, 0.35f, 700f);
                if (g.Chance(0.5f))
                {
                    float r0 = g.Range(40f, 70f);
                    Creak(x, g, g.Range(0.05f, len * 0.5f), g.Range(0.12f, 0.22f), 0.25f, r0, r0 * g.Range(0.5f, 1.3f),
                        new[] { g.Range(300f, 420f), g.Range(650f, 900f) }, new[] { 7f, 9f });
                }
                float plant = len - 0.03f;
                Thud(x, g, plant, 0.9f, g.Vary(95f, 0.1f), 48f, 0.045f, 0.005f);
                Knock(x, g, plant, 0.4f, 0.014f, g.Range(450f, 700f), 1.5f);
                Drops(x, g, plant + 0.01f, plant + 0.12f, g.Int(1, 4), 0.2f, 180f, 420f);
                return Done(x, g);
            }
            #endregion
        }
    }

    public static partial class SfxIds
    {
        // Footsteps by surface (step_snow, step_wood, step_concrete and step_grass are with the original ids).
        public const string StepGravel = "step_gravel", StepDirt = "step_dirt", StepMud = "step_mud", StepMetal = "step_metal",
            StepCarpet = "step_carpet", StepTile = "step_tile", StepLeaves = "step_leaves", StepWater = "step_water",
            StepConcreteWet = "step_concrete_wet", StepWoodWet = "step_wood_wet", StepMetalWet = "step_metal_wet", StepSlush = "step_slush",
            StepGear = "step_gear", StepLand = "step_land";

        // The dead and the beasts.
        public const string StepZombieShuffle = "zstep_shuffle", StepZombieDrag = "zstep_drag", StepZombieLurch = "zstep_lurch",
            StepZombieRun = "zstep_run", StepCrawl = "zcrawl",
            StepPaw = "paw_step", StepPawClaw = "paw_claw", StepHoof = "hoof_step", StepHoofHard = "hoof_hard",
            StepHeavy = "heavy_step", StepStomp = "stomp_step", StepShade = "shade_step", StepPuppet = "puppet_drag";
    }
}
