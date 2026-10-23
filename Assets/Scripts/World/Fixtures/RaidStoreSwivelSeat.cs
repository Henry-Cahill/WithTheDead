using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A swivel office chair in the raid town's county office. Like the home's chairs (<see cref="SeatFixture"/>) [E] sits
    /// the survivor down facing the way the chair faces and [E] again gets them up; sitting rests them a little better
    /// than a kitchen chair does. The chair turns on its base: an empty one can be given a shove and left facing wherever
    /// it comes to rest, and sitting in it the survivor can spin themselves round once, the way everyone does in an
    /// office chair when nobody's looking. Nothing about it is saved (raids never are).
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreSwivelSeat : Interactable
    {
        private const string Label = "Office chair";

        private Transform _chair;    // the chair's body: it turns on its base
        private float _yaw;          // its facing as it was set down
        private float _spin;         // how far it has turned since (degrees)
        private float _spinSpeed;    // turning by itself after a shove (deg/s)
        private bool _rocking; // true while the Rock timed action is active
        private float _rockTimer; // elapsed time inside the rock action
        private float _rockOffset; // current rocking offset in degrees
        private const float RockDuration = 5f; // seconds, fits existing UI timing
        private const float RockAmplitude = 8f; // max swing angle
        private const float RockSpeed = 2f; // swings per second
        // Prevents the player from inspecting the chair every frame
        private float _calmAt;
        private float _inspectCooldown; // time until the next inspect is allowed

        public override string DisplayName => Label;
        public override Transform OcclusionRoot => _chair != null ? _chair : transform;

        private float Facing => _yaw + _spin;

        /// <summary>Where the hips rest on the cushion (world).</summary>
        public Vector3 SeatPoint => Frame(new Vector3(0f, CloneZConfig.RaidSwivelSeatHeight, CloneZConfig.RaidSwivelSeatBack));

        public override Vector3 InteractPoint => Frame(new Vector3(0f, CloneZConfig.RaidSwivelSeatHeight + 0.14f, 0.2f));

        /// <summary>A swivel seat for the chair body <paramref name="chair"/> (unrotated; its mesh drawn turned by
        /// <paramref name="yaw"/>).</summary>
        internal static RaidStoreSwivelSeat Create(Transform chair, float yaw)
        {
            var go = new GameObject("Seat_" + Label);
            go.transform.SetParent(chair, false);
            var s = go.AddComponent<RaidStoreSwivelSeat>();
            s._chair = chair;
            s._yaw = yaw;
            return s;
        }

        private Vector3 Frame(Vector3 local)
        {
            Vector3 origin = _chair != null ? _chair.position : transform.position;
            return origin + Quaternion.Euler(0f, Facing, 0f) * local;
        }

        private void Update()
        {
            if (_spinSpeed > 0f)
            {
                float dt = Time.deltaTime;
                _spin = Mathf.Repeat(_spin + _spinSpeed * dt, 360f);
                _spinSpeed = Mathf.Max(0f, _spinSpeed - CloneZConfig.RaidSwivelSpinDrag * dt);
            }
            // Turn always uses current _spin + possible _rockOffset
            Turn();
        }

        // The body (and the seat's own object, riding on it) turned by the spin so far.
        private void Turn()
        {
            if (_chair != null)
            {
                // Apply spin plus any active rocking offset
                _chair.rotation = Quaternion.Euler(0f, _spin + _rockOffset, 0f);
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerSeat seat = ctx.Seat;
            if (seat == null) return;
            if (seat.IsSittingOn(this))
            {
                into.Add(new InteractAction("Stand up", c => c.Seat.Stand()));
                into.Add(SpinRound());
                into.Add(Relax());
                into.Add(Rock());
                into.Add(new InteractAction("Inspect chair", Inspect).DisabledIf(Time.time < _inspectCooldown, "You’ve just examined it."));
                return;
            }
            into.Add(new InteractAction("Sit down", SitDown).DisabledIf(_spinSpeed > 30f, "Let it stop spinning first"));
            into.Add(new InteractAction("Give the chair a spin", Shove));
            into.Add(new InteractAction("Kick the chair", Kick));
            into.Add(new InteractAction("Inspect chair", Inspect).DisabledIf(Time.time < _inspectCooldown, "You’ve just examined it."));
        }

        private void SitDown(InteractContext ctx)
        {
            if (ctx.Seat == null) return;
            if (!ctx.Seat.TrySit(this, SeatPoint, Facing, CloneZConfig.RaidSwivelComfort, out string reason)) ctx.Notify(reason);
        }

        private void Shove(InteractContext ctx)
        {
            _spinSpeed = CloneZConfig.RaidSwivelFreeSpin * Random.Range(0.7f, 1.15f);
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify(Random.value < 0.5f
                ? "The chair whirls round on its castors with a dry little squeak."
                : "You send the chair spinning. It squeaks to a stop facing nowhere in particular.");
        }

        private void Kick(InteractContext ctx)
        {
            // stronger spin than a normal shove
            _spinSpeed = CloneZConfig.RaidSwivelFreeSpin * Random.Range(1.2f, 1.6f);
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify(Random.value < 0.5f
                ? "You kick the chair hard, sending it into a rapid spin!"
                : "The chair rockets around wildly after your kick.");
        }

        // New inspect action: small noise, random tidying comment, minor calm boost, cooldown
        private void Inspect(InteractContext ctx)
        {
            // Small audible cue
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            // Random period‑correct comment
            ctx.Notify(LivingFixtures.RandomTidyingMessage());
            // Minor calm boost
            LivingFixtures.Calm(ctx, CloneZConfig.RaidSwivelSpinCalm * 0.3f);
            // Cooldown – reuse the tidy‑cooldown constant for consistency
            _inspectCooldown = Time.time + CloneZConfig.TidyCooldown;
        }
        private InteractAction Relax()
        {
            var a = new InteractAction("Relax").Timed(CloneZConfig.RaidSwivelSpinSeconds, "Relaxing", HumanoidActivity.None);
            a.Begin = c =>
            {
                // Play the same small fixture noise as a spin
                MakeNoise(CloneZConfig.FixtureNoiseSmall);
            };
            a.Run = c =>
            {
                LivingFixtures.Calm(c, CloneZConfig.RaidSwivelSpinCalm);
                c.Notify("You sit back and relax, feeling a brief moment of calm.");
            };
            return a;
        }

        private InteractAction Rock()
        {
            var a = new InteractAction("Rock chair")
                .Timed(RockDuration, "Rocking", HumanoidActivity.None);
            a.Begin = c =>
            {
                _rocking = true;
                _rockTimer = 0f;
                MakeNoise(CloneZConfig.FixtureNoiseSmall);
            };
            a.Tick = (c, dt, progress) =>
            {
                _rockTimer += dt;
                // sinusoidal swing
                _rockOffset = Mathf.Sin(_rockTimer * RockSpeed * Mathf.PI * 2f) * RockAmplitude;
                Turn(); // apply new offset this frame
                // small calm boost while rocking
                LivingFixtures.Calm(c, CloneZConfig.RaidSwivelSpinCalm * 0.5f);
            };
            a.Stopped = c =>
            {
                _rocking = false;
                _rockOffset = 0f;
                Turn(); // reset rotation
                c.Notify("You stop rocking the chair, feeling a little steadier.");
            };
            return a;
        }

        private InteractAction SpinRound()
        {
            float from = 0f;
            var a = new InteractAction("Spin round").Timed(CloneZConfig.RaidSwivelSpinSeconds, "Spinning round", HumanoidActivity.None);
            a.Begin = c =>
            {
                from = _spin;
                _spinSpeed = 0f;
            };
            a.Tick = (c, dt, progress) => SpinTo(c, from + 360f * Mathf.SmoothStep(0f, 1f, progress));
            a.Stopped = c => SpinTo(c, _spin);
            a.Run = c =>
            {
                SpinTo(c, from);
                if (Time.time >= _calmAt)
                {
                    _calmAt = Time.time + CloneZConfig.TidyCooldown;
                    LivingFixtures.Calm(c, CloneZConfig.RaidSwivelSpinCalm);
                    c.Notify("Round you go, the office wheeling past. For a second you're nine years old again.");
                }
                else c.Notify("You spin round again. It's less fun the second time.");
            };
            return a;
        }

        private void SpinTo(InteractContext ctx, float spin)
        {
            _spin = Mathf.Repeat(spin, 360f);
            Turn();
            if (ctx.Seat != null && ctx.Seat.IsSittingOn(this))
                ctx.Seat.TrySit(this, SeatPoint, Facing, CloneZConfig.RaidSwivelComfort, out _);
        }
    }
}
