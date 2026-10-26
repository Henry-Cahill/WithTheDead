using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The rural mailbox at the kerb. "Check the mail" opens its door and reads whatever has come, a line at a
    /// time: the last ordinary post of the first few days — a bill and a church flyer, then a county public-health
    /// notice, then a neighbour's note pushed in by hand — and after that nothing at all, because the mail stopped
    /// coming. The little red flag goes up and down, for all the good it does. A raid house's box holds its own post
    /// instead, all of it come already (see the overload of <see cref="Create(Transform, Vector3, string[][])"/>).
    /// Notes the survivor leaves in it are read after the post. Remembers how much of the post has been read
    /// (<see cref="FixtureSave.A"/>) and how many of the notes (<see cref="FixtureSave.B"/>), the notes themselves and
    /// the letters marked important (<see cref="FixtureSave.Text"/>), the door and the flag.
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
                "County water department notice: scheduled main repair on March 5th, 2‑4 am. Expect brief pressure loss.",
                "Neighborhood watch call: meeting at the Miller barn on Thursday, 7 pm. Bring flashlights.",
                "Flyer from the local co‑op: FALL HARVEST FESTIVAL – live music, homemade preserves, kids' games.",
                "Hand‑written note from Mrs. Caldwell: \"If you see any stray cats, please bring them to my porch. I’ll feed them.\"",
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
        private int _read;   // post deliveries read so far (the survivor's notes are counted by _notesRead)
        private List<bool> _important; // one entry per delivery, true = player marked it important
        private List<string> _playerNotes; // personal notes left by the survivor
        private int _notesRead;   // how many of _playerNotes have been read
        private string[][] _post;   // a raid house's own post, all of it come already (null: the starter home's days of post)

private void Awake()
{
    // Ensure the importance list matches the number of deliveries for this mailbox
    // Initialize with empty lists; importance list will be sized when needed based on actual post length.
    _important = new List<bool>();
    _playerNotes = new List<string>();
}

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
        private int BaseDeliveries => _post != null ? _post.Length : Delivered;
        // What's still to read: the post first, then the survivor's own notes.
        private int UnreadPost => Mathf.Max(0, BaseDeliveries - _read);
        private int UnreadNotes => Mathf.Max(0, _playerNotes.Count - _notesRead);

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            int unread = UnreadPost + UnreadNotes;
            InteractAction check = unread > 0 ? ReadNext() : new InteractAction("Check the mail", CheckEmpty);
            InteractAction close = _open ? new InteractAction("Close the mailbox", c => SetOpen(false, false)) : null;
            if (close != null && unread == 0) into.Add(close);
            into.Add(check);
            // Offer to read all remaining mail at once when more than one unread delivery remains
            if (unread > 1)
                into.Add(new InteractAction("Read all mail", c =>
                {
                    SetOpen(true, false);
                    int come = BaseDeliveries;
                    for (int i = _read; i < come; i++)
                        foreach (var line in Post[i])
                            c.Notify(line);
                    for (int i = _notesRead; i < _playerNotes.Count; i++)
                        c.Notify(_playerNotes[i]);
                    _read = Mathf.Max(_read, come);
                    _notesRead = _playerNotes.Count;
                    c.Notify("All mail has been read.");
                }));
            if (ctx.Inventory != null && ctx.Inventory.Has("paper"))
                {
into.Add(new InteractAction("Leave a note", c =>
{
    // consume one paper
    var inv = ctx.Inventory;
    bool removed = false;
    // Find first stack with ID "paper"
    foreach (var stack in inv.Inventory.Stacks)
    {
        if (stack.Def != null && stack.Def.Id == "paper")
        {
            inv.Inventory.Remove(stack, 1);
            removed = true;
            break;
        }
    }
    if (!removed)
    {
        c.Notify("You have no paper to write a note.");
        return;
    }
    // Add placeholder note
    _playerNotes.Add("A handwritten note from you: \u201CStay safe, stay warm.\u201D");
    c.Notify("You slide a personal note into the mailbox. It will arrive with the next batch of mail.");
}));
                }
            if (close != null && unread > 0) into.Add(close);
            into.Add(_flagUp
                ? new InteractAction("Lower the flag", c => SetFlag(c, false))
                : new InteractAction("Raise the flag", c => SetFlag(c, true)));
            // Offer to toggle importance on the last post delivery read (once one has been read; notes don't count)
            if (_read > 0 && _read <= Post.Length)
            {
if (_important == null || _important.Count != Post.Length)
    _important = new List<bool>(new bool[Post.Length]);
                bool currently = _important[_read - 1];
                string label = currently ? "Unmark last letter as important" : "Mark last letter as important";
                into.Add(new InteractAction(label, c =>
                {
                    _important[_read - 1] = !currently;
                    c.Notify(_important[_read - 1]
                        ? "You fold a small paper bookmark into the letter – it will stay with you."
                        : "You remove the bookmark; the letter is no longer marked.");
                }));
            }
            into.Add(new InteractAction("Inspect the mailbox", c =>
            {
                int left = UnreadPost + UnreadNotes;
                c.Notify(string.Format("Mailbox is {0}, flag is {1}, {2}", _open ? "open" : "closed", _flagUp ? "up" : "down",
                    left == 0 ? "no more mail." : left == 1 ? "1 unread letter." : $"{left} unread letters."));
            }));
            // If any letters have been marked, let the player review them
            if (_important != null && _important.Exists(b => b))
            {
                into.Add(new InteractAction("Recall important letters", c =>
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Important letters you kept:");
                    for (int i = 0; i < _important.Count; i++)
                    {
                        if (_important[i])
                        {
                            sb.AppendLine($"- Day {i + 1}:");
                            foreach (var line in Post[i])
                                sb.AppendLine($" \"{line}\"");
                        }
                    }
                    c.Notify(sb.ToString());
                }));
            }
            if (unread == 0)
            {
                into.Add(new InteractAction("Search the mailbox", c =>
                {
                    c.Notify("You search the mailbox but find nothing else.");
                }));
            }
        }

        // Reading the oldest unread delivery (the post first, then the survivor's notes): the door drops open and the
        // letter's lines come up one by one.
        private InteractAction ReadNext()
        {
            // Settled now, so a day's post arriving mid-read can't change which count this read advances.
            bool note = UnreadPost == 0;
            string[] lines = note ? new[] { _playerNotes[_notesRead] } : Post[_read];
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
                if (note) _notesRead = Mathf.Min(_notesRead + 1, _playerNotes.Count);
                else _read = Mathf.Min(_read + 1, Post.Length);
                if (UnreadPost + UnreadNotes > 0) {
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
            save.B = _notesRead;
            // Serialize importance list as a comma‑separated "0/1" string
            var importance = _important.Count > 0
    ? string.Join(",", _important.ConvertAll(b => b ? "1" : "0"))
    : "";
            // Serialize player notes as a semicolon‑separated list
            var notes = string.Join(";", _playerNotes);
            save.Text = importance + "|" + notes;
        }

        public override void RestoreState(FixtureSave save)
        {
            SetOpen((save.State & 1) != 0, true);
            _flagUp = (save.State & 2) != 0;
            if (_flag != null) _flag.SetOpen(_flagUp, true);
            // Deserialize importance list; if the saved string is malformed, fall back to all false
            _important = new List<bool>(new bool[Post.Length]);
            _playerNotes = new List<string>();
            if (!string.IsNullOrEmpty(save.Text))
            {
                var split = save.Text.Split('|');
                // first part = importance
                var impParts = split[0].Split(',');
                for (int i = 0; i < Mathf.Min(impParts.Length, _important.Count); i++)
                    _important[i] = impParts[i] == "1";
                // second part = notes (may be empty)
                if (split.Length > 1 && !string.IsNullOrEmpty(split[1]))
                    _playerNotes.AddRange(split[1].Split(';'));
            }
            // Ensure the importance list matches the actual post length, preserving existing flags where possible
if (_important.Count != Post.Length)
{
    var resized = new List<bool>(new bool[Post.Length]);
    for (int i = 0; i < Mathf.Min(_important.Count, resized.Count); i++)
        resized[i] = _important[i];
    _important = resized;
}
        }
    }
}
