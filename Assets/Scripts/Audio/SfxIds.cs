using UnityEngine;

namespace CloneZ
{
    /// <summary>The volume buses: each has its own slider in the settings and its own share of ducking.</summary>
    public enum SfxBus { Effects, Ambience, Music, Interface }

    /// <summary>
    /// How the game plays a sound (its routing and behaviour), as opposed to how it is synthesised. Set alongside the
    /// recipe that makes the sound; <see cref="Sfx"/> reads it on every play.
    /// </summary>
    public sealed class SfxMeta
    {
        /// <summary>The routing used for an id with no recipe metadata.</summary>
        public static readonly SfxMeta Default = new SfxMeta();

        /// <summary>The volume bus (and settings slider) the sound belongs to.</summary>
        public SfxBus Bus = SfxBus.Effects;
        /// <summary>Unity voice priority: 0 is never culled, 256 goes first when too many sounds play at once.</summary>
        public int Priority = 128;
        /// <summary>The shortest audible range in metres, whatever the caller asks for (a far-off boom must carry).</summary>
        public float MinRange;
        /// <summary>The distant version, played instead when the listener is further than <see cref="FarDistance"/>.</summary>
        public string Far;
        public float FarDistance;
        /// <summary>A follow-up sound (a spent case on the floor, a body hitting the ground) played after a delay.</summary>
        public string Then;
        public float ThenDelay, ThenJitter, ThenVolume = 0.5f;
        /// <summary>Ambience and music duck by this many dB (negative) when it plays within <see cref="DuckRange"/> of the listener.</summary>
        public float DuckDb;
        public float DuckRange = 40f, DuckHold = 0.6f, DuckRelease = 2.5f;
        /// <summary>Within this range of the listener the blast deafens: the whole mix is muffled and the ears ring.</summary>
        public float ConcussRange;
        /// <summary>Never muffled by walls or distance (sounds inside the survivor's head or on their body).</summary>
        public bool NoOcclusion;
        /// <summary>How much of the room's reverb the sound picks up, 0..1 (0 for sounds that carry their own space).</summary>
        public float Reverb = 1f;
        /// <summary>Doppler level for moving loops (engines, rotors); one-shots stay at 0.</summary>
        public float Doppler;
        /// <summary>3D spread in degrees, for wide sources (a rotor overhead, a crowd).</summary>
        public float Spread;
    }

    /// <summary>Produces the clip for a sound id (the procedural synthesiser implements this).</summary>
    public interface ISfxBackend
    {
        /// <summary>A clip for <paramref name="id"/>, or null when unknown. May return a different variation each call.
        /// Renders on the calling thread when nothing is ready yet.</summary>
        AudioClip Get(string id);
        /// <summary>The given variation of <paramref name="id"/> (wrapped into range) — one zombie's own throat. While it
        /// is still being built another variation stands in.</summary>
        AudioClip Get(string id, int variation);
        /// <summary>The routing for <paramref name="id"/>; never null.</summary>
        SfxMeta Meta(string id);
        /// <summary>True when <see cref="Get"/> can answer without rendering (a variation is already built).</summary>
        bool IsReady(string id);
        /// <summary>Asks for <paramref name="id"/> to be built in the background ahead of the queue.</summary>
        void Request(string id);
    }

    /// <summary>
    /// Every sound id the game asks for; the synthesiser is expected to produce all of these. Partial: each family of
    /// recipes (Assets/Scripts/Audio/Synth/Recipes) may declare its own ids beside them.
    /// </summary>
    public static partial class SfxIds
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

        /// <summary>The ringing in the ears after a blast too close (played by the mixer's concussion).</summary>
        public const string EarRing = "ear_ring";
    }
}
