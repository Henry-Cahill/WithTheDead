using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The rural mailbox at the kerb. "Check the mail" opens its door and reads whatever has come, a line at a
    /// time: the last ordinary post of the first few days — a bill and a church flyer, then a county public-health
    /// notice, then a neighbour's note pushed in by hand — and after that nothing at all, because the mail stopped
    /// coming. The little red flag goes up and down, for all the good it does. A raid house's box holds its own post
    /// instead, all of it come already (see the overload of <see cref="Create(Transform, Vector3, string[][])"/>).
    /// Remembers how much of the post has been read (<see cref="FixtureSave.A"/>), the door and the flag.
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
                "\"Saw your lights on. We're heading to my sister's in the city — they say there's a shelter.\"",
                "\"If we're not back by Sunday, please let Biscuit out of the yard. Thank you! — June, no. 14\"",
            },
            new[]
            {
                "A winter weather advisory: heavy snow expected tomorrow. Keep warm and stay indoors unless necessary.",
                "Roads may be icy; clear driveways and expect occasional power outages.",
                "Stock up on firewood and blankets now to stay safe through the cold night."
            },
            new[]
            {
                "Community request: we need firewood for the coming freeze. If you have spare logs, please leave them at the shed.",
                "Any contributions will be shared among families in the block. Thank you for your generosity.",
                "Contact the neighborhood watch on the posted number for coordination.",
                "Neighborly aid: the Miller family ran low on potatoes after the frost; any spares can be left at the corner shed.",
                "Winter weather update: another cold front rolls in tonight, expect sub‑zero temps and icy roads; keep heaters on low.",
                "Farm equipment trouble: the old tractor's belt snapped in the field; if you have a replacement belt or tools, please leave them by the barn.",
                "Church announcement: Sunday service will be followed by a pot‑luck; bring a home‑cooked casserole or jar of preserves, no store‑bought goods."
            },
            new[]
            {
                "Missing pet notice: our cat Whiskers disappeared near the old mill road two nights ago.",
                "She is grey with a white patch on her chest; please check your barns and call if found.",
                "Owner will reward with a fresh fish and gratitude.",
                "Missing dog notice: our border collie Daisy vanished near the creek bend last afternoon; please look for a black‑and‑white coat.",
                "Cold‑weather alert: several hens were found frozen in the barn; anyone with spare warm blankets, please drop them off at the community shed.",
                "Fire‑wood request: we have a bundle of dry pine left; if you can spare a stack, leave it by the north fence.",
                "Thank‑you note: the Miller family rescued a stray calf from the icy creek and are grateful for the extra hay donations.",
                "Reminder: check the cellar for frozen canned goods before the power goes out again; share any surplus with neighbours."
            },
            new[]
            {
                "Hand‑written reminder: church pot‑luck this Sunday after service. Bring a homemade dish to share.",
                "Suggested dishes: casserole, pies, preserves. No store‑bought items please.",
                "Sign up on the board in the foyer to avoid duplicates."
            },
        };

        private string _key;
        private Vector3 _point;
        private FixturePart _door, _flag;
        private bool _open, _flagUp;
        private int _read;   // deliveries read so far
        private string[][] _post;   // a raid house's own post, all of it come already (null: the starter home's days of post)

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

        /// <summary>A mailbox whose <paramref name="post"/> has all come already (a raid house's: the letters nobody was
        /// home to read), read a delivery at a time like the starter home's.</summary>
        internal static MailboxFixture Create(Transform host, Vector3 point, string[][] post)
        {
            MailboxFixture m = Create(host, point);
            m._post = post != null && post.Length > 0 ? post : null;
            return m;
        }

        private string[][] Post => _post ?? Deliveries;

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
                if (tod == null) return 0;
                int day = tod.Hour < CloneZConfig.MailDeliveryHour ? tod.Day - 1 : tod.Day;
                return Mathf.Clamp(day, 0, Deliveries.Length);
            }
        }

        // How many deliveries this box holds by now (a raid house's post has all come).
        private int Come => _post != null ? _post.Length : Delivered;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            InteractAction check = _read < Come ? ReadNext() : new InteractAction("Check the mail", CheckEmpty);
            InteractAction close = _open ? new InteractAction("Close the mailbox", c => SetOpen(false, false)) : null;
            if (close != null && _read >= Come) into.Add(close);
            into.Add(check);
            if (close != null && _read < Come) into.Add(close);
            into.Add(_flagUp
                ? new InteractAction("Lower the flag", c => SetFlag(c, false))
                : new InteractAction("Raise the flag", c => SetFlag(c, true)));
            // New lightweight inspection action
            into.Add(new InteractAction("Inspect the mailbox", ctx => {
                ctx.Notify($"Mailbox is {(_open ? "open" : "closed")}, flag is {(_flagUp ? "up" : "down")}, {(_read < Come ? $"{Come - _read} unread letters." : "no more mail.")}");
            }));
            // New action after all mail read
            if (_read >= Come) {
                into.Add(new InteractAction("Search the mailbox", ctx => {
                    ctx.Notify("You search the mailbox but find nothing else.");
                }));
            }
        }

        // Reading the oldest unread delivery: the door drops open and the letter's lines come up one by one.
        private InteractAction ReadNext()
        {
            string[] lines = Post[_read];
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
                _read = Mathf.Min(_read + 1, Post.Length);
                if (_read < Come) {
                    c.Notify("There's more post underneath.");
                } else {
                    // Subtle hope after the final piece of mail
                    c.Notify("A faint glimmer of hope lingers in the last letter.");
                }
            };
            return a;
        }

        private void CheckEmpty(InteractContext ctx)
        {
            SetOpen(true, false);
            if (_post != null)
            {
                ctx.Notify("Nothing else in the box. Whoever lived here never came out for it.");
                return;
            }
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
            _read = Mathf.Clamp(Mathf.RoundToInt(save.A), 0, Post.Length);
            SetOpen((save.State & 1) != 0, true);
            _flagUp = (save.State & 2) != 0;
            if (_flag != null) _flag.SetOpen(_flagUp, true);
        }
    }
}
