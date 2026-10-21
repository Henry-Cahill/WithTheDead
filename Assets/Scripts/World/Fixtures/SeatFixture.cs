using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Somewhere to sit: a dining chair or the armchair. [E] sits the survivor down (<see cref="PlayerSeat.TrySit"/>) —
    /// the hips on the seat, facing the way the seat faces — and [E] again gets them up; so does any movement key.
    /// Sitting rests the survivor: endurance comes back faster and, the comfier the seat, the more fatigue and nerves
    /// ease. In the armchair they can sink right back. Seats keep a registry so the dining table can offer "Sit at the
    /// table" on a chair pulled up to it. Nothing about a seat is saved.
    /// </summary>
    [DisallowMultipleComponent]
    public class SeatFixture : Interactable
    {
        private static readonly List<SeatFixture> s_Seats = new List<SeatFixture>();

        private Transform _furniture;
        private string _name, _sitLabel, _standLabel, _loungeLabel;
        private Vector3 _seatLocal;   // on the furniture, in its own frame (front +Z)
        private float _yaw, _comfort;
        private bool _lounge;         // the armchair: can be sunk back into

        /// <summary>Every seat in the world (for the dining table's "Sit at the table").</summary>
        public static IReadOnlyList<SeatFixture> Seats => s_Seats;

        public override string DisplayName => _name;
        public override Transform OcclusionRoot => _furniture != null ? _furniture : transform;

        /// <summary>Where the hips rest: a point on the seat's surface (world).</summary>
        public Vector3 SeatPoint => Frame(_seatLocal);

        /// <summary>The way a sitter faces (degrees).</summary>
        public float SeatYaw => _yaw;

        /// <summary>The front edge of the seat, a hand's height above it — where the survivor turns to sit.</summary>
        public override Vector3 InteractPoint => Frame(_seatLocal + new Vector3(0f, 0.14f, 0.2f));

        /// <summary>A seat on <paramref name="furniture"/> (its root, unrotated: the piece is drawn turned by
        /// <paramref name="yaw"/>), the hips resting at <paramref name="seatLocal"/> in the piece's own frame. A
        /// <paramref name="lounge"/> seat can be sunk back into — "<paramref name="loungeLabel"/>" (the armchair's, by default).</summary>
        internal static SeatFixture Create(Transform furniture, string displayName, Vector3 seatLocal, float yaw, float comfort,
            string sitLabel, string standLabel, bool lounge, string loungeLabel = null)
        {
            var go = new GameObject("Seat_" + displayName);
            go.transform.SetParent(furniture, false);
            var s = go.AddComponent<SeatFixture>();
            s._furniture = furniture;
            s._name = displayName;
            s._seatLocal = seatLocal;
            s._yaw = yaw;
            s._comfort = Mathf.Clamp01(comfort);
            s._sitLabel = sitLabel;
            s._standLabel = standLabel;
            s._lounge = lounge;
            s._loungeLabel = loungeLabel ?? "Sink back into the armchair";
            go.transform.SetPositionAndRotation(s.SeatPoint, Quaternion.Euler(0f, yaw, 0f));
            return s;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!s_Seats.Contains(this)) s_Seats.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            s_Seats.Remove(this);
        }

        private Vector3 Frame(Vector3 local)
        {
            Vector3 origin = _furniture != null ? _furniture.position : transform.position;
            return origin + Quaternion.Euler(0f, _yaw, 0f) * local;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerSeat seat = ctx.Seat;
            if (seat == null) return;
            if (seat.IsSittingOn(this))
            {
                into.Add(new InteractAction(_standLabel, c => c.Seat.Stand()));
                into.Add(new InteractAction("Stand up slowly", c => { c.Seat?.Stand(); c.Notify("You rise slowly, stretching out a bit before standing."); }));
                if (_lounge)
                {
                    bool sunk = seat.Comfort > _comfort + 0.01f;
                    into.Add(new InteractAction(sunk ? "Sit up straight" : _loungeLabel, c => Settle(c, !sunk)));
                }
                return;
            }
            into.Add(new InteractAction(_sitLabel, SitDown));
            into.Add(new InteractAction("Inspect seat", c => c.Notify($"This seat feels {(int)(_comfort * 100)}% comfortable.")));
        }

        /// <summary>Sits the survivor down here (the dining table's "Sit at the table" comes here too).</summary>
        public void SitDown(InteractContext ctx)
        {
            if (ctx.Seat == null) return;
            if (!ctx.Seat.TrySit(this, SeatPoint, _yaw, _comfort, out string reason)) ctx.Notify(reason);
        }

        private void Settle(InteractContext ctx, bool sinkBack)
        {
            float comfort = sinkBack ? CloneZConfig.ArmchairFeetUpComfort : _comfort;
            if (ctx.Seat != null && ctx.Seat.TrySit(this, SeatPoint, _yaw, comfort, out _) && sinkBack)
                ctx.Notify("You sink back into the cushions and let your shoulders drop.");
        }

        /// <summary>The seat whose seat is nearest <paramref name="from"/> among those within <paramref name="range"/>
        /// (flat) of <paramref name="around"/> — the chairs at a table — or null.</summary>
        public static SeatFixture NearestAround(Vector3 around, float range, Vector3 from)
        {
            SeatFixture best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < s_Seats.Count; i++)
            {
                SeatFixture s = s_Seats[i];
                if (s == null || !s.IsAvailable || s._lounge) continue;
                Vector3 p = s.SeatPoint;
                if (Flat(p - around).sqrMagnitude > range * range) continue;
                float d = Flat(p - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = s; }
            }
            return best;
        }

        /// <summary>True when <paramref name="seat"/> is one of the seats within <paramref name="range"/> of <paramref name="around"/>.</summary>
        public static bool IsAround(Interactable seat, Vector3 around, float range) =>
            seat is SeatFixture s && s != null && Flat(s.SeatPoint - around).sqrMagnitude <= range * range;

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
