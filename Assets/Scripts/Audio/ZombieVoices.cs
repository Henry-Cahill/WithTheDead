using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The voices of the dead nearby: each corpse-walker within earshot groans now and then on its own clock, hunters
    /// more often and louder, all of them throttled so a horde is a chorus rather than a wall of noise.
    /// </summary>
    public sealed class ZombieVoices : MonoBehaviour
    {
        private static ZombieVoices s_Instance;

        private float _groanTimer;
        private readonly Dictionary<Actor, float> _nextGroan = new Dictionary<Actor, float>();
        private readonly List<Actor> _near = new List<Actor>();

        /// <summary>Ensures a single instance exists (it outlives scenes).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[ZombieVoices]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<ZombieVoices>();
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        private void Update()
        {
            Actor local = Actor.LocalPlayer;
            if (Time.timeScale <= 0f || local == null || GameSession.IsHub) return;
            Vector3 ears = ListenerRig.Active ? ListenerRig.Position : local.transform.position;
            Groans(ears, Time.unscaledDeltaTime);
        }

        // Each nearby corpse-walker groans now and then on its own clock; hunters more often, all of them throttled.
        private void Groans(Vector3 ears, float dt)
        {
            _groanTimer -= dt;
            if (_groanTimer > 0f) return;
            _groanTimer = 0.45f;

            _near.Clear();
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a.Kind != ActorKind.Zombie || !a.IsAlive) continue;
                if ((a.transform.position - ears).sqrMagnitude > 30f * 30f) continue;
                _near.Add(a);
            }
            if (_near.Count == 0) return;

            Actor pick = _near[Random.Range(0, _near.Count)];
            float now = Time.time;
            if (_nextGroan.TryGetValue(pick, out float next) && now < next) return;

            var ai = pick.GetComponent<ZombieAI>();
            bool hunting = ai != null && ai.IsHunting;
            if (_nextGroan.Count > 400) _nextGroan.Clear();
            _nextGroan[pick] = now + (hunting ? Random.Range(2.5f, 5f) : Random.Range(6f, 14f));
            Sfx.Play(SfxIds.ZombieGroan, pick.transform.position + Vector3.up * 1.6f, hunting ? 0.75f : 0.5f, Random.Range(0.85f, 1.12f), hunting ? 30f : 22f);
        }
    }
}
