using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// The light of a lamp by a front door: lit after dark, out by day, with a faint flicker; never shadowed. However
    /// many porches a stretch of the streamed state has, only the <see cref="MaxLit"/> lamps nearest the survivor within
    /// <see cref="CullDistance"/> are lit (the rest still glow, see BuildingPass's lamp globes), so a street of porch lights
    /// stays cheap. Built lit (a map built at night, or in the editor, shows them on); the lamps keep no Update of their
    /// own: one driver works them all a few times a second, and flickers only the lit ones.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PorchLamp : MonoBehaviour
    {
        /// <summary>Lamps farther than this from the survivor are switched off.</summary>
        internal const float CullDistance = 42f;
        /// <summary>At most this many porch lamps are lit at once: the nearest.</summary>
        internal const int MaxLit = 14;
        private const float FlickerSpeed = 8f;   // how fast the flicker's noise runs
        private const float FlickerDepth = 0.1f;   // the flicker's swing, as a fraction of the lamp's output
        private const float CheckInterval = 0.25f;

        private static readonly List<PorchLamp> s_All = new List<PorchLamp>();
        private static PorchLampDriver s_Driver;
        private static float s_NextCheck;
        private static Transform s_Survivor;
        // The nearest lamps found by a check, nearest first, and their squared distances.
        private static readonly int[] s_Near = new int[MaxLit];
        private static readonly float[] s_NearD = new float[MaxLit];

        private Light _light;
        private float _intensity, _phase;
        private bool _wanted;

        /// <summary>A porch lamp's light at <paramref name="at"/> (world) under <paramref name="parent"/>;
        /// <paramref name="phase"/> keeps it from flickering in step with its neighbours.</summary>
        internal static PorchLamp Spawn(Transform parent, Vector3 at, float phase)
        {
            // Create a new GameObject to host the lamp instance.
            // Create a new GameObject to host the lamp instance.
            var go = new GameObject("PorchLamp");
            // Attach the lamp to its parent container without altering local transform.
            // Attach the lamp to its parent container without altering local transform.
            go.transform.SetParent(parent, false);
            // Position the lamp in world space at the requested coordinates.
            // Position the lamp in world space at the requested coordinates.
            go.transform.position = at;
            // Add a Light component to emit illumination.
            // Add a Light component to emit illumination.
            var light = go.AddComponent<Light>();
            // Set the light as a point source to radiate in all directions.
            // Set the light as a point source to radiate in all directions.
            light.type = LightType.Point;
            // Choose a warm hue matching a typical porch lamp.
            // Choose a warm hue matching a typical porch lamp.
            light.color = new Color(1f, 0.82f, 0.56f);
            // Set a base intensity that will later be modulated for flicker.
            // Set a base intensity that will later be modulated for flicker.
            light.intensity = 1.8f;
            // Define the radius within which the lamp can affect nearby objects.
            // Define the radius within which the lamp can affect nearby objects.
            light.range = 5.5f;
            // Disable shadows for performance; porch lamps are not meant to cast shadows.
            light.shadows = LightShadows.None;
            // Add the PorchLamp component to bind behaviour to the GameObject.
            var lamp = go.AddComponent<PorchLamp>();
            // Cache the Light for fast access during updates.
            lamp._light = light;
            // Store the original intensity for later flicker modulation.
            lamp._intensity = light.intensity;
            // Record the per‑lamp phase offset to desynchronize flicker between neighbours.
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

        /// <summary>Lights the porch lamps nearest the survivor after dark, puts out the rest, and flickers the lit ones.</summary>
        internal static void TickAll()
        {
            float time = Time.time;
            if (time >= s_NextCheck)
            {
                s_NextCheck = time + CheckInterval;
                Check();
            }
            for (int i = 0; i < s_All.Count; i++)
            {
                PorchLamp l = s_All[i];
                if (l._light == null || !l._light.enabled) continue;
                l._light.intensity = l._intensity * (1f - FlickerDepth * 0.5f + FlickerDepth * Mathf.PerlinNoise((time + l._phase) * FlickerSpeed, 0f));
            }
        }

        /// <summary>Evaluates which porch lamps should be on based on time of day and distance to survivor.</summary>
        private static void Check()
        {
            TimeOfDay day = TimeOfDay.Instance;
            bool night = day == null || day.IsNight;
            bool found = TryGetSurvivor(out Vector3 at);
            float cull = CullDistance * CullDistance;
            int near = 0;
            for (int i = 0; i < s_All.Count; i++)
            {
                PorchLamp l = s_All[i];
                l._wanted = false;
                if (!night || l._light == null) continue;
                float d = found ? (l.transform.position - at).sqrMagnitude : 0f;
                if (d >= cull) continue;
                // Keep the MaxLit nearest, nearest first (an insertion into a short list).
                if (near == MaxLit && d >= s_NearD[near - 1]) continue;
                int k = near < MaxLit ? near++ : near - 1;
                while (k > 0 && s_NearD[k - 1] > d)
                {
                    s_NearD[k] = s_NearD[k - 1];
                    s_Near[k] = s_Near[k - 1];
                    k--;
                }
                s_NearD[k] = d;
                s_Near[k] = i;
            }
            for (int k = 0; k < near; k++) s_All[s_Near[k]]._wanted = true;
            for (int i = 0; i < s_All.Count; i++)
            {
                PorchLamp l = s_All[i];
                if (l._light != null && l._light.enabled != l._wanted) l._light.enabled = l._wanted;
            }
        }

        /// <summary>Attempts to locate the survivor's position for lamp culling.</summary>
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
