using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// A door of the starter home, hung in a finished doorway <see cref="BuildPiece"/> by <c>StarterHome.Doors.cs</c>:
    /// a pair of panelled leaves filling the front door's and the store room's wide openings, a single leaf beside a
    /// fixed panel in the bathroom's. [E] opens or shuts it. The front door and the bathroom door lock — from the
    /// inside only (the living room, the bathroom); from the far side a locked door just rattles. The front door has
    /// a peephole onto the street, and anyone can knock on any door (a lure: the dead hear it).
    ///
    /// A shut door is solid: a collider over its leaves stops the survivor and every line of sight (the dead's
    /// included), a carving <see cref="NavMeshObstacle"/> closes the navmesh across the gap, and the doorway is
    /// flagged (<see cref="BuildPiece.HasDoor"/>, <see cref="BuildPiece.DoorOpen"/>) so the dead batter it down rather
    /// than walk in. An open door has neither: the swung-back leaves are only for show. Shutting is refused while the
    /// survivor or one of the dead stands in the doorway. Saved: bit 1 open, bit 2 locked.
    ///
    /// A door needn't hang in a <see cref="BuildPiece"/>: the raid town's (<see cref="RaidDoor"/>) hang in a raid
    /// building's doorways with no piece at all — keyed by where they hang, hiding with the wall segment above them.
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeDoor : Interactable
    {
        /// <summary>How a door is hung, handed over by its builder.</summary>
        internal struct Setup
        {
            /// <summary>"Front door" — the name over it.</summary>
            public string Name;
            /// <summary>"front door" — for "Open the front door".</summary>
            public string Noun;
            /// <summary>The leaves, built shut, hinged to swing a quarter turn open.</summary>
            public FixturePart[] Leaves;
            /// <summary>The gap's middle on the wall line at handle height (reached from either side).</summary>
            public Vector3 Point;
            /// <summary>The wall's normal toward its +1 side.</summary>
            public Vector3 Normal;
            /// <summary>The shut leaves (world, axis-aligned): their collider.</summary>
            public Bounds Leaf;
            /// <summary>The gap through the wall's full thickness: the navmesh carve.</summary>
            public Bounds Gap;
            /// <summary>Where nobody may stand for the door to shut (the gap, from just above the floor).</summary>
            public Bounds Block;
            /// <summary>0: no lock; otherwise the side (of <see cref="Normal"/>) it locks from.</summary>
            public int LockSide;
            /// <summary>"inside the house" — told to whoever tries to lock it from the far side.</summary>
            public string LockFrom;
            /// <summary>A front door's deadbolt (otherwise a bathroom's little bolt).</summary>
            public bool Deadbolt;
            /// <summary>A peephole, looked through from the lock side.</summary>
            public bool Peephole;
            /// <summary>A fresh run's state.</summary>
            public bool Open;
            /// <summary>A fresh run finds it locked (only a shut door with a lock).</summary>
            public bool Locked;
            /// <summary>Glazed leaves: the shut door stops the survivor and the dead's way through, but not their eyes.</summary>
            public bool SeeThrough;
            /// <summary>The cutaway marker its parts hide with; by default the doorway piece's, else the nearest one above the
            /// door (a raid door: its wall segment's).</summary>
            public CutawayOccluder Occluder;
        }

        private string _name = "Door", _noun = "door", _key;
        private string _openLabel, _closeLabel, _tryLabel, _lockElsewhere;
        private BuildPiece _piece;
        private CutawayOccluder _occluder;
        private FixturePart[] _leaves = new FixturePart[0];
        private BoxCollider _solid;
        private NavMeshObstacle _carver;
        private Vector3 _point, _normal = Vector3.forward;
        private Bounds _block;
        private int _lockSide;
        private bool _deadbolt, _peephole;
        private bool _open, _locked;
        // True when the door has been broken; it can no longer be locked and its solid parts are removed.
        private bool _broken;

        private Action<InteractContext> _openAct, _closeAct, _lockAct, _unlockAct, _rattleAct, _peepAct, _knockAct, _listenAct;

        private static readonly Collider[] s_Hits = new Collider[16];
        private static readonly int BodyMask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);
        private static readonly Color KnockCaption = new Color(0.86f, 0.74f, 0.52f);

        /// <summary>True while the door stands open.</summary>
        public bool IsOpen => _open;
        /// <summary>True while the door is locked (only ever while shut).</summary>
        public bool IsLocked => _locked;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override float Reach => CloneZConfig.DoorReach;
        public override bool IsAvailable => base.IsAvailable && (_piece == null || _piece.Health > 0f);
        public override string SaveKey => _key;

        /// <summary>The leaves, for a door that poses them itself (a raid door battered off its hinges).</summary>
        protected FixturePart[] Leaves => _leaves;
        /// <summary>The wall's normal toward its +1 side.</summary>
        protected Vector3 Normal => _normal;
        /// <summary>0: no lock; otherwise the side of <see cref="Normal"/> it locks from.</summary>
        protected int LockSide => _lockSide;
        /// <summary>"front door".</summary>
        protected string Noun => _noun;

        protected virtual void Awake()
        {
            _openAct = c => SetOpen(true, true);
            _closeAct = Close;
            _lockAct = c => SetLocked(true, c);
            _unlockAct = c => SetLocked(false, c);
            _rattleAct = Rattle;
            _peepAct = Peep;
            _knockAct = Knock;
            _listenAct = Listen;
        }

        /// <summary>Makes <paramref name="root"/> (already holding the leaves, under the doorway <paramref name="piece"/>)
        /// a working door: adds the shut leaves' collider and the gap's carve, and sets the fresh-run state.</summary>
        internal static HomeDoor Attach(GameObject root, BuildPiece piece, in Setup s) => Attach<HomeDoor>(root, piece, s);

        /// <summary>As <see cref="Attach(GameObject, BuildPiece, in Setup)"/>, for a door of a kind built on this one (a raid's
        /// <see cref="RaidDoor"/>). <paramref name="piece"/> may be null: a door hung in a raid building's doorway, keyed by
        /// where <paramref name="root"/> stands.</summary>
        internal static T Attach<T>(GameObject root, BuildPiece piece, in Setup s) where T : HomeDoor
        {
            var d = root.AddComponent<T>();
            d._piece = piece;
            d._occluder = s.Occluder;
            d._name = string.IsNullOrEmpty(s.Name) ? "Door" : s.Name;
            d._noun = string.IsNullOrEmpty(s.Noun) ? "door" : s.Noun;
            d._key = KeyFor("door", piece != null ? piece.transform.position : root.transform.position);
            d._leaves = s.Leaves ?? new FixturePart[0];
            d._point = s.Point;
            d._normal = s.Normal.sqrMagnitude > 1e-6f ? s.Normal.normalized : Vector3.forward;
            d._block = s.Block;
            d._lockSide = s.LockSide;
            d._deadbolt = s.Deadbolt;
            d._peephole = s.Peephole;
            d._openLabel = "Open the " + d._noun;
            d._closeLabel = "Close the " + d._noun;
            d._tryLabel = "Try the " + d._noun;
            d._lockElsewhere = "It only locks from " + (string.IsNullOrEmpty(s.LockFrom) ? "the other side" : s.LockFrom);

            // The shut leaves' collider sits in world axes (the doorway itself may be turned 90°).
            var solid = new GameObject("ShutLeaves");
            solid.transform.SetParent(root.transform, false);
            solid.transform.SetPositionAndRotation(s.Leaf.center, Quaternion.identity);
            if (s.SeeThrough) solid.layer = CloneZConfig.IgnoreRaycastLayer;   // still solid, but sight lines pass, as through glass
            d._solid = solid.AddComponent<BoxCollider>();
            d._solid.size = s.Leaf.size;

            var carve = new GameObject("Carver");
            carve.transform.SetParent(root.transform, false);
            carve.transform.SetPositionAndRotation(s.Gap.center, Quaternion.identity);
            d._carver = carve.AddComponent<NavMeshObstacle>();
            d._carver.shape = NavMeshObstacleShape.Box;
            d._carver.center = Vector3.zero;
            d._carver.size = s.Gap.size;
            d._carver.carving = true;
            d._carver.carveOnlyStationary = false;   // it never moves: carve the moment the door shuts

            d._open = s.Open;
            d._locked = !s.Open && s.LockSide != 0 && s.Locked;
            d.Apply(true);
            return d;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            int side = SideOf(ctx.Position, _point, _normal);
            bool lockSide = _lockSide != 0 && side == _lockSide;

            if (_open)
                into.Add(new InteractAction(_closeLabel, _closeAct));
            else if (_locked && !lockSide)
                into.Add(new InteractAction(_tryLabel, _rattleAct));
            else
                into.Add(new InteractAction(_openLabel, _openAct).DisabledIf(_locked, "It's locked \u2014 unlock it first"));

            // Force open action: shoulder a stuck door open (a locked one still has to be unlocked, or kicked in on a raid)
            into.Add(new InteractAction("Force open the " + _noun, ctx => SetOpen(true, true)).DisabledIf(_open, "It's already open")
                .DisabledIf(_locked, "It's locked \u2014 it won't budge"));

            if (_lockSide != 0)
            {
                string fault = LockFault;
                if (lockSide)
                {
                    if (_locked) into.Add(new InteractAction("Unlock the door", _unlockAct));
                    else into.Add(new InteractAction("Lock the door", _lockAct).DisabledIf(_open, "Close the door first")
                        .DisabledIf(fault != null, fault));
                }
                else if (!_locked) into.Add(new InteractAction("Lock the door").DisabledIf(true, fault ?? _lockElsewhere));
            }

            if (_locked && !_broken)
            {
                into.Add(new InteractAction("Bash the door", SetBroken)
                    .Timed(2f, "Bashing the door", HumanoidActivity.Use));
            }

            into.Add(new InteractAction("Listen at the door", _listenAct));
        // Check lock state without trying to open
        if (!_open) into.Add(new InteractAction("Check the door", ctx => ctx.Notify(_locked ? "The door is locked." : "The door is unlocked.")));
        // Inspect the lock type when on the lock side of a shut door
        if (!_open && _lockSide != 0 && lockSide)
            into.Add(new InteractAction("Inspect the lock", ctx => ctx.Notify(_deadbolt ? "A heavy deadbolt secures the door." : "A small bolt secures the door.")));
        }

        /// <summary>Opens or shuts the door — with the creak and thud the dead can hear when <paramref name="noisy"/>.
        /// Unchecked: the action that shuts it first makes sure nobody stands in the way.</summary>
        public void SetOpen(bool open, bool noisy)
        {
            if (_open == open) return;
            _open = open;
            if (open) _locked = false;
            Apply(false);
            // Notify cut‑away occluder that the door's visual parts have changed so they render correctly when the door opens or closes.
            PartsChanged();
            if (noisy)
            {
                MakeNoise(CloneZConfig.FixtureNoiseDoor);
                Sfx.Play(open ? SfxIds.DoorOpen : SfxIds.DoorClose, transform.position + Vector3.up * 1.2f, 0.75f, UnityEngine.Random.Range(0.92f, 1.08f), 22f);
            }
        }

        // The leaves, the collider over them, the carve across the gap and the doorway's flags, all in step.
        protected virtual void Apply(bool instant)
        {
            for (int i = 0; i < _leaves.Length; i++)
                if (_leaves[i] != null) _leaves[i].SetOpen(_open, instant);

            if (_broken)
            {
                // A broken door cannot be locked and its solid parts disappear.
                _locked = false;
                _lockSide = 0;
                SetSolid(false); // remove collider and navmesh carve
            }
            else
            {
                SetSolid(!_open);
            }

            if (_piece != null)
            {
                _piece.HasDoor = true;
                _piece.DoorOpen = _open;
            }
        }

        /// <summary>Switches the shut door's collider and its carve across the gap on or off.</summary>
        protected void SetSolid(bool solid)
        {
            if (_solid != null) _solid.enabled = solid;
            if (_carver != null) _carver.enabled = solid;   // switching it off lifts the carve; on, it carves again
        }

        /// <summary>Why the lock can't be turned (it's been kicked out of the frame), or null while it works.</summary>
        protected virtual string LockFault => null;

        /// <summary>Tells the camera's cutaway the door's parts changed (a renderer added or shown), so they hide with its
        /// wall while it's cut away. Also runs when the door becomes broken.
        /// </summary>
        protected void PartsChanged()
        {
            if (_occluder == null) _occluder = _piece != null ? _piece.GetComponent<CutawayOccluder>() : GetComponentInParent<CutawayOccluder>();
            if (_occluder != null) _occluder.PartsChanged();
        }

        private void Close(InteractContext ctx)
        {
            if (Obstructed())
            {
                ctx.Notify("Something's in the way");
                return;
            }
            SetOpen(false, true);
        }

        // True while the survivor or one of the dead stands where the shut leaves would be.
        private bool Obstructed()
        {
            int n = Physics.OverlapBoxNonAlloc(_block.center, _block.extents, s_Hits, Quaternion.identity, BodyMask, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            for (int i = 0; i < n && !blocked; i++)
            {
                Collider c = s_Hits[i];
                if (c == null) continue;
                blocked = c is CharacterController
                    || c.GetComponentInParent<ZombieHealth>() != null
                    || c.GetComponentInParent<PlayerStats>() != null;
            }
            Array.Clear(s_Hits, 0, n);
            return blocked;
        }

        private void SetBroken(InteractContext ctx)
        {
            if (_broken) return;
            _broken = true;
            _locked = false;
            _lockSide = 0;
            Apply(false);
            PartsChanged();
            Sfx.Play(SfxIds.DoorBash, transform.position + Vector3.up * 1.2f, 0.75f, UnityEngine.Random.Range(0.92f, 1.08f), 22f);
            ctx.Notify("You slam the door hard; the lock snaps and the door is now broken.");
        }

        // Trying the handle of a door locked from the other side.
        private void SetLocked(bool locked, InteractContext ctx)
        {
            if (_broken) return; // a broken door cannot be locked
            if (_lockSide == 0 || _locked == locked || (locked && (_open || LockFault != null))) return;
            _locked = locked;
            if (ctx == null) return;
            if (_deadbolt) ctx.Notify(locked ? "You turn the deadbolt. The " + _noun + " is locked." : "You draw back the deadbolt.");
            else ctx.Notify(locked ? "You slide the little bolt across. The " + _noun + " is locked." : "You slide the bolt back.");
        }
        private void Rattle(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.DoorRattleNoise);
            ctx.Notify("The handle turns, but the door won't budge \u2014 it's locked from the inside.");
        }

        // An eye to the peephole: the dead out on the street, as far as the light allows and where nothing hides them.
        private void Peep(InteractContext ctx)
        {
            Vector3 outward = _normal * -_lockSide;
            float range = Lookout.Range(CloneZConfig.DoorPeepRange, out bool dark);
            Vector3 eye = _point + Vector3.up * 0.5f + outward * 0.16f;
            int n = Lookout.Count(eye, _point, outward, range, out float nearest);
            string seen;
            if (n == 0) seen = dark ? "It's dark out there \u2014 nothing moves that you can make out." : "The street is clear.";
            else if (nearest <= CloneZConfig.DoorDoorstepRange)
                seen = n == 1 ? "One of the dead is right outside the door!"
                    : Lookout.Capitalised(Lookout.Number(n)) + " of the dead out front \u2014 one right on the doorstep!";
            else seen = n == 1 ? "You count one of the dead out front." : "You count " + Lookout.Number(n) + " of the dead out front.";
            if (dark && n > 0) seen += " There may be more in the dark.";
            ctx.Notify(seen);
        }

        // A knock carries: the dead nearby come to see who's calling.
        private void Knock(InteractContext ctx)
        {
            MakeNoise(CloneZConfig.DoorKnockNoise);
            Subtitles.Show(_name, "Knock, knock, knock.", KnockCaption, 2.5f);
            bool farSide = _lockSide != 0 && SideOf(ctx.Position, _point, _normal) != _lockSide;
            ctx.Notify(farSide ? "You knock. Nobody answers \u2014 nobody living, anyway."
                : "You rap on the door. The sound carries \u2014 anything nearby will have heard it.");
        }

        private void Listen(InteractContext ctx)
        {
            // Use the same range as the peephole view; hearing is plausible through a thin door.
            float range = CloneZConfig.DoorPeepRange;
            int n = Lookout.Count(_point, _point, _normal, range, out float nearest);
            string msg;
            if (n == 0) msg = "You hear nothing but the wind.";
            else if (n == 1) msg = "You hear a shuffling footstep nearby.";
            else msg = $"You hear {Lookout.Number(n)} shuffling footsteps near the door.";
            ctx.Notify(msg);
        }

        public override void CaptureState(FixtureSave save) => save.State = (_open ? 1 : 0) | (_locked ? 2 : 0) | (_broken ? 4 : 0);

        public override void RestoreState(FixtureSave save)
        {
            _open = (save.State & 1) != 0;
            _locked = !_open && _lockSide != 0 && (save.State & 2) != 0;
            _broken = (save.State & 4) != 0;
            Apply(true);
        }
    }
}
