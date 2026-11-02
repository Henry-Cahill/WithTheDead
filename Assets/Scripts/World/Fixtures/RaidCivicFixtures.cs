using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A workplace fitting with two states, flipped with [E]: a bench vice wound open or shut, a ward's privacy screen
    /// folded away or pulled round, a desk's computer switched on or off. Its moving parts (<see cref="FixturePart"/>s) swing
    /// or slide with it, and renderers can show for one state or the other (a lit screen, a dark one). A fitting that needs
    /// power won't come on without it; extra actions can follow the main one. Built and dressed by the raid town's kit
    /// (<see cref="RaidBuilding"/>). Saves which way it's set.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicToggle : Interactable
    {
        private string _name, _key, _onLabel, _offLabel, _onNote, _offNote;
        private Vector3 _point;
        private Transform _occlusion;
        private FixturePart[] _parts;
        private Renderer[] _whenOn, _whenOff;
        private float _noise = CloneZConfig.FixtureNoiseSmall;
        private bool _needsPower, _on, _locked;
        private Action<InteractContext, List<InteractAction>> _extra;
        private Action<InteractContext> _flip;

        /// <summary>True while it's on (open, folded, switched on).</summary>
        public bool IsOn => _on;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _occlusion != null ? _occlusion : transform;
        public override string SaveKey => _key;

        private void Awake() => _flip = c => Set(c, !_on);

        /// <summary>A two-state fitting on <paramref name="host"/>, reached at <paramref name="point"/> (world), worked with
        /// <paramref name="onLabel"/> / <paramref name="offLabel"/>. <paramref name="occlusion"/> is the furniture it's part of.</summary>
        internal static CivicToggle Create(Transform host, string name, string key, Vector3 point, Transform occlusion, string onLabel, string offLabel)
        {
            var go = new GameObject("Toggle_" + name);
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var t = go.AddComponent<CivicToggle>();
            t._name = name;
            t._key = key;
            t._point = point;
            t._occlusion = occlusion;
            t._onLabel = onLabel;
            t._offLabel = offLabel;
            // Default extra actions; Extra() will merge rather than replace.
            t._extra = (c, list) => {
                list.Add(new InteractAction("Inspect", t.Inspect));
                list.Add(new InteractAction("Power status", t.PowerInfo));
                list.Add(new InteractAction(t._locked ? "Unlock" : "Lock", t.ToggleLock));
            };
            return t;
        }

        /// <summary>The parts that open as it turns on (and close as it turns off).</summary>
        internal CivicToggle Moving(params FixturePart[] parts)
        {
            _parts = parts;
            Apply(true);
            return this;
        }

        /// <summary>Renderers shown only while it's on, and only while it's off.</summary>
        internal CivicToggle Showing(Renderer whenOn, Renderer whenOff)
        {
            _whenOn = whenOn != null ? new[] { whenOn } : null;
            _whenOff = whenOff != null ? new[] { whenOff } : null;
            Apply(true);
            return this;
        }

        /// <summary>What the survivor is told as it turns on, and off (null: nothing).</summary>
        internal CivicToggle Notes(string on, string off)
        {
            _onNote = on;
            _offNote = off;
            return this;
        }

        /// <summary>How far the dead hear it being worked.</summary>
        internal CivicToggle Noise(float radius)
        {
            _noise = radius;
            return this;
        }

        /// <summary>It won't come on without power (<see cref="PowerGrid.HousePowered"/>).</summary>
        internal CivicToggle NeedsPower()
        {
            _needsPower = true;
            return this;
        }

        /// <summary>Actions listed after the main one.</summary>
        internal CivicToggle Extra(Action<InteractContext, List<InteractAction>> extra)
        {
            var previous = _extra;
            _extra = (c, list) => {
                previous?.Invoke(c, list);
                extra?.Invoke(c, list);
            };
            return this;
        }

        /// <summary>Sets how it starts, without a sound.</summary>
        internal CivicToggle StartingOn(bool on)
        {
            _on = on;
            Apply(true);
            return this;
        }

        private void Inspect(InteractContext ctx) => ctx.Notify("The " + _name + " is currently " + (_on ? "on" : "off") + (_locked ? " and locked." : "."));
        private void PowerInfo(InteractContext ctx) => ctx.Notify(PowerGrid.PoweredAt(this) ? "The building has power." : "The building has no power.");

        private void ToggleLock(InteractContext ctx)
        {
            _locked = !_locked;
            ctx.Notify(_locked ? "You lock the device." : "You unlock the device.");
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            bool dead = _needsPower && !_on && !PowerGrid.PoweredAt(this);
            bool locked = _locked;
            into.Add(new InteractAction(_on ? _offLabel : _onLabel, _flip).DisabledIf(dead || locked, locked ? "The device is locked" : PowerGrid.NoPowerReasonAt(this) ?? "There's no power"));
            _extra?.Invoke(ctx, into);
        }

        /// <summary>Turns it on or off, with its sound and its note.</summary>
        public void Set(InteractContext ctx, bool on)
        {
            if (_on == on) return;
            _on = on;
            Apply(false);
            MakeNoise(_noise);
            string note = on ? _onNote : _offNote;
            if (note != null && ctx != null) ctx.Notify(note);
        }

        private void Apply(bool instant)
        {
            if (_parts != null)
                for (int i = 0; i < _parts.Length; i++)
                    if (_parts[i] != null) _parts[i].SetOpen(_on, instant);
            Show(_whenOn, _on);
            Show(_whenOff, !_on);
        }

        private static void Show(Renderer[] rs, bool on)
        {
            if (rs == null) return;
            for (int i = 0; i < rs.Length; i++)
                if (rs[i] != null && rs[i].enabled != on) rs[i].enabled = on;
        }

        public override void CaptureState(FixtureSave save) => save.State = (_on ? 1 : 0) | (_locked ? 2 : 0);

        public override void RestoreState(FixtureSave save)
        {
            _on = (save.State & 1) != 0;
            _locked = (save.State & 2) != 0;
            Apply(true);
        }
    }

    /// <summary>
    /// Something in a workplace to look over or handle rather than use — a drip stand, a stack of tyres, a jerry can: [E]
    /// does its one thing (a look, a heft, a check) and tells the survivor about it, a different line each time round. A
    /// timed look holds the survivor there a moment first; a reassuring one takes the edge off their nerves. Nothing to save.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicLook : Interactable
    {
        private string _name, _label, _busy;
        private Vector3 _point;
        private Transform _occlusion;
        private string[] _lines;
        private int _next;
        private float _seconds, _calm;
        private HumanoidActivity _activity = HumanoidActivity.Use;
        private Action<InteractContext> _look;
        private Action<InteractContext, List<InteractAction>> _extra;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override Transform OcclusionRoot => _occlusion != null ? _occlusion : transform;

        /// <summary>A thing on <paramref name="host"/>, reached at <paramref name="point"/> (world), whose one action is
        /// <paramref name="label"/>; each use tells the next of <paramref name="lines"/>, starting from <paramref name="first"/>.</summary>
        internal static CivicLook Create(Transform host, string name, Vector3 point, Transform occlusion, string label, string[] lines, int first = 0)
        {
            var go = new GameObject("Look_" + name);
            go.transform.SetParent(host, false);
            go.transform.SetPositionAndRotation(point, Quaternion.identity);
            var l = go.AddComponent<CivicLook>();
            l._name = name;
            l._point = point;
            l._occlusion = occlusion;
            l._label = label;
            l._lines = lines != null && lines.Length > 0 ? lines : new[] { "Nothing out of the ordinary." };
            l._next = Mathf.Abs(first) % l._lines.Length;
            return l;
        }

        /// <summary>Makes the look take <paramref name="seconds"/>, with a progress bar reading <paramref name="busy"/>.</summary>
        internal CivicLook Taking(float seconds, string busy, HumanoidActivity activity = HumanoidActivity.Use)
        {
            _seconds = seconds;
            _busy = busy;
            _activity = activity;
            return this;
        }

        /// <summary>Each look eases the survivor's panic by <paramref name="panic"/>.</summary>
        internal CivicLook Calming(float panic)
        {
            _calm = panic;
            return this;
        }

        /// <summary>Actions listed after the look.</summary>
        internal CivicLook Extra(Action<InteractContext, List<InteractAction>> extra)
        {
            _extra = extra;
            return this;
        }

        private void Awake() => _look = Look;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            var a = new InteractAction(_label, _look);
            if (_seconds > 0f) a.Timed(_seconds, _busy ?? _label, _activity);
            into.Add(a);
            _extra?.Invoke(ctx, into);
        }

        private void Look(InteractContext ctx)
        {
            ctx.Notify(_lines[_next]);
            _next = (_next + 1) % _lines.Length;
            LivingFixtures.Calm(ctx, _calm);
        }
    }

    /// <summary>
    /// A bed in one of the town's workplaces — an army bunk, a hospital bed, the clinic's examination couch: somewhere to
    /// lie down and rest awake, as on the home's bed (an open-ended job in which endurance comes back three times as fast and
    /// fatigue and nerves ease while the clock runs on; moving or [E] gets up — see <see cref="PlayerSeat.LieDown"/>), and,
    /// unless there's a bunk above it, to sit on the edge of. A bunk bed has two berths, the top one climbed into. No
    /// sleeping the night away here: a raid doesn't wait. Nothing about it is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class CivicRest : Interactable
    {
        private Transform _body;
        private string _name, _sitLabel, _standLabel;
        private float _yaw, _halfLength, _halfWidth, _edgeY, _pointY, _comfort;
        private bool _canSit;
        private Transform[] _berths;
        private string[] _lieLabels, _busyLabels;
        private Action<InteractContext>[] _lieDown;
        private Action<InteractContext> _getUpFromBed, _sit, _stand;
        private Action<InteractContext, List<InteractAction>> _extra;

        // Set default extra action to check comfort if none provided
        private void Awake()
        {
            var previous = _extra;
            _extra = (c, list) =>
            {
                previous?.Invoke(c, list);
                list.Add(new InteractAction("Check comfort", ComfortInfo));
            };
        }

        // Notify the survivor about the bed's comfort level
        private void ComfortInfo(InteractContext ctx)
        {
            ctx.Notify($"The bed feels {(int)(_comfort * 100)}% comfortable.");
        }

        public override string DisplayName => _name;
        public override float Reach => CloneZConfig.BedReach;
        public override Transform OcclusionRoot => _body != null ? _body : transform;

        /// <summary>Down the middle of the bed at the mattress, level with the survivor: in reach from either side or the foot.</summary>
        public override Vector3 InteractPoint
        {
            get
            {
                Vector3 centre = transform.position;
                Quaternion q = Quaternion.Euler(0f, _yaw, 0f);
                float z = 0f, hz = Mathf.Max(0f, _halfLength - 0.2f);
                PlayerInteractor pi = PlayerInteractor.Current;
                if (pi != null) z = Mathf.Clamp((Quaternion.Inverse(q) * (pi.transform.position - centre)).z, -hz, hz);
                return centre + q * new Vector3(0f, _pointY, z);
            }
        }

        /// <summary>A bed on <paramref name="body"/>, its middle at <paramref name="centre"/> (world, on the floor), its head
        /// toward its own -Z turned <paramref name="yaw"/> degrees, <paramref name="length"/> × <paramref name="width"/> m. Each
        /// of <paramref name="mattressTops"/> (heights above the floor) is a berth, lain on with the matching label;
        /// <paramref name="edgeHeight"/> &gt; 0 lets the survivor sit on its edge.</summary>
        internal static CivicRest Create(Transform body, string name, string noun, Vector3 centre, float yaw, float length, float width,
            float[] mattressTops, string[] lieLabels, float edgeHeight, float comfort)
        {
            if (mattressTops == null || mattressTops.Length == 0)
            {
                Debug.LogWarning($"CivicRest.Create called with empty mattressTops for {name}; using default height 0.5m.");
                mattressTops = new[] { 0.5f };
            }
            var go = new GameObject("Rest_" + name);
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(centre, Quaternion.Euler(0f, yaw, 0f));
            var r = go.AddComponent<CivicRest>();
            r._body = body;
            r._name = name;
            r._sitLabel = "Sit on the edge of the " + noun;
            r._standLabel = "Get up off the " + noun;
            r._yaw = yaw;
            r._halfLength = length * 0.5f;
            r._halfWidth = width * 0.5f;
            r._edgeY = edgeHeight;
            r._canSit = edgeHeight > 0f;
            r._comfort = comfort;
            int n = mattressTops.Length;
            r._pointY = mattressTops[0] + 0.05f;
            r._berths = new Transform[n];
            r._lieLabels = lieLabels;
            r._busyLabels = new string[n];
            r._lieDown = new Action<InteractContext>[n];
            for (int i = 0; i < n; i++)
            {
                // The lying pose rests on a bed root 0.51 m under the mattress (the home's bed): a pose root per berth.
                var berth = new GameObject("Berth_" + i).transform;
                berth.SetParent(go.transform, false);
                berth.SetPositionAndRotation(centre + Vector3.up * (mattressTops[i] - 0.51f), Quaternion.Euler(0f, yaw, 0f));
                r._berths[i] = berth;
                r._busyLabels[i] = i == 0 ? "Resting" : "Resting up top";
                int k = i;
                r._lieDown[i] = c => r.LieDown(c, k);
            }
            r._getUpFromBed = c => { if (c.Seat != null) c.Seat.GetUp(); };
            r._sit = r.SitOnEdge;
            r._stand = c => { if (c.Seat != null) c.Seat.Stand(); };
            return r;
        }

        /// <summary>Actions listed after lying down and sitting (reading the chart, looking under the bunk).</summary>
        internal CivicRest Extra(Action<InteractContext, List<InteractAction>> extra)
        {
            _extra = extra;
            return this;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerSeat seat = ctx.Seat;
            for (int i = 0; i < _berths.Length; i++)
            {
                var a = new InteractAction(_lieLabels[i]).Timed(-1f, _busyLabels[i], HumanoidActivity.None).DisabledIf(seat == null, "You can't lie down now");
                a.Begin = _lieDown[i];
                a.Stopped = _getUpFromBed;
                into.Add(a);
            }
            if (_canSit)
            {
                if (seat != null && seat.IsSittingOn(this)) into.Add(new InteractAction(_standLabel, _stand));
                else into.Add(new InteractAction(_sitLabel, _sit).DisabledIf(seat == null, "You can't sit down now"));
            }
            _extra?.Invoke(ctx, into);
        }

        private void LieDown(InteractContext ctx, int berth)
        {
            string reason = null;
            if (ctx.Seat == null || !ctx.Seat.LieDown(_berths[berth], out reason))
            {
                ctx.Notify(reason ?? "You can't lie down now");
                if (ctx.Interactor != null) ctx.Interactor.StopJob();
            }
        }

        // On whichever long side the survivor stands, a little in from the edge, facing out into the room.
        private void SitOnEdge(InteractContext ctx)
        {
            if (ctx.Seat == null) return;
            Quaternion q = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 me = Quaternion.Inverse(q) * (ctx.Position - transform.position);
            float side = me.x < 0f ? -1f : 1f;
            var local = new Vector3(side * (_halfWidth - CloneZConfig.BedEdgeInset), _edgeY,
                Mathf.Clamp(me.z, -_halfLength + 0.65f, _halfLength - 0.3f));
            if (!ctx.Seat.TrySit(this, transform.position + q * local, _yaw + (side < 0f ? 270f : 90f), _comfort, out string reason))
                ctx.Notify(reason);
        }
    }
}
