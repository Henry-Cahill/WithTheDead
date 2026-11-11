using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Added to every zombie by the spawner; hides the zombie's body while the survivor can't see it,
    /// Project Zomboid style. A zombie is seen when it is within sight distance (by day ~30 m; at night
    /// the reach of the survivor's vision light, further with night vision or when the zombie stands in
    /// the flashlight beam or a lit room), inside the survivor's vision cone or right beside them, and
    /// nothing solid lies between the survivor's eye and the zombie. Solid means the Sight Occluder layer,
    /// where the world's solid colliders live (<see cref="SightOccluders"/>).
    ///
    /// The checks don't run per zombie: the <see cref="VisionController"/> calls <see cref="TickAll"/> once
    /// a frame. Every zombie gets the cheap part of the rule every frame (range, cone, arm's reach), which
    /// settles one out of view without a ray. A zombie in view has its line of sight checked when it's due:
    /// at once when it has just come into view, every <see cref="NearInterval"/> close by, easing to every
    /// <see cref="FarInterval"/> far off, and sooner as it moves. Up to <see cref="MaxChecksPerFrame"/> due
    /// checks run a frame, the most overdue first; on top of those goes any zombie in view that would
    /// otherwise wait <see cref="MaxCheckAge"/>, however big the horde. A zombie that was seen is re-checked
    /// with one ray, to whichever point (chest or head) was clear last time. Hiding waits
    /// <see cref="HideDelay"/> so a zombie on the edge of view doesn't flicker; showing is immediate.
    /// With no vision rig (or no survivor) every zombie is shown.
    /// </summary>
    [DisallowMultipleComponent]
    public class ZombieVisibility : MonoBehaviour
    {
        /// <summary>
        /// What the survivor can see this frame, filled in by the <see cref="VisionController"/>.
        /// </summary>
        public struct Sight
        {
            /// <summary>The survivor's eye, the start of every line-of-sight test.</summary>
            public Vector3 Eye;
            /// <summary>Flat unit vector the survivor faces / looks along.</summary>
            public Vector3 Facing;
            /// <summary>Cosine of the vision cone's half angle.</summary>
            public float CosHalfAngle;
            /// <summary>Anything this close (flat distance) is noticed whichever way the survivor faces.</summary>
            public float AwarenessRadius;
            /// <summary>How far the survivor can see in the current light.</summary>
            public float SightDistance;
            /// <summary>How far a zombie standing in a lit room (or the flashlight beam) can be seen.</summary>
            public float LitSightDistance;
            /// <summary>True while the flashlight beam is shining.</summary>
            public bool BeamOn;
            /// <summary>Where the flashlight beam starts.</summary>
            public Vector3 BeamOrigin;
            /// <summary>Unit direction of the flashlight beam.</summary>
            public Vector3 BeamDirection;
            /// <summary>Reach of the flashlight beam.</summary>
            public float BeamRange;
            /// <summary>Cosine of the flashlight beam's half angle.</summary>
            public float BeamCosHalfAngle;
        }

        /// <summary>What one <see cref="TickAll"/> did, for the profiler and the smoke test.</summary>
        public struct TickStats
        {
            /// <summary>Zombies registered.</summary>
            public int Zombies;
            /// <summary>Zombies in view: in range and in the cone, or within arm's reach. Their line of sight decides.</summary>
            public int InView;
            /// <summary>Line-of-sight checks run, and how many of them were held to <see cref="MaxCheckAge"/>.</summary>
            public int Checks, Forced;
            /// <summary>Rays cast.</summary>
            public int Rays;
            /// <summary>The longest a zombie in view had gone unchecked when the tick ended, in seconds.</summary>
            public float OldestInView;
        }

        /// <summary>How often a zombie in view within <see cref="NearDistance"/> of the eye is checked, in seconds.</summary>
        public const float NearInterval = 1f / 20f;
        /// <summary>How often a zombie in view <see cref="FarDistance"/> or more from the eye is checked, in seconds.</summary>
        public const float FarInterval = 1f / 8f;
        /// <summary>Between these distances from the eye the check interval eases from near to far.</summary>
        public const float NearDistance = 6f, FarDistance = 30f;
        /// <summary>A zombie that has moved this far since its last check is due another, however recent that was.</summary>
        public const float MoveForCheck = 0.5f;
        /// <summary>Most due checks run in one frame, the most overdue first. Checks held to <see cref="MaxCheckAge"/> come
        /// on top.</summary>
        public const int MaxChecksPerFrame = 32;
        /// <summary>No zombie in view goes longer than this between checks, however many there are (frame rate allowing).</summary>
        public const float MaxCheckAge = 0.2f;
        /// <summary>Most checks held to <see cref="MaxCheckAge"/> in one frame, so catching up after a hitch is spread out.</summary>
        public const int MaxForcedChecksPerFrame = 256;
        /// <summary>A zombie must stay unseen this long before it is hidden.</summary>
        public const float HideDelay = 0.2f;
        /// <summary>The profiler sample <see cref="TickAll"/> runs under.</summary>
        public const string ProfilerName = "ZombieVisibility.TickAll";

        // Checks held to MaxCheckAge allow for the next frame running this much longer than this one, so a frame-time
        // wobble (or float rounding on a steady one) doesn't carry a zombie past it.
        private const float NextFrameAllowance = 1.25f;

        private const float ChestHeight = 1.2f;
        private const float HeadHeight = 1.62f;
        private const float CrawlerChestHeight = 0.35f;
        private const float CrawlerHeadHeight = 0.55f;
        private const float CorpseHeight = 0.3f;

        // Solid things spawned after the world was built join the Sight Occluder layer in a sweep round the eye
        // (SightOccluders.Sweep): this often, or as soon as the eye has moved this far; out to this beyond the furthest
        // the survivor can see (more than the move, so nothing the last sweep left outside can be in sight before the
        // next); at most this many colliders a sweep (a full one sweeps again the next frame).
        private const float SweepSeconds = 1f;
        private const float SweepMove = 8f;
        private const float SweepMargin = 10f;
        private const int SweepMaxClaims = 128;

        // The cheap part of the rule: out of view, in view (the line of sight decides), or in view only if lit.
        private enum View : byte { Out, In, IfLit }

        private static readonly List<ZombieVisibility> s_All = new List<ZombieVisibility>(64);
        private static readonly List<ZombieVisibility> s_Due = new List<ZombieVisibility>(64);
        private static readonly MostOverdueFirst s_MostOverdueFirst = new MostOverdueFirst();
        private static readonly ProfilerMarker s_TickMarker = new ProfilerMarker(ProfilerName);
        private static Sight s_Sight;
        private static bool s_HasSight;
        private static int s_Rays;
        private static float s_SweepTimer;
        private static Vector3 s_SweptFrom;

        /// <summary>Every live zombie's visibility (iterate with a for loop to stay allocation free).</summary>
        public static IReadOnlyList<ZombieVisibility> All => s_All;
        /// <summary>True while a vision rig is deciding who is seen (otherwise everyone is shown).</summary>
        public static bool HasSight => s_HasSight;
        /// <summary>The survivor's sight as of the last <see cref="TickAll"/>.</summary>
        public static Sight CurrentSight => s_Sight;
        /// <summary>What the last <see cref="TickAll"/> did.</summary>
        public static TickStats LastTick { get; private set; }
        /// <summary>The longest any zombie in view went between checks since <see cref="ResetWorstGap"/>, in seconds
        /// (measured as it's checked or leaves the view).</summary>
        public static float WorstGap { get; private set; }

        /// <summary>True while this zombie's body is drawn.</summary>
        public bool IsShown { get; private set; } = true;
        /// <summary>Time.time when the survivor last saw this zombie.</summary>
        public float LastSeenTime { get; private set; } = -999f;

        private Renderer[] _renderers;
        private int _hierarchyCount = -1;
        private ZombieAI _ai;
        private Transform _transform;
        // This frame's verdict from the cheap part of the rule, and where the survivor looks for the zombie.
        private View _view;
        private Vector3 _root, _chest, _head;
        private bool _inView;
        private float _urgency;
        // When it was last settled (its line of sight checked, or found out of view), and where it stood then.
        private float _settledAt = float.NegativeInfinity;
        private Vector3 _settledRoot;
        // Which point was clear when it was last seen (1 the chest, 2 the head, 0 neither): tried first next time.
        private byte _clearPoint;

        private void Awake()
        {
            _transform = transform;
            _ai = GetComponent<ZombieAI>();
            // The ZombieAvatar (added before this component) has already built the body in its Awake.
            CacheRenderers();
        }

        private void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
            Forget();
            // Decide straight away, so a zombie spawned out of sight never shows for a frame.
            if (s_HasSight) Evaluate(Time.time);
            else SetShown(true);
        }

        private void Start()
        {
            // Anything added to the body since Awake (gear, a crawler's type change) gets picked up here.
            if (_transform.hierarchyCount != _hierarchyCount) CacheRenderers();
            if (s_HasSight) Evaluate(Time.time);
        }

        private void OnDisable()
        {
            int i = s_All.IndexOf(this);
            if (i < 0) return;
            int last = s_All.Count - 1;
            s_All[i] = s_All[last];
            s_All.RemoveAt(last);
        }

        /// <summary>Runs this frame's visibility checks (called by the vision controller).</summary>
        public static void TickAll(in Sight sight, float dt)
        {
            using (s_TickMarker.Auto())
            {
                s_Sight = sight;
                s_HasSight = true;
                dt = Mathf.Max(0f, dt);
                float now = Time.time;
                s_Rays = 0;
                s_Due.Clear();
                SweepOccluders(in sight, dt);

                var stats = new TickStats { Zombies = s_All.Count };
                for (int i = 0; i < s_All.Count; i++)
                {
                    ZombieVisibility z = s_All[i];
                    if (z == null || !z.Prepare(in sight, now)) continue;
                    stats.InView++;
                    // Waiting for the next frame could leave it MaxCheckAge unchecked: it goes now, whatever is queued.
                    if (now - z._settledAt + dt * NextFrameAllowance >= MaxCheckAge && stats.Forced < MaxForcedChecksPerFrame)
                    {
                        z.Check(in sight, now);
                        stats.Forced++;
                    }
                    else if (z._urgency >= 1f) s_Due.Add(z);
                }

                // Then the due ones, most overdue first: those just come into view, then the near and the moving.
                int due = Mathf.Min(s_Due.Count, MaxChecksPerFrame);
                if (s_Due.Count > due) s_Due.Sort(s_MostOverdueFirst);
                for (int k = 0; k < due; k++) s_Due[k].Check(in sight, now);
                s_Due.Clear();

                for (int i = 0; i < s_All.Count; i++)
                {
                    ZombieVisibility z = s_All[i];
                    if (z != null && z._inView && now - z._settledAt > stats.OldestInView) stats.OldestInView = now - z._settledAt;
                }
                stats.Checks = stats.Forced + due;
                stats.Rays = s_Rays;
                LastTick = stats;
            }
        }

        /// <summary>Stops deciding visibility and shows every zombie (the vision rig went away).</summary>
        public static void ReleaseAll()
        {
            s_HasSight = false;
            for (int i = 0; i < s_All.Count; i++)
            {
                ZombieVisibility z = s_All[i];
                if (z == null) continue;
                z.Forget();
                z.SetShown(true);
            }
        }

        /// <summary>Starts a new <see cref="WorstGap"/> measurement.</summary>
        public static void ResetWorstGap() => WorstGap = 0f;

        /// <summary>
        /// The shared sight rule: is <paramref name="target"/> (or, failing that,
        /// <paramref name="alternate"/>, e.g. the head) visible to the survivor? Distance and vision cone
        /// are judged on <paramref name="target"/>; the line of sight must be clear to either point.
        /// </summary>
        public static bool CanSee(in Sight sight, Vector3 target, Vector3 alternate) =>
            CanSee(in sight, target, alternate, CloneZConfig.SightOccluderMask);

        /// <summary>The sight rule with the line of sight cast against <paramref name="mask"/> (the smoke test holds the
        /// Sight Occluder layer up against the whole world).</summary>
        internal static bool CanSee(in Sight sight, Vector3 target, Vector3 alternate, int mask)
        {
            View view = InView(in sight, target, out _);
            if (view == View.Out || (view == View.IfLit && !Lit(in sight, target))) return false;
            if (!Physics.Linecast(sight.Eye, target, mask, QueryTriggerInteraction.Ignore)) return true;
            return alternate != target &&
                   !Physics.Linecast(sight.Eye, alternate, mask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Where the survivor looks for this zombie: its chest and head (lower for a crawler; one point just off
        /// the ground for a corpse).</summary>
        internal void SightPoints(out Vector3 chest, out Vector3 head) => PointsAt(_transform.position, out chest, out head);

        /// <summary>True when <paramref name="target"/> is in the cone but past the sight distance, so only a lamp or the
        /// flashlight lets the survivor see it there.</summary>
        internal static bool SeenOnlyIfLit(in Sight sight, Vector3 target) => InView(in sight, target, out _) == View.IfLit;

        // The cheap part of the rule: arm's reach, the vision cone, and how far the light lets the survivor see.
        private static View InView(in Sight sight, Vector3 target, out float distSqr)
        {
            Vector3 to = target - sight.Eye;
            distSqr = to.sqrMagnitude;
            float flatSqr = to.x * to.x + to.z * to.z;
            float aware = sight.AwarenessRadius;
            // Within arm's reach it's noticed whichever way the survivor faces.
            if (flatSqr <= aware * aware) return View.In;
            // Further out it must be inside the vision cone...
            float cos = (to.x * sight.Facing.x + to.z * sight.Facing.z) / Mathf.Sqrt(flatSqr);
            if (cos < sight.CosHalfAngle) return View.Out;
            // ...and close enough to see in this light; a lit zombie can be seen from further.
            float reach = sight.SightDistance;
            if (distSqr <= reach * reach) return View.In;
            float litReach = Mathf.Max(reach, sight.LitSightDistance);
            return distSqr <= litReach * litReach ? View.IfLit : View.Out;
        }

        // In the flashlight beam or a lit room.
        private static bool Lit(in Sight sight, Vector3 point) => InBeam(in sight, point) || RoomLight.IsPointLit(point);

        private static bool InBeam(in Sight sight, Vector3 point)
        {
            if (!sight.BeamOn) return false;
            Vector3 to = point - sight.BeamOrigin;
            float d = to.magnitude;
            if (d > sight.BeamRange) return false;
            if (d < 1e-3f) return true;
            return Vector3.Dot(to / d, sight.BeamDirection) >= sight.BeamCosHalfAngle;
        }

        // Nothing solid between the eye and the point.
        private static bool Clear(in Sight sight, Vector3 point)
        {
            s_Rays++;
            return !Physics.Linecast(sight.Eye, point, CloneZConfig.SightOccluderMask, QueryTriggerInteraction.Ignore);
        }

        // The cheap part of the rule, for every zombie every frame. Out of view settles it (unseen) without a ray; in view
        // it gets how overdue its line-of-sight check is. True when it's in view.
        private bool Prepare(in Sight sight, float now)
        {
            _root = _transform.position;
            PointsAt(_root, out _chest, out _head);
            _view = InView(in sight, _chest, out float distSqr);
            bool wasInView = _inView;
            _inView = _view != View.Out;
            if (!_inView)
            {
                if (wasInView) NoteGap(now);
                Settle(now);
                _clearPoint = 0;
                Unseen(now);
                return false;
            }
            // Just come into view, it may need showing: first in the queue.
            if (!wasInView) _urgency = float.MaxValue;
            else
            {
                float interval = Mathf.Lerp(NearInterval, FarInterval, Mathf.InverseLerp(NearDistance, FarDistance, Mathf.Sqrt(distSqr)));
                _urgency = (now - _settledAt) / interval + Vector3.Distance(_root, _settledRoot) / MoveForCheck;
            }
            return true;
        }

        // The rest of the rule for a zombie in view (after this frame's Prepare): the light, then the line of sight.
        private void Check(in Sight sight, float now)
        {
            NoteGap(now);
            Settle(now);
            if ((_view == View.IfLit && !Lit(in sight, _chest)) || !ClearLine(in sight))
            {
                _clearPoint = 0;
                Unseen(now);
                return;
            }
            LastSeenTime = now;
            SetShown(true);
        }

        // The whole rule at once, off the frame's schedule (a zombie just spawned, or just finished).
        private void Evaluate(float now)
        {
            if (Prepare(in s_Sight, now)) Check(in s_Sight, now);
        }

        // The line of sight to the chest or the head. The point that was clear last time goes first, so a zombie in plain
        // view costs one ray.
        private bool ClearLine(in Sight sight)
        {
            bool headFirst = _clearPoint == 2;
            Vector3 first = headFirst ? _head : _chest;
            Vector3 second = headFirst ? _chest : _head;
            if (Clear(in sight, first))
            {
                _clearPoint = headFirst ? (byte)2 : (byte)1;
                return true;
            }
            if (second == first || !Clear(in sight, second)) return false;
            _clearPoint = headFirst ? (byte)1 : (byte)2;
            return true;
        }

        private void PointsAt(Vector3 root, out Vector3 chest, out Vector3 head)
        {
            // A dead body lies low: one point just off the ground.
            if (_ai != null && _ai.IsDeadState)
            {
                chest = head = root + Vector3.up * CorpseHeight;
            }
            // A crawler is down on the ground.
            else if (_ai != null && _ai.Type == ZombieType.Crawler)
            {
                chest = root + Vector3.up * CrawlerChestHeight;
                head = root + Vector3.up * CrawlerHeadHeight;
            }
            else
            {
                chest = root + Vector3.up * ChestHeight;
                head = root + Vector3.up * HeadHeight;
            }
        }

        // Hidden once it has gone unseen for HideDelay.
        private void Unseen(float now)
        {
            if (now - LastSeenTime >= HideDelay) SetShown(false);
        }

        private void Settle(float now)
        {
            _settledAt = now;
            _settledRoot = _root;
        }

        // How long it went unchecked in view, now that it's checked or has left the view.
        private void NoteGap(float now)
        {
            float gap = now - _settledAt;
            if (gap > WorstGap && gap < float.PositiveInfinity) WorstGap = gap;
        }

        // Back to knowing nothing: it's checked as soon as it's in view.
        private void Forget()
        {
            _inView = false;
            _settledAt = float.NegativeInfinity;
            _clearPoint = 0;
        }

        private static void SweepOccluders(in Sight sight, float dt)
        {
            s_SweepTimer -= dt;
            if (s_SweepTimer > 0f && (sight.Eye - s_SweptFrom).sqrMagnitude < SweepMove * SweepMove) return;
            float reach = Mathf.Max(Mathf.Max(sight.SightDistance, sight.LitSightDistance), sight.AwarenessRadius);
            int moved = SightOccluders.Sweep(sight.Eye, reach + SweepMargin, SweepMaxClaims);
            s_SweepTimer = moved >= SweepMaxClaims ? 0f : SweepSeconds;
            s_SweptFrom = sight.Eye;
        }

        private void SetShown(bool shown)
        {
            // A body part added since (gear, a swapped mesh) gets picked up on the next change.
            if (_transform.hierarchyCount != _hierarchyCount) CacheRenderers();
            if (shown == IsShown) return;
            IsShown = shown;
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r != null) r.forceRenderingOff = !shown;
            }
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _hierarchyCount = _transform.hierarchyCount;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].forceRenderingOff = !IsShown;
        }

        // Enter Play Mode keeps statics from one play session to the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetLastPlaySession()
        {
            s_All.Clear();
            s_Due.Clear();
            s_HasSight = false;
            s_SweepTimer = 0f;
            LastTick = default;
            WorstGap = 0f;
        }

        private sealed class MostOverdueFirst : IComparer<ZombieVisibility>
        {
            public int Compare(ZombieVisibility a, ZombieVisibility b) => b._urgency.CompareTo(a._urgency);
        }
    }
}
