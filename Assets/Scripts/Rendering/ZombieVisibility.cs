using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Added to every zombie by the spawner; hides the zombie's body while the survivor can't see it,
    /// Project Zomboid style. A zombie is seen when it is within sight distance (by day ~30 m; at night
    /// the reach of the survivor's vision light, further with night vision or when the zombie stands in
    /// the flashlight beam or a lit room), inside the survivor's vision cone or right beside them, and
    /// nothing solid lies between the survivor's eye and the zombie.
    ///
    /// The checks don't run per zombie: the <see cref="VisionController"/> calls <see cref="TickAll"/>
    /// once a frame and it works through the static registry round-robin, about ten checks per zombie
    /// per second with a hard cap per frame. Hiding waits <see cref="HideDelay"/> so a zombie on the edge
    /// of view doesn't flicker; showing is immediate. With no vision rig (or no survivor) every zombie
    /// is shown.
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

        /// <summary>Visibility checks per zombie per second.</summary>
        public const float CheckRateHz = 10f;
        /// <summary>Most visibility checks run in one frame, however many zombies there are.</summary>
        public const int MaxChecksPerFrame = 24;
        /// <summary>A zombie must stay unseen this long before it is hidden.</summary>
        public const float HideDelay = 0.2f;

        private const float ChestHeight = 1.2f;
        private const float HeadHeight = 1.62f;
        private const float CrawlerChestHeight = 0.35f;
        private const float CrawlerHeadHeight = 0.55f;
        private const float CorpseHeight = 0.3f;

        private static readonly int LineOfSightMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);

        private static readonly List<ZombieVisibility> s_All = new List<ZombieVisibility>(64);
        private static Sight s_Sight;
        private static bool s_HasSight;
        private static int s_Cursor;
        private static float s_Budget;

        /// <summary>Every live zombie's visibility (iterate with a for loop to stay allocation free).</summary>
        public static IReadOnlyList<ZombieVisibility> All => s_All;
        /// <summary>True while a vision rig is deciding who is seen (otherwise everyone is shown).</summary>
        public static bool HasSight => s_HasSight;
        /// <summary>The survivor's sight as of the last <see cref="TickAll"/>.</summary>
        public static Sight CurrentSight => s_Sight;

        /// <summary>True while this zombie's body is drawn.</summary>
        public bool IsShown { get; private set; } = true;
        /// <summary>Time.time when the survivor last saw this zombie.</summary>
        public float LastSeenTime { get; private set; } = -999f;

        private Renderer[] _renderers;
        private int _hierarchyCount = -1;
        private ZombieAI _ai;

        private void Awake()
        {
            _ai = GetComponent<ZombieAI>();
            // The ZombieAvatar (added before this component) has already built the body in its Awake.
            CacheRenderers();
        }

        private void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
            // Decide straight away, so a zombie spawned out of sight never shows for a frame.
            if (s_HasSight) Evaluate(in s_Sight, Time.time);
            else SetShown(true);
        }

        private void Start()
        {
            // Anything added to the body since Awake (gear, a crawler's type change) gets picked up here.
            if (transform.hierarchyCount != _hierarchyCount) CacheRenderers();
            if (s_HasSight) Evaluate(in s_Sight, Time.time);
        }

        private void OnDisable()
        {
            int i = s_All.IndexOf(this);
            if (i < 0) return;
            s_All.RemoveAt(i);
            if (s_Cursor > i) s_Cursor--;
        }

        /// <summary>Runs this frame's share of visibility checks (called by the vision controller).</summary>
        public static void TickAll(in Sight sight, float dt)
        {
            s_Sight = sight;
            s_HasSight = true;
            int n = s_All.Count;
            if (n == 0) { s_Budget = 0f; return; }

            s_Budget = Mathf.Min(s_Budget + n * CheckRateHz * Mathf.Max(0f, dt), n);
            int todo = Mathf.Min((int)s_Budget, MaxChecksPerFrame);
            s_Budget -= todo;
            float now = Time.time;
            for (int k = 0; k < todo && s_All.Count > 0; k++)
            {
                if (s_Cursor >= s_All.Count) s_Cursor = 0;
                ZombieVisibility z = s_All[s_Cursor++];
                if (z != null) z.Evaluate(in sight, now);
            }
        }

        /// <summary>Stops deciding visibility and shows every zombie (the vision rig went away).</summary>
        public static void ReleaseAll()
        {
            s_HasSight = false;
            for (int i = 0; i < s_All.Count; i++)
                if (s_All[i] != null) s_All[i].SetShown(true);
        }

        /// <summary>
        /// The shared sight rule: is <paramref name="target"/> (or, failing that,
        /// <paramref name="alternate"/>, e.g. the head) visible to the survivor? Distance and vision cone
        /// are judged on <paramref name="target"/>; the line of sight must be clear to either point.
        /// </summary>
        public static bool CanSee(in Sight sight, Vector3 target, Vector3 alternate)
        {
            Vector3 to = target - sight.Eye;
            float flatSqr = to.x * to.x + to.z * to.z;
            float aware = sight.AwarenessRadius;
            if (flatSqr > aware * aware)
            {
                // Outside arm's reach: it must be inside the vision cone...
                float flat = Mathf.Sqrt(flatSqr);
                float cos = (to.x * sight.Facing.x + to.z * sight.Facing.z) / flat;
                if (cos < sight.CosHalfAngle) return false;

                // ...and close enough to see in this light; a lit zombie can be seen from further.
                float distSqr = to.sqrMagnitude;
                float reach = sight.SightDistance;
                if (distSqr > reach * reach)
                {
                    bool lit = InBeam(in sight, target) || RoomLight.IsPointLit(target);
                    float litReach = Mathf.Max(reach, sight.LitSightDistance);
                    if (!lit || distSqr > litReach * litReach) return false;
                }
            }

            if (!Physics.Linecast(sight.Eye, target, LineOfSightMask, QueryTriggerInteraction.Ignore)) return true;
            return alternate != target &&
                   !Physics.Linecast(sight.Eye, alternate, LineOfSightMask, QueryTriggerInteraction.Ignore);
        }

        private static bool InBeam(in Sight sight, Vector3 point)
        {
            if (!sight.BeamOn) return false;
            Vector3 to = point - sight.BeamOrigin;
            float d = to.magnitude;
            if (d > sight.BeamRange) return false;
            if (d < 1e-3f) return true;
            return Vector3.Dot(to / d, sight.BeamDirection) >= sight.BeamCosHalfAngle;
        }

        private void Evaluate(in Sight sight, float now)
        {
            Vector3 root = transform.position;
            Vector3 chest, head;
            bool dead = _ai != null && _ai.IsDeadState;
            if (dead)
            {
                chest = head = root + Vector3.up * CorpseHeight;
            }
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

            if (CanSee(in sight, chest, head))
            {
                LastSeenTime = now;
                SetShown(true);
            }
            else if (now - LastSeenTime >= HideDelay)
            {
                SetShown(false);
            }
        }

        private void SetShown(bool shown)
        {
            // A body part added since (gear, a swapped mesh) gets picked up on the next change.
            if (transform.hierarchyCount != _hierarchyCount) CacheRenderers();
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
            _hierarchyCount = transform.hierarchyCount;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].forceRenderingOff = !IsShown;
        }
    }
}
