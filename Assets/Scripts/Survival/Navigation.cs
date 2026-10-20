using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The Zone map (P), on a clipboard: streets and buildings, the landmarks, the exits — and no dot saying where you
    /// are unless you carry a GPS (the compass in the corner only gives your heading). Click anywhere to pencil in a
    /// mark ("anomalies", "safe route", "stash"): marks are account knowledge, so they survive death and wipes —
    /// knowledge is the real loot.
    /// </summary>
    public sealed class ZoneMapScreen : UIScreen
    {
        protected override string Title => (ZoneCatalog.Current != null ? ZoneCatalog.Current.Name : "Map") + " — field map";
        protected override Vector2 WindowSize => new Vector2(1100f, 960f);

        private static Texture2D s_Tex;
        private static int s_TexSeed = int.MinValue;
        private RectTransform _paper;
        private float _half;
        private string _markLabel = "Anomalies";

        protected override void Build()
        {
            PlayerInventory inv = Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerInventory>() : null;
            if (!GameSession.IsExtraction)
            {
                UIKit.Label(Body, "There is no map of the bar. You know where the door is.", 20, UIKit.TextDim, TextAnchor.MiddleCenter);
                return;
            }
            bool hasMap = inv != null && inv.Has("map_zone");
            bool gps = inv != null && inv.Has("gps");
            _half = ZoneWorld.Map != null ? ZoneWorld.Map.HalfSize : CloneZConfig.ArenaHalfSize;

            Image paper = UIKit.Panel(Body, "Paper", new Color(0.86f, 0.83f, 0.74f));
            _paper = UIKit.Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(760f, 760f));
            // Add subtle worn‑edge detail
            int seed = ZoneCatalog.Current != null ? ZoneCatalog.Current.LayoutSeed : 0;
            System.Random rng = new System.Random(seed);
            // top edge – two short strips with random offset
            for (int i = 0; i < 2; i++)
            {
                float offsetX = (float)rng.NextDouble() * 6f - 3f; // ±3 px random shift
                float width = 760f - (float)rng.NextDouble() * 20f; // slight length variation
                var edge = UIKit.Panel(_paper, "EdgeTop" + i, new Color(0.78f, 0.75f, 0.72f));
                UIKit.Place(edge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(offsetX, 0f), new Vector2(width, 4f));
            }
            // bottom edge – two short strips
            for (int i = 0; i < 2; i++)
            {
                float offsetX = (float)rng.NextDouble() * 6f - 3f;
                float width = 760f - (float)rng.NextDouble() * 20f;
                var edge = UIKit.Panel(_paper, "EdgeBottom" + i, new Color(0.78f, 0.75f, 0.72f));
                UIKit.Place(edge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(offsetX, 0f), new Vector2(width, 4f));
            }
            // left edge – two short strips
            for (int i = 0; i < 2; i++)
            {
                float offsetY = (float)rng.NextDouble() * 6f - 3f;
                float height = 760f - (float)rng.NextDouble() * 20f;
                var edge = UIKit.Panel(_paper, "EdgeLeft" + i, new Color(0.78f, 0.75f, 0.72f));
                UIKit.Place(edge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, offsetY), new Vector2(4f, height));
            }
            // right edge – two short strips
            for (int i = 0; i < 2; i++)
            {
                float offsetY = (float)rng.NextDouble() * 6f - 3f;
                float height = 760f - (float)rng.NextDouble() * 20f;
                var edge = UIKit.Panel(_paper, "EdgeRight" + i, new Color(0.78f, 0.75f, 0.72f));
                UIKit.Place(edge.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, offsetY), new Vector2(4f, height));
            }
            if (hasMap)
            {
                var raw = new GameObject("Streets").AddComponent<RawImage>();
                raw.transform.SetParent(_paper, false);
                raw.texture = Texture();
                raw.color = new Color(1f, 1f, 1f, 0.9f);
                UIKit.Stretch(raw.rectTransform, 10f, 10f, 10f, 10f);
                foreach (var kv in HumanUtil.Landmarks()) Pin(kv.Value, "\u25B2 " + HumanUtil.Pretty(kv.Key), new Color(0.25f, 0.2f, 0.15f));
                if (ZoneWorld.Map != null)
                    foreach (RaidExit e in ZoneWorld.Map.Exits) Pin(e.Position, "\u2192 " + e.Name, new Color(0.1f, 0.45f, 0.15f));
            }
            else
            {
                UIKit.Label(_paper, "You have no map of this Zone — only your own pencil marks.\nBuy one from the guide, or find one.", 18,
                    new Color(0.3f, 0.25f, 0.2f), TextAnchor.MiddleCenter);
            }
            foreach (Mark m in Marks()) Pin(m.Pos, "\u2716 " + m.Label, new Color(0.65f, 0.12f, 0.1f));
            if (gps && Actor.LocalPlayer != null) Pin(Actor.LocalPlayer.transform.position, "\u25CF you", new Color(0.1f, 0.3f, 0.8f));

            var click = paper.gameObject.AddComponent<Button>();
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(PlaceMark);

            RectTransform row = UIKit.Row(Body, 40f);
            UIKit.Place(row, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(900f, 40f));
            UIKit.Label(row, "Click the map to pencil a mark:", 16, UIKit.TextDim);
            foreach (string label in new[] { "Anomalies", "Safe route", "Stash", "Danger", "Loot" })
            {
                string l = label;
                UIKit.Button(row, label, () => { _markLabel = l; SetStatus("Next mark: " + l); }, l == _markLabel ? UIKit.Accent : UIKit.ButtonColor, 14);
            }
            UIKit.Button(row, "Erase marks", () =>
            {
                AccountProfile a = ProfileStore.Account;
                a?.Knowledge.RemoveAll(k => k.StartsWith(Prefix));
                Rebuild();
            }, UIKit.ButtonColor, 14);
            Sfx.PlayUI(SfxIds.PageTurn, 0.5f);
        }

        private void Pin(Vector3 world, string text, Color c)
        {
            Vector2 uv = new Vector2(Mathf.InverseLerp(-_half, _half, world.x), Mathf.InverseLerp(-_half, _half, world.z));
            Text t = UIKit.Label(_paper, text, 13, c, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(t.rectTransform, uv, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(200f, 18f));
            t.raycastTarget = false;
        }

        private void PlaceMark()
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_paper, UnityEngine.InputSystem.Mouse.current.position.ReadValue(), null, out local)) return;
            Rect r = _paper.rect;
            float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x), v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
            var world = new Vector3(Mathf.Lerp(-_half, _half, u), 0f, Mathf.Lerp(-_half, _half, v));
            ProfileStore.Account?.Learn(Prefix + Mathf.RoundToInt(world.x) + "," + Mathf.RoundToInt(world.z) + ":" + _markLabel);
            Sfx.PlayUI(SfxIds.PageTurn, 0.4f, 1.3f);
            Rebuild();
        }

        private static string Prefix => "mark:" + GameSession.ZoneId + ":";

        private struct Mark { public Vector3 Pos; public string Label; }

        private static IEnumerable<Mark> Marks()
        {
            AccountProfile a = ProfileStore.Account;
            if (a == null) yield break;
            foreach (string k in a.Knowledge)
            {
                if (!k.StartsWith(Prefix)) continue;
                string[] p = k.Substring(Prefix.Length).Split(':');
                string[] xy = p[0].Split(',');
                if (xy.Length < 2 || !int.TryParse(xy[0], out int x) || !int.TryParse(xy[1], out int z)) continue;
                yield return new Mark { Pos = new Vector3(x, 0f, z), Label = p.Length > 1 ? p[1] : "?" };
            }
        }

        // The streets drawn once per raid layout, read from the world: paving grey, buildings dark, woods green.
        private Texture2D Texture()
        {
            int seed = ZoneCatalog.Current != null ? ZoneCatalog.Current.LayoutSeed : 0;
            if (s_Tex != null && s_TexSeed == seed) return s_Tex;
            const int N = 192;
            if (s_Tex == null) s_Tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var p = new Vector3(Mathf.Lerp(-_half, _half, (x + 0.5f) / N), 0f, Mathf.Lerp(-_half, _half, (y + 0.5f) / N));
                    Color c = new Color(0.86f, 0.83f, 0.74f, 0f);
                    if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out RaycastHit hit, 40f, 1, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.point.y > 2f) c = new Color(0.32f, 0.29f, 0.26f, 0.85f);                 // a roof
                        else if (ZoneForest.Density(p) > 0.4f) c = new Color(0.45f, 0.55f, 0.38f, 0.5f);
                        else
                        {
                            string n = hit.collider.name.ToLowerInvariant();
                            if (n.Contains("road") || n.Contains("street") || n.Contains("lot")) c = new Color(0.55f, 0.53f, 0.5f, 0.7f);
                        }
                    }
                    px[y * N + x] = c;
                }
            s_Tex.SetPixels32(px);
            s_Tex.Apply();
            s_TexSeed = seed;
            return s_Tex;
        }
    }

    /// <summary>
    /// The notebook (J): what you know. Stashes noted from PDAs, the rules of the anomalies you've learned, artifact
    /// properties, rumours, the Zone's marks on your body and your illnesses. Almost all of it is account knowledge.
    /// </summary>
    public sealed class JournalScreen : UIScreen
    {
        protected override string Title => "Notebook";
        protected override Vector2 WindowSize => new Vector2(1300f, 880f);

        protected override void Build()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            AccountProfile a = ProfileStore.Account;
            RectTransform[] cols = HubUI.Columns(Body, 16f, 1f, 1f);
            HubUI.Card(cols[0]);
            RectTransform left = HubUI.Scroll(cols[0], 0f, 0f, 4f);
            HubUI.Card(cols[1]);
            RectTransform right = HubUI.Scroll(cols[1], 0f, 0f, 4f);

            HubUI.Section(left, "Jobs");
            int jobs = 0;
            if (c != null)
                foreach (QuestState q in c.Quests)
                    if (q.Status == QuestState.Active || q.Status == QuestState.Ready) { HubUI.Note(left, QuestBook.Summary(q)); jobs++; }
            if (jobs == 0) HubUI.Note(left, "No jobs taken. The board at the bar has work.", UIKit.TextDim);

            HubUI.Section(left, "Stashes");
            int stashes = 0;
            if (c != null)
                foreach (BuriedStash s in c.Stashes)
                    if (!s.Opened) { HubUI.Note(left, (s.FromPda ? "From a PDA: " : "Your cache: ") + s.Hint); stashes++; }
            if (stashes == 0) HubUI.Note(left, "None. Read the PDAs you find on the dead.", UIKit.TextDim);

            HubUI.Section(left, "Your body");
            if (c != null)
            {
                foreach (string m in c.ZoneMarks) HubUI.Note(left, ZoneMarkRegistry.NameOf(m) + " — " + ZoneMarkRegistry.Describe(m));
                if (c.Health != null) foreach (string d in c.Health.Diseases) HubUI.Note(left, "Ill: " + d, UIKit.Warn);
                var rad = Actor.LocalPlayer != null ? Actor.LocalPlayer.GetComponent<PlayerRadiation>() : null;
                float dose = rad != null ? rad.Dose : (c.Health != null ? c.Health.Radiation : 0f);
                if (dose > 0.01f) HubUI.Note(left, "Radiation dose " + (dose * 6f).ToString("0.00") + " Sv", UIKit.Warn);
                HubUI.Note(left, "Zone exposure: " + c.Exposure.ToString("0.00"), UIKit.TextDim);
            }

            HubUI.Section(right, "Anomalies — what you've learned");
            foreach (AnomalyDef d in AnomalyCatalog.All.Values)
            {
                bool known = a != null && a.Knows("anomaly_rule:" + d.Id);
                HubUI.Note(right, d.Name + ": " + (known ? d.Rule : "??? (survive one, or ask around)"), known ? UIKit.TextColor : UIKit.TextDim);
            }
            HubUI.Section(right, "Artifacts");
            foreach (ArtifactCatalog.Def d in ArtifactCatalog.All)
            {
                int k = ArtifactCatalog.KnownCount(d.Id);
                if (k == 0) continue;
                HubUI.Note(right, d.Name + ":\n" + ArtifactCatalog.Describe(d.Id));
            }
            HubUI.Section(right, "Rumours heard");
            if (a != null)
                foreach (string k in a.Knowledge)
                    if (k.StartsWith("rumour:")) HubUI.Note(right, Rumour(k), UIKit.TextDim);
            Sfx.PlayUI(SfxIds.PageTurn, 0.5f);
        }

        private static string Rumour(string k)
        {
            string[] p = k.Split(':');
            return p.Length >= 3 ? ArtifactCatalog.NameOf(p[1]) + " seen in " + ZoneCatalog.Get(p[2]).Name : k;
        }
    }

    /// <summary>The compass strip at the top of the screen (only with a compass; never in hardcore).</summary>
    public sealed class HudCompass : MonoBehaviour
    {
        private Text _text;
        private PlayerInventory _inv;
        private float _check;
        private bool _has;

        public static HudCompass Create(PlayerInventory inv)
        {
            RectTransform slot = HudLayer.Anchor("Compass", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(420f, 30f));
            var h = slot.gameObject.AddComponent<HudCompass>();
            h._inv = inv;
            h._text = UIKit.Label(slot, "", 18, new Color(0.9f, 0.88f, 0.8f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Stretch(h._text.rectTransform);
            return h;
        }

        private static readonly string[] s_Points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        private System.Text.StringBuilder _sb = new System.Text.StringBuilder();
        private float _lastYaw;
        private bool _lastVisible;

        private void LateUpdate()
        {
            // Update compass possession check once per second
            if (Time.time > _check) { _check = Time.time + 1f; _has = _inv != null && _inv.Has("compass"); }
            Camera cam = Camera.main;
            bool visible = _has && cam != null && GameSettings.Current.Hud != HudMode.Hardcore;
            // Clear text immediately when compass is not visible (lost, camera missing, or hardcore HUD)
            if (!visible) { _text.text = ""; _lastVisible = false; return; }
            float yaw = cam.transform.eulerAngles.y;
            // Rebuild strip only if visibility changed or yaw moved enough
            if (!_lastVisible || Mathf.Abs(Mathf.DeltaAngle(yaw, _lastYaw)) > 0.2f)
            {
                _sb.Clear();
                for (int d = -60; d <= 60; d += 15)
                {
                    float h = Mathf.Repeat(yaw + d, 360f);
                    int idx = Mathf.RoundToInt(h / 45f) % 8;
                    if (Mathf.Abs(Mathf.DeltaAngle(h, idx * 45f)) < 7.5f) _sb.Append(d == 0 ? "[" + s_Points[idx] + "]" : s_Points[idx]);
                    else _sb.Append(d == 0 ? "|" : "\u00b7");
                    _sb.Append("  ");
                }
                _text.text = _sb.ToString();
                _lastYaw = yaw;
                _lastVisible = true;
            }
            // else keep existing text unchanged
        }
    }
}
