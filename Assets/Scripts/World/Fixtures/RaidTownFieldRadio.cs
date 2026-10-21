using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A National Guard field radio left on the quarantine tent's field desk (and one more on the ledge of the checkpoint's
    /// guard booth), still on the military nets. Switched on, it plays whichever net it's tuned to the way the home's
    /// radio plays its stations (<see cref="LivingReceiver"/>): a caption for each transmission the survivor can hear from
    /// where they stand, and every few seconds a squawk that carries as far as its volume — so a set left blaring draws
    /// the dead. Three nets round its dial: the Guard company's own, the medical net to the tent, and an automated
    /// battalion relay that reads out which landing zones are taking evacuees tonight (the raid's open exits). All of it is
    /// original chatter; each net walks its traffic in order and starts again. Nothing is saved (raids never are).
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidTownFieldRadio : Interactable
    {
        private const string TooFar = "Get closer to reach the knobs";
        private const int MaxVolume = 2;
        private const int RelayNet = 2;

        private sealed class Net
        {
            public string Name, Title;
            public Color Caption;
            public string[] Lines;
        }

        private static readonly Net[] Nets =
        {
            new Net
            {
                Name = "Channel 1", Title = "Company net", Caption = new Color(0.62f, 0.90f, 0.55f),
                Lines = new[]
                {
                    "Juniper Base, this is Lantern at the tent site. Radio check, over.",
                    "Lantern, Juniper. Read you five by five. Say your status, over.",
                    "Juniper, Lantern. Decon line's holding. The cloud is drifting east toward Depot Road \u2014 masks on south of the creek.",
                    "Juniper, Lantern... we have walkers at the tape again. Five. Six. They don't stop for the tape, Juniper.",
                    "Lantern, Juniper. No birds tonight. I say again, no medevac tonight. Hold what you've got. Juniper out.",
                    "Checkpoint Delta, Checkpoint Delta, this is Lantern. Do you copy? ...Delta, come in, over.",
                    "*a long hiss, then somebody breathing hard into the handset*",
                    "Juniper, Lantern. We're falling back on the armory. Leaving the tent. Leaving everything. Lantern out.",
                    "Juniper Base, this is Lantern. Supplies are running low, blankets scarce, over.",
                    "Lantern, Juniper. Temperatures dropped below minus twelve, frostbite reports rising, over.",
                    "Juniper, Lantern. Morale is slipping, men complaining about the cold and empty stomachs, over.",
                    "Lantern, Juniper. Fuel for heaters is half, conserve what you can, over.",
                    "Juniper Base, this is Lantern. Snow drifts blocking the supply road, expect delays, over.",
                    "*the carrier clicks on and off, on and off, and nobody speaks*",
                },
            },
            new Net
            {
                Name = "Channel 2", Title = "Medical net", Caption = new Color(1f, 0.78f, 0.52f),
                Lines = new[]
                {
                    "Mercy Six, this is Tent One. Two more in the cots with the fever and the shakes. Requesting antibiotics, over.",
                    "Tent One, Mercy Six. Negative. The clinic's gone dark. Use what's in the kit and don't let them get up.",
                    "Mercy Six, Tent One. Patient in cot three passed at twenty-two ten. He... he sat up again at twenty-two forty.",
                    "Tent One, Mercy Six. Burn the linens. Double-glove. Keep your mask on, whatever happens. Over.",
                    "*somebody coughing, a long time, before the handset clicks off*",
                    "This is Tent One. Anyone listening: the spare filters are in the equipment locker. Take them. We won't need them.",
"Tent One, Mercy Six. Supplies of warm clothing are dwindling; only a few threadbare jackets remain, conserve heat.",
"Mercy Six, Tent One. The generator at the field clinic is broken, run on batteries only and limit power usage.",
"Tent One, Mercy Six. Child in cot two broke his arm, need a splint from the med kit ASAP.",
"Mercy Six, Tent One. The creek north has frozen solid, water collection is hazardous—use melt rigs.",
"Tent One, Mercy Six. Wind is picking up, we need extra firewood for the heaters, anyone with logs, over.",
                },
            },
            new Net
            {
                Name = "Channel 3", Title = "Battalion relay", Caption = new Color(0.66f, 0.80f, 1f),
                Lines = new[]
                {
                    "This is an automated relay. The county is under a federal quarantine order. Remain where you are.",
                    null,   // the landing zones open tonight, read out live
                    "Civilians at the cordon will be screened before transport. Have identification ready. Do not run.",
                    "*three falling tones; the relay begins again*",
                "Evacuation notice: snow‑blocked roads on Highway 7, expect delays. Seek shelter until cleared.",
                "All units, maintain firewatch; temperatures dropping below minus ten, frostbite risk high.",
                "Winter convoy routes updated: primary extraction point shifted to the north depot, proceed with caution.",
                "Remember to keep your respirators on in sub‑zero winds; exposure can cause severe hypothermia.",
                },
            },
        };

        private Transform _host;
        private Vector3 _point;
        private Renderer _dial;
        private string _name;
        private bool _on;
        private int _net, _volume = CloneZConfig.ReceiverDefaultVolume, _exitCursor;
        private readonly int[] _cursor = new int[Nets.Length];
        private float _noiseAt, _captionAt;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override float Reach => CloneZConfig.RaidFieldRadioReach;
        public override Transform OcclusionRoot => _host != null ? _host : transform;

        /// <summary>A field radio's fixture on <paramref name="host"/> (the desk or booth it stands on), its knobs at
        /// <paramref name="knobs"/> and its dial's glow <paramref name="dial"/>.</summary>
        internal static RaidTownFieldRadio Create(Transform host, string displayName, Vector3 knobs, Renderer dial, int seed)
        {
            var go = new GameObject("Fixture_FieldRadio");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(knobs, Quaternion.identity);
            var r = go.AddComponent<RaidTownFieldRadio>();
            r._host = host;
            r._name = displayName;
            r._point = knobs;
            r._dial = dial;
            r._net = (seed & 0x7fff) % Nets.Length;
            r.ShowOn(false);
            return r;
        }

        private void Update()
        {
            // This method runs only when the component is enabled, i.e., when the radio is on.
            float now = Time.time;
            if (now >= _noiseAt)
            {
                _noiseAt = now + CloneZConfig.RaidFieldRadioNoiseInterval;
                MakeNoise(CloneZConfig.RaidFieldRadioNoise[_volume]);
            }
            if (now >= _captionAt) Caption(now);
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool near = LivingFixtures.FlatDistance(ctx.Position, _point) <= CloneZConfig.RaidFieldRadioKnobReach;
            if (!_on)
            {
                into.Add(new InteractAction("Switch on the radio", SwitchOn).DisabledIf(!near, TooFar));
                return;
            }
            into.Add(new InteractAction("Switch off the radio", c => Switch(false)).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Change channel", Retune).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Previous channel", PrevChannel).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn it up", c => Louder(c, 1)).DisabledIf(_volume >= MaxVolume, "It won't go any louder").DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn it down", c => Louder(c, -1)).DisabledIf(_volume <= 0, "It's down as low as it goes").DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Check radio status", CheckStatus).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Key the handset", KeyHandset).DisabledIf(!near, TooFar));
        }

        private void SwitchOn(InteractContext ctx)
        {
            Switch(true);
            Net n = Nets[_net];
            ctx.Notify("The set hums and its dial lights up amber. " + n.Name + " \u2014 " + n.Title + ".");
        }

        private void CheckStatus(InteractContext ctx)
        {
            Net n = Nets[_net];
            string vol = _volume <= 0 ? "low" : _volume >= MaxVolume ? "loud" : "medium";
            ctx.Notify($"Radio is on {n.Name} — {n.Title}. Volume: {vol}.");
        }

        private void Switch(bool on)
        {
            if (_on == on) return;
            _on = on;
            this.enabled = on;
            ShowOn(on);
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            if (on)
            {
                _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption;
                _noiseAt = Time.time + 0.5f;
            }
        }

        private void Retune(InteractContext ctx)
        {
            _net = (_net + 1) % Nets.Length;
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption * 0.5f;
            Net n = Nets[_net];
            ctx.Notify("You click the channel selector round to " + n.Name + " \u2014 " + n.Title + ".");
        }

        private void PrevChannel(InteractContext ctx)
        {
            _net = (_net + Nets.Length - 1) % Nets.Length;
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            _captionAt = Time.time + CloneZConfig.ReceiverFirstCaption * 0.5f;
            Net n = Nets[_net];
            ctx.Notify("You click the channel selector round to " + n.Name + " \u2014 " + n.Title + ".");
        }


        private void Louder(InteractContext ctx, int step)
        {
            _volume = Mathf.Clamp(_volume + step, 0, MaxVolume);
            MakeNoise(CloneZConfig.ReceiverKnobNoise);
            _noiseAt = Time.time;
            ctx.Notify("Volume: " + (_volume <= 0 ? "low." : _volume >= MaxVolume ? "loud. It carries a long way." : "medium."));
        }

        private void KeyHandset(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.FixtureNoiseSmall);
            ctx.Notify("You key the handset. \u201CAnyone on this net? Anyone?\u201D You let go. Only the carrier hiss answers.");
        }

        private void ShowOn(bool on)
        {
            if (_dial != null) _dial.enabled = on;
        }

        // A transmission is due: the next line of the net, if the survivor can hear it from where they are.
        private void Caption(float now)
        {
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null || !InEarshot(pi))
            {
                _captionAt = now + 1f;
                return;
            }
            Net n = Nets[_net];
            string line = null;
            for (int tries = 0; tries < n.Lines.Length && line == null; tries++)
            {
                int i = _cursor[_net] % n.Lines.Length;
                _cursor[_net] = i + 1;
                line = n.Lines[i] ?? (_net == RelayNet ? ExitNotice() : null);
            }
            Subtitles.Show("Field radio \u2014 " + n.Name, line ?? "*static*", n.Caption, CloneZConfig.ReceiverCaptionSeconds);
            _captionAt = now + Random.Range(CloneZConfig.RaidFieldRadioCaptionMin, CloneZConfig.RaidFieldRadioCaptionMax);
        }

        // The relay's live notice: one of tonight's landing zones (the raid's exits), open or closed, in turn.
        private string ExitNotice()
        {
            ExtractionManager em = ExtractionManager.Instance;
            IReadOnlyList<RaidExit> exits = em != null ? em.Exits : null;
            if (exits == null || exits.Count == 0) return null;
            RaidExit e = exits[_exitCursor++ % exits.Count];
            if (e == null) return null;
            return e.Open
                ? "Evacuation notice: aircraft are setting down at the " + e.Name + (string.IsNullOrEmpty(e.Hint) ? "" : " \u2014 " + e.Hint)
                    + ". Make your way there with caution."
                : "Evacuation notice: the " + e.Name + " is closed. I say again, the " + e.Name + " is closed. Do not approach.";
        }

        private bool InEarshot(PlayerInteractor pi)
        {
            InteractContext ctx = pi.Context;
            if (ctx != null && ctx.Stats != null && ctx.Stats.IsDead) return false;
            if (ctx != null && ctx.Sleep != null && ctx.Sleep.IsSleeping) return false;
            float range = CloneZConfig.RaidFieldRadioCaptionRange * CloneZConfig.ReceiverHearingScale[_volume];
            return LivingFixtures.FlatDistance(pi.transform.position, _point) <= range;
        }
    }
}
