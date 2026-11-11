using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A noise lure: something thrown to make a din somewhere else. Firecrackers (lit as they leave the hand: a fizzing fuse,
    /// then seconds of bangs like a firefight), a wind-up alarm clock (set to ring a few seconds after it's thrown, for half a
    /// minute) and a transistor radio taped to a kitchen timer (it comes on a little later and blares for a minute). While one
    /// sounds it calls the dead to it — every pulse is a lure (<see cref="NoiseSystem.EmitLure"/>): the dead in earshot come to
    /// look, and those that have lost sight of whoever they were after give the chase up for it, off a door they were
    /// battering, away from the house its thrower hides in (<see cref="ZombieAI"/>). Mutants come to it as to any lure. The
    /// clock and the radio still work afterwards: each is left lying where it stopped, to be picked up and thrown again; the
    /// firecrackers are spent.
    ///
    /// The thrower's machine flies it. Once it sounds, the server (which runs the dead) sounds a copy of its own where it
    /// lies, and the other players hear one (<see cref="NetThrowables"/>).
    /// </summary>
    public sealed class NoiseLure : MonoBehaviour
    {
        /// <summary>One kind of lure.</summary>
        public sealed class Kind
        {
            public string Id;
            /// <summary>Seconds from the throw until it sounds, and how long it sounds.</summary>
            public float Delay, Duration;
            /// <summary>How far it carries, metres (one of the dead hears it from its own hearing further off).</summary>
            public float Radius;
            /// <summary>Still works once it stops: it's left lying, to be picked up again.</summary>
            public bool Reusable;
            /// <summary>What it plays while it sounds (looped), or null (the firecrackers' bangs are one by one).</summary>
            public string Loop;
            public float LoopVolume, LoopReach;
        }

        public static readonly Kind Firecrackers = new Kind { Id = Throwables.Firecrackers, Delay = 1.6f, Duration = 7f, Radius = 45f };
        public static readonly Kind AlarmClock = new Kind
        {
            Id = Throwables.AlarmClock, Delay = 4f, Duration = 30f, Radius = 32f, Reusable = true,
            Loop = SfxIds.AlarmBell, LoopVolume = 1f, LoopReach = 70f,
        };
        public static readonly Kind TimerRadio = new Kind
        {
            Id = Throwables.TimerRadio, Delay = 7f, Duration = 60f, Radius = 24f, Reusable = true,
            Loop = SfxIds.RadioPlay, LoopVolume = 0.9f, LoopReach = 55f,
        };

        /// <summary>The lure an item id makes, or null.</summary>
        public static Kind Of(string id)
            => id == Throwables.Firecrackers ? Firecrackers : id == Throwables.AlarmClock ? AlarmClock : id == Throwables.TimerRadio ? TimerRadio : null;

        /// <summary>Every lure out in the world, thrown or heard.</summary>
        public static readonly List<NoiseLure> All = new List<NoiseLure>();

        private const float PulseEvery = 0.3f, CallEvery = 2f, MaxLife = 150f;
        private static readonly Color FlashColor = new Color(1f, 0.82f, 0.55f);

        /// <summary>What it is.</summary>
        public Kind Def { get; private set; }
        /// <summary>Its thrower's copy: the one that flies, and that's left lying once it stops.</summary>
        public bool Thrown { get; private set; }
        /// <summary>The server's copy (or the only one, playing alone): it calls the dead.</summary>
        public bool Authority { get; private set; }
        /// <summary>True while it makes its din.</summary>
        public bool Sounding => _t >= _delay && _t < _delay + _duration;
        /// <summary>When it started sounding (<see cref="Time.time"/>), or -1 before then.</summary>
        public float SoundedAt { get; private set; } = -1f;
        /// <summary>How many bangs it has let off (firecrackers).</summary>
        public int Bangs { get; private set; }

        private float _t, _delay, _duration, _pulseAt, _callAt, _bangAt;
        private bool _started, _done, _landed;
        private Rigidbody _rb;
        private Transform _model;
        private Quaternion _rest;
        private AudioSource _loop, _fuse;
        private Light _flash;
        private ParticleSystem _sparks, _smoke;

        /// <summary>Throws one from the hand (<see cref="Throwables.Throw"/>).</summary>
        public static NoiseLure Throw(string id, Vector3 from, Vector3 velocity, GameObject thrower)
        {
            Kind k = Of(id);
            if (k == null) return null;
            GameObject go = Throwables.Body(id, from, velocity, thrower, k == Firecrackers ? 0.15f : 0.45f, true);
            NoiseLure l = go.AddComponent<NoiseLure>();
            l.Begin(k, k.Delay, k.Duration, NetAuthority.IsServer, true);
            return l;
        }

        /// <summary>One sounding where another machine's came down (<see cref="NetThrowables"/>), for
        /// <paramref name="seconds"/>: with <paramref name="authority"/> (the server) it calls the dead; elsewhere it's only
        /// heard. It's set down on whatever lies under <paramref name="at"/> (firecrackers can go off before they land).</summary>
        public static NoiseLure Remote(string id, Vector3 at, float seconds, bool authority)
        {
            Kind k = Of(id);
            if (k == null) return null;
            GameObject go = Throwables.Body(id, at, Vector3.zero, null, 0f, false);
            ThrownLook look = go.GetComponent<ThrownLook>();
            if (Physics.Raycast(at + Vector3.up * 0.3f, Vector3.down, out RaycastHit floor, 60f, CloneZConfig.WorldMask, QueryTriggerInteraction.Ignore))
                go.transform.position = floor.point + Vector3.up * (look != null ? look.Size.y * 0.5f : 0.05f);
            NoiseLure l = go.AddComponent<NoiseLure>();
            l.Begin(k, 0f, Mathf.Clamp(seconds, 0f, k.Duration), authority, false);
            return l;
        }

        private void Begin(Kind k, float delay, float duration, bool authority, bool thrown)
        {
            Def = k;
            _delay = delay;
            _duration = duration;
            Authority = authority;
            Thrown = thrown;
            _rb = GetComponent<Rigidbody>();
            ThrownLook look = GetComponent<ThrownLook>();
            _model = look != null ? look.Model : null;
            if (_model != null) _rest = _model.localRotation;
            if (k != Firecrackers) return;
            _sparks = FireFx.Sparks(transform, transform.position, delay > 0f ? 30f : 0f);
            if (delay > 0f) _fuse = Sfx.Loop(SfxIds.FlareBurn, transform, 0.25f, 12f, 2.2f);
            var lit = new GameObject("Flash");
            lit.transform.SetParent(transform, false);
            lit.transform.localPosition = Vector3.up * 0.3f;
            _flash = lit.AddComponent<Light>();
            _flash.type = LightType.Point;
            _flash.color = FlashColor;
            _flash.range = 9f;
            _flash.intensity = 0f;
            _flash.shadows = LightShadows.None;
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        // It thuds down and stays about where it lands, rather than skittering off.
        private void OnCollisionEnter(Collision c)
        {
            if (_landed || _rb == null) return;
            _landed = true;
            _rb.linearDamping = 2.5f;
            _rb.angularDamping = 4f;
            Sfx.Play(SfxIds.BoltLand, transform.position, 0.45f, 0.7f, 20f);
        }

        private void OnDestroy()
        {
            Sfx.Stop(_loop);
            Sfx.Stop(_fuse);
        }

        private void Update()
        {
            if (Def == null || _done) return;
            float dt = Time.deltaTime;
            _t += dt;
            if (transform.position.y < -200f || _t > MaxLife) { Destroy(gameObject); return; }
            if (_t < _delay) return;
            if (!_started) StartSounding();
            if (_t >= _delay + _duration) { Finish(); return; }

            Vector3 at = transform.position;
            if (Authority && Time.time >= _pulseAt)
            {
                _pulseAt = Time.time + PulseEvery;
                NoiseSystem.EmitLure(at, Def.Radius);
            }
            if (Authority && Time.time >= _callAt)
            {
                _callAt = Time.time + CallEvery;
                GameEvents.RaiseWorldEvent(at, Def.Radius * 1.5f, "lure");
            }
            if (Def == Firecrackers) Crackle(at, dt);
            else Rattle(Def == AlarmClock ? 7f : 1.5f);
        }

        // The fizz and the smoke go up, however the firecrackers have come to lie.
        private void LateUpdate()
        {
            if (_sparks != null) _sparks.transform.rotation = Upward;
            if (_smoke != null) _smoke.transform.rotation = Upward;
        }

        private static readonly Quaternion Upward = Quaternion.Euler(-90f, 0f, 0f);

        private void StartSounding()
        {
            _started = true;
            SoundedAt = Time.time;
            Sfx.Stop(_fuse);
            _fuse = null;
            if (_sparks != null) SetRate(_sparks, 0f);
            if (Def.Loop != null) _loop = Sfx.Loop(Def.Loop, transform, Def.LoopVolume, Def.LoopReach);
            if (Def == Firecrackers) _smoke = FireFx.Smoke(transform, transform.position, 0.2f, 7f, 0.5f, 0.5f);
            if (Thrown) NetThrowables.Sounding(Def.Id, transform.position, _delay + _duration - _t);
        }

        // Firecrackers: bangs in bursts and lulls, each with a flash, a spray of sparks and a hop.
        private void Crackle(Vector3 at, float dt)
        {
            if (_flash != null) _flash.intensity = Mathf.MoveTowards(_flash.intensity, 0f, dt * 90f);
            if (Time.time < _bangAt) return;
            _bangAt = Time.time + (Random.value < 0.3f ? Random.Range(0.25f, 0.5f) : Random.Range(0.04f, 0.16f));
            Bangs++;
            Sfx.Play(SfxIds.Firecracker, at + Random.insideUnitSphere * 0.15f, Random.Range(0.75f, 1f), Random.Range(0.9f, 1.15f), 160f);
            if (_flash != null) _flash.intensity = Random.Range(4f, 7f);
            if (_sparks != null) _sparks.Emit(Random.Range(6, 14));
            if (Authority) NoiseSystem.EmitLure(at, Def.Radius);
            if (_rb != null && !_rb.isKinematic)
            {
                _rb.AddForce(new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(0.4f, 1f), Random.Range(-0.3f, 0.3f)) * 0.35f, ForceMode.Impulse);
                _rb.AddTorque(Random.insideUnitSphere * 0.02f, ForceMode.Impulse);
            }
        }

        // A ringing clock dances on its feet; a radio buzzes a little.
        private void Rattle(float degrees)
        {
            if (_model == null) return;
            float t = Time.time * 31f;
            _model.localRotation = _rest * Quaternion.Euler(Mathf.Sin(t) * degrees, Mathf.Sin(t * 1.7f) * degrees * 0.5f, Mathf.Cos(t * 1.3f) * degrees);
        }

        private void Finish()
        {
            _done = true;
            Sfx.Stop(_loop);
            _loop = null;
            if (_model != null) _model.localRotation = _rest;
            if (_flash != null) _flash.intensity = 0f;
            if (Def == Firecrackers)
            {
                // The smoke drifts off; the spent paper's nothing to pick up.
                if (_smoke != null) SetRate(_smoke, 0f);
                Destroy(gameObject, 3.5f);
                return;
            }
            if (Thrown && Def.Reusable) LeaveLying();
            Destroy(gameObject);
        }

        // Stopped, it's an item again: on the ground where it lies, for anyone to pick up.
        private void LeaveLying()
        {
            ItemDef def = ItemDatabase.Get(Def.Id);
            Vector3 at = transform.position;
            if (def != null && WorldItem.Spawn(null, at, def, 1) != null) PlayerInventory.AnnounceGroundDrop(def, 1, at, null);
        }

        private static void SetRate(ParticleSystem ps, float rate)
        {
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = rate;
        }
    }
}
