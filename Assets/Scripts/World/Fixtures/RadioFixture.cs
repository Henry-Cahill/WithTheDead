using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The kitchen radio on the worktop (see <see cref="LivingReceiver"/>): battery powered, so it plays on long after the
    /// grid has failed. Three stations round the dial — easy listening that ends in dead air, a talk station that goes
    /// off the air a few days in, and the highway advisory frequency that turns into the automated emergency broadcast —
    /// three volume steps, and a warm glow behind the dial while it's on. "Listen to the radio" settles the nerves; the
    /// dining table is close enough to listen from.
    /// </summary>
    [DisallowMultipleComponent]
    public class RadioFixture : LivingReceiver
    {
        private Renderer _dialLight;

        public override string DisplayName => "Kitchen radio";
        public override float Reach => CloneZConfig.RadioReach;
        protected override BroadcastStation[] Dial => Broadcasts.RadioStations;
        protected override string SaveKind => "radio";
        protected override string SourceName => "Radio";
        protected override float CaptionRange => CloneZConfig.RadioCaptionRange;
        protected override float CalmPerSecond => CloneZConfig.ListenRadioPanicEasePerSec;

        /// <summary>Builds the radio's fixture on <paramref name="host"/> (the radio's furniture root): <paramref name="knobs"/>
        /// is where the survivor reaches (world), <paramref name="dialLight"/> the dial's glow.</summary>
        internal static RadioFixture Create(Transform host, Vector3 knobs, Vector3 facing, Renderer dialLight)
        {
            var go = new GameObject("Fixture_Radio");
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(knobs, Quaternion.LookRotation(facing, Vector3.up));
            var radio = go.AddComponent<RadioFixture>();
            radio._dialLight = dialLight;
            radio.Init(host, knobs, HomeFurnitureKind.Counter);
            return radio;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool near = KnobsInReach(ctx);
            if (!IsOn)
            {
                into.Add(new InteractAction("Turn on the radio", TurnOn).DisabledIf(!near, TooFar));
                return;
            }
            into.Add(PassTime("Listen to the radio", "Listening to the radio", "The radio is off"));
            into.Add(new InteractAction("Turn off the radio", c => Switch(false)).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Check battery", c => c.Notify("The battery still hums – enough juice for a few more hours.")).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Tune to the previous station", RetunePrev).DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn it up", c => Louder(c, 1))
                .DisabledIf(AtLoudest, "It won't go any louder")
                .DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Turn it down", c => Louder(c, -1))
                .DisabledIf(AtQuietest, "It's down as low as it goes")
                .DisabledIf(!near, TooFar));
            into.Add(new InteractAction("Check station", c => c.Notify("Currently tuned to " + Describe(Station) + (Broadcasts.IsOnAir(Station) ? "." : " \u2014 just static.")))
                .DisabledIf(!near, TooFar));
        }

        private void TurnOn(InteractContext ctx)
        {
            Switch(true);
            ctx.Notify("The dial lights up. " + Describe(Station) + (Broadcasts.IsOnAir(Station) ? "." : " \u2014 just static."));
        }

        private void RetunePrev(InteractContext ctx)
        {
            BroadcastStation s = TunePrevious();
            ctx.Notify("You turn the dial back to " + Describe(s) + (Broadcasts.IsOnAir(s) ? "." : " \u2014 nothing there but static."));
        }

        private void Louder(InteractContext ctx, int step)
        {
            Turn(step);
            ctx.Notify("Volume: " + VolumeWord + (Volume >= 2 ? ". It carries a long way." : "."));
        }

        protected override void ShowOn(bool on)
        {
            if (_dialLight != null) _dialLight.enabled = on;
        }
    }
}
