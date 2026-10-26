using System;

namespace CloneZ
{
    // The fight: guns, their brass and their echoes; bullets going past and what they hit; blades and blows; grenades
    // and blasts; and the ringing they leave in the ears. Transients at 44.1 kHz; the distant versions and the tinnitus,
    // which hold nothing above 16 kHz, at 32 kHz.
    //
    // A gunshot is four layers. The crack: the bullet's shock (an N-wave a fraction of a millisecond long) and the hiss
    // of the shocked air. The muzzle blast: a Friedlander pressure wave, its noisy body and the pitched thump of the
    // charge, saturated and compressed so it hits hard and holds. The mechanism: slide, bolt carrier, pump or bolt, quiet
    // under the blast. And the place: smeared reflections at irregular delays (the ground, walls, trees), darker the later
    // they come, a few far echoes and a long low roll — never one fixed echo, which combs a tail into a flanged ring.
    // Every gun is the same recipe with its own numbers.
    public sealed partial class SfxSynth
    {
        partial void RegisterWeapons()
        {
            // ── Guns ────────────────────────────────────────────────────────────────────────────────
            // Near shots hand over to a distant version beyond 55–80 m, which carries 450–650 m: a firefight across the
            // valley (and the A-Life's battles out of sight) is a dull boom and a long roll off the hills. The spent brass
            // lands a moment later; a shot close by dips the ambience and music a little.
            Add(SfxIds.GunPistol, g => WeaponKit.Shot(g, WeaponKit.Pistol()), 4)
                .Peak(0.95f).Far(SfxIds.GunFarLight, 55f).Then(SfxIds.ShellCasing, 0.5f, 0.12f, 0.22f)
                .Duck(-3f, 25f, 0.3f, 1.5f).Reverb(0.4f).Priority(30);
            Add(SfxIds.GunRifle, g => WeaponKit.Shot(g, WeaponKit.Rifle()), 4)
                .Peak(0.95f).Far(SfxIds.GunFarHeavy, 65f).Then(SfxIds.ShellCasing, 0.6f, 0.15f, 0.22f)
                .Duck(-4f, 25f, 0.35f, 1.8f).Reverb(0.4f).Priority(30);
            Add(SfxIds.GunShotgun, g => WeaponKit.Shot(g, WeaponKit.Shotgun()), 4)
                .Peak(0.95f).Far(SfxIds.GunFarHeavy, 65f).Then(SfxIds.ShellShotgun, 0.95f, 0.1f, 0.35f)
                .Duck(-4.5f, 25f, 0.35f, 1.8f).Reverb(0.4f).Priority(30);
            Add(SfxIds.GunSniper, g => WeaponKit.Shot(g, WeaponKit.Sniper()), 4)
                .Peak(0.95f).Far(SfxIds.GunFarHeavy, 80f).Then(SfxIds.ShellCasing, 1.12f, 0.1f, 0.25f)
                .Duck(-5f, 30f, 0.45f, 2f).Reverb(0.35f).Priority(28);
            Add(SfxIds.GunSuppressed, g => WeaponKit.Suppressed(g, false), 4)
                .Peak(0.9f).Then(SfxIds.ShellCasing, 0.5f, 0.12f, 0.3f).Reverb(0.7f).Priority(40);
            Add(SfxIds.GunSuppressedRifle, g => WeaponKit.Suppressed(g, true), 4)
                .Peak(0.9f).Range(55f).Then(SfxIds.ShellCasing, 0.6f, 0.15f, 0.3f).Reverb(0.6f).Priority(36);
            Add(SfxIds.GunFlare, g => WeaponKit.Flare(g), 2).Peak(0.75f).Duck(-2f, 20f).Reverb(0.6f).Priority(40);
            // The distant versions' peaks are set so they meet the near shots at the same loudness where they take over.
            Add(SfxIds.GunFarLight, g => WeaponKit.FarLight(g), 3, BedRate).Peak(0.27f).Range(450f).Dry().Priority(70);
            Add(SfxIds.GunFarHeavy, g => WeaponKit.FarHeavy(g), 4, BedRate).Peak(0.4f).Range(650f).Dry().Priority(64);
            Add(SfxIds.ShellCasing, g => WeaponKit.Casing(g), 6).Peak(0.6f).Priority(200);
            Add(SfxIds.ShellShotgun, g => WeaponKit.Hull(g), 4).Peak(0.6f).Priority(200);

            // ── Handling ────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.DryFire, g => WeaponKit.DryFire(g), 3).Priority(90);
            Add(SfxIds.Jam, g => WeaponKit.Jam(g), 2).Priority(90);
            Add(SfxIds.Reload, g => WeaponKit.Reload(g, WeaponKit.Reloads.Generic), 2).Priority(100);
            Add(SfxIds.ReloadPistol, g => WeaponKit.Reload(g, WeaponKit.Reloads.Pistol), 2).Priority(100);
            Add(SfxIds.ReloadRifle, g => WeaponKit.Reload(g, WeaponKit.Reloads.Rifle), 2).Priority(100);
            Add(SfxIds.ReloadShotgun, g => WeaponKit.Reload(g, WeaponKit.Reloads.Shotgun), 2).Priority(100);
            Add(SfxIds.ReloadBolt, g => WeaponKit.Reload(g, WeaponKit.Reloads.Bolt), 2).Priority(100);
            Add(SfxIds.BoltThrow, g => WeaponKit.BoltThrow(g), 3).Peak(0.85f).Priority(140);
            Add(SfxIds.BoltLand, g => WeaponKit.BoltLand(g), 4).Peak(0.9f).Priority(140);
            Add(SfxIds.Grenade, g => WeaponKit.GrenadePin(g), 2).Peak(0.75f).Priority(90);

            // ── Bullets: going past, and what they hit ──────────────────────────────────────────────
            Add(SfxIds.BulletWhiz, g => WeaponKit.Whiz(g), 4).Peak(0.9f).Reverb(0.15f).Priority(36);
            Add(SfxIds.BulletFlyby, g => WeaponKit.Flyby(g), 4).Peak(0.6f).Reverb(0.15f).Priority(40);
            Add(SfxIds.HitFlesh, g => WeaponKit.HitFlesh(g), 5).Peak(0.95f).Priority(80);
            Add(SfxIds.HitArmor, g => WeaponKit.HitArmor(g), 4).Peak(0.85f).Priority(80);
            Add(SfxIds.Ricochet, g => WeaponKit.Ricochet(g), 4).Priority(110);
            Add(SfxIds.ImpactDirt, g => WeaponKit.ImpactDirt(g), 4).Priority(150);
            Add(SfxIds.ImpactConcrete, g => WeaponKit.ImpactConcrete(g), 4).Priority(150);
            Add(SfxIds.ImpactMetal, g => WeaponKit.ImpactMetal(g), 4).Peak(0.65f).Priority(150);
            Add(SfxIds.ImpactWood, g => WeaponKit.ImpactWood(g), 4).Priority(150);
            Add(SfxIds.ImpactGlass, g => WeaponKit.ImpactGlass(g), 3).Priority(150);

            // ── Melee ───────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.MeleeSwing, g => WeaponKit.Swing(g), 4).Peak(0.75f).Priority(100);
            Add(SfxIds.MeleeHit, g => WeaponKit.Blow(g), 4).Peak(0.9f).Priority(70);

            // ── Explosions ──────────────────────────────────────────────────────────────────────────
            // Close by, a blast deafens (the mix is muffled and the ears ring) and buries the ambience; dirt and grit
            // rain down after it. Past 110 m it is a deep boom rolling off the hills for seconds, heard almost a kilometre off.
            Add(SfxIds.Explosion, g => WeaponKit.Explosion(g), 3)
                .Peak(0.97f).Range(250f).Far(SfxIds.ExplosionFar, 110f).Then(SfxIds.Debris, 0.35f, 0.08f, 0.5f)
                .Duck(-12f, 90f, 1.2f, 4f).Concuss(12f).Reverb(0.4f).Priority(10);
            Add(SfxIds.ExplosionFar, g => WeaponKit.ExplosionFar(g), 2, BedRate).Peak(0.6f).Range(900f).Dry().Priority(40);
            Add(SfxIds.Debris, g => WeaponKit.Debris(g), 3).Peak(0.7f).Priority(150);

            // ── The ears ────────────────────────────────────────────────────────────────────────────
            Add(SfxIds.EarRing, g => WeaponKit.EarRing(g), 2, BedRate).Peak(0.35f).Inside().Dry().Priority(8);
        }

        private static class WeaponKit
        {
            /// <summary>Where transients start: clear of the 1.5 ms fade-in every one-shot gets.</summary>
            internal const float T0 = 0.003f;

            internal static float Db(float db) => MathF.Pow(10f, db / 20f);

            /// <summary>This variation's own stream, derived from the core's generator and the variation number (so the
            /// variations differ however the core seeds).</summary>
            internal static SynthGen Fresh(SynthGen g)
                => new SynthGen(g.Rate, (g.NextUInt() ^ (0x9E3779B9u * (uint)(g.Variation + 1))) | 1u, g.Variation);

            #region Primitives
            /// <summary>
            /// Filtered white noise under a linear attack, an optional hold and an exponential decay, added from
            /// <paramref name="at"/> seconds. The noise is filtered before the envelope, so the attack stays as sharp as
            /// asked; <paramref name="gain"/> is roughly the burst's peak. lp/hp/bp of 0 are skipped; a steep low-pass
            /// runs twice (24 dB/octave, for things heard far off).
            /// </summary>
            internal static void Noise(float[] x, SynthGen g, float at, float gain, float attack, float tau,
                float lp = 0f, float hp = 0f, float bp = 0f, float q = 1f, float hold = 0f, bool steep = false)
            {
                int rate = g.Rate;
                int start = Math.Max(0, (int)(at * rate));
                if (start >= x.Length || gain <= 0f) return;
                int n = Math.Min(x.Length - start, (int)((attack + hold + tau * 6.5f) * rate) + 2);
                if (n < 2) return;
                var b = new float[n];
                for (int i = 0; i < n; i++) b[i] = g.Bipolar();
                if (hp > 0f) Dsp.HP(b, hp, rate);
                if (lp > 0f) Dsp.LP(b, lp, rate);
                if (lp > 0f && steep) Dsp.LP(b, lp, rate);
                if (bp > 0f) Dsp.BP(b, bp, q, rate);
                float r = Dsp.Rms(b);
                if (r < 1e-9f) return;
                float k = gain * 0.36f / r;
                int a = Math.Max(1, (int)(attack * rate)), h = a + (int)(hold * rate);
                float decay = MathF.Exp(-1f / (Math.Max(1e-5f, tau) * rate)), e = 1f;
                for (int i = 0; i < n; i++)
                {
                    float env;
                    if (i < a) env = i / (float)a;
                    else if (i < h) env = 1f;
                    else { env = e; e *= decay; }
                    x[start + i] += b[i] * k * env;
                }
            }

            /// <summary>White noise through a low- or band-pass following hz(t seconds), its first 50 ms at Noise's level.</summary>
            internal static float[] Swept(SynthGen g, float seconds, float q, Func<float, float> hz, bool band)
            {
                int rate = g.Rate;
                var b = new float[g.N(seconds)];
                for (int i = 0; i < b.Length; i++) b[i] = g.Bipolar();
                if (band) Dsp.SweepBP(b, rate, q, hz);
                else Dsp.SweepLP(b, rate, q, hz);
                int head = Math.Min(b.Length, Math.Max(16, rate / 20));
                double s = 0;
                for (int i = 0; i < head; i++) s += b[i] * b[i];
                float r = (float)Math.Sqrt(s / head);
                if (r > 1e-9f) Dsp.Gain(b, 0.36f / r);
                return b;
            }

            /// <summary>An N-wave: a shock front — a jump up, a straight fall through zero, a jump back.</summary>
            internal static void NWave(float[] x, int rate, float at, float gain, float seconds)
            {
                int start = (int)(at * rate);
                int n = Math.Max(3, (int)(seconds * rate + 0.5f));
                for (int i = 0; i < n; i++)
                {
                    int k = start + i;
                    if (k < 0) continue;
                    if (k >= x.Length) break;
                    x[k] += gain * (1f - 2f * i / (n - 1));
                }
            }

            /// <summary>A Friedlander blast wave: an instant pressure jump falling through zero into a long, shallow
            /// suction (b = 1, so it carries no DC). <paramref name="positive"/> is the positive phase in seconds.</summary>
            internal static void Friedlander(float[] x, int rate, float at, float gain, float positive)
            {
                int start = (int)(at * rate);
                float inv = 1f / Math.Max(1f, positive * rate);
                int n = (int)(positive * rate * 9f) + 1;
                for (int i = 0; i < n; i++)
                {
                    int k = start + i;
                    if (k < 0) continue;
                    if (k >= x.Length) break;
                    float u = i * inv;
                    x[k] += gain * (1f - u) * MathF.Exp(-u);
                }
            }

            /// <summary>A small steel part striking another (a slide, a bolt, a latch): a tick and a short inharmonic ring.</summary>
            internal static void Clack(float[] x, SynthGen g, float at, float gain, float hz, float tau, float tick = 0.6f)
            {
                float[] ratio = { 1f, g.Range(1.42f, 1.66f), g.Range(2.2f, 2.8f), g.Range(3.3f, 4.1f), g.Range(4.9f, 6.3f) };
                var f = new float[5];
                var a = new float[5];
                var t = new float[5];
                for (int m = 0; m < 5; m++)
                {
                    f[m] = hz * ratio[m];
                    a[m] = (1f - 0.17f * m) * g.Range(0.5f, 1f);
                    t[m] = tau * (1f - 0.15f * m);
                }
                Dsp.Modes(x, g.Rate, at, gain * 0.5f, f, a, t);
                Noise(x, g, at, gain * tick, 0.00004f, 0.0005f, 0f, 1800f);
            }

            /// <summary>Metal sliding on metal (a slide, a bolt, a magazine in its well): gritty stick-slip noise through
            /// two narrow resonances under a quick hump.</summary>
            internal static void Scrape(float[] x, SynthGen g, float at, float seconds, float gain, float hz, float q = 2.5f)
            {
                int rate = g.Rate;
                int start = Math.Max(0, (int)(at * rate));
                int n = Math.Min(x.Length - start, (int)(seconds * rate));
                if (n < 16 || gain <= 0f) return;
                var a = new float[n];
                float level = 1f;
                int hold = 0;
                for (int i = 0; i < n; i++)
                {
                    if (--hold <= 0)
                    {
                        hold = g.Int(rate / 3000, rate / 600);
                        level = 0.2f + 0.8f * g.Next() * g.Next();
                    }
                    a[i] = g.Bipolar() * level;
                }
                var b = (float[])a.Clone();
                Dsp.BP(a, hz, q, rate);
                Dsp.BP(b, hz * g.Range(1.55f, 1.85f), q * 1.4f, rate);
                for (int i = 0; i < n; i++) a[i] += 0.6f * b[i];
                float r = Dsp.Rms(a);
                if (r < 1e-9f) return;
                float k = gain * 0.36f / r;
                float len = n / (float)rate;
                for (int i = 0; i < n; i++) x[start + i] += a[i] * k * Dsp.Bell(i / (float)rate, len, 0.4f);
            }

            /// <summary>
            /// Something small and hard dropped on the ground: a strike, then bounces closing up (each gap × e) and fading,
            /// every strike ringing the object's modes with a fresh balance (it lands on a different face each time), with
            /// the ground's own knock under it. Returns the time after the last strike.
            /// </summary>
            internal static float Bounces(float[] x, SynthGen g, float at, float gain, float[] hz, float tau, int strikes,
                float gap, float e, float knock, float knockTau)
            {
                int rate = g.Rate;
                float t = at, a = gain, end = x.Length / (float)rate - 0.03f;
                var f = new float[hz.Length];
                var amp = new float[hz.Length];
                var taus = new float[hz.Length];
                for (int s = 0; s < strikes && t < end; s++)
                {
                    for (int m = 0; m < hz.Length; m++)
                    {
                        f[m] = hz[m] * g.Range(0.995f, 1.005f);
                        amp[m] = g.Range(0.25f, 1f) / (1f + 0.35f * m);
                        taus[m] = tau * g.Range(0.75f, 1.2f) / (1f + 0.3f * m);
                    }
                    Dsp.Modes(x, rate, t, a * 0.55f, f, amp, taus);
                    Noise(x, g, t, a * 0.6f, 0.00003f, 0.0004f, 0f, 2500f);
                    if (knock > 0f) Noise(x, g, t, a * knock, 0.0002f, knockTau, 1100f, 90f);
                    t += gap * g.Range(0.75f, 1.25f);
                    gap *= e;
                    a *= g.Range(0.45f, 0.72f);
                }
                return t;
            }

            /// <summary>The long low decay of the surroundings from <paramref name="at"/> to the end: noise darkening as it
            /// dies (lp0 → lp1 over about one and a half time constants), rolling in slow, uneven swells. A tilt (a
            /// one-pole low-pass at that frequency) leans the noise toward the low end: a boom rather than a hiss.</summary>
            internal static void Roll(float[] x, SynthGen g, float at, float gain, float rise, float tau, float lp0, float lp1,
                float hp, float depth, float hz, float tilt = 0f)
            {
                int rate = g.Rate;
                int start = Math.Max(0, (int)(at * rate));
                int n = x.Length - start;
                if (n <= 16 || gain <= 0f) return;
                var b = new float[n];
                for (int i = 0; i < n; i++) b[i] = g.Bipolar();
                if (tilt > 0f) Dsp.LP1(b, tilt, rate);
                if (hp > 0f) Dsp.HP(b, hp, rate);
                float span = Math.Max(0.05f, tau * 1.5f);
                Dsp.SweepLP(b, rate, 0.707f, t => Dsp.Glide(lp0, lp1, t / span));
                int head = Math.Min(n, rate / 10);
                double s = 0;
                for (int i = 0; i < head; i++) s += b[i] * b[i];
                float r = (float)Math.Sqrt(s / head);
                if (r < 1e-9f) return;
                float k = gain * 0.36f / r;
                float[] w = depth > 0f ? Dsp.Wander(g, n, rate, hz) : null;
                float dec = MathF.Exp(-1f / (tau * rate)), ris = MathF.Exp(-1f / (Math.Max(1e-4f, rise) * rate)), e = 1f, q = 1f;
                for (int i = 0; i < n; i++)
                {
                    float env = (1f - q) * e;
                    q *= ris;
                    e *= dec;
                    if (w != null) env *= 1f - depth + depth * w[i];
                    x[start + i] += b[i] * k * env;
                }
            }

            /// <summary>The place a shot or a blast goes off in: early reflections, far echoes and the long roll.</summary>
            internal sealed class Surround
            {
                public int Early = 8;
                public float EarlyFrom = 0.008f, EarlyTo = 0.16f, EarlyDb = -9f, EarlyFall = 12f;
                public float EarlyLp0 = 6000f, EarlyLp1 = 1400f, EarlyTau = 0.012f, EarlyHp = 150f;
                public int Echoes = 2;
                public float EchoFrom = 0.22f, EchoTo = 0.9f, EchoDb = -22f, EchoLp = 1300f, EchoHp = 110f, EchoTau = 0.06f, EchoAttack = 0.009f;
                public float LateDb = -18f, LateDelay = 0.02f, LateRise = 0.035f, LateTau = 0.35f;
                public float LateLp0 = 3200f, LateLp1 = 450f, LateHp = 80f, LateTilt = 0f, Roll = 0.4f, RollHz = 4f;
            }

            /// <summary>
            /// Adds the space around a sound that started at <paramref name="t0"/>: smeared reflections at irregular
            /// delays (more of them early, each darker and longer the later it comes), a few distinct far echoes spread
            /// over their window, and the long roll. Every reflection is fresh noise, so nothing lines up into a comb.
            /// Levels are relative to a direct sound peaking at 1.
            /// </summary>
            internal static void Surroundings(float[] x, SynthGen g, float t0, Surround r)
            {
                for (int i = 0; i < r.Early; i++)
                {
                    float u = MathF.Pow(g.Next(), 1.4f);
                    float at = t0 + r.EarlyFrom + (r.EarlyTo - r.EarlyFrom) * u;
                    float db = r.EarlyDb - r.EarlyFall * u + g.Range(-3f, 2f);
                    Noise(x, g, at, Db(db), g.Range(0.0003f, 0.0012f) + 0.004f * u, r.EarlyTau * g.Range(0.7f, 1.3f) * (1f + u),
                        Dsp.Glide(r.EarlyLp0, r.EarlyLp1, u) * g.Range(0.85f, 1.15f), r.EarlyHp);
                }
                for (int i = 0; i < r.Echoes; i++)
                {
                    float u = (i + g.Range(0.15f, 0.85f)) / r.Echoes;
                    float at = t0 + r.EchoFrom + (r.EchoTo - r.EchoFrom) * u;
                    Noise(x, g, at, Db(r.EchoDb - 4f * u + g.Range(-2.5f, 2.5f)), r.EchoAttack * g.Range(0.55f, 1.55f), r.EchoTau * g.Range(0.8f, 1.3f),
                        r.EchoLp * g.Range(0.8f, 1.15f), r.EchoHp, steep: true);
                }
                Roll(x, g, t0 + r.LateDelay, Db(r.LateDb + g.Range(-1.5f, 1.5f)), r.LateRise, r.LateTau * g.Range(0.88f, 1.12f),
                    r.LateLp0, r.LateLp1, r.LateHp, r.Roll, r.RollHz, r.LateTilt);
            }
            #endregion

            #region Gunshots
            /// <summary>One gun's numbers for <see cref="Shot"/>. Levels are relative to the crack (1).</summary>
            internal sealed class ShotSpec
            {
                public float Length = 1.6f;
                // The crack: an N-wave of NWaveMs, the hiss of shocked air, and (heavy rounds) a few ms of tearing.
                public float Crack = 1f, NWaveMs = 0.35f, Sizzle = 0.4f, SizzleTau = 0.001f, SizzleHp = 2000f;
                public int Tear;
                // The blast: a Friedlander wave of BlastMs positive phase, a bright burst, a band-passed body and a low
                // boom of noise, saturated by Drive and compressed, then mixed at BlastLevel.
                public float BlastLevel = 0.75f, BlastMs = 0.7f;
                public float Bright = 0.6f, BrightTau = 0.005f, BrightLp = 8000f, BrightHp = 300f;
                public float Body = 0.8f, BodyTau = 0.012f, BodyHz = 800f, BodyQ = 0.8f;
                public float Low = 0.6f, LowTau = 0.03f, LowLp = 450f;
                public float Drive = 2.2f, Threshold = -14f, Ratio = 3f, Release = 30f;
                // The pitched thump of the charge, gliding down.
                public float Thump = 0.4f, ThumpHz0 = 160f, ThumpHz1 = 70f, ThumpGlide = 0.015f, ThumpTau = 0.035f;
                public Action<float[], SynthGen, float> Mechanism;
                public Surround Place = new Surround();
            }

            /// <summary>A gunshot heard from close by: crack, muzzle blast, mechanism and the space around it.</summary>
            internal static float[] Shot(SynthGen g, ShotSpec s)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float size = g.Range(0.94f, 1.06f);   // a hotter or milder load, a longer or shorter barrel
                float[] x = g.Buffer(s.Length);

                // The crack: the bullet's shock and the hiss of the shocked air, left sharp (no saturation).
                NWave(x, rate, T0, s.Crack * g.Range(0.85f, 1.05f), s.NWaveMs * 0.001f * size);
                Noise(x, g, T0, s.Sizzle * g.Range(0.8f, 1.15f), 0.00004f, s.SizzleTau * size, 16000f, s.SizzleHp);
                for (int i = 0; i < s.Tear; i++)
                    NWave(x, rate, T0 + 0.0006f + g.Range(0.0004f, 0.0012f) * (i + 1), s.Crack * g.Range(0.12f, 0.3f) / (1f + 0.4f * i),
                        g.Range(0.0001f, 0.0003f));

                // The muzzle blast: the pressure wave and its noisy body, driven and squeezed so it hits hard and holds.
                float tb = T0 + g.Range(0.0002f, 0.0008f);
                var b = new float[g.N(Math.Min(s.Length, 0.6f))];
                Friedlander(b, rate, tb, 1f, s.BlastMs * 0.001f * size);
                Noise(b, g, tb, s.Bright * g.Range(0.85f, 1.15f), 0.00015f, s.BrightTau * size, s.BrightLp, s.BrightHp);
                Noise(b, g, tb, s.Body * g.Range(0.85f, 1.15f), 0.0003f, s.BodyTau * size, 0f, 0f, s.BodyHz / size * g.Range(0.92f, 1.08f), s.BodyQ);
                Noise(b, g, tb, s.Low * g.Range(0.85f, 1.15f), 0.0008f, s.LowTau * size, s.LowLp, 40f);
                Dsp.Norm(b);
                Dsp.Saturate(b, s.Drive);
                Dsp.Compress(b, rate, s.Threshold, s.Ratio, 0.3f, s.Release);
                Dsp.Norm(b);
                Dsp.Mix(x, b, 0, s.BlastLevel * g.Range(0.9f, 1.08f));

                // The charge's thump: weight under the blast, falling in pitch.
                Dsp.Thump(x, rate, tb, s.Thump * g.Range(0.85f, 1.1f), s.ThumpHz0 / size, s.ThumpHz1 / size, s.ThumpGlide, 0.0006f, s.ThumpTau * size);
                Dsp.Norm(x);

                s.Mechanism?.Invoke(x, g, tb);
                Surroundings(x, g, tb, s.Place);
                Dsp.HP(x, 30f, rate);
                return x;
            }

            /// <summary>9 mm: a snappy, mid-heavy pop with a short tail.</summary>
            internal static ShotSpec Pistol() => new ShotSpec
            {
                Length = 1.4f,
                Crack = 0.6f, NWaveMs = 0.22f, Sizzle = 0.45f, SizzleTau = 0.0006f, SizzleHp = 3000f,
                BlastLevel = 0.85f, BlastMs = 0.45f,
                Bright = 0.6f, BrightTau = 0.0035f, BrightLp = 7500f, BrightHp = 450f,
                Body = 0.9f, BodyTau = 0.008f, BodyHz = 1150f, BodyQ = 0.9f,
                Low = 0.45f, LowTau = 0.016f, LowLp = 550f,
                Drive = 2f, Threshold = -12f, Release = 20f,
                Thump = 0.3f, ThumpHz0 = 230f, ThumpHz1 = 95f, ThumpGlide = 0.01f, ThumpTau = 0.022f,
                Mechanism = PistolAction,
                Place = new Surround
                {
                    Early = 7, EarlyTo = 0.12f, EarlyDb = -10f, EarlyLp0 = 5000f, EarlyLp1 = 1600f, EarlyTau = 0.008f,
                    Echoes = 2, EchoFrom = 0.18f, EchoTo = 0.65f, EchoDb = -17f, EchoLp = 1400f, EchoTau = 0.05f,
                    LateDb = -15f, LateTau = 0.4f, LateLp0 = 2600f, LateLp1 = 550f, LateTilt = 700f, Roll = 0.35f, RollHz = 5f,
                },
            };

            /// <summary>5.45 assault rifle: a sharp supersonic crack over a hard, powerful blast and a long tail.</summary>
            internal static ShotSpec Rifle() => new ShotSpec
            {
                Length = 2f,
                Crack = 1f, NWaveMs = 0.28f, Sizzle = 0.7f, SizzleTau = 0.001f, SizzleHp = 2800f, Tear = 2,
                BlastLevel = 0.8f, BlastMs = 0.7f,
                Bright = 0.75f, BrightTau = 0.005f, BrightLp = 10000f, BrightHp = 300f,
                Body = 0.85f, BodyTau = 0.012f, BodyHz = 780f, BodyQ = 0.8f,
                Low = 0.6f, LowTau = 0.03f, LowLp = 450f,
                Drive = 2.4f, Threshold = -14f, Release = 30f,
                Thump = 0.4f, ThumpHz0 = 170f, ThumpHz1 = 70f, ThumpGlide = 0.015f, ThumpTau = 0.035f,
                Mechanism = RifleAction,
                Place = new Surround
                {
                    Early = 9, EarlyTo = 0.18f, EarlyDb = -9f, EarlyLp0 = 6000f, EarlyLp1 = 1500f, EarlyTau = 0.012f,
                    Echoes = 3, EchoFrom = 0.25f, EchoTo = 1.1f, EchoDb = -14f, EchoLp = 1100f, EchoTau = 0.07f,
                    LateDb = -12f, LateTau = 0.6f, LateLp0 = 2600f, LateLp1 = 450f, LateTilt = 500f, Roll = 0.45f, RollHz = 4f,
                },
            };

            /// <summary>12 gauge: little crack, a wide, boomy blast and a heavy roll; the pump worked after it.</summary>
            internal static ShotSpec Shotgun() => new ShotSpec
            {
                Length = 2f,
                Crack = 0.45f, NWaveMs = 0.3f, Sizzle = 0.3f, SizzleTau = 0.0008f, SizzleHp = 2200f,
                BlastLevel = 1f, BlastMs = 1.3f,
                Bright = 0.55f, BrightTau = 0.006f, BrightLp = 6000f, BrightHp = 250f,
                Body = 0.9f, BodyTau = 0.022f, BodyHz = 420f, BodyQ = 0.6f,
                Low = 0.9f, LowTau = 0.05f, LowLp = 350f,
                Drive = 2.6f, Threshold = -15f, Release = 45f,
                Thump = 0.55f, ThumpHz0 = 130f, ThumpHz1 = 55f, ThumpGlide = 0.02f, ThumpTau = 0.06f,
                Mechanism = PumpAction,
                Place = new Surround
                {
                    Early = 9, EarlyTo = 0.2f, EarlyDb = -8f, EarlyLp0 = 4200f, EarlyLp1 = 1200f, EarlyTau = 0.016f,
                    Echoes = 3, EchoFrom = 0.28f, EchoTo = 1.2f, EchoDb = -15f, EchoLp = 1000f, EchoTau = 0.08f,
                    LateDb = -11f, LateTau = 0.6f, LateLp0 = 2200f, LateLp1 = 400f, LateTilt = 450f, Roll = 0.45f, RollHz = 3.5f,
                },
            };

            /// <summary>7.62x54R bolt-action: a long, tearing crack, a huge blast, a tail off every hill; then the bolt.</summary>
            internal static ShotSpec Sniper() => new ShotSpec
            {
                Length = 2.8f,
                Crack = 1f, NWaveMs = 0.5f, Sizzle = 0.8f, SizzleTau = 0.0025f, SizzleHp = 2500f, Tear = 5,
                BlastLevel = 0.9f, BlastMs = 1.1f,
                Bright = 0.8f, BrightTau = 0.008f, BrightLp = 9000f, BrightHp = 250f,
                Body = 0.9f, BodyTau = 0.02f, BodyHz = 560f, BodyQ = 0.7f,
                Low = 0.85f, LowTau = 0.055f, LowLp = 400f,
                Drive = 2.8f, Threshold = -15f, Ratio = 3.5f, Release = 50f,
                Thump = 0.55f, ThumpHz0 = 120f, ThumpHz1 = 48f, ThumpGlide = 0.025f, ThumpTau = 0.07f,
                Mechanism = BoltAction,
                Place = new Surround
                {
                    Early = 10, EarlyTo = 0.25f, EarlyDb = -8f, EarlyLp0 = 5500f, EarlyLp1 = 1300f, EarlyTau = 0.018f,
                    Echoes = 4, EchoFrom = 0.35f, EchoTo = 1.8f, EchoDb = -13f, EchoLp = 1000f, EchoTau = 0.1f,
                    LateDb = -10f, LateTau = 0.8f, LateLp0 = 2600f, LateLp1 = 380f, LateTilt = 450f, Roll = 0.5f, RollHz = 3f,
                },
            };

            // A pistol's slide: back as the bullet leaves, home 30–40 ms later.
            private static void PistolAction(float[] x, SynthGen g, float t0)
            {
                float back = t0 + g.Range(0.003f, 0.006f), home = t0 + g.Range(0.03f, 0.042f);
                Clack(x, g, back, Db(-28f), g.Range(2900f, 3600f), 0.005f);
                Scrape(x, g, back + 0.002f, home - back - 0.002f, Db(-34f), g.Range(3500f, 4500f));
                Clack(x, g, home, Db(-23f), g.Range(2200f, 2900f), 0.008f, 0.8f);
            }

            // The rifle's bolt carrier slamming back and home.
            private static void RifleAction(float[] x, SynthGen g, float t0)
            {
                Clack(x, g, t0 + g.Range(0.007f, 0.011f), Db(-27f), g.Range(1700f, 2200f), 0.008f);
                Clack(x, g, t0 + g.Range(0.052f, 0.068f), Db(-22f), g.Range(2100f, 2700f), 0.01f, 0.8f);
            }

            // The pump racked back and forward between shots.
            private static void PumpAction(float[] x, SynthGen g, float t0)
            {
                float back = t0 + g.Range(0.36f, 0.46f), fwd = back + g.Range(0.12f, 0.17f);
                Scrape(x, g, back - 0.05f, 0.06f, Db(-30f), g.Range(1800f, 2500f), 2f);
                Clack(x, g, back, Db(-23f), g.Range(1300f, 1700f), 0.012f, 0.7f);
                Scrape(x, g, fwd - 0.05f, 0.055f, Db(-31f), g.Range(1900f, 2600f), 2f);
                Clack(x, g, fwd, Db(-21f), g.Range(1500f, 1900f), 0.012f, 0.8f);
            }

            // The bolt worked between shots: up, back, forward, down.
            private static void BoltAction(float[] x, SynthGen g, float t0)
            {
                float up = t0 + g.Range(0.55f, 0.7f);
                float back = up + g.Range(0.07f, 0.1f), fwd = back + g.Range(0.16f, 0.22f), down = fwd + g.Range(0.06f, 0.09f);
                Clack(x, g, up, Db(-30f), g.Range(2600f, 3200f), 0.006f);
                Scrape(x, g, up + 0.01f, back - up - 0.01f, Db(-33f), g.Range(2200f, 3000f));
                Clack(x, g, back, Db(-25f), g.Range(1600f, 2000f), 0.01f, 0.7f);
                Scrape(x, g, back + 0.03f, fwd - back - 0.03f, Db(-33f), g.Range(2200f, 3000f));
                Clack(x, g, fwd, Db(-25f), g.Range(1800f, 2300f), 0.01f, 0.7f);
                Clack(x, g, down, Db(-27f), g.Range(2400f, 3000f), 0.007f);
            }

            /// <summary>Through a can: a subsonic cough, the action loud beside it (a rifle's supersonic round still cracks).</summary>
            internal static float[] Suppressed(SynthGen g, bool rifle)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(rifle ? 0.8f : 0.6f);
                var d = new float[x.Length];
                // Core suppressed blast: Friedlander pressure wave, low‑pass filtered to simulate the muffled blast inside a can.
                Friedlander(d, rate, T0, 1f, g.Range(0.0012f, 0.0017f));
                // Add a thin hiss to simulate the cough of gas escaping the can.
                Dsp.LP(d, g.Range(1200f, 1600f), rate);
                // Two overlapping noise bursts: a short high‑frequency hiss and a lower‑frequency rumble.
                Noise(d, g, T0 + 0.0004f, 0.9f, 0.0008f, g.Range(0.007f, 0.011f), 1500f, 120f);
                // Additional low‑frequency rumble.
                Noise(d, g, T0 + 0.001f, 0.5f, 0.001f, 0.014f, 0f, 0f, g.Range(500f, 700f), 1.2f);
                // A low‑frequency thump to give the impression of the gas hammering against the can.
                Dsp.Thump(d, rate, T0, 0.7f, g.Range(170f, 210f), 70f, 0.012f, 0.0008f, 0.028f);
                if (rifle)
                {
                    // Rifle‑specific high‑frequency crack from the supersonic round.
                    NWave(d, rate, T0 + 0.0003f, 0.6f, g.Range(0.00025f, 0.00035f));
                    // Additional rifle noise: a brief burst to reinforce the supersonic crack.
                    Noise(d, g, T0 + 0.0003f, 0.35f, 0.00004f, 0.0008f, 16000f, 2200f);
                }
                // Normalize after adding the generic suppressed components.
                Dsp.Norm(d);
                Dsp.Saturate(d, 1.6f);
                Dsp.Compress(d, rate, -18f, 3f, 0.3f, 35f);
                Dsp.Norm(d);
                Dsp.Mix(x, d, 0);
                if (rifle)
                {
                    Clack(x, g, T0 + g.Range(0.007f, 0.011f), Db(-12f), g.Range(1700f, 2200f), 0.009f);
                    Clack(x, g, T0 + g.Range(0.052f, 0.066f), Db(-8f), g.Range(2100f, 2600f), 0.011f, 0.8f);
                }
                else
                {
                    float back = T0 + g.Range(0.003f, 0.006f), home = T0 + g.Range(0.03f, 0.042f);
                    Clack(x, g, back, Db(-13f), g.Range(2900f, 3500f), 0.006f);
                    Scrape(x, g, back + 0.002f, home - back - 0.002f, Db(-22f), g.Range(3500f, 4500f));
                    Clack(x, g, home, Db(-8f), g.Range(2300f, 2900f), 0.009f, 0.9f);
                }
                Surroundings(x, g, T0, new Surround
                {
                    Early = 4, EarlyTo = 0.08f, EarlyDb = -20f, EarlyLp0 = 2500f, EarlyLp1 = 1000f, EarlyTau = 0.008f, Echoes = 0,
                    LateDb = rifle ? -27f : -30f, LateTau = rifle ? 0.18f : 0.12f, LateLp0 = 1800f, LateLp1 = 500f, Roll = 0.3f,
                });
                Dsp.HP(x, 40f, rate);
                return x;
            }

            /// <summary>A flare pistol: a hollow, low-pressure "thoonk", then the flare catching and fizzing away upward.</summary>
            internal static float[] Flare(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(1.6f);
                var d = new float[g.N(0.4f)];
                Friedlander(d, rate, T0, 1f, g.Range(0.0018f, 0.0025f));
                Dsp.LP(d, 2200f, rate);
                Noise(d, g, T0, 0.7f, 0.0006f, 0.012f, 2000f, 100f);
                Dsp.Thump(d, rate, T0, 0.8f, g.Range(210f, 250f), 110f, 0.015f, 0.001f, 0.04f);
                Dsp.Modes(d, rate, T0, 0.2f, new[] { g.Range(380f, 440f), g.Range(1050f, 1200f) }, new[] { 1f, 0.5f }, new[] { 0.03f, 0.015f });
                Dsp.Norm(d);
                Dsp.Saturate(d, 1.5f);
                Dsp.Norm(d);
                Dsp.Mix(x, d, 0);
                float s0 = g.Range(900f, 1200f), s1 = g.Range(2600f, 3400f), fizz = g.Range(0.3f, 0.45f);
                float[] f = Swept(g, 1.57f, 0.9f, t => Dsp.Glide(s0, s1, t / 1.2f), true);
                Dsp.Shape(f, rate, t => Dsp.AD(t, 0.06f, fizz));
                Dsp.Mix(x, f, g.N(0.02f), Db(-9f));
                var c = new float[g.N(1.3f)];
                Dsp.Grains(c, g, 0f, 1.2f, 140, 1f, 0.0004f, u => (1f - u) * (1f - u));
                Dsp.HP(c, 2500f, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, g.N(0.05f), Db(-20f));
                Surroundings(x, g, T0, new Surround
                {
                    Early = 5, EarlyTo = 0.12f, EarlyDb = -16f, EarlyLp0 = 3000f, EarlyLp1 = 1000f, Echoes = 1, EchoDb = -28f,
                    LateDb = -24f, LateTau = 0.3f, LateLp0 = 2000f, LateLp1 = 500f,
                });
                Dsp.HP(x, 40f, rate);
                return x;
            }

            /// <summary>A pistol-class shot a few hundred metres off: a muffled pop and its echo coming back again and again.</summary>
            internal static float[] FarLight(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                const float t0 = 0.004f;
                float[] x = g.Buffer(2.8f);
                var d = new float[g.N(0.3f)];
                Friedlander(d, rate, t0, 1f, g.Range(0.0008f, 0.0012f));
                Noise(d, g, t0, 0.8f, 0.0008f, g.Range(0.01f, 0.016f), 0f, 0f, g.Range(650f, 850f), 0.8f);
                Dsp.Thump(d, rate, t0, 0.45f, 140f, 65f, 0.015f, 0.001f, 0.03f);
                Dsp.LP(d, g.Range(1600f, 2200f), rate);
                Dsp.LP(d, 2400f, rate);
                Dsp.Norm(d);
                Dsp.Mix(x, d, 0);
                int echoes = g.Int(4, 7);
                for (int i = 0; i < echoes; i++)
                {
                    float u = (i + g.Range(0.1f, 0.9f)) / echoes;
                    Noise(x, g, t0 + 0.12f + 1.5f * u * u, Db(-8f - 12f * u + g.Range(-3f, 2f)), g.Range(0.004f, 0.012f),
                        g.Range(0.04f, 0.1f) * (1f + u), Dsp.Glide(1300f, 700f, u), 120f);
                }
                Roll(x, g, t0 + 0.04f, Db(-13f), 0.08f, g.Range(0.7f, 0.9f), 1200f, 330f, 90f, 0.5f, 3f, 600f);
                Dsp.HP(x, 45f, rate);
                return x;
            }

            /// <summary>A rifle, shotgun or sniper shot a few hundred metres off: a dry snap, a low boom and a long roll
            /// back off the hills and buildings.</summary>
            internal static float[] FarHeavy(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                const float t0 = 0.004f;
                float[] x = g.Buffer(3.6f);
                var d = new float[g.N(1f)];
                NWave(d, rate, t0, 0.35f, 0.0005f);
                Noise(d, g, t0, 0.3f, 0.0002f, 0.003f, 3500f, 700f);
                float tb = t0 + g.Range(0.001f, 0.004f);
                Friedlander(d, rate, tb, 1f, g.Range(0.0018f, 0.0026f));
                Noise(d, g, tb, 0.9f, 0.0015f, g.Range(0.025f, 0.04f), 900f, 50f);
                Dsp.Thump(d, rate, tb, 0.9f, g.Range(85f, 100f), 38f, 0.04f, 0.0015f, g.Range(0.12f, 0.18f));
                Dsp.LP(d, 1500f, rate);
                Dsp.Norm(d);
                Dsp.Saturate(d, 1.5f);
                Dsp.Norm(d);
                Dsp.Mix(x, d, 0);
                int echoes = g.Int(5, 9);
                for (int i = 0; i < echoes; i++)
                {
                    float u = (i + g.Range(0.1f, 0.9f)) / echoes;
                    float at = t0 + 0.15f + 2.3f * u, db = -7f - 11f * u + g.Range(-3f, 3f);
                    Noise(x, g, at, Db(db), g.Range(0.006f, 0.02f), g.Range(0.08f, 0.2f) * (1f + 0.5f * u),
                        Dsp.Glide(900f, 450f, u) * g.Range(0.85f, 1.15f), 60f);
                    if (g.Chance(0.5f)) Dsp.Thump(x, rate, at, Db(db - 4f), g.Range(70f, 85f), 38f, 0.05f, 0.004f, g.Range(0.08f, 0.14f));
                }
                Roll(x, g, t0 + 0.06f, Db(-11f), 0.15f, g.Range(0.8f, 1.1f), 800f, 220f, 45f, 0.55f, 2f);
                Dsp.HP(x, 30f, rate);
                return x;
            }

            /// <summary>A spent case on the ground: a bright brass "tink", bouncing and closing up; the last two
            /// variations land on earth (a duller tick, little ring).</summary>
            internal static float[] Casing(SynthGen g)
            {
                g = Fresh(g);
                float[] x = g.Buffer(0.6f);
                bool soft = g.Variation >= 4;
                float f = g.Range(3600f, 5200f);
                float[] hz = { f, f * g.Range(1.5f, 1.62f), f * g.Range(2.6f, 2.85f), f * g.Range(3.6f, 4f) };
                float end = Bounces(x, g, T0 + 0.002f, 1f, hz, soft ? 0.008f : g.Range(0.03f, 0.06f), soft ? g.Int(2, 4) : g.Int(4, 7),
                    g.Range(0.09f, 0.16f), g.Range(0.55f, 0.68f), soft ? 0.6f : 0.15f, 0.004f);
                if (!soft && g.Chance(0.6f))
                {
                    // A short roll on its side to finish: a light, quick rattle.
                    int n = g.Int(4, 8);
                    for (int i = 0; i < n; i++)
                        Clack(x, g, end + i * g.Range(0.012f, 0.02f), Db(-24f - 2f * i), f * g.Range(0.98f, 1.02f), 0.006f, 0.3f);
                }
                Dsp.HP(x, 200f, g.Rate);
                return x;
            }

            /// <summary>A spent shotgun hull: a plastic tube with a brass head — a hollow "tok", a little brass, a short roll.</summary>
            internal static float[] Hull(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.6f);
                float tube = g.Range(900f, 1400f), brass = g.Range(3400f, 4400f);
                float t = T0 + 0.002f, a = 1f, gap = g.Range(0.1f, 0.16f);
                int hits = g.Int(2, 4);
                for (int i = 0; i < hits; i++)
                {
                    Dsp.Modes(x, rate, t, a * 0.5f, new[] { tube, tube * g.Range(2.2f, 2.5f), tube * g.Range(3.8f, 4.3f) },
                        new[] { 1f, 0.45f, 0.2f }, new[] { g.Range(0.008f, 0.014f), 0.007f, 0.004f });
                    Noise(x, g, t, a * 0.5f, 0.0001f, 0.0025f, 3000f, 300f);
                    if (g.Chance(0.7f))
                        Dsp.Modes(x, rate, t + g.Range(0.002f, 0.01f), a * 0.18f, new[] { brass, brass * 1.6f, brass * 2.7f },
                            new[] { 1f, 0.5f, 0.3f }, new[] { 0.015f, 0.01f, 0.006f });
                    t += gap * g.Range(0.8f, 1.2f);
                    gap *= 0.55f;
                    a *= g.Range(0.4f, 0.6f);
                }
                int rolls = g.Int(3, 6);
                for (int i = 0; i < rolls; i++) Noise(x, g, t + i * g.Range(0.015f, 0.025f), Db(-22f - 2f * i), 0.0003f, 0.002f, 2500f, 400f);
                Dsp.HP(x, 150f, rate);
                return x;
            }
            #endregion

            #region Handling
            /// <summary>Trigger take-up, then the hammer (or striker) falling on nothing.</summary>
            internal static float[] DryFire(SynthGen g)
            {
                g = Fresh(g);
                float[] x = g.Buffer(0.18f);
                Clack(x, g, T0, 0.2f, g.Range(4200f, 5200f), 0.003f);
                float fall = T0 + g.Range(0.03f, 0.055f);
                Clack(x, g, fall, 1f, g.Range(2600f, 3300f), g.Range(0.005f, 0.008f), 0.9f);
                Noise(x, g, fall, 0.25f, 0.0002f, 0.006f, 1500f, 150f);
                Dsp.HP(x, 150f, g.Rate);
                return x;
            }

            /// <summary>The hammer falls on a dud — a dead click — then the action is worked and sticks: a gritty
            /// half-stroke, catching, and a hard stop on the stuck case.</summary>
            internal static float[] Jam(SynthGen g)
            {
                g = Fresh(g);
                float[] x = g.Buffer(0.75f);
                Clack(x, g, T0, 0.8f, g.Range(2400f, 2900f), 0.004f, 0.6f);
                Noise(x, g, T0, 0.3f, 0.0003f, 0.008f, 1200f, 100f);
                float t = T0 + g.Range(0.24f, 0.32f);
                Scrape(x, g, t, 0.09f, 0.35f, g.Range(1800f, 2400f), 1.8f);
                for (int i = 0; i < 4; i++)
                    Clack(x, g, t + 0.015f + i * g.Range(0.014f, 0.022f), g.Range(0.08f, 0.16f), g.Range(2000f, 3000f), 0.003f);
                Clack(x, g, t + 0.1f, 0.9f, g.Range(1300f, 1700f), 0.012f, 0.7f);
                Noise(x, g, t + 0.1f, 0.35f, 0.0004f, 0.01f, 900f, 80f);
                Dsp.HP(x, 120f, g.Rate);
                return x;
            }

            internal enum Reloads { Generic, Pistol, Rifle, Shotgun, Bolt }

            /// <summary>A reload, by kind of gun, compressed into a second or so (it plays as the reload starts).</summary>
            internal static float[] Reload(SynthGen g, Reloads kind)
            {
                g = Fresh(g);
                float[] x;
                switch (kind)
                {
                    case Reloads.Pistol:
                    {
                        // Magazine catch, the empty sliding out; the fresh one in and seated; the slide released.
                        x = g.Buffer(1f);
                        Clack(x, g, T0, 0.3f, g.Range(4200f, 4800f), 0.003f);
                        Scrape(x, g, T0 + 0.02f, 0.08f, 0.3f, g.Range(2300f, 2800f));
                        float seat = g.Range(0.48f, 0.55f);
                        Scrape(x, g, seat - 0.07f, 0.07f, 0.3f, g.Range(2000f, 2500f));
                        Clack(x, g, seat, 0.9f, g.Range(1700f, 2100f), 0.008f, 0.8f);
                        Noise(x, g, seat, 0.4f, 0.0003f, 0.006f, 900f, 80f);
                        float rel = g.Range(0.78f, 0.85f);
                        Scrape(x, g, rel - 0.03f, 0.03f, 0.25f, g.Range(3200f, 3800f));
                        Clack(x, g, rel, 1f, g.Range(2400f, 2900f), 0.01f, 0.9f);
                        break;
                    }
                    case Reloads.Rifle:
                    {
                        // The empty rocked out; the fresh one hooked in and rocked home; the charging handle racked.
                        x = g.Buffer(1.4f);
                        Clack(x, g, T0, 0.6f, g.Range(1200f, 1500f), 0.012f, 0.5f);
                        Scrape(x, g, T0 + 0.01f, 0.1f, 0.35f, g.Range(1400f, 1800f));
                        float hook = g.Range(0.52f, 0.6f);
                        Scrape(x, g, hook, 0.06f, 0.3f, g.Range(1500f, 1900f));
                        Clack(x, g, hook + 0.07f, 1f, g.Range(1400f, 1700f), 0.012f, 0.8f);
                        Noise(x, g, hook + 0.07f, 0.4f, 0.0004f, 0.008f, 700f, 60f);
                        float rack = g.Range(0.95f, 1.02f);
                        Scrape(x, g, rack, 0.07f, 0.35f, g.Range(2000f, 2500f));
                        Clack(x, g, rack + 0.07f, 0.5f, g.Range(1800f, 2100f), 0.01f);
                        Clack(x, g, rack + g.Range(0.13f, 0.16f), 1f, g.Range(2100f, 2500f), 0.012f, 0.9f);
                        Noise(x, g, rack + 0.15f, 0.4f, 0.0004f, 0.008f, 1200f, 80f);
                        break;
                    }
                    case Reloads.Shotgun:
                    {
                        // Shells thumbed into the tube one by one, then the pump racked.
                        x = g.Buffer(1.5f);
                        int shells = 3;
                        for (int i = 0; i < shells; i++)
                        {
                            // Build suppressed shot: generate core suppressed blast, add low‑frequency cough and thump, then handle rifle‑specific crack and pistol‑specific clack/scrape, finally add environmental surround.
                            float t = T0 + 0.05f + i * g.Range(0.24f, 0.3f);
                            Scrape(x, g, t, 0.05f, 0.25f, g.Range(1300f, 1700f), 1.5f);
                            Clack(x, g, t + 0.05f, 0.55f, g.Range(2600f, 3000f), 0.005f, 0.5f);
                            Noise(x, g, t + 0.05f, 0.3f, 0.0003f, 0.004f, 1200f, 100f);
                        }
                        float back = g.Range(0.92f, 0.98f), fwd = back + g.Range(0.15f, 0.19f);
                        Scrape(x, g, back - 0.06f, 0.06f, 0.35f, g.Range(1800f, 2300f), 2f);
                        Clack(x, g, back, 0.8f, g.Range(1300f, 1600f), 0.012f, 0.7f);
                        Scrape(x, g, fwd - 0.05f, 0.05f, 0.3f, g.Range(1900f, 2400f), 2f);
                        Clack(x, g, fwd, 1f, g.Range(1500f, 1800f), 0.012f, 0.9f);
                        break;
                    }
                    case Reloads.Bolt:
                    {
                        // Bolt up and back, a stripper clip pressed down into the magazine and flicked away, bolt home.
                        x = g.Buffer(1.6f);
                        Clack(x, g, T0, 0.5f, g.Range(2600f, 3000f), 0.006f);
                        Scrape(x, g, T0 + 0.05f, 0.1f, 0.35f, g.Range(2300f, 2800f));
                        Clack(x, g, T0 + 0.15f, 0.8f, g.Range(1600f, 1900f), 0.01f, 0.7f);
                        float clip = g.Range(0.38f, 0.44f);
                        Clack(x, g, clip, 0.5f, g.Range(3000f, 3500f), 0.006f);
                        Scrape(x, g, clip + 0.05f, 0.35f, 0.3f, g.Range(1800f, 2300f), 1.8f);
                        for (int i = 0; i < 5; i++) Clack(x, g, clip + 0.09f + i * g.Range(0.055f, 0.065f), 0.2f, g.Range(3000f, 4000f), 0.004f);
                        Dsp.Modes(x, g.Rate, clip + 0.52f, 0.35f, new[] { g.Range(4200f, 5000f), g.Range(6800f, 7600f) }, new[] { 1f, 0.5f }, new[] { 0.04f, 0.02f });
                        float home = g.Range(1.15f, 1.22f);
                        Scrape(x, g, home - 0.1f, 0.1f, 0.35f, g.Range(2300f, 2800f));
                        Clack(x, g, home, 0.9f, g.Range(1900f, 2200f), 0.01f, 0.8f);
                        Clack(x, g, home + g.Range(0.08f, 0.12f), 0.7f, g.Range(2500f, 2900f), 0.007f);
                        break;
                    }
                    default:
                    {
                        // Magazine out, magazine in, the action racked.
                        x = g.Buffer(1.2f);
                        Clack(x, g, T0, 0.5f, g.Range(1500f, 1900f), 0.01f, 0.5f);
                        Scrape(x, g, T0 + 0.01f, 0.09f, 0.3f, g.Range(1700f, 2200f));
                        float seat = g.Range(0.45f, 0.52f);
                        Scrape(x, g, seat - 0.06f, 0.06f, 0.3f, g.Range(1600f, 2000f));
                        Clack(x, g, seat, 0.9f, g.Range(1600f, 1900f), 0.01f, 0.8f);
                        Noise(x, g, seat, 0.35f, 0.0004f, 0.007f, 800f, 70f);
                        float rack = g.Range(0.82f, 0.88f);
                        Scrape(x, g, rack, 0.06f, 0.3f, g.Range(2200f, 2700f));
                        Clack(x, g, rack + 0.06f, 0.45f, g.Range(1900f, 2200f), 0.008f);
                        Clack(x, g, rack + g.Range(0.12f, 0.15f), 1f, g.Range(2200f, 2600f), 0.011f, 0.9f);
                        break;
                    }
                }
                Dsp.HP(x, 100f, g.Rate);
                return x;
            }

            /// <summary>A bolt thrown: the arm's quick whoosh and the rag tied to it fluttering as it sails off.</summary>
            internal static float[] BoltThrow(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.5f);
                float len = g.Range(0.2f, 0.28f), peak = g.Range(0.5f, 0.65f), lo = g.Range(400f, 550f), hi = g.Range(1300f, 1900f);
                float[] w = Swept(g, len, 1.4f, t => lo + (hi - lo) * Dsp.Bell(t, len, peak), true);
                Dsp.Shape(w, rate, t => MathF.Pow(Dsp.Bell(t, len, peak), 1.5f));
                Dsp.Mix(x, w, g.N(0.005f), 1f);
                float at = 0.005f + len * peak;
                float flap = g.Range(22f, 38f), rag = g.Range(900f, 1400f);
                var r = new float[x.Length - g.N(at)];
                for (int i = 0; i < r.Length; i++) r[i] = g.Bipolar();
                Dsp.BP(r, rag, 1.2f, rate);
                Dsp.Norm(r);
                Dsp.Shape(r, rate, t =>
                {
                    float s = MathF.Max(0f, MathF.Sin(Dsp.TwoPi * flap * t));
                    return Dsp.AD(t, 0.02f, 0.09f) * (0.35f + 0.65f * s * s);
                });
                Dsp.Mix(x, r, g.N(at), Db(-9f));
                Noise(x, g, 0.004f, Db(-24f), 0.003f, 0.02f, 7000f, 2000f);
                Dsp.HP(x, 120f, rate);
                return x;
            }

            /// <summary>
            /// A thrown bolt (a heavy steel nut) landing: on hard ground it rings and skitters; on earth (the last two
            /// variations) it knocks and stops; its rag flops down after it. Also a grenade bouncing, pitched down.
            /// </summary>
            internal static float[] BoltLand(SynthGen g)
            {
                g = Fresh(g);
                float[] x = g.Buffer(0.75f);
                bool hard = g.Variation < 2;
                float f = g.Range(2300f, 3000f);
                float[] hz = { f, f * g.Range(1.45f, 1.6f), f * g.Range(2.3f, 2.6f), f * g.Range(3.3f, 3.8f), f * g.Range(4.6f, 5.2f) };
                Bounces(x, g, T0, 1f, hz, hard ? g.Range(0.03f, 0.05f) : 0.007f, hard ? g.Int(3, 6) : g.Int(2, 4), g.Range(0.08f, 0.13f),
                    g.Range(0.5f, 0.65f), hard ? 0.3f : 0.9f, hard ? 0.003f : 0.008f);
                Noise(x, g, T0 + g.Range(0.06f, 0.12f), Db(-20f), 0.004f, 0.02f, 3000f, 300f);
                Dsp.HP(x, 70f, g.Rate);
                return x;
            }

            /// <summary>A grenade's pin drawn (a steel scrape ending in a tick and a ping of the ring), then the spoon
            /// flying off with a springy sheet-steel ping.</summary>
            internal static float[] GrenadePin(SynthGen g)
            {
                g = Fresh(g);
                float[] x = g.Buffer(0.7f);
                float pull = g.Range(0.05f, 0.08f);
                Scrape(x, g, T0, pull, 0.5f, g.Range(3500f, 4500f), 3f);
                float free = T0 + pull;
                Clack(x, g, free, 0.6f, g.Range(4200f, 5200f), 0.02f, 0.5f);
                float spoon = free + g.Range(0.2f, 0.32f);
                Clack(x, g, spoon, 0.9f, g.Range(2600f, 3200f), 0.035f, 0.5f);
                Noise(x, g, spoon, 0.15f, 0.005f, 0.04f, 0f, 0f, 2500f, 1.5f);
                Dsp.HP(x, 200f, g.Rate);
                return x;
            }
            #endregion

            #region Bullets
            /// <summary>A supersonic round passing within a few metres: its bow shock cracks hard (and again off the
            /// ground a few ms later), then the hiss of its wake tears past, falling in pitch.</summary>
            internal static float[] Whiz(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.4f);
                NWave(x, rate, T0, 1f, g.Range(0.00022f, 0.00038f));
                Noise(x, g, T0, 0.4f, 0.00003f, 0.0006f, 16000f, 3000f);
                var b = new float[g.N(0.004f)];
                NWave(b, rate, 0f, 1f, 0.0004f);
                Dsp.LP(b, 7000f, rate);
                Dsp.Mix(x, b, g.N(T0 + g.Range(0.003f, 0.008f)), g.Range(0.25f, 0.4f));
                float f0 = g.Range(5500f, 7500f), f1 = g.Range(1600f, 2400f), sweep = g.Range(0.05f, 0.09f), tail = g.Range(0.03f, 0.05f);
                float[] w = Swept(g, 0.3f, 2.2f, t => Dsp.Glide(f0, f1, t / sweep), true);
                Dsp.Shape(w, rate, t => Dsp.AD(t, 0.002f, tail));
                Dsp.Mix(x, w, g.N(T0 + 0.0005f), Db(-6f));
                Noise(x, g, T0 + 0.002f, Db(-12f), 0.004f, 0.03f, 900f, 120f);
                Dsp.HP(x, 120f, rate);
                return x;
            }

            /// <summary>A subsonic (pistol) bullet or buckshot passing close: no crack, a quick "zzip" swelling to the
            /// closest point and dropping in pitch as it goes.</summary>
            internal static float[] Flyby(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.45f);
                float tp = g.Range(0.05f, 0.08f), before = g.Range(0.02f, 0.03f), after = g.Range(0.035f, 0.055f);
                float fc = g.Range(1600f, 2300f), swing = g.Range(0.25f, 0.4f), spin = g.Range(110f, 190f);
                Func<float, float> pitch = t => fc * (1f + swing * MathF.Tanh(-(t - tp) / 0.02f));
                float[] w = Swept(g, 0.44f, 3f, pitch, true);
                float[] v = Swept(g, 0.44f, 6f, t => 2f * pitch(t), true);
                for (int i = 0; i < w.Length; i++)
                {
                    float t = i / (float)rate;
                    float env = t < tp ? MathF.Exp(-(tp - t) / before) : MathF.Exp(-(t - tp) / after);
                    w[i] = (w[i] + 0.4f * v[i]) * env * (1f + 0.2f * MathF.Sin(Dsp.TwoPi * spin * t));
                }
                Dsp.Mix(x, w, 0, 1f);
                Dsp.HP(x, 300f, rate);
                return x;
            }

            /// <summary>A round striking a body: a hard, wet smack, the meat's dull thud, a squelch and a few drops.</summary>
            internal static float[] HitFlesh(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.35f);
                Noise(x, g, T0, 0.8f, 0.00008f, g.Range(0.004f, 0.007f), 0f, 0f, g.Range(1400f, 2000f), 1.1f);
                Noise(x, g, T0, 0.3f, 0.00003f, 0.0005f, 0f, 3000f);
                Noise(x, g, T0 + 0.0005f, 0.6f, 0.0005f, g.Range(0.014f, 0.022f), 1600f, 300f);
                Dsp.Thump(x, rate, T0, 1f, g.Range(190f, 240f), g.Range(75f, 95f), 0.01f, 0.0006f, g.Range(0.02f, 0.028f));
                Noise(x, g, T0 + 0.0005f, 0.7f, 0.0008f, g.Range(0.012f, 0.018f), 520f, 50f);
                float s0 = g.Range(1600f, 2200f), s1 = g.Range(450f, 650f);
                float[] s = Swept(g, 0.1f, 5f, t => Dsp.Glide(s0, s1, t / 0.045f), true);
                Dsp.Shape(s, rate, t => Dsp.AD(t, 0.002f, 0.022f));
                Dsp.Mix(x, s, g.N(T0 + 0.003f), Db(-3f));
                // Spatter: a few wet splats landing around it.
                int drops = g.Int(3, 7);
                for (int i = 0; i < drops; i++)
                    Noise(x, g, T0 + g.Range(0.015f, 0.18f), Db(g.Range(-26f, -16f)), 0.0003f, g.Range(0.002f, 0.005f), 0f, 0f,
                        g.Range(1200f, 3500f), 2.5f);
                Dsp.Norm(x);
                Dsp.Compress(x, rate, -20f, 3f, 0.2f, 30f);
                Dsp.HP(x, 40f, rate);
                return x;
            }

            /// <summary>A round stopped by armour: a hard crack and the blow through the carrier; a plate rings (even
            /// variations), soft armour only thuds with a ceramic crunch.</summary>
            internal static float[] HitArmor(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.7f);
                bool plate = g.Variation % 2 == 0;
                NWave(x, rate, T0, 0.5f, 0.00015f);
                Noise(x, g, T0, 0.5f, 0.00003f, 0.0007f, 0f, 2500f);
                Dsp.Thump(x, rate, T0, 0.8f, g.Range(160f, 200f), 80f, 0.012f, 0.0006f, 0.03f);
                Noise(x, g, T0, 0.5f, 0.0006f, 0.012f, 1500f, 80f);
                float f = g.Range(800f, 1150f);
                float[] ratio = { 1f, 1.59f, 2.14f, 2.97f, 3.83f, 4.6f, 5.9f };
                var hz = new float[ratio.Length];
                var amp = new float[ratio.Length];
                var tau = new float[ratio.Length];
                for (int m = 0; m < ratio.Length; m++)
                {
                    hz[m] = f * ratio[m] * g.Range(0.97f, 1.03f);
                    amp[m] = g.Range(0.4f, 1f) / (1f + 0.25f * m);
                    tau[m] = (plate ? g.Range(0.06f, 0.11f) : 0.025f) / (1f + 0.35f * m);
                }
                Dsp.Modes(x, rate, T0, plate ? 0.5f : 0.2f, hz, amp, tau);
                if (!plate)
                {
                    var c = new float[g.N(0.05f)];
                    Dsp.Grains(c, g, 0f, 0.025f, 24, 1f, 0.0002f, u => 1f - u);
                    Dsp.HP(c, 2000f, rate);
                    Dsp.Norm(c);
                    Dsp.Mix(x, c, g.N(T0), Db(-10f));
                }
                Dsp.HP(x, 60f, rate);
                return x;
            }

            /// <summary>A round glancing off stone or steel: a sharp strike and a spray of grit, then the deformed bullet
            /// tumbling away — a whistling whine falling as it leaves, warbling as it turns over.</summary>
            internal static float[] Ricochet(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.8f);
                NWave(x, rate, T0, 0.6f, 0.00015f);
                Noise(x, g, T0, 0.6f, 0.00003f, 0.0008f, 0f, 2000f);
                Noise(x, g, T0 + 0.001f, 0.25f, 0.0005f, 0.004f, 12000f, 4000f);
                float start = T0 + g.Range(0.004f, 0.012f);
                float f0 = g.Range(2800f, 4200f), f1 = g.Range(900f, 1500f), tc = g.Range(0.12f, 0.26f), wd = g.Range(0.05f, 0.09f);
                float tumble = g.Range(55f, 140f), am = g.Range(0.2f, 0.45f), fm = g.Range(0.015f, 0.035f), tau = g.Range(0.16f, 0.26f);
                Func<float, float> pitch = t => (f1 + (f0 - f1) / (1f + MathF.Exp((t - tc) / wd))) * (1f + fm * MathF.Sin(Dsp.TwoPi * tumble * t));
                float[] hiss = Swept(g, 0.8f - start, 14f, pitch, true);
                var tone = new float[hiss.Length];
                float ph = 0f;
                for (int i = 0; i < tone.Length; i++)
                {
                    float t = i / (float)rate;
                    ph += pitch(t) / rate;
                    ph -= MathF.Floor(ph);
                    tone[i] = MathF.Sin(Dsp.TwoPi * ph);
                }
                Dsp.Norm(hiss);
                for (int i = 0; i < hiss.Length; i++)
                {
                    float t = i / (float)rate;
                    float wob = 1f - am + am * (0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * tumble * 0.5f * t));
                    hiss[i] = (hiss[i] + 0.45f * tone[i]) * Dsp.AD(t, 0.008f, tau) * wob;
                }
                Dsp.Mix(x, hiss, g.N(start), Db(-4f));
                Dsp.HP(x, 250f, rate);
                return x;
            }
            #endregion

            #region Impacts on the world
            /// <summary>Into earth or grass: a dull, punchy "thup", a sandy spray of grit and a few clods falling back.</summary>
            internal static float[] ImpactDirt(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.5f);
                Noise(x, g, T0, 1f, 0.0002f, g.Range(0.005f, 0.008f), 1300f, 90f);
                Dsp.Thump(x, rate, T0, 0.6f, g.Range(170f, 220f), 70f, 0.012f, 0.0005f, 0.02f);
                var c = new float[g.N(0.35f)];
                Dsp.Grains(c, g, 0f, 0.3f, g.Int(70, 110), 1f, 0.00025f, u => MathF.Exp(-u * 4f));
                Dsp.HP(c, 1500f, rate);
                Dsp.LP(c, 9000f, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, g.N(T0 + 0.002f), Db(-7f));
                Noise(x, g, T0 + 0.002f, Db(-12f), 0.006f, 0.05f, 6000f, 1200f);
                int clods = g.Int(3, 6);
                for (int i = 0; i < clods; i++) Noise(x, g, T0 + g.Range(0.08f, 0.4f), Db(g.Range(-26f, -18f)), 0.0003f, 0.003f, 900f, 120f);
                Dsp.HP(x, 50f, rate);
                return x;
            }

            /// <summary>Into concrete, brick or stone: a hard, bright crack, a stony chip ring, dust, chips landing.</summary>
            internal static float[] ImpactConcrete(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.55f);
                NWave(x, rate, T0, 0.7f, 0.00012f);
                Noise(x, g, T0, 0.8f, 0.00003f, 0.001f, 0f, 1500f);
                Noise(x, g, T0, 0.8f, 0.0002f, g.Range(0.006f, 0.009f), 0f, 0f, g.Range(1300f, 2200f), 0.9f);
                Noise(x, g, T0, 0.5f, 0.0003f, 0.012f, 900f, 150f);
                float f = g.Range(1800f, 3200f);
                Dsp.Modes(x, rate, T0, 0.25f, new[] { f, f * g.Range(1.35f, 1.5f), f * g.Range(2.2f, 2.5f) }, new[] { 1f, 0.6f, 0.4f },
                    new[] { g.Range(0.004f, 0.006f), 0.004f, 0.003f });
                Dsp.Thump(x, rate, T0, 0.2f, 320f, 150f, 0.004f, 0.0003f, 0.008f);
                var c = new float[g.N(0.25f)];
                Dsp.Grains(c, g, 0f, 0.2f, 50, 1f, 0.0002f, u => MathF.Exp(-u * 5f));
                Dsp.HP(c, 3000f, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, g.N(T0 + 0.001f), Db(-10f));
                Noise(x, g, T0 + 0.002f, Db(-14f), 0.008f, 0.05f, 9000f, 2500f);
                int chips = g.Int(3, 7);
                for (int i = 0; i < chips; i++)
                {
                    float hz = g.Range(2500f, 5000f);
                    Dsp.Modes(x, rate, T0 + g.Range(0.06f, 0.45f), Db(g.Range(-30f, -20f)), new[] { hz, hz * 1.6f }, new[] { 1f, 0.5f },
                        new[] { g.Range(0.002f, 0.004f), 0.002f });
                }
                Dsp.HP(x, 80f, rate);
                return x;
            }

            /// <summary>Into steel: thick plate rings a clean "ping" (first two variations); thin sheet (a car, a roof, a
            /// barrel) clangs dense and buzzing and rattles in its frame.</summary>
            internal static float[] ImpactMetal(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.9f);
                bool sheet = g.Variation >= 2;
                NWave(x, rate, T0, 0.5f, 0.0001f);
                Noise(x, g, T0, 0.6f, 0.00003f, 0.0008f, 0f, 2500f);
                if (!sheet)
                {
                    float f = g.Range(1500f, 2300f);
                    Dsp.Modes(x, rate, T0, 0.5f, new[] { f, f * g.Range(2.28f, 2.36f), f * g.Range(4.15f, 4.35f), f * g.Range(6.5f, 6.8f) },
                        new[] { 1f, 0.7f, 0.4f, 0.25f }, new[] { g.Range(0.15f, 0.25f), 0.1f, 0.05f, 0.025f });
                }
                else
                {
                    const int modes = 10;
                    var hz = new float[modes];
                    var amp = new float[modes];
                    var tau = new float[modes];
                    for (int m = 0; m < modes; m++)
                    {
                        hz[m] = 300f * MathF.Pow(12f, g.Next());
                        amp[m] = g.Range(0.3f, 1f);
                        tau[m] = g.Range(0.04f, 0.18f);
                    }
                    Dsp.Modes(x, rate, T0, 0.3f, hz, amp, tau);
                    int rattles = g.Int(2, 4);
                    for (int i = 0; i < rattles; i++)
                        Clack(x, g, T0 + g.Range(0.015f, 0.07f), Db(g.Range(-22f, -14f)), g.Range(900f, 1600f), 0.01f, 0.4f);
                }
                Dsp.Thump(x, rate, T0, 0.25f, 260f, 120f, 0.008f, 0.0004f, 0.012f);
                Noise(x, g, T0 + 0.0005f, 0.2f, 0.0002f, 0.002f, 12000f, 4500f);
                Dsp.HP(x, 100f, rate);
                return x;
            }

            /// <summary>Into timber: a hard, woody "thock" (a few quickly damped resonances) and a dry crackle of splinters.</summary>
            internal static float[] ImpactWood(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.45f);
                Noise(x, g, T0, 0.7f, 0.00005f, 0.0012f, 0f, 1200f);
                float f = g.Range(380f, 650f);
                Dsp.Modes(x, rate, T0, 0.6f, new[] { f, f * g.Range(1.8f, 2.1f), f * g.Range(2.6f, 3f), f * g.Range(3.9f, 4.4f) },
                    new[] { 1f, 0.6f, 0.4f, 0.25f }, new[] { g.Range(0.015f, 0.025f), 0.012f, 0.008f, 0.005f });
                Noise(x, g, T0, 0.45f, 0.0002f, 0.005f, 0f, 0f, g.Range(800f, 1200f), 1.2f);
                Dsp.Thump(x, rate, T0, 0.35f, 170f, 95f, 0.01f, 0.0005f, 0.015f);
                var c = new float[g.N(0.1f)];
                Dsp.Grains(c, g, 0f, 0.07f, 30, 1f, 0.0002f, u => 1f - u);
                Dsp.HP(c, 2000f, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, g.N(T0 + 0.001f), Db(-13f));
                Dsp.HP(x, 70f, rate);
                return x;
            }

            /// <summary>Through glass: a sharp crack, the pane bursting into ringing shards, pieces landing and tinkling.</summary>
            internal static float[] ImpactGlass(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(1.2f);
                NWave(x, rate, T0, 0.6f, 0.0001f);
                Noise(x, g, T0, 0.7f, 0.00003f, 0.0015f, 0f, 2000f);
                Noise(x, g, T0 + 0.001f, 0.35f, 0.002f, 0.02f, 0f, 3500f);
                int shards = g.Int(35, 55);
                for (int i = 0; i < shards; i++)
                {
                    float u = MathF.Pow(g.Next(), 2.2f);
                    float f = g.Range(2500f, 9000f);
                    Dsp.Modes(x, rate, T0 + 0.001f + 0.35f * u, g.Range(0.05f, 0.25f) * (1f - 0.7f * u), new[] { f, f * g.Range(1.6f, 2.4f) },
                        new[] { 1f, 0.5f }, new[] { g.Range(0.004f, 0.025f), 0.006f });
                }
                int falls = g.Int(10, 18);
                for (int i = 0; i < falls; i++)
                {
                    float at = T0 + g.Range(0.28f, 1f), f = g.Range(2000f, 7000f), a = g.Range(0.03f, 0.12f);
                    Dsp.Modes(x, rate, at, a, new[] { f, f * g.Range(1.5f, 2.2f) }, new[] { 1f, 0.6f }, new[] { g.Range(0.01f, 0.04f), 0.01f });
                    Noise(x, g, at, a * 0.8f, 0.00003f, 0.0004f, 0f, 3000f);
                }
                Dsp.HP(x, 300f, rate);
                return x;
            }
            #endregion

            #region Melee
            /// <summary>A weapon swung hard: a whoosh rising and falling through the band as it passes, a low push of air,
            /// a rustle of clothing as the swing starts.</summary>
            internal static float[] Swing(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.45f);
                float len = g.Range(0.3f, 0.42f), peak = g.Range(0.45f, 0.6f), lo = g.Range(250f, 380f), hi = g.Range(900f, 1500f);
                float[] w = Swept(g, len, 1.8f, t => lo + (hi - lo) * Dsp.Bell(t, len, peak), true);
                Dsp.LP(w, 3500f, rate);
                Dsp.Shape(w, rate, t => MathF.Pow(Dsp.Bell(t, len, peak), 1.6f));
                Dsp.Mix(x, w, g.N(0.004f), 1f);
                float[] a = Swept(g, len, 0.7f, t => 300f, false);
                Dsp.Shape(a, rate, t => MathF.Pow(Dsp.Bell(t, len, peak), 1.6f));
                Dsp.Mix(x, a, g.N(0.004f), Db(-8f));
                Noise(x, g, 0.005f, Db(-26f), 0.01f, 0.03f, 6000f, 1800f);
                Dsp.HP(x, 60f, rate);
                return x;
            }

            /// <summary>A blunt blow landing on a body: a heavy thud, a smack, the body's low boom, sometimes bone giving.</summary>
            internal static float[] Blow(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(0.4f);
                Dsp.Thump(x, rate, T0, 1f, g.Range(130f, 160f), g.Range(60f, 75f), 0.012f, 0.0008f, g.Range(0.03f, 0.045f));
                Noise(x, g, T0, 0.9f, 0.0003f, g.Range(0.008f, 0.014f), 2400f, 180f);
                Noise(x, g, T0 + 0.001f, 0.6f, 0.002f, g.Range(0.02f, 0.035f), 260f, 30f);
                if (g.Chance(0.5f))
                {
                    int snaps = g.Int(3, 7);
                    for (int i = 0; i < snaps; i++)
                        Noise(x, g, T0 + g.Range(0.002f, 0.03f), g.Range(0.15f, 0.35f), 0.00003f, 0.0006f, 0f, 0f, g.Range(2200f, 4200f), 2f);
                }
                Dsp.Norm(x);
                Dsp.Saturate(x, 1.5f);
                Dsp.Compress(x, rate, -14f, 3f, 0.3f, 30f);
                Dsp.HP(x, 35f, rate);
                return x;
            }
            #endregion

            #region Explosions
            /// <summary>
            /// A grenade or charge close by: the blast front (a shock and a long, heavy positive phase), the fireball's
            /// broadband roar darkening fast, a sub-bass shove, fragments and grit cracking through the air, and the
            /// place answering — walls and trees close by, the hills further off — then a long roll like thunder.
            /// </summary>
            internal static float[] Explosion(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(5.5f);

                // The front: the shock wave's crack.
                NWave(x, rate, T0, 0.5f, 0.0006f);

                // The blast and the fireball: a long, heavy positive phase, a broadband roar darkening fast, a low body
                // and a punch, all driven and squeezed into one heavy mass that holds for a few hundred milliseconds.
                var d = new float[g.N(1.6f)];
                Friedlander(d, rate, T0 + 0.0003f, 0.7f, g.Range(0.0035f, 0.005f));
                float b0 = g.Range(9000f, 12000f), b1 = g.Range(500f, 700f), fast = g.Range(0.08f, 0.12f), slow = g.Range(0.3f, 0.45f);
                float[] roar = Swept(g, 1.5f, 0.7f, t => Dsp.Glide(b0, b1, t / 0.35f), false);
                Dsp.Shape(roar, rate, t => 0.65f * Dsp.AD(t, 0.001f, fast) + 0.35f * Dsp.AD(t, 0.015f, slow));
                Dsp.Mix(d, roar, g.N(T0), 1.6f);
                Noise(d, g, T0 + 0.001f, 0.6f, 0.003f, g.Range(0.18f, 0.25f), 500f, 45f);
                Dsp.Thump(d, rate, T0, 0.6f, g.Range(85f, 100f), 42f, 0.05f, 0.002f, g.Range(0.16f, 0.22f));
                Dsp.Thump(d, rate, T0, 0.35f, g.Range(140f, 160f), 65f, 0.02f, 0.001f, 0.07f);
                Dsp.Norm(d);
                Dsp.Saturate(d, 3.5f);
                Dsp.Compress(d, rate, -20f, 4f, 0.5f, 200f);
                Dsp.Norm(d);
                Dsp.Mix(x, d, 0, 0.9f);

                // Fragments and grit cracking through the air, thickest at first.
                var c = new float[g.N(1.6f)];
                Dsp.Grains(c, g, 0.01f, 1.5f, 260, 1f, 0.0005f, u => (1f - u) * (1f - u));
                Dsp.HP(c, 1800f, rate);
                Dsp.LP(c, 9000f, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, g.N(T0), Db(-15f));

                // The place answering: walls and trees close by, the hills further off, then a long roll like thunder.
                Surroundings(x, g, T0, new Surround
                {
                    Early = 10, EarlyFrom = 0.02f, EarlyTo = 0.45f, EarlyDb = -6f, EarlyFall = 10f, EarlyLp0 = 3000f, EarlyLp1 = 700f,
                    EarlyTau = 0.05f, EarlyHp = 60f,
                    Echoes = 4, EchoFrom = 0.4f, EchoTo = 2.4f, EchoDb = -13f, EchoLp = 700f, EchoHp = 40f, EchoTau = 0.2f, EchoAttack = 0.04f,
                    LateDb = -8f, LateDelay = 0.04f, LateRise = 0.12f, LateTau = 0.9f, LateLp0 = 1800f, LateLp1 = 200f, LateHp = 35f,
                    LateTilt = 350f, Roll = 0.6f, RollHz = 1.5f,
                });
                int booms = g.Int(2, 4);
                for (int i = 0; i < booms; i++)
                    Dsp.Thump(x, rate, T0 + g.Range(0.3f, 2.2f), Db(g.Range(-20f, -14f)), g.Range(55f, 70f), 32f, 0.08f, 0.03f, g.Range(0.15f, 0.3f));
                Dsp.HP(x, 30f, rate);
                Dsp.Compress(x, rate, -18f, 2.5f, 2f, 300f);
                return x;
            }

            /// <summary>A blast a long way off: a deep, soft boom, then thunder — the blast coming back off hills and
            /// buildings for seconds in slow swells.</summary>
            internal static float[] ExplosionFar(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                const float t0 = 0.004f;
                float[] x = g.Buffer(6f);
                var d = new float[g.N(2f)];
                Friedlander(d, rate, t0, 1f, g.Range(0.006f, 0.009f));
                Noise(d, g, t0, 0.8f, 0.006f, g.Range(0.08f, 0.12f), 450f, 25f);
                Noise(d, g, t0 + 0.002f, 0.7f, 0.01f, g.Range(0.2f, 0.3f), 400f, 70f);
                Dsp.Thump(d, rate, t0, 1f, g.Range(70f, 80f), 38f, 0.1f, 0.004f, g.Range(0.3f, 0.4f));
                Dsp.LP(d, 900f, rate);
                Dsp.Norm(d);
                Dsp.Saturate(d, 1.8f);
                Dsp.Norm(d);
                Dsp.Mix(x, d, 0);
                int echoes = g.Int(5, 8);
                for (int i = 0; i < echoes; i++)
                {
                    float u = (i + g.Range(0.1f, 0.9f)) / echoes;
                    float at = t0 + 0.25f + 3.2f * u, db = -6f - 10f * u + g.Range(-3f, 3f);
                    Noise(x, g, at, Db(db), g.Range(0.02f, 0.06f), g.Range(0.2f, 0.45f), Dsp.Glide(500f, 260f, u), 30f, steep: true);
                    Dsp.Thump(x, rate, at, Db(db - 3f), g.Range(52f, 62f), 30f, 0.1f, 0.01f, g.Range(0.2f, 0.35f));
                }
                Roll(x, g, t0 + 0.08f, Db(-8f), 0.3f, g.Range(1.4f, 1.8f), 450f, 150f, 28f, 0.6f, 1.2f, 250f);
                Dsp.HP(x, 26f, rate);
                return x;
            }

            /// <summary>What a blast threw up coming down: grit pattering, pebbles knocking, clods thudding — thickest
            /// early, thinning over a couple of seconds.</summary>
            internal static float[] Debris(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                const float len = 2.4f;
                float[] x = g.Buffer(len);
                float rise = g.Range(0.12f, 0.25f), fall = g.Range(0.4f, 0.6f);
                Func<float, float> density = t => t < rise ? t / rise : MathF.Exp(-(t - rise) / fall);
                float Pick()
                {
                    for (int k = 0; k < 16; k++)
                    {
                        float t = g.Range(0.005f, len - 0.15f);
                        if (g.Next() < density(t)) return t;
                    }
                    return g.Range(0.005f, 0.8f);
                }

                var s = new float[x.Length];
                for (int i = 0; i < s.Length; i++) s[i] = g.Bipolar();
                Dsp.HP(s, 500f, rate);
                Dsp.LP(s, 5000f, rate);
                Dsp.Norm(s);
                Dsp.Shape(s, rate, density);
                Dsp.Mix(x, s, 0, Db(-16f));

                var c = new float[x.Length];
                Dsp.Grains(c, g, 0f, len - 0.1f, 500, 1f, 0.00025f, u => density(u * len));
                Dsp.HP(c, 1500f, rate);
                Dsp.Norm(c);
                Dsp.Mix(x, c, 0, Db(-9f));

                int pebbles = g.Int(28, 44);
                for (int i = 0; i < pebbles; i++)
                {
                    float t = Pick(), f = g.Range(1200f, 3800f);
                    float a = Db(g.Range(-20f, -6f)) * (0.4f + 0.6f * density(t));
                    Dsp.Modes(x, rate, t, a * 0.5f, new[] { f, f * g.Range(1.5f, 1.8f), f * g.Range(2.4f, 2.9f) }, new[] { 1f, 0.6f, 0.35f },
                        new[] { g.Range(0.002f, 0.006f), 0.003f, 0.002f });
                    Noise(x, g, t, a * 0.5f, 0.00003f, 0.0004f, 0f, 2000f);
                }
                int clods = g.Int(10, 16);
                for (int i = 0; i < clods; i++)
                {
                    float t = Pick();
                    float a = Db(g.Range(-14f, -4f)) * (0.5f + 0.5f * density(t));
                    Noise(x, g, t, a, 0.0004f, g.Range(0.004f, 0.01f), 800f, 60f);
                    Dsp.Thump(x, rate, t, a * 0.5f, g.Range(150f, 260f), 90f, 0.01f, 0.0005f, 0.015f);
                }
                for (int i = 0; i < 2; i++)
                {
                    float t = g.Range(0.03f, 0.3f);
                    Noise(x, g, t, Db(-3f), 0.0005f, 0.015f, 600f, 40f);
                    Dsp.Thump(x, rate, t, Db(-6f), 120f, 55f, 0.02f, 0.001f, 0.04f);
                }
                Dsp.HP(x, 40f, rate);
                return x;
            }
            #endregion

            #region The ears
            /// <summary>Tinnitus after a blast: a high, near-pure whine with a slow beat and a faint hiss around it,
            /// fading over four or five seconds.</summary>
            internal static float[] EarRing(SynthGen g)
            {
                g = Fresh(g);
                int rate = g.Rate;
                float[] x = g.Buffer(5f);
                float f = g.Range(3500f, 4400f), beat = g.Range(1.5f, 4f), drift = g.Range(0.15f, 0.35f);
                var env = new float[x.Length];
                float p1 = 0f, p2 = 0f;
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)rate;
                    float wob = 1f + 0.0015f * MathF.Sin(Dsp.TwoPi * drift * t);
                    p1 += f * wob / rate;
                    p2 += (f + beat) * wob / rate;
                    p1 -= MathF.Floor(p1);
                    p2 -= MathF.Floor(p2);
                    env[i] = Dsp.SmoothStep(t / 0.12f) * MathF.Exp(-t / 1.5f) * (1f - Dsp.SmoothStep((t - 3.8f) / 1.2f));
                    x[i] = (MathF.Sin(Dsp.TwoPi * p1) + 0.4f * MathF.Sin(Dsp.TwoPi * p2)) * env[i];
                }
                var h = new float[x.Length];
                for (int i = 0; i < h.Length; i++) h[i] = g.Bipolar();
                Dsp.BP(h, f, 8f, rate);
                Dsp.Norm(h);
                for (int i = 0; i < x.Length; i++) x[i] += h[i] * env[i] * Db(-22f);
                return x;
            }
            #endregion
        }
    }

    public static partial class SfxIds
    {
        /// <summary>Distant shots and blasts (the mixer swaps them in far off), the spent brass, the debris of a blast.</summary>
        public const string GunFarLight = "gun_far_light", GunFarHeavy = "gun_far_heavy", ExplosionFar = "explosion_far",
            GunSuppressedRifle = "gun_suppressed_rifle", ShellCasing = "shell_casing", ShellShotgun = "shell_shotgun", Debris = "debris";

        /// <summary>Bullets passing close (a supersonic crack, a subsonic zip) and striking the world.</summary>
        public const string BulletWhiz = "bullet_whiz", BulletFlyby = "bullet_flyby",
            ImpactDirt = "impact_dirt", ImpactConcrete = "impact_concrete", ImpactMetal = "impact_metal", ImpactWood = "impact_wood",
            ImpactGlass = "impact_glass";

        /// <summary>Reloads by kind of gun (<see cref="Reload"/> stays as the generic one).</summary>
        public const string ReloadPistol = "reload_pistol", ReloadRifle = "reload_rifle", ReloadShotgun = "reload_shotgun",
            ReloadBolt = "reload_bolt";
    }
}
