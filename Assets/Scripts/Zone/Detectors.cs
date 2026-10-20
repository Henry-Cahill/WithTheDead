using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The artifact detector, switched on with O (or "Switch on" in the pack). Better detectors change how you hunt,
    /// not just the numbers: the Echo only clicks faster as you close in (and only hears the commonest finds), the
    /// Bearing swings a needle toward the nearest, the Lattice plots everything around you — even the ones that hide.
    /// Every tier also chirps near an anomaly, which is how most runners find them before the bolts do.
    /// </summary>
    public sealed class DetectorDevice : MonoBehaviour
    {
        public sealed class Model
        {
            public string ItemId, Name;
            public int Tier;
            public float Range, RevealRange;
        }

        public static readonly Model[] Models =
        {
            new Model { ItemId = "detector_echo", Name = "Echo", Tier = 1, Range = 18f, RevealRange = 2.5f },
            new Model { ItemId = "detector_bearing", Name = "Bearing", Tier = 2, Range = 28f, RevealRange = 4f },
            new Model { ItemId = "detector_lattice", Name = "Lattice", Tier = 3, Range = 36f, RevealRange = 6.5f },
        };

        /// <summary>Whether the detector is currently on.</summary>
        public bool On { get; private set; }
        /// <summary>The currently selected detector model, or null if none.</summary>
        public Model Active { get; private set; }
        /// <summary>The nearest sensed artifact, or null.</summary>
        public ArtifactNode Target { get; private set; }
        /// <summary>Distance to the current target artifact, in metres.</summary>
        public float TargetDistance { get; private set; }
        /// <summary>Nearest anomaly distance, or MaxValue if none.</summary>
        public float NearestAnomaly { get; private set; } = float.MaxValue;


        private PlayerInventory _inv;
        private float _beepAt, _alertAt, _scanAt;
        private HudDetector _hud;

        /// <summary>Finds the best detector the player currently has in inventory.</summary>
        public static Model Best(PlayerInventory inv)
        {
            if (inv == null) return null;
            for (int i = Models.Length - 1; i >= 0; i--) if (inv.Has(Models[i].ItemId)) return Models[i];
            return null;
        }

        private void Awake() => _inv = GetComponent<PlayerInventory>();

        private void OnDestroy()
        {
            if (_hud != null) Destroy(_hud.gameObject);
        }

        /// <summary>Toggles the detector on/off, playing a click sound and showing a toast.</summary>
        public void Toggle()
        {
            Model m = Best(_inv);
            if (m == null)
            {
                On = false;
                GameEvents.RaiseToast("You have no detector.");
                return;
            }
            On = !On;
            Sfx.PlayUI(SfxIds.UiClick, 0.6f, On ? 1.2f : 0.8f);
            GameEvents.RaiseToast(m.Name + " detector " + (On ? "on" : "off"));
        }

        private void Update()
        {
            if (GameInput.Pressed(GameAction.Detector)) Toggle();
            Active = Best(_inv);
            if (Active == null) On = false;
            if (_hud == null && On) _hud = HudDetector.Create(this);
            if (_hud != null) _hud.gameObject.SetActive(On);
            if (!On) { Target = null; return; }

            if (Time.time >= _scanAt)
            {
                _scanAt = Time.time + 0.15f;
                Scan();
            }

            // Artifact clicks: faster as you close in.
            if (Target != null && Time.time >= _beepAt)
            {
                float k = Mathf.Clamp01(TargetDistance / Active.Range);
                _beepAt = Time.time + Mathf.Lerp(0.09f, 1.3f, k);
                Sfx.PlayUI(SfxIds.DetectorBeep, Mathf.Lerp(0.55f, 0.25f, k), Mathf.Lerp(1.5f, 1f, k));
            }
            // Anomaly chirps: a different, harsher double note.
            if (NearestAnomaly < 4f && Time.time >= _alertAt)
            {
                _alertAt = Time.time + Mathf.Lerp(0.25f, 0.9f, NearestAnomaly / 4f);
                Sfx.PlayUI(SfxIds.DetectorAlert, Mathf.Lerp(0.6f, 0.3f, NearestAnomaly / 4f), 0.8f);
            }
        }

        private void Scan()
        {
            Vector3 me = transform.position;
            Target = null;
            TargetDistance = float.MaxValue;
            for (int i = 0; i < ArtifactNode.All.Count; i++)
            {
                ArtifactNode n = ArtifactNode.All[i];
                if (n == null || n.Tier > Active.Tier) continue;
                float d = Vector3.Distance(me, n.transform.position);
                float range = n.IsPrize ? Active.Range * 3f : Active.Range;
                if (d > range) continue;
                if (d <= Active.RevealRange) n.Reveal(20f);
                if (d < TargetDistance) { TargetDistance = d; Target = n; }
            }
            NearestAnomaly = float.MaxValue;
            for (int i = 0; i < Anomaly.All.Count; i++)
            {
                Anomaly a = Anomaly.All[i];
                if (a == null) continue;
                Vector3 p = a.transform.position;
                float d = new Vector2(p.x - me.x, p.z - me.z).magnitude - a.Radius;
                if (d < NearestAnomaly) NearestAnomaly = Mathf.Max(0f, d);
            }
        }

        /// <summary>Lists every sensed artifact for the Lattice's screen.</summary>
        public void Sensed(List<ArtifactNode> into)
        {
            into.Clear();
            if (!On || Active == null) return;
            Vector3 me = transform.position;
            for (int i = 0; i < ArtifactNode.All.Count; i++)
            {
                ArtifactNode n = ArtifactNode.All[i];
                if (n != null && n.Tier <= Active.Tier && Vector3.Distance(me, n.transform.position) <= Active.Range) into.Add(n);
            }
        }

        internal static void RegisterItemVerbs()
        {
            foreach (Model m in Models)
                ItemActions.RegisterUse(m.ItemId, (inv, stack) =>
                {
                    var d = inv.GetComponent<DetectorDevice>();
                    if (d != null) d.Toggle();
                    return true;
                });
        }
    }

    /// <summary>The detector's face on the HUD: a lamp for the Echo, a needle for the Bearing, a plot for the Lattice.</summary>
    public sealed class HudDetector : MonoBehaviour
    {
        private DetectorDevice _d;
        private Text _title;
        private Image _lamp;
        private RectTransform _needle, _plot;
        private readonly List<Image> _dots = new List<Image>();
        private readonly List<ArtifactNode> _sensed = new List<ArtifactNode>();
        private Image _anomalyRing;
        // Cache for title text to avoid per‑frame string allocations
        private DetectorDevice.Model _cachedModel;
        private string _cachedUpperName;
        private int _cachedDist;
        private bool _cachedHasTarget;

        /// <summary>Creates a new HUD widget for the given detector device.</summary>
        public static HudDetector Create(DetectorDevice d)
        {
            RectTransform slot = HudLayer.Anchor("Detector", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 150f), new Vector2(200f, 200f));
            var h = slot.gameObject.AddComponent<HudDetector>();
            h._d = d;
            Image bg = UIKit.Panel(slot, "Bg", new Color(0.05f, 0.07f, 0.06f, 0.85f));
            UIKit.Stretch(bg.rectTransform);
            h._title = UIKit.Label(slot, "", 14, new Color(0.6f, 0.9f, 0.6f), TextAnchor.UpperCenter);
            UIKit.Place(h._title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(190f, 20f));

            h._plot = UIKit.Rect("Plot", slot);
            UIKit.Place(h._plot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(160f, 160f));
            h._anomalyRing = UIKit.Panel(h._plot, "AnomalyGlow", new Color(1f, 0.3f, 0.2f, 0f));
            UIKit.Stretch(h._anomalyRing.rectTransform);
            Image centre = UIKit.Panel(h._plot, "Me", new Color(0.6f, 0.95f, 0.6f, 0.9f));
            UIKit.Place(centre.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f));

            h._lamp = UIKit.Panel(h._plot, "Lamp", new Color(0.2f, 1f, 0.3f, 0.2f));
            UIKit.Place(h._lamp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

            Image needle = UIKit.Panel(h._plot, "Needle", new Color(0.9f, 0.95f, 0.6f, 0.95f));
            h._needle = needle.rectTransform;
            UIKit.Place(h._needle, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(4f, 66f));
            return h;
        }

        private void LateUpdate()
        {
            if (_d == null || _d.Active == null) return;
            int tier = _d.Active.Tier;

            // Update title only when needed
            bool modelChanged = _d.Active != _cachedModel;
            bool hasTarget = _d.Target != null;
            int dist = hasTarget ? Mathf.RoundToInt(_d.TargetDistance) : 0;
            string upperName = _d.Active.Name.ToUpperInvariant();

            if (modelChanged)
            {
                // New active detector – reset cache
                _cachedModel = _d.Active;
                _cachedUpperName = upperName;
                _cachedHasTarget = hasTarget;
                _cachedDist = dist;
                _title.text = _cachedUpperName + (hasTarget ? "  " + dist + "m" : "");
            }
            else
            {
                // Same model – update only if target state or distance changed
                if (hasTarget != _cachedHasTarget || (hasTarget && dist != _cachedDist))
                {
                    _cachedHasTarget = hasTarget;
                    _cachedDist = dist;
                    _title.text = _cachedUpperName + (hasTarget ? "  " + dist + "m" : "");
                }
            }

            float anomaly = _d.NearestAnomaly < 4f ? Mathf.Lerp(0.35f, 0f, _d.NearestAnomaly / 4f) : 0f;
            _anomalyRing.color = new Color(1f, 0.3f, 0.2f, anomaly * (0.6f + 0.4f * Mathf.Sin(Time.time * 18f)));

            // Echo: a lamp that pulses faster as you close in.
            _lamp.gameObject.SetActive(tier == 1);
            if (tier == 1)
            {
                float k = _d.Target != null ? Mathf.Clamp01(_d.TargetDistance / _d.Active.Range) : 1f;
                float pulse = _d.Target != null ? 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(30f, 4f, k)) : 0f;
                _lamp.color = new Color(0.2f, 1f, 0.3f, 0.15f + 0.8f * pulse * (1f - k * 0.6f));
            }

            // Bearing: a needle toward the nearest find, relative to where the view faces.
            _needle.gameObject.SetActive(tier == 2 && _d.Target != null);
            if (tier == 2 && _d.Target != null) _needle.localEulerAngles = new Vector3(0f, 0f, -Bearing(_d.Target.transform.position));

            // Lattice: plot everything sensed.
            if (tier == 3) _d.Sensed(_sensed); else _sensed.Clear();
            while (_dots.Count < _sensed.Count)
            {
                Image dot = UIKit.Panel(_plot, "Dot", Color.white);
                UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(7f, 7f));
                _dots.Add(dot);
            }
            for (int i = 0; i < _dots.Count; i++)
            {
                bool show = i < _sensed.Count;
                _dots[i].gameObject.SetActive(show);
                if (!show) continue;
                ArtifactNode n = _sensed[i];
                Vector3 off = n.transform.position - _d.transform.position;
                float ang = Bearing(n.transform.position) * Mathf.Deg2Rad;
                float r = Mathf.Clamp01(new Vector2(off.x, off.z).magnitude / _d.Active.Range) * 76f;
                _dots[i].rectTransform.anchoredPosition = new Vector2(Mathf.Sin(ang) * r, Mathf.Cos(ang) * r);
                _dots[i].color = n.IsPrize ? new Color(1f, 0.9f, 0.4f) : ArtifactCatalog.ColorOf(n.ArtifactId);
            }
        }

        private float Bearing(Vector3 target)
        {
            Vector3 to = target - _d.transform.position;
            to.y = 0f;
            Camera cam = Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : _d.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = _d.transform.forward;
            return Vector3.SignedAngle(fwd, to, Vector3.up);
        }
    }
}
