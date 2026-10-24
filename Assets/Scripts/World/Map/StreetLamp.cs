using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A street lamp that still burns: the fixture at the foot of its pole that switches its light on and off, lets it
    /// flicker or steadies it, and switches every burning lamp of the block at once. The light is a <see cref="RoomLight"/>
    /// (child of the fixture), which culls itself far from the survivor and does its own flickering, so the lamps keep no
    /// Update of their own; the block is found through the one list of live lamps. The fixture carries the pole's
    /// collider, so the pole never blocks the reach for its own switch. Its state is saved under its place
    /// (<c>"streetlamp@x,z"</c>): bit 0 on, bit 1 flickering.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class StreetLamp : Interactable
    {
        private const float BlockRadius = 30f;   // how far "the block's lamps" reach
        private const float ClickNoise = 2f;
        private const int OnBit = 1, FlickerBit = 2;

        private static readonly List<StreetLamp> s_All = new List<StreetLamp>();

        private RoomLight _light;
        private string _key;
        private System.Action<InteractContext> _switch, _flicker, _blockOn, _blockOff;

        /// <summary>Puts a burning lamp's fixture at the foot of its pole (<paramref name="foot"/>, the pole turned
        /// <paramref name="yaw"/> degrees and <paramref name="pole"/> in size), with its light at <paramref name="light"/>
        /// (all world; counted in <paramref name="town"/>'s lights).</summary>
        internal static StreetLamp Attach(RaidTown town, Vector3 foot, float yaw, Vector3 pole, Vector3 light, Color color,
            float range, float intensity, bool flicker, bool shadows)
        {
            var go = new GameObject("StreetLamp");
            go.transform.SetParent(town.World, false);
            go.transform.SetPositionAndRotation(foot, Quaternion.Euler(0f, yaw, 0f));
            var box = go.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, pole.y * 0.5f, 0f);
            box.size = pole;
            town.Light(go.transform, light, color, range, intensity, flicker, shadows);
            return go.AddComponent<StreetLamp>();   // last: its Awake finds the light
        }

        private void Awake()
        {
            _light = GetComponentInChildren<RoomLight>(true);
            _key = KeyFor("streetlamp", transform.position);
            _switch = c => Switch(c, !IsOn);
            _flicker = c => SetFlicker(c, !_light.Flicker);
            _blockOn = c => SwitchBlock(c, true);
            _blockOff = c => SwitchBlock(c, false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_All.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_All.Remove(this);
        }

        private bool IsOn => _light != null && _light.On;

        public override string DisplayName => "Street lamp";
        public override Vector3 InteractPoint => transform.position + Vector3.up * 1.2f;
        public override bool IsAvailable => base.IsAvailable && _light != null;
        public override string SaveKey => _key;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool on = IsOn;
            into.Add(new InteractAction(on ? "Turn off street lamp" : "Turn on street lamp", _switch));
            if (on) into.Add(new InteractAction(_light.Flicker ? "Turn off flicker" : "Turn on flicker", _flicker));
            into.Add(on ? new InteractAction("Turn off block lamps", _blockOff) : new InteractAction("Turn on block lamps", _blockOn));
        }

        private void Switch(InteractContext ctx, bool on)
        {
            _light.On = on;
            MakeNoise(ClickNoise);
            ctx.Notify(on ? "The street lamp clicks on." : "The street lamp clicks off.");
        }

        private void SetFlicker(InteractContext ctx, bool flicker)
        {
            _light.Flicker = flicker;
            MakeNoise(ClickNoise);
            ctx.Notify(flicker ? "The lamp begins to flicker." : "The lamp steadies.");
        }

        private void SwitchBlock(InteractContext ctx, bool on)
        {
            float reach = BlockRadius * BlockRadius;
            for (int i = 0; i < s_All.Count; i++)
            {
                StreetLamp lamp = s_All[i];
                if (lamp._light != null && (lamp.transform.position - transform.position).sqrMagnitude <= reach) lamp._light.On = on;
            }
            MakeNoise(ClickNoise);
            ctx.Notify(on ? "All nearby street lamps click on." : "All nearby street lamps click off.");
        }

        public override void CaptureState(FixtureSave save)
        {
            save.State = (IsOn ? OnBit : 0) | (_light != null && _light.Flicker ? FlickerBit : 0);
        }

        public override void RestoreState(FixtureSave save)
        {
            if (_light == null) return;
            _light.On = (save.State & OnBit) != 0;
            _light.Flicker = (save.State & FlickerBit) != 0;
        }
    }
}
