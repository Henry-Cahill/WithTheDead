using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// How easily the survivor is seen and heard right now (Metro's wrist light, Gray Zone Warfare's visibility
    /// indicator): the light falling on them (sun or moon, lamps nearby, their own torch or lighter), their stance
    /// and movement, fog, and anything bending it (camouflage traits, artifacts). Darkness is a resource: the torch
    /// that lets you see is the torch that gets you seen. Written to <see cref="Actor.Visibility01"/>, which every AI
    /// sight check reads; shown as an eye and an ear on the HUD (or the watch's lamp in the diegetic modes).
    /// </summary>
    public class Stealth : MonoBehaviour
    {
        /// <summary>Gets the locally active Stealth instance for the player.</summary>
        public static Stealth Local { get; private set; }

        /// <summary>A hand-held flame (the lighter) is lit.</summary>
        public static bool LighterOn;

        /// <summary>Current visibility level (0‑1) of the survivor, used for AI sight checks.</summary>
        public float Visibility01 { get; private set; }
        /// <summary>Current noise level (0‑1) the survivor makes, affecting detection.</summary>
        public float Noise01 { get; private set; }

        private Actor _actor;
        private PlayerGear _gear;
        private TimeOfDay _tod;
        private StatMods _mods;
        private PlayerController _ctl;
        private PlayerStats _stats;
        private PlayerCombat _combat;
        private float _next;
        private float _shotSpike;
        private float _lightScan;
        private static readonly List<Light> s_Lights = new List<Light>(64);

        /// <summary>Initialises component references and registers for fire events.</summary>
        private void Start()
        {
            // Cache nearby components once at start; stealth polls them each tick rather than GetComponent each time
            _actor = GetComponent<Actor>();
            _ctl = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _combat = GetComponent<PlayerCombat>();
            // Gunshots create an immediate ear spike that decays over time (handled in Update)
            if (_combat != null) _combat.OnFire += OnFire;
            // Only the local player feeds visibility to Actor and gets a HUD widget
            if (_actor != null && _actor.Kind == ActorKind.Player)
            {
                Local = this;
                HudStealth.Create(this);
            }
            // Lighter state is shared statically; reset to off at spawn
            LighterOn = false;
            // Pull long-lived singletons once; they may not be ready if order changes, but Start is post-bootstrap here
            _gear = PlayerGear.Instance;
            _tod = TimeOfDay.Instance;
            _mods = PlayerModifiers.Current;
        }

        /// <summary>Cleans up event subscriptions and clears the static instance.</summary>
        private void OnDestroy()
        {
            if (_combat != null) _combat.OnFire -= OnFire;
            if (Local == this) Local = null;
        }

        /// <summary>A shot spikes the noise meter (the ear); AI hears the gun through NoiseSystem, not through visibility.</summary>
        private void OnFire() => _shotSpike = 1f;

        private void Update()
        {
            // Decay gunshot ear spike over time so AI hears it briefly rather than permanently
            _shotSpike = Mathf.Max(0f, _shotSpike - Time.deltaTime * 0.8f);
            // Throttle expensive visibility scans to avoid per-frame work
            if (Time.time < _next) return;
            _next = Time.time + 0.2f;
            // Sample light at head height to match what sight checks see
            Vector3 p = transform.position + Vector3.up * 1.2f;

            float ambient;
            TimeOfDay tod = _tod;
            // Night scales by darkness setting; day is bright by default
            if (tod != null && tod.IsNight) ambient = Mathf.Lerp(0.3f, 0.06f, SandboxSettings.Current.NightDarkness);
            else ambient = 0.85f;
            // Being under a roof cuts ambient light significantly
            if (Shelter.IsSheltered(p)) ambient *= 0.45f;

            float lamps = 0f;
            // Refresh the light list infrequently to avoid FindObjectsByType every frame
            if (Time.time >= _lightScan)
            {
                _lightScan = Time.time + 2f;
                s_Lights.Clear();
                s_Lights.AddRange(FindObjectsByType<Light>());
            }
            for (int i = 0; i < s_Lights.Count; i++)
            {
                Light l = s_Lights[i];
                // Skip dead, disabled, directional (global), or lights attached to self
                if (l == null || !l.isActiveAndEnabled || l.type == LightType.Directional || l.transform.IsChildOf(transform)) continue;
                float d = Vector3.Distance(l.transform.position, p);
                if (d >= l.range) continue;
                // Falloff squared and scaled down so local lamps don't dominate visibility
                float f = 1f - d / l.range;
                lamps += l.intensity * f * f * 0.35f;
            }

            PlayerGear gear = _gear;
            float own = 0f;
            // Own light sources are loud visually: flashlight is strongest, lighter weaker
            if (gear != null && gear.FlashlightOn) own += 0.55f;
            if (LighterOn) own += 0.25f;

            float vis = Mathf.Clamp01(ambient + Mathf.Min(0.8f, lamps) + own);
            bool sneaking = _ctl != null && _ctl.IsSneaking;
            float speed = _ctl != null ? _ctl.CurrentSpeed : 0f;
            // Stance and exertion bend visibility: crouch hides, sprinting exposes
            if (sneaking) vis *= 0.6f;
            else if (_stats != null && _stats.Exertion == Exertion.Sprinting) vis = Mathf.Min(1f, vis + 0.15f);
            // Thicker fog makes you harder to see
            float fog = Mathf.Clamp01((75f - RenderSettings.fogEndDistance) / 60f);
            vis *= 1f - 0.35f * fog;
            // Global trait/artifact modifiers
            vis *= _mods.VisibilityMult;
            Visibility01 = Mathf.Clamp01(vis);
            // Never report zero visibility to AI; a floor keeps you just barely detectable
            if (_actor != null) _actor.Visibility01 = Mathf.Max(0.05f, Visibility01);

            // Noise scales with movement: still is quiet, walking mid, sprinting loud; sneaking dampens
            float noise = speed < 0.1f ? 0.03f : sneaking ? 0.15f : (_stats != null && _stats.Exertion == Exertion.Sprinting) ? 0.85f : 0.4f;
            noise *= _mods.NoiseMult;
            // Gunshots override movement noise
            Noise01 = Mathf.Clamp01(Mathf.Max(noise, _shotSpike));
        }
    }

    /// <summary>The eye and the ear: how visible and how loud the survivor is.</summary>
    public class HudStealth : MonoBehaviour
    {
        private Stealth _s;
        private Image _eye, _ear;
        private float _lastVis = -1f;
        private float _lastNoise = -1f;

        /// <summary>Creates the HUD stealth widget for the given Stealth instance.</summary>
        public static void Create(Stealth s)
        {
            RectTransform root = HudLayer.Anchor("Stealth", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 150f), new Vector2(120f, 46f));
            var h = root.gameObject.AddComponent<HudStealth>();
            h._s = s;
            h._eye = h.Meter(root, "SEEN", 26f);
            h._ear = h.Meter(root, "HEARD", 4f);
        }

        /// <summary>Builds a label and horizontal bar for a stealth meter at the given vertical offset.</summary>
        private Image Meter(RectTransform root, string label, float y)
        {
            Text t = UIKit.Label(root, label, 12, UIKit.TextDim);
            UIKit.Place(t.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, y), new Vector2(46f, 16f));
            Image back = UIKit.Panel(root, label + "Back", new Color(0f, 0f, 0f, 0.55f));
            UIKit.Place(back.rectTransform, Vector2.zero, Vector2.zero, new Vector2(48f, y + 3f), new Vector2(70f, 10f));
            Image fill = UIKit.Panel(back.rectTransform, "Fill", UIKit.Good);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            return fill;
        }

        private void Update()
        {
            if (_s == null) return;
            // Only update HUD if values have changed beyond a small epsilon
            if (!Mathf.Approximately(_s.Visibility01, _lastVis))
            {
                Set(_eye, _s.Visibility01);
                _lastVis = _s.Visibility01;
            }
            if (!Mathf.Approximately(_s.Noise01, _lastNoise))
            {
                Set(_ear, _s.Noise01);
                _lastNoise = _s.Noise01;
            }
        }

        private static void Set(Image fill, float v)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1f);
            fill.color = v > 0.66f ? UIKit.Bad : v > 0.33f ? UIKit.Warn : UIKit.Good;
        }
    }
}
