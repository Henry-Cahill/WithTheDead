using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The look of the game on top of the cameras: a URP post-processing volume carrying the accessibility floor
    /// (brightness, optional motion blur), the bodycam's bent lens and grain (scaled by the lens-distortion setting),
    /// the grey of blood loss and the red edge of a bad wound, and the dark rim of a gas mask's visor.
    /// </summary>
    public sealed class PostFx : MonoBehaviour
    {
        private Volume _volume;
        private ColorAdjustments _color;
        private MotionBlur _blur;
        private LensDistortion _lens;
        private ChromaticAberration _chroma;
        private FilmGrain _grain;
        private Vignette _vignette;
        private PlayerBlood _blood;
        private PlayerStats _stats;
        private PlayerGear _gear;
        private Actor _for;
        private Camera _cachedCam;
        private UniversalAdditionalCameraData _cachedData;

        public static void Ensure()
        {
            if (FindAnyObjectByType<PostFx>() != null) return;
            new GameObject("[PostFx]").AddComponent<PostFx>();
        }

        private void Awake()
        {
            _volume = gameObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 50f;
            VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
            _color = p.Add<ColorAdjustments>(true);
            _blur = p.Add<MotionBlur>(true);
            _lens = p.Add<LensDistortion>(true);
            _chroma = p.Add<ChromaticAberration>(true);
            _grain = p.Add<FilmGrain>(true);
            _vignette = p.Add<Vignette>(true);
            _volume.sharedProfile = p;
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            // Update cached camera and URP data only when needed
            if (cam != _cachedCam || _cachedData == null)
            {
                _cachedCam = cam;
                if (cam != null)
                {
                    var data = cam.GetComponent<UniversalAdditionalCameraData>();
                    if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    _cachedData = data;
                }
                else
                {
                    _cachedData = null;
                }
            }
            if (_cachedData != null && !_cachedData.renderPostProcessing) _cachedData.renderPostProcessing = true;
            Actor me = Actor.LocalPlayer;
            if (me != _for)
            {
                _for = me;
                _blood = me != null ? me.GetComponent<PlayerBlood>() : null;
                _stats = me != null ? me.GetComponent<PlayerStats>() : null;
                _gear = me != null ? me.GetComponent<PlayerGear>() : null;
            }
            GameSettings g = GameSettings.Current;
            bool body = ViewState.Current == CameraView.Bodycam;
            bool eyes = ViewState.FirstPersonLike;

            _color.postExposure.Override(g.Brightness * 1.2f);
            float blood01 = _blood != null ? _blood.Blood01 : 1f;
            _color.saturation.Override(Mathf.Lerp(-85f, 0f, Mathf.InverseLerp(0.45f, 0.85f, blood01)) + (body ? -12f : 0f));

            _blur.active = g.MotionBlur && ViewState.MouseLook;
            _blur.intensity.Override(0.35f);

            float lens = body ? g.LensDistortion : 0f;
            _lens.active = lens > 0.01f;
            _lens.intensity.Override(-0.42f * lens);
            _lens.scale.Override(1f + 0.08f * lens);
            _chroma.active = lens > 0.01f;
            _chroma.intensity.Override(0.3f * lens);
            _grain.active = body;
            _grain.intensity.Override(0.35f);
            _grain.type.Override(FilmGrainLookup.Medium3);

            float hurt = _stats != null ? Mathf.InverseLerp(45f, 10f, _stats.Health) : 0f;
            bool mask = eyes && _gear != null && _gear.HasGasMask;
            _vignette.active = hurt > 0.01f || mask || body;
            _vignette.color.Override(hurt > 0.01f ? new Color(0.45f, 0f, 0f) : Color.black);
            _vignette.intensity.Override(Mathf.Max(mask ? 0.5f : 0f, body ? 0.3f : 0f, hurt * 0.45f));
            _vignette.smoothness.Override(mask ? 0.25f : 0.45f);
        }
    }

    /// <summary>
    /// Metro's gas mask from the inside (first person and bodycam): the visor cracks as the mask takes damage,
    /// condensation creeps in from the edges as you breathe hard or the cold bites, and a wipe (F2 or the mouse's
    /// forward button) clears it — with the squeak of a glove on the glass. The other views keep the breathing and
    /// show the filter timer instead.
    /// </summary>
    public sealed class GasVisorOverlay : MonoBehaviour
    {
        private Canvas _canvas;
        private RawImage _cracks, _fog;
        private Text _filter;
        private PlayerGear _gear;
        private PlayerClothing _clothing;
        private PlayerStats _stats;
        private float _condensation;
        private bool _filterShown;
        private int _filterPct = -1;
        private static Texture2D s_Cracks, s_Fog;

        public static GasVisorOverlay Create(PlayerStats p)
        {
            var go = new GameObject("GasVisor");
            var v = go.AddComponent<GasVisorOverlay>();
            v._gear = p.GetComponent<PlayerGear>();
            v._clothing = p.GetComponent<PlayerClothing>();
            v._stats = p;
            v._canvas = UIKit.CreateCanvas("GasVisor Canvas", 35, go.transform);
            v._fog = Raw(v._canvas.transform, FogTexture(), "Condensation");
            v._cracks = Raw(v._canvas.transform, CrackTexture(), "Cracks");
            v._filter = UIKit.Label(HudLayer.Root, "", 15, new Color(0.8f, 0.9f, 0.8f), TextAnchor.MiddleLeft);
            UIKit.Place(v._filter.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 230f), new Vector2(260f, 24f));
            return v;
        }

        private static RawImage Raw(Transform parent, Texture tex, string name)
        {
            var img = new GameObject(name).AddComponent<RawImage>();
            img.transform.SetParent(parent, false);
            img.texture = tex;
            img.raycastTarget = false;
            UIKit.Stretch(img.rectTransform);
            return img;
        }

        private void Update()
        {
            bool mask = _gear != null && _gear.HasGasMask && _stats != null && !_stats.IsDead;
            bool inside = mask && ViewState.FirstPersonLike;
            _canvas.enabled = inside;
            bool shown = mask && !inside;
            int pct = shown ? Mathf.RoundToInt(_gear.FilterCharge01 * 100f) : -1;
            if (shown != _filterShown || pct != _filterPct)
            {
                _filterShown = shown;
                _filterPct = pct;
                _filter.text = shown ? "Mask filter " + pct + "%" : "";
            }
            if (!mask) { _condensation = 0f; return; }

            float exertion = _stats.Exertion == Exertion.Sprinting ? 1f : _stats.Exertion == Exertion.Walking ? 0.3f : 0.1f;
            _condensation = Mathf.Clamp01(_condensation + Time.deltaTime * (0.004f + exertion * 0.01f + _stats.ColdStrength * 0.012f));
            if (GameInput.Pressed(GameAction.WipeVisor))
            {
                _condensation = 0f;
                Sfx.PlayUI(SfxIds.VisorWipe, 0.6f);
            }
            float damage = _clothing != null ? 1f - _clothing.GetCondition01(ClothingSlot.Mask) : 0f;
            _cracks.color = new Color(1f, 1f, 1f, Mathf.Clamp01(damage * 1.3f));
            _fog.color = new Color(1f, 1f, 1f, _condensation * 0.85f);
            if (damage > 0.6f && Random.value < Time.deltaTime * 0.02f) Sfx.PlayUI(SfxIds.MaskCrack, 0.35f);
        }

        // A spider-web of cracks from a couple of impact points.
        private static Texture2D CrackTexture()
        {
            if (s_Cracks != null) return s_Cracks;
            const int W = 512, H = 288;
            var px = new Color32[W * H];
            var rng = new System.Random(77);
            for (int hit = 0; hit < 3; hit++)
            {
                var c = new Vector2((float)rng.NextDouble() * W, (float)rng.NextDouble() * H);
                int rays = 7 + rng.Next(5);
                for (int r = 0; r < rays; r++)
                {
                    float a = (float)rng.NextDouble() * Mathf.PI * 2f, len = 40f + (float)rng.NextDouble() * 220f;
                    Vector2 p = c;
                    for (float s = 0f; s < len; s += 1f)
                    {
                        a += ((float)rng.NextDouble() - 0.5f) * 0.12f;
                        p += new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        int x = (int)p.x, y = (int)p.y;
                        if (x < 0 || y < 0 || x >= W || y >= H) break;
                        byte alpha = (byte)Mathf.Clamp(220 - s * 0.6f, 40, 220);
                        px[y * W + x] = new Color32(230, 235, 240, alpha);
                    }
                }
            }
            s_Cracks = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            s_Cracks.SetPixels32(px);
            s_Cracks.Apply();
            return s_Cracks;
        }

        // Condensation: thick at the rim, thin in the middle.
        private static Texture2D FogTexture()
        {
            if (s_Fog != null) return s_Fog;
            const int W = 128, H = 72;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x / (float)W - 0.5f) * 2f, dy = (y / (float)H - 0.5f) * 2f;
                    float r = Mathf.Sqrt(dx * dx * 0.8f + dy * dy * 1.2f);
                    float n = Mathf.PerlinNoise(x * 0.11f, y * 0.11f);
                    byte a = (byte)(Mathf.Clamp01(Mathf.InverseLerp(0.35f, 1.1f, r) + n * 0.25f) * 230f);
                    px[y * W + x] = new Color32(215, 222, 228, a);
                }
            s_Fog = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            s_Fog.SetPixels32(px);
            s_Fog.Apply();
            return s_Fog;
        }
    }

    /// <summary>
    /// The isometric view's aim line: from the muzzle at chest height toward the cursor, drawn faintly whenever a gun
    /// is in hand and clearly while Alt is held — and turning red past the first thing that would stop the round.
    /// (The cursor sits on the ground plane; the shot leaves the muzzle at chest height, so the line shows what the
    /// bullet will actually meet.)
    /// </summary>
    public sealed class IsoAimLine : MonoBehaviour
    {
        private LineRenderer _clear, _blocked;
        private PlayerController _controller;
        private PlayerCombat _combat;

        public static IsoAimLine Create(PlayerStats p)
        {
            var go = new GameObject("AimLine");
            var a = go.AddComponent<IsoAimLine>();
            a._controller = p.GetComponent<PlayerController>();
            a._combat = p.GetComponent<PlayerCombat>();
            a._clear = Line(go.transform, new Color(1f, 1f, 1f, 0.5f));
            a._blocked = Line(go.transform, new Color(1f, 0.25f, 0.2f, 0.6f));
            return a;
        }

        private static LineRenderer Line(Transform parent, Color c)
        {
            var go = new GameObject("Line");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.widthMultiplier = 0.035f;
            lr.material = ZoneKit.BeamMaterial();
            lr.startColor = lr.endColor = c;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private void LateUpdate()
        {
            bool gun = _combat != null && _combat.EquippedFirearm != null;
            bool held = GameInput.Held(GameAction.AimLine);
            bool show = ViewState.Current == CameraView.Isometric && _controller != null && (gun || held) && !GameInput.Blocked
                && GameSettings.Current.Hud != HudMode.Hardcore;
            _clear.enabled = _blocked.enabled = show;
            if (!show) return;
            Vector3 from = _controller.transform.position + Vector3.up * 1.35f;
            Vector3 to = _controller.AimPoint;
            to.y = from.y;
            Vector3 d = to - from;
            float len = Mathf.Max(0.5f, d.magnitude);
            d /= len;
            float alpha = held ? 0.75f : 0.25f;
            if (Physics.Raycast(from, d, out RaycastHit hit, len, 1, QueryTriggerInteraction.Ignore))
            {
                _clear.SetPosition(0, from); _clear.SetPosition(1, hit.point);
                _blocked.SetPosition(0, hit.point); _blocked.SetPosition(1, to);
                _blocked.enabled = true;
            }
            else
            {
                _clear.SetPosition(0, from); _clear.SetPosition(1, to);
                _blocked.enabled = false;
            }
            _clear.startColor = new Color(1f, 1f, 1f, alpha);
            _clear.endColor = new Color(1f, 1f, 1f, alpha * 0.3f);
        }
    }

    /// <summary>
    /// Metro's wristwatch, for the diegetic HUD (and only if you wear one): the time, the gas filter's minutes, and a
    /// lamp that glows brighter the easier you are to see — the readable stealth meter, in the world's own terms.
    /// </summary>
    public sealed class DiegeticWatch : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _time, _filter;
        private Image _lamp;
        private PlayerInventory _inv;
        private PlayerGear _gear;
        private float _check;
        private bool _wearing;
        private int _timeH = -1, _timeM = -1;
        private bool _filterShown;
        private int _filterPct = -1;

        public static DiegeticWatch Create(PlayerStats p)
        {
            var go = new GameObject("DiegeticWatch");
            var w = go.AddComponent<DiegeticWatch>();
            w._inv = p.GetComponent<PlayerInventory>();
            w._gear = p.GetComponent<PlayerGear>();
            w._canvas = UIKit.CreateCanvas("Watch Canvas", 45, go.transform);
            RectTransform face = UIKit.Place(UIKit.Rect("Face", w._canvas.transform), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(170f, 170f));
            Image strap = UIKit.Panel(face, "Strap", new Color(0.15f, 0.12f, 0.1f));
            UIKit.Place(strap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 200f));
            Image stitch1 = UIKit.Panel(strap.rectTransform, "Stitch1", Color.white);
            UIKit.Place(stitch1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 30f));
            stitch1.rectTransform.anchoredPosition = new Vector2(0f, -45f);
            Image stitch2 = UIKit.Panel(strap.rectTransform, "Stitch2", Color.white);
            UIKit.Place(stitch2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 30f));
            stitch2.rectTransform.anchoredPosition = new Vector2(0f, -15f);
            Image dial = UIKit.Panel(face, "Dial", new Color(0.08f, 0.09f, 0.08f));
            UIKit.Stretch(dial.rectTransform, 10f, 10f, 10f, 10f);
            w._time = UIKit.Label(dial.rectTransform, "", 30, new Color(0.65f, 0.95f, 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(w._time.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 40f));
            w._filter = UIKit.Label(dial.rectTransform, "", 15, new Color(0.65f, 0.9f, 0.9f), TextAnchor.MiddleCenter);
            UIKit.Place(w._filter.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 24f));
            w._lamp = UIKit.Panel(dial.rectTransform, "VisibilityLamp", Color.green);
            UIKit.Place(w._lamp.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
            return w;
        }

        private void Update()
        {
            if (Time.time > _check) { _check = Time.time + 1f; _wearing = _inv != null && _inv.Has("watch_wrist"); }
            bool show = _wearing && GameSettings.Current.Hud == HudMode.Diegetic && UIScreen.Current == null;
            _canvas.enabled = show;
            if (!show) return;
            TimeOfDay t = TimeOfDay.Instance;
            if (t != null)
            {
                int h = Mathf.FloorToInt(t.Hour), m = Mathf.FloorToInt((t.Hour - h) * 60f);
                if (h != _timeH || m != _timeM)
                {
                    _timeH = h;
                    _timeM = m;
                    _time.text = h.ToString("00") + ":" + m.ToString("00");
                }
            }
            bool fShown = _gear != null && _gear.HasGasMask;
            int fPct = fShown ? Mathf.RoundToInt(_gear.FilterCharge01 * 100f) : -1;
            if (fShown != _filterShown || fPct != _filterPct)
            {
                _filterShown = fShown;
                _filterPct = fPct;
                _filter.text = fShown ? "filter " + fPct + "%" : "";
            }
            float vis = Actor.LocalPlayer != null ? Actor.LocalPlayer.Visibility01 : 0f;
            _lamp.color = Color.Lerp(new Color(0.1f, 0.25f, 0.1f), new Color(1f, 0.85f, 0.2f), vis);
        }
    }

    /// <summary>Installs the presentation layer for every survivor body and wires recoil and blasts to the camera.</summary>
    public sealed class PresentationModule : IGameModule
    {
        public int Order => 80;

        public void Init()
        {
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                PostFx.Ensure();
                GasVisorOverlay.Create(p);
                IsoAimLine.Create(p);
                DiegeticWatch.Create(p);
                if (p.GetComponent<WeaponHandIK>() == null) p.gameObject.AddComponent<WeaponHandIK>();
                var combat = p.GetComponent<PlayerCombat>();
                Transform t = p.transform;
                if (combat != null) combat.OnRecoil += kick => CinemachineViews.Shake(t.position + Vector3.up * 1.5f, Mathf.Clamp(kick, 0.2f, 3f));
            };
            GameEvents.WorldEvent += (at, radius, kind) =>
            {
                if (kind != "explosion" || Actor.LocalPlayer == null) return;
                float d = Vector3.Distance(at, Actor.LocalPlayer.transform.position);
                float k = Mathf.Clamp01(1f - d / Mathf.Max(1f, radius * 0.5f));
                if (k > 0f) CinemachineViews.Shake(at, 6f * k);
            };
        }
    }
}
