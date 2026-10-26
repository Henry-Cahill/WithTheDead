using System;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Defines how zombie bites affect the survivor: lethal, chance‑based, or disabled.</summary>
    public enum InfectionMode { BiteLethal, BiteChance, Off }
    /// <summary>Determines what happens to a survivor's inventory on death.</summary>
    public enum DeathPenalty { LoseCarried, CorpseRecovery }

    /// <summary>
    /// The sandbox dials (Project Zomboid style) plus the server rules: nearly everything that changes how hard
    /// or how long the game is, written to <c>clonez_sandbox.json</c> so server owners can tune a world without
    /// touching code. The host's settings are sent to every client when a session starts.
    /// </summary>
    [Serializable]
    public class SandboxSettings
    {
        // The dead.
        public float ZombiePopulation = 1f;        // x the zone's base population
        public float SprinterShare = 0.12f;        // 0..1 share of runners among the dead
        public bool ZombieRespawn = true;
        public InfectionMode Infection = InfectionMode.BiteLethal;

        // Loot and the economy.
        public float LootAbundance = 1f;           // x rolls in every container
        public bool CentralEconomy = true;         // nominal/min/lifetime targets and restocks (economy.json)
        public bool FleaMarket = true;
        public int FleaMinLevel = 10;
        public int InsuranceReturnDays = 2;
        public int ScavCooldownMinutes = 20;
        public int SeasonLengthDays = 0;           // 0: never wipe automatically

        // Survival drain, tuned to the real session length (a 40-minute raid shouldn't starve you).
        public float HungerRate = 1f, ThirstRate = 1f, FatigueRate = 1f;
        public bool AutoTuneDrainToRaid = true;

        // The world clock and its threats.
        public float RaidHours = 7f;               // in-game hours from insertion to dawn
        public bool OpenZoneNoTimer = false;       // GZW: stay as long as supplies and nerve last
        public int UtilityShutoffMinDay = 3, UtilityShutoffMaxDay = 9;
        public float NightDarkness = 0.85f;        // 0 bright nights .. 1 pitch black (Build 42 darkness)
        public float AnomalyDensity = 1f;
        public float EmissionIntervalMinutes = 22f; // real minutes between emissions in a raid (0 = never)
        public float PsiStormChance = 0.3f;        // share of emissions that are psi-storms instead
        public float HelicopterEventChance = 0.35f;
        public bool UncannyEvents = true;

        // Deep systems (tension 4: pick a few to make deep; the rest can be simplified).
        public bool DeepMedical = true;            // limbs, blood, shock, fractures
        public bool DeepBallistics = true;         // penetration vs armour class
        public bool Diseases = true;               // cholera, wound infection, colds

        // Death.
        public DeathPenalty Death = DeathPenalty.LoseCarried;
        public bool PermadeathCharacters = false;  // DayZ: death deletes the character (Ironman forces it per character)

        // Server rules (multiplayer).
        public bool PvpEnabled = true;
        public bool FriendlyFire = false;
        public bool FirstPersonOnlyInPvp = true;   // tension 10: no shoulder-peeking in PvP raids
        public bool AllowPlayerMonsters = true;
        public float VoiceRange = 30f;
        public int MaxPlayers = 8;
        public bool OfflineRaidProtection = true;  // bases can't be raided while their owner is offline / asleep

        public const string FileName = "clonez_sandbox.json";
        /// <summary>The full path to the sandbox settings file in the persistent data folder.</summary>

        /// <summary>The full path to the sandbox settings JSON file in persistent storage.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static SandboxSettings s_Current;

        /// <summary>The active settings (the host's, during a networked session).</summary>
        public static SandboxSettings Current
        {
            get
            {
                if (s_Current == null) s_Current = LoadFromDisk();
                return s_Current;
            }
        }

        /// <summary>Replaces the active settings (a client adopting the host's).</summary>
        public static void Adopt(SandboxSettings s)
        {
            if (s == null) return;
            s_Current = s;
            ServerRules.FromSandbox(s);
        }

        /// <summary>Loads the sandbox settings from the JSON file on disk, or creates defaults if missing.</summary>
public static SandboxSettings LoadFromDisk()
        {
            SandboxSettings s = null;
            try
            {
                if (File.Exists(FilePath)) s = JsonUtility.FromJson<SandboxSettings>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Could not read sandbox settings: " + e.Message);
            }
            if (s == null) s = new SandboxSettings();
            ServerRules.FromSandbox(s);
            return s;
        }

        /// <summary>Saves the current sandbox settings to disk as JSON.</summary>
        /// <summary>Writes the current game settings to PlayerPrefs and updates audio volume.</summary>
        public void Save()
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(this, true)); }
            catch (Exception e) { Debug.LogWarning("[CloneZ] Could not save sandbox settings: " + e.Message); }
            ServerRules.FromSandbox(this);
        }

        /// <summary>Serialises these sandbox settings to a JSON string.</summary>
        public string ToJson() => JsonUtility.ToJson(this);
        /// <summary>Deserialises a sandbox settings instance from a JSON string.</summary>
        public static SandboxSettings FromJson(string json)
        {
            try { return JsonUtility.FromJson<SandboxSettings>(json); }
            catch { return null; }
        }

        /// <summary>
        /// Survival drain per need, tuned to the real session length (tension 3: needs barely matter in a 40-minute
        /// raid and dominate a six-hour life). The base rates were tuned for the endless arena; in a raid of
        /// <see cref="RaidHours"/> in-game hours they're stretched to about one meal and two drinks per raid, a
        /// little more in the open zone, where supplies are the clock.
        /// </summary>
        public float DrainScale(string need)
        {
            if (!AutoTuneDrainToRaid || !GameSession.IsExtraction) return 1f;
            float length = 7f / Mathf.Max(1f, RaidHours);
            bool open = GameSession.IsOpenZone || OpenZoneNoTimer;
            switch (need)
            {
                case "hunger": return (open ? 0.35f : 0.21f) * length;
                case "thirst": return (open ? 0.62f : 0.48f) * length;
                default: return (open ? 0.5f : 0.3f) * length;
            }
        }
    }

    /// <summary>The HUD style options available to the player.</summary>
    public enum HudMode { Full, Minimal, Diegetic, Hardcore }

    /// <summary>
    /// The player's own presentation and accessibility choices (tension 12's readability floor): brightness,
    /// motion blur, lens distortion, camera shake, field of view, subtitles, the HUD style and audio. Stored in
    /// PlayerPrefs, so they follow the machine rather than the profile.
    /// </summary>
    [Serializable]
    public class GameSettings
    {
        public float Brightness = 0f;              // -1 .. +1 exposure offset
        public bool MotionBlur = false;
        public float LensDistortion = 1f;          // 0 off .. 1 full bodycam lens
        public float CameraShake = 1f;
        public float FieldOfView = 70f;            // first-person / bodycam FOV
        public float FogReadability = 0f;          // 0 authored fog .. 1 thinner fog for readability
        public bool Subtitles = true;
        public HudMode Hud = HudMode.Full;
        public bool Crosshair = true;
        public float MasterVolume = 0.9f;
        public float VoiceVolume = 1f;
        public float MusicVolume = 0.65f;
        public float EffectsVolume = 1f;
        public float AmbienceVolume = 0.9f;
        public float InterfaceVolume = 0.8f;
        public bool Microphone = true;
        public bool PushToTalk = true;
        public string MicrophoneDevice = "";
        public float MouseSensitivity = 1f;

        private const string PrefsKey = "clonez.settings";
        private static GameSettings s_Current;

        /// <summary>The currently loaded game settings (from PlayerPrefs) for the local player.</summary>
        public static GameSettings Current
        {
            get
            {
                if (s_Current == null) Load();
                return s_Current;
            }
        }

        public static event Action Changed;

        /// <summary>Loads the game settings from PlayerPrefs, falling back to defaults.</summary>
        public static void Load()
        {
            s_Current = null;
            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, "");
                if (!string.IsNullOrEmpty(json))
                {
                    // Overwrite a fresh instance, so settings added since the save keep their defaults.
                    s_Current = new GameSettings();
                    JsonUtility.FromJsonOverwrite(json, s_Current);
                }
            }
            catch { s_Current = null; }
            if (s_Current == null) s_Current = new GameSettings();
            AudioListener.volume = s_Current.MasterVolume;
        }

        /// <summary>Writes the current game settings to PlayerPrefs and updates audio volume.</summary>
        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
            Changed?.Invoke();
        }

        /// <summary>True when the HUD should hide its widgets (diegetic and hardcore modes rely on the world instead).</summary>
        public bool HudHidden => Hud == HudMode.Diegetic || Hud == HudMode.Hardcore;
        /// <summary>Indicates whether the crosshair should be displayed (hidden in hardcore modes).</summary>
        public bool ShowCrosshair => Crosshair && Hud != HudMode.Hardcore;
    }
}
