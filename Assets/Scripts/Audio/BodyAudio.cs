using UnityEngine;

namespace CloneZ
{
    /// <summary>The survivor's own body: the rasp of breath inside a gas mask, and a cough.</summary>
    public sealed class BodyAudio : MonoBehaviour
    {
        private static BodyAudio s_Instance;

        private AudioSource _mask;
        private Actor _actor;
        private PlayerStats _stats;
        private PlayerGear _gear;

        /// <summary>Ensures a single instance exists (it outlives scenes).</summary>
        public static void Ensure()
        {
            if (s_Instance != null) return;
            var go = new GameObject("[BodyAudio]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<BodyAudio>();
        }

        /// <summary>Hooks the sounds the survivor's gear raises (a cough).</summary>
        public static void OnPlayerBuilt(PlayerStats p)
        {
            var gear = p.GetComponent<PlayerGear>();
            Transform t = p.transform;
            if (gear != null) gear.Coughed += () => Sfx.Play(SfxIds.Cough, t.position + Vector3.up * 1.6f, 0.7f, Random.Range(0.9f, 1.1f), 18f);
        }

        private void OnDestroy()
        {
            if (s_Instance == this) s_Instance = null;
        }

        private void Update()
        {
            Actor local = Actor.LocalPlayer;
            if (local != _actor)
            {
                _actor = local;
                _stats = local != null ? local.GetComponent<PlayerStats>() : null;
                _gear = local != null ? local.GetComponent<PlayerGear>() : null;
            }
            float mask = 0f;
            if (!GameSession.IsHub && _stats != null && _gear != null && _gear.HasGasMask && !_stats.IsDead) mask = 0.32f;

            if (_mask == null)
            {
                if (mask <= 0f) return;
                if (!Sfx.Ready(SfxIds.BreathMask)) { Sfx.Request(SfxIds.BreathMask); return; }
                _mask = Sfx.LoopFlat(SfxIds.BreathMask, 0f);
                if (_mask == null) return;
            }
            Sfx.SetVolume(_mask, Mathf.MoveTowards(Sfx.GetVolume(_mask), mask, Time.unscaledDeltaTime * 0.75f));
        }
    }
}
