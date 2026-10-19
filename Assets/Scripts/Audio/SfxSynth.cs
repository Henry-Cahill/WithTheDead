using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Every sound in the game, synthesised in code on first use (there are no audio assets): gunshots with a crack,
    /// a body and a tail, footsteps per surface, creaking doors, the groans of the dead, struck metal and glass,
    /// anomalies, the emission siren, the helicopter, breathing in a mask, ambiences, a guitar by the bar's stage.
    /// Frequent sounds get a few seeded variations; loops are cross-faded so they wrap without a click. Mono,
    /// 22 kHz, cached.
    /// </summary>
    public sealed class SfxSynth : ISfxBackend
    {
        private const int Rate = 22050;

        private sealed class Recipe
        {
            public Func<SynthGen, float[]> Make;
            public int Variations = 1;
            public bool Loop;
            public float Peak = 0.8f;
        }

        private readonly Dictionary<string, Recipe> _recipes = new Dictionary<string, Recipe>();
        private readonly Dictionary<string, AudioClip[]> _cache = new Dictionary<string, AudioClip[]>();
        private readonly System.Random _pick = new System.Random();

        public SfxSynth() => Register();

        public bool Has(string id) => id != null && _recipes.ContainsKey(id);

        public AudioClip Get(string id)
        {
            if (id == null || !_recipes.TryGetValue(id, out Recipe r)) return null;
            if (!_cache.TryGetValue(id, out AudioClip[] clips))
            {
                clips = new AudioClip[r.Variations];
                _cache[id] = clips;
            }
            int v = r.Variations > 1 ? _pick.Next(r.Variations) : 0;
            if (clips[v] == null) clips[v] = Make(id, r, v);
            return clips[v];
        }

        private static AudioClip Make(string id, Recipe r, int variation)
        {
            var g = new SynthGen(Rate, (uint)(id.GetHashCode() & 0x7fffffff) + 17u, variation);
            float[] x;
            try { x = r.Make(g); }
            catch (Exception e) { Debug.LogWarning("[CloneZ] Sound '" + id + "' failed: " + e.Message); x = new float[64]; }
            Dsp.Sanitize(x);
            Dsp.RemoveMean(x);
            Dsp.Norm(x, r.Peak);
            if (!r.Loop)
            {
                Dsp.FadeIn(x, Math.Min(32, x.Length / 4));
                Dsp.FadeOut(x, Math.Min(Rate / 50, x.Length / 3));
            }
            var clip = AudioClip.Create(id + "_" + variation, Math.Max(1, x.Length), 1, Rate, false);
            clip.SetData(x, 0);
            return clip;
        }

        private void R(string id, Func<SynthGen, float[]> make, int variations = 1, bool loop = false, float peak = 0.8f)
            => _recipes[id] = new Recipe { Make = make, Variations = variations, Loop = loop, Peak = peak };

        // A loop: render a little longer than wanted, then cross-fade the tail into the head.
        private static float[] Loop(SynthGen g, float seconds, Action<float[]> fill)
        {
            int len = g.N(seconds);
            var x = new float[len + g.N(0.3f)];
            fill(x);
            return Dsp.CrossfadeLoop(x, len);
        }

        private void Register()
        {
            // ── Guns ────────────────────────────────────────────────────────────────────────────────
            R(SfxIds.GunPistol, g => Gun(g, 0.9f, 200f, 60f, 0.07f, 5500f, 0.11f, 0.3f), 3, peak: 0.95f);
            R(SfxIds.GunRifle, g => Gun(g, 1.3f, 150f, 45f, 0.09f, 7000f, 0.16f, 0.42f), 3, peak: 0.95f);
            R(SfxIds.GunShotgun, g => Gun(g, 1.2f, 110f, 38f, 0.13f, 3600f, 0.2f, 0.4f), 3, peak: 0.95f);
            R(SfxIds.GunSniper, g => Gun(g, 2.4f, 120f, 34f, 0.12f, 8000f, 0.28f, 0.55f), 2, peak: 0.95f);
            R(SfxIds.GunSuppressed, g =>
            {
                float[] x = g.Buffer(0.45f);
                Dsp.AddBurst(x, g, 0f, 0.7f, 0.001f, 0.02f, 1800f);
                Dsp.Thump(x, Rate, 0f, 0.5f, 160f, 70f, 0.02f, 0.001f, 0.04f);
                Dsp.AddBurst(x, g, 0.005f, 0.35f, 0.0005f, 0.004f, 0f, 2500f);
                return x;
            }, 3);
            R(SfxIds.GunFlare, g =>
            {
                float[] x = g.Buffer(1.4f);
                Dsp.Thump(x, Rate, 0f, 0.6f, 160f, 60f, 0.04f, 0.001f, 0.08f);
                float[] w = Dsp.White(g, x.Length, 0.6f);
                Dsp.SweepBP(w, Rate, 2f, u => 400f + 2400f * u);
                Dsp.Shape(w, Rate, t => Dsp.AD(t, 0.05f, 0.4f));
                Dsp.Mix(x, w, 0);
                return x;
            });
            R(SfxIds.DryFire, g => Click(g, 2600f, 0.012f), 2);
            R(SfxIds.Reload, g =>
            {
                float[] x = g.Buffer(0.8f);
                AddClick(x, g, 0f, 1800f, 0.02f, 0.9f);
                Dsp.Grains(x, g, 0.2f, 0.45f, 30, 0.25f, 0.002f, u => 1f - u);
                AddClick(x, g, 0.55f, 2200f, 0.015f, 1f);
                return x;
            }, 2);
            R(SfxIds.Jam, g =>
            {
                float[] x = g.Buffer(0.5f);
                AddClick(x, g, 0f, 1400f, 0.02f, 1f);
                AddClick(x, g, 0.18f, 1200f, 0.025f, 0.8f);
                return x;
            });
            R(SfxIds.BoltThrow, g => Whoosh(g, 0.3f, 900f, 2400f, 0.5f), 2);
            R(SfxIds.BoltLand, g =>
            {
                float[] x = g.Buffer(0.6f);
                Dsp.Ring(x, Rate, 0f, 0.6f, g.Vary(3100f, 0.08f), new[] { 1f, 2.41f, 4.07f, 6.3f }, 0.09f);
                Dsp.AddBurst(x, g, 0f, 0.4f, 0.0005f, 0.004f, 0f, 2000f);
                return x;
            }, 3);

            // ── Footsteps ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.StepSnow, g => { float[] x = g.Buffer(0.25f); Dsp.Grains(x, g, 0f, 0.16f, 70, 0.6f, 0.0015f, u => Dsp.Bell(u, 1f, 0.3f)); Dsp.LP(x, 4500f, Rate); return x; }, 4);
            R(SfxIds.StepWood, g => { float[] x = g.Buffer(0.25f); Dsp.Thump(x, Rate, 0f, 0.8f, g.Vary(140f, 0.1f), 90f, 0.02f, 0.002f, 0.04f); Dsp.AddBurst(x, g, 0f, 0.3f, 0.001f, 0.015f, 1200f); return x; }, 4);
            R(SfxIds.StepConcrete, g => { float[] x = g.Buffer(0.2f); Dsp.AddBurst(x, g, 0f, 0.6f, 0.0008f, 0.01f, 0f, 1400f); Dsp.Thump(x, Rate, 0f, 0.3f, 220f, 140f, 0.01f, 0.001f, 0.02f); return x; }, 4);
            R(SfxIds.StepGrass, g => { float[] x = g.Buffer(0.3f); Dsp.Grains(x, g, 0f, 0.2f, 40, 0.4f, 0.003f, u => Dsp.Bell(u, 1f, 0.4f)); Dsp.BP(x, 2500f, 0.8f, Rate); return x; }, 4);

            // ── Doors and gear ──────────────────────────────────────────────────────────────────────
            R(SfxIds.DoorOpen, g => Dsp.Creak(g, 0.9f, u => 18f + 30f * u, u => Dsp.Bell(u, 1f, 0.4f), new[] { 480f, 1150f, 2300f }, new[] { 6f, 8f, 10f }, 0.35f), 2);
            R(SfxIds.DoorClose, g => { float[] x = g.Buffer(0.6f); Dsp.Thump(x, Rate, 0f, 1f, 110f, 60f, 0.03f, 0.002f, 0.12f); Dsp.AddBurst(x, g, 0f, 0.4f, 0.001f, 0.03f, 900f); return x; }, 2);
            R(SfxIds.DoorBash, g => { float[] x = g.Buffer(0.8f); Dsp.Thump(x, Rate, 0f, 1f, 95f, 50f, 0.04f, 0.002f, 0.18f); Dsp.AddBurst(x, g, 0f, 0.7f, 0.001f, 0.06f, 2500f); Dsp.Grains(x, g, 0.02f, 0.3f, 25, 0.3f, 0.003f, u => 1f - u); return x; }, 3);
            R(SfxIds.GearRustle, g => { float[] x = g.Buffer(0.4f); Dsp.Grains(x, g, 0f, 0.35f, 60, 0.4f, 0.004f, u => Dsp.Bell(u, 1f, 0.5f)); Dsp.BP(x, 1800f, 0.7f, Rate); return x; }, 3);

            // ── The dead ────────────────────────────────────────────────────────────────────────────
            R(SfxIds.ZombieGroan, g => Dsp.Voice(g, g.Range(1.1f, 1.7f), new VoiceParams
            {
                F0 = u => g.Vary(85f, 0.15f) + 18f * MathF.Sin(u * 7f), Formants = u => Vowel.Lerp(Vowel.Uh, Vowel.O, u),
                Amp = u => Dsp.Bell(u, 1f, 0.35f), Rough = 0.75f, Gurgle = 0.45f, Breath = 0.4f, Jitter = 0.03f,
            }), 4);
            R(SfxIds.ZombieAttack, g => Dsp.Voice(g, 0.6f, new VoiceParams
            {
                F0 = u => 150f - 40f * u, Formants = u => Vowel.A, Amp = u => Dsp.AD(u, 0.08f, 0.35f), Rough = 0.95f, Gurgle = 0.3f, Breath = 0.5f,
            }), 3);
            R(SfxIds.ZombieDie, g => Dsp.Voice(g, 1.2f, new VoiceParams
            {
                F0 = u => Dsp.Glide(120f, 55f, u), Formants = u => Vowel.Lerp(Vowel.Aw, Vowel.U, u), Amp = u => Dsp.Bell(u, 1f, 0.2f), Rough = 0.8f, Gurgle = 0.8f, Breath = 0.45f,
            }), 2);

            // ── Impacts ─────────────────────────────────────────────────────────────────────────────
            R(SfxIds.HitFlesh, g => { float[] x = g.Buffer(0.3f); Dsp.Thump(x, Rate, 0f, 1f, 150f, 80f, 0.02f, 0.001f, 0.05f); Dsp.AddBurst(x, g, 0f, 0.5f, 0.001f, 0.03f, 1200f); return x; }, 3);
            R(SfxIds.HitArmor, g => { float[] x = g.Buffer(0.9f); Dsp.Ring(x, Rate, 0f, 0.7f, g.Vary(900f, 0.1f), new[] { 1f, 2.76f, 5.4f, 8.93f }, 0.25f); Dsp.AddBurst(x, g, 0f, 0.6f, 0.0005f, 0.006f, 0f, 2000f); return x; }, 3);
            R(SfxIds.Ricochet, g =>
            {
                float[] x = g.Buffer(0.5f);
                float ph = 0f;
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = Dsp.Glide(3200f, 1100f, t / 0.45f) * (1f + 0.03f * MathF.Sin(t * 90f));
                    ph += f / Rate;
                    x[i] = MathF.Sin(Dsp.TwoPi * ph) * Dsp.AD(t, 0.005f, 0.12f);
                }
                Dsp.AddBurst(x, g, 0f, 0.5f, 0.0005f, 0.003f, 0f, 2500f);
                return x;
            }, 2);
            R(SfxIds.MeleeSwing, g => Whoosh(g, 0.28f, 500f, 1500f, 0.7f), 3);
            R(SfxIds.MeleeHit, g => { float[] x = g.Buffer(0.3f); Dsp.Thump(x, Rate, 0f, 1f, 120f, 70f, 0.02f, 0.001f, 0.06f); Dsp.AddBurst(x, g, 0f, 0.45f, 0.001f, 0.02f, 1500f); return x; }, 3);
            R(SfxIds.Explosion, g =>
            {
                float[] x = g.Buffer(2.8f);
                Dsp.Thump(x, Rate, 0f, 1f, 75f, 28f, 0.15f, 0.003f, 0.6f);
                float[] b = Dsp.Brown(g, x.Length);
                Dsp.LP(b, 700f, Rate);
                Dsp.Shape(b, Rate, t => Dsp.AD(t, 0.005f, 0.7f));
                Dsp.Norm(b, 0.8f);
                Dsp.Mix(x, b, 0);
                Dsp.Grains(x, g, 0.15f, 1.6f, 120, 0.2f, 0.004f, u => 1f - u);
                Dsp.Echo(x, Rate, 0.28f, 0.35f, 1200f, 0.35f);
                return x;
            }, 2, peak: 1f);
            R(SfxIds.Grenade, g => { float[] x = g.Buffer(0.5f); AddClick(x, g, 0f, 3400f, 0.03f, 1f); AddClick(x, g, 0.2f, 2800f, 0.05f, 0.7f); return x; });

            // ── Zone gear ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.DetectorBeep, g => Tone(g, 0.07f, 1800f, 0.005f, 0.03f));
            R(SfxIds.DetectorAlert, g => { float[] x = g.Buffer(0.25f); MixTone(x, 0f, 2400f, 0.004f, 0.02f, 1f); MixTone(x, 0.11f, 2400f, 0.004f, 0.02f, 1f); return x; });
            R(SfxIds.Geiger, g => { float[] x = g.Buffer(0.03f); Dsp.AddBurst(x, g, 0f, 1f, 0.0002f, 0.0012f, 0f, 3000f); return x; }, 3);

            // ── Anomalies ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.AnomalyHum, g => Loop(g, 3f, x =>
            {
                float f1 = Dsp.Snap(55f, 3f), f2 = Dsp.Snap(110.5f, 3f), f3 = Dsp.Snap(83f, 3f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.5f * MathF.Sin(Dsp.TwoPi * f1 * t) + 0.25f * MathF.Sin(Dsp.TwoPi * f2 * t) + 0.18f * MathF.Sin(Dsp.TwoPi * f3 * t);
                }
                float[] p = Dsp.Pink(g, x.Length);
                Dsp.LP(p, 500f, Rate);
                Dsp.Mix(x, p, 0, 0.2f);
            }), loop: true, peak: 0.6f);
            R(SfxIds.AnomalyGravity, g =>
            {
                float[] x = Dsp.White(g, g.N(1.6f), 0.8f);
                Dsp.SweepLP(x, Rate, 3f, u => 150f + 1300f * Dsp.Bell(u, 1f, 0.5f));
                Dsp.Shape(x, Rate, t => Dsp.Bell(t, 1.6f, 0.5f));
                Dsp.Thump(x, Rate, 0.7f, 0.9f, 60f, 30f, 0.2f, 0.01f, 0.5f);
                return x;
            }, 2);
            R(SfxIds.AnomalyFire, g =>
            {
                float[] x = Dsp.Brown(g, g.N(1.5f));
                Dsp.LP(x, 1000f, Rate);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 0.08f, 1f, 0.4f));
                Dsp.Grains(x, g, 0f, 1.4f, 160, 0.25f, 0.002f, u => 1f);
                return x;
            }, 2);
            R(SfxIds.AnomalyElectric, g =>
            {
                float[] x = g.Buffer(0.9f);
                Dsp.Grains(x, g, 0f, 0.8f, 220, 0.6f, 0.0008f, u => 1f - u * 0.5f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] += 0.25f * Dsp.Square(Dsp.Wrap(t * 120f), 120f / Rate) * Dsp.ASR(t, 0.01f, 0.6f, 0.2f);
                }
                Dsp.HP(x, 300f, Rate);
                return x;
            }, 3);
            R(SfxIds.AnomalyAcid, g =>
            {
                float[] x = Dsp.White(g, g.N(1.2f), 0.4f);
                Dsp.HP(x, 2500f, Rate);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 0.05f, 0.8f, 0.3f));
                for (int b = 0; b < 8; b++) Dsp.Mix(x, Dsp.Bubble(Rate, g.Range(300f, 900f), 0.12f, 1.4f), g.N(g.Range(0f, 1f)), 0.6f);
                return x;
            }, 2);
            R(SfxIds.AnomalyTrigger, g => { float[] x = g.Buffer(1f); Dsp.AddBurst(x, g, 0f, 1f, 0.001f, 0.08f, 5000f); Dsp.Thump(x, Rate, 0f, 0.9f, 140f, 40f, 0.06f, 0.002f, 0.3f); return x; }, 2);

            // ── Emissions and psi ───────────────────────────────────────────────────────────────────
            R(SfxIds.EmissionSiren, g => Loop(g, 4f, x =>
            {
                float ph = 0f;
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = 650f + 260f * MathF.Sin(Dsp.TwoPi * t / 4f);
                    ph += f / Rate;
                    float s = MathF.Sin(Dsp.TwoPi * ph);
                    x[i] = 0.6f * s + 0.25f * MathF.Sin(Dsp.TwoPi * ph * 2f) + 0.1f * MathF.Sin(Dsp.TwoPi * ph * 3f);
                }
                Dsp.Echo(x, Rate, 0.35f, 0.3f, 1500f, 0.4f, true);
            }), loop: true, peak: 0.7f);
            R(SfxIds.EmissionRumble, g =>
            {
                float[] x = Dsp.Brown(g, g.N(4f));
                Dsp.LP(x, 140f, Rate);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 1f, 2f, 1f));
                for (int k = 0; k < 5; k++) Dsp.Thump(x, Rate, g.Range(0.2f, 3.5f), 0.7f, 50f, 25f, 0.3f, 0.05f, 0.6f);
                return x;
            });
            R(SfxIds.PsiWhisper, g => Whisper(g, 1.6f), 3);

            // ── Machines ────────────────────────────────────────────────────────────────────────────
            R(SfxIds.HeliRotor, g => Loop(g, 2f, x =>
            {
                float blade = Dsp.Snap(4.5f, 2f);
                int beats = Mathf.RoundToInt(blade * 2f);
                for (int b = 0; b < beats; b++)
                {
                    var thump = new float[g.N(0.18f)];
                    Dsp.Thump(thump, Rate, 0f, 1f, 95f, 55f, 0.03f, 0.004f, 0.05f);
                    Dsp.AddBurst(thump, g, 0f, 0.35f, 0.003f, 0.04f, 700f);
                    Dsp.MixWrap(x, thump, g.N(b / blade));
                }
                float whine = Dsp.Snap(1180f, 2f);
                for (int i = 0; i < x.Length; i++) x[i] += 0.06f * MathF.Sin(Dsp.TwoPi * whine * i / Rate);
                float[] n = Dsp.Pink(g, x.Length);
                Dsp.LP(n, 900f, Rate);
                Dsp.Mix(x, n, 0, 0.25f);
            }), loop: true);
            R(SfxIds.FlareBurn, g => Loop(g, 2f, x =>
            {
                float[] n = Dsp.White(g, x.Length, 0.5f);
                Dsp.BP(n, 2000f, 0.7f, Rate);
                Dsp.Mix(x, n, 0);
                Dsp.Grains(x, g, 0f, 2.2f, 90, 0.3f, 0.0015f, u => 1f);
            }), loop: true, peak: 0.6f);
            R(SfxIds.EngineLoop, g => Loop(g, 1f, x =>
            {
                float fire = Dsp.Snap(28f, 1f), saw = Dsp.Snap(42f, 1f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.5f * Dsp.Saw(Dsp.Wrap(saw * t), saw / Rate) + 0.5f * MathF.Pow(MathF.Max(0f, MathF.Sin(Dsp.TwoPi * fire * t)), 6f);
                }
                Dsp.LoopLP(x, 600f, Rate);
                float[] n = Dsp.Brown(g, x.Length);
                Dsp.Mix(x, n, 0, 0.15f);
            }), loop: true, peak: 0.6f);
            R(SfxIds.EngineStartFail, g =>
            {
                float[] x = g.Buffer(1.6f);
                for (int i = 0; i < g.N(1.1f); i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.5f * Dsp.Square(Dsp.Wrap(t * (55f + 8f * MathF.Sin(t * 17f))), 60f / Rate) * Dsp.ASR(t, 0.05f, 0.9f, 0.15f);
                }
                Dsp.LP(x, 700f, Rate);
                Dsp.Thump(x, Rate, 1.15f, 0.8f, 80f, 40f, 0.05f, 0.005f, 0.12f);
                return x;
            });
            R(SfxIds.DroneBuzz, g => Loop(g, 1f, x =>
            {
                float a = Dsp.Snap(180f, 1f), b = Dsp.Snap(183f, 1f);
                for (int i = 0; i < x.Length; i++)
                {
                    float t = i / (float)Rate;
                    x[i] = 0.4f * Dsp.Saw(Dsp.Wrap(a * t), a / Rate) + 0.4f * Dsp.Saw(Dsp.Wrap(b * t), b / Rate);
                }
                Dsp.LoopBP(x, 900f, 0.8f, Rate);
            }), loop: true, peak: 0.5f);

            // ── The body ────────────────────────────────────────────────────────────────────────────
            R(SfxIds.BreathMask, g => Loop(g, 3.2f, x =>
            {
                float[] n = Dsp.Pink(g, x.Length);
                for (int i = 0; i < n.Length; i++)
                {
                    float t = (i / (float)Rate) % 3.2f;
                    float env = t < 1.3f ? Dsp.Bell(t, 1.3f, 0.4f) : Dsp.Bell(t - 1.5f, 1.5f, 0.3f) * 0.8f;
                    n[i] *= env;
                }
                Dsp.BP(n, 900f, 1.2f, Rate);
                Dsp.Mix(x, n, 0);
                AddClick(x, g, 1.4f, 1500f, 0.01f, 0.3f);
            }), loop: true, peak: 0.55f);
            R(SfxIds.MaskCrack, g => { float[] x = g.Buffer(0.8f); Dsp.Ring(x, Rate, 0f, 0.8f, 2600f, new[] { 1f, 1.73f, 2.9f, 4.4f, 6.1f }, 0.12f); Dsp.Grains(x, g, 0f, 0.25f, 40, 0.5f, 0.0008f, u => 1f - u); return x; });
            R(SfxIds.VisorWipe, g => { float[] x = Dsp.White(g, g.N(0.5f), 0.4f); Dsp.SweepBP(x, Rate, 4f, u => 1200f + 1800f * u); Dsp.Shape(x, Rate, t => Dsp.Bell(t, 0.5f, 0.5f)); return x; });
            R(SfxIds.Heartbeat, g => { float[] x = g.Buffer(0.9f); Dsp.Thump(x, Rate, 0f, 1f, 70f, 45f, 0.03f, 0.005f, 0.06f); Dsp.Thump(x, Rate, 0.22f, 0.7f, 65f, 45f, 0.03f, 0.005f, 0.06f); return x; });
            R(SfxIds.Sneeze, g =>
            {
                float[] x = Dsp.White(g, g.N(0.9f), 0.5f);
                Dsp.SweepBP(x, Rate, 1.5f, u => 600f + 1800f * u);
                Dsp.Shape(x, Rate, t => t < 0.55f ? Dsp.Bell(t, 0.55f, 0.8f) * 0.35f : Dsp.AD(t - 0.55f, 0.01f, 0.08f));
                return x;
            }, 2);
            R(SfxIds.Cough, g =>
            {
                float[] x = g.Buffer(0.9f);
                for (int k = 0; k < 3; k++) Dsp.AddBurstBP(x, g, k * 0.25f, 1f - k * 0.2f, 0.005f, 0.06f, 550f, 1.2f);
                return x;
            }, 2);

            // ── Ambience and music ──────────────────────────────────────────────────────────────────
            R(SfxIds.WindAmbience, g => Loop(g, 8f, x =>
            {
                float[] p = Dsp.Pink(g, x.Length);
                float f = 1f / 8f;
                Dsp.SweepBP(p, Rate, 0.8f, u => 380f + 700f * (0.5f + 0.5f * MathF.Sin(Dsp.TwoPi * (u * 8f * f * 2f))));
                Dsp.Mix(x, p, 0);
            }), loop: true, peak: 0.5f);
            R(SfxIds.ForestAmbience, g => Loop(g, 10f, x =>
            {
                float[] p = Dsp.Pink(g, x.Length);
                Dsp.LP(p, 900f, Rate);
                Dsp.Mix(x, p, 0, 0.6f);
                for (int b = 0; b < 4; b++)
                {
                    var chirp = new float[g.N(0.25f)];
                    float f0 = g.Range(2500f, 4200f), ph = 0f;
                    for (int i = 0; i < chirp.Length; i++)
                    {
                        float t = i / (float)Rate;
                        ph += (f0 + 900f * MathF.Sin(t * 60f)) / Rate;
                        chirp[i] = MathF.Sin(Dsp.TwoPi * ph) * Dsp.Bell(t, 0.25f, 0.3f) * 0.25f;
                    }
                    Dsp.MixWrap(x, chirp, g.N(g.Range(0f, 9.5f)));
                }
                Dsp.MixWrap(x, Dsp.Creak(g, 1.2f, u => 9f + 5f * u, u => Dsp.Bell(u, 1f, 0.5f) * 0.4f, new[] { 300f, 700f }, new[] { 5f, 6f }, 0.4f), g.N(4f));
            }), loop: true, peak: 0.45f);
            R(SfxIds.BarAmbience, g => Loop(g, 10f, x =>
            {
                for (int v = 0; v < 14; v++)
                {
                    float[] voice = Dsp.Voice(g, g.Range(0.8f, 1.8f), new VoiceParams
                    {
                        F0 = u => g.Vary(g.Chance(0.5f) ? 120f : 200f, 0.15f) * (1f + 0.1f * MathF.Sin(u * 9f)),
                        Formants = u => Vowel.Speech[(int)(u * 6f) % Vowel.Speech.Length], Amp = u => Dsp.Bell(u, 1f, 0.5f), Breath = 0.2f,
                    });
                    Dsp.MixWrap(x, voice, g.N(g.Range(0f, 9.8f)), 0.25f);
                }
                for (int c = 0; c < 5; c++)
                {
                    var clink = new float[g.N(0.4f)];
                    Dsp.Ring(clink, Rate, 0f, 0.3f, g.Range(2600f, 3600f), new[] { 1f, 2.3f, 3.9f }, 0.1f);
                    Dsp.MixWrap(x, clink, g.N(g.Range(0f, 9.8f)));
                }
                Dsp.LoopLP(x, 2500f, Rate);
                Dsp.Reverb(x, Rate, 0.5f, 0.5f, 0.3f, 1f, true);
            }), loop: true, peak: 0.45f);
            R(SfxIds.Guitar, g => Loop(g, 12f, x =>
            {
                float[] notes = { 57, 60, 64, 69, 67, 64, 60, 62, 55, 59, 62, 67, 64, 62, 59, 57 };
                float beat = 0.75f;
                for (int i = 0; i < notes.Length; i++)
                {
                    float[] pluck = Pluck(g, Dsp.Midi(notes[i]), 1.6f);
                    Dsp.MixWrap(x, pluck, g.N(i * beat), 0.5f);
                    if (i % 4 == 0) Dsp.MixWrap(x, Pluck(g, Dsp.Midi(notes[i] - 12f), 2.2f), g.N(i * beat), 0.35f);
                }
                Dsp.Reverb(x, Rate, 0.4f, 0.6f, 0.25f, 1f, true);
            }), loop: true, peak: 0.6f);

            // ── Animals and mutants ─────────────────────────────────────────────────────────────────
            R(SfxIds.WolfHowl, g =>
            {
                float len = 2.6f;
                float[] x = g.Buffer(len + 1f);
                float ph = 0f;
                for (int i = 0; i < g.N(len); i++)
                {
                    float u = i / (float)g.N(len);
                    float f = 380f + 260f * Dsp.Bell(u, 1f, 0.3f) - 80f * u;
                    ph += f * (1f + 0.01f * MathF.Sin(i * 0.002f)) / Rate;
                    x[i] = (MathF.Sin(Dsp.TwoPi * ph) + 0.2f * MathF.Sin(Dsp.TwoPi * ph * 2f)) * Dsp.Bell(u, 1f, 0.25f);
                }
                Dsp.Reverb(x, Rate, 0.8f, 0.4f, 0.45f);
                return x;
            }, 2);
            R(SfxIds.BearRoar, g => Dsp.Voice(g, 1.6f, new VoiceParams { F0 = u => 75f + 25f * Dsp.Bell(u, 1f, 0.3f), Formants = u => Vowel.A, Amp = u => Dsp.Bell(u, 1f, 0.25f), Rough = 1f, Breath = 0.6f }), 2);
            R(SfxIds.MutantScreech, g =>
            {
                float[] x = Dsp.Voice(g, 1f, new VoiceParams { F0 = u => 700f + 400f * u, Formants = u => Vowel.I, Amp = u => Dsp.AD(u, 0.05f, 0.35f), Rough = 0.9f, Breath = 0.6f });
                float[] n = Dsp.White(g, x.Length, 0.5f);
                Dsp.SweepBP(n, Rate, 3f, u => 1500f + 3000f * u);
                Dsp.Shape(n, Rate, t => Dsp.AD(t, 0.02f, 0.3f));
                Dsp.Mix(x, n, 0);
                return x;
            }, 2);
            R(SfxIds.MutantGrowl, g => Dsp.Voice(g, 1.3f, new VoiceParams { F0 = u => 55f + 10f * MathF.Sin(u * 12f), Formants = u => Vowel.U, Amp = u => Dsp.Bell(u, 1f, 0.4f), Rough = 1f, Gurgle = 0.6f, Breath = 0.4f }), 3);
            R(SfxIds.DeerCall, g => Dsp.Voice(g, 0.5f, new VoiceParams { F0 = u => 520f - 120f * u, Formants = u => Vowel.E, Amp = u => Dsp.Bell(u, 1f, 0.3f), Breath = 0.3f }));

            // ── Interface ───────────────────────────────────────────────────────────────────────────
            R(SfxIds.UiClick, g => Tone(g, 0.03f, 2200f, 0.001f, 0.008f), peak: 0.4f);
            R(SfxIds.UiBuy, g => { float[] x = g.Buffer(0.5f); Dsp.Ring(x, Rate, 0f, 0.5f, 1320f, new[] { 1f, 2f, 3f }, 0.15f); Dsp.Ring(x, Rate, 0.09f, 0.5f, 1760f, new[] { 1f, 2f, 3f }, 0.2f); return x; }, peak: 0.5f);
            R(SfxIds.UiError, g => { float[] x = g.Buffer(0.25f); for (int i = 0; i < x.Length; i++) x[i] = 0.4f * Dsp.Square(Dsp.Wrap(i * 180f / Rate), 180f / Rate) * Dsp.ASR(i / (float)Rate, 0.005f, 0.18f, 0.05f); Dsp.LP(x, 1500f, Rate); return x; }, peak: 0.4f);
            R(SfxIds.Cash, g => { float[] x = g.Buffer(0.7f); for (int k = 0; k < 5; k++) Dsp.Ring(x, Rate, g.Range(0f, 0.35f), 0.4f, g.Range(3000f, 4200f), new[] { 1f, 2.6f, 4.9f }, 0.08f); return x; }, 2, peak: 0.5f);

            // ── The uncanny ─────────────────────────────────────────────────────────────────────────
            R(SfxIds.UncannyWhisper, g => Whisper(g, 2.2f), 3, peak: 0.5f);
            R(SfxIds.RadioStatic, g =>
            {
                float[] x = Dsp.White(g, g.N(1.6f), 0.6f);
                Dsp.BP(x, 1900f, 1f, Rate);
                Dsp.Grains(x, g, 0f, 1.5f, 60, 0.6f, 0.001f, u => 1f);
                Dsp.Shape(x, Rate, t => Dsp.ASR(t, 0.05f, 1.3f, 0.2f));
                return x;
            }, 2, peak: 0.55f);
            R(SfxIds.Stinger, g =>
            {
                float len = 2.6f;
                float[] x = g.Buffer(len + 0.8f);
                float[] hz = { 220f, 233.1f, 311.1f, 466.2f };
                for (int i = 0; i < g.N(len); i++)
                {
                    float t = i / (float)Rate;
                    float env = MathF.Pow(t / len, 2.2f);
                    float s = 0f;
                    for (int k = 0; k < hz.Length; k++) s += MathF.Sin(Dsp.TwoPi * hz[k] * t * (1f + 0.002f * k * t));
                    x[i] = s * 0.25f * env;
                }
                Dsp.AddBurst(x, g, len - 0.05f, 1f, 0.002f, 0.25f, 3000f);
                Dsp.Reverb(x, Rate, 0.9f, 0.3f, 0.5f);
                return x;
            }, 2, peak: 0.7f);

            // ── Small actions ───────────────────────────────────────────────────────────────────────
            R(SfxIds.Pickup, g => { float[] x = g.Buffer(0.25f); Dsp.Grains(x, g, 0f, 0.2f, 30, 0.4f, 0.003f, u => Dsp.Bell(u, 1f, 0.3f)); Dsp.BP(x, 2000f, 0.8f, Rate); return x; }, 2, peak: 0.45f);
            R(SfxIds.Eat, g => { float[] x = g.Buffer(0.6f); Dsp.Grains(x, g, 0f, 0.5f, 50, 0.5f, 0.002f, u => 0.5f + 0.5f * MathF.Sin(u * 30f)); Dsp.LP(x, 3000f, Rate); return x; }, 2, peak: 0.5f);
            R(SfxIds.Drink, g => { float[] x = g.Buffer(0.9f); for (int k = 0; k < 3; k++) Dsp.Mix(x, Dsp.Bubble(Rate, g.Range(250f, 420f), 0.18f, 1.6f), g.N(k * 0.28f), 0.8f); return x; }, peak: 0.5f);
            R(SfxIds.Bandage, g => { float[] x = g.Buffer(0.7f); Dsp.Grains(x, g, 0f, 0.6f, 120, 0.5f, 0.0012f, u => 1f - u * 0.5f); Dsp.BP(x, 3200f, 0.9f, Rate); return x; }, 2, peak: 0.5f);
            R(SfxIds.Injector, g => { float[] x = g.Buffer(0.6f); AddClick(x, g, 0f, 2400f, 0.01f, 0.8f); float[] h = Dsp.White(g, g.N(0.4f), 0.3f); Dsp.HP(h, 3500f, Rate); Dsp.Shape(h, Rate, t => Dsp.AD(t, 0.02f, 0.15f)); Dsp.Mix(x, h, g.N(0.05f)); return x; }, peak: 0.5f);
            R(SfxIds.PageTurn, g => { float[] x = Dsp.White(g, g.N(0.4f), 0.5f); Dsp.SweepBP(x, Rate, 1.5f, u => 1500f + 2000f * u); Dsp.Shape(x, Rate, t => Dsp.Bell(t, 0.4f, 0.4f)); return x; }, 2, peak: 0.45f);
            R(SfxIds.Splash, g => { float[] x = Dsp.White(g, g.N(0.9f), 0.7f); Dsp.LP(x, 3000f, Rate); Dsp.Shape(x, Rate, t => Dsp.AD(t, 0.01f, 0.15f)); for (int k = 0; k < 4; k++) Dsp.Mix(x, Dsp.Bubble(Rate, g.Range(400f, 900f), 0.1f, 1.5f), g.N(g.Range(0.05f, 0.6f)), 0.4f); return x; }, 2);
            R(SfxIds.Crank, g => { float[] x = g.Buffer(1.1f); for (int k = 0; k < 12; k++) AddClick(x, g, k / 12f, 1700f + (k % 2) * 300f, 0.012f, 0.6f); return x; }, peak: 0.6f);
            R(SfxIds.Lighter, g => { float[] x = g.Buffer(0.8f); AddClick(x, g, 0f, 3000f, 0.006f, 0.8f); float[] w = Dsp.White(g, g.N(0.6f), 0.3f); Dsp.BP(w, 800f, 0.7f, Rate); Dsp.Shape(w, Rate, t => Dsp.AD(t, 0.05f, 0.25f)); Dsp.Mix(x, w, g.N(0.05f)); return x; }, peak: 0.5f);
            R(SfxIds.Handcuffs, g => { float[] x = g.Buffer(0.7f); for (int k = 0; k < 6; k++) Dsp.Ring(x, Rate, k * 0.05f, 0.4f, 2800f, new[] { 1f, 2.2f, 3.7f }, 0.03f); return x; }, peak: 0.6f);
        }

        #region Building blocks
        // A gunshot: the supersonic crack, the bloom of the charge, and the tail rolling off the surroundings.
        private static float[] Gun(SynthGen g, float len, float bodyHz, float bodyEnd, float bodyTau, float crackLp, float tailTau, float echo)
        {
            float[] x = g.Buffer(len);
            Dsp.AddBurst(x, g, 0f, 1f, 0.0004f, 0.006f, crackLp);
            Dsp.Thump(x, Rate, 0f, 0.95f, g.Vary(bodyHz, 0.06f), bodyEnd, 0.025f, 0.0008f, bodyTau);
            Dsp.AddBurst(x, g, 0.002f, 0.55f, 0.002f, tailTau, 1800f);
            Dsp.Echo(x, Rate, g.Range(0.09f, 0.16f), echo, 2200f, 0.35f);
            return x;
        }

        private static float[] Whoosh(SynthGen g, float len, float hz0, float hz1, float q)
        {
            float[] x = Dsp.White(g, g.N(len), 0.8f);
            Dsp.SweepBP(x, Rate, 1f / Math.Max(0.1f, q), u => Dsp.Lerp(hz0, hz1, u));
            Dsp.Shape(x, Rate, t => Dsp.Bell(t, len, 0.45f));
            return x;
        }

        private static float[] Click(SynthGen g, float hz, float tau)
        {
            float[] x = g.Buffer(0.15f);
            AddClick(x, g, 0f, hz, tau, 1f);
            return x;
        }

        private static void AddClick(float[] x, SynthGen g, float at, float hz, float tau, float gain)
        {
            Dsp.Ring(x, Rate, at, gain * 0.6f, g.Vary(hz, 0.05f), new[] { 1f, 2.7f, 5.4f }, tau);
            Dsp.AddBurst(x, g, at, gain * 0.5f, 0.0003f, 0.002f, 0f, 1500f);
        }

        private static float[] Tone(SynthGen g, float len, float hz, float attack, float tau)
        {
            float[] x = g.Buffer(len);
            MixTone(x, 0f, hz, attack, tau, 1f);
            return x;
        }

        private static void MixTone(float[] x, float at, float hz, float attack, float tau, float gain)
        {
            int start = (int)(at * Rate);
            for (int i = start; i < x.Length; i++)
            {
                float t = (i - start) / (float)Rate;
                x[i] += gain * MathF.Sin(Dsp.TwoPi * hz * t) * Dsp.AD(t, attack, tau);
            }
        }

        private static float[] Whisper(SynthGen g, float len)
        {
            float[] x = Dsp.Voice(g, len, new VoiceParams
            {
                Whisper = true,
                Formants = u => Vowel.Speech[(int)(u * 9f) % Vowel.Speech.Length],
                Amp = u => Dsp.Bell(u, 1f, 0.5f) * (0.6f + 0.4f * MathF.Sin(u * 40f)),
            });
            var tail = new float[x.Length + g.N(0.8f)];
            Array.Copy(x, tail, x.Length);
            Dsp.Reverb(tail, Rate, 0.85f, 0.4f, 0.5f);
            return tail;
        }

        // Karplus-Strong: a plucked string from a noise burst in a damped delay line.
        private static float[] Pluck(SynthGen g, float hz, float len)
        {
            int n = g.N(len);
            int period = Math.Max(2, (int)(Rate / hz));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = g.Bipolar();
            var x = new float[n];
            int k = 0;
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float cur = line[k];
                float next = 0.996f * 0.5f * (cur + prev);
                prev = cur;
                line[k] = next;
                x[i] = cur;
                if (++k >= period) k = 0;
            }
            Dsp.LP(x, 3500f, Rate);
            return x;
        }
        #endregion
    }
}
