using System;
using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A window of the starter home, glazed and hung by <c>StarterHome.Windows.cs</c>: a fixed upper sash over a lower
    /// one that slides up, a sash lock on the meeting rails, and curtains on the room side — or, in the kitchen, a roller
    /// blind under the valance. From inside the survivor can throw the sash up or pull it down, latch it, draw the
    /// curtains (lower the blind), peer out to count the dead, and climb out once it's open; from outside an unlatched
    /// window opens, and an open one can be climbed in through. Drawn curtains or a lowered blind hide whoever is
    /// inside: a collider across the opening blocks every line of sight, the dead's included (glass alone doesn't).
    /// Saved: bit 1 open, bit 2 curtains drawn / blind down, bit 4 latched.
    ///
    /// A window needn't sit in a <see cref="BuildPiece"/>: the raid town's (<see cref="RaidWindow"/>) are fitted in a
    /// raid building's wall segments with no piece at all — keyed by where they stand, hiding with their segment.
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeWindow : Interactable
    {
        /// <summary>How a window is fitted, handed over by its builder.</summary>
        internal struct Setup
        {
            /// <summary>"Kitchen window" — the name over it.</summary>
            public string Name;
            /// <summary>The lower sash, built shut, sliding up to open.</summary>
            public FixturePart Sash;
            /// <summary>The curtains drawn back and drawn across (the kitchen: the blind rolled up and pulled down).</summary>
            public Renderer CurtainsOpen, CurtainsDrawn;
            /// <summary>The sash lock's lever, turned onto its keeper and turned away (both ride with the sash).</summary>
            public Renderer LatchShut, LatchOpen;
            /// <summary>The opening (world, axis-aligned), blocked while the curtains are drawn.</summary>
            public Bounds Screen;
            /// <summary>The glazing's middle on the wall line (reached from either side).</summary>
            public Vector3 Point;
            /// <summary>The wall's normal toward its +1 side, and its run.</summary>
            public Vector3 Normal, Along;
            /// <summary>The outdoors side of <see cref="Normal"/> (-1/+1), or 0 with a room either side.</summary>
            public int OutSide;
            /// <summary>The kitchen's roller blind instead of curtains.</summary>
            public bool Blind;
            /// <summary>A fresh run finds the sash up, the curtains drawn, the sash lock off (by default: shut, open, latched).</summary>
            public bool Open, Drawn, Unlatched;
            /// <summary>The cutaway marker its parts hide with; by default the window piece's, else the nearest one above the
            /// window (a raid window: its wall segment's).</summary>
            public CutawayOccluder Occluder;
        }

        private string _name = "Window", _key;
        private BuildPiece _piece;
        private FixturePart _sash;
        private Renderer _curtainsOpen, _curtainsDrawn, _latchShut, _latchOpen;
        private BoxCollider _screen;
        private CutawayOccluder _occluder;
        private Vector3 _point, _normal = Vector3.forward, _along = Vector3.right;
        private int _outSide;
        private bool _blind;
        private bool _open, _drawn, _latched = true;

        // Whether the far side has room to land, worked out for the prompt and reused for a moment.
        private float _climbCheckedAt = -1f;
        private int _climbSide;
        private bool _climbClear;

        private Action<InteractContext> _openAct, _closeAct, _latchAct, _unlatchAct, _curtainAct, _lookAct, _climbAct, _checkAct, _knockAct;

        private static readonly Collider[] s_Hits = new Collider[16];
        private static readonly int BodyMask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);
        private static readonly int SightMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        // Where a climb may land: straight across from the window first, then a stride further in or either way
        // along the wall (round a table, a barrel, a worktop under the sill).
        private static readonly float[] LandDepths = { 0.75f, 1.05f, 1.4f, 1.8f };
        private static readonly float[] LandAlong = { 0f, -0.45f, 0.45f, -0.9f, 0.9f };
        private const float BodyRadius = 0.4f;
        private const float WallFace = 0.1f;   // half the house walls' thickness

        /// <summary>True while the lower sash is up.</summary>
        public bool IsOpen => _open;
        /// <summary>True while the curtains are drawn (the blind is down).</summary>
        public bool CurtainsDrawn => _drawn;
        /// <summary>True while the sash lock is turned (only ever while shut).</summary>
        public bool IsLatched => _latched;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override float Reach => CloneZConfig.WindowReach;
        public override bool IsAvailable => base.IsAvailable && (_piece == null || _piece.Health > 0f);
        public override string SaveKey => _key;

        /// <summary>The outdoors side of <see cref="Normal"/> (-1/+1), or 0 with a room either side.</summary>
        protected int OutSide => _outSide;
        /// <summary>The wall's normal toward its +1 side, and its run.</summary>
        protected Vector3 Normal => _normal;
        protected Vector3 Along => _along;
        /// <summary>The window's own actions' work, for a window that lists them its own way.</summary>
        protected Action<InteractContext> OpenAction => _openAct;
        protected Action<InteractContext> CloseAction => _closeAct;
        protected Action<InteractContext> LatchAction => _latchAct;
        protected Action<InteractContext> UnlatchAction => _unlatchAct;
        protected Action<InteractContext> CurtainAction => _curtainAct;
        protected Action<InteractContext> LookAction => _lookAct;

        protected virtual void Awake()
        {
            _openAct = c => SetOpen(true, true);
            _closeAct = c => SetOpen(false, true);
            _latchAct = c => SetLatched(true, c);
            _unlatchAct = c => SetLatched(false, c);
            _curtainAct = c => SetDrawn(!_drawn);
            _lookAct = Look;
            _checkAct = c => CheckWindow(c);
            _knockAct = c => { MakeNoise(CloneZConfig.WindowNoise); c.Notify("You rap a fist against the window."); };
        }

        /// <summary>Makes <paramref name="root"/> (already holding the sash and the curtains, under the window
        /// <paramref name="piece"/>) a working window: adds the drawn curtains' screen and sets the fresh-run state —
        /// shut, latched, curtains open.</summary>
        internal static HomeWindow Attach(GameObject root, BuildPiece piece, in Setup s) => Attach<HomeWindow>(root, piece, s);

        /// <summary>As <see cref="Attach(GameObject, BuildPiece, in Setup)"/>, for a window of a kind built on this one (a raid's
        /// <see cref="RaidWindow"/>), with the fresh-run state <paramref name="s"/> asks for. <paramref name="piece"/> may be
        /// null: a window fitted in a raid building's wall, keyed by where <paramref name="root"/> stands.</summary>
        internal static T Attach<T>(GameObject root, BuildPiece piece, in Setup s) where T : HomeWindow
        {
            var w = root.AddComponent<T>();
            w._piece = piece;
            w._occluder = s.Occluder;
            w._name = string.IsNullOrEmpty(s.Name) ? "Window" : s.Name;
            w._key = KeyFor("window", piece != null ? piece.transform.position : root.transform.position);
            w._sash = s.Sash;
            w._curtainsOpen = s.CurtainsOpen;
            w._curtainsDrawn = s.CurtainsDrawn;
            w._latchShut = s.LatchShut;
            w._latchOpen = s.LatchOpen;
            w._point = s.Point;
            w._normal = s.Normal.sqrMagnitude > 1e-6f ? s.Normal.normalized : Vector3.forward;
            w._along = s.Along.sqrMagnitude > 1e-6f ? s.Along.normalized : Vector3.Cross(Vector3.up, w._normal);
            w._outSide = s.OutSide;
            w._blind = s.Blind;

            // The screen sits in world axes (the window itself may be turned 90°).
            var screen = new GameObject("Screen");
            screen.transform.SetParent(root.transform, false);
            screen.transform.SetPositionAndRotation(s.Screen.center, Quaternion.identity);
            w._screen = screen.AddComponent<BoxCollider>();
            w._screen.size = s.Screen.size;

            w._open = s.Open;
            w._drawn = s.Drawn;
            w._latched = !s.Open && !s.Unlatched;
            w.Apply(true);
            return w;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            int side = SideOf(ctx.Position, _point, _normal);
            bool inside = _outSide == 0 || side != _outSide;

            // Outside an open window, climbing in is the point; everywhere else the sash comes first.
            if (!inside && _open) AddClimb(ctx, side, false, into);

            if (_open) into.Add(new InteractAction("Close the window", _closeAct));
            else into.Add(new InteractAction("Open the window", _openAct)
                .DisabledIf(_latched, inside ? "The window is latched" : "It's latched from the inside"));

            if (inside)
            {
                if (_latched) into.Add(new InteractAction("Unlatch the window", _unlatchAct));
                else into.Add(new InteractAction("Latch the window", _latchAct).DisabledIf(_open, "Close the window first"));

                string draw = _blind ? (_drawn ? "Raise the blind" : "Lower the blind") : (_drawn ? "Open the curtains" : "Close the curtains");
                into.Add(new InteractAction("Check window", _checkAct));
                into.Add(new InteractAction("Knock on the window", _knockAct));

                into.Add(new InteractAction(_outSide == 0 ? "Look through the window" : "Look outside", _lookAct)
                    .Timed(CloneZConfig.WindowLookSeconds, "Looking outside", HumanoidActivity.Use));
                AddClimb(ctx, side, true, into);
            }
            else if (!_open) AddClimb(ctx, side, false, into);
        }

        protected void AddClimb(InteractContext ctx, int side, bool inside, List<InteractAction> into)
        {
            var a = new InteractAction(ClimbLabel(inside), _climbAct)
                .Timed(CloneZConfig.WindowClimbSeconds, ClimbBusyLabel(inside), HumanoidActivity.Search);
            if (!CanClimb) a.DisabledIf(true, ClimbBlocked);
            else if (!ClimbClear(ctx, side)) a.DisabledIf(true, "Something's blocking the other side");
            into.Add(a);
        }

        /// <summary>True while there's a way through to climb (the sash is up).</summary>
        protected virtual bool CanClimb => _open;
        /// <summary>Why there's no way through.</summary>
        protected virtual string ClimbBlocked => "The window is shut";
        /// <summary>"Climb out of the window", from inside (or "Climb in…", from outside).</summary>
        protected virtual string ClimbLabel(bool inside)
            => _outSide == 0 ? "Climb through the window" : inside ? "Climb out of the window" : "Climb in through the window";
        protected virtual string ClimbBusyLabel(bool inside) => _outSide == 0 ? "Climbing through" : inside ? "Climbing out" : "Climbing in";
        /// <summary>Called once the survivor has climbed through and landed on the far side.</summary>
        protected virtual void OnClimbed(InteractContext ctx) { }

        /// <summary>Throws the lower sash up or pulls it down (a latched window won't open). Noisy when
        /// <paramref name="noisy"/>: sashes rattle.</summary>
        public void SetOpen(bool open, bool noisy)
        {
            if (_open == open || (open && _latched)) return;
            _open = open;
            if (_sash != null) _sash.SetOpen(open);
            if (noisy) MakeNoise(CloneZConfig.WindowNoise);
            _climbCheckedAt = -1f;
        }

        private void SetLatched(bool latched, InteractContext ctx)
        {
            if (_latched == latched || (latched && _open)) return;
            _latched = latched;
            ShowLatch();
            if (ctx != null) ctx.Notify(latched ? "You turn the sash lock. The window is latched." : "You turn the sash lock back.");
        }

        private void SetDrawn(bool drawn)
        {
            if (_drawn == drawn) return;
            _drawn = drawn;
            ShowCurtains();
        }

        private void Apply(bool instant)
        {
            if (_sash != null) _sash.SetOpen(_open, instant);
            ShowLatch();
            ShowCurtains();
            _climbCheckedAt = -1f;
        }

        private void ShowLatch()
        {
            if (_latchShut != null) _latchShut.enabled = _latched;
            if (_latchOpen != null) _latchOpen.enabled = !_latched;
            PartsChanged();
        }

        // Drawn curtains (a lowered blind) show, and screen the opening; drawn back, the opening is clear again.
        private void ShowCurtains()
        {
            if (_curtainsOpen != null) _curtainsOpen.enabled = !_drawn;
            if (_curtainsDrawn != null) _curtainsDrawn.enabled = _drawn;
            if (_screen != null) _screen.enabled = _drawn;
            PartsChanged();
        }

        // A renderer switched on or off: the camera's cutaway gathers the wall's parts afresh, so a look shown while
        // the wall is cut away hides with it.
        protected void PartsChanged()
        {
            if (_occluder == null) _occluder = _piece != null ? _piece.GetComponent<CutawayOccluder>() : GetComponentInParent<CutawayOccluder>();
            if (_occluder != null) _occluder.PartsChanged();
        }

        // Peering out: the dead on the far side, as far as the light allows and where nothing hides them.
        private void CheckWindow(InteractContext ctx)
        {
            var parts = new List<string>();
            parts.Add(_open ? "open" : "closed");
            if (_latched) parts.Add("latched");
            if (_drawn) parts.Add(_blind ? "blind down" : "curtains drawn");
            string status = "The window is " + string.Join(", ", parts) + ".";
            ctx.Notify(status);
        }

        // Peering out: the dead on the far side, as far as the light allows and where nothing hides them.
        private void Look(InteractContext ctx)
        {
            int side = SideOf(ctx.Position, _point, _normal);
            Vector3 outward = _normal * -side;
            float range = Lookout.Range(CloneZConfig.WindowLookRange, out bool dark);
            int n = Lookout.Count(_point + outward * (WallFace + 0.06f), _point, outward, range, out float nearest);
            string where = _outSide == 0 ? "on the other side" : "outside";
            string seen;
            if (n == 0) seen = dark ? "It's dark out there \u2014 nothing moves that you can make out." : "Nothing moves out there.";
            else if (nearest <= CloneZConfig.WindowUnderRange)
                seen = n == 1 ? "One of the dead is right under the window!"
                    : Lookout.Capitalised(Lookout.Number(n)) + " of the dead " + where + " \u2014 one right under the window!";
            else seen = n == 1 ? "One of the dead shuffles past " + where + "."
                : Lookout.Capitalised(Lookout.Number(n)) + " of the dead shuffle past " + where + ".";
            if (dark && n > 0) seen += " There may be more in the dark.";
            string peek = LookPrefix;
            if (peek != null) seen = peek + seen;
            ctx.Notify(seen);
        }

        /// <summary>What the survivor does first to see out (ease the drawn curtain aside), or null when nothing's in the way.</summary>
        protected virtual string LookPrefix
            => !_drawn ? null : _blind ? "You lift the edge of the blind and peer out. " : "You ease the curtain aside and peer out. ";

        private bool ClimbClear(InteractContext ctx, int side)
        {
            float now = Time.time;
            if (_climbCheckedAt >= 0f && _climbSide == side && now - _climbCheckedAt < CloneZConfig.WindowClimbRecheck) return _climbClear;
            _climbCheckedAt = now;
            _climbSide = side;
            _climbClear = FindLanding(-side, ctx.Transform, out _);
            return _climbClear;
        }

        // Through the open window to the far side, facing away from the wall — if there's still room to land.
        private void Climb(InteractContext ctx)
        {
            if (!CanClimb)
            {
                ctx.Notify(ClimbBlocked);
                return;
            }
            int to = -SideOf(ctx.Position, _point, _normal);
            _climbCheckedAt = -1f;
            if (!FindLanding(to, ctx.Transform, out Vector3 at))
            {
                ctx.Notify("Something's blocking the other side");
                return;
            }
            Vector3 n = _normal * to;
            ctx.Teleport(at, Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg);
            if (ctx.Stats != null) ctx.Stats.TryConsumeEndurance(CloneZConfig.WindowClimbEndurance);
            MakeNoise(CloneZConfig.WindowClimbNoise);
            OnClimbed(ctx);
        }

        // The first clear spot on side `toSide` to land on: room to stand (nothing solid, none of the dead) and a clear
        // reach to it from the opening over whatever stands under the window. Feet on the floor (or the ground) there.
        private bool FindLanding(int toSide, Transform self, out Vector3 at)
        {
            Vector3 n = _normal * toSide;
            var foot = new Vector3(_point.x, 0f, _point.z);
            Vector3 sill = foot + n * (WallFace + 0.03f) + Vector3.up * (_point.y - 0.25f);
            for (int d = 0; d < LandDepths.Length; d++)
            {
                for (int a = 0; a < LandAlong.Length; a++)
                {
                    Vector3 p = foot + n * LandDepths[d] + _along * LandAlong[a];
                    p.y = BuildPiece.FloorTopAt(p);
                    if (!RoomToStand(p, self)) continue;
                    if (Physics.Linecast(sill, p + Vector3.up * 1.2f, SightMask, QueryTriggerInteraction.Ignore)) continue;
                    at = p;
                    return true;
                }
            }
            at = default;
            return false;
        }

        // A survivor-sized capsule standing at `feet` (from just above the floor) touches nothing solid and none of the dead.
        private static bool RoomToStand(Vector3 feet, Transform self)
        {
            Vector3 a = feet + Vector3.up * (0.15f + BodyRadius), b = feet + Vector3.up * (2f - BodyRadius);
            int n = Physics.OverlapCapsuleNonAlloc(a, b, BodyRadius, s_Hits, BodyMask, QueryTriggerInteraction.Ignore);
            bool clear = true;
            for (int i = 0; i < n && clear; i++)
            {
                Collider c = s_Hits[i];
                if (c != null && (self == null || !c.transform.IsChildOf(self))) clear = false;
            }
            Array.Clear(s_Hits, 0, n);
            return clear;
        }

        public override void CaptureState(FixtureSave save) => save.State = (_open ? 1 : 0) | (_drawn ? 2 : 0) | (_latched ? 4 : 0);

        public override void RestoreState(FixtureSave save)
        {
            _open = (save.State & 1) != 0;
            _drawn = (save.State & 2) != 0;
            _latched = !_open && (save.State & 4) != 0;
            Apply(true);
        }
    }

    /// <summary>Counting the dead in view from a door or a window — what the peephole and "Look outside" report.</summary>
    internal static class Lookout
    {
        private static readonly int SightMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        /// <summary>How far out the survivor can make the dead out right now: <paramref name="range"/> by day, less after
        /// dark (the vision rig's sight distance); <paramref name="dark"/> when the dark cut it short.</summary>
        public static float Range(float range, out bool dark)
        {
            float r = range;
            if (ZombieVisibility.HasSight) r = Mathf.Min(r, Mathf.Max(3f, ZombieVisibility.CurrentSight.SightDistance));
            else if (TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight) r *= 0.5f;
            dark = r < range * 0.75f;
            return r;
        }

        /// <summary>The living dead beyond a wall — on the <paramref name="outward"/> side of the plane through
        /// <paramref name="wall"/>, within <paramref name="range"/> (flat) — that <paramref name="eye"/> can see: nothing
        /// solid between it and their chest or their middle (other bodies don't hide them).</summary>
        public static int Count(Vector3 eye, Vector3 wall, Vector3 outward, float range, out float nearest)
        {
            nearest = float.MaxValue;
            int n = 0;
            IReadOnlyList<ZombieVisibility> all = ZombieVisibility.All;
            for (int i = 0; i < all.Count; i++)
            {
                ZombieVisibility z = all[i];
                if (z == null) continue;
                ZombieHealth h = z.GetComponent<ZombieHealth>();
                if (h == null || h.IsDead) continue;
                Vector3 p = z.transform.position;
                Vector3 d = p - wall;
                d.y = 0f;
                if (Vector3.Dot(d, outward) < 0.3f) continue;
                float dist = d.magnitude;
                if (dist > range) continue;
                if (Physics.Linecast(eye, p + Vector3.up * 1.2f, SightMask, QueryTriggerInteraction.Ignore)
                    && Physics.Linecast(eye, p + Vector3.up * 0.5f, SightMask, QueryTriggerInteraction.Ignore)) continue;
                n++;
                if (dist < nearest) nearest = dist;
            }
            return n;
        }

        private static readonly string[] Words =
        {
            "none", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve",
        };

        /// <summary>A count in words up to twelve ("three"), in figures past it.</summary>
        public static string Number(int n) => n >= 0 && n < Words.Length ? Words[n] : n.ToString();

        public static string Capitalised(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
