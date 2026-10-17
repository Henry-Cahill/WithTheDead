using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The living room's battery wall clock. Its hands follow the time of day round a twelve-hour dial — the hour
    /// hand creeping on between the hours — with or without mains power. "Check the time" reads it out with the day
    /// and the date; "Listen to it tick" is a quiet moment that eases the nerves a little.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClockFixture : Interactable
    {
        private Vector3 _point, _face, _normal;
        private Transform _hour, _minute;
        private float _calmAgainAt;

        public override string DisplayName => "Wall clock";
        public override Vector3 InteractPoint => _point;
        public override bool IsAvailable => base.IsAvailable && LightingFixtures.SurvivorInFront(_face, _normal);

        /// <summary>A clock under <paramref name="host"/> (the wall piece) whose dial is centred on <paramref name="face"/>
        /// (on the wall face) and looks along <paramref name="normal"/>. <paramref name="point"/> is where the survivor reaches.</summary>
        internal static ClockFixture Create(Transform host, Vector3 point, Vector3 face, Vector3 normal)
        {
            var go = new GameObject("WallClock");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(face, Quaternion.identity);
            var c = go.AddComponent<ClockFixture>();
            c._point = point;
            c._face = face;
            c._normal = normal;
            return c;
        }

        /// <summary>Hands over the hands: each drawn pointing at twelve, its object's origin on the dial's centre.</summary>
        internal void SetHands(Transform hour, Transform minute)
        {
            _hour = hour;
            _minute = minute;
            Turn();
        }

        private void Update() => Turn();

        private void Turn()
        {
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod == null) return;
            // Turning about the outward normal sweeps twelve toward three, as a viewer facing the dial sees it.
            float hour = Mathf.Repeat(tod.Hour, 12f) * 30f;
            float minute = Mathf.Repeat(tod.Hour, 1f) * 360f;
            if (_hour != null) _hour.rotation = Quaternion.AngleAxis(hour, _normal);
            if (_minute != null) _minute.rotation = Quaternion.AngleAxis(minute, _normal);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Check the time", CheckTime));
            into.Add(new InteractAction("Listen to it tick", Listen)
                .Timed(CloneZConfig.ClockListenSeconds, "Listening to the clock", HumanoidActivity.None));
        }

        private static void CheckTime(InteractContext ctx)
        {
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
