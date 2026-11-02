using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A hole in a <see cref="BreakableWall"/> (a partition or an outside wall), or the place in one the dead are battering a hole through. While the wall stands
    /// there it only keeps count of the blows (<see cref="RaidDoor"/>'s: the dead batter it, a blast shakes it, and every blow
    /// carries down the street); at none it gives way with a crash: cut out of the wall, its broken studs and ragged plaster
    /// showing and the plaster on the floor, wide and tall enough for the NavMesh to lead through. A survivor can nail planks
    /// across it again ([E], with a hammer, planks and nails): the dead batter those down in turn, and the build tool's Demolish
    /// takes them down again (giving most of the planks back).
    /// </summary>
    internal sealed class WallBreach : Interactable, IDamageable, IDemolishable
    {
        /// <summary>What it is: the wall still standing there (being battered), a hole broken through, or a hole boarded over.</summary>
        internal const int Intact = 0, Broken = 1, Boarded = 2;

        private static readonly Color StudWood = new Color(0.62f, 0.49f, 0.33f);
        private static readonly Color Plaster = new Color(0.84f, 0.83f, 0.79f);
        private static readonly Color PlankWood = new Color(0.50f, 0.40f, 0.28f);
        private static readonly Color BangCaption = new Color(0.88f, 0.62f, 0.46f);

        internal BreakableWall Wall { get; private set; }
        /// <summary>Where it runs along its wall (in the wall's object) and how high it goes.</summary>
        internal float A { get; private set; }
        internal float B { get; private set; }
        internal float Top { get; private set; }
        internal float Centre => (A + B) * 0.5f;
        internal int State { get; private set; }
        /// <summary>True once it's a way through (broken through, not boarded over).</summary>
        internal bool IsOpen => State == Broken;

        private float _health, _maxHealth;
        private int _side = 1;
        private GameObject _edges, _rubble, _boards, _splinters;
        private BoxCollider _patch;
        private float _captionAt = -999f;
        private Action<InteractContext> _boardAct, _boardBegin;
        private Action<InteractContext, float, float> _boardTick;
        private float _boardClock, _boardNext;

        internal void Init(BreakableWall wall, float a, float b, float top)
        {
            Wall = wall;
            A = a;
            B = b;
            Top = top;
            State = Intact;
            _health = _maxHealth = CloneZConfig.RaidPartitionHealth;
            _boardAct = BoardUp;
            _boardBegin = _ =>
            {
                _boardClock = 0f;
                _boardNext = CloneZConfig.RaidPartitionPatchInterval;
            };
            _boardTick = BoardTick;
        }

        // The hole's height inside the room (it may be cut a little past the top of the wall).
        private float Height => Mathf.Min(Top, Wall.Height);

        private int Seed
        {
            get
            {
                Vector3 p = transform.position;
                return unchecked(Mathf.RoundToInt(p.x * 100f) * 73856093 ^ Mathf.RoundToInt(p.z * 100f) * 19349663 ^ Mathf.RoundToInt(p.y * 10f));
            }
        }

        /// <summary>Round the hole, grown a little (what the NavMesh takes in again once it changes).</summary>
        internal Bounds Bounds
        {
            get
            {
                float w = B - A;
                return new Bounds(transform.position + Vector3.up * (Height * 0.5f), new Vector3(w + 2f, Height + 1f, w + 2f));
            }
        }

        #region Interactable
        public override string DisplayName => State == Boarded ? "Boarded-up hole" : "Hole in the wall";
        public override Vector3 InteractPoint => transform.TransformPoint(new Vector3(0f, 1.05f, 0f));
        public override bool IsAvailable => State == Broken && isActiveAndEnabled;

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (State != Broken) return;
            PlayerInventory inv = ctx.Inventory;
            int planks = CloneZConfig.RaidPartitionPatchPlanks, nails = CloneZConfig.RaidPartitionPatchNails;
            string why = inv == null ? "You've nothing to do it with"
                : !inv.Has("hammer") ? "You need a hammer"
                : !inv.Has("plank", planks) ? "You need " + planks + " planks"
                : !inv.Has("nails", nails) ? "You need " + nails + " nails" : null;
            var a = new InteractAction("Board up the hole", _boardAct)
                .Timed(CloneZConfig.RaidPartitionPatchSeconds, "Nailing planks across the hole", HumanoidActivity.Build)
                .DisabledIf(why != null, why);
            a.Begin = _boardBegin;
            a.Tick = _boardTick;
            into.Add(a);
        }

        // A hammer blow every so often while the planks go up.
        private void BoardTick(InteractContext ctx, float dt, float progress)
        {
            _boardClock += dt;
            if (_boardClock < _boardNext) return;
            _boardNext += CloneZConfig.RaidPartitionPatchInterval;
            NoiseSystem.Emit(InteractPoint, CloneZConfig.NoiseHammering);
            Sfx.Play(SfxIds.DoorBash, InteractPoint, 0.35f, UnityEngine.Random.Range(1.15f, 1.3f), 26f);
        }

        private void BoardUp(InteractContext ctx)
        {
            if (State != Broken || Wall == null) return;
            PlayerInventory inv = ctx.Inventory;
            int planks = CloneZConfig.RaidPartitionPatchPlanks, nails = CloneZConfig.RaidPartitionPatchNails;
            if (inv == null || !inv.Has("hammer") || !inv.Has("plank", planks) || !inv.Has("nails", nails)) return;
            inv.Consume("plank", planks);
            inv.Consume("nails", nails);
            inv.WearTool("hammer");
            Board(Wall.SideOf(ctx.Position), CloneZConfig.RaidPartitionPatchHealth, false);
            NoiseSystem.Emit(InteractPoint, CloneZConfig.NoiseHammering);
            if (ctx.Skills != null) ctx.Skills.AddXp(PlayerSkills.Skill.Carpentry, CloneZConfig.XpCarpentryPerPlank * planks);
            ctx.Notify("You nail the planks across the hole. Nothing's getting through it now.");
        }
        #endregion

        #region The dead, and blasts
        public bool IsDead => State == Broken || Wall == null;
        public Transform Transform => transform;

        /// <summary>A blow from one of the dead (or a blast): it cracks, and at the last it gives way. A survivor's swings do
        /// nothing: they take a wall (or boards) down with the build tool.</summary>
        public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            if (info.Kind != DamageKind.Explosion && (info.Source == null || info.Source.GetComponentInParent<ZombieAI>() == null)) return;
            _health -= Mathf.Max(0f, info.Amount);
            if (_health <= 0f)
            {
                if (State == Intact) BreakThrough(true);
                else Unboard(true);
                return;
            }
            NoiseSystem.Emit(InteractPoint, CloneZConfig.RaidPartitionHitNoise);
            Wall.Jolted(this);
            bool weak = _health < _maxHealth * 0.35f;
            Caption(State == Boarded
                ? (weak ? "The boards over the hole are splintering!" : "Something is pounding on the boards.")
                : (weak ? "The wall is cracking under the blows!" : "Something is pounding on the wall."), false);
        }

        /// <summary>Breaks it through: its broken edges drawn and the wall cut round it; <paramref name="loud"/>, with the crash
        /// (and the caption) of the dead battering it down.</summary>
        internal void BreakThrough(bool loud)
        {
            if (State != Intact || Wall == null) return;
            State = Broken;
            _health = 0f;
            Edges();
            Wall.Opened(this, false);
            if (!loud) return;
            NoiseSystem.Emit(InteractPoint, CloneZConfig.RaidPartitionBreakNoise);
            Sfx.Play(SfxIds.DoorBash, InteractPoint, 1f, UnityEngine.Random.Range(0.7f, 0.8f), 48f);
            Caption("The wall gives way with a crash of splintering plaster!", true);
        }

        private void Caption(string text, bool always)
        {
            float now = Time.time;
            if (!always && now - _captionAt < CloneZConfig.RaidDoorCaptionGap) return;
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null) return;
            Vector3 d = pi.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > CloneZConfig.RaidDoorHearRange * CloneZConfig.RaidDoorHearRange) return;
            _captionAt = now;
            Subtitles.Show("Wall", text, BangCaption, 3f);
        }
        #endregion

        #region Boards
        /// <summary>How sound it is now (the wall there, or the boards across it).</summary>
        internal float Health => _health;

        /// <summary>Planks nailed across the hole from <paramref name="side"/> of the wall, as a survivor nails them (for the harness).</summary>
        internal void BoardOver(int side)
        {
            if (State == Broken) Board(side, CloneZConfig.RaidPartitionPatchHealth, false);
        }

        // Planks nailed across the hole on `side` (of the wall), as sound as `health`: an object of their own that blocks the
        // way (and the NavMesh), with a box to stop bodies.
        private void Board(int side, float health, bool restoring)
        {
            State = Boarded;
            _side = side >= 0 ? 1 : -1;
            _maxHealth = CloneZConfig.RaidPartitionPatchHealth;
            _health = Mathf.Clamp(health, 1f, _maxHealth);
            Drop(ref _boards);
            var m = new RaidMesh();
            FurnitureBuilder.OpeningBoards(m, new System.Random(Seed ^ 0x5EED), new OpeningLine(Wall.AlongX, 0f), Centre, B - A, 0.02f,
                _side, Wall.HalfThick + 0.03f, Height - 0.04f);
            _boards = Emit(m, "Boards", ShadowCastingMode.On, false);
            if (_patch == null) _patch = gameObject.AddComponent<BoxCollider>();
            _patch.center = new Vector3(0f, Height * 0.5f, 0f);
            _patch.size = Wall.Size(B - A, Height, Wall.HalfThick * 2f);
            _patch.enabled = true;
            Wall.Changed(this, restoring);
        }

        // The boards gone: battered down (`loud`, leaving their splinters) or taken down with the build tool.
        private void Unboard(bool loud)
        {
            if (State != Boarded) return;
            State = Broken;
            _health = 0f;
            if (_patch != null) _patch.enabled = false;
            Drop(ref _boards);
            if (loud) Splinters();
            Wall.Changed(this, false);
            if (!loud) return;
            NoiseSystem.Emit(InteractPoint, CloneZConfig.RaidPartitionBreakNoise);
            Sfx.Play(SfxIds.DoorBash, InteractPoint, 0.9f, UnityEngine.Random.Range(0.8f, 0.9f), 44f);
            Caption("The boards over the hole give way!", true);
        }

        // What's left of battered boards: broken lengths on the floor the side they were nailed on.
        private void Splinters()
        {
            Drop(ref _splinters);
            var rng = new System.Random(Seed ^ Time.frameCount);
            var m = new RaidMesh();
            Surf plank = DecorMaterials.Planks(PlankWood);
            for (int k = 0; k < 5; k++)
            {
                var size = new Vector3(R(rng, 0.3f, 0.9f), 0.025f, 0.15f);
                Vector3 at = Wall.Local(R(rng, A, B), 0.045f, _side * R(rng, 0.25f, 1f));
                m.Box(at, size, Quaternion.Euler(R(rng, -4f, 4f), R(rng, 0f, 180f), R(rng, -3f, 3f)), plank);
            }
            _splinters = Emit(m, "Splinters", ShadowCastingMode.Off, true);
        }
        #endregion

        #region Looks
        // The broken edges: the cut stud at either side (and the header over a hole that stops short of the top) with plaster
        // jutting ragged off both faces; and on the floor the sole plate left across the gap, broken plaster either side.
        private void Edges()
        {
            if (_edges != null) return;
            var rng = new System.Random(Seed);
            float h = Height, t = Wall.HalfThick;
            Surf stud = DecorPalette.Matte(StudWood), plaster = DecorPalette.Matte(Plaster);
            var m = new RaidMesh();
            for (int e = -1; e <= 1; e += 2)
            {
                float cut = e < 0 ? A : B, inward = -e;
                m.Box(Wall.Local(cut + inward * 0.004f, h * 0.5f, 0f), Wall.Size(0.008f, h, t * 2f + 0.006f), plaster);
                m.Box(Wall.Local(cut + inward * 0.027f, h * 0.5f, 0f), Wall.Size(0.038f, h, 0.089f), stud);
                for (int f = -1; f <= 1; f += 2)
                    for (int k = 0; k < 3; k++)
                    {
                        float len = R(rng, 0.12f, 0.4f), jut = R(rng, 0.03f, 0.11f), y = R(rng, 0.15f, h - 0.2f - len);
                        m.Box(Wall.Local(cut + inward * jut * 0.5f, y + len * 0.5f, f * (t - 0.004f)), Wall.Size(jut, len, 0.012f), plaster);
                    }
            }
            if (Top < Wall.Height - 0.01f)
            {
                m.Box(Wall.Local(Centre, h + 0.004f, 0f), Wall.Size(B - A, 0.008f, t * 2f + 0.006f), plaster);
                m.Box(Wall.Local(Centre, h - 0.019f, 0f), Wall.Size(B - A, 0.038f, 0.089f), stud);
                for (int f = -1; f <= 1; f += 2)
                    for (int k = 0; k < 3; k++)
                    {
                        float len = R(rng, 0.15f, 0.45f), jut = R(rng, 0.03f, 0.1f), x = R(rng, A + 0.1f, B - 0.1f - len);
                        m.Box(Wall.Local(x + len * 0.5f, h - jut * 0.5f, f * (t - 0.004f)), Wall.Size(len, jut, 0.012f), plaster);
                    }
            }
            _edges = Emit(m, "Edges", ShadowCastingMode.On, true);

            var floor = new RaidMesh();
            floor.Box(Wall.Local(Centre, 0.045f, 0f), Wall.Size(B - A, 0.03f, 0.089f), stud);
            for (int k = 0; k < 12; k++)
            {
                int side = (k & 1) == 0 ? 1 : -1;
                var size = new Vector3(R(rng, 0.05f, 0.2f), R(rng, 0.008f, 0.02f), R(rng, 0.04f, 0.16f));
                Vector3 at = Wall.Local(R(rng, A - 0.25f, B + 0.25f), 0.035f + size.y * 0.5f, side * R(rng, t + 0.05f, t + 0.75f));
                floor.Box(at, size, Quaternion.Euler(R(rng, -6f, 6f), R(rng, 0f, 180f), R(rng, -6f, 6f)), plaster);
            }
            _rubble = Emit(floor, "Rubble", ShadowCastingMode.Off, true);
        }

        // Draws `m` (in the wall's object) as a child of this one, its mesh going with the building's; `offNav` keeps it out of
        // the NavMesh (the edges and rubble: the way through is as wide as the cut).
        private GameObject Emit(RaidMesh m, string name, ShadowCastingMode shadows, bool offNav)
        {
            if (m.IsEmpty) return null;
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            List<Mesh> outer = RaidMesh.CreatedMeshes;
            RaidMesh.CreatedMeshes = Wall.Meshes;
            try { m.ApplyTo(go, transform.localPosition, shadows); }
            finally { RaidMesh.CreatedMeshes = outer; }
            if (offNav) go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            DecorPalette.Apply();
            return go;
        }

        // Takes a part away; its mesh stays listed with the building's until it goes (a NavMesh update may be reading it).
        private static void Drop(ref GameObject go)
        {
            if (go == null) return;
            InteriorPass.Kill(go);
            go = null;
        }

        private static float R(System.Random rng, float a, float b) => b <= a ? a : a + (float)rng.NextDouble() * (b - a);
        #endregion

        #region Kept
        /// <summary>What it is, to be put back when its building's raised again.</summary>
        internal BreakableWall.Kept Keep() => new BreakableWall.Kept { Centre = Centre, Boarded = State == Boarded, Side = _side, Health = _health };

        /// <summary>Put back as it was kept (the building being raised again): broken through, and boarded if it was.</summary>
        internal void Restore(in BreakableWall.Kept k)
        {
            State = Broken;
            _health = 0f;
            Edges();
            Wall.Opened(this, true);
            if (k.Boarded) Board(k.Side, k.Health, true);
        }
        #endregion

        #region The build tool (boards)
        public string DemolishName => "boards";

        public bool CanDemolish(out string reason)
        {
            reason = State == Boarded ? null : "There's nothing nailed up here";
            return State == Boarded;
        }

        public void GetSalvage(out int planks, out int nails)
        {
            planks = State == Boarded ? Mathf.Max(0, CloneZConfig.RaidPartitionPatchPlanks - 1) : 0;
            nails = 0;
        }

        public bool CarvesNavMesh => false;

        public void Dismantle() => Unboard(false);
        #endregion
    }
}
