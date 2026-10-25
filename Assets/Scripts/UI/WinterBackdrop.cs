using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The winter night behind the title screen and the loading card: a sky lit orange-grey at the horizon by a town's
    /// lights, a small Kentucky town's roofline with a few lit windows, a steeple and a water tower, snow lying in front,
    /// and snow coming down. All UI, animated on unscaled time, so it moves while the game is paused or a world is
    /// loading.
    /// </summary>
    public sealed class WinterBackdrop : MonoBehaviour
    {
        private struct Flake
        {
            public RectTransform Rect;
            public float Speed, Sway, Rate, Phase, X, Y;
        }

        private static readonly Color Silhouette = new Color(0.028f, 0.032f, 0.048f, 1f);
        private static readonly Color Lit = new Color(1f, 0.74f, 0.38f, 0.9f);
        private static Texture2D s_Sky, s_Vignette;
        private static Sprite s_Gable;

        private readonly List<Flake> _flakes = new List<Flake>();
        private RectTransform _area;
        private System.Random _rng;

        /// <summary>Builds the night over <paramref name="parent"/> (stretched across it): the sky, the town
        /// (<paramref name="town"/>) and <paramref name="flakes"/> snowflakes.</summary>
        public static WinterBackdrop Build(RectTransform parent, int flakes = 120, bool town = true, int seed = 1993)
        {
            RectTransform area = UIKit.Stretch(UIKit.Rect("WinterBackdrop", parent));
            var backdrop = area.gameObject.AddComponent<WinterBackdrop>();
            backdrop._area = area;
            backdrop._rng = new System.Random(seed);

            RawImage sky = UIKit.Rect("Sky", area).gameObject.AddComponent<RawImage>();
            UIKit.Stretch(sky.rectTransform);
            sky.texture = SkyTexture();
            sky.raycastTarget = false;

            if (town) backdrop.BuildTown(area);
            backdrop.BuildFlakes(area, flakes);

            RawImage vignette = UIKit.Rect("Vignette", area).gameObject.AddComponent<RawImage>();
            UIKit.Stretch(vignette.rectTransform);
            vignette.texture = VignetteTexture();
            vignette.raycastTarget = false;
            return backdrop;
        }

        #region The town
        private void BuildTown(RectTransform area)
        {
            RectTransform town = UIKit.Rect("Town", area);
            town.anchorMin = new Vector2(0f, 0f);
            town.anchorMax = new Vector2(1f, 0f);
            town.pivot = new Vector2(0.5f, 0f);
            town.anchoredPosition = Vector2.zero;
            town.sizeDelta = new Vector2(0f, 300f);

            const float Ground = 34f;
            // The roofline: a run of storefronts and houses across the whole width, taller downtown in the middle.
            float x = -0.02f;
            while (x < 1.02f)
            {
                float w = 0.025f + (float)_rng.NextDouble() * 0.05f;
                float downtown = 1f - Mathf.Abs(x - 0.42f) * 1.6f;
                float h = 46f + (float)_rng.NextDouble() * 60f + Mathf.Max(0f, downtown) * 70f;
                bool gable = _rng.NextDouble() < 0.45;
                House(town, x, w, Ground, h, gable);
                x += w + (float)_rng.NextDouble() * 0.012f;
            }
            // The courthouse's cupola and a church steeple above the roofs, and the water tower off to the east.
            Block(town, 0.415f, 0.05f, Ground, 150f, Silhouette);
            Block(town, 0.432f, 0.016f, Ground + 150f, 40f, Silhouette);
            Roof(town, 0.432f, 0.016f, Ground + 190f, 26f);
            Block(town, 0.215f, 0.012f, Ground, 205f, Silhouette);
            Roof(town, 0.215f, 0.012f, Ground + 205f, 70f);
            WaterTower(town, 0.76f, Ground);
            // Snow lying in front of it all.
            Image snow = UIKit.Panel(town, "Snow", new Color(0.70f, 0.74f, 0.82f, 1f));
            snow.raycastTarget = false;
            RectTransform s = snow.rectTransform;
            s.anchorMin = new Vector2(0f, 0f);
            s.anchorMax = new Vector2(1f, 0f);
            s.pivot = new Vector2(0.5f, 0f);
            s.anchoredPosition = Vector2.zero;
            s.sizeDelta = new Vector2(0f, Ground);
            Image drift = UIKit.Panel(town, "SnowEdge", new Color(0.86f, 0.89f, 0.95f, 1f));
            drift.raycastTarget = false;
            RectTransform d = drift.rectTransform;
            d.anchorMin = new Vector2(0f, 0f);
            d.anchorMax = new Vector2(1f, 0f);
            d.pivot = new Vector2(0.5f, 1f);
            d.anchoredPosition = new Vector2(0f, Ground);
            d.sizeDelta = new Vector2(0f, 3f);
        }

        private void House(RectTransform town, float x, float w, float ground, float h, bool gable)
        {
            float wall = gable ? h * 0.7f : h;
            Block(town, x, w, ground, wall, Silhouette);
            if (gable) Roof(town, x, w, ground + wall, h * 0.3f);
            // A few lit windows: the town's last lights.
            int lit = _rng.NextDouble() < 0.55 ? 1 + _rng.Next(3) : 0;
            for (int i = 0; i < lit; i++)
            {
                float wx = x + w * (0.2f + (float)_rng.NextDouble() * 0.6f);
                float wy = ground + 10f + (float)_rng.NextDouble() * Mathf.Max(4f, wall - 30f);
                Image win = UIKit.Panel(town, "Window", Lit);
                win.raycastTarget = false;
                RectTransform r = win.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(wx, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, wy);
                r.sizeDelta = new Vector2(7f, 9f);
            }
        }

        private void WaterTower(RectTransform town, float x, float ground)
        {
            const float Legs = 150f, TankH = 46f;
            for (int i = 0; i < 4; i++)
            {
                Image leg = UIKit.Panel(town, "Leg", Silhouette);
                leg.raycastTarget = false;
                RectTransform r = leg.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(x, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(-30f + i * 20f, ground);
                r.sizeDelta = new Vector2(4f, Legs);
            }
            Image tank = UIKit.Panel(town, "Tank", Silhouette);
            tank.raycastTarget = false;
            RectTransform t = tank.rectTransform;
            t.anchorMin = t.anchorMax = new Vector2(x, 0f);
            t.pivot = new Vector2(0.5f, 0f);
            t.anchoredPosition = new Vector2(0f, ground + Legs);
            t.sizeDelta = new Vector2(96f, TankH);
            Image cap = UIKit.Rect("Cap", town).gameObject.AddComponent<Image>();
            cap.sprite = GableSprite();
            cap.color = Silhouette;
            cap.raycastTarget = false;
            RectTransform c = cap.rectTransform;
            c.anchorMin = c.anchorMax = new Vector2(x, 0f);
            c.pivot = new Vector2(0.5f, 0f);
            c.anchoredPosition = new Vector2(0f, ground + Legs + TankH);
            c.sizeDelta = new Vector2(100f, 22f);
            // The red beacon on top.
            Image beacon = UIKit.Panel(town, "Beacon", new Color(0.95f, 0.22f, 0.16f, 0.9f));
            beacon.raycastTarget = false;
            RectTransform b = beacon.rectTransform;
            b.anchorMin = b.anchorMax = new Vector2(x, 0f);
            b.pivot = new Vector2(0.5f, 0f);
            b.anchoredPosition = new Vector2(0f, ground + Legs + TankH + 20f);
            b.sizeDelta = new Vector2(5f, 5f);
        }

        private static void Block(RectTransform town, float x, float w, float bottom, float h, Color c)
        {
            Image img = UIKit.Panel(town, "Block", c);
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2(x, 0f);
            r.anchorMax = new Vector2(x + w, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, bottom);
            r.sizeDelta = new Vector2(0f, h);
        }

        private static void Roof(RectTransform town, float x, float w, float bottom, float h)
        {
            Image img = UIKit.Rect("Roof", town).gameObject.AddComponent<Image>();
            img.sprite = GableSprite();
            img.color = Silhouette;
            img.raycastTarget = false;
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2(x, 0f);
            r.anchorMax = new Vector2(x + w, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = new Vector2(0f, bottom);
            r.sizeDelta = new Vector2(0f, h);
        }
        #endregion

        #region Snow
        private void BuildFlakes(RectTransform area, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float near = (float)_rng.NextDouble();
                float size = Mathf.Lerp(2f, 6.5f, near * near);
                Image img = UIKit.Panel(area, "Flake", new Color(0.92f, 0.95f, 1f, Mathf.Lerp(0.35f, 0.9f, near)));
                img.raycastTarget = false;
                RectTransform r = img.rectTransform;
                r.anchorMin = r.anchorMax = Vector2.zero;
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(size, size);
                _flakes.Add(new Flake
                {
                    Rect = r,
                    Speed = Mathf.Lerp(22f, 95f, near),
                    Sway = Mathf.Lerp(6f, 26f, (float)_rng.NextDouble()),
                    Rate = Mathf.Lerp(0.25f, 0.9f, (float)_rng.NextDouble()),
                    Phase = (float)_rng.NextDouble() * 6.28f,
                    X = (float)_rng.NextDouble(),
                    Y = (float)_rng.NextDouble(),
                });
            }
        }

        private void Update()
        {
            if (_area == null) return;
            Rect rect = _area.rect;
            float w = Mathf.Max(1f, rect.width), h = Mathf.Max(1f, rect.height);
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f), t = Time.unscaledTime;
            for (int i = 0; i < _flakes.Count; i++)
            {
                Flake f = _flakes[i];
                f.Y -= f.Speed * dt / h;
                if (f.Y < -0.02f)
                {
                    f.Y = 1.02f;
                    f.X = (float)_rng.NextDouble();
                }
                f.Rect.anchoredPosition = new Vector2(f.X * w + Mathf.Sin(t * f.Rate + f.Phase) * f.Sway, f.Y * h);
                _flakes[i] = f;
            }
        }
        #endregion

        #region Textures
        private static Texture SkyTexture()
        {
            if (s_Sky != null) return s_Sky;
            const int H = 128;
            s_Sky = new Texture2D(2, H, TextureFormat.RGBA32, false) { name = "WinterSky", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var top = new Color(0.015f, 0.022f, 0.045f);
            var mid = new Color(0.045f, 0.058f, 0.095f);
            var glow = new Color(0.17f, 0.13f, 0.11f);
            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f);
                Color c = v > 0.45f ? Color.Lerp(mid, top, (v - 0.45f) / 0.55f) : Color.Lerp(glow, mid, Mathf.SmoothStep(0f, 1f, v / 0.45f));
                s_Sky.SetPixel(0, y, c);
                s_Sky.SetPixel(1, y, c);
            }
            s_Sky.Apply(false, true);
            return s_Sky;
        }

        private static Texture VignetteTexture()
        {
            if (s_Vignette != null) return s_Vignette;
            const int N = 64;
            s_Vignette = new Texture2D(N, N, TextureFormat.RGBA32, false) { name = "Vignette", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N - 0.5f, dy = (y + 0.5f) / N - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 0.7071f;
                    s_Vignette.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 0.75f, (d - 0.45f) / 0.55f)));
                }
            s_Vignette.Apply(false, true);
            return s_Vignette;
        }

        // A gable (an isosceles triangle, its base along the bottom) for roofs and the water tower's cap.
        private static Sprite GableSprite()
        {
            if (s_Gable != null) return s_Gable;
            const int W = 64, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "Gable", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float half = (1f - (y + 0.5f) / H) * W * 0.5f;
                    float dx = Mathf.Abs(x + 0.5f - W * 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(half - dx + 0.5f)));
                }
            tex.Apply(false, true);
            s_Gable = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0f), 100f);
            s_Gable.hideFlags = HideFlags.DontSave;
            return s_Gable;
        }
        #endregion
    }
}
