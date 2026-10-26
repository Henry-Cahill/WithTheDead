using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The title screen: a winter night over a small Kentucky town, your runners (up to three per account) and making a
    /// new one, playing together (hosting or joining a server), the original wave-survival mode, and the settings and the
    /// field manual. Picking a runner takes you to their hub under the loading card — for a new runner, The Rusty Kettle
    /// in Bardstown, whose front door opens onto the town; a dead runner's slot can take a new runner.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        /// <summary>Set by the netcode: start a host (then enter the hub) / join an address. Null hides the buttons.</summary>
        public static System.Action<ushort> HostAction;
        public static System.Action<string, ushort> JoinAction;

        /// <summary>True while the title screen is up (the score plays its theme under it).</summary>
        public static bool Showing { get; private set; }

        private static readonly Color TitleRed = new Color(0.86f, 0.24f, 0.20f);
        private static readonly Color PlaceColor = new Color(0.88f, 0.83f, 0.74f);
        private static readonly Color CardColor = new Color(0.055f, 0.065f, 0.085f, 0.90f);
        private static readonly Color CardActive = new Color(0.10f, 0.11f, 0.14f, 0.95f);
        private static readonly Color HomeColor = new Color(0.90f, 0.72f, 0.44f);

        private GameBootstrapper _boot;
        private Canvas _canvas;
        private RectTransform _root, _panel;
        private string _joinAddress = "127.0.0.1";
        private string _port = "7777";

        /// <summary>Initialises the main menu UI with the provided bootstrapper.</summary>
        public void Bind(GameBootstrapper boot)
        {
            _boot = boot;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            UIKit.EnsureEventSystem();
            _canvas = UIKit.CreateCanvas("MainMenu Canvas", 200, transform);
            _root = (RectTransform)_canvas.transform;
            Image bg = UIKit.Panel(_root, "Backdrop", new Color(0.02f, 0.025f, 0.04f, 1f));
            UIKit.Stretch(bg.rectTransform);
            WinterBackdrop.Build(_root, 160, true);
            ShowTitle();
        }

        private void OnEnable() => Showing = true;

        private void OnDisable() => Showing = false;

        private void Clear()
        {
            if (_panel != null) Destroy(_panel.gameObject);
            _panel = UIKit.Rect("Panel", _root);
            UIKit.Stretch(_panel);
        }

        // Letters spread out, for the small caps under the title (a word gap comes out three spaces wide).
        private static string Spaced(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length * 2);
            for (int i = 0; i < s.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private void ShowTitle()
        {
            // Reset UI state before rebuilding title screen
            ResetCreateState();
            // Clear previous UI elements to start fresh
            Clear();
            // Add title text with large font and bold styling
            Text title = UIKit.Label(_panel, "WithTheDead", 112, TitleRed, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.885f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 140f));
            Text place = UIKit.Label(_panel, Spaced("BARDSTOWN, KENTUCKY") + "      \u00b7      " + Spaced("WINTER 1993"), 22, PlaceColor, TextAnchor.MiddleCenter);
            UIKit.Place(place.rectTransform, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 34f));
            AccountProfile a = ProfileStore.Account;
            Text season = UIKit.Label(_panel, "Season " + a.Season + (a.Wipes > 0 ? "  \u00b7  " + a.Wipes + " wipes survived" : ""), 16, UIKit.TextDim, TextAnchor.MiddleCenter);
            UIKit.Place(season.rectTransform, new Vector2(0.5f, 0.765f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 26f));

            // Runners.
            Text runners = UIKit.Label(_panel, Spaced("YOUR RUNNERS"), 15, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(runners.rectTransform, new Vector2(0.5f, 0.69f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1364f, 24f));
            RectTransform slots = UIKit.Place(UIKit.Rect("Runners", _panel), new Vector2(0.5f, 0.555f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 250f));
            var h = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 22f;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            for (int i = 0; i < AccountProfile.MaxCharacters; i++) Slot(slots, i);

            // The other ways to play: together, or the waves.
            RectTransform modes = UIKit.Place(UIKit.Rect("Modes", _panel), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 124f));
            var mh = modes.gameObject.AddComponent<HorizontalLayoutGroup>();
            mh.spacing = 22f;
            mh.childAlignment = TextAnchor.MiddleCenter;
            mh.childControlWidth = mh.childControlHeight = true;
            mh.childForceExpandWidth = mh.childForceExpandHeight = false;
            Together(modes);
            Waves(modes);

            // The rest, small, along the foot.
            RectTransform foot = UIKit.Row(_panel, 40f, null, 12f, 0);
            UIKit.Place(foot, new Vector2(0.5f, 0.125f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 40f));
            foot.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Small(foot, "Settings", () => UIScreen.Show<SettingsScreen>());
            Small(foot, "Field manual", () => UIScreen.Show<ManualScreen>());
            Small(foot, "Sandbox rules", () => UIScreen.Show<SandboxScreen>());
            Small(foot, "Quit", Quit);

            Text hint = UIKit.Label(_panel, "You lose what you carried if you die \u2014 never what you learned, never your secure pouch.", 15,
                new Color(0.66f, 0.70f, 0.76f), TextAnchor.MiddleCenter);
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 26f));
            Text osm = UIKit.Label(_panel, "Map data \u00a9 OpenStreetMap contributors", 13, new Color(0.68f, 0.71f, 0.76f), TextAnchor.MiddleRight);
            UIKit.Place(osm.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 44f), new Vector2(520f, 22f));
        }

        private void Slot(RectTransform parent, int i)
        {
            AccountProfile a = ProfileStore.Account;
            CharacterProfile c = i < a.Characters.Count ? a.Characters[i] : null;
            bool active = c != null && !c.Dead && i == a.ActiveIndex;
            Image card = UIKit.Panel(parent, "Slot", active ? CardActive : CardColor);
            UIKit.Size(card.gameObject, 440f, 250f);
            // A strip along the top: the accent for the runner you last played, else their faction's colour (red for the
            // dead, faint for an empty slot).
            Color strip = c == null ? new Color(1f, 1f, 1f, 0.08f) : c.Dead ? UIKit.Bad : active ? UIKit.Accent : Factions.Get(c.Faction).Color;
            Image bar = UIKit.Panel(card.rectTransform, "Strip", strip);
            RectTransform br = bar.rectTransform;
            br.anchorMin = new Vector2(0f, 1f);
            br.anchorMax = new Vector2(1f, 1f);
            br.pivot = new Vector2(0.5f, 1f);
            br.anchoredPosition = Vector2.zero;
            br.sizeDelta = new Vector2(0f, active ? 6f : 4f);
            RectTransform col = UIKit.Column(card.rectTransform, 6f, 16);
            UIKit.Stretch(col, 0f, 0f, 0f, 4f);
            if (c == null)
            {
                UIKit.Label(col, "Empty slot", 22, UIKit.TextDim, TextAnchor.MiddleCenter);
                UIKit.Label(col, "A new runner starts at The Rusty Kettle, a roadhouse in Bardstown.", 14, UIKit.TextDim, TextAnchor.MiddleCenter);
                UIKit.Size(UIKit.Button(col, "New runner", () => NewRunner(-1), UIKit.Accent, 20).gameObject, -1f, 54f);
                return;
            }
            ProfessionDef p = ProfessionCatalog.Get(c.Profession);
            HubDef hub = HubCatalog.Get(c.Hub);
            UIKit.Label(col, c.Name + (c.Ironman ? "  <size=14><color=#e0b050>IRONMAN</color></size>" : ""), 26, c.Dead ? UIKit.Bad : UIKit.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Label(col, "Level " + c.Level + " " + p.Name + "  \u00b7  " + Factions.NameOf(c.Faction), 16, UIKit.TextDim);
            if (c.Dead) UIKit.Label(col, "Dead: " + (string.IsNullOrEmpty(c.CauseOfDeath) ? "the Zone" : c.CauseOfDeath), 15, UIKit.Bad);
            else
            {
                UIKit.Label(col, "Day " + c.Day + "  \u00b7  " + c.Raids + " raids  \u00b7  " + c.Extracts + " extracts", 15, UIKit.TextColor);
                UIKit.Label(col, "At " + hub.Name + (hub.Id == HubCatalog.Kettle ? ", Bardstown" : ""), 15, HomeColor);
            }
            RectTransform row = UIKit.Row(col, 46f, null, 8f, 0);
            row.GetComponent<LayoutElement>().flexibleHeight = 0f;   // keep to its own height, not the card's spare room
            int idx = i;
            // A dead runner can't be played, so their slot takes a new runner instead.
            Button main = c.Dead
                ? UIKit.Button(row, "New runner", () => NewRunner(idx), UIKit.Accent, 20)
                : UIKit.Button(row, hub.Id == HubCatalog.Kettle ? "Into the Kettle" : "Play", () =>
                {
                    ProfileStore.SelectCharacter(idx);
                    EnterHub();
                }, UIKit.Accent, 20);
            Button delete = UIKit.Button(row, "Delete", () => ConfirmDelete(idx), UIKit.ButtonColor, 16);
            // The row hands out width by flexible share only; a bare button has none and would collapse to nothing.
            UIKit.Size(main.gameObject, -1f, -1f, 2f);
            UIKit.Size(delete.gameObject, -1f, -1f, 1f);
        }

        private void Together(RectTransform parent)
        {
            RectTransform card = GroupCard(parent, "PLAY TOGETHER", 800f);
            RectTransform row = UIKit.Row(card, 48f, null, 12f, 0);
            Big(row, "Host a server", HostAction != null, () => { ushort.TryParse(_port, out ushort p); HostAction(p == 0 ? (ushort)7777 : p); }, 200f);
            UIKit.Size(UIKit.Input(row, _joinAddress, "address", v => _joinAddress = v, 18).gameObject, 230f, 44f);
            UIKit.Size(UIKit.Input(row, _port, "port", v => _port = v, 18).gameObject, 96f, 44f);
            Big(row, "Join", JoinAction != null, () => { ushort.TryParse(_port, out ushort p); JoinAction(_joinAddress, p == 0 ? (ushort)7777 : p); }, 150f);
        }

        private void Waves(RectTransform parent)
        {
            RectTransform card = GroupCard(parent, "WAVE SURVIVAL", 520f);
            RectTransform row = UIKit.Row(card, 48f, null, 12f, 0);
            Big(row, "New run", true, () => { if (_boot != null) _boot.StartNewGame(); Destroy(gameObject); }, 230f);
            Big(row, "Continue", SaveSystem.HasSave, () => { if (_boot != null) _boot.ContinueGame(); Destroy(gameObject); }, 230f);
        }

        // A titled card in the row of other ways to play: its heading over a row of controls.
        private static RectTransform GroupCard(RectTransform parent, string heading, float width)
        {
            Image card = UIKit.Panel(parent, heading, CardColor);
            UIKit.Size(card.gameObject, width, 120f);
            RectTransform col = UIKit.Column(card.rectTransform, 10f, 16);
            UIKit.Stretch(col);
            UIKit.Label(col, Spaced(heading), 15, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            return col;
        }

        // Off to the active runner's hub, under the loading card (the Kettle's town takes a while to build).
        private void EnterHub()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            HubDef hub = HubCatalog.Get(c != null ? c.Hub : HubCatalog.Kettle);
            LoadingScreen.Show(hub.Name.ToUpperInvariant(), KettleLobby.Where(hub));
            GameBootstrapper boot = _boot;
            LoadingScreen.Run(() => { if (boot != null) boot.StartHub(); });
            Destroy(gameObject);
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
        private int _replaceSlot = -1;

        private void ResetCreateState()
        {
            _replaceSlot = -1;
            _name = "";
            _prof = "drifter";
            _faction = Factions.Freehold;
            _traits.Clear();
            _blood = null;
            _ironman = false;
        }

        // A new runner for an empty slot (-1), or to take the place of the dead runner in replaceSlot.
        private void NewRunner(int replaceSlot)
        {
            _replaceSlot = replaceSlot;
            ShowCreate();
        }

        private void ShowCreate()
        {
            Clear();
            if (_blood == null) _blood = BloodTypes.Roll();
            Text title = UIKit.Label(_panel, "A new runner", 40, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(title.rectTransform, new Vector2(0.5f, 0.955f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 60f));
            Text where = UIKit.Label(_panel, "You'll start at The Rusty Kettle, a roadhouse on the edge of Bardstown, Kentucky. Its front door opens onto the town.",
                17, PlaceColor, TextAnchor.MiddleCenter);
            UIKit.Place(where.rectTransform, new Vector2(0.5f, 0.915f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 28f));
            RectTransform area = UIKit.Place(UIKit.Rect("Create", _panel), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1800f, 800f));
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
            Big(row, "Into the Kettle", left >= 0 && _name.Trim().Length > 0, Create);
        }

        private void Create()
        {
            CharacterProfile c = ProfileStore.NewCharacter(_name, _prof, new List<string>(_traits), _faction, _blood, _ironman);
            ProfessionCatalog.ApplyKit(c);
            ProfileStore.AddCharacter(c, _replaceSlot);
            GameSession.SurvivorName = c.Name;
            EnterHub();
        }
        #endregion

        private static Button Big(Transform parent, string text, bool enabled, System.Action onClick, float width = 230f)
        {
            Button b = UIKit.Button(parent, text, onClick, enabled ? UIKit.ButtonColor : new Color(0.18f, 0.18f, 0.2f), 20);
            b.interactable = enabled;
            UIKit.Size(b.gameObject, width, 50f);
            return b;
        }

        private static void Small(Transform parent, string text, System.Action onClick)
        {
            Button b = UIKit.Button(parent, text, onClick, new Color(0.13f, 0.15f, 0.18f, 0.92f), 16);
            UIKit.Size(b.gameObject, 170f, 36f);
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
