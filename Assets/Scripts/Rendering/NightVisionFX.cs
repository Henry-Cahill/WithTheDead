using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The night-vision goggles' look (reference photo 2): a runtime global URP volume that turns the
    /// picture into grainy phosphor green — brighter, contrasty, a round vignette centred on the survivor
    /// and a little bloom — plus a scope overlay on its own canvas (sort order 90, under the HUD): clear
    /// around the survivor, green-black beyond, with faint scanlines and sensor noise. It blends with
    /// <see cref="VisionController.NightVisionBlend"/>; the controller also lifts the darkness so the
    /// goggles have something to amplify, still respecting line of sight. Ticked by the controller.
    ///
    /// The green comes from the channel mixer (every channel = luminance, weighted to phosphor green)
    /// rather than colour filter + saturation -100: URP applies saturation after the colour filter and
    /// channel mixer, so a full desaturate would grey the tint out again.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NightVisionFX : MonoBehaviour
    {
        /// <summary>Sort order of the scope overlay canvas (the HUD is 100).</summary>
        public const int OverlaySortingOrder = 90;
        private const float VolumePriority = 50f;      // the scene's Global Volume is 0

        // Post-processing at full strength.
        private const float NightExposure = 1.3f;      // EV added at full night
        private const float DayExposure = 0.1f;        // in daylight the tubes are near saturation already
        private const float BatteryDipExposure = 2f;   // EV lost in a low-battery flicker
        private const float Contrast = 22f;
        private static readonly Color Filter = new Color(0.85f, 1f, 0.88f);
        private static readonly Vector3 Luma = new Vector3(0.30f, 0.59f, 0.11f);
        private static readonly Color Phosphor = new Color(0.30f, 1.25f, 0.38f);
        private const float VignetteIntensity = 0.48f;
        private const float VignetteSmoothness = 0.45f;
        private static readonly Color VignetteColor = new Color(0f, 0.025f, 0.008f);
        private const FilmGrainLookup GrainType = FilmGrainLookup.Medium3;
        private const float GrainIntensity = 0.6f;
        private const float GrainResponse = 0.55f;
        private const float BloomIntensity = 0.55f;
        private const float BloomThreshold = 0.75f;
        private const float BloomScatter = 0.65f;
        private static readonly Color BloomTint = new Color(0.6f, 1f, 0.65f);

        // Scope overlay.
        private const float ScopeRadius = 0.4f;         // of screen height, to the middle of the soft rim
        private static readonly Color ScopeColor = new Color(0.008f, 0.04f, 0.016f, 0.86f);
        private const float ScanlineAlpha = 0.16f;
        private const float ScanlineScroll = 6f;        // screen pixels per second
        private static readonly Color NoiseColor = new Color(0.55f, 1f, 0.6f, 0.07f);
        private const float NoisePixel = 2f;            // screen pixels per noise texel
        private const float SurvivorScreenHeight = 1.1f;// the scope centres on this point of the survivor
        private const float ThirdPersonCentreBias = 0.5f;

        // Low battery flicker.
        private const float LowCharge = 0.1f;

        private Volume _volume;
        private VolumeProfile _profile;
        private ColorAdjustments _color;
        private Vignette _vignette;

        private Canvas _canvas;
        private CanvasGroup _group;
        private RectTransform _scope, _left, _right, _bottom, _top;
        private RawImage _scan, _noise;
        private float _scanOffset;
        private float _dipLeft, _dipDepth, _dipTimer;
        private bool _shown;

        /// <summary>The survivor's position on screen the scope is centred on (pixels), as of the last tick.</summary>
        public Vector2 ScopeCentre { get; private set; }

        /// <summary>Builds the volume and the overlay (called once by the vision controller).</summary>
        internal void Build()
        {
            BuildVolume();
            BuildOverlay();
            Show(false);
        }

        /// <summary>Creates the post‑processing volume profile for night‑vision.</summary>
        private void BuildVolume()
        {
            // Create a fresh VolumeProfile to hold all post‑processing settings for night‑vision.
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "NightVisionProfile";

            // Configure base colour correction to achieve the characteristic green tint and contrast.
            _color = _profile.Add<ColorAdjustments>();
            _color.postExposure.Override(NightExposure);
            _color.contrast.Override(Contrast);
            _color.colorFilter.Override(Filter);

            // Use a channel mixer to remap all colour channels toward phosphor‑green for the night‑vision look.
            var mixer = _profile.Add<ChannelMixer>();
            mixer.redOutRedIn.Override(Phosphor.r * Luma.x * 100f);
            mixer.redOutGreenIn.Override(Phosphor.r * Luma.y * 100f);
            mixer.redOutBlueIn.Override(Phosphor.r * Luma.z * 100f);
            mixer.greenOutRedIn.Override(Phosphor.g * Luma.x * 100f);
            mixer.greenOutGreenIn.Override(Phosphor.g * Luma.y * 100f);
            mixer.greenOutBlueIn.Override(Phosphor.g * Luma.z * 100f);
            mixer.blueOutRedIn.Override(Phosphor.b * Luma.x * 100f);
            mixer.blueOutGreenIn.Override(Phosphor.b * Luma.y * 100f);
            mixer.blueOutBlueIn.Override(Phosphor.b * Luma.z * 100f);

            // Add a vignette to darken the periphery and focus attention on the centre.
            _vignette = _profile.Add<Vignette>();
            _vignette.color.Override(VignetteColor);
            _vignette.center.Override(new Vector2(0.5f, 0.5f));
            _vignette.intensity.Override(VignetteIntensity);
            _vignette.smoothness.Override(VignetteSmoothness);
            _vignette.rounded.Override(true);

            // Add film grain to simulate CRT‑style sensor noise.
            var grain = _profile.Add<FilmGrain>();
            grain.type.Override(GrainType);
            grain.intensity.Override(GrainIntensity);
            grain.response.Override(GrainResponse);

            // Add bloom to give the image a soft glow around bright areas.
            var bloom = _profile.Add<Bloom>();
            bloom.intensity.Override(BloomIntensity);
            bloom.threshold.Override(BloomThreshold);
            bloom.scatter.Override(BloomScatter);
            bloom.tint.Override(BloomTint);

            // Attach a global Volume component to apply the profile everywhere.
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = VolumePriority;
            _volume.weight = 0f;
            _volume.sharedProfile = _profile;
        }

        /// <summary>Creates the UI overlay canvas and its components for the night‑vision scope.</summary>
        private void BuildOverlay()
        {
            var canvasGo = new GameObject("NightVisionOverlay");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = OverlaySortingOrder;
            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _group.alpha = 0f;
            var root = (RectTransform)canvasGo.transform;

            // No CanvasScaler: one canvas unit is one screen pixel, so the scope can follow the survivor
            // with plain screen coordinates.
            _scope = NewImage("Scope", root, VisionAssets.ScopeSprite(), new Vector2(0.5f, 0.5f));
            _left = NewImage("MaskLeft", root, null, Vector2.zero);
            _right = NewImage("MaskRight", root, null, Vector2.zero);
            _bottom = NewImage("MaskBottom", root, null, Vector2.zero);
            _top = NewImage("MaskTop", root, null, Vector2.zero);
            _scan = NewFullScreen("Scanlines", root, VisionAssets.Scanlines(), new Color(1f, 1f, 1f, ScanlineAlpha));
            _noise = NewFullScreen("Noise", root, VisionAssets.Noise(), NoiseColor);
        }

        /// <summary>Creates a UI Image for a single sprite, positioned and pivoted as specified.</summary>
        private static RectTransform NewImage(string name, RectTransform parent, Sprite sprite, Vector2 pivot)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = pivot;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = ScopeColor;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>Creates a full‑screen RawImage for textures that scroll or tile (scanlines, noise).</summary>
        private static RawImage NewFullScreen(string name, RectTransform parent, Texture texture, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var raw = go.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.raycastTarget = false;
            return raw;
        }

        /// <summary>Blends the effect with the controller's night-vision blend and lays out the scope.</summary>
        internal void Tick(VisionController vision, float dt)
        {
            // Exit early if the Volume hasn't been built – should only happen during editor shutdown.
            if (_volume == null) return;
            // Pull the current blend amount from the VisionController; if it's negligible, hide the effect.
            float blend = vision.NightVisionBlend;
            if (blend <= 0.001f)
            {
                // When the effect is off, ensure the overlay is hidden and skip further work.
                if (_shown) Show(false);
                return;
            }
            // When turning on for the first time, enable the overlay.
            if (!_shown) Show(true);

            // Update the global volume weight and UI opacity to match the blend amount.
            _volume.weight = blend;
            _group.alpha = blend;
            // Blend day‑time and night‑time exposure, then apply a dip for low‑battery flicker.
            float exposure = Mathf.Lerp(DayExposure, NightExposure, vision.Night01);
            exposure -= BatteryDipExposure * (1f - BatteryFlicker(dt));
            _color.postExposure.value = exposure;

            // Convert screen size to width/height values for later layout calculations.
            float w = Screen.width, h = Screen.height;
            // Position the scope based on the survivor's screen location and update the vignette centre.
            Vector2 c = SurvivorOnScreen(vision, w, h);
            ScopeCentre = c;
            _vignette.center.value = new Vector2(c.x / Mathf.Max(1f, w), c.y / Mathf.Max(1f, h));
            // Position the scope and its masks, then animate grain and noise.
            LayoutScope(c, w, h);
            AnimateGrain(dt, w, h);
        }

        /// <summary>Enables or disables the volume and overlay, resetting weights when hidden.</summary>
        private void Show(bool show)
        {
            _shown = show;
            if (_volume != null)
            {
                _volume.enabled = show;
                if (!show) _volume.weight = 0f;
            }
            if (_canvas != null) _canvas.enabled = show;
            if (!show && _group != null) _group.alpha = 0f;
        }

        // The scope sits on the survivor in the isometric view, near them over the shoulder and in the
        // middle of the screen in first person.
        private Vector2 SurvivorOnScreen(VisionController vision, float w, float h)
        {
            var centre = new Vector2(w * 0.5f, h * 0.5f);
            Camera cam = vision.Camera;
            Transform player = vision.PlayerTransform;
            if (cam == null || player == null || ViewState.Current == CameraView.FirstPerson) return centre;
            Vector3 sp = cam.WorldToScreenPoint(player.position + Vector3.up * SurvivorScreenHeight);
            if (sp.z <= 0f) return centre;
            var p = new Vector2(sp.x, sp.y);
            if (ViewState.Current == CameraView.ThirdPerson) p = Vector2.Lerp(centre, p, ThirdPersonCentreBias);
            return p;
        }

        /// <summary>Positions and sizes the scope image and its mask rectangles around the survivor.</summary>
        private void LayoutScope(Vector2 c, float w, float h)
        {
            // Half size of the scope image, so the middle of its soft rim lands on ScopeRadius.
            float half = ScopeRadius * h / ((VisionAssets.ScopeClearRadius + 1f) * 0.5f);
            _scope.anchoredPosition = c;
            _scope.sizeDelta = new Vector2(half * 2f, half * 2f);

            const float pad = 8f;
            float x0 = c.x - half, x1 = c.x + half, y0 = c.y - half, y1 = c.y + half;
            SetRect(_left, -pad, -pad, x0, h + pad);
            SetRect(_right, x1, -pad, w + pad, h + pad);
            SetRect(_bottom, x0, -pad, x1, y0);
            SetRect(_top, x0, y1, x1, h + pad);
        }

        private static void SetRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchoredPosition = new Vector2(xMin, yMin);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
        }

        /// <summary>Animates scanline scrolling and random noise texture offsets each frame.</summary>
        private void AnimateGrain(float dt, float w, float h)
        {
            // Scanlines: the 4-texel pattern tiled one texel per pixel, creeping slowly upwards.
            _scanOffset = Mathf.Repeat(_scanOffset + dt * ScanlineScroll / 4f, 1f);
            _scan.uvRect = new Rect(0f, _scanOffset, w / 4f, h / 4f);

            // Sensor noise: the speckle texture jumps to a new random offset every frame.
            if (dt > 0f)
            {
                float tiles = 128f * NoisePixel;
                _noise.uvRect = new Rect(Random.value, Random.value, w / tiles, h / tiles);
            }
        }

        // 1 normally; with the goggles' battery nearly flat the picture now and then browns out.
        private float BatteryFlicker(float dt)
        {
            PlayerGear gear = PlayerGear.Instance;
            // If the player gear is missing or night‑vision is off or charge is sufficient, reset dip state.
            if (gear == null || !gear.NightVisionOn || gear.NightVisionCharge01 >= LowCharge)
            {
                _dipLeft = 0f;
                return 1f;
            }
            if (_dipLeft > 0f)
            {
                _dipLeft -= dt;
                return _dipDepth;
            }
            _dipTimer -= dt;
            if (_dipTimer <= 0f)
            {
                _dipLeft = Random.Range(0.03f, 0.09f);
                _dipDepth = Random.Range(0.35f, 0.7f);
                _dipTimer = Random.Range(0.3f, 2.5f);
            }
            return 1f;
        }

        private void OnDestroy()
        {
            if (_profile == null) return;
            for (int i = 0; i < _profile.components.Count; i++)
                if (_profile.components[i] != null) Destroy(_profile.components[i]);
            Destroy(_profile);
        }
    }
}
