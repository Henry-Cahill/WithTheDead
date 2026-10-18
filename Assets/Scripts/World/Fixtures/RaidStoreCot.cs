using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The quarantine tent's folding camp cot: "Lie down and rest" stretches out on the canvas, awake (an open-ended job
    /// the way the home's bed does it — endurance comes back fast and the nerves settle, until the survivor moves or
    /// presses [E]); the survivor can also sit on its edge, or look underneath. No sleeping here: the gas is outside the
    /// flap and the dead aren't far behind it.
    /// </summary>
    [DisallowMultipleComponent]
    public class RaidStoreCot : Interactable
    {
        private Transform _body;     // the cot's furniture
        private Transform _rest;     // the bed the survivor lies on: head toward its -Z
        private float _yaw;          // the cot's facing (its length runs along its own X)
        private int _looked;

        private static readonly string[] Underneath =
        {
            "A pair of wet socks, a paperback with its cover torn off and a dog tag on a broken chain. The name's been scratched out.",
            "Dust, cigarette ends and an empty sachet of instant coffee. Somebody slept here more than once.",
            "Nothing under there but the floorboards and the cold coming up through them.",
        };

        public override string DisplayName => "Camp cot";
        public override Transform OcclusionRoot => _body != null ? _body : transform;
        public override float Reach => CloneZConfig.BedReach;

        /// <summary>On the canvas, level with the survivor along the cot, so it's in reach from its whole length.</summary>
        public override Vector3 InteractPoint
        {
            get
            {
                Vector3 c = _body != null ? _body.position : transform.position;
                Vector3 along = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;
                PlayerInteractor pi = PlayerInteractor.Current;
                float t = 0f;
                if (pi != null) t = Mathf.Clamp(Vector3.Dot(pi.transform.position - c, along), -0.75f, 0.75f);
                return c + along * t + Vector3.up * 0.55f;
            }
        }

        /// <summary>The cot's fixture on its body <paramref name="body"/> (drawn turned by <paramref name="yaw"/>, its length
        /// along its own X with the head at -X).</summary>
        internal static RaidStoreCot Create(Transform body, float yaw)
        {
            var go = new GameObject("Fixture_Cot");
            go.transform.SetParent(body, false);
            go.transform.SetPositionAndRotation(body.position, Quaternion.identity);
            var cot = go.AddComponent<RaidStoreCot>();
            cot._body = body;
            cot._yaw = yaw;
            // Lying down, the body's head goes to the rest's -Z: turned a quarter so -Z runs to the cot's head (-X).
            var rest = new GameObject("Rest");
            rest.transform.SetParent(body, false);
            rest.transform.SetPositionAndRotation(body.position + Vector3.up * (CloneZConfig.RaidCotLieHeight - CloneZConfig.RaidHomeMattressHeight),
                Quaternion.Euler(0f, yaw + 90f, 0f));
            cot._rest = rest.transform;
            return cot;
        }

        public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            PlayerSeat seat = ctx.Seat;
            bool onEdge = seat != null && seat.IsSittingOn(this);
            into.Add(RestAction().DisabledIf(seat == null, "You can't lie down now"));
            if (onEdge) into.Add(new InteractAction("Get up off the cot", c => c.Seat.Stand()));
            else into.Add(new InteractAction("Sit on the cot", SitOnEdge).DisabledIf(seat == null, "You can't sit down now"));
            into.Add(new InteractAction("Look under the cot", LookUnder).Timed(1.6f, "Looking under the cot", HumanoidActivity.Search));
        }

        private InteractAction RestAction()
        {
            var a = new InteractAction("Lie down and rest").Timed(-1f, "Resting on the cot", HumanoidActivity.None);
            a.Begin = c =>
            {
                string reason = null;
                if (c.Seat == null || !c.Seat.LieDown(_rest, out reason))
                {
                    c.Notify(reason ?? "You can't lie down now");
                    if (c.Interactor != null) c.Interactor.StopJob();
                    return;
                }
                c.Notify("The canvas creaks and sags under you. Not comfortable, but it's off the floor.");
            };
            a.Stopped = c =>
            {
                if (c.Seat != null) c.Seat.GetUp();
            };
            return a;
        }

        // On whichever side the survivor stands, a little in from the edge, facing out.
        private void SitOnEdge(InteractContext ctx)
        {
            if (ctx.Seat == null || _body == null) return;
            Quaternion q = Quaternion.Euler(0f, _yaw, 0f);
            Vector3 me = Quaternion.Inverse(q) * (ctx.Position - _body.position);
            float side = me.z < 0f ? -1f : 1f;
            var local = new Vector3(Mathf.Clamp(me.x, -0.6f, 0.7f), CloneZConfig.RaidCotSeatHeight, side * 0.26f);
            float yaw = _yaw + (side < 0f ? 180f : 0f);
            if (!ctx.Seat.TrySit(this, _body.position + q * local, yaw, CloneZConfig.RaidCotComfort, out string reason))
                ctx.Notify(reason);
        }

        private void LookUnder(InteractContext ctx)
        {
            ctx.Notify(Underneath[_looked % Underneath.Length]);
            _looked++;
        }
    }
}
