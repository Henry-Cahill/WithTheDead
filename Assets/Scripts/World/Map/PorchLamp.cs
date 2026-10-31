using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        private static GameManager s_GameManager;
        private static Camera s_MainCam;
        // The nearest lamps found by a check, nearest first, and their squared distances.
        private static readonly int[] s_Near = new int[MaxLit];
        private static readonly float[] s_NearD = new float[MaxLit];

        private Light _light;
        private static Material s_PostMaterial, s_GlobeMaterial;

        // The post's and the globe's materials, made with the first lamp, in URP's Lit like every material the game makes.
        // Not in a static constructor: one runs inside the first AddComponent<PorchLamp>(), where Unity's API may not be
        // called, and it threw there, taking the whole piece's build (and with it the lobby after dark) down.
        private static void EnsureMaterials()
        {
            if (s_PostMaterial != null && s_GlobeMaterial != null) return;
            s_PostMaterial = CloneZUtil.Mat(new Color(0.22f, 0.15f, 0.10f), 0.3f);   // dark-brown wood
            var glass = new Color(1f, 0.82f, 0.56f);                                    // warm, glowing glass
            s_GlobeMaterial = new Material(CloneZUtil.LitShader) { name = "PorchLampGlobe", color = glass, enableInstancing = true };
            if (s_GlobeMaterial.HasProperty("_BaseColor")) s_GlobeMaterial.SetColor("_BaseColor", glass);
            if (s_GlobeMaterial.HasProperty("_Smoothness")) s_GlobeMaterial.SetFloat("_Smoothness", 0.9f);
            s_GlobeMaterial.EnableKeyword("_EMISSION");
            s_GlobeMaterial.SetColor("_EmissionColor", new Color(1f, 0.65f, 0.30f) * 2f);
        }
        private float _intensity, _phase;
        private bool _wanted;

        /// <summary>A porch lamp's light at <paramref name="at"/> (world) under <paramref name="parent"/>;
        /// <paramref name="phase"/> keeps it from flickering in step with its neighbours.</summary>
        internal static PorchLamp Spawn(Transform parent, Vector3 at, float phase)
        {
            EnsureMaterials();
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
            light.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            // Disable shadows for performance; porch lamps are not meant to cast shadows.
            light.shadows = LightShadows.None;
            // Add the PorchLamp component to bind behaviour to the GameObject.
            var lamp = go.AddComponent<PorchLamp>();
            // Cache the Light for fast access during updates.
            lamp._light = light;
            // Store the original intensity for later flicker modulation.
            lamp._intensity = light.intensity;
            // ------- visual post & globe -------
            float randScale = UnityEngine.Random.Range(0.9f, 1.1f);
            // create the wooden post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>()); // no collider needed
            post.transform.SetParent(go.transform, false);
            post.transform.localScale = new Vector3(0.07f, 0.5f, 0.07f) * randScale;
            post.transform.localPosition = new Vector3(0f, 0.5f, 0f); // pivot at centre, so bottom sits on ground
            post.GetComponent<Renderer>().sharedMaterial = s_PostMaterial;
            // create the glass globe
            GameObject globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.DestroyImmediate(globe.GetComponent<Collider>());
            globe.transform.SetParent(go.transform, false);
            globe.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f) * randScale;
            globe.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            globe.GetComponent<Renderer>().sharedMaterial = s_GlobeMaterial;
            globe.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            // Ensure objects are not static (avoid light‑map baking)
            post.isStatic = false;
            globe.isStatic = false;
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
            if (s_GameManager == null) s_GameManager = GameManager.Instance;
            GameManager gm = s_GameManager;
            if (gm != null && gm.Player != null) s_Survivor = gm.Player.transform;
            else {
                if (s_MainCam == null) s_MainCam = Camera.main;
                if (s_Survivor == null && s_MainCam != null) s_Survivor = s_MainCam.transform;
            }
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
