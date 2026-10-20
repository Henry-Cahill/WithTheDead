using UnityEngine;

namespace CloneZ
{
    /// <summary>The stations the home's sets can pick up: the portable TV's three channels, then the kitchen radio's
    /// three stations.</summary>
    public enum BroadcastStation : byte { TvNews, TvEntertainment, TvPublic, RadioMusic, RadioTalk, RadioEmergency }

    /// <summary>
    /// What's on. Every TV channel and radio station has a scripted schedule that follows the outbreak day by day
    /// (and a little by the hour): ordinary local news with a growing unease on the first day; curfews, evacuation
    /// routes and boil-water notices on the second and third; a skeleton crew's last broadcasts after that, then dead
    /// air — while the emergency frequency drones on by itself. A set asks for its next caption with
    /// <see cref="NextLine"/>, which walks the current programme in order and starts afresh when the programme changes.
    ///
    /// A line may begin with a condition — <c>?water:</c>, <c>?nowater:</c>, <c>?power:</c> or <c>?nopower:</c> —
    /// to be read only while the mains or the grid are still on (or once they're off), and may say
    /// <c>{greeting}</c> ("Good morning"…) or <c>{weekday}</c>. Sounds are written between asterisks.
    /// </summary>
    public static class Broadcasts
    {
        private sealed class Programme
        {
            public int FromDay, ToDay;       // inclusive
            public float FromHour, ToHour;   // the hours it airs (FromHour > ToHour runs past midnight; equal = all day)
            public bool OffAir;              // dead air, static, a test card
            public string[] Lines;
        }

        private sealed class Station
        {
            public string Name;      // "Channel 4", "91.3 FM"
            public string Title;     // "Newswatch 4"
            public Color Caption;    // the caption's source colour
            public Color Screen;     // the TV picture's cast while it's on the air
            public Programme[] Schedule;
        }

        private const int Forever = int.MaxValue;

        private static Programme Days(int from, int to, params string[] lines) =>
            new Programme { FromDay = from, ToDay = to, Lines = lines };

        private static Programme Hours(int from, int to, float fromHour, float toHour, params string[] lines) =>
            new Programme { FromDay = from, ToDay = to, FromHour = fromHour, ToHour = toHour, Lines = lines };

        private static Programme Dead(int from, params string[] lines) =>
            new Programme { FromDay = from, ToDay = Forever, OffAir = true, Lines = lines };

        private static readonly Station[] s_Stations =
        {
            // ── TV ─────────────────────────────────────────────────────────────────────────────────────
            new Station
            {
                Name = "Channel 4", Title = "Newswatch 4",
                Caption = new Color(0.58f, 0.76f, 1f), Screen = new Color(0.70f, 0.80f, 1f),
                Schedule = new[]
                {
                    Hours(1, 3, 22f, 5f,
                        "Newswatch 4 is staying on the air through the night to bring you updates as they come in.",
                        "Police are asking residents to lock their doors and stay off the roads until morning.",
                        "*a weather map of the county, the same one as an hour ago*",
                        "We'll have more as soon as we have it. Stay with us."),
                    Days(1, 1,
                        "{greeting}, and welcome to Newswatch 4. Our top story: Fenwick General has closed its doors to visitors.",
                        "Staff describe a fast-moving fever. Patients grow confused, and in a few cases, violent.",
                        "Police have shut Route 9 at the quarry turn-off after what they call a disturbance involving several pedestrians.",
                        "The county health office says there's no cause for alarm. Wash your hands, and keep sick relatives at home.",
                        "The hospital's switchboard is jammed. Please call only for a genuine emergency.",
                        "In happier news, the Bobcats took their semi-final last night, nine runs to four.",
                        "Sheriff Daniels asks the public not to approach anyone who seems unwell or disoriented. Call it in.",
                        "Now the weather: warm and muggy, with storms likely after dark. Back to you, Dale.",
                        "The Pine Street farmers' market is cancelled this weekend, on the advice of the health office.",
"Ice on sidewalks can cause falls— wear sturdy boots and clear walkways around your home.",
"Fuel stations are running low; conserve gasoline and consider carpooling when possible.",
"Check on elderly neighbours and share heat sources if you can.",
"Avoid crowded gatherings; report anyone showing flu‑like symptoms to health officials.",
"Remember the legend of the abandoned lighthouse on Willow Creek— stay clear of its ruins after dark.",
                        "We're hearing the interstate is backed up eastbound. If you don't need to travel tonight, don't."),
                    Days(2, 3,
                        "This is a Newswatch 4 special report. A county-wide curfew is in effect from eight at night until six in the morning.",
                        "The Governor has called in the National Guard. Checkpoints are going up on every road out of the county.",
                        "Evacuation buses leave the high-school car park at nine a.m. One bag per person. Bring your medication.",
                        "?water:The water department warns supply may be interrupted. Fill clean containers now, and boil all tap water.",
                        "?nowater:Taps across the county have run dry. Do not drink from a stream or a pond without boiling it first.",
                        "?power:The power company says rolling blackouts are expected. Keep a flashlight and spare batteries by the bed.",
                        "If someone in your home has been bitten, do not drive them to the hospital. Keep them apart and call the hotline.",
                        "Keep off the interstate. The Guard needs the lanes clear for its convoys.",
                        "We're putting up a list of shelters. If your town isn't on it, stay where you are and lock up.",
                        "We've had no word from the Millbrook checkpoint since early this morning."),
                    Days(4, 5,
                        "We're on the air with a skeleton crew this morning. Please bear with us.",
                        "The evacuation centre at the fairground is no longer taking new arrivals. Do not go to the fairground.",
                        "Stay indoors. Keep your lights low and your voices lower. They come to noise.",
                        "Do not open your door to anyone. Even someone you know. Especially someone you know.",
                        "Ration your water. Ration your food. Help is coming, but it isn't coming soon.",
                        "Dale went out to his car about an hour ago. He hasn't come back."),
                    Days(6, 6,
                        "Is this still going out? ...Okay.",
                        "There's nobody left here but me and the camera.",
                        "If you're watching this, you made it this far. That counts for something.",
                        "Look after each other. Lock up. Keep quiet.",
                        "I'm going to leave the camera running. Goodnight, everybody.",
                        "*an empty news desk; a chair sits askew*"),
                    Dead(7,
                        "*colour bars and a thin, unbroken tone*",
                        "*static*"),
                },
            },
            new Station
            {
                Name = "Channel 7", Title = "Channel 7",
                Caption = new Color(1f, 0.84f, 0.46f), Screen = new Color(1f, 0.86f, 0.68f),
                Schedule = new[]
                {
                    Days(1, 1,
                        "*a game-show buzzer* ...and that brand-new dinette set goes to Linda from Oakdale!",
                        "*the studio audience roars with laughter*",
                        "Vivian, I can't marry you. Not while my brother is still in that coma.",
                        "*an organ sting; the soap fades out to an advert*",
                        "Krunchies breakfast squares: still crunchy at the bottom of the bowl!",
                        "*a man in a cardigan sells mattresses at prices he calls 'frankly unhinged'*",
                        "*a sitcom laugh track swells and dies away*",
                        "Stay tuned for 'Harbour Patrol', then the late movie: 'The Wayward Cowpoke'."),
                    Days(2, 3,
                        "We interrupt this programme for a message from the county emergency management office.",
                        "Regular programming has been suspended. Please stay tuned to your local news station.",
                        "*a still card: PLEASE STAND BY*",
                        "*a gentle instrumental loop plays under the card*"),
                    Dead(4,
                        "*snow hisses across the screen*",
                        "*static*"),
                },
            },
            new Station
            {
                Name = "Channel 12", Title = "Public Television",
                Caption = new Color(0.62f, 0.95f, 0.72f), Screen = new Color(0.74f, 1f, 0.86f),
                Schedule = new[]
                {
                    Days(1, 1,
                        "Now, the secret to a good pie crust is cold butter. Very cold butter.",
                        "We'll let that rest in the icebox while we make a start on the filling.",
                        "See how the dough comes away from the side of the bowl? That's how you know.",
                        "The pack hunts at dusk, when its prey is tired and slow.",
                        "Once the herd scatters, the wolves single out the stragglers.",
                        "Coming up: a history of the county's covered bridges, and the families who built them.",
                        "This programme was made possible by viewers like you. Thank you.",
                        "Heavy snow has piled on low‑slope roofs; clear the edges before they give way.",
                        "Shovelling the roof can be dangerous; consider professional help if the load looks unsafe.",
                        "Snow accumulation adds extra weight; check for loose tiles or missing shingles before the storm.",
                        "Keep walkways clear of snow and ice to prevent slips when you step outside.",
                        "If you notice a roof sag, evacuate the house and call for assistance immediately."),
                    Days(2, Forever,
                        "This is the Emergency Alert System. This is not a test.",
                        "A civil emergency has been declared for all residents of the county.",
                        "Remain indoors. Lock all doors and windows. Await further instructions.",
                        "Do not approach anyone who appears ill, confused or aggressive.",
                        "?water:Boil all tap water for at least one minute before drinking it.",
                        "?nowater:Collect rainwater in clean containers, and boil it before use.",
                        "*a long, flat tone; the message begins again*"),
                },
            },

            // ── Radio ──────────────────────────────────────────────────────────────────────────────────
            new Station
            {
                Name = "91.3 FM", Title = "Easy 91.3",
                Caption = new Color(0.95f, 0.70f, 0.86f), Screen = Color.white,
                Schedule = new[]
                {
Days(1, 2,
                        "You're listening to Easy 91.3, the soft sound of the valley, all night long.",
                        "*soft piano*",
                        "That was 'Porch Light Waltz' by the Delmonico Sisters. Here's one for the night shift.",
                        "*a fiddle tune drifts out*",
                        "A dedication from Barb to her Walt, working late at the plant: come home safe, honey.",
                        "*a slow saxophone number*",
                        "The request line's quiet tonight, folks. Hope you're all tucked up safe at home.",
                        "*a lonesome harmonica, then a train whistle on the record*",
                        "It's {weekday} on Easy 91.3, and the music doesn't stop.",
                        "*a crooner sings about a train that never came*",
                        "Stay alert, the night is growing uneasy.",
                        "?water:Remember to boil water before drinking, stay safe."
                    ),
                    Days(3, 4,
                        "*the same easy-listening tape, looping*",
                        "*a ballad cuts out mid-chorus, then starts again from the top*",
                        "*soft piano*",
                        "*a long silence between songs*",
                        "Curfew enforced, stay indoors after dark.",
                        "?nowater:Boil water before drinking, beware of contamination."),
                    Dead(5,
                        "*dead air*",
                        "*the faint hiss of an empty frequency*"),
                },
            },
            new Station
            {
                Name = "1040 AM", Title = "Talk 1040",
                Caption = new Color(1f, 0.68f, 0.40f), Screen = Color.white,
                Schedule = new[]
                {
                    Days(1, 1,
                        "It's {weekday}, and this is Talk 1040. The lines are open.",
                        "Talk 1040, you're on the air. Go ahead, caller.",
                        "Caller: 'My neighbour bit the mailman. Bit him! I am not making this up.'",
                        "Folks, let's keep our heads. The county says it's a flu. A nasty one, but a flu.",
                        "Caller: 'They sent the kids home from school at lunch. Nobody's saying why.'",
                        "Traffic: Route 9 is shut at the quarry, and the interstate's crawling eastbound.",
                        "After the break: Bobcats baseball and the farm report. Stay with us.",
                        "Caller: 'Why are there soldiers parked outside the Quik-Fill?'",
                        "Stay alert, the night is growing uneasy.",
                        "?nowater:Boil water before drinking tonight."),
                    Days(2, 3,
                        "Talk 1040 news. Curfew tonight from eight. Anyone outdoors after dark may be detained.",
                        "Evacuation route: north on Route 9 to the county line. The south bridge is closed.",
                        "?water:The water department says fill your bathtub now. Supply may not last the week.",
                        "?nowater:The taps are dry across most of the county. Boil whatever water you collect.",
                        "?power:Rolling blackouts tonight. Unplug your appliances and keep the fridge door shut.",
                        "?nopower:Most of the county is without power. If you can hear this, your batteries are good. Save them.",
                        "If you've got a generator, run it outdoors. Never inside the house.",
                        "Caller: 'There's smoke over by the grain elevator. Is anybody coming?'",
                        "We've lost touch with our reporter at the fairground."),
                    Days(4, 4,
                        "This is Talk 1040. I think we're the last ones left in the building.",
                        "The phones stopped ringing around midnight.",
                        "If you're hearing this: stay inside, stay quiet, and don't come looking for us.",
                        "We'll stay on as long as the generator holds out."),
                    Dead(5,
                        "*static*",
                        "*a carrier hum, then nothing*"),
                },
            },
            new Station
            {
                Name = "1610 AM", Title = "Highway Advisory",
                Caption = new Color(1f, 0.46f, 0.42f), Screen = Color.white,
                Schedule = new[]
                {
                    Days(1, 1,
                        "Highway advisory radio. Roadworks on Route 9 at the quarry turn-off. Expect delays.",
                        "Highway advisory radio. Drive safely, and buckle up.",
                        "*a recorded chime; the message repeats*",
                        "Stay safe on the road, drive carefully.",
                        "?nowater:If water is scarce, boil before use."),
                    Days(2, Forever,
                        "This is the Emergency Broadcast System. This is not a test.",
                        "A state of emergency is in effect. Remain in your homes.",
                        "Do not approach the sick or the injured. Do not attempt to restrain them.",
                        "?water:Boil all water for one minute before drinking.",
                        "?nowater:Municipal water is unavailable. Collect rainwater and boil it.",
                        "?nopower:Power is out across the region. Conserve batteries and fuel.",
                        "Curfew enforced, stay indoors after dark.",
                        "?water:Boil water before drinking, supplies may be limited.",
                        "Further instructions will follow on this frequency.",
                        "*three tones; the message begins again*"),
                },
            },
        };

        /// <summary>The TV's channels, in dial order.</summary>
        public static readonly BroadcastStation[] TvChannels =
            { BroadcastStation.TvNews, BroadcastStation.TvEntertainment, BroadcastStation.TvPublic };

        /// <summary>The radio's stations, in dial order.</summary>
        public static readonly BroadcastStation[] RadioStations =
            { BroadcastStation.RadioMusic, BroadcastStation.RadioTalk, BroadcastStation.RadioEmergency };

        private static Station Of(BroadcastStation s) => s_Stations[Mathf.Clamp((int)s, 0, s_Stations.Length - 1)];

        /// <summary>Where it is on the dial: "Channel 4", "91.3 FM".</summary>
        public static string NameOf(BroadcastStation s) => Of(s).Name;

        /// <summary>What it calls itself: "Newswatch 4", "Talk 1040".</summary>
        public static string TitleOf(BroadcastStation s) => Of(s).Title;

        /// <summary>The colour its captions are credited in.</summary>
        public static Color CaptionColour(BroadcastStation s) => Of(s).Caption;

        /// <summary>The cast a TV picture takes on this channel right now (a cold grey once it's off the air).</summary>
        public static Color ScreenTint(BroadcastStation s) => IsOnAir(s) ? Of(s).Screen : new Color(0.78f, 0.80f, 0.84f);

        /// <summary>True while the station still broadcasts (false: dead air, static, a test card).</summary>
        public static bool IsOnAir(BroadcastStation s)
        {
            Clock(out int day, out float hour);
            Programme p = Current(Of(s), day, hour, out _);
            return p != null && !p.OffAir;
        }

        /// <summary>The next caption from <paramref name="s"/>: walks the current programme from
        /// <paramref name="cursor"/> (a set keeps one per station it listens to; start it at 0) and moves the cursor on.
        /// Returns null when there's nothing to hear.</summary>
        public static string NextLine(BroadcastStation s, ref int cursor)
        {
            Clock(out int day, out float hour);
            Programme p = Current(Of(s), day, hour, out int index);
            if (p == null || p.Lines == null || p.Lines.Length == 0) return null;

            int id = index + 1;   // which programme the cursor is in: a new programme starts from its first line
            int n = p.Lines.Length;
            int at = cursor / 1000 == id ? cursor % 1000 % n : 0;
            for (int tries = 0; tries < n; tries++)
            {
                string raw = p.Lines[at];
                at = (at + 1) % n;
                if (!Applies(raw, out string text)) continue;
                cursor = id * 1000 + at;
                return Fill(text, hour);
            }
            cursor = id * 1000 + at;
            return null;
        }

        private static void Clock(out int day, out float hour)
        {
            TimeOfDay t = TimeOfDay.Instance;
            day = t != null ? t.Day : 1;
            hour = t != null ? t.Hour : CloneZConfig.StartHour;
        }

        // The first programme in the schedule that airs at this day and hour (night slots are listed first); past
        // the end of the schedule, its last programme runs on.
        private static Programme Current(Station st, int day, float hour, out int index)
        {
            Programme[] list = st.Schedule;
            for (int i = 0; i < list.Length; i++)
            {
                Programme p = list[i];
                if (day < p.FromDay || day > p.ToDay || !InHours(p, hour)) continue;
                index = i;
                return p;
            }
            index = list.Length - 1;
            return list.Length > 0 && day > list[index].FromDay ? list[index] : null;
        }

        private static bool InHours(Programme p, float hour)
        {
            if (Mathf.Approximately(p.FromHour, p.ToHour)) return true;
            return p.FromHour < p.ToHour
                ? hour >= p.FromHour && hour < p.ToHour
                : hour >= p.FromHour || hour < p.ToHour;
        }

        // Strips a line's condition, returning false when the condition doesn't hold right now.
        private static bool Applies(string raw, out string text)
        {
            text = raw;
            if (string.IsNullOrEmpty(raw) || raw[0] != '?') return !string.IsNullOrEmpty(raw);
            int colon = raw.IndexOf(':');
            if (colon < 0) return true;
            string tag = raw.Substring(1, colon - 1);
            text = raw.Substring(colon + 1);
            switch (tag)
            {
                case "water": return WaterMains.MainsOn;
                case "nowater": return !WaterMains.MainsOn;
                case "power": return PowerGrid.GridOn;
                case "nopower": return !PowerGrid.GridOn;
                default: return true;
            }
        }

        private static string Fill(string text, float hour)
        {
            if (text.IndexOf('{') < 0) return text;
            string greeting = hour < 12f ? "Good morning" : hour < 18f ? "Good afternoon" : "Good evening";
            string weekday = TimeOfDay.Instance != null ? TimeOfDay.Instance.Date.DayOfWeek.ToString() : "today";
            return text.Replace("{greeting}", greeting).Replace("{weekday}", weekday);
        }
    }
}
