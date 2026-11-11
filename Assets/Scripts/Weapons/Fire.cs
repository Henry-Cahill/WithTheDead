using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>A thrown molotov: a bottle of petrol with a burning rag in its neck, that bursts into flame (a
    /// <see cref="FirePatch"/>) on the first thing it hits.</summary>
    public sealed class MolotovBottle : MonoBehaviour
    {
        private GameObject _thrower;
        private Rigidbody _rb;
        private ParticleSystem _rag;
        private float _age, _neck;
        private bool _burst;

        /// <summary>Throws one (<see cref="Throwables.Throw"/>).</summary>
        public static MolotovBottle Spawn(Vector3 at, Vector3 velocity, GameObject thrower)
        {
            GameObject go = Throwables.Body(Throwables.Molotov, at, velocity, thrower, 0.9f, true);
            var m = go.AddComponent<MolotovBottle>();
            m._thrower = thrower;
            m._rb = go.GetComponent<Rigidbody>();
            ThrownLook look = go.GetComponent<ThrownLook>();
            m._neck = look != null ? look.Size.y * 0.5f : 0.15f;
            m._rag = FireFx.Flames(null, m.Neck, 0.02f, 45f, 0.3f);
            ZoneFlameLight.Spawn(go.transform, m.Neck, FireFx.FlameColor, 5f, 1.4f, 0.5f);
            return m;
        }

        private Vector3 Neck => transform.position + transform.up * _neck;

        private void LateUpdate()
        {
            if (_rag != null) _rag.transform.position = Neck;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age > 8f || transform.position.y < -200f) { Destroy(gameObject); return; }
            // Come to rest without breaking (soft ground, a mattress): the rag burns down into it anyway.
            if (_age > 0.6f && _rb != null && _rb.linearVelocity.sqrMagnitude < 0.04f) Burst(transform.position, Vector3.up);
        }

        private void OnCollisionEnter(Collision c)
        {
            if (c.contactCount > 0)
            {
                ContactPoint p = c.GetContact(0);
                Burst(p.point, p.normal);
            }
            else Burst(transform.position, Vector3.up);
        }

        private void Burst(Vector3 at, Vector3 normal)
        {
            if (_burst) return;
            _burst = true;
            Sfx.Play(SfxIds.ImpactGlass, at, 1f, Random.Range(0.85f, 1.05f), 30f);
            NetThrowables.Ignite(FirePatch.Floor(at, normal), _thrower);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (_rag == null) return;
            FireFx.SetRate(_rag, 0f);
            Destroy(_rag.gameObject, 1f);
        }
    }

    /// <summary>
    /// A molotov's fire: the petrol splashed out round where the bottle burst, a pool of flame a few metres across that
    /// spreads — tongues of it run on along the floor over its first seconds, stopped by walls and drops — and gutters out
    /// after <see cref="Life"/> seconds, leaving a scorch. Whatever stands in it catches (<see cref="Aflame"/>): the dead keep
    /// walking, alight, and set those they crowd against alight too, so a doorway full of them burns down to the last. It
    /// roars, a little (a noise, not a lure). The server's fire burns; a client's — started from the server's word, with the
    /// same seed, so the same shape — only shows (<see cref="NetThrowables"/>).
    /// </summary>
    public sealed class FirePatch : MonoBehaviour
    {
        /// <summary>How long the petrol burns, seconds, and the pool's radius once it has splashed out.</summary>
        public const float Life = 14f, Radius = 2f;
        /// <summary>How far past the pool its tongues can run, at most.</summary>
        public const float TongueReach = 2.4f;

        private const float Grow = 1.1f, Fade = 2.5f, TickEvery = 0.25f, TongueStep = 0.7f, TongueRadius = 0.75f, RoarEvery = 1f, RoarNoise = 8f;

        /// <summary>Every fire burning.</summary>
        public static readonly List<FirePatch> All = new List<FirePatch>();

        private struct Spot
        {
            public Vector3 At;
            public float Radius, Born;
            public ParticleSystem Flames;
        }

        private readonly List<Spot> _spots = new List<Spot>(16);
        private GameObject _source;
        private bool _authority, _ended;
        private float _t, _tickAt, _roarAt, _bound;
        private ZoneFlameLight _light;
        private ParticleSystem _smoke;
        private AudioSource _roar, _crackle;

        /// <summary>The server's (it burns) rather than a client's copy (it only shows).</summary>
        public bool Authority => _authority;
        /// <summary>How many places it burns in: the pool and every stretch of its tongues (so far).</summary>
        public int Spots
        {
            get
            {
                int n = 0;
                foreach (Spot s in _spots) if (_t >= s.Born) n++;
                return n;
            }
        }

        /// <summary>Starts a fire at <paramref name="at"/> (on the floor), set by <paramref name="source"/>; its shape follows
        /// from <paramref name="seed"/>. Go through <see cref="NetThrowables.Ignite"/> to have everyone see it.</summary>
        public static FirePatch Ignite(Vector3 at, GameObject source, bool authority, int seed)
        {
            var go = new GameObject("Fire");
            go.transform.position = at;
            var f = go.AddComponent<FirePatch>();
            f._source = source;
            f._authority = authority;
            f.Plan(new System.Random(seed));
            f.Dress();
            return f;
        }

        /// <summary>Where a bottle bursting at <paramref name="at"/>, on a surface facing <paramref name="normal"/>, puts its
        /// fire: on the floor below, a little off a wall it hit. The look down starts just off the surface, on the bottle's side
        /// of it: from above, a bottle that hit a ceiling would find the floor upstairs.</summary>
        public static Vector3 Floor(Vector3 at, Vector3 normal)
        {
            Vector3 from = at + normal * 0.1f + CloneZUtil.Flat(normal) * 0.2f;
            return Physics.Raycast(from, Vector3.down, out RaycastHit hit, 6f, CloneZConfig.WorldMask, QueryTriggerInteraction.Ignore)
                ? hit.point : at;
        }

        /// <summary>True when <paramref name="p"/> (feet) stands in its flames now, give or take <paramref name="margin"/>.</summary>
        public bool Covers(Vector3 p, float margin = 0f)
        {
            for (int i = 0; i < _spots.Count; i++)
            {
                Spot s = _spots[i];
                float r = RadiusOf(s);
                if (r <= 0f || Mathf.Abs(p.y - s.At.y) > 1.2f) continue;
                float dx = p.x - s.At.x, dz = p.z - s.At.z;
                if (dx * dx + dz * dz <= (r + margin) * (r + margin)) return true;
            }
            return false;
        }

        // The pool, then each tongue as a run of small spots, worked out once: the petrol runs along the floor it's on, and
        // stops at a wall, a step up or a drop.
        private void Plan(System.Random rng)
        {
            Vector3 c = transform.position;
            _spots.Add(new Spot { At = c, Radius = Radius, Born = 0f });
            _bound = Radius;
            int tongues = 3 + rng.Next(3);
            float a0 = (float)rng.NextDouble() * 360f;
            for (int i = 0; i < tongues; i++)
            {
                float yaw = a0 + i * 360f / tongues + ((float)rng.NextDouble() - 0.5f) * 50f;
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float reach = Radius + 0.4f + (float)rng.NextDouble() * TongueReach;
                float born = 0.25f + (float)rng.NextDouble() * 0.4f;
                Vector3 prev = c;
                for (float d = Radius * 0.8f; d <= reach; d += TongueStep)
                {
                    Vector3 p = c + dir * d;
                    if (Physics.Linecast(prev + Vector3.up * 0.3f, p + Vector3.up * 0.3f, CloneZConfig.WorldMask, QueryTriggerInteraction.Ignore)) break;
                    if (!Physics.Raycast(p + Vector3.up * 0.6f, Vector3.down, out RaycastHit hit, 1.4f, CloneZConfig.WorldMask, QueryTriggerInteraction.Ignore)
                        || Mathf.Abs(hit.point.y - c.y) > 0.45f) break;
                    born += 0.25f + (float)rng.NextDouble() * 0.35f;
                    float r = TongueRadius * (1f - 0.35f * Mathf.Clamp01((d - Radius) / TongueReach));
                    _spots.Add(new Spot { At = hit.point, Radius = r, Born = born });
                    _bound = Mathf.Max(_bound, d + r);
                    prev = p;
                }
            }
        }

        private void Dress()
        {
            Vector3 c = transform.position;
            _light = ZoneFlameLight.Spawn(transform, c + Vector3.up * 0.9f, FireFx.FlameColor, 11f, 3.4f, 0.6f);
            _smoke = FireFx.Smoke(transform, c + Vector3.up * 1.2f, 0.6f, 6f, 1.3f, 0.9f);
            Sfx.Play(SfxIds.AnomalyFire, c + Vector3.up * 0.5f, 1f, 0.8f, 40f);
            _roar = Sfx.Loop(SfxIds.AnomalyHumFire, transform, 0.9f, 30f, 0.9f);
            _crackle = Sfx.Loop(SfxIds.FireCrackle, transform, 0.8f, 24f);
            FireFx.Scorch(c, Radius + 0.4f);
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        private void OnDestroy()
        {
            Sfx.Stop(_roar);
            Sfx.Stop(_crackle);
        }

        // 1 while it burns, then down as it gutters.
        private float Level => _t < Life - Fade ? 1f : Mathf.Lerp(1f, 0.3f, (_t - (Life - Fade)) / Fade);

        private float RadiusOf(in Spot s)
        {
            float age = _t - s.Born;
            if (age < 0f || _ended) return 0f;
            return s.Radius * Mathf.Lerp(0.25f, 1f, Mathf.Sqrt(Mathf.Clamp01(age / Grow))) * Level;
        }

        private void Update()
        {
            if (_ended) return;
            _t += Time.deltaTime;
            if (_t >= Life)
            {
                End();
                return;
            }
            float level = Level;
            for (int i = 0; i < _spots.Count; i++)
            {
                Spot s = _spots[i];
                float r = RadiusOf(s);
                if (r <= 0f) continue;
                if (s.Flames == null)
                {
                    s.Flames = FireFx.Flames(transform, s.At, r, 0f, s.Radius >= Radius ? 1f : 0.8f);
                    _spots[i] = s;
                }
                FireFx.SetShape(s.Flames, r);
                FireFx.SetRate(s.Flames, Mathf.Clamp(14f + 26f * r * r, 10f, 110f) * level);
            }
            if (_light != null) _light.Intensity = 3.4f * level;
            if (_authority && Time.time >= _roarAt)
            {
                _roarAt = Time.time + RoarEvery;
                NoiseSystem.Emit(transform.position, RoarNoise);
            }
            if (Time.time >= _tickAt)
            {
                _tickAt = Time.time + TickEvery;
                Catch();
            }
        }

        // Whatever stands in it catches: on the server it burns; a client only shows its own survivor (and other runners)
        // alight — the server says which of its creatures burn.
        private void Catch()
        {
            Vector3 c = transform.position;
            float bound = _bound + 0.5f;
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || !a.IsAlive) continue;
                Vector3 p = a.transform.position;
                if ((p.x - c.x) * (p.x - c.x) + (p.z - c.z) * (p.z - c.z) > bound * bound || !Covers(p, 0.2f)) continue;
                if (_authority) Aflame.Ignite(a, _source);
                else if (a.GetComponent<NetAgentProxy>() == null) Aflame.Show(a, a.Kind == ActorKind.Player ? Aflame.LivingAfterburn : 0.6f);
            }
        }

        private void End()
        {
            _ended = true;
            foreach (Spot s in _spots)
                if (s.Flames != null) FireFx.SetRate(s.Flames, 0f);
            if (_smoke != null) FireFx.SetRate(_smoke, 0f);
            if (_light != null) Destroy(_light.gameObject);
            Sfx.Stop(_roar);
            Sfx.Stop(_crackle);
            _roar = _crackle = null;
            Destroy(gameObject, 4f);   // the last flames and smoke drift off
        }
    }

    /// <summary>
    /// An actor on fire: flames over the body and a crackle, burn damage every half second while it lasts, and the fire
    /// passed to anyone it brushes against. The dead don't stop for it (a burn neither staggers them: <see cref="ZombieHealth"/>),
    /// so a burning one keeps coming, and a packed crowd catches it one from another. Standing in a <see cref="FirePatch"/>
    /// keeps it lit, and an incendiary round lights it (<see cref="CombatResolver"/>); out of the flames it burns on — the dead,
    /// soaked and shambling, for <see cref="DeadAfterburn"/> seconds (a normal one doesn't last that long), the living, who
    /// beat it out, for <see cref="LivingAfterburn"/>. A body burns down a moment after it falls. On the server it burns; on a
    /// client it only shows (the server's snapshot says which of its creatures are alight: <see cref="NetWorld"/>).
    /// </summary>
    public sealed class Aflame : MonoBehaviour
    {
        /// <summary>Limb hit points a second it costs: the dead (a normal one falls in about four seconds) and the living.</summary>
        public const float DeadDps = 10f, LivingDps = 7f;
        /// <summary>Seconds one burns on once out of the flames.</summary>
        public const float DeadAfterburn = 7f, LivingAfterburn = 3f;
        /// <summary>Within this (flat metres) of a burning body one catches, with <see cref="SpreadChance"/> each half second.</summary>
        public const float SpreadReach = 0.9f, SpreadChance = 0.3f;
        /// <summary>What a fire's burns count as (the kill feed, quests); an incendiary round's count as the gun that fired it.</summary>
        public const string Weapon = Throwables.Molotov;

        private const float TickEvery = 0.5f, CorpseBurn = 2.5f;
        private const int MaxLights = 6, MaxCrackles = 4;

        /// <summary>Everyone alight (or just out).</summary>
        public static readonly List<Aflame> All = new List<Aflame>();
        /// <summary>How many have caught from someone else's flames rather than a fire's, this session.</summary>
        public static int Spread { get; private set; }
        private static int s_Lights, s_Crackles;

        // Statics outlive a play session in the editor (domain reload is off); the lights and sounds counted didn't.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewSession()
        {
            Spread = 0;
            s_Lights = s_Crackles = 0;
        }

        private Actor _actor;
        private GameObject _source;
        private string _weapon = Weapon;
        private bool _authority, _shown, _fell;
        private float _until, _tickAt;
        private ParticleSystem _flames;
        private ZoneFlameLight _light;
        private AudioSource _crackle;

        /// <summary>True while it's alight.</summary>
        public bool Lit => Time.time < _until;
        /// <summary>Who's burning.</summary>
        public Actor Actor => _actor;

        /// <summary>Sets <paramref name="a"/> alight, or keeps it alight (it burns: the server's, or one playing alone),
        /// started by <paramref name="source"/> with <paramref name="weapon"/> (what its burns count as; a fire's unless
        /// given). Whoever lit it first keeps the credit while it burns.</summary>
        public static Aflame Ignite(Actor a, GameObject source, string weapon = Weapon)
        {
            Aflame b = Get(a);
            if (b == null) return null;
            if (!b.Lit || b._source == null)
            {
                b._source = source;
                b._weapon = string.IsNullOrEmpty(weapon) ? Weapon : weapon;
            }
            b._authority = true;
            b.Kindle(a.Kind == ActorKind.Zombie ? DeadAfterburn : LivingAfterburn);
            return b;
        }

        /// <summary>Shows <paramref name="a"/> alight for <paramref name="seconds"/> more without burning it (a client's
        /// copy).</summary>
        public static void Show(Actor a, float seconds)
        {
            Aflame b = Get(a);
            if (b != null) b.Kindle(seconds);
        }

        /// <summary>True while <paramref name="a"/> is alight.</summary>
        public static bool IsOn(Actor a) => a != null && a.TryGetComponent(out Aflame b) && b.Lit;

        private static Aflame Get(Actor a)
        {
            if (a == null) return null;
            if (!a.TryGetComponent(out Aflame b))
            {
                b = a.gameObject.AddComponent<Aflame>();
                b._actor = a;
            }
            return b;
        }

        private void Kindle(float seconds)
        {
            if (!Lit && _actor != null && _actor.Kind == ActorKind.Player && _actor.IsAlive) GameEvents.RaiseToast("You're on fire!");
            _until = Mathf.Max(_until, Time.time + seconds);
            enabled = true;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable()
        {
            All.Remove(this);
            Hide();
        }

        private void Update()
        {
            if (_actor == null) { Destroy(this); return; }
            float now = Time.time;
            bool alive = _actor.IsAlive;
            if (!alive && !_fell)
            {
                _fell = true;
                _until = Mathf.Min(_until, now + CorpseBurn);
            }
            if (now >= _until)
            {
                enabled = false;
                return;
            }
            if (!_shown) ShowFlames();
            if (!_authority || !alive || now < _tickAt) return;
            _tickAt = now + TickEvery;
            Hurt();
            Pass();
        }

        private void Hurt()
        {
            IDamageable d = _actor.Damageable;
            if (d == null || d.IsDead) return;
            bool dead = _actor.Kind == ActorKind.Zombie;
            var info = new DamageInfo((dead ? DeadDps : LivingDps) * TickEvery, DamageKind.Burn, _actor.Center, Vector3.up, _source)
            {
                HpScale = true, WeaponId = _weapon,
            };
            // The living's limbs take it in turn; the dead burn all over alike (no limb's share of it).
            if (!dead) info = info.WithZone(BodyZones.RandomStrike());
            CombatResolver.Apply(d, info, _source != null ? _source.GetComponentInParent<Actor>() : null);
        }

        // The fire catches on whoever it's pressed against.
        private void Pass()
        {
            if (_actor == null || !_actor.IsAlive) return;
            Vector3 p = _actor.transform.position;
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor o = Actor.All[i];
                if (o == null || o == _actor || !o.IsAlive) continue;
                Vector3 q = o.transform.position;
                float dx = q.x - p.x, dz = q.z - p.z;
                if (dx * dx + dz * dz > SpreadReach * SpreadReach || Mathf.Abs(q.y - p.y) > 1.2f) continue;
                if (IsOn(o) || Random.value > SpreadChance) continue;
                Ignite(o, _source, _weapon);
                Spread++;
            }
        }

        private void ShowFlames()
        {
            _shown = true;
            if (_flames == null) _flames = FireFx.BodyFlames(transform, _actor.Height);
            else _flames.Play();
            if (_light == null && s_Lights < MaxLights)
            {
                _light = ZoneFlameLight.Spawn(transform, transform.position + Vector3.up * _actor.Height * 0.6f, FireFx.FlameColor, 6f, 2f, 0.6f);
                s_Lights++;
            }
            if (_crackle == null && s_Crackles < MaxCrackles)
            {
                _crackle = Sfx.Loop(SfxIds.FireCrackle, transform, 0.6f, 18f, 1.15f);
                if (_crackle != null) s_Crackles++;
            }
        }

        private void Hide()
        {
            _shown = false;
            if (_flames != null) _flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_light != null)
            {
                Destroy(_light.gameObject);
                _light = null;
                s_Lights = Mathf.Max(0, s_Lights - 1);
            }
            if (_crackle != null)
            {
                Sfx.Stop(_crackle);
                _crackle = null;
                s_Crackles = Mathf.Max(0, s_Crackles - 1);
            }
        }
    }

    /// <summary>Flames, smoke, sparks and scorch for the fire and whatever burns or fizzes: particles over the shared ember and
    /// smoke materials, nothing to load.</summary>
    internal static class FireFx
    {
        public static readonly Color FlameColor = new Color(1f, 0.58f, 0.24f);
        private static readonly Quaternion Up = Quaternion.Euler(-90f, 0f, 0f);
        private static Gradient s_FlameFade, s_SmokeFade;
        private static Material s_Scorch;

        /// <summary>Flames rising off a disc of <paramref name="radius"/> at <paramref name="at"/> (world), at
        /// <paramref name="rate"/> a second, sized by <paramref name="size"/>.</summary>
        public static ParticleSystem Flames(Transform parent, Vector3 at, float radius, float rate, float size)
        {
            ParticleSystem ps = Make("Flames", parent, at, out ParticleSystemRenderer r);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f * size, 1.7f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.24f * size, 0.6f * size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.7f), new Color(1f, 0.78f, 0.5f));
            main.maxParticles = 160;
            main.gravityModifier = -0.12f;
            SetRate(ps, rate);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = Mathf.Max(0.01f, radius);
            shape.radiusThickness = 1f;
            Rise(ps, FlameFade, 0.15f);
            r.sharedMaterial = ZoneKit.EmberMaterial();
            ps.Play();
            return ps;
        }

        /// <summary>Flames all over a body <paramref name="height"/> tall (on <paramref name="body"/>, rising off it as it
        /// walks).</summary>
        public static ParticleSystem BodyFlames(Transform body, float height)
        {
            ParticleSystem ps = Flames(body, body.position + Vector3.up * height * 0.45f, 0.2f, 55f, 0.75f);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.42f, 0.3f, height * 0.8f);
            return ps;
        }

        /// <summary>Dark smoke rising from <paramref name="at"/>.</summary>
        public static ParticleSystem Smoke(Transform parent, Vector3 at, float radius, float rate, float size, float rise)
        {
            ParticleSystem ps = Make("Smoke", parent, at, out ParticleSystemRenderer r);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(rise * 0.6f, rise);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.17f, 0.16f, 0.15f, 0.55f);
            main.maxParticles = 90;
            main.gravityModifier = -0.02f;
            SetRate(ps, rate);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = Mathf.Max(0.01f, radius);
            Rise(ps, SmokeFade, 2.6f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.strength = 0.3f;
            noise.frequency = 0.3f;
            r.sharedMaterial = ZoneKit.SmokeMaterial();
            ps.gameObject.AddComponent<ZoneSmokeTint>();
            ps.Play();
            return ps;
        }

        /// <summary>A fizz of sparks off a fuse at <paramref name="at"/> (<see cref="ParticleSystem.Emit(int)"/> for a burst).</summary>
        public static ParticleSystem Sparks(Transform parent, Vector3 at, float rate)
        {
            ParticleSystem ps = Make("Sparks", parent, at, out ParticleSystemRenderer r);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.6f), new Color(1f, 0.7f, 0.3f));
            main.maxParticles = 120;
            main.gravityModifier = 0.6f;
            SetRate(ps, rate);
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.02f;
            r.sharedMaterial = ZoneKit.EmberMaterial();
            ps.Play();
            return ps;
        }

        /// <summary>A scorch on the floor at <paramref name="at"/>, <paramref name="radius"/> across, that fades after a minute.</summary>
        public static void Scorch(Vector3 at, float radius)
        {
            if (DedicatedServer.Requested) return;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Scorch";
            Object.Destroy(q.GetComponent<Collider>());
            q.layer = CloneZConfig.IgnoreRaycastLayer;
            q.transform.SetPositionAndRotation(at + Vector3.up * 0.02f, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            q.transform.localScale = Vector3.one * radius * 2f;
            var mr = q.GetComponent<MeshRenderer>();
            if (s_Scorch == null)
            {
                s_Scorch = CreatureMaterials.TransparentLit(new Color(0.05f, 0.04f, 0.035f, 0.6f), 0.05f);
                s_Scorch.mainTexture = VisionAssets.SoftDot();
            }
            mr.sharedMaterial = s_Scorch;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            Object.Destroy(q, 60f);
        }

        public static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;
        }

        public static void SetShape(ParticleSystem ps, float radius)
        {
            if (ps == null) return;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.radius = Mathf.Max(0.01f, radius);
        }

        private static ParticleSystem Make(string name, Transform parent, Vector3 at, out ParticleSystemRenderer r)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Up);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        // Grows or shrinks to `endSize` of its start over its life, and colours and fades by `fade`.
        private static void Rise(ParticleSystem ps, Gradient fade, float endSize)
        {
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endSize));
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(fade);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 1.2f;
            noise.quality = ParticleSystemNoiseQuality.Low;
        }

        private static Gradient FlameFade
        {
            get
            {
                if (s_FlameFade != null) return s_FlameFade;
                s_FlameFade = new Gradient();
                s_FlameFade.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.95f, 0.65f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.4f),
                        new GradientColorKey(new Color(0.55f, 0.13f, 0.05f), 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.75f, 0.55f), new GradientAlphaKey(0f, 1f) });
                return s_FlameFade;
            }
        }

        private static Gradient SmokeFade
        {
            get
            {
                if (s_SmokeFade != null) return s_SmokeFade;
                s_SmokeFade = new Gradient();
                s_SmokeFade.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
                return s_SmokeFade;
            }
        }
    }
}
