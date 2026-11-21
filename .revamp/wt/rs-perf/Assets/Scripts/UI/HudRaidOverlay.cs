using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Raid feedback from <see cref="ExtractionManager"/>: the floating green "Spawn Protection: 40s"
    /// over the survivor's head, the centred "EXTRACTING — exit" progress bar while standing in an exit,
    /// and an amber "Dawn in m:ss" warning for the last five minutes before the exits close.
    /// Everything stays hidden outside a raid (no manager).
    /// </summary>
    public sealed class HudRaidOverlay
    {
        private const float HeadHeight = 2.2f, DawnWarnSeconds = 300f;
        private static readonly Color Protection = new Color(0.45f, 1f, 0.45f, 1f);
        private static readonly Color Extracting = new Color(0.55f, 1f, 0.62f, 1f);

        private readonly HudContext _ctx;
        private readonly RectTransform _root;
        private readonly Text _protection, _extractLabel, _dawn;
        private readonly GameObject _extractRoot;
        private readonly Image _extractFill;
        private Camera _cam;

        private int _shownProtection = -1, _shownExtract = -1, _shownDawn = -1;
        private RaidExit _shownExit;
        private string[] _protectionTexts;

        public HudRaidOverlay(HudContext ctx, RectTransform root, Transform protectionLayer)
        {
            _ctx = ctx;
            _root = root;

            _protection = HudUI.Label(protectionLayer, "SpawnProtection", 20, TextAnchor.MiddleCenter, Protection,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 26f), FontStyle.Bold);
            var outline = _protection.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            _protection.enabled = false;

            RectTransform bar = HudUI.Rect(root, "Extraction", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(440f, 46f));
            _extractLabel = HudUI.Label(bar, "Label", 18, TextAnchor.UpperCenter, Extracting, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(440f, 24f), FontStyle.Bold);
            _extractFill = HudUI.Bar(bar, "Progress", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f),
                new Vector2(380f, 8f), new Color(0f, 0f, 0f, 0.6f), Extracting, out RectTransform track);
            UITheme.AddShadow(track.GetComponent<Image>());
            _extractRoot = bar.gameObject;
            _extractRoot.SetActive(false);

            _dawn = HudUI.Label(root, "Dawn", 22, TextAnchor.UpperCenter, HudUI.Amber, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -22f), new Vector2(400f, 28f), FontStyle.Bold);
            _dawn.enabled = false;
        }

        /// <summary>Positions / refreshes the raid overlays (no-op outside a raid).</summary>
        public void Tick(float dt)
        {
            ExtractionManager em = ExtractionManager.Instance;
            bool live = em != null && (em.State == ExtractionManager.RaidState.InRaid || em.State == ExtractionManager.RaidState.Extracting);
            UpdateProtection(live ? em : null);
            UpdateExtraction(live ? em : null);
            UpdateDawn(live ? em : null);
        }

        private void UpdateProtection(ExtractionManager em)
        {
            bool on = em != null && em.SpawnProtected && _ctx.Stats != null;
            if (on)
            {
                if (_cam == null) _cam = Camera.main;
                on = _cam != null;
            }
            if (on)
            {
                Vector3 sp = _cam.WorldToScreenPoint(_ctx.Stats.transform.position + Vector3.up * HeadHeight);
                on = sp.z > 0f && sp.x > -50f && sp.x < Screen.width + 50f && sp.y > -20f && sp.y < Screen.height + 20f;
                if (on && RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, sp, null, out Vector2 local))
                    _protection.rectTransform.anchoredPosition = local;
            }
            if (_protection.enabled != on) _protection.enabled = on;
            if (!on) return;

            int secs = Mathf.CeilToInt(em.SpawnProtectionRemaining);
            if (secs != _shownProtection)
            {
                _shownProtection = secs;
                _protection.text = ProtectionText(secs);
            }
        }

        // Cached per whole second so the countdown never allocates after its first pass.
        private string ProtectionText(int secs)
        {
            if (secs < 0 || secs > 600) return "Spawn Protection: " + secs + "s";
            if (_protectionTexts == null) _protectionTexts = new string[601];
            return _protectionTexts[secs] ?? (_protectionTexts[secs] = "Spawn Protection: " + HudUI.Int(secs) + "s");
        }

        private void UpdateExtraction(ExtractionManager em)
        {
            RaidExit exit = em != null ? em.ActiveExit : null;
            HudUI.Show(_extractRoot, exit != null);
            if (exit == null) { _shownExit = null; return; }

            float p = Mathf.Clamp01(em.ExtractProgress01);
            _extractFill.fillAmount = p;
            int left = Mathf.CeilToInt((1f - p) * Mathf.Max(0f, exit.ExtractSeconds));
            if (exit != _shownExit || left != _shownExtract)
            {
                _shownExit = exit;
                _shownExtract = left;
                string name = string.IsNullOrEmpty(exit.Name) ? "Exit" : exit.Name;
                _extractLabel.text = "EXTRACTING \u2014 " + name + "   " + HudUI.Int(left) + "s";
            }
        }

        private void UpdateDawn(ExtractionManager em)
        {
            float remaining = em != null ? em.RaidSecondsRemaining : 0f;
            bool on = em != null && remaining > 0f && remaining < DawnWarnSeconds;
            if (_dawn.enabled != on) _dawn.enabled = on;
            if (!on) return;

            int secs = Mathf.CeilToInt(remaining);
            if (secs != _shownDawn)
            {
                _shownDawn = secs;
                _dawn.text = "Dawn in " + HudUI.Clock(secs);
            }
            _dawn.color = secs <= 60 ? HudUI.WithAlpha(HudUI.Amber, HudUI.Pulse(5f, 0.45f)) : HudUI.Amber;
        }
    }
}
