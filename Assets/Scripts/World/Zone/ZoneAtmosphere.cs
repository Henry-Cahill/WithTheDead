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
            if (!Mathf.Approximately(RenderSettings.fogStartDistance, _lastStart)) _baseStart = RenderSettings.fogStartDistance;
            if (!Mathf.Approximately(RenderSettings.fogEndDistance, _lastEnd)) _baseEnd = RenderSettings.fogEndDistance;
            if (RenderSettings.ambientLight != _lastAmbient) _baseAmbient = RenderSettings.ambientLight;
            if (RenderSettings.fogColor != _lastFogColor) _baseFogColor = RenderSettings.fogColor;

            float fog = Mathf.Clamp01(_zone.FogDensity * (1f - _gs.FogReadability * 0.75f));
            float end = Mathf.Lerp(_baseEnd, Mathf.Min(_baseEnd, 24f), fog);
            float start = Mathf.Lerp(_baseStart, 1.5f, fog);
            if (_gs.FogReadability > 0f) end = Mathf.Max(end, Mathf.Lerp(end, 70f, _gs.FogReadability * 0.5f));

            Color ambient = _baseAmbient;
            TimeOfDay tod = _tod;
            if (tod != null && tod.IsNight) ambient *= Mathf.Lerp(1.15f, 0.4f, _ss.NightDarkness);
            ambient = Multiply(ambient, _zone.Tint);
            Color fogColor = Multiply(_baseFogColor, _zone.Tint);
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
        }

        private static Color Multiply(Color a, Color b) => new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
    }
}
