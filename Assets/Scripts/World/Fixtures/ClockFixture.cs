using System.Collections.Generic;
using System.Globalization;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// The living room's battery wall clock. Its hands follow the time of day round a twelve-hour dial — the hour
    /// hand creeping on between the hours — with or without mains power. "Check the time" reads it out with the day
    /// and the date; "Listen to it tick" is a quiet moment that eases the nerves a little. A clock whose battery has
    /// run down (now and then, one of the raid town's) keeps its hands where they stopped and can only tell that time.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClockFixture : Interactable
    {
        private Vector3 _point, _face, _normal;
        private Transform _hour, _minute;
        private float _calmAgainAt;
        private float _stoppedAt = -1f;   // the hour (0..12) a stopped clock's hands show; below 0 while it runs
        // cache of last assigned hand angles; NaN forces first assignment
        private float _lastHourAngle = float.NaN;
        private float _lastMinuteAngle = float.NaN;

        public override string DisplayName => "Wall clock";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);

        /// <summary>True for a clock whose battery has run down: its hands stay where they stopped.</summary>
        public bool Stopped => _stoppedAt >= 0f;

        /// <summary>A clock under <paramref name="host"/> (the wall piece) whose dial is centred on <paramref name="face"/>
        /// (on the wall face) and looks along <paramref name="normal"/>. <paramref name="point"/> is where the survivor reaches.
        /// A clock <paramref name="stoppedAt"/> an hour (0..12; below 0 for a clock that runs) has a flat battery.</summary>
        internal static ClockFixture Create(Transform host, Vector3 point, Vector3 face, Vector3 normal, float stoppedAt = -1f)
        {
            var go = new GameObject("WallClock");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(face, Quaternion.identity);
            var c = go.AddComponent<ClockFixture>();
            c._point = point;
            c._face = face;
            c._normal = normal;
            c._stoppedAt = stoppedAt < 0f ? -1f : Mathf.Repeat(stoppedAt, 12f);
            return c;
        }

        /// <summary>Hands over the hands: each drawn pointing at twelve, its object's origin on the dial's centre.</summary>
        internal void SetHands(Transform hour, Transform minute)
        {
            _hour = hour;
            _minute = minute;
            Turn();
        }

        /// <summary>Gives the clock the usual pair of hands (as the home's clock has them) over a dial drawn with
        /// <see cref="FurnitureBuilder.ClockDial"/> on the clock's face.</summary>
        internal void AddHands()
        {
            Transform hour = Hand("HourHand", 0.075f, 0.012f, 0.0455f);
            Transform minute = Hand("MinuteHand", 0.112f, 0.008f, 0.049f);
            SetHands(hour, minute);
        }

        private Transform Hand(string name, float length, float width, float lift)
        {
            var mesh = new RaidMesh();
            FurnitureBuilder.ClockHand(mesh, _face, _normal, length, width, lift);
            GameObject go = mesh.Emit(name, transform, _face, ShadowCastingMode.Off);
            go.transform.rotation = Quaternion.identity;   // drawn in world axes, pointing at twelve
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            return go.transform;
        }

        private void Update()
        {
            if (_stoppedAt < 0f) Turn();
        }

        private void Turn()
        {
            float hours;
            if (_stoppedAt >= 0f) hours = _stoppedAt;
            else
            {
                TimeOfDay tod = TimeOfDay.Instance;
                if (tod == null) return;
                hours = tod.Hour;
            }
            // Turning about the outward normal sweeps twelve toward three, as a viewer facing the dial sees it.
            float hour = Mathf.Repeat(hours, 12f) * 30f;
            float minute = Mathf.Repeat(hours, 1f) * 360f;
            // Only assign rotations if angle changed > 0.5 degrees since last assignment
            if (_hour != null && (float.IsNaN(_lastHourAngle) || Mathf.Abs(Mathf.DeltaAngle(_lastHourAngle, hour)) > 0.5f))
            {
                _hour.rotation = Quaternion.AngleAxis(hour, _normal);
                _lastHourAngle = hour;
            }
            if (_minute != null && (float.IsNaN(_lastMinuteAngle) || Mathf.Abs(Mathf.DeltaAngle(_lastMinuteAngle, minute)) > 0.5f))
            {
                _minute.rotation = Quaternion.AngleAxis(minute, _normal);
                _lastMinuteAngle = minute;
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Check the time", CheckTime));
            // Polishing the clock provides a calming action regardless of battery state.
            into.Add(new InteractAction("Polish the clock", Polish)
                .Timed(CloneZConfig.ClockListenSeconds, "Polishing the clock", HumanoidActivity.None));
            if (_stoppedAt >= 0f) return;
            into.Add(new InteractAction("Listen to it tick", Listen)
                .Timed(CloneZConfig.ClockListenSeconds, "Listening to the clock", HumanoidActivity.None));
        }

        private void CheckTime(InteractContext ctx)
        {
            if (_stoppedAt >= 0f)
            {
                int sh = Mathf.FloorToInt(_stoppedAt) % 12;
                int sm = Mathf.FloorToInt((_stoppedAt - Mathf.Floor(_stoppedAt)) * 60f);
                ctx.Notify("The hands say " + (sh == 0 ? 12 : sh) + ":" + sm.ToString("00") + " and haven't moved for a while. Its battery must be flat.");
                return;
            }
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod == null)
            {
                ctx.Notify("The hands aren't moving.");
                return;
            }
            int h = Mathf.FloorToInt(tod.Hour) % 24;
            int m = Mathf.FloorToInt((tod.Hour - Mathf.Floor(tod.Hour)) * 60f);
            int h12 = h % 12 == 0 ? 12 : h % 12;
            string date = tod.Date.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);
            ctx.Notify("It's " + h12 + ":" + m.ToString("00") + " " + tod.AmPm + " on day " + tod.Day + " \u2014 " + date + ".");
        }

        private void Polish(InteractContext ctx)
        {
            // Same cooldown logic as Listen, but with a polishing‑themed message.
            if (Time.time >= _calmAgainAt)
            {
                LightingFixtures.Calm(ctx, CloneZConfig.ClockCalm);
                _calmAgainAt = Time.time + CloneZConfig.ClockCalmCooldown;
                ctx.Notify("You gently polish the clock face; the steady motion steadies your thoughts.");
            }
            else ctx.Notify("You polish the clock, but its rhythm offers no new calm right now.");
        }

        private void Listen(InteractContext ctx)
        {
            if (Time.time >= _calmAgainAt)
            {
                LightingFixtures.Calm(ctx, CloneZConfig.ClockCalm);
                _calmAgainAt = Time.time + CloneZConfig.ClockCalmCooldown;
                ctx.Notify("Tick. Tick. Tick. Steady as anything \u2014 your breathing slows to match it.");
            }
            else ctx.Notify("Tick. Tick. Tick. It doesn't care what's going on outside.");
        }
    }
}
