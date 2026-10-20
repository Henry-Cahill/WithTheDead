using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Fog of war for any actor that isn't a zombie (people, mutants, animals, other players): its body is drawn only
    /// while the survivor could actually see it — the same rule <see cref="ZombieVisibility"/> applies to the dead
    /// (vision cone, light, line of sight from the survivor's eye, never the camera's). This is what stops the
    /// isometric and shoulder cameras from seeing round corners; the server applies the same test before it sends
    /// another player's position, so a modified client can't simply switch it off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ActorFog : MonoBehaviour
    {
        private Renderer[] _renderers;
        private int _count = -1;
        private bool _shown = true;
        private float _next, _lastSeen = -99f;
        private Actor _actor;
        private Transform _transform;

        /// <summary>True while this actor is drawn for the local survivor.</summary>
        public bool Shown => _shown;

        private void Start()
        {
            _actor = GetComponent<Actor>();
            _transform = transform;
            _next = Time.time + Random.value * 0.1f;
            Cache();
        }

        private void LateUpdate()
        {
            if (Time.time < _next) return;
            _next = Time.time + 0.1f;

            bool show = true;
            if (ZombieVisibility.HasSight && Actor.LocalPlayer != null && _actor != Actor.LocalPlayer)
            {
                float h = _actor != null ? _actor.Height : 1.6f;
                Vector3 root = _transform.position;
                bool dead = _actor != null && !_actor.IsAlive;
                Vector3 chest = root + Vector3.up * (dead ? 0.3f : h * 0.65f);
                Vector3 head = root + Vector3.up * (dead ? 0.3f : h * 0.92f);
                bool seen = ZombieVisibility.CanSee(ZombieVisibility.CurrentSight, chest, head);
                if (_actor != null && _actor.Cloaked && !seen) seen = false;
                if (seen) _lastSeen = Time.time;
                show = seen || Time.time - _lastSeen < 0.25f;
            }
            Set(show);
        }

        private void Set(bool show)
        {
            if (show == _shown) return;
            _shown = show;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].forceRenderingOff = !show;
        }

        private void Cache()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _count = _transform.hierarchyCount;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].forceRenderingOff = !_shown;
        }



        private void OnTransformChildrenChanged()
        {
            Cache();
        }

        private void OnDisable()
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].forceRenderingOff = false;
        }

    }
}
