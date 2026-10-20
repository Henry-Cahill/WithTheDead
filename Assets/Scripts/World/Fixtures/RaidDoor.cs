using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A door of the raid town: the starter home's door (<see cref="HomeDoor"/>) — a pair of leaves drawn shut across the
    /// doorway that open, shut and (an outer door, a bathroom's) lock just as the home's do — hung in a raid building's
    /// doorway by <c>RaidBuilding.Doors.cs</c>, with what a town overrun by the dead adds:
    /// <list type="bullet">
    /// <item>The dead batter down a shut door they can't get round (<see cref="ZombieAI"/> finds it through <see cref="Registry"/>,
    /// <see cref="NearestObstructing"/> and <see cref="Detour"/>): every blow costs it health and carries down the street,
    /// and at none it bursts open for good — its leaves hang askew off their hinges over a litter of splinters, and it can
    /// never be shut or locked again.</item>
    /// <item>From the far side of a locked door the survivor can kick it in: a few loud boots and a lot of breath, and it
    /// swings open with its lock torn out of the frame — it shuts again, but it won't lock.</item>
    /// </list>
    /// Raids are never saved, but the state keeps the home's bits (1 open, 2 locked) and adds 4 burst, 8 lock torn out.
    /// </summary>
    public sealed class RaidDoor : HomeDoor, IDamageable
    {
        /// <summary>Every raid door standing, for the dead's queries (empty outside a raid).</summary>
        public static readonly List<RaidDoor> Registry = new List<RaidDoor>();

        /// <summary>Moves on whenever a raid door (or a roll-up door) opens, shuts or bursts: the only times a way round the
        /// dead couldn't find before can turn up, so they think again only then.</summary>
        public static int Changes { get; internal set; }

        /// <summary>What a raid adds to a door's <see cref="HomeDoor.Setup"/>.</summary>
        internal struct Raid
        {
            /// <summary>How much battering it takes (<see cref="CloneZConfig.ZombieBarricadeDamage"/> a blow).</summary>
            public float Health;
            /// <summary>The wall line the doorway is cut in, its middle along it and its width.</summary>
            public OpeningLine Line;
            public float Mid, Width;
            /// <summary>The side (of the wall's normal) the leaves swing open toward.</summary>
            public int Swing;
            /// <summary>The building's footprint (world XZ): the dead only make for a door of a building they're in or are
            /// chasing someone into.</summary>
            public Rect Building;
            /// <summary>Each leaf's hinge (the foot of its hinged edge), the way it reaches across the gap shut, and how far
            /// it swings open (degrees, signed about the vertical).</summary>
            public Vector3[] Hinges, Reaches;
            public float[] Angles;
            /// <summary>The wood, for the splinters it leaves.</summary>
            public Color Wood;
            public int Seed;
        }

        private float _health = 1f, _maxHealth = 1f;
        private bool _broken, _lockBroken;
        private OpeningLine _line;
        private float _mid, _width = 1.5f;
        private int _swing = 1;
        private Rect _building;
        private Vector3[] _hinges = new Vector3[0], _reaches = new Vector3[0];
        private float[] _angles = new float[0];
        private Color _wood = new Color(0.42f, 0.27f, 0.15f);
        private int _seed;
        private string _brokenLabel = "Close the door";
        private float _captionAt = -999f;
        private float _kickClock, _kickNext;

        private Action<InteractContext> _kickAct, _kickBegin;
        private Action<InteractContext, float, float> _kickTick;

        private static readonly Color BangCaption = new Color(0.88f, 0.62f, 0.46f);

        // The detour search's candidates, best first (no allocation per search).
        private const int MaxCandidates = 6;
        private static readonly RaidDoor[] s_Doors = new RaidDoor[MaxCandidates];
        private static readonly float[] s_Scores = new float[MaxCandidates];
        private static readonly Vector3[] s_Stands = new Vector3[MaxCandidates];

        /// <summary>True while the door stands shut and whole: in the dead's way.</summary>
        public bool Obstructs => !_broken && !IsOpen;
        /// <summary>True once the dead have battered it off its hinges.</summary>
        public bool IsBroken => _broken;
        /// <summary>Its health, 0..1.</summary>
        public float HealthNormalized => _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;

        bool IDamageable.IsDead => this == null || !Obstructs;
        Transform IDamageable.Transform => transform;

        protected override string LockFault => _lockBroken ? "The lock's been kicked out of the frame" : null;

        protected override void Awake()
        {
            base.Awake();
            _kickAct = KickIn;
            _kickBegin = c =>
            {
                _kickClock = 0f;
                _kickNext = CloneZConfig.RaidDoorKickInterval * 0.4f;
            };
            _kickTick = KickTick;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!Registry.Contains(this)) Registry.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Registry.Remove(this);
        }

        /// <summary>Makes <paramref name="root"/> (holding the leaves, at the doorway's middle on the wall line, under the
        /// doorway's wall segment) a working raid door.</summary>
        internal static RaidDoor Attach(GameObject root, in Setup s, in Raid r)
        {
            RaidDoor d = Attach<RaidDoor>(root, null, s);
            d._maxHealth = d._health = Mathf.Max(1f, r.Health);
            d._line = r.Line;
            d._mid = r.Mid;
            d._width = Mathf.Max(0.2f, r.Width);
            d._swing = r.Swing >= 0 ? 1 : -1;
            d._building = r.Building;
            d._hinges = r.Hinges ?? new Vector3[0];
            d._reaches = r.Reaches ?? new Vector3[0];
            d._angles = r.Angles ?? new float[0];
            d._wood = r.Wood;
            d._seed = r.Seed;
            d._brokenLabel = "Close the " + d.Noun;
            return d;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            if (_broken)
            {
                into.Add(new InteractAction(_brokenLabel).DisabledIf(true, "It's been battered off its hinges"));
                return;
            }
            int start = into.Count;
            base.GetActions(ctx, into);

            // The far side of a locked door: put a boot (or a shoulder) to it.
            if (IsLocked && LockSide != 0 && SideOf(ctx.Position, InteractPoint, Normal) != LockSide)
            {
                var kick = new InteractAction("Kick the door in", _kickAct)
                    .Timed(CloneZConfig.RaidDoorKickSeconds, "Kicking the door in", HumanoidActivity.Build)
                    .DisabledIf(ctx.Stats != null && ctx.Stats.Endurance < CloneZConfig.RaidDoorKickMinEndurance, "You're too winded to kick it in");
                kick.Begin = _kickBegin;
                kick.Tick = _kickTick;
                into.Insert(Mathf.Min(start + 1, into.Count), kick);
            }

            // New lightweight inspection action for raid doors
            into.Add(new InteractAction("Inspect door", ctx => {
                float health = _health;
                float maxHealth = _maxHealth;
                int percent = Mathf.RoundToInt(HealthNormalized * 100f);
                ctx.Notify($"Door health: {health:0.##}/{maxHealth:0.##} ({percent}%){(_broken ? " (broken)" : string.Empty)}");
            }));

            // New action to listen to the door's condition
            into.Add(new InteractAction("Listen to door", ctx => {
                if (_broken)
                    ctx.Notify("The door lies in splintered wood, silent and useless.");
                else {
                    int percent = Mathf.RoundToInt(HealthNormalized * 100f);
                    string state = percent < 35 ? "severely damaged" : "in decent shape";
                    ctx.Notify($"You hear the door creak; it’s {state} ({percent} %).");
                }
            }));
        }

        // A burst door keeps its leaves where they fell and has nothing left to shut; otherwise as the home's.
        protected override void Apply(bool instant)
        {
            Changes++;
            if (_broken)
            {
                SetSolid(false);
                return;
            }
            base.Apply(instant);
        }

        #region The dead
        /// <summary>The squared flat distance from <paramref name="p"/> to the nearest point of the doorway.</summary>
        public float SqrDistance(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            Vector3 along = _line.Along;
            float t = Mathf.Clamp(Vector3.Dot(d, along), -_width * 0.5f, _width * 0.5f);
            return (d - along * t).sqrMagnitude;
        }

        /// <summary>Which side of the door (of its wall's normal) <paramref name="p"/> stands on: +1 or -1.</summary>
        public int SideOfDoor(Vector3 p)
        {
            Vector3 d = p - transform.position;
            d.y = 0f;
            return Vector3.Dot(d, Normal) >= 0f ? 1 : -1;
        }

        /// <summary>Where one of the dead stands to batter the door from its <paramref name="side"/>: clear of the carve
        /// across the shut doorway.</summary>
        public Vector3 StandingPoint(int side) => transform.position + Normal * (side * CloneZConfig.ZombieDoorStandOff);

        // True when p lies in (or right at the walls of) the door's building.
        private bool Holds(Vector3 p)
        {
            Rect b = _building;
            return p.x > b.xMin - 0.3f && p.x < b.xMax + 0.3f && p.z > b.yMin - 0.3f && p.z < b.yMax + 0.3f;
        }

        /// <summary>The nearest shut door within <paramref name="range"/> (flat, to its doorway) of <paramref name="from"/>
        /// that stands between it and <paramref name="towards"/> — the one a zombie with no way round should batter.</summary>
        public static RaidDoor NearestObstructing(Vector3 from, Vector3 towards, float range)
        {
            RaidDoor best = null;
            float bestSqr = range * range;
            for (int i = 0; i < Registry.Count; i++)
            {
                RaidDoor d = Registry[i];
                if (d == null || !d.Obstructs) continue;
                float s = d.SqrDistance(from);
                if (s > bestSqr) continue;
                if (d.SideOfDoor(from) == d.SideOfDoor(towards)) continue;   // not between them
                bestSqr = s;
                best = d;
            }
            return best;
        }

        /// <summary>
        /// The shut door a zombie at <paramref name="from"/> that can't reach <paramref name="target"/> should make for: a door of
        /// the building one of them is in, within <paramref name="range"/>, that going through brings it nearer the target, and
        /// that it can walk up to on the side away from the target (a complete path to <paramref name="standAt"/>, worked out in
        /// the caller's reusable <paramref name="path"/>) — the shortest way through first, at most three paths tried. Null when
        /// there's none.
        /// </summary>
        public static RaidDoor Detour(Vector3 from, Vector3 target, float range, NavMeshPath path, out Vector3 standAt)
        {
            standAt = default;
            if (path == null) return null;
            float rangeSqr = range * range;
            float direct = Flat(target - from).magnitude;
            int n = 0;
            for (int i = 0; i < Registry.Count; i++)
            {
                RaidDoor d = Registry[i];
                if (d == null || !d.Obstructs) continue;
                if (!d.Holds(from) && !d.Holds(target)) continue;
                if (d.SqrDistance(from) > rangeSqr) continue;
                int side = -d.SideOfDoor(target);   // batter it from the side away from the target
                Vector3 near = d.StandingPoint(side), far = d.StandingPoint(-side);
                float beyond = Flat(far - target).magnitude;
                if (beyond >= direct - 0.25f) continue;   // through it the target gets no nearer
                float score = Flat(near - from).magnitude + beyond;
                if (n == MaxCandidates && score >= s_Scores[n - 1]) continue;
                int j = n < MaxCandidates ? n++ : n - 1;
                while (j > 0 && s_Scores[j - 1] > score)
                {
                    s_Scores[j] = s_Scores[j - 1];
                    s_Doors[j] = s_Doors[j - 1];
                    s_Stands[j] = s_Stands[j - 1];
                    j--;
                }
                s_Scores[j] = score;
                s_Doors[j] = d;
                s_Stands[j] = near;
            }

            RaidDoor found = null;
            for (int k = 0; k < n && k < 3 && found == null; k++)
            {
                if (!NavMesh.CalculatePath(from, s_Stands[k], NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                found = s_Doors[k];
                standAt = s_Stands[k];
            }
            Array.Clear(s_Doors, 0, MaxCandidates);
            return found;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        /// <summary>A blow from one of the dead: it splinters, and at the last it bursts open.</summary>
        public void TakeDamage(in DamageInfo info)
        {
            if (!Obstructs) return;
            _health -= Mathf.Max(0f, info.Amount);
            if (_health <= 0f)
            {
                Burst(info.HitDirection);
                return;
            }
            NoiseSystem.Emit(InteractPoint, CloneZConfig.RaidDoorHitNoise);
            Caption(_health < _maxHealth * 0.35f ? "The " + Noun + " is splintering under the blows!" : "Something is pounding on the " + Noun + ".", false);
        }

        // Battered down: it flies open toward where the blow was heading and never shuts again.
        private void Burst(Vector3 push)
        {
            _health = 0f;
            _broken = true;
            int toward = Vector3.Dot(push, Normal) >= 0f ? 1 : -1;
            SetOpen(true, false);
            Wreck(toward);
            NoiseSystem.Emit(InteractPoint, CloneZConfig.RaidDoorBreakNoise);
            Caption("The " + Noun + " gives way with a crash of splintering wood!", true);
        }

        // The leaves left hanging askew off their hinges, swung toward `toward`, and the splinters on the floor.
        private void Wreck(int toward)
        {
            var rng = new System.Random(_seed);
            FixturePart[] leaves = Leaves;
            Vector3 along = _line.Along;
            for (int i = 0; i < leaves.Length; i++)
            {
                FixturePart leaf = leaves[i];
                if (leaf == null || i >= _hinges.Length || i >= _reaches.Length || i >= _angles.Length) continue;
                Vector3 reach = _reaches[i];
                float swing = toward == _swing ? _angles[i] : -Mathf.Sign(_angles[i]) * 80f;
                Quaternion turn = Quaternion.AngleAxis(swing * (0.55f + (float)rng.NextDouble() * 0.4f), Vector3.up);
                Quaternion lift = Quaternion.AngleAxis(3f + (float)rng.NextDouble() * 5f, Vector3.Cross(reach, Vector3.up));
                float roll = 5f + (float)rng.NextDouble() * 7f;
                // Tilted about its foot, its top falls toward the doorway rather than into the wall beside it.
                Vector3 top = reach * (_width * 0.25f) + Vector3.up * 2.1f;
                Quaternion q = Quaternion.identity;
                float best = float.MaxValue;
                for (int s = -1; s <= 1; s += 2)
                {
                    Quaternion t = turn * Quaternion.AngleAxis(s * roll, reach) * lift;
                    float off = Mathf.Abs(Vector3.Dot(_hinges[i] + t * top - transform.position, along));
                    if (off < best)
                    {
                        best = off;
                        q = t;
                    }
                }
                leaf.enabled = false;   // posed by hand from here on
                leaf.transform.SetPositionAndRotation(_hinges[i], q);
            }

            var m = new RaidMesh();
            FurnitureBuilder.DoorSplinters(m, rng, _line, _mid, _width, toward, _wood);
            m.Emit("Splinters", transform, transform.position, ShadowCastingMode.Off);
            DecorPalette.Apply();
            PartsChanged();
        }

        // Tells the survivor (a caption) what they can hear, if they're near enough — every blow at most every few seconds.
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
            Subtitles.Show(DisplayName, text, BangCaption, 3f);
        }
        #endregion

        #region Kicking it in
        private void KickTick(InteractContext ctx, float dt, float progress)
        {
            _kickClock += dt;
            if (_kickClock < _kickNext) return;
            _kickNext += CloneZConfig.RaidDoorKickInterval;
            MakeNoise(CloneZConfig.RaidDoorKickNoise);
        }

        // The last boot: the strike plate tears out of the frame and the door flies open.
        private void KickIn(InteractContext ctx)
        {
            if (_broken || !IsLocked) return;
            if (ctx.Stats != null) ctx.Stats.TryConsumeEndurance(CloneZConfig.RaidDoorKickEndurance);
            _lockBroken = true;
            _health = Mathf.Min(_health, _maxHealth * 0.6f);   // and the frame's the weaker for it
            SetOpen(true, true);
            MakeNoise(CloneZConfig.RaidDoorKickNoise);
            ctx.Notify("The frame splits and the " + Noun + " flies open. That lock won't hold again.");
        }
        #endregion

        public override void CaptureState(FixtureSave save)
        {
            base.CaptureState(save);
            save.State |= (_broken ? 4 : 0) | (_lockBroken ? 8 : 0);
            save.A = _health;
        }

        public override void RestoreState(FixtureSave save)
        {
            _lockBroken = (save.State & 8) != 0;
            if (save.A > 0f) _health = Mathf.Min(save.A, _maxHealth);
            if ((save.State & 4) != 0 && !_broken)
            {
                _broken = true;
                _health = 0f;
                SetOpen(true, false);
                Wreck(_swing);
                return;
            }
            base.RestoreState(save);
        }
    }
}
