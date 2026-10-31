using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The floors and stairs of one building on the Kentucky map (BuildingPass.Levels): on their collider's object. A building
    /// with a basement lists it while it's switched on (a streamed piece: while the piece is), so that a survivor's body
    /// standing over it passes through the ground (<see cref="KeepUp"/>): the snowfield the outdoors stands on is one box
    /// with its top at y 0 (each piece's own, and the streamer's under everyone), which would otherwise be a floor over the
    /// stairs down and a ceiling over the basement. The ground floor over a basement is the building's own floor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelStructure : MonoBehaviour
    {
        /// <summary>The basement's corners on the site (this object's local x, z), anticlockwise or clockwise.</summary>
        internal Vector2[] Basement;

        private Vector2[] _world;
        private Rect _bounds;
        private static readonly List<LevelStructure> s_Basements = new List<LevelStructure>();

        private void OnEnable()
        {
            if (Basement == null || Basement.Length < 3) return;
            _world = new Vector2[Basement.Length];
            float x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue;
            for (int i = 0; i < Basement.Length; i++)
            {
                Vector3 w = transform.TransformPoint(new Vector3(Basement[i].x, 0f, Basement[i].y));
                _world[i] = new Vector2(w.x, w.z);
                x0 = Mathf.Min(x0, w.x); x1 = Mathf.Max(x1, w.x);
                z0 = Mathf.Min(z0, w.z); z1 = Mathf.Max(z1, w.z);
            }
            _bounds = Rect.MinMaxRect(x0, z0, x1, z1);
            s_Basements.Add(this);
        }

        private void OnDisable() => s_Basements.Remove(this);

        /// <summary>True when a point (world) is over a basement switched on now.</summary>
        internal static bool OverBasement(Vector3 p)
        {
            var q = new Vector2(p.x, p.z);
            for (int i = 0; i < s_Basements.Count; i++)
            {
                LevelStructure s = s_Basements[i];
                if (s == null || !s._bounds.Contains(q)) continue;
                if (Inside(s._world, q)) return true;
            }
            return false;
        }

        private static bool Inside(Vector2[] p, Vector2 q)
        {
            bool inside = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
                if ((p[i].y > q.y) != (p[j].y > q.y) && q.x < (p[j].x - p[i].x) * (q.y - p[i].y) / (p[j].y - p[i].y) + p[i].x)
                    inside = !inside;
            return inside;
        }

        #region The survivors' bodies
        private static readonly Collider[] s_Hits = new Collider[16];
        private static readonly Dictionary<CharacterController, List<Collider>> s_Ignoring = new Dictionary<CharacterController, List<Collider>>();

        // Enter Play Mode without a domain reload keeps statics from the last session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Basements.Clear();
            s_Ignoring.Clear();
        }

        /// <summary>
        /// Call before moving a survivor's body: over a basement it passes through the ground boxes there (every frame, as
        /// switching a collider off and on, which the streamer does to a lost ground box, forgets it); anywhere else it meets
        /// them again.
        /// </summary>
        internal static void KeepUp(CharacterController body)
        {
            if (body == null) return;
            Vector3 at = body.transform.position;
            bool over = s_Basements.Count > 0 && OverBasement(at);
            s_Ignoring.TryGetValue(body, out List<Collider> ignoring);
            if (!over)
            {
                if (ignoring == null || ignoring.Count == 0) return;
                foreach (Collider c in ignoring)
                    if (c != null) Physics.IgnoreCollision(body, c, false);
                ignoring.Clear();
                return;
            }
            if (ignoring == null) s_Ignoring[body] = ignoring = new List<Collider>(4);
            // The ground boxes under this spot: what's there half a metre below the ground's top.
            int n = Physics.OverlapBoxNonAlloc(new Vector3(at.x, -0.5f, at.z), new Vector3(0.6f, 0.3f, 0.6f), s_Hits, Quaternion.identity, 1,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = s_Hits[i];
                if (!(c is BoxCollider) || c.gameObject.name != "Ground") continue;
                Physics.IgnoreCollision(body, c, true);
                if (!ignoring.Contains(c)) ignoring.Add(c);
            }
        }
        #endregion
    }
}
