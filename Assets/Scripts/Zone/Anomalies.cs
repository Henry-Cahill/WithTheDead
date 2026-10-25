using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>One kind of anomaly: what it does and the single consistent rule a survivor can learn about it.</summary>
    public sealed class AnomalyDef
    {
        public string Id, Name, Rule;
        public DamageKind Kind;
        public float Radius;
        public Color Tint;
        public string TriggerSfx;
    }

    /// <summary>
    /// The anomalies. Each follows one rule that never changes (the Zone is indifferent, not evil): learn the rule and
    /// the trap becomes a tool. Nobody knows what they are.
    /// </summary>
    public static class AnomalyCatalog
    {
        public static readonly Dictionary<string, AnomalyDef> All = new Dictionary<string, AnomalyDef>
        {
            ["sinkhole"] = new AnomalyDef
            {
                Id = "sinkhole", Name = "Sinkhole", Kind = DamageKind.Environment, Radius = 2.3f, Tint = new Color(0.42f, 0.40f, 0.36f),
                Rule = "Presses down on anything that stops inside it. Running through costs a bruise; standing still costs your life.",
                TriggerSfx = SfxIds.AnomalyGravity,
            },
            ["embermoss"] = new AnomalyDef
            {
                Id = "embermoss", Name = "Embermoss", Kind = DamageKind.Burn, Radius = 1.9f, Tint = new Color(0.95f, 0.45f, 0.15f),
                Rule = "Flares at anything that moves fast. It ignores what creeps.",
                TriggerSfx = SfxIds.AnomalyFire,
            },
            ["staticbloom"] = new AnomalyDef
            {
                Id = "staticbloom", Name = "Staticbloom", Kind = DamageKind.Shock, Radius = 2.7f, Tint = new Color(0.55f, 0.75f, 1f),
                Rule = "Crackles four times, then strikes whatever is closest. A bolt spends the charge.",
                TriggerSfx = SfxIds.AnomalyElectric,
            },
            ["greysyrup"] = new AnomalyDef
            {
                Id = "greysyrup", Name = "Grey Syrup", Kind = DamageKind.Chemical, Radius = 2.1f, Tint = new Color(0.55f, 0.58f, 0.55f),
                Rule = "Eats living tissue, slowly. Boots and sealed suits slow it. Dead things pass through untouched.",
                TriggerSfx = SfxIds.AnomalyAcid,
            },
            ["tollgate"] = new AnomalyDef
            {
                Id = "tollgate", Name = "Tollgate", Kind = DamageKind.Psi, Radius = 1.7f, Tint = new Color(0.62f, 0.52f, 0.75f),
                Rule = "Takes the first living thing that crosses. Then it sleeps a while. Somebody has to go first.",
                TriggerSfx = SfxIds.AnomalyTrigger,
            },
        };

                /// <summary>Retrieve an anomaly definition by id, returning null if not found.</summary>
        /// <summary>Retrieve an anomaly definition by id, returning null if not found.</summary>
        public static AnomalyDef Get(string id) => id != null && All.TryGetValue(id, out AnomalyDef d) ? d : null;
    }

    /// <summary>
    /// A live anomaly. Invisible or nearly so: a faint disturbance on the ground that only shows up close, a hum,
    /// the beep of a detector — and its reaction to a thrown bolt. In the isometric view, which would otherwise see
    /// them from across the map, the disturbance fades in only within a few metres of the survivor.
    /// </summary>
    public sealed class Anomaly : MonoBehaviour
    {
        public static readonly List<Anomaly> All = new List<Anomaly>();

        /// <summary>The definition of this anomaly instance.</summary>
        public AnomalyDef Def { get; private set; }
        /// <summary>Radius of the anomaly effect area.</summary>
        public float Radius { get; private set; }
        /// <summary>Strength multiplier for damage and effects.</summary>
        public float Strength { get; private set; } = 1f;
        /// <summary>True while a tollgate sleeps (it has taken its toll).</summary>
        public bool Dormant => Time.time < _dormantUntil;

        private Transform _visual;
        private static Vector3 _cachedEyePos;
        private static int _cachedEyeFrame;
        private ParticleSystem _ps;
        private float _psRate;
        private float _tick, _charge, _cooldown, _dormantUntil, _visible = -1f;
        private int _crackles;
        private AudioSource _hum;
        private readonly Dictionary<Actor, float> _inside = new Dictionary<Actor, float>();
        private readonly List<Actor> _scratch = new List<Actor>();
        private Light _flash;
        private float _flashUntil;

        /// <summary>Create a new anomaly instance under a parent transform.</summary>
        public static Anomaly Spawn(Transform parent, AnomalyDef def, Vector3 at, float strength, System.Random rng)
        {
            var go = new GameObject("Anomaly " + def.Id);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            ZoneKit.NoBake(go);
            var a = go.AddComponent<Anomaly>();
            a.Def = def;
            a.Strength = strength;
            a.Radius = def.Radius * Mathf.Lerp(0.85f, 1.2f, (float)rng.NextDouble());
            a.BuildVisual(rng);
            return a;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable()
        {
            All.Remove(this);
            if (_hum != null) Sfx.Stop(_hum);
            _hum = null;
        }

        #region Visuals
        /// <summary>Construct the visual mesh for this anomaly based on its type.</summary>
        private void BuildVisual(System.Random rng)
        {
            // Create a child transform to hold generated meshes
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);
            // Helper to build geometry primitives for the visual
            var m = new RaidMesh();
            Vector3 c = transform.position;
            m.SetFrame(c, 0f);
            Color t = Def.Tint;
            // Build geometry depending on the anomaly’s definition ID
            switch (Def.Id)
            {
                case "sinkhole":
                    m.FlatBlob(new Vector3(0f, 0.012f, 0f), Radius * 0.95f, Radius * 0.9f, ZoneKit.R(rng, 0f, 360f), 0.15f, rng, ZoneKit.M(new Color(0.22f, 0.21f, 0.2f)));
                    m.FlatBlob(new Vector3(0f, 0.016f, 0f), Radius * 0.45f, Radius * 0.42f, ZoneKit.R(rng, 0f, 360f), 0.2f, rng, ZoneKit.M(new Color(0.13f, 0.12f, 0.12f)));
                    // Add radial crack-like ellipsoids around the centre
                    for (int i = 0; i < 9; i++)
                    {
                        float ang = i * 40f + ZoneKit.R(rng, -10f, 10f), r = ZoneKit.R(rng, 0.3f, Radius * 0.7f);
                        Vector3 p = Quaternion.Euler(0f, ang, 0f) * new Vector3(r, ZoneKit.R(rng, 0.4f, 1.4f), 0f);
                        m.Ellipsoid(p, Vector3.one * ZoneKit.R(rng, 0.05f, 0.11f), ZoneKit.M(ZoneKit.V(new Color(0.35f, 0.32f, 0.28f), rng)), 5, 3);
                    }
                    break;
                case "embermoss":
                    // Scatter moss patches as ellipsoids in a circular area
                    for (int i = 0; i < 26; i++)
                    {
                        Vector2 o = Random2(rng) * Radius * 0.85f;
                        float s = ZoneKit.R(rng, 0.08f, 0.2f);
                        m.Ellipsoid(new Vector3(o.x, s * 0.4f, o.y), new Vector3(s * 1.4f, s, s * 1.4f), ZoneKit.Gl(ZoneKit.V(t, rng, 0.12f)), 6, 3);
                    }
                    m.FlatBlob(new Vector3(0f, 0.01f, 0f), Radius * 0.9f, Radius * 0.9f, 0f, 0.3f, rng, ZoneKit.M(new Color(0.16f, 0.12f, 0.1f)));
                    break;
                case "staticbloom":
                    // Create vertical rods with glowing tips to suggest electrical arcs
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 o = Random2(rng) * Radius * 0.35f;
                        Vector3 b = new Vector3(o.x, 0f, o.y);
                        Vector3 tip = b + new Vector3(ZoneKit.R(rng, -0.25f, 0.25f), ZoneKit.R(rng, 0.5f, 1.3f), ZoneKit.R(rng, -0.25f, 0.25f));
                        m.Rod(b, tip, 0.04f, ZoneKit.Mt(new Color(0.42f, 0.46f, 0.52f)), 5);
                        m.Ellipsoid(tip, Vector3.one * 0.07f, ZoneKit.Gl(t), 5, 3);
                    }
                    // Weathered pole with lantern – winter detail
                    // foot‑box at ground centre
                    m.Box(new Vector3(0f, 0.02f, 0f), new Vector3(0.12f, 0.04f, 0.12f), ZoneKit.Mt(ZoneKit.Rust));
                    // pole – thin rusted rod
                    m.Rod(new Vector3(0f, 0.04f, 0f), new Vector3(0f, 1.12f, 0f), 0.02f, ZoneKit.Mt(ZoneKit.Rust), 5);
                    // lantern housing – weather‑worn wood
                    m.Box(new Vector3(0f, 1.22f, 0f), new Vector3(0.18f, 0.22f, 0.07f), ZoneKit.M(ZoneKit.Bark));
                    // lantern glass – slightly frosted
                    m.Ellipsoid(new Vector3(0f, 1.28f, 0f), Vector3.one * 0.045f, ZoneKit.Gl(new Color(0.78f, 0.86f, 1f, 0.4f)), 5, 3);
                    // hanging rope – thin rusted rod
                    m.Rod(new Vector3(0.07f, 1.25f, 0f), new Vector3(0.07f, 1.10f, 0f), 0.008f, ZoneKit.Mt(ZoneKit.Rust), 5);
                    m.FlatBlob(new Vector3(0f, 0.01f, 0f), Radius * 0.7f, Radius * 0.7f, 0f, 0.25f, rng, ZoneKit.M(new Color(0.1f, 0.1f, 0.12f)));
                    break;
                case "greysyrup":
                    m.FlatBlob(new Vector3(0f, 0.02f, 0f), Radius, Radius * 0.85f, ZoneKit.R(rng, 0f, 360f), 0.22f, rng, ZoneKit.G(new Color(0.36f, 0.38f, 0.37f)));
                    m.FlatBlob(new Vector3(0.2f, 0.025f, -0.1f), Radius * 0.55f, Radius * 0.5f, ZoneKit.R(rng, 0f, 360f), 0.3f, rng, ZoneKit.G(new Color(0.45f, 0.48f, 0.45f)));
                    break;
                case "tollgate":
                    // Build two side pillars and top beam for the gate
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector3 b = new Vector3(side * 1.1f, 0f, 0f), top = new Vector3(side * 0.55f, 2.6f, ZoneKit.R(rng, -0.1f, 0.1f));
m.Rod(b, top, 0.08f, ZoneKit.M(ZoneKit.Bark), 6);
                     // Add a rusty bolt base ellipsoid at the rod's base
                     m.Ellipsoid(new Vector3(b.x, 0.04f, b.z), Vector3.one * 0.02f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
// Add a rusty ellipsoid cap at the top of each main rod
                      m.Ellipsoid(top, Vector3.one * 0.05f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);

// Top beam spanning between side rods
                      m.Box(new Vector3(0f, 2.62f, 0f), new Vector3(2.2f, 0.07f, 0.12f), ZoneKit.M(ZoneKit.Bark));

                    // Sign post – a vertical plank in front of the gate
                    m.Box(new Vector3(0f, 0.45f, -0.35f), new Vector3(0.08f, 0.9f, 0.08f), ZoneKit.M(ZoneKit.Bark));

                    // Sign board – weathered wood
                    m.Box(new Vector3(0f, 0.85f, -0.55f), new Vector3(0.6f, 0.35f, 0.02f), ZoneKit.M(ZoneKit.Bark));

                    // Rope – thin metal rod
                    m.Rod(new Vector3(0f, 0.9f, -0.35f), new Vector3(0f, 0.85f, -0.55f), 0.01f, ZoneKit.Mt(ZoneKit.Rust), 5);

                    // Rusty nails – small ellipsoids on the sign front
                    for (int i = 0; i < 4; i++)
                    {
                        float ox = ZoneKit.R(rng, -0.27f, 0.27f);
                        float oy = ZoneKit.R(rng, -0.12f, 0.12f);
                        m.Ellipsoid(new Vector3(ox, 0.85f + oy, -0.545f), Vector3.one * 0.012f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
                    }

                    // Faded paint patch – slightly off‑bark colour
                    Color basePaint = ZoneKit.Bark;
                    Color patch = new Color(
                        Mathf.Clamp01(basePaint.r + ZoneKit.R(rng, -0.05f, 0.05f)),
                        Mathf.Clamp01(basePaint.g + ZoneKit.R(rng, -0.05f, 0.05f)),
                        Mathf.Clamp01(basePaint.b + ZoneKit.R(rng, -0.05f, 0.05f)));
                    m.Ellipsoid(new Vector3(0f, 0.85f, -0.545f), Vector3.one * 0.07f, ZoneKit.Gl(patch), 5, 3);

                    // Metal tag – tiny rusted plate
                    m.Box(new Vector3(0.24f, 0.71f, -0.545f), new Vector3(0.08f, 0.04f, 0.005f), ZoneKit.Mt(ZoneKit.Rust));

                     // Middle wooden cross‑bar providing structural support
                     m.Box(new Vector3(0f, 1.6f, 0f), new Vector3(2.2f, 0.06f, 0.10f), ZoneKit.M(ZoneKit.Bark));

                     // Lower wooden brace, slightly shorter for visual variety
                     m.Box(new Vector3(0f, 0.9f, 0f), new Vector3(2.0f, 0.05f, 0.09f), ZoneKit.M(ZoneKit.Bark));

                     // Rusted chain – four short links made from thin rods
                     Vector3 chainStart = new Vector3(-0.55f, 1.2f, 0f);
                     Vector3 linkDir = new Vector3(0.35f, 0f, 0f);
                     float linkLen = 0.12f; // not used directly, length set by end point
                     // Build a short rusted chain of four links between the side pillars
                     // Place small rusty nails on the sign front for detail
                     for (int i = 0; i < 4; i++)
                     {
                         Vector3 a = chainStart + i * linkDir;
                         Vector3 linkEnd = a + new Vector3(0f, 0f, 0.06f);
                         m.Rod(a, linkEnd, 0.015f, ZoneKit.Mt(ZoneKit.Rust), 5);
                     }

                     // Hinged metal latch bar that swings when the gate is closed
                     m.Box(new Vector3(0f, 0.78f, -0.07f), new Vector3(0.30f, 0.04f, 0.02f), ZoneKit.Mt(ZoneKit.Rust));

                     // Random rivets on the left plank
                     for (int i = 0; i < 3; i++)
                     {
                         float x = -0.55f + ZoneKit.R(rng, -0.01f, 0.01f);
                         float y = 0.6f + ZoneKit.R(rng, -0.3f, 0.8f);
                         float z = ZoneKit.R(rng, -0.07f, 0.07f);
                         m.Ellipsoid(new Vector3(x, y, z), Vector3.one * 0.012f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
                     }
                     // Random rivets on the right plank
                     for (int i = 0; i < 3; i++)
                     {
                         float x = 0.55f + ZoneKit.R(rng, -0.01f, 0.01f);
                         float y = 0.6f + ZoneKit.R(rng, -0.3f, 0.8f);
                         float z = ZoneKit.R(rng, -0.07f, 0.07f);
                         m.Ellipsoid(new Vector3(x, y, z), Vector3.one * 0.012f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
                     }

                     // Vertical side planks attached to each main rod with subtle yaw for weathered look
                     m.Box(new Vector3(-0.55f, 1.28f, 0f), new Vector3(0.07f, 2.4f, 0.15f), Quaternion.Euler(0f, ZoneKit.R(rng, -2f, 2f), 0f), ZoneKit.M(ZoneKit.Bark));
                     m.Box(new Vector3( 0.55f, 1.28f, 0f), new Vector3(0.07f, 2.4f, 0.15f), Quaternion.Euler(0f, ZoneKit.R(rng, -2f, 2f), 0f), ZoneKit.M(ZoneKit.Bark));

                    // Rusted metal latch plate
                    m.Box(new Vector3(0f, 0.82f, 0.11f), new Vector3(0.18f, 0.08f, 0.03f), ZoneKit.Mt(ZoneKit.Rust));
                    // Latch knob (rust colour)
                    m.Ellipsoid(new Vector3(0f, 0.86f, 0.13f), Vector3.one * 0.025f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);

                    // Hinge pins on each side rod
                    m.Rod(new Vector3(-0.55f, 2.58f, 0f), new Vector3(-0.55f, 2.68f, 0f), 0.02f, ZoneKit.Mt(ZoneKit.Rust), 5);
                    m.Rod(new Vector3( 0.55f, 2.58f, 0f), new Vector3( 0.55f, 2.68f, 0f), 0.02f, ZoneKit.Mt(ZoneKit.Rust), 5);

                    // Subtle rust patches on planks and center board
                    m.Ellipsoid(new Vector3(-0.55f, 1.9f, 0.05f), Vector3.one * 0.04f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
                    m.Ellipsoid(new Vector3( 0.55f, 1.2f, -0.04f), Vector3.one * 0.035f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
                    m.Ellipsoid(new Vector3(0f, 2.0f, 0.06f), Vector3.one * 0.045f, ZoneKit.Gl(new Color(0.55f, 0.30f, 0.10f)), 5, 3);
                }
                m.FlatBlob(new Vector3(0f, 0.012f, 0f), Radius, Radius * 0.7f, 0f, 0.2f, rng, ZoneKit.M(new Color(0.18f, 0.16f, 0.14f)));
                for (int i = 0; i < 6; i++)
                    {
                        Vector2 o = Random2(rng) * Radius;
                        m.Rod(new Vector3(o.x, 0.04f, o.y), new Vector3(o.x + ZoneKit.R(rng, -0.3f, 0.3f), 0.05f, o.y + ZoneKit.R(rng, -0.3f, 0.3f)), 0.035f,
                            ZoneKit.M(new Color(0.85f, 0.82f, 0.74f)), 5);
                    }
                    break;
            }
            ZoneKit.Emit(m, "Mesh", _visual, c, ShadowCastingMode.Off);

            Color pc = new Color(t.r, t.g, t.b, 0.35f);
            switch (Def.Id)
            {
                case "embermoss": _ps = Particles(c + Vector3.up * 0.2f, 0.06f, 8f, 0.5f, new Color(1f, 0.6f, 0.2f, 0.9f), true); break;
                case "staticbloom": _ps = Particles(c + Vector3.up * 0.8f, 0.05f, 6f, 0.2f, new Color(0.7f, 0.85f, 1f, 0.9f), true); break;
                case "greysyrup": _ps = ZoneKit.Plume(_visual, c + Vector3.up * 0.05f, false, 0.9f, 2.5f, 0.15f, pc); break;
                case "sinkhole": _ps = ZoneKit.Plume(_visual, c + Vector3.up * 0.05f, false, 0.6f, 1.5f, -0.05f, new Color(0.6f, 0.58f, 0.55f, 0.18f)); break;
                case "tollgate": _ps = ZoneKit.Plume(_visual, c + Vector3.up * 1.2f, false, 1.2f, 1.2f, 0.02f, new Color(0.6f, 0.5f, 0.75f, 0.12f)); break;
            }
            if (_ps != null) _psRate = _ps.emission.rateOverTime.constant;
        }

        /// <summary>Create a particle system for a spark effect used by some anomalies.</summary>
        private ParticleSystem Particles(Vector3 at, float size, float rate, float speed, Color color, bool glow)
        {
            // Temporary object to host the particle system for the anomaly effect
            var go = new GameObject("Sparks");
            // Parent to the visual hierarchy so it moves with the anomaly
            go.transform.SetParent(_visual, false);
            // Position at the specified offset and tilt downwards to align with the ground
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(-90f, 0f, 0f));
            // Configure the particle system to loop with a variable lifetime, speed and size
            var ps = go.AddComponent<ParticleSystem>();
            // Stop any initial emission and clear existing particles
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            // Main module: set loop, lifetime range, speed range, size range and colour
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = color;
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Emission module: define how many particles per second are spawned
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;
            // Shape module: emit from a circle matching the anomaly radius
            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = Radius * 0.6f;
            // Assign a simple unlit material and disable shadows for performance
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = ZoneKit.EmberMaterial();
            r.shadowCastingMode = ShadowCastingMode.Off;
            // Start the particle system now that it’s fully configured
            ps.Play();
            // Return the configured particle system for later control
            return ps;
        }

        private static Vector2 Random2(System.Random rng)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble());
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        // Close to the survivor the disturbance shows; from the high isometric camera it fades in only nearby
        // (the doc's "visible from too far: fade them in by distance").
        private static Vector3 GetEyePosition()
        {
            if (Time.frameCount != _cachedEyeFrame)
            {
                _cachedEyePos = ListenerRig.Active ? ListenerRig.Position : (Actor.LocalPlayer != null ? Actor.LocalPlayer.transform.position : Vector3.zero);
                _cachedEyeFrame = Time.frameCount;
            }
            return _cachedEyePos;
        }
                private void UpdateVisibility()
        {
            Vector3 eye = GetEyePosition();
            float d = Vector3.Distance(new Vector3(eye.x, 0f, eye.z), new Vector3(transform.position.x, 0f, transform.position.z));
            float k = ViewState.Current == CameraView.Isometric ? Mathf.InverseLerp(9f, 5f, d) : Mathf.InverseLerp(26f, 18f, d);
            if (Def.Id == "tollgate" && Dormant) k *= 0.5f;
            if (Mathf.Abs(k - _visible) < 0.02f) return;
            _visible = k;
            _visual.localScale = new Vector3(1f, Mathf.Max(0.001f, k), 1f);
            _visual.gameObject.SetActive(k > 0.01f);
            if (_ps != null)
            {
                ParticleSystem.EmissionModule em = _ps.emission;
                em.rateOverTime = _psRate * k;
            }

            // The hum only plays near the listener (dozens of anomalies, a handful of voices).
            bool near = d < 12f;
            if (near && _hum == null) _hum = Sfx.Loop(SfxIds.AnomalyHum, transform, 0.35f, 9f, Def.Id == "staticbloom" ? 1.4f : Def.Id == "sinkhole" ? 0.7f : 1f);
            else if (!near && d > 16f && _hum != null) { Sfx.Stop(_hum); _hum = null; }
        }
        #endregion

        #region Rules
        private void Update()
        {
            UpdateVisibility();
            if (_flash != null && Time.time > _flashUntil) _flash.enabled = false;

            _tick -= Time.deltaTime;
            if (_tick > 0f) return;
            float dt = 0.1f - _tick;
            _tick = 0.1f;
            if (_cooldown > 0f) _cooldown -= dt;

            // Bolts first: they trip a trap before a body does.
            for (int i = Bolt.Live.Count - 1; i >= 0; i--)
            {
                Bolt b = Bolt.Live[i];
                if (b == null || b.Spent) continue;
                if (Inside(b.transform.position, 0.2f)) OnBolt(b);
            }

            _scratch.Clear();
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || !a.IsAlive) continue;
                if (Inside(a.transform.position, 0.3f)) _scratch.Add(a);
            }
            // Forget whoever left.
            if (_inside.Count > 0)
            {
                var gone = s_Gone;
                gone.Clear();
                foreach (var kv in _inside) if (!_scratch.Contains(kv.Key)) gone.Add(kv.Key);
                for (int i = 0; i < gone.Count; i++) _inside.Remove(gone[i]);
            }
            for (int i = 0; i < _scratch.Count; i++)
            {
                Actor a = _scratch[i];
                _inside.TryGetValue(a, out float t);
                _inside[a] = t + dt;
            }

            switch (Def.Id)
            {
                case "sinkhole": TickSinkhole(dt); break;
                case "embermoss": TickEmbermoss(); break;
                case "staticbloom": TickStatic(dt); break;
                case "greysyrup": TickSyrup(dt); break;
                case "tollgate": TickTollgate(); break;
            }
        }

        private static readonly List<Actor> s_Gone = new List<Actor>();

        /// <summary>Check if a point lies inside the anomaly’s radius (with optional margin).</summary>
        public bool Inside(Vector3 p, float margin)
        {
            Vector3 c = transform.position;
            float dy = p.y - c.y;
            if (dy < -1f || dy > 2.6f) return false;
            float dx = p.x - c.x, dz = p.z - c.z;
            float r = Radius + margin;
            return dx * dx + dz * dz <= r * r;
        }

        // Sinkhole: anything that lingers past a heartbeat is pressed into the ground.
        private void TickSinkhole(float dt)
        {
            foreach (var kv in _inside)
            {
                Actor a = kv.Key;
                float t = kv.Value;
                if (t < 0.5f) continue;
                float speed = Speed(a);
                float hp = (speed > 3.2f ? 6f : 38f) * Strength * dt * 10f;
                Hurt(a, hp, speed > 3.2f ? RandomLeg() : BodyZone.Thorax);
                if (_cooldown <= 0f) { Burst(0.9f); _cooldown = 1.6f; }
            }
        }

        // Embermoss: flares at fast movement within it; creeping is safe.
        private void TickEmbermoss()
        {
            // Skip processing if the anomaly is cooling down
            if (_cooldown > 0f) return;
            foreach (var kv in _inside)
            {
                if (Speed(kv.Key) < 1.6f) continue;
                Burst(1f);
                for (int i = 0; i < _scratch.Count; i++) Hurt(_scratch[i], 55f * Strength, RandomLeg());
                _cooldown = 2.2f;
                return;
            }
        }

        // Staticbloom: four crackles, then a strike on the nearest thing within reach. Bolts spend the charge.
        /// <summary>Run the staticbloom timing logic: charge, crackles and final strike.</summary>
        // Implements the staticbloom’s timing: charging, crackling and striking
        // Handles staticbloom’s charging, crackling and striking phases
        private void TickStatic(float dt)
        {
            // Skip processing if the anomaly is cooling down
            if (_cooldown > 0f) return;
            bool anyone = _inside.Count > 0 || NearAnyone(Radius + 3f);
            if (!anyone) { _charge = 0f; _crackles = 0; return; }
            _charge += dt;
            if (_charge >= 0.55f * (_crackles + 1) && _crackles < 4)
            {
                _crackles++;
                Sfx.Play(SfxIds.DetectorBeep, transform.position + Vector3.up, 0.45f, 0.5f + _crackles * 0.08f, 14f);
                Flash(0.08f, 1.2f);
            }
            if (_crackles < 4 || _charge < 2.4f) return;
            Actor nearest = null;
            float best = float.MaxValue;
            foreach (var kv in _inside)
            {
                float d = (kv.Key.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = kv.Key; }
            }
            Burst(1f);
            if (nearest != null) Hurt(nearest, 95f * Strength, BodyZone.Thorax);
            Discharge();
        }

        private void Discharge()
        {
            _charge = 0f;
            _crackles = 0;
            _cooldown = 2f;
            Flash(0.25f, 6f);
        }

        // Grey syrup: steady chemical burn to the living standing in it, mostly the legs; gear takes it too.
        private void TickSyrup(float dt)
        {
            foreach (var kv in _inside)
            {
                Actor a = kv.Key;
                if (a.Kind == ActorKind.Zombie) continue;     // dead things pass through untouched
                Hurt(a, 14f * Strength * dt * 10f, RandomLeg());
                var clothing = a.GetComponent<PlayerClothing>();
                if (clothing != null) GearWear.Corrode(clothing, dt * 0.6f);
            }
            if (_inside.Count > 0 && _cooldown <= 0f) { Sfx.Play(SfxIds.AnomalyAcid, transform.position, 0.5f, 1f, 12f); _cooldown = 2.5f; }
        }

        // Tollgate: takes the first living thing that crosses, then sleeps.
        private void TickTollgate()
        {
            if (Dormant) return;
            foreach (var kv in _inside)
            {
                Actor a = kv.Key;
                if (a.Kind == ActorKind.Zombie && a.GetComponent<ZombieHealth>() == null) continue;
                Burst(1f);
                Hurt(a, 520f * Strength, BodyZone.Head);
                Hurt(a, 300f * Strength, BodyZone.Thorax);
                _dormantUntil = Time.time + 150f;
                GameEvents.RaiseWorldEvent(transform.position, 40f, "tollgate");
                return;
            }
        }

        private void OnBolt(Bolt b)
        {
            GameEvents.RaiseQuestProgress("bolt_anomaly", 1f);
            ProfileStore.Account?.Learn("anomaly_rule:" + Def.Id);
            switch (Def.Id)
            {
                case "sinkhole":
                    b.Fling(Vector3.down * 2f, true);
                    Burst(0.7f);
                    break;
                case "embermoss":
                    if (_cooldown <= 0f) { Burst(0.8f); _cooldown = 1.5f; }
                    b.Fling(Vector3.up * 3f + Random.insideUnitSphere, false);
                    break;
                case "staticbloom":
                    Burst(0.8f);
                    Discharge();
                    b.Fling(Vector3.up * 4f + Random.insideUnitSphere * 2f, false);
                    break;
                case "greysyrup":
                    Sfx.Play(SfxIds.Splash, b.transform.position, 0.6f, 0.8f, 16f);
                    b.Sink();
                    break;
                case "tollgate":
                    // A bolt isn't alive: it passes through, but the air ripples where it crossed.
                    if (!Dormant) Flash(0.15f, 2f);
                    b.MarkSpent();
                    break;
            }
        }

        private void Burst(float volume)
        {
            Sfx.Play(Def.TriggerSfx, transform.position + Vector3.up * 0.5f, volume, Random.Range(0.92f, 1.08f), 35f);
            NoiseSystem.Emit(transform.position, 14f);
            Flash(0.2f, Def.Id == "staticbloom" ? 8f : 3f);
            if (_ps != null) _ps.Emit(25);
        }

        /// <summary>Trigger a brief flash of light for visual feedback of an anomaly.</summary>
        // Add a flash of light when the anomaly strikes or crackles
        private void Flash(float seconds, float intensity)
        {
            if (Def.Id == "greysyrup" || Def.Id == "sinkhole") return;
            if (_flash == null)
            {
                var go = new GameObject("Flash");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.up * 1f;
                _flash = go.AddComponent<Light>();
                _flash.type = LightType.Point;
                _flash.range = Radius * 3f;
                _flash.shadows = LightShadows.None;
                _flash.color = Def.Tint;
            }
            _flash.intensity = intensity;
            _flash.enabled = true;
            _flashUntil = Time.time + seconds;
        }

        private bool NearAnyone(float r)
        {
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a != null && a.IsAlive && (a.transform.position - transform.position).sqrMagnitude < r * r) return true;
            }
            return false;
        }

        private static BodyZone RandomLeg() => Random.value < 0.5f ? BodyZone.LeftLeg : BodyZone.RightLeg;

        private static float Speed(Actor a)
        {
            var cc = a.GetComponent<CharacterController>();
            if (cc != null) return new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
            var agent = a.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled) return agent.velocity.magnitude;
            var rb = a.GetComponent<Rigidbody>();
            return rb != null ? rb.linearVelocity.magnitude : 0f;
        }

        private void Hurt(Actor a, float hp, BodyZone zone)
        {
            if (a == null || a.Damageable == null || a.Damageable.IsDead) return;
            var info = new DamageInfo(hp, Def.Kind, a.Center, Vector3.up, gameObject)
            {
                HpScale = true, WeaponId = "anomaly_" + Def.Id, BleedChance = Def.Kind == DamageKind.Environment ? 0.3f : 0f,
                FractureChance = Def.Kind == DamageKind.Environment ? 0.25f : 0f,
            }.WithZone(zone);
            CombatResolver.Apply(a.Damageable, info, null);
        }
        #endregion
    }

    /// <summary>Wear on worn gear from the Zone (acid, fire): condition off the boots first, then the trousers.</summary>
    public static class GearWear
    {
        private static readonly Dictionary<PlayerClothing, float> s_Owed = new Dictionary<PlayerClothing, float>();

        /// <summary>Apply corrosion to the given clothing piece by the specified amount.</summary>
        public static void Corrode(PlayerClothing clothing, float amount)
        {
            if (clothing == null || amount <= 0f) return;
            s_Owed.TryGetValue(clothing, out float owed);
            owed += amount;
            while (owed >= 1f)
            {
                owed -= 1f;
                ClothingSlot slot = clothing.IsWearing(ClothingSlot.Feet) && clothing.GetPiece(ClothingSlot.Feet).Condition > 0 ? ClothingSlot.Feet
                    : clothing.IsWearing(ClothingSlot.Legs) ? ClothingSlot.Legs : ClothingSlot.Torso;
                PlayerClothing.WornPiece p = clothing.GetPiece(slot);
                if (p.Exists) clothing.SetCondition(slot, p.Condition - 1, p.Condition - 1 <= 0);
            }
            s_Owed[clothing] = owed;
            if (s_Owed.Count > 16) s_Owed.Clear();
        }
    }
}
