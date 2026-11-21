using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The air of a zone, layered over the clock and the weather each frame: Mirelock's fog that never lifts, the
    /// Spillway's sickly cast, the sandbox's night darkness (Build 42's darker nights), and the accessibility floor
    /// (tension 12): a player can thin the fog for readability. Emissions paint over everything with
    /// <see cref="SkyOverride"/>.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class ZoneAtmosphere : MonoBehaviour
    {
        /// <summary>An overriding sky tint (an emission's red) and how strongly it applies, 0..1.</summary>
        public static Color SkyOverride = Color.red;
        public static float SkyOverrideStrength;

        private ZoneDef _zone;
        private float _baseStart, _baseEnd, _lastStart = -1f, _lastEnd = -1f;
        private Color _baseAmbient, _baseFogColor, _lastAmbient, _lastFogColor;
        private TimeOfDay _tod;
        private GameSettings _gs;
        private SandboxSettings _ss;
        // Cached input values to detect changes
        private float _lastFogDensity;
        private float _lastFogReadability;
        private float _lastNightDarkness;
        private Color _lastTint;
        private float _lastSkyOverrideStrength;
        private bool _lastIsNight;

        public static ZoneAtmosphere Attach(GameObject host, ZoneDef zone)
        {
            var a = host.GetComponent<ZoneAtmosphere>();
            if (a == null) a = host.AddComponent<ZoneAtmosphere>();
            a._zone = zone;
            SkyOverrideStrength = 0f;
            return a;
        }

        private void LateUpdate()
        {
            if (_zone == null) return;
            // Re-read whatever the clock and the weather wrote this frame (or keep the last base when they didn't).
            // Initialize cached singleton references if needed
            if (_tod == null || _gs == null || _ss == null)
            {
                _tod = TimeOfDay.Instance;
                _gs = GameSettings.Current;
                _ss = SandboxSettings.Current;
            }
            // Update base render settings when they change externally
            // Capture current render settings
            float curStart = RenderSettings.fogStartDistance;
            float curEnd = RenderSettings.fogEndDistance;
            Color curAmbient = RenderSettings.ambientLight;
            Color curFogColor = RenderSettings.fogColor;
            // Update base values if they have changed since last frame
            if (!Mathf.Approximately(curStart, _lastStart)) _baseStart = curStart;
            if (!Mathf.Approximately(curEnd, _lastEnd)) _baseEnd = curEnd;
            if (curAmbient != _lastAmbient) _baseAmbient = curAmbient;
            if (curFogColor != _lastFogColor) _baseFogColor = curFogColor;

            // Determine if any inputs changed since last frame
            float curFogDensity = _zone.FogDensity;
            float curFogReadability = _gs.FogReadability;
            float curNightDarkness = _ss.NightDarkness;
            Color curTint = _zone.Tint;
            float curSkyOverrideStrength = SkyOverrideStrength;
            bool curIsNight = _tod != null && _tod.IsNight;

            bool inputsUnchanged =
                Mathf.Approximately(curFogDensity, _lastFogDensity) &&
                Mathf.Approximately(curFogReadability, _lastFogReadability) &&
                Mathf.Approximately(curNightDarkness, _lastNightDarkness) &&
                curTint == _lastTint &&
                Mathf.Approximately(curSkyOverrideStrength, _lastSkyOverrideStrength) &&
                curIsNight == _lastIsNight &&
                Mathf.Approximately(curStart, _lastStart) &&
                Mathf.Approximately(curEnd, _lastEnd) &&
                curAmbient == _lastAmbient &&
                curFogColor == _lastFogColor;

            if (inputsUnchanged) return; // early‑out, nothing changed

            float fog = Mathf.Clamp01(curFogDensity * (1f - curFogReadability * 0.75f));
            float end = Mathf.Lerp(_baseEnd, Mathf.Min(_baseEnd, 24f), fog);
            float start = Mathf.Lerp(_baseStart, 1.5f, fog);
            if (_gs.FogReadability > 0f) end = Mathf.Max(end, Mathf.Lerp(end, 70f, _gs.FogReadability * 0.5f));

            Color ambient = _baseAmbient;

            if (curIsNight) ambient *= Mathf.Lerp(1.15f, 0.4f, curNightDarkness);
            ambient = Multiply(ambient, curTint);
            Color fogColor = Multiply(_baseFogColor, curTint);
            if (SkyOverrideStrength > 0f)
            {
                ambient = Color.Lerp(ambient, SkyOverride * 0.55f, SkyOverrideStrength);
                fogColor = Color.Lerp(fogColor, SkyOverride * 0.4f, SkyOverrideStrength);
                end = Mathf.Lerp(end, Mathf.Min(end, 40f), SkyOverrideStrength);
            }

            RenderSettings.fogStartDistance = _lastStart = start;
            RenderSettings.fogEndDistance = _lastEnd = end;
            RenderSettings.ambientLight = _lastAmbient = ambient;
            RenderSettings.fogColor = _lastFogColor = fogColor;

            // Cache input values for next frame
            _lastFogDensity = curFogDensity;
            _lastFogReadability = curFogReadability;
            _lastNightDarkness = curNightDarkness;
            _lastTint = curTint;
            _lastSkyOverrideStrength = curSkyOverrideStrength;
            _lastIsNight = curIsNight;
        }

        private static Color Multiply(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
    }
}
