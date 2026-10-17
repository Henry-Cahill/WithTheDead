using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The rural mailbox at the kerb. "Check the mail" opens its door and reads whatever has come, a line at a
    /// time: the last ordinary post of the first few days — a bill and a church flyer, then a county public-health
    /// notice, then a neighbour's note pushed in by hand — and after that nothing at all, because the mail stopped
    /// coming. The little red flag goes up and down, for all the good it does. Remembers how much of the post has
    /// been read (<see cref="FixtureSave.A"/>), the door and the flag.
    /// </summary>
    [DisallowMultipleComponent]
    public class MailboxFixture : Interactable
    {
        // One delivery a day for the first days, read a line at a time. All original flavour text.
        private static readonly string[][] Deliveries =
        {
            new[]
            {
                "An electric bill from Tri-County Power & Light: $46.12, due July 23rd. Late fees apply.",
                "A pink flyer from Grace Methodist: BAKE SALE this Saturday! Pies, brownies, lemon bars.",
                "Someone's added in blue pen: \"Bring a dish if you can! All proceeds to the roof fund.\"",
            },
            new[]
            {
                "A county public-health notice, photocopied slightly crooked. URGENT \u2014 READ AND KEEP.",
                "Stay home. Avoid anyone showing signs of \"the illness\": fever, confusion, sudden aggression.",
                "Do not try to nurse the sick at home. Boil all tap water for one full minute before drinking.",
                "\"Help is on the way.\" The hotline number underneath has been struck through by hand.",
            },
            new[]
            {
                "No stamp: a folded note, pushed in by hand. The writing is shaky.",
                "\"Saw your lights on. We're heading to my sister's in the city \u2014 they say there's a shelter.\"",
                "\"If we're not back by Sunday, please let Biscuit out of the yard. Thank you! \u2014 June, no. 14\"",
            },
        };

        private string _key;
        private Vector3 _point;
        private FixturePart _door, _flag;
        private bool _open, _flagUp;
        private int _read;   // deliveries read so far

        public override string DisplayName => "Mailbox";
        public override Vector3 InteractPoint => _point;
        public override string SaveKey => _key;

        /// <summary>A mailbox fixture under <paramref name="host"/> (the yard); <paramref name="point"/> is the door's front.</summary>
        internal static MailboxFixture Create(Transform host, Vector3 point)
        {
            var go = new GameObject("MailboxFixture");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var m = go.AddComponent<MailboxFixture>();
            m._point = point;
            m._key = KeyFor("mailbox", point);
            return m;
        }

        /// <summary>Hands over the door (hinged at its foot to drop open) and the flag (hinged to swing up).</summary>
        internal void SetParts(FixturePart door, FixturePart flag)
        {
            _door = door;
            _flag = flag;
            if (_door != null) _door.SetOpen(_open, true);
            if (_flag != null) _flag.SetOpen(_flagUp, true);
        }

        // How many of the deliveries have come by now: one each morning of the first days.
        private static int Delivered
        {
            get
            {
                TimeOfDay tod = TimeOfDay.Instance;
                if (tod == null) return 1;
                int day = tod.Hour < CloneZConfig.MailDeliveryHour ? tod.Day - 1 : tod.Day;
                return Mathf.Clamp(day, 0, Deliveries.Length);
            }
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            InteractAction check = _read < Delivered ? ReadNext() : new InteractAction("Check the mail", CheckEmpty);
            InteractAction close = _open ? new InteractAction("Close the mailbox", c => SetOpen(false, false)) : null;
            if (close != null && _read >= Delivered) into.Add(close);
            into.Add(check);
            if (close != null && _read < Delivered) into.Add(close);
            into.Add(_flagUp
                ? new InteractAction("Lower the flag", c => SetFlag(c, false))
                : new InteractAction("Raise the flag", c => SetFlag(c, true)));
        }

        // Reading the oldest unread delivery: the door drops open and the letter's lines come up one by one.
        private InteractAction ReadNext()
        {
            string[] lines = Deliveries[_read];
            int shown = 0;
            var a = new InteractAction("Check the mail")
                .Timed(lines.Length * CloneZConfig.MailLineSeconds, "Reading the mail", HumanoidActivity.Use);
            a.Begin = c =>
            {
                SetOpen(true, false);
                shown = 0;
            };
            a.Tick = (c, dt, progress) =>
            {
                int due = Mathf.Min(lines.Length, 1 + Mathf.FloorToInt(progress * lines.Length));
                while (shown < due) c.Notify(lines[shown++]);
            };
            a.Run = c =>
            {
                _read = Mathf.Min(_read + 1, Deliveries.Length);
                if (_read < Delivered) c.Notify("There's more post underneath.");
            };
            return a;
        }

        private void CheckEmpty(InteractContext ctx)
        {
            SetOpen(true, false);
            TimeOfDay tod = TimeOfDay.Instance;
            int day = tod != null ? tod.Day : 1;
            if (day > Deliveries.Length) ctx.Notify("Nothing. The mail stopped coming.");
            else if (Delivered < day) ctx.Notify("Nothing yet \u2014 the mail usually comes by mid-morning.");
            else ctx.Notify("Nothing new since this morning.");
        }

        private void SetOpen(bool open, bool instant)
        {
            if (_open == open && !instant) return;
            _open = open;
            if (_door != null) _door.SetOpen(open, instant);
            if (!instant) MakeNoise(CloneZConfig.FixtureNoiseSmall);
        }

        private void SetFlag(InteractContext ctx, bool up)
        {
            _flagUp = up;
            if (_flag != null) _flag.SetOpen(up);
            ctx.Notify(up
                ? "You flip the little red flag up. Nobody's coming to collect, but old habits die hard."
                : "You fold the flag back down against the box.");
        }

        public override void CaptureState(FixtureSave save)
        {
            save.State = (_open ? 1 : 0) | (_flagUp ? 2 : 0);
            save.A = _read;
        }

        public override void RestoreState(FixtureSave save)
        {
            _read = Mathf.Clamp(Mathf.RoundToInt(save.A), 0, Deliveries.Length);
            SetOpen((save.State & 1) != 0, true);
            _flagUp = (save.State & 2) != 0;
            if (_flag != null) _flag.SetOpen(_flagUp, true);
        }
    }
}
