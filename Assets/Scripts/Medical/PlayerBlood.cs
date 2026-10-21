using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// DayZ's separate blood and shock pools. Bleeding drains blood as well as health; a body short on blood is
    /// slow, grey and faint, and below a point it simply stops. Big hits, blood loss and psi pressure build shock;
    /// enough shock knocks the survivor unconscious for a while — helpless, unless someone (or nothing) comes.
    /// Blood only comes back with food and water, or a transfusion of the right type: the wrong one is a
    /// haemolytic reaction. Your blood type isn't known until it's tested.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerBlood : MonoBehaviour, IStatModSource
    {
        public const float MaxBlood = 5000f;
        public const float MlPerHealth = 22f;

        public float Blood = MaxBlood;
        public float Shock;
        private float _unconscious;
        private float _faintRoll;
        private PlayerStats _stats;
        private Image _blackout;
        private StatMods _modifiers;
        private Text _blackoutText;
        private CharacterProfile _cachedProfile;

        /// <summary>Gets the normalized blood amount (0‑1) based on current blood.</summary>
        public float Blood01 => Mathf.Clamp01(Blood / MaxBlood);
        /// <summary>Gets the normalized shock amount (0‑1) based on current shock.</summary>
        public float Shock01 => Mathf.Clamp01(Shock / 100f);
        /// <summary>Indicates whether the survivor is currently unconscious.</summary>
        public bool IsUnconscious => _unconscious > 0f;
        /// <summary>Gets the elapsed unconscious time in seconds.</summary>
        public float UnconsciousSeconds => _unconscious;

        public event System.Action<bool> UnconsciousChanged;

        /// <summary>Gets the blood type of the survivor, falling back to a random scavenger type if unknown.</summary>
        public string BloodType
        {
            get
            {
                return _cachedProfile != null && !GameSession.IsScavRun && !string.IsNullOrEmpty(_cachedProfile.BloodType) ? _cachedProfile.BloodType : _scavType;
            }
        }

        private string _scavType;

        /// <summary>True once a test kit has told the survivor their type.</summary>
        public bool TypeKnown
        {
            get
            {
                return _cachedProfile != null && _cachedProfile.HasFlag("bloodknown");
            }
        }

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _modifiers = PlayerModifiers.Current;
            _scavType = BloodTypes.Roll();
        }

        private void Start()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            bool carries = c != null && (GameSession.IsHub || (GameSession.IsExtraction && !GameSession.IsScavRun && !GameSession.IsMonsterRun));
            if (carries && c.Health != null) Blood = Mathf.Clamp(c.Health.Blood, 2600f, MaxBlood);
        }

        /// <summary>Blood lost with health that bled away (called by <see cref="PlayerStats.Bleed"/>).</summary>
        public void LoseBloodForHealth(float health) => Blood = Mathf.Max(0f, Blood - health * MlPerHealth * _modifiers.BleedRate);

        /// <summary>Adds shock to the survivor, respecting dead state and clamping the total.</summary>
        public void AddShock(float amount)
        {
            if (amount <= 0f || _stats.IsDead) return;
            Shock = Mathf.Min(120f, Shock + amount);
            if (Shock >= 100f && !IsUnconscious) KnockOut(Random.Range(10f, 22f));
        }

        /// <summary>Knocks the survivor out for a duration, capping shock and marking them incapacitated.</summary>
        public void KnockOut(float seconds)
        {
            if (_stats.IsDead) return;
            bool was = IsUnconscious;
            _unconscious = Mathf.Max(_unconscious, seconds);
            Shock = Mathf.Min(Shock, 70f);
            _stats.Incapacitated = true;
            if (!was)
            {
                UnconsciousChanged?.Invoke(true);
                NoiseSystem.Emit(transform.position, 8f);
                Sfx.PlayUI(SfxIds.Heartbeat, 0.8f);
            }
        }

        /// <summary>Someone shakes the survivor awake (only when there's blood enough to stay up). True if it worked.</summary>
        /// <summary>Attempts to wake the survivor; succeeds only if enough blood remains.</summary>
        public bool Rouse()
        {
            if (!IsUnconscious || Blood < 2900f) return false;
            _unconscious = Mathf.Min(_unconscious, 1f);
            Shock = Mathf.Min(Shock, 60f);
            return true;
        }

        /// <summary>A pint of donor blood through an IV line. Wrong type: a haemolytic reaction.</summary>
        public bool Transfuse(string donorType, out string message)
        {
            if (BloodTypes.Compatible(donorType, BloodType))
            {
                float before = Blood;
                Blood = Mathf.Min(MaxBlood, Blood + 500f);
                Shock = Mathf.Max(0f, Shock - 20f);
                message = before >= MaxBlood - 1f ? "You didn't need it, but it's in you now." : "The colour comes back to your hands.";
                return true;
            }
            AddShock(75f);
            _stats.Sickness = Mathf.Min(CloneZConfig.SicknessMax, _stats.Sickness + 45f);
            _stats.EnvironmentDrain(12f, "died of a transfusion reaction");
            message = "Fever, then chills. That blood was the wrong type.";
            return false;
        }

        /// <summary>Draws a pint of your own blood into an empty bag (for a squadmate). False when you can't spare it.</summary>
        public bool DrawBlood()
        {
            if (Blood < 3600f) return false;
            Blood -= 500f;
            AddShock(10f);
            return true;
        }

        private void Update()
        {
            _cachedProfile = ProfileStore.ActiveCharacter;
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;

            // Blood comes back slowly, and only to a fed and watered body.
            if (_stats.Hunger < 0.6f && _stats.Thirst < 0.6f && Blood < MaxBlood)
                Blood = Mathf.Min(MaxBlood, Blood + 1.6f * _modifiers.HealRate * dt);

            float shockFloor = Blood < 3500f ? (3500f - Blood) / 30f : 0f;
            Shock = Mathf.Max(shockFloor, Shock - (Blood > 4000f ? 4f : 2f) * dt);

            if (Blood < 2700f && !IsUnconscious)
            {
                _faintRoll -= dt;
                if (_faintRoll <= 0f)
                {
                    _faintRoll = 6f;
                    if (Random.value < 0.25f) KnockOut(Random.Range(8f, 16f));
                }
            }
            if (Blood <= 1800f) _stats.Kill("bled out");

            if (_unconscious > 0f)
            {
                _unconscious -= dt;
                if (_unconscious <= 0f)
                {
                    _unconscious = 0f;
                    _stats.Incapacitated = false;
                    UnconsciousChanged?.Invoke(false);
                }
            }
            UpdateOverlay();
        }

        private void UpdateOverlay()
        {
            if (!IsUnconscious && _blackout == null) return;
            if (_blackout == null)
            {
                Canvas canvas = UIKit.CreateCanvas("Unconscious", 260, transform);
                _blackout = UIKit.Panel(canvas.transform, "Black", Color.black);
                UIKit.Stretch(_blackout.rectTransform);
                _blackoutText = UIKit.Label(_blackout.rectTransform, "", 26, new Color(0.7f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
                UIKit.Stretch(_blackoutText.rectTransform);
            }
            float a = IsUnconscious ? 0.97f : Mathf.Max(0f, _blackout.color.a - Time.deltaTime * 0.8f);
            _blackout.color = new Color(0f, 0f, 0f, a);
            _blackoutText.text = IsUnconscious ? "You're out cold... (" + Mathf.CeilToInt(_unconscious) + ")" : "";
            _blackout.gameObject.SetActive(a > 0.01f);
        }

        /// <summary>Applies stat modifiers based on current blood and shock levels.</summary>
        public void ApplyMods(StatMods m)
        {
            if (Blood < 3500f)
            {
                float t = Mathf.InverseLerp(3500f, 2200f, Blood);
                m.StaminaRegen *= Mathf.Lerp(1f, 0.5f, t);
                m.AimSpread *= Mathf.Lerp(1f, 1.3f, t);
                m.MoveSpeed *= Mathf.Lerp(1f, 0.8f, t);
            }
            if (Shock > 50f) m.AimSpread *= 1f + (Shock - 50f) / 200f;
        }
    }
}
