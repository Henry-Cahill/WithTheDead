using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The small crosshair at the screen centre in the shoulder and first-person views: a dot and four
    /// ticks, built in code on its own overlay canvas (sort order 101, just above the HUD) by
    /// <see cref="IsoCameraFollow"/>. It shows only while mouse-look has the mouse
    /// (<see cref="IsoCameraFollow.MouseLookEngaged"/>) — so never in the isometric view, under a menu,
    /// asleep or dead. It turns red over a living zombie and kicks open for a moment with each shot.
    /// </summary>
    [DefaultExecutionOrder(60)]   // after the camera has decided whether mouse-look is engaged this frame
    [DisallowMultipleComponent]
    public class ViewCrosshair : MonoBehaviour
    {
        /// <summary>Canvas sort order: just above the HUD (100), below the inventory (105).</summary>
        public const int SortOrder = 101;

        public Color Tint = new Color(0.95f, 0.95f, 0.9f, 0.9f);
        public Color OnZombieTint = new Color(1f, 0.33f, 0.27f, 0.95f);
        [Tooltip("Gap between the centre and each tick, in reference pixels (1920x1080).")]
        public float Gap = 6f;
        public float TickLength = 7f;
        public float Thickness = 2f;
        public float DotSize = 3f;
        [Tooltip("How far the ticks jump open on a shot.")]
        public float FireKick = 7f;

        private Canvas _canvas;
        private readonly Image[] _parts = new Image[5];   // dot, up, down, left, right
        private float _kick;
        private float _laidOutGap = -1f;
        private Color _shownTint;

        private Transform _boundTarget;
    private PlayerController _controller;
    private IsoCameraFollow _cam;
    private PlayerCombat _combat;
    private GameSettings _settings;



        /// <summary>Builds the crosshair under <paramref name="parent"/> (the camera).</summary>
        public static ViewCrosshair Create(Transform parent)
        {
            var go = new GameObject("Crosshair");
            go.transform.SetParent(parent, false);
            return go.AddComponent<ViewCrosshair>();
        }

        private void Awake()
    {
        Build();
        _cam = IsoCameraFollow.Instance;
        _settings = GameSettings.Current;
    }

        private void LateUpdate()
        {
            // No crosshair through the bodycam, or in hardcore: aim with the weapon itself.
            bool show = IsoCameraFollow.MouseLookEngaged && ViewState.Current != CameraView.Bodycam && _settings.ShowCrosshair;
            if (_canvas.enabled != show) _canvas.enabled = show;
            if (!show)
            {
                _kick = 0f;
                return;
            }

            Bind();
            _kick = Mathf.MoveTowards(_kick, 0f, Time.unscaledDeltaTime * 45f);
            Layout(Gap + _kick);
            ZombieHealth zombie = _controller != null ? _controller.CrosshairZombie : null;
            SetTint(zombie != null ? OnZombieTint : Tint);
        }

        private void OnDestroy() => Unbind();

        // Follows the camera's survivor, to read their aim and hear their gun.
        private void Bind()
        {
            IsoCameraFollow cam = _cam;
            Transform target = cam != null ? cam.Target : null;
            if (ReferenceEquals(target, _boundTarget)) return;
            Unbind();
            _boundTarget = target;
            _controller = target != null ? target.GetComponent<PlayerController>() : null;
            _combat = target != null ? target.GetComponent<PlayerCombat>() : null;
            if (_combat != null) _combat.OnFire += HandleFire;
        }

        private void Unbind()
        {
            if (_combat != null) _combat.OnFire -= HandleFire;
            _combat = null;
            _controller = null;
            _boundTarget = null;
        }

        private void HandleFire() => _kick = FireKick;

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortOrder;
            _canvas.pixelPerfect = true;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            // No GraphicRaycaster: the crosshair never takes a click.

            RectTransform root = _canvas.GetComponent<RectTransform>();
            for (int i = 0; i < _parts.Length; i++)
            {
                var part = new GameObject(i == 0 ? "Dot" : "Tick");
                var rt = part.AddComponent<RectTransform>();
                rt.SetParent(root, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                var image = part.AddComponent<Image>();
                image.raycastTarget = false;
                var outline = part.AddComponent<Outline>();   // a dark rim keeps it readable on snow and in the dark
                outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
                outline.effectDistance = new Vector2(1f, -1f);
                _parts[i] = image;
            }
            _parts[0].rectTransform.sizeDelta = new Vector2(DotSize, DotSize);
            _canvas.enabled = false;
            SetTint(Tint);
            Layout(Gap);
        }

        private void Layout(float gap)
        {
            if (Mathf.Abs(gap - _laidOutGap) < 0.01f) return;
            _laidOutGap = gap;
            float c = gap + TickLength * 0.5f;
            Place(1, new Vector2(0f, c), new Vector2(Thickness, TickLength));
            Place(2, new Vector2(0f, -c), new Vector2(Thickness, TickLength));
            Place(3, new Vector2(-c, 0f), new Vector2(TickLength, Thickness));
            Place(4, new Vector2(c, 0f), new Vector2(TickLength, Thickness));
        }

        private void Place(int index, Vector2 position, Vector2 size)
        {
            RectTransform rt = _parts[index].rectTransform;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private void SetTint(Color color)
        {
            if (color == _shownTint) return;
            _shownTint = color;
            for (int i = 0; i < _parts.Length; i++) _parts[i].color = color;
        }
    }
}
