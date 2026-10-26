using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The survivor's own body, heard from inside the head: breathing through a gas mask, heavy breathing when winded
    /// bare-faced, and a cough on toxic gas. The breath follows exertion: sprinting winds the survivor within a couple of
    /// seconds (behind a mask the breathing turns fast, hard and louder) and it settles slowly as endurance comes back.
    /// Never in the hub. The loops start lazily, once rendered in the background, and glide to their levels.
    /// </summary>
    public sealed class BodyAudio : MonoBehaviour
    {
        // Base volumes of the breathing loops (before the effects bus).
        private const float MaskCalmVolume = 0.34f, MaskHardVolume = 0.4f, WindedVolume = 0.22f;
        // Effort above which the mask breathing turns hard, and below which it calms again (the loops' paces, about 2.85 s
        // and 1.6 s a breath, are pitched toward each other near the switch so the change in pace stays small).
        private const float HardAbove = 0.55f, CalmBelow = 0.3f;

        private static BodyAudio s_Instance;

        private AudioSource _maskCalm, _maskHard, _winded;
        private Actor _actor;
        private PlayerStats _stats;
        private PlayerGear _gear;
        private float _effort;   // 0..1: how hard the survivor is breathing
        private float _sprint;   // 0..1: built up by sustained sprinting
        private bool _hard;      // the mask is on its fast, hard loop

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
            if (gear != null) gear.Coughed += () => Sfx.Play(SfxIds.Cough, t.position + Vector3.up * 1.6f, 0.7f, Random.Range(0.9f, 1.1f), 24f);
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
                _effort = _sprint = 0f;
                _hard = false;
            }
            bool alive = !GameSession.IsHub && _stats != null && !_stats.IsDead;
            bool mask = alive && _gear != null && _gear.HasGasMask;

            // Effort: sprinting winds the survivor within a couple of seconds; spent endurance keeps the breath hard while
            // it comes back. Game time, so the pause holds the breath where it is.
            float dt = Time.deltaTime;
            if (alive)
            {
                Exertion ex = _stats.Exertion;
                float push = ex == Exertion.Sprinting ? 1f : ex == Exertion.Running ? 0.6f : 0f;
                _sprint = Mathf.MoveTowards(_sprint, push, dt * (push > _sprint ? 0.35f : 0.2f));
                float winded = Mathf.Clamp01((0.85f - _stats.Endurance) / 0.6f);
                float target = Mathf.Max(_sprint, winded);
                _effort = Mathf.MoveTowards(_effort, target, dt * (target > _effort ? 0.5f : 0.07f));
            }
            else _effort = _sprint = 0f;

            // Behind the mask: a calm loop and a hard one, switched with some hysteresis so the two paces only ever
            // overlap in a short cross-fade; within each, the pace and the level follow the effort.
            if (_hard ? _effort < CalmBelow : _effort > HardAbove) _hard = !_hard;
            float calm = mask && !_hard ? MaskCalmVolume * (1f + 0.25f * _effort) : 0f;
            float hard = mask && _hard ? Mathf.Lerp(MaskCalmVolume, MaskHardVolume, _effort) : 0f;
            Drive(ref _maskCalm, SfxIds.BreathMask, calm, 1f + 0.2f * Mathf.Clamp01(_effort / HardAbove), 0.5f);
            Drive(ref _maskHard, SfxIds.BreathMaskHard, hard, 0.85f + 0.23f * _effort, 0.5f);

            // Bare-faced: heavy breathing once really winded, easing off as the breath comes back.
            float heavy = alive && !mask ? WindedVolume * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.85f, _effort)) : 0f;
            Drive(ref _winded, SfxIds.BreathHeavy, heavy, 0.9f + 0.15f * _effort, 0.35f);
        }

        // A breathing loop: started silent once its clip is ready (asked for in the background until then), glided toward
        // its level and pace, and stopped once it has faded out.
        private static void Drive(ref AudioSource src, string id, float volume, float pitch, float fadePerSecond)
        {
            if (src == null)
            {
                if (volume <= 0.001f) return;
                if (!Sfx.Ready(id)) { Sfx.Request(id); return; }
                src = Sfx.LoopFlat(id, 0f, pitch);
                if (src == null) return;
            }
            float dt = Time.unscaledDeltaTime;
            float v = Mathf.MoveTowards(Sfx.GetVolume(src), volume, dt * fadePerSecond);
            Sfx.SetVolume(src, v);
            src.pitch = Mathf.MoveTowards(src.pitch, pitch, dt * 0.2f);
            if (volume <= 0f && v <= 0f)
            {
                Sfx.Stop(src);
                src = null;
            }
        }
    }
}
