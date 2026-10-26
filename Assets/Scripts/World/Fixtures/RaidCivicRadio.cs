using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A two-way set the survivor can listen in on — the National Guard post's olive field radio, the depot office's CB set.
    /// Switched on, it plays the traffic on whatever channel it's tuned to: a caption for each transmission the survivor can
    /// hear from where they are, every few seconds, the hiss of an open channel between — the Guard's command and checkpoint
    /// nets on the field radio, the truckers' channel and the emergency channel on the CB. Every few seconds it makes a noise
    /// that carries as far as its volume (low, medium or loud), so a set left blaring draws the dead. Its knobs need a hand
    /// on them; listening in doesn't (an open-ended job that steadies the nerves a little — voices that still sound as if
    /// somebody's in charge). Keying the handset to call for help is heard by the dead, and by nobody else. Battery powered.
    /// Saves whether it's on, its channel and its volume.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicRadio : Interactable
    {
        private const string TooFar = "Get closer to reach the knobs";
        private const int MaxVolume = 2;

        /// <summary>The traffic one channel carries, read in turn.</summary>
        internal sealed class Channel
        {
            public string Name;
            public Color Caption;
            public string[] Lines;
        }

        /// <summary>Which set it is: the Guard's field radio or a CB.</summary>
        internal enum Band { Guard, Citizens }

        private static readonly Channel[] GuardNets =
        {
            new Channel
            {
                Name = "Battalion net", Caption = new Color(0.62f, 0.86f, 0.56f),
                Lines = new[]
                {
                    "Ironwood, Ironwood, this is Juniper Base. Radio check, over.",
                    "Juniper Base, Ironwood. I read you three by five. You're breaking up, over.",
                    "Ironwood, Juniper. Sitrep follows. Power's on, the perimeter is not. No contact with Kestrel Two-One since twenty-two hundred, over.",
                    "Juniper, Ironwood. Convoy Sledge is held at the county line by the snow. Zero-six-hundred at the earliest. Hold what you've got.",
                    "*a long squelch, then a voice too close to the mike* ...say again, all stations, say again your last...",
                    "Ironwood, Juniper. Civilians at the gate asking for the clinic. The clinic isn't answering its phone. Advise, over.",
                    "All stations, Ironwood. Rules of engagement unchanged. Do not fire on civilians without positive identification.",
                    "Juniper, Ironwood. ...Define it how you have to. Ironwood out.",
                    "*static, and a generator droning behind a voice* ...the tent at the fairground is full. Turn them round.",
                    "Ironwood, Juniper Base. We're a skeleton crew. Request permission to fall back to the armory, over.",
                    "Juniper, Ironwood. Negative. Hold the post until relieved. Ironwood out.",
                    "*squelch*",
                    "Any station this net, any station, this is Juniper Base. Ironwood is not answering. Over.",
                    "*a carrier hum, and under it somebody breathing into a handset*",
"Juniper, Ironwood. Farmer down in six feet of snow, tractor stalled, need a tow, over.",
"Juniper, Ironwood. Town hall lost power, lights out, generators down, over.",
"Juniper, Ironwood. Request bandages, wounded hand, need stitches, over.",
"Juniper, Ironwood. Thin ice on the river, avoid crossing, over.",
"Juniper, Ironwood. School closed due to storm, no classes today, over.",
"Juniper, Ironwood. Keep warm, we'll get through this, over.",
                },
            },
            new Channel
            {
                Name = "Checkpoint net", Caption = new Color(0.94f, 0.80f, 0.46f),
                Lines = new[]
                {
                    "Kestrel Two-Two, Checkpoint Delta. You copy? Over.",
                    "Delta, Two-Two. Copy. The plows never came up the river road. We're walking it, over.",
                    "Two-Two, Delta. There's a pickup in the ditch at the bridge. The driver's... not right. He won't answer us.",
                    "Delta, Two-Two. Don't let him close. You hear me? Keep him on the far side of the wire.",
                    "*boots crunching snow, someone counting under their breath*",
                    "Delta, Kestrel Two-Two. Contact, north end of Main. A dozen, maybe more. They aren't stopping. Over.",
                    "Two-Two, Delta. Fall back to the depot and wait for the truck. Say your status.",
                    "...Delta, Two-Two. Two of us left. Moving to the depot now.",
                    "*squelch, a burst of static, the rattle of a rifle a long way off*",
                    "Checkpoint Delta, this is Mercy Six. We're full. We can't take any more from the bridge. Mercy Six out.",
                    "Delta to anybody on this net: the bridge is closed. Nobody crosses. Nobody.",
                    "*the open channel hisses; nobody keys up*",
                },
            },
        };

        private static readonly Channel[] CbChannels =
        {
            new Channel
            {
                Name = "Channel 19", Caption = new Color(0.96f, 0.72f, 0.44f),
                Lines = new[]
                {
                    "Breaker one-nine, anybody got their ears on out by the interstate?",
                    "Come on, breaker. Night Owl here, hauling dry goods out of Lexington. Got you wall to wall.",
                    "Night Owl, it's Cotton Picker. Guard's got the on-ramp shut tight at the forty marker. Turned me round with a rifle.",
                    "Ten-four, Cotton Picker. What're they saying about the county?",
                    "They ain't saying nothing, Owl. That's what's got me spooked.",
                    "*somebody whistling, then a long burp of static*",
                    "This is Granny Goose up on the hill road. My boy went down to the depot for snow chains two hours ago. Anybody seen a blue Ford?",
                    "Granny Goose, Night Owl. Ain't seen a soul on that road but the plow. You stay in tonight, hear?",
                    "Breaker one-nine. There's folks walking the median by the truck stop. In this snow. Something ain't right with 'em.",
                    "*a long squelch, and a man saying 'no, no, no' until it cuts out*",
                    "Anybody on nineteen? Anybody at all? ...Come back.",
                    "Tractor stuck six feet deep in the snow near the creek, need a tow, over.",
                    "Thin ice forming on the east creek, avoid crossing until cleared, over.",
                    "Makeshift clinic reporting a shortage of antibiotics, send supplies if possible, over.",
                    "Keep transmissions short, battery's low and we need to save power, over.",
                    "Farmer here, lost my border collie near the old barn, any help locating him, over.",
                },
            },
            new Channel
            {
                Name = "Channel 9", Caption = new Color(1f, 0.52f, 0.46f),
                Lines = new[]
                {
                    "Channel nine emergency. This is the county REACT base, monitoring. Go ahead with your emergency.",
                    "REACT base, I've got a woman here bit on the arm, running a fever. The ambulance won't come.",
                    "Copy. Keep her warm, keep her apart from everybody, keep your door shut. That's all I can tell you.",
                    "*static*",
                    "REACT base to all stations: the hospital is turning folks away. Do not drive in. Repeat, do not drive in.",
                    "*the base's carrier drops out halfway through a word*",
                },
            },
        };

		private static readonly string[] GuardCalls = new[] {
			"\u201CWe’re low on fuel, the generator sputters, over.\u201D A weak hum fades into static.",
			"\u201CMedical supplies depleted, need bandages and antibiotics, over.\u201D The breath is shaky.",
			"\u201CBridge collapsed after the river burst, anyone with a spare rope, over.\u201D A panicked voice cracks.",
			"\u201CAny station, any station, this is Juniper Base\u2026\u201D You let go of the key. Only the hiss answers.",
			"You give the post's call sign and ask for anyone at all. A voice comes back in pieces \u2014 \u201C...say again... cannot...\u201D \u2014 then nothing.",
			"You key up and ask for help. Somebody keys back, breathes, and lets go without a word.",
			"\u201CMy tractor's stuck, the snow's six feet deep, I'm freezing out here.\u201D You hear his ragged breath before the static devours the line.",
			"\u201CPlease, my little boy ran into the woods and I can't find him.\u201D Your heart tightens as a sobbing whisper fades into the hiss.",
			"\u201CGenerator #3 sputters and died, the lights are out across the block.\u201D A clank follows, then the channel goes silent.",
			"\u201CThe barn's catching fire, the flames are leaping toward the grain store.\u201D Flames crackle faintly before the line bursts.",
			"\u201CI’m on Highway 12, something's howling out there, I think I'm being followed.\u201D A low howl echoes, then the static swallows the transmission.",
			"\u201CMy ewe’s gone missing near the old barn, anyone seen her?\u201D You hear a weary farmer’s voice over the crackle.",
			"\u201CWatch the high‑tension line by the creek, it’s down and sparking. Stay clear.\u201D A sharp warning cuts through the static.",
			"\u201CMy 8‑year‑old is out in the woods, didn’t come back at dark. Please, someone find her.\u201D A mother’s panic tears the air.",
			"\u201CWe’re down to two cans of beans and a half‑full water barrel. Any food drops would save us.\u201D Hunger rattles the line.",
			"\u201COur only med‑kit ran out of antibiotics. Any doctor out there?\u201D The plea is thin, the breath shallow.",
		};
		public static string RandomGuardCall()
		{
			return GuardCalls[UnityEngine.Random.Range(0, GuardCalls.Length)];
		}

        private static readonly string[] CbCalls =
        {
            "\u201CBreaker one-nine, anybody out there?\u201D A long pause. Somebody keys up, says \u201Cstay inside, friend,\u201D and is gone.",
            "You ask for help on channel nine. The REACT base doesn't come back. Just the carrier, humming.",
            "You call and call. Somewhere a trucker laughs at something, a long way off, and the channel goes quiet.",
            "\u201CThis is Farmer J. I need a rope, my mule slipped into a snow‑drift, over.\u201D",
            "\u201CWatch out, downed power line near the creek, stay clear, over.\u201D",
            "\u201CNeed a spare can of gasoline for the school generator, anyone got one? Over.\u201D",
            "\u201CHey, have you seen my orange tabby cat near the old mill? Over.\u201D",
            "\u201CCounty road B is blocked by a fallen tree, need a chainsaw, over.\u201D"
        };

        private Transform _host;
        private Vector3 _point;
        private Renderer _dialLight;
        private string _name, _source;
        private Channel[] _channels;
        private string[] _calls;
        private int[] _cursors;
        private int _tuned, _volume = CloneZConfig.ReceiverDefaultVolume, _call;
        private string _lastLine;
        private bool _on;
        private float _noiseAt, _captionAt;
        private Action<InteractContext> _turnOn, _turnOff, _retune, _louder, _softer, _callIn, _checkChannel, _checkVolume;
        private Action<InteractContext> _recall;
        private Action<InteractContext, float, float> _listen;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override float Reach => CloneZConfig.CivicRadioReach;
        public override Transform OcclusionRoot => _host != null ? _host : transform;
        public override string SaveKey => KeyFor("civicradio", _point);

        private Channel Tuned => _channels[Mathf.Clamp(_tuned, 0, _channels.Length - 1)];

        /// <summary>The set's fixture on <paramref name="host"/> (the desk or table it stands on): its knobs at
        /// <paramref name="knobs"/> (world), its dial's glow <paramref name="dialLight"/>.</summary>
        internal static CivicRadio Create(Transform host, string name, Vector3 knobs, Renderer dialLight, Band band)
        {
            var go = new GameObject("Radio_" + name);
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(knobs, Quaternion.identity);
            var r = go.AddComponent<CivicRadio>();
            r._host = host;
            r._name = name;
            r._point = knobs;
            r._dialLight = dialLight;
            r._channels = band == Band.Guard ? GuardNets : CbChannels;
            r._calls = band == Band.Guard ? GuardCalls : CbCalls;
            r._source = band == Band.Guard ? "Field radio" : "CB radio";
            r._cursors = new int[r._channels.Length];
            r.ShowOn(false);
		// After creating the radio component, add a crumpled note with a random guard call.
		var noteObj = new GameObject("Note_" + name);
		noteObj.transform.SetParent(host, false);
		noteObj.transform.localPosition = new Vector3(-0.3f, 0.5f, 0f);
		var note = noteObj.AddComponent<CivicRadioNote>();
		note.SetRandomText();
		return r;
        }

        private void Awake()
        {
            _turnOn = TurnOn;
            _checkVolume = ctx => ctx.Notify("Current volume: " + (_volume <= 0 ? "low." : _volume >= MaxVolume ? "loud. It carries a long way." : "medium."));
            _turnOff = c => Switch(false);
            _retune = Retune;
            _louder = c => Turn(c, 1);
            _softer = c => Turn(c, -1);
            _callIn = CallIn;
            _listen = Listen;
            _checkChannel = ctx => ctx.Notify("Current channel: " + Tuned.Name);
            _recall = Recall;
        }

        private void Update()
        {
            if (!_on) return;
            float now = Time.time;
            if (now >= _noiseAt)
            {
                _noiseAt = now + CloneZConfig.ReceiverNoiseInterval;
                MakeNoise(CloneZConfig.ReceiverNoiseRadius[_volume]);
            }
            if (now >= _captionAt) Caption(now);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool near = LivingFixtures.FlatDistance(ctx.Position, _point) <= CloneZConfig.ReceiverKnobReach;
            if (!_on)
            {
                into.Add(new InteractAction("Turn on the radio", _turnOn).DisabledIf(!near, TooFar));
                return;
            }
            var listen = new InteractAction("Listen in").Timed(-1f, "Listening to the radio", HumanoidActivity.None);
            listen.Tick = _listen;
            into.Add(listen);
            into.Add(new InteractAction("Turn off the radio", _turnOff).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Change channel", _retune).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn it up", _louder).DisabledIf(_volume >= MaxVolume, "It won't go any louder").DisabledIf(!near, TooFar));
                into.Add(new InteractAction("Turn it down", _softer).DisabledIf(_volume <= 0, "It won't go any quieter").DisabledIf(!near, TooFar));
                into.Add(new InteractAction("Check channel", _checkChannel).DisabledIf(!near, TooFar));
                into.Add(new InteractAction("Check volume", _checkVolume).DisabledIf(!near, TooFar));
                into.Add(new InteractAction("Recall last transmission", _recall).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Key the handset and call for help", _callIn)
                .Timed(2.5f, "Calling in", HumanoidActivity.Use)
                .DisabledIf(!near, "Get closer to reach the handset"));
        }

        #region Knobs
        private void Recall(InteractContext ctx)
        {
            if (string.IsNullOrEmpty(_lastLine))
                ctx.Notify("No transmission has been heard yet.");
            else
                ctx.Notify("Last transmission: \"" + _lastLine + "\"");
        }

        private void TurnOn(InteractContext ctx)
        {
            Switch(true);
            ctx.Notify("The dial glows amber and the speaker hisses. " + Tuned.Name + ".");
        }

        private void Retune(InteractContext ctx)
        {
            _tuned = (_tuned + 1) % _channels.Length;
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption * 0.5f;
            ctx.Notify("You click the selector round, through a squeal of static, to " + Tuned.Name + ".");
        }

        private void CallIn(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.CivicRadioCallNoise);
            ctx.Notify(_calls[_call++ % _calls.Length]);
        }

        private void Switch(bool on)
        {
            if (_on == on) return;
            _on = on;
            ShowOn(on);
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            if (on) Freshen();
        }

        private void Turn(InteractContext ctx, int step)
        {
            _volume = Mathf.Clamp(_volume + step, 0, MaxVolume);
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            _noiseAt = Time.time;
            ctx.Notify("Volume: " + (_volume <= 0 ? "low." : _volume >= MaxVolume ? "loud. It carries a long way." : "medium."));
        }
        #endregion

        private void ShowOn(bool on)
        {
            if (_dialLight != null && _dialLight.enabled != on) _dialLight.enabled = on;
        }

        #region Playing
        private void Listen(InteractContext ctx, float dt, float elapsed)
        {
            if (!_on)
            {
                if (ctx.Interactor != null) ctx.Interactor.StopJob();
                return;
            }
            if (ctx.Stats != null && dt > 0f) ctx.Stats.Panic = Mathf.Max(0f, ctx.Stats.Panic - CloneZConfig.CivicRadioListenCalm * dt);
        }

        // The next transmission is due: a caption, if the survivor can hear it from where they are.
        private void Caption(float now)
        {
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null || !InEarshot(pi))
            {
                _captionAt = now + 1f;
                return;
            }
            Channel ch = Tuned;
            int k = Mathf.Clamp(_tuned, 0, _cursors.Length - 1);
            string line = ch.Lines[_cursors[k] % ch.Lines.Length];
                _lastLine = line; // remember the most recent caption
            _cursors[k] = (_cursors[k] + 1) % ch.Lines.Length;
            Subtitles.Show(_source + " \u2014 " + ch.Name, line, ch.Caption, CloneZConfig.ReceiverCaptionSeconds);
            _captionAt = now + UnityEngine.Random.Range(CloneZConfig.ReceiverCaptionMin, CloneZConfig.ReceiverCaptionMax + 2f);
        }

        private bool InEarshot(PlayerInteractor pi)
        {
            InteractContext ctx = pi.Context;
            if (ctx != null && ctx.Stats != null && ctx.Stats.IsDead) return false;
            if (ctx != null && ctx.Sleep != null && ctx.Sleep.IsSleeping) return false;
            float range = CloneZConfig.CivicRadioCaptionRange * CloneZConfig.ReceiverHearingScale[_volume];
            return LivingFixtures.FlatDistance(pi.transform.position, _point) <= range;
        }

        private void Freshen()
        {
            _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption;
            _noiseAt = Time.time + 0.5f;
        }
        #endregion

        #region Persistence
        public override void CaptureState(FixtureSave save)
        {
            save.State = _on ? 1 : 0;
            save.A = _tuned;
            save.B = _volume;
        }

        public override void RestoreState(FixtureSave save)
        {
            _tuned = Mathf.Clamp(Mathf.RoundToInt(save.A), 0, _channels.Length - 1);
            _volume = Mathf.Clamp(Mathf.RoundToInt(save.B), 0, MaxVolume);
            _on = (save.State & 1) != 0;
            ShowOn(_on);
            if (_on) Freshen();
        }
        #endregion
    }
}
