using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CloneZ
{
    /// <summary>
    /// The one manager of the punctual lights' shadows. URP packs every shadowed spot light (one shadow map) and point
    /// light (six) into a single atlas; asked for more than fits, it shrinks every map for that frame (or drops some),
    /// which blurs exactly the dark rooms the lamps light. So no light switches its own shadows on: each one that may cast
    /// them carries a <see cref="ShadowedLight"/>, and a few times a second, or as soon as one comes, goes or changes its
    /// mind, the budget hands them out:
    /// <list type="bullet">
    /// <item>the survivor's own lights (<see cref="ShadowedLight.Pinned"/>: the sight cone, the flashlight) first, their
    /// atlas space held even while they're off, so switching one on never overfills the atlas;</item>
    /// <item>then the lamps that want them, nearest the survivor first, up to the quality's count
    /// (<see cref="LampsFor"/>) and only while their maps still fit the atlas at full size. A lamp the survivor can't see
    /// ranks as if it were <see cref="UnseenFactor"/> times as far (it isn't left out: its light still reaches through a
    /// wall, which only its shadows stop), and one already casting shadows a little nearer, so two lamps at much the same
    /// distance don't trade them back and forth. Lamps outside the camera's view get none (URP would skip them anyway).</item>
    /// </list>
    /// The shadow quality (<see cref="GameSettings.ShadowQuality"/>) sets the lamps' count, edges and resolution and, on
    /// High with the video memory for it, a 4096 atlas. In the isometric view the sun's shadows take
    /// <see cref="IsoCascades"/> cascades instead of the asset's four. Both change the shared pipeline asset, whose own
    /// values are put back when play ends.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class ShadowBudget : MonoBehaviour
    {
        private const float Interval = 0.2f;
        private const float MoveReplan = 1.5f;      // a survivor this far from the last plan gets a new one at once
        /// <summary>Lamps farther than this from the survivor get no shadows.</summary>
        public const float LampReach = 40f;
        /// <summary>A lamp the survivor can't see ranks as if it were this many times as far away.</summary>
        public const float UnseenFactor = 2f;
        private const float KeepFactor = 0.85f;     // a lamp already casting shadows ranks this much nearer
        private const float EyeHeight = 1.6f;
        private const float SightClearance = 0.3f;  // sight lines stop this short of a lamp, clear of its own fitting
        private const int BigAtlas = 4096;
        private const int BigAtlasMemoryMB = 4000;  // video memory High wants before it takes the 4096 atlas
        /// <summary>The sun's shadow cascades in the isometric view. Its orthographic camera looks down from 16-25 m back,
        /// so the nearer two of the asset's four cover little but air; two, the first ending just past the survivor, draw
        /// the same ground in half the passes.</summary>
        public const int IsoCascades = 2;
        private const float IsoSplitPast = 5f;      // the first iso cascade ends this far beyond the survivor

        private struct Candidate
        {
            public ShadowedLight Light;
            public float Key;
            public long Cost;
            public int Tier;
        }

        private static readonly List<ShadowedLight> s_Lights = new List<ShadowedLight>();
        private static readonly List<Candidate> s_Candidates = new List<Candidate>(64);
        private static readonly Comparison<Candidate> s_ByKey = (a, b) => a.Key.CompareTo(b.Key);
        private static readonly Plane[] s_Planes = new Plane[6];
        private static ShadowBudget s_Driver;
        private static bool s_Dirty = true;
        private static bool s_Quitting;
        private static UniversalRenderPipelineAsset s_Asset;   // the asset whose atlas size High changed
        private static int s_AssetAtlas;                        // and its own size, put back after
        private static UniversalRenderPipelineAsset s_CascadeAsset;   // the asset whose cascades the iso view changed
        private static int s_AssetCascades;                            // and its own count and two-cascade split
        private static float s_AssetSplit;

        private float _next;
        private Vector3 _planned;

        /// <summary>Every light under the budget.</summary>
        public static IReadOnlyList<ShadowedLight> Lights => s_Lights;
        /// <summary>The lamps the last plan let cast shadows (the survivor's own lights aside).</summary>
        public static int LampsShadowed { get; private set; }
        /// <summary>The atlas texels the last plan handed out, all within <see cref="AtlasSize"/> squared.</summary>
        public static long TexelsUsed { get; private set; }
        /// <summary>The additional lights' shadow atlas size the last plan worked to.</summary>
        public static int AtlasSize { get; private set; }

        /// <summary>How many lamps may cast shadows at once at <paramref name="quality"/>.</summary>
        public static int LampsFor(ShadowQualityLevel quality) =>
            quality == ShadowQualityLevel.Low ? 2 : quality == ShadowQualityLevel.High ? 8 : 4;

        /// <summary>Plans the shadows again before the next frame renders (a light switched, the setting changed).</summary>
        public static void Refresh() => s_Dirty = true;

        internal static void Add(ShadowedLight light)
        {
            if (light.Index >= 0) return;
            light.Index = s_Lights.Count;
            s_Lights.Add(light);
            s_Dirty = true;
            if (s_Driver == null && Application.isPlaying && !s_Quitting)
            {
                var go = new GameObject("ShadowBudget");
                DontDestroyOnLoad(go);
                s_Driver = go.AddComponent<ShadowBudget>();
            }
        }

        internal static void Remove(ShadowedLight light)
        {
            int i = light.Index;
            if (i < 0 || i >= s_Lights.Count || s_Lights[i] != light) return;
            int last = s_Lights.Count - 1;
            ShadowedLight moved = s_Lights[last];
            s_Lights[i] = moved;
            moved.Index = i;
            s_Lights.RemoveAt(last);
            light.Index = -1;
            s_Dirty = true;
        }

        // Play mode keeps statics from one session to the next (no domain reload): start each one clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            foreach (ShadowedLight l in s_Lights)
                if (l != null) l.Index = -1;
            s_Lights.Clear();
            s_Candidates.Clear();
            s_Driver = null;
            s_Dirty = true;
            s_Quitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
            s_Asset = null;
            s_CascadeAsset = null;
            LampsShadowed = 0;
            TexelsUsed = 0;
            AtlasSize = 0;
        }

        // Quitting (or leaving play mode): no new driver while everything is torn down, and the asset's own values back.
        private static void OnQuitting()
        {
            s_Quitting = true;
            PutBackAtlas();
            PutBackCascades();
        }

        private void OnDestroy()
        {
            if (s_Driver == this) s_Driver = null;
            PutBackAtlas();
            PutBackCascades();
        }

        private void LateUpdate()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;   // a headless server draws nothing
            ShapeCascades(UniversalRenderPipeline.asset);
            bool survivor = Survivor(out Vector3 centre, out Vector3 eye);
            float now = Time.unscaledTime;
            if (!s_Dirty && now < _next && (centre - _planned).sqrMagnitude < MoveReplan * MoveReplan) return;
            s_Dirty = false;
            _next = now + Interval;
            _planned = centre;
            Plan(survivor, centre, eye);
        }

        private static void Plan(bool survivor, Vector3 centre, Vector3 eye)
        {
            ShadowQualityLevel quality = GameSettings.Current.ShadowQuality;
            UniversalRenderPipelineAsset urp = UniversalRenderPipeline.asset;
            int atlas = SizeAtlas(urp, quality);
            long left = (long)atlas * atlas;
            // Lamps' shadow maps: sharper on High (with the big atlas to hold them), hard-edged and coarse on Low.
            bool big = quality == ShadowQualityLevel.High && atlas >= BigAtlas;
            int pointTier = big ? UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium
                : UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;
            int spotTier = big ? UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh
                : quality == ShadowQualityLevel.Low ? UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow
                : UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;

            // The survivor's own lights first, their space kept whether they're on or not.
            for (int i = 0; i < s_Lights.Count; i++)
            {
                ShadowedLight s = s_Lights[i];
                if (s == null || !s.Pinned || s.Light == null) continue;
                long cost = Cost(urp, s.Light, s.Tier);
                bool fits = cost <= left;
                if (fits) left -= cost;
                s.Apply(fits && s.Wants, s.Shadows, -1);
            }

            // Then the lamps, nearest first.
            Camera cam = Camera.main;
            bool frustum = cam != null;
            if (frustum) GeometryUtility.CalculateFrustumPlanes(cam, s_Planes);
            s_Candidates.Clear();
            float reach = LampReach * LampReach;
            for (int i = 0; i < s_Lights.Count; i++)
            {
                ShadowedLight s = s_Lights[i];
                if (s == null || s.Pinned) continue;
                Light l = s.Light;
                if (l == null || !s.Wants || (s.OnlyWhileEnabled && !l.enabled) || (l.type != LightType.Point && l.type != LightType.Spot))
                {
                    s.Apply(false, LightShadows.None, -1);
                    continue;
                }
                Vector3 p = l.transform.position;
                float d = (p - centre).sqrMagnitude;
                if (d > reach || (frustum && !GeometryUtility.TestPlanesAABB(s_Planes, new Bounds(p, Vector3.one * (2f * l.range)))))
                {
                    s.Apply(false, LightShadows.None, -1);
                    continue;
                }
                float key = Mathf.Sqrt(d);
                if (survivor && !Seen(eye, p)) key *= UnseenFactor;
                if (s.Granted) key *= KeepFactor;
                int tier = l.type == LightType.Point ? pointTier : spotTier;
                s_Candidates.Add(new Candidate { Light = s, Key = key, Cost = Cost(urp, l, tier), Tier = tier });
            }
            s_Candidates.Sort(s_ByKey);
            int lamps = LampsFor(quality), granted = 0;
            for (int i = 0; i < s_Candidates.Count; i++)
            {
                Candidate c = s_Candidates[i];
                bool ok = granted < lamps && c.Cost <= left;
                if (ok)
                {
                    left -= c.Cost;
                    granted++;
                }
                c.Light.Apply(ok, quality == ShadowQualityLevel.Low ? LightShadows.Hard : c.Light.Shadows, ok ? c.Tier : -1);
            }
            s_Candidates.Clear();
            LampsShadowed = granted;
            AtlasSize = atlas;
            TexelsUsed = (long)atlas * atlas - left;
        }

        // A light's room in the atlas: its maps (six for a point light) at its tier's resolution.
        private static long Cost(UniversalRenderPipelineAsset urp, Light light, int tier)
        {
            long res = Resolution(urp, tier);
            return (light.type == LightType.Point ? 6L : 1L) * res * res;
        }

        private static int Resolution(UniversalRenderPipelineAsset urp, int tier)
        {
            if (urp == null) return 1024;
            if (tier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow) return urp.additionalLightsShadowResolutionTierLow;
            if (tier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium) return urp.additionalLightsShadowResolutionTierMedium;
            return urp.additionalLightsShadowResolutionTierHigh;   // High, and a custom resolution counted as High
        }

        // High takes a 4096 atlas on a GPU with the memory for it; anything else keeps the asset's own size.
        private static int SizeAtlas(UniversalRenderPipelineAsset urp, ShadowQualityLevel quality)
        {
            if (urp == null) return 2048;
            if (s_Asset != urp)
            {
                PutBackAtlas();
                s_Asset = urp;
                s_AssetAtlas = urp.additionalLightsShadowmapResolution;
            }
            int want = quality == ShadowQualityLevel.High && SystemInfo.graphicsMemorySize >= BigAtlasMemoryMB
                ? Mathf.Max(BigAtlas, s_AssetAtlas) : s_AssetAtlas;
            if (urp.additionalLightsShadowmapResolution != want) urp.additionalLightsShadowmapResolution = want;
            return want;
        }

        private static void PutBackAtlas()
        {
            if (s_Asset != null && s_Asset.additionalLightsShadowmapResolution != s_AssetAtlas)
                s_Asset.additionalLightsShadowmapResolution = s_AssetAtlas;
            s_Asset = null;
        }

        // The isometric view's sun shadows in IsoCascades cascades, the first ending just past the survivor; any other view
        // keeps the asset's own.
        private static void ShapeCascades(UniversalRenderPipelineAsset urp)
        {
            if (urp == null) return;
            if (s_CascadeAsset != urp)
            {
                PutBackCascades();
                s_CascadeAsset = urp;
                s_AssetCascades = urp.shadowCascadeCount;
                s_AssetSplit = urp.cascade2Split;
            }
            IsoCameraFollow iso = ViewState.Current == CameraView.Isometric ? IsoCameraFollow.Instance : null;
            int cascades = iso != null ? IsoCascades : s_AssetCascades;
            float split = iso != null ? Mathf.Clamp((iso.IsoDistance + IsoSplitPast) / Mathf.Max(1f, urp.shadowDistance), 0.2f, 0.8f) : s_AssetSplit;
            if (urp.shadowCascadeCount != cascades) urp.shadowCascadeCount = cascades;
            if (Mathf.Abs(urp.cascade2Split - split) > 0.002f) urp.cascade2Split = split;
        }

        private static void PutBackCascades()
        {
            if (s_CascadeAsset != null)
            {
                if (s_CascadeAsset.shadowCascadeCount != s_AssetCascades) s_CascadeAsset.shadowCascadeCount = s_AssetCascades;
                s_CascadeAsset.cascade2Split = s_AssetSplit;
            }
            s_CascadeAsset = null;
        }

        // The survivor (and their eyes), or failing that the camera.
        private static bool Survivor(out Vector3 centre, out Vector3 eye)
        {
            Actor me = Actor.LocalPlayer;
            if (me != null)
            {
                centre = me.transform.position;
                VisionController vision = VisionController.Active;
                eye = vision != null && vision.PlayerTransform == me.transform ? vision.EyePosition : centre + Vector3.up * EyeHeight;
                return true;
            }
            Camera cam = Camera.main;
            centre = eye = cam != null ? cam.transform.position : Vector3.zero;
            return false;
        }

        // True when the survivor can see the lamp's room: the spot under it at eye height (a ceiling lamp in the next room
        // shows through its doorway though the wall over the door hides the fitting), or else the lamp itself.
        private static bool Seen(Vector3 eye, Vector3 lamp)
        {
            if (lamp.y > eye.y && Clear(eye, new Vector3(lamp.x, eye.y, lamp.z))) return true;
            return Clear(eye, lamp);
        }

        private static bool Clear(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float length = d.magnitude;
            if (length <= SightClearance) return true;
            return !Physics.Raycast(from, d / length, length - SightClearance, Actor.SightMask, QueryTriggerInteraction.Ignore);
        }
    }
}
