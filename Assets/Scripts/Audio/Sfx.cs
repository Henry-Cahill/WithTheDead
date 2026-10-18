using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Produces the clip for a sound id (the procedural synthesiser implements this).</summary>
    public interface ISfxBackend
    {
        /// <summary>A clip for <paramref name="id"/>, or null when unknown. May return a different variation each call.</summary>
        AudioClip Get(string id);
    }

    /// <summary>
    /// The game's sound API: positional one-shots, UI clicks and attached loops by id (see <see cref="SfxIds"/>).
    /// Every sound is positional in the world and heard from the survivor's head (the listener rides the head
    /// bone, never the camera — <see cref="ListenerRig"/>). Sources are pooled. With no backend registered
    /// every call is a silent no-op, so gameplay never depends on audio.
    /// </summary>
    public static class Sfx
    {
        public static ISfxBackend Backend;

        private static Transform s_Root;
        private static readonly List<AudioSource> s_Pool = new List<AudioSource>(48);
        private const int MaxPool = 64;

        public static bool Enabled => Backend != null;

        private static Transform Root
        {
            get
            {
                if (s_Root == null)
                {
                    s_Root = new GameObject("[Sfx]").transform;
                    s_Pool.Clear();
                }
                return s_Root;
            }
        }

        private static AudioClip Clip(string id)
        {
            if (Backend == null || string.IsNullOrEmpty(id)) return null;
            try { return Backend.Get(id); }
            catch (System.Exception e) { Debug.LogException(e); return null; }
        }

        private static AudioSource Rent()
        {
            Transform root = Root;
            for (int i = 0; i < s_Pool.Count; i++)
            {
                AudioSource s = s_Pool[i];
                if (s != null && !s.isPlaying && !s.loop) return s;
            }
            if (s_Pool.Count >= MaxPool)
            {
                AudioSource oldest = s_Pool[0];
                s_Pool.RemoveAt(0);
                s_Pool.Add(oldest);
                oldest.Stop();
                return oldest;
            }
            var go = new GameObject("Sfx");
            go.transform.SetParent(root, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.dopplerLevel = 0f;
            s_Pool.Add(src);
            return src;
        }

        /// <summary>A positional one-shot. Returns the source (null when silent).</summary>
        public static AudioSource Play(string id, Vector3 position, float volume = 1f, float pitch = 1f, float maxDistance = 45f)
        {
            AudioClip clip = Clip(id);
            if (clip == null) return null;
            AudioSource src = Rent();
            src.transform.SetParent(Root, false);
            src.transform.position = position;
            Configure(src, volume, pitch, maxDistance, true);
            src.loop = false;
            src.clip = clip;
            src.Play();
            return src;
        }

        /// <summary>A flat (non-positional) sound: menus, the survivor's own breathing.</summary>
        public static AudioSource PlayUI(string id, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip = Clip(id);
            if (clip == null) return null;
            AudioSource src = Rent();
            Configure(src, volume, pitch, 10f, false);
            src.loop = false;
            src.clip = clip;
            src.Play();
            return src;
        }

        /// <summary>A looping sound that follows a transform until <see cref="Stop"/> (an engine, a rotor, an anomaly's hum).</summary>
        public static AudioSource Loop(string id, Transform follow, float volume = 1f, float maxDistance = 25f, float pitch = 1f)
        {
            AudioClip clip = Clip(id);
            if (clip == null || follow == null) return null;
            var go = new GameObject("SfxLoop " + id);
            go.transform.SetParent(follow, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            Configure(src, volume, pitch, maxDistance, true);
            src.loop = true;
            src.clip = clip;
            src.Play();
            return src;
        }

        public static void Stop(AudioSource src)
        {
            if (src == null) return;
            src.Stop();
            if (src.loop && !s_Pool.Contains(src)) Object.Destroy(src.gameObject);
        }

        private static void Configure(AudioSource src, float volume, float pitch, float maxDistance, bool spatial)
        {
            src.volume = Mathf.Clamp01(volume);
            src.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            src.spatialBlend = spatial ? 1f : 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = Mathf.Min(2f, maxDistance * 0.1f);
            src.maxDistance = Mathf.Max(3f, maxDistance);
        }
    }

    /// <summary>Every sound id the game asks for. The synthesiser is expected to produce all of these.</summary>
    public static class SfxIds
    {
        public const string GunPistol = "gun_pistol", GunRifle = "gun_rifle", GunShotgun = "gun_shotgun", GunSniper = "gun_sniper",
            GunSuppressed = "gun_suppressed", GunFlare = "gun_flare", DryFire = "dryfire", Reload = "reload", Jam = "jam",
            BoltThrow = "bolt_throw", BoltLand = "bolt_land",
            StepSnow = "step_snow", StepWood = "step_wood", StepConcrete = "step_concrete", StepGrass = "step_grass",
            DoorOpen = "door_open", DoorClose = "door_close", DoorBash = "door_bash", GearRustle = "gear_rustle",
            ZombieGroan = "zombie_groan", ZombieAttack = "zombie_attack", ZombieDie = "zombie_die",
            HitFlesh = "hit_flesh", HitArmor = "hit_armor", Ricochet = "ricochet", MeleeSwing = "melee_swing", MeleeHit = "melee_hit",
            Explosion = "explosion", Grenade = "grenade_pin",
            DetectorBeep = "detector_beep", DetectorAlert = "detector_alert", Geiger = "geiger_click",
            AnomalyHum = "anomaly_hum", AnomalyGravity = "anomaly_gravity", AnomalyFire = "anomaly_fire", AnomalyElectric = "anomaly_electric",
            AnomalyAcid = "anomaly_acid", AnomalyTrigger = "anomaly_trigger",
            EmissionSiren = "emission_siren", EmissionRumble = "emission_rumble", PsiWhisper = "psi_whisper",
            HeliRotor = "heli_rotor", FlareBurn = "flare_burn",
            BreathMask = "breath_mask", MaskCrack = "mask_crack", VisorWipe = "visor_wipe", Heartbeat = "heartbeat", Sneeze = "sneeze", Cough = "cough",
            WindAmbience = "amb_wind", ForestAmbience = "amb_forest", BarAmbience = "amb_bar", Guitar = "guitar",
            WolfHowl = "wolf_howl", BearRoar = "bear_roar", MutantScreech = "mutant_screech", MutantGrowl = "mutant_growl", DeerCall = "deer_call",
            UiClick = "ui_click", UiBuy = "ui_buy", UiError = "ui_error", Cash = "cash",
            UncannyWhisper = "uncanny_whisper", RadioStatic = "radio_static", Stinger = "horror_stinger",
            EngineLoop = "engine_loop", EngineStartFail = "engine_start_fail", DroneBuzz = "drone_buzz",
            Pickup = "pickup", Eat = "eat", Drink = "drink", Bandage = "bandage", Injector = "injector", PageTurn = "page_turn",
            Splash = "splash", Crank = "crank", Lighter = "lighter", Handcuffs = "handcuffs";
    }
}
