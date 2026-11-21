using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The light of a lamp by a front door: lit after dark, out by day, with a faint flicker; never shadowed, and off
    /// beyond <see cref="CullDistance"/> of the survivor, so a street of porch lights stays cheap. Built lit (a map
    /// built at night, or in the editor, shows them on); the lamps keep no Update of their own: one driver works them all
    /// a few times a second, and flickers only the lit ones.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PorchLamp : MonoBehaviour
    {
        /// <summary>Lamps farther than this from the survivor are switched off.</summary>
        internal const float CullDistance = 42f;
        private const float FlickerSpeed = 8f;   // how fast the flicker's noise runs
        private const float FlickerDepth = 0.1f;   // the flicker's swing, as a fraction of the lamp's output
        private const float CheckInterval = 0.25f;

        private static readonly List<PorchLamp> s_All = new List<PorchLamp>();
        private static PorchLampDriver s_Driver;
        private static float s_NextCheck;
        private static Transform s_Survivor;

        private Light _light;
        private float _intensity, _phase;

        /// <summary>A porch lamp's light at <paramref name="at"/> (world) under <paramref name="parent"/>;
        /// <paramref name="phase"/> keeps it from flickering in step with its neighbours.</summary>
        internal static PorchLamp Spawn(Transform parent, Vector3 at, float phase)
        {
            var go = new GameObject("PorchLamp");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.82f, 0.56f);
            light.intensity = 1.8f;
            light.range = 5.5f;
            light.shadows = LightShadows.None;
            var lamp = go.AddComponent<PorchLamp>();
            lamp._light = light;
            lamp._intensity = light.intensity;
            lamp._phase = phase;
            return lamp;
        }

        private void OnEnable()
        {
            s_All.Add(this);
            s_NextCheck = 0f;
            if (s_Driver == null) s_Driver = new GameObject("PorchLamps").AddComponent<PorchLampDriver>();
        }

        private void OnDisable() => s_All.Remove(this);

        /// <summary>Lights or puts out every porch lamp for the time of day and the survivor's whereabouts, and flickers
        /// the lit ones.</summary>
        internal static void TickAll()
        {
            float time = Time.time;
            if (time >= s_NextCheck)
            {
                s_NextCheck = time + CheckInterval;
                TimeOfDay day = TimeOfDay.Instance;
                bool night = day == null || day.IsNight;
                bool found = TryGetSurvivor(out Vector3 at);
                float cull = CullDistance * CullDistance;
                for (int i = 0; i < s_All.Count; i++)
                {
                    PorchLamp l = s_All[i];
                    if (l._light == null) continue;
                    bool on = night && (!found || (l.transform.position - at).sqrMagnitude < cull);
                    if (l._light.enabled != on) l._light.enabled = on;
                }
            }
            for (int i = 0; i < s_All.Count; i++)
            {
                PorchLamp l = s_All[i];
                if (l._light == null || !l._light.enabled) continue;
                l._light.intensity = l._intensity * (1f - FlickerDepth * 0.5f + FlickerDepth * Mathf.PerlinNoise((time + l._phase) * FlickerSpeed, 0f));
            }
        }

        private static bool TryGetSurvivor(out Vector3 position)
        {
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Player != null) s_Survivor = gm.Player.transform;
            else if (s_Survivor == null && Camera.main != null) s_Survivor = Camera.main.transform;
            if (s_Survivor == null)
            {
                position = default;
                return false;
            }
            position = s_Survivor.position;
            return true;
        }
    }

    /// <summary>Works every <see cref="PorchLamp"/>, once a frame; made with the first lamp, gone with the scene.</summary>
    internal sealed class PorchLampDriver : MonoBehaviour
    {
        private void Update() => PorchLamp.TickAll();
    }
}
