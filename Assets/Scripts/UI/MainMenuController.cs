using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The title screen: your runners (up to three per account), making a new one, hosting or joining a server, the
    /// settings and the field manual — and the original wave-survival mode. Picking a runner takes you to their hub.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        /// <summary>Set by the netcode: start a host (then enter the hub) / join an address. Null hides the buttons.</summary>
        public static System.Action<ushort> HostAction;
        public static System.Action<string, ushort> JoinAction;

        private GameBootstrapper _boot;
        private Canvas _canvas;
        private RectTransform _root, _panel;
        private string _joinAddress = "127.0.0.1";
        private string _port = "7777";

        public void Bind(GameBootstrapper boot)
        {
            _boot = boot;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UIKit.EnsureEventSystem();
            _canvas = UIKit.CreateCanvas("MainMenu Canvas", 200, transform);
            _root = (RectTransform)_canvas.transform;
            Image bg = UIKit.Panel(_root, "Backdrop", new Color(0.04f, 0.05f, 0.07f, 1f));
            UIKit.Stretch(bg.rectTransform);
            ShowTitle();
        }

        private void Clear()
        {
            if (_panel != null) Destroy(_panel.gameObject);
            _panel = UIKit.Rect("Panel", _root);
            UIKit.Stretch(_panel);
        }

        private void ShowTitle()
        {
            Clear();
            Text title = UIKit.Label(_panel, "CLONE-Z", 96, new Color(0.85f, 0.22f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 130f));
            AccountProfile a = ProfileStore.Account;
            Text sub = UIKit.Label(_panel, "The Zone  \u00b7  season " + a.Season + (a.Wipes > 0 ? " (" + a.Wipes + " wipes survived)" : ""), 24, new Color(0.7f, 0.74f, 0.8f), TextAnchor.MiddleCenter);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 40f));

            // Runners.
            RectTransform slots = UIKit.Place(UIKit.Rect("Runners", _panel), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 260f));
            var h = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            for (int i = 0; i < AccountProfile.MaxCharacters; i++) Slot(slots, i);

            RectTransform col = UIKit.Place(UIKit.Rect("Buttons", _panel), new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 210f));
            var row1 = UIKit.Row(col, 52f, null, 16f, 0);
            UIKit.Place(row1, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1500f, 52f));
            row1.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Big(row1, "Host a server", HostAction != null, () => { ushort.TryParse(_port, out ushort p); HostAction(p == 0 ? (ushort)7777 : p); });
            UIKit.Size(UIKit.Input(row1, _joinAddress, "address", v => _joinAddress = v, 18).gameObject, 220f, 46f);
            UIKit.Size(UIKit.Input(row1, _port, "port", v => _port = v, 18).gameObject, 100f, 46f);
            Big(row1, "Join", JoinAction != null, () => { ushort.TryParse(_port, out ushort p); JoinAction(_joinAddress, p == 0 ? (ushort)7777 : p); });

            var row2 = UIKit.Row(col, 52f, null, 16f, 0);
            UIKit.Place(row2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 52f));
            row2.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Big(row2, "Settings", true, () => UIScreen.Show<SettingsScreen>());
            Big(row2, "Field manual", true, () => UIScreen.Show<ManualScreen>());
            Big(row2, "Sandbox rules", true, () => UIScreen.Show<SandboxScreen>());
            Big(row2, "Wave survival (new)", true, () => { if (_boot != null) _boot.StartNewGame(); Destroy(gameObject); });
            Big(row2, "Continue waves", SaveSystem.HasSave, () => { if (_boot != null) _boot.ContinueGame(); Destroy(gameObject); });
            Big(row2, "Quit", true, Quit);

            Text hint = UIKit.Label(_panel, "You lose what you carried if you die \u2014 never what you learned, never your secure pouch.", 16,
                new Color(0.5f, 0.54f, 0.6f), TextAnchor.MiddleCenter);
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 40f));
        }

        private void Slot(RectTransform parent, int i)
        {
            AccountProfile a = ProfileStore.Account;
            CharacterProfile c = i < a.Characters.Count ? a.Characters[i] : null;
            Image card = UIKit.Panel(parent, "Slot", new Color(1f, 1f, 1f, c != null && i == a.ActiveIndex ? 0.1f : 0.05f));
            UIKit.Size(card.gameObject, 440f, 240f);
            RectTransform col = UIKit.Column(card.rectTransform, 6f, 14);
            UIKit.Stretch(col);
            if (c == null)
            {
                UIKit.Label(col, "Empty slot", 22, UIKit.TextDim, TextAnchor.MiddleCenter);
                UIKit.Size(UIKit.Button(col, "New runner", () => ShowCreate(), UIKit.Accent, 20).gameObject, -1f, 54f);
                return;
            }
            ProfessionDef p = ProfessionCatalog.Get(c.Profession);
            UIKit.Label(col, c.Name + (c.Ironman ? "  [IRONMAN]" : ""), 24, c.Dead ? UIKit.Bad : UIKit.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Label(col, "Level " + c.Level + " " + p.Name + " \u00b7 " + Factions.NameOf(c.Faction), 16, UIKit.TextDim);
            UIKit.Label(col, c.Dead ? "Dead: " + (c.CauseOfDeath ?? "the Zone") : "Day " + c.Day + " \u00b7 raids " + c.Raids + " \u00b7 extracts " + c.Extracts + " \u00b7 " + HubCatalog.Get(c.Hub).Name, 15, UIKit.TextDim);
            RectTransform row = UIKit.Row(col, 46f, null, 8f, 0);
            int idx = i;
            UIKit.Button(row, c.Dead ? "Gone" : "Play", () =>
            {
                if (c.Dead) return;
                ProfileStore.SelectCharacter(idx);
                if (_boot != null) _boot.StartHub();
                Destroy(gameObject);
            }, c.Dead ? UIKit.ButtonColor : UIKit.Accent, 20).interactable = !c.Dead;
            UIKit.Button(row, "Delete", () => ConfirmDelete(idx), UIKit.ButtonColor, 16);
        }

        private void ConfirmDelete(int idx)
        {
            Clear();
            CharacterProfile c = ProfileStore.Account.Characters[idx];
            Text t = UIKit.Label(_panel, "Delete " + c.Name + "? Their level, stash and hideout are gone for good.\n(What the account learned about the Zone stays.)", 24, UIKit.TextColor, TextAnchor.MiddleCenter);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 120f));
            RectTransform row = UIKit.Place(UIKit.Row(_panel, 54f, null, 20f, 0), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 54f));
            Big(row, "Delete", true, () => { ProfileStore.DeleteCharacter(idx); ShowTitle(); });
            Big(row, "Keep", true, ShowTitle);
        }

        #region Character creation
        private string _name = "";
        private string _prof = "drifter";
        private string _faction = Factions.Freehold;
        private readonly List<string> _traits = new List<string>();
        private string _blood;
        private bool _ironman;

        private void ShowCreate()
        {
            Clear();
            if (_blood == null) _blood = BloodTypes.Roll();
            Text title = UIKit.Label(_panel, "A new runner", 40, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 60f));
            RectTransform area = UIKit.Place(UIKit.Rect("Create", _panel), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800f, 820f));
            RectTransform[] cols = HubUI.Columns(area, 16f, 1f, 1.2f, 1f);

            // Who.
            HubUI.Card(cols[0]);
            RectTransform a = HubUI.Scroll(cols[0], 0f, 0f, 6f);
            HubUI.Heading(a, "Name", 20);
            UIKit.Size(UIKit.Input(a, _name, "what the Zone will call you", v => _name = v, 20).gameObject, -1f, 44f);
            HubUI.Heading(a, "Faction", 20);
            foreach (string f in Factions.Joinable)
            {
                Factions.Def d = Factions.Get(f);
                string pick = f;
                UIKit.Size(UIKit.Button(a, d.Name, () => { _faction = pick; ShowCreate(); }, f == _faction ? d.Color * 0.8f : UIKit.ButtonColor, 18).gameObject, -1f, 40f);
                HubUI.Para(a, d.Blurb, 13, UIKit.TextDim);
            }
            HubUI.Heading(a, "Blood type: " + _blood, 18);
            UIKit.Size(UIKit.Button(a, "Reroll blood type", () => { _blood = BloodTypes.Roll(); ShowCreate(); }, UIKit.ButtonColor, 15).gameObject, -1f, 34f);
            HubUI.Para(a, "It matters: transfusions need a compatible donor. O- gives to anyone; AB+ takes from anyone.", 13, UIKit.TextDim);
            UIKit.Toggle(a, "Ironman (one life — death deletes the runner)", _ironman, v => _ironman = v);

            // Profession.
            HubUI.Card(cols[1]);
            RectTransform b = HubUI.Scroll(cols[1], 0f, 0f, 4f);
            HubUI.Heading(b, "Profession", 20);
            foreach (ProfessionDef p in ProfessionCatalog.Professions)
            {
                var sb = new System.Text.StringBuilder(p.Blurb);
                foreach ((PlayerSkills.Skill s, int lv) in p.Skills) sb.Append("  · ").Append(s).Append(' ').Append(lv);
                if (p.Kit.Length > 0) { sb.Append("\nKit:"); foreach ((string id, int n) in p.Kit) sb.Append(' ').Append(n).Append("x ").Append(Trading.NameOf(id)); }
                string pick = p.Id;
                UIKit.Size(UIKit.Button(b, p.Name + "  (" + p.Points + " trait points)", () => { _prof = pick; ShowCreate(); }, p.Id == _prof ? UIKit.Accent : UIKit.ButtonColor, 17).gameObject, -1f, 38f);
                HubUI.Para(b, sb.ToString(), 13, UIKit.TextDim);
            }

            // Traits.
            int left = ProfessionCatalog.PointsLeft(_prof, _traits);
            HubUI.Card(cols[2]);
            RectTransform c = HubUI.Scroll(cols[2], 0f, 0f, 3f);
            HubUI.Heading(c, "Traits — " + left + " points left", 20, left < 0 ? UIKit.Bad : UIKit.Accent);
            foreach (TraitDef t in ProfessionCatalog.Traits)
            {
                bool on = _traits.Contains(t.Id);
                bool blocked = !on && ProfessionCatalog.Conflicts(_traits, t);
                TraitDef pick = t;
                Button btn = UIKit.Button(c, (on ? "\u2714 " : "") + t.Name + "  (" + (t.Cost > 0 ? "-" : "+") + Mathf.Abs(t.Cost) + ")", () =>
                {
                    if (_traits.Contains(pick.Id)) _traits.Remove(pick.Id); else _traits.Add(pick.Id);
                    ShowCreate();
                }, on ? (t.Cost > 0 ? new Color(0.3f, 0.5f, 0.3f) : new Color(0.55f, 0.3f, 0.25f)) : UIKit.ButtonColor, 15);
                btn.interactable = !blocked;
                UIKit.Size(btn.gameObject, -1f, 32f);
                HubUI.Para(c, t.Blurb, 12, UIKit.TextDim);
            }

            RectTransform row = UIKit.Place(UIKit.Row(_panel, 54f, null, 20f, 0), new Vector2(0.5f, 0.04f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 54f));
            Big(row, "Back", true, ShowTitle);
            Big(row, "Into the bar", left >= 0 && _name.Trim().Length > 0, Create);
        }

        private void Create()
        {
            CharacterProfile c = ProfileStore.NewCharacter(_name, _prof, new List<string>(_traits), _faction, _blood, _ironman);
            ProfessionCatalog.ApplyKit(c);
            ProfileStore.AddCharacter(c);
            GameSession.SurvivorName = c.Name;
            if (_boot != null) _boot.StartHub();
            Destroy(gameObject);
        }
        #endregion

        private static Button Big(Transform parent, string text, bool enabled, System.Action onClick)
        {
            Button b = UIKit.Button(parent, text, onClick, enabled ? UIKit.ButtonColor : new Color(0.18f, 0.18f, 0.2f), 20);
            b.interactable = enabled;
            UIKit.Size(b.gameObject, 230f, 50f);
            return b;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
