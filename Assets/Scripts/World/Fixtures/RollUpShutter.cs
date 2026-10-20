using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// A depot's roll-up door: a steel curtain of slatted bands that rolls up into the drum over the inside of a wide doorway
    /// (<c>RaidBuilding.Openings.cs</c> hangs one in each of the depot's vehicle doorways). [E] hauls it up or pulls it down,
    /// from either side — a racket of rattling steel the dead hear well down the street. Down, it's solid: a collider across
    /// the doorway stops the survivor, the shots and every line of sight, and a carving <see cref="NavMeshObstacle"/> closes
    /// the navmesh across the gap (the dead don't batter steel; they go round). It won't come down on anyone standing under
    /// it. Up, the doorway is clear. Raids are never saved (bit 1: up).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RollUpShutter : Interactable
    {
        /// <summary>How a roll-up door is hung, handed over by its builder.</summary>
        internal struct Setup
        {
            /// <summary>"Roll-up door".</summary>
            public string Name;
            /// <summary>The curtain's bands, drawn down, each sliding up into the drum.</summary>
            public FixturePart[] Bands;
            /// <summary>The doorway's middle on the wall line at handle height.</summary>
            public Vector3 Point;
            /// <summary>The lowered curtain's collider (world, axis-aligned), the gap's carve, and where nobody may stand for it to
            /// come down.</summary>
            public Bounds Solid, Gap, Block;
            /// <summary>A fresh raid finds it up.</summary>
            public bool Open;
        }

        private string _name = "Roll-up door", _key;
        private FixturePart[] _bands = new FixturePart[0];
        private BoxCollider _solid;
        private NavMeshObstacle _carver;
        private Vector3 _point;
        private Bounds _block;
        private bool _open;

        private Action<InteractContext> _raiseAct, _lowerAct, _bangAct, _inspectAct;

        private static readonly Collider[] s_Hits = new Collider[16];
        private static readonly int BodyMask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);

        /// <summary>True while the curtain is rolled up.</summary>
        public bool IsOpen => _open;

        public override string DisplayName => _name;
        public override Vector3 InteractPoint => _point;
        public override float Reach => CloneZConfig.RollUpReach;
        public override string SaveKey => _key;

        private void Awake()
        {
            _raiseAct = Raise;
            _lowerAct = Lower;
            _bangAct = Bang;
            _inspectAct = Inspect;
        }

        /// <summary>Makes <paramref name="root"/> (holding the bands, at the doorway's middle on the wall line) a working
        /// roll-up door: adds the lowered curtain's collider and the gap's carve, and sets the fresh-raid state.</summary>
        internal static RollUpShutter Attach(GameObject root, in Setup s)
        {
            var d = root.AddComponent<RollUpShutter>();
            d._name = string.IsNullOrEmpty(s.Name) ? "Roll-up door" : s.Name;
            d._key = KeyFor("rollup", root.transform.position);
            d._bands = s.Bands ?? new FixturePart[0];
            d._point = s.Point;
            d._block = s.Block;

            var solid = new GameObject("Curtain");
            solid.transform.SetParent(root.transform, false);
            solid.transform.SetPositionAndRotation(s.Solid.center, Quaternion.identity);
            d._solid = solid.AddComponent<BoxCollider>();
            d._solid.size = s.Solid.size;

            var carve = new GameObject("Carver");
            carve.transform.SetParent(root.transform, false);
            carve.transform.SetPositionAndRotation(s.Gap.center, Quaternion.identity);
            d._carver = carve.AddComponent<NavMeshObstacle>();
            d._carver.shape = NavMeshObstacleShape.Box;
            d._carver.center = Vector3.zero;
            d._carver.size = s.Gap.size;
            d._carver.carving = true;
            d._carver.carveOnlyStationary = false;

            d._open = s.Open;
            d.Apply(true);
            return d;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_open)
                into.Add(new InteractAction("Lower the roll-up door", _lowerAct)
                    .Timed(CloneZConfig.RollUpWorkSeconds, "Pulling the door down", HumanoidActivity.Use));
            else
                into.Add(new InteractAction("Raise the roll-up door", _raiseAct)
                    .Timed(CloneZConfig.RollUpWorkSeconds, "Hauling the door up", HumanoidActivity.Use));

            into.Add(new InteractAction("Bang the roll-up door", _bangAct)
                .Timed(0f, "Banging the door", HumanoidActivity.Use));
            into.Add(new InteractAction("Inspect the roll‑up door", _inspectAct).Timed(0f, "Inspecting the door", HumanoidActivity.Use));
        }

        private void Inspect(InteractContext ctx) => ctx.Notify(_open ? "The steel curtain is raised – the doorway is clear." : "The steel curtain is down – the doorway is blocked.");
        private void Raise(InteractContext ctx)
        {
            if (_open) return;
            _open = true;
            Apply(false);
            MakeNoise(CloneZConfig.RollUpNoise);
        }

        private void Bang(InteractContext ctx) => MakeNoise(CloneZConfig.RollUpNoise);

        private void Lower(InteractContext ctx)
        {
            if (!_open) return;
            if (Obstructed())
            {
                ctx.Notify("Something's in the way");
                return;
            }
            _open = false;
            Apply(false);
            MakeNoise(CloneZConfig.RollUpNoise);
        }

        // The bands, the collider across the doorway and the carve, all in step.
        private void Apply(bool instant)
        {
            for (int i = 0; i < _bands.Length; i++)
                if (_bands[i] != null) _bands[i].SetOpen(_open, instant);
            if (_solid != null) _solid.enabled = !_open;
            if (_carver != null) _carver.enabled = !_open;
            RaidDoor.Changes++;   // a way round may have opened (or shut) for the dead
        }

        // True while the survivor or one of the dead stands where the curtain comes down.
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

        public override void CaptureState(FixtureSave save) => save.State = _open ? 1 : 0;

        public override void RestoreState(FixtureSave save)
        {
            _open = (save.State & 1) != 0;
            Apply(true);
        }
    }
}
