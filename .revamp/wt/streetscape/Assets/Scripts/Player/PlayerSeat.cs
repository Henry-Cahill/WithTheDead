using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Sitting down — on a dining chair, the armchair, the edge of the bed, the toilet lid — and lying down on the bed
    /// to rest. While seated the survivor rests: endurance comes back faster and, on something comfortable, fatigue and
    /// nerves ease a little. Any movement key or Space stands them up again (<see cref="PlayerController"/>), as does
    /// being hurt, swinging a weapon, falling asleep, setting to build or garden, or being moved by something else.
    ///
    /// Fixtures call <see cref="TrySit"/> with where the hips rest (a point on the seat's surface) and which way to
    /// face. Seated, the survivor's root sits right over the seat with the CharacterController switched off — a chair's
    /// collider would otherwise shove them off it — and standing up puts them on a free spot beside the seat (in front
    /// of it if there's room, else where they stood before) with the controller back on. This component owns the
    /// seated state that <see cref="PlayerController"/> (no walking or turning while seated) and the avatar's sitting
    /// pose read — the avatar eases the figure over each time <see cref="Version"/> moves on — and lies the figure on
    /// the bed while <see cref="IsLyingDown"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerSeat : MonoBehaviour
    {
        /// <summary>True while the survivor sits.</summary>
        public bool IsSeated { get; protected set; }

        /// <summary>The fixture being sat on (null when standing).</summary>
        public Interactable CurrentSeat { get; protected set; }

        /// <summary>World point the hips rest on (on the seat's surface), and the facing (degrees) while seated.</summary>
        public Vector3 SeatPoint { get; protected set; }
        public float SeatYaw { get; protected set; }

        /// <summary>0..1: a kitchen chair ~0.3, the armchair ~0.8 — how restful the seat is.</summary>
        public float Comfort { get; protected set; }

        /// <summary>Where the survivor stood (their feet) and which way they faced just before sitting down.</summary>
        public Vector3 StoodFrom { get; protected set; }
        public float StoodYaw { get; protected set; }

        /// <summary>Moves on whenever the survivor sits down, settles differently or stands up (the avatar re-poses).</summary>
        public int Version { get; protected set; }

        /// <summary>True while lying on a bed resting, awake (see <see cref="LieDown"/>).</summary>
        public bool IsLyingDown { get; protected set; }

        /// <summary>The bed being lain on (its root: head toward -Z), or null.</summary>
        public Transform RestBed { get; protected set; }

        private static readonly int SolidMask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
        private static readonly float[] FrontSteps = { 0.6f, 0.8f, 1.0f };
        private static readonly float[] AroundAngles = { 45f, -45f, 90f, -90f, 135f, -135f, 180f };
        private static readonly float[] AroundSteps = { 0.7f, 0.95f };

        private PlayerStats _stats;
        private CharacterController _body;
        private PlayerSleep _sleep;
        private PlayerInteractor _interactor;
        private PlayerBuilder _builder;
        private PlayerFarming _farming;
        private PlayerCombat _combat;
        private bool _subscribed;
        private Vector3 _heldAt;   // where the root was set down over the seat

        private PlayerStats Stats => _stats != null ? _stats : (_stats = GetComponent<PlayerStats>());
        private CharacterController Body => _body != null ? _body : (_body = GetComponent<CharacterController>());
        private PlayerSleep Sleep => _sleep != null ? _sleep : (_sleep = GetComponent<PlayerSleep>());
        private PlayerInteractor Interactor => _interactor != null ? _interactor : (_interactor = GetComponent<PlayerInteractor>());
        private PlayerBuilder Builder => _builder != null ? _builder : (_builder = GetComponent<PlayerBuilder>());
        private PlayerFarming Farming => _farming != null ? _farming : (_farming = GetComponent<PlayerFarming>());

        private void OnEnable() => Subscribe();
        private void Start() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            if (IsSeated) Release();
            IsLyingDown = false;
            RestBed = null;
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            if (Stats == null) return;
            if (_combat == null) _combat = GetComponent<PlayerCombat>();
            _subscribed = true;
            Stats.OnDamaged += StandUpStartled;
            if (_combat != null)
            {
                _combat.OnSwing += StandUpStartled;
                _combat.OnShove += StandUpStartled;
            }
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            if (_stats != null) _stats.OnDamaged -= StandUpStartled;
            if (_combat != null)
            {
                _combat.OnSwing -= StandUpStartled;
                _combat.OnShove -= StandUpStartled;
            }
        }

        // A blow, a swing or a shove: nobody stays sitting (or lying back) through that.
        private void StandUpStartled() => GetUpFromInput();

        /// <summary>True while the survivor sits on <paramref name="seat"/>.</summary>
        public bool IsSittingOn(Interactable seat) => IsSeated && seat != null && CurrentSeat == seat;

        /// <summary>A movement key or Space (see <see cref="PlayerController"/>): up off the seat, or up off the bed,
        /// ending the rest.</summary>
        public void GetUpFromInput()
        {
            if (IsSeated) Stand();
            if (!IsLyingDown) return;
            if (Interactor != null && Interactor.IsWorking) Interactor.StopJob();

            // Move the survivor to a clear spot in front of the bed before clearing the lying‑down flags
            if (RestBed != null)
            {
                // Find a safe stand‑up spot near the bed (tries front, then around the bed).
                Vector3 spot = FindBedStandSpot(RestBed);
                // If no spot found, fall back to the survivor's current position.
                if (spot == Vector3.zero) spot = transform.position;
                // Re‑enable the character controller before moving so physics does not block the teleport.
                if (Body != null && !Body.enabled) Body.enabled = true;
                transform.SetPositionAndRotation(spot, Quaternion.Euler(0f, RestBed.eulerAngles.y, 0f));
            }

            GetUp();
        }

        #region Sitting
        /// <summary>Sits the survivor on <paramref name="seat"/>: hips at <paramref name="seatPoint"/>, facing
        /// <paramref name="yaw"/> degrees. Returns false (with a reason for the HUD) when they can't sit now. Sitting
        /// again on the seat already sat on just settles differently (the armchair's feet up, say).</summary>
        public virtual bool TrySit(Interactable seat, Vector3 seatPoint, float yaw, float comfort, out string reason)
        {
            reason = null;
            if (seat == null) { reason = "There's nothing to sit on"; return false; }
            if (Stats != null && Stats.IsDead) { reason = "You can't do that now"; return false; }
            if (Sleep != null && Sleep.IsSleeping) { reason = "You're asleep"; return false; }

            if (IsSeated && CurrentSeat == seat && FlatDistance(SeatPoint, seatPoint) < 0.05f)
            {
                SeatYaw = yaw;
                Comfort = Mathf.Clamp01(comfort);
                Version++;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                return true;
            }
            if (FlatDistance(transform.position, seatPoint) > CloneZConfig.SeatMaxReach) { reason = "That's too far away"; return false; }

            if (IsLyingDown)
            {
                // Still resting on the bed: that job ends here.
                if (Interactor != null && Interactor.IsWorking) Interactor.StopJob();
                GetUp();
            }
            if (IsSeated) Stand();

            StoodFrom = transform.position;
            StoodYaw = transform.eulerAngles.y;
            CurrentSeat = seat;
            SeatPoint = seatPoint;
            SeatYaw = yaw;
            Comfort = Mathf.Clamp01(comfort);
            IsSeated = true;
            Version++;

            // Over the seat, out of the way of its collider: the body's capsule is off until they stand.
            if (Body != null) Body.enabled = false;
            _heldAt = new Vector3(seatPoint.x, transform.position.y, seatPoint.z);
            transform.SetPositionAndRotation(_heldAt, Quaternion.Euler(0f, yaw, 0f));
            return true;
        }

        /// <summary>Gets up again, onto a free spot beside the seat.</summary>
        public virtual void Stand()
        {
            if (!IsSeated) return;
            Vector3 spot = FindStandSpot();
            transform.SetPositionAndRotation(spot, Quaternion.Euler(0f, SeatYaw, 0f));
            Release();
        }

        // Ends the sitting wherever the survivor is now: the body's capsule back on, the state cleared.
        private void Release()
        {
            IsSeated = false;
            CurrentSeat = null;
            Version++;
            if (Body != null && !Body.enabled) Body.enabled = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (IsLyingDown) TickLying(dt);
            if (!IsSeated) return;

            // Moved off the seat by something else (a fixture stepping the survivor somewhere): already up.
            Vector3 off = transform.position - _heldAt;
            off.y = 0f;
            if (off.sqrMagnitude > 0.04f)
            {
                Release();
                return;
            }

            bool dead = Stats != null && Stats.IsDead;
            if (dead || (Sleep != null && Sleep.IsSleeping) || UIState.BuildMode
                || (Builder != null && Builder.IsBuilding) || (Farming != null && Farming.IsBusy)
                || CurrentSeat == null || !CurrentSeat.isActiveAndEnabled)
            {
                Stand();
                return;
            }

            if (Stats == null || dt <= 0f) return;
            float c = Comfort;
            Stats.Endurance = Mathf.Clamp01(Stats.Endurance + CloneZConfig.EnduranceRegen * (CloneZConfig.SeatEnduranceBonus + c) * dt);
            Stats.Fatigue = Mathf.Max(0f, Stats.Fatigue - CloneZConfig.SeatFatigueEasePerSec * c * dt);
            Stats.Panic = Mathf.Max(0f, Stats.Panic - CloneZConfig.SeatPanicEasePerSec * (0.5f + c) * dt);
        }

        // In front of the seat if there's room; else where the survivor stood before sitting; else anywhere round it.
        private Vector3 FindStandSpot()
        {
            Vector3 seat = new Vector3(SeatPoint.x, transform.position.y, SeatPoint.z);
            Vector3 front = Quaternion.Euler(0f, SeatYaw, 0f) * Vector3.forward;
            for (int i = 0; i < FrontSteps.Length; i++)
            {
                Vector3 p = seat + front * FrontSteps[i];
                if (CanStandAt(p, seat)) return p;
            }
            if (FlatDistance(StoodFrom, seat) < CloneZConfig.SeatMaxReach + 0.5f && CanStandAt(StoodFrom, seat)) return StoodFrom;
            for (int a = 0; a < AroundAngles.Length; a++)
            {
                Vector3 dir = Quaternion.Euler(0f, SeatYaw + AroundAngles[a], 0f) * Vector3.forward;
                for (int s = 0; s < AroundSteps.Length; s++)
                {
                    Vector3 p = seat + dir * AroundSteps[s];
                    if (CanStandAt(p, seat)) return p;
                }
            }
return StoodFrom;
}

        // Finds a walkable spot next to the given bed, using the same algorithm as FindStandSpot but
        // anchored to the bed’s centre and forward direction. Returns Vector3.zero if nothing is found.
        private Vector3 FindBedStandSpot(Transform bed)
        {
            Vector3 bedCenter = bed.position;
            Vector3 forward = bed.forward; // bed “head” faces –Z, forward points to the foot side.
            for (int i = 0; i < FrontSteps.Length; i++)
            {
                Vector3 p = bedCenter + forward * FrontSteps[i];
                if (CanStandAt(p, bedCenter)) return p;
            }
            // Try the original standing point the player had before lying down (if still reachable).
            if (CanStandAt(StoodFrom, bedCenter)) return StoodFrom;
            // Search around the bed using the same angle/step tables.
            for (int a = 0; a < AroundAngles.Length; a++)
            {
                Vector3 dir = Quaternion.Euler(0f, AroundAngles[a], 0f) * forward;
                for (int s = 0; s < AroundSteps.Length; s++)
                {
                    Vector3 p = bedCenter + dir * AroundSteps[s];
                    if (CanStandAt(p, bedCenter)) return p;
                }
            }
            return Vector3.zero;
        }

        // Room for the body's capsule, reached from the seat without passing through a wall.
        private bool CanStandAt(Vector3 feet, Vector3 seat)
        {
            float r = CloneZConfig.SeatStandClearance;
            float h = Body != null ? Body.height : 2f;
            Vector3 low = feet + Vector3.up * (r + 0.12f);
            Vector3 high = feet + Vector3.up * Mathf.Max(r + 0.13f, h - r);
            if (Physics.CheckCapsule(low, high, r, SolidMask, QueryTriggerInteraction.Ignore)) return false;
            return !Physics.Linecast(seat + Vector3.up * 1.1f, feet + Vector3.up * 1.1f, SolidMask, QueryTriggerInteraction.Ignore);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
        #endregion

        #region Lying down
        /// <summary>Lies the survivor down on <paramref name="bed"/> to rest, awake (the bed's fixture runs it as an
        /// open-ended job and calls <see cref="GetUp"/> when it ends). Returns false with a reason when they can't.</summary>
        public virtual bool LieDown(Transform bed, out string reason)
        {
            reason = null;
            if (bed == null) { reason = "There's nowhere to lie down"; return false; }
            if (Stats != null && Stats.IsDead) { reason = "You can't do that now"; return false; }
            if (Sleep != null && Sleep.IsSleeping) { reason = "You're asleep"; return false; }
            if (IsSeated) Stand();
            IsLyingDown = true;
            RestBed = bed;
            return true;
        }

        /// <summary>Gets up off the bed.</summary>
        public virtual void GetUp()
        {
            IsLyingDown = false;
            RestBed = null;
        }

        // Resting lying down: endurance back three times as fast, the edge taken off tiredness and fear. It lasts as long
        // as the job at the bed does.
        private void TickLying(float dt)
        {
            bool dead = Stats != null && Stats.IsDead;
            if (dead || RestBed == null || Interactor == null || !Interactor.IsWorking)
            {
                GetUp();
                return;
            }
            if (Stats == null || dt <= 0f) return;
            Stats.Endurance = Mathf.Clamp01(Stats.Endurance + CloneZConfig.EnduranceRegen * CloneZConfig.RestEnduranceBonus * dt);
            Stats.Fatigue = Mathf.Max(0f, Stats.Fatigue - CloneZConfig.RestFatigueEasePerSec * dt);
            Stats.Panic = Mathf.Max(0f, Stats.Panic - CloneZConfig.RestPanicEasePerSec * dt);
        }
        #endregion
    }
}