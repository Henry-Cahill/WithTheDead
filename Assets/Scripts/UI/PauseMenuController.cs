using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The in-game pause menu (Esc). Freezes the world (<c>Time.timeScale = 0</c>) and opens a menu down the left of the
    /// screen, leaving the frozen scene visible on the right: Resume, Save Game, the camera view and the HUD style (both
    /// switch live, so the HUD behind the menu previews each view's own HUD), Settings, the Field manual and Save &amp;
    /// Quit. A survivor card on the right reads out health, supplies, injuries and the run so far. The mouse, the
    /// keyboard (arrows or WASD, Enter, Esc) and a gamepad's d-pad / A / B all drive it. Esc only opens it when no
    /// other panel is up, so it never collides with the inventory / skills / health screens (which also close on Esc).
    /// Built from code and created by the <see cref="GameBootstrapper"/> alongside the HUD.
    ///
    /// During an extraction raid there is nothing to save: the menu offers "Abandon Raid" instead (a second click
    /// confirms), which counts as missing in action — the kit is lost — and returns to the title screen. In the hub
    /// the profile saves itself as you go, so it offers "Quit to Title".
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        private const float AbandonConfirmSeconds = 4f;   // real seconds the "confirm" click stays armed
        private const float ColumnX = 120f, ItemWidth = 500f, ItemHeight = 54f, ItemGap = 8f, ItemsTop = -196f;
        private const float CardWidth = 420f, BarWidth = 160f;

        private static readonly Color Amber = new Color(0.95f, 0.68f, 0.25f, 1f);
        private static readonly Color Dim = new Color(0.62f, 0.66f, 0.72f, 1f);
        private static readonly Color Idle = new Color(0.74f, 0.77f, 0.82f, 1f);
        private static readonly Color DangerIdle = new Color(0.86f, 0.52f, 0.46f, 1f);
        private static readonly Color DangerHot = new Color(1f, 0.42f, 0.34f, 1f);
        private static readonly Color Good = new Color(0.55f, 0.88f, 0.5f, 1f);
        private static readonly Color Bad = new Color(1f, 0.4f, 0.34f, 1f);
        private static readonly Color Frost = new Color(0.55f, 0.75f, 1f, 1f);

        private static readonly CameraView[] AllViews = { CameraView.Isometric, CameraView.ThirdPerson, CameraView.FirstPerson, CameraView.Bodycam };
        private static readonly CameraView[] EyesOnly = { CameraView.FirstPerson, CameraView.Bodycam };
        private static readonly string[] HudStyleNames = { "Full", "Minimal", "Diegetic", "Hardcore" };

        private enum ItemKind { Action, Cycle }

        private sealed class Item
        {
            public ItemKind Kind;
            public string Label;
            public string Key;
            public bool Danger;
            public Action Activate;
            public Action<int> Cycle;
            public Func<string> Value;
            public Func<string> Description;

            public RectTransform Content;
            public CanvasGroup Group;
            public Image Highlight, Accent, LeftArrow, RightArrow;
            public Text Text, KeyText, ValueText;
            public string ShownValue;
            public float Hover;
        }

        private sealed class CardRow
        {
            public Text Value;
            public Image Fill;
        }

        private readonly List<Item> _items = new List<Item>();
        private readonly StringBuilder _sb = new StringBuilder(96);

        private UIFader _fader;
        private RectTransform _column, _card;
        private CanvasGroup _columnGroup, _cardGroup;
        private Text _eyebrow, _info, _description, _status;
        private string _shownDescription;
        private float _statusTimer;
        private int _selected;
        private float _openedAt;
        private bool _open;
        private bool _prevBlocked;   // was some OTHER panel blocking gameplay last frame?
private GameManager _gm;
private ExtractionManager _em;

        private Item _abandon;
        private bool _abandonArmed;
        private float _abandonArmedAt;

        // The survivor card.
        private Text _cardName, _condition, _run;
        private CardRow _health, _stamina, _food, _water, _rest;
        private Text _warmth;
        private float _cardTimer;
        private PlayerStats _cardFor;
        private PlayerWounds _wounds;

        /// <summary>
        /// Indicates whether the pause menu is currently open.
        /// </summary>
        public bool IsOpen => _open;

        private void Awake()
{
    Build();
    _gm = GameManager.Instance;
    _em = ExtractionManager.Instance;
}

        private void OnEnable() => UIScreen.Closed += OnScreenClosed;

        private void OnDisable()
        {
            UIScreen.Closed -= OnScreenClosed;
            // Never leave the world frozen if this object is torn down while paused.
            if (_open)
            {
                _open = false;
                UIState.GameplayBlocked = false;
                Time.timeScale = 1f;
            }
        }

        // A screen opened from the menu (settings, the manual) unblocks gameplay as it closes, but the menu is still up:
        // block again at once, before the camera can take the cursor for a frame.
        private void OnScreenClosed()
        {
            if (_open) UIState.GameplayBlocked = true;
        }

        #region Flow
        private void Update()
        {
            // _gm cached in Awake
            // _em cached in Awake
bool esc = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            if (_open) TickOpen(esc);
            else
            {
                bool playing = _gm != null && _gm.State == GameState.Playing;
                // A finished raid belongs to its report screen.
                if (_em != null && _em.RaidOver) playing = false;
                bool free = !UIState.GameplayBlocked && !UIState.BuildMode && !UIState.ActionMenuOpen
                            && UIState.EscapeConsumedFrame != Time.frameCount
                            && !(PlayerInteractor.Current != null && PlayerInteractor.Current.IsBusy);   // Esc stops the job instead
                // Require a clear previous frame too, so the Esc that closes another panel
                // doesn't also pop the pause menu open on the same press.
                if (esc && playing && free && !_prevBlocked) Open();
            }

            _prevBlocked = (UIState.GameplayBlocked || UIState.ActionMenuOpen) && !_open;
        }

        private void TickOpen(bool esc)
        {
            // A screen opened from here (settings, the manual) unblocks gameplay when it closes: the menu is still up.
            if (!UIState.GameplayBlocked) UIState.GameplayBlocked = true;
            if (Time.timeScale != 0f) Time.timeScale = 0f;

            if (_abandonArmed && Time.unscaledTime - _abandonArmedAt > AbandonConfirmSeconds) DisarmAbandon();
            Animate(Time.unscaledDeltaTime);

            // While that screen is up it owns the input (its own Esc closes it, and marks the frame as handled).
            if (UIScreen.Current != null) return;
            if (esc && UIState.EscapeConsumedFrame != Time.frameCount)
            {
                UIState.EscapeConsumedFrame = Time.frameCount;
                Resume();
                return;
            }
            ReadNavigation();
        }

        private void Open()
        {
            _open = true;
            UIState.GameplayBlocked = true;
            Time.timeScale = 0f;              // freeze the world (zombies, clock, physics)
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _openedAt = Time.unscaledTime;
            _statusTimer = 0f;
            _status.enabled = false;
            DisarmAbandon();
            RefreshHeader();
            _cardTimer = 0f;
            for (int i = 0; i < _items.Count; i++) _items[i].Hover = 0f;
            Select(0, false);
            Animate(0f);
            if (_fader != null) _fader.Show();
        }

        private void Resume()
        {
            _open = false;
            UIState.GameplayBlocked = false;
            Time.timeScale = 1f;
            DisarmAbandon();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (_fader != null) _fader.Hide();
        }

        private void OnSave()
        {
            GameManager.Instance?.QuickSave();
            Flash("Game saved", Good);
        }

        private void OnQuitToMenu()
        {
            GameManager.Instance?.QuickSave();
            _open = false;
            Time.timeScale = 1f;
            UIState.GameplayBlocked = false;
            GameManager.Instance?.Restart();   // reloads the scene → the bootstrapper shows the title screen
        }

        // The hub saves the profile as you go: commit the kit on the body and head for the title screen.
        private void OnQuitHub()
        {
            _open = false;
            // Unfreeze the world and unblock gameplay before returning to the title screen.
            Time.timeScale = 1f;
            UIState.GameplayBlocked = false;
            HubKit.Commit();
            GameFlow.ToMainMenu();
        }

        // First click arms it (the label asks for confirmation), a second click within a few seconds gives the raid up.
        private void OnAbandonRaid()
        {
            if (!_abandonArmed)
            {
                _abandonArmed = true;
                _abandonArmedAt = Time.unscaledTime;
                if (_abandon != null && _abandon.Text != null) _abandon.Text.text = "CLICK AGAIN TO ABANDON";
                _shownDescription = null;
                Sfx.PlayUI(SfxIds.UiError, 0.5f);
                return;
            }

            // Missing in action: the kit stays in town for good.
            if (ExtractionManager.Instance != null) ExtractionManager.Instance.Abandon();
            else RaidLoadout.Clear();

            _open = false;
            _abandonArmed = false;
            Time.timeScale = 1f;
            UIState.GameplayBlocked = false;
            GameSession.AutoStartRaid = false;   // back to the title screen, not straight into a new raid
            if (GameManager.Instance != null) GameManager.Instance.Restart();
            else
            {
                NoiseSystem.Clear();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void DisarmAbandon()
        {
            if (!_abandonArmed) return;
            _abandonArmed = false;
            if (_abandon != null && _abandon.Text != null) _abandon.Text.text = _abandon.Label;
            _shownDescription = null;
        }

        private void OpenScreen<T>() where T : UIScreen
        {
            Sfx.PlayUI(SfxIds.PageTurn, 0.5f);
            UIScreen.Show<T>();
        }

        private void Flash(string message, Color color)
        {
            _status.text = message;
            _status.color = color;
            _status.enabled = true;
            _statusTimer = 2.2f;
        }
        #endregion

        #region Views and HUD style
        private static CameraView[] Views => IsoCameraFollow.FirstPersonOnly ? EyesOnly : AllViews;

        private static CameraView CurrentView => IsoCameraFollow.Instance != null ? IsoCameraFollow.Instance.View : ViewState.Current;

        private void CycleView(int dir)
        {
            IsoCameraFollow cam = IsoCameraFollow.Instance;
            if (cam == null) return;
            CameraView[] views = Views;
            int at = Array.IndexOf(views, cam.View);
            int next = at < 0 ? 0 : (at + dir + views.Length) % views.Length;
            cam.SetView(views[next]);
        }

        private static string ViewName(CameraView v)
        {
            switch (v)
            {
                case CameraView.ThirdPerson: return "Over the shoulder";
                case CameraView.FirstPerson: return "First person";
                case CameraView.Bodycam: return "Bodycam";
                default: return "Isometric";
            }
        }

        private static string ViewDescription(CameraView v)
        {
            switch (v)
            {
                case CameraView.ThirdPerson:
                    return "Action HUD: vitals and supplies bottom-left, the weapon card with a pip per round bottom-right, hit markers and damage arcs.";
                case CameraView.FirstPerson:
                    return "Immersive HUD: hairline bars and a compact magazine that fade back while all is well, moodles as a row of small tiles.";
                case CameraView.Bodycam:
                    return "Found footage: a 1993 camcorder's REC light, tape counter, date stamp and heart rate. No crosshair \u2014 aim with the weapon.";
                default:
                    return "Tactical HUD: hand slots, the watch, moodles down the right and the supplies row \u2014 Project Zomboid-style.";
            }
        }

        private static void CycleHud(int dir)
        {
            GameSettings g = GameSettings.Current;
            int n = HudStyleNames.Length;
            g.Hud = (HudMode)(((int)g.Hud + dir + n) % n);
            g.Save();
        }

        private static string HudDescription(HudMode m)
        {
            switch (m)
            {
                case HudMode.Minimal: return "Only what matters right now: vitals, the weapon and moodles. No watch, mode box or key hints.";
                case HudMode.Diegetic: return "No HUD widgets: the world, your wristwatch and your notebook tell you everything.";
                case HudMode.Hardcore: return "No HUD and no crosshair. Prompts and captions stay.";
                default: return "Everything each view's HUD has to offer.";
            }
        }
        #endregion

        #region Input
        private void ReadNavigation()
        {
            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool up = (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) || (pad != null && pad.dpad.up.wasPressedThisFrame);
            bool down = (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) || (pad != null && pad.dpad.down.wasPressedThisFrame);
            bool left = (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) || (pad != null && pad.dpad.left.wasPressedThisFrame);
            bool right = (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) || (pad != null && pad.dpad.right.wasPressedThisFrame);
            bool submit = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                          || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            bool back = pad != null && (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);

            if (back) { Resume(); return; }
            if (up) Select((_selected - 1 + _items.Count) % _items.Count, true);
            if (down) Select((_selected + 1) % _items.Count, true);
            Item it = _selected >= 0 && _selected < _items.Count ? _items[_selected] : null;
            if (it != null && it.Kind == ItemKind.Cycle)
            {
                if (left) CycleItem(_selected, -1);
                if (right) CycleItem(_selected, 1);
            }
            if (submit) ActivateItem(_selected);
        }

        private void Select(int index, bool sound)
        {
            if (index < 0 || index >= _items.Count) return;
            if (index != _selected && sound) Sfx.PlayUI(SfxIds.UiClick, 0.22f, 1.25f);
            if (index != _selected) _shownDescription = null;
            _selected = index;
        }

        private void ActivateItem(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            Item it = _items[index];
            Select(index, false);
            if (it.Kind == ItemKind.Cycle)
            {
                CycleItem(index, 1);
                return;
            }
            Sfx.PlayUI(SfxIds.UiClick, 0.6f);
            it.Activate?.Invoke();
        }

        private void CycleItem(int index, int dir)
        {
            Item it = _items[index];
            Select(index, false);
            Sfx.PlayUI(SfxIds.UiClick, 0.45f, dir > 0 ? 1.1f : 0.95f);
            it.Cycle?.Invoke(dir);
            _shownDescription = null;
        }
        #endregion

        #region Animation and live text
        private void Animate(float dt)
        {
            float t = Time.unscaledTime - _openedAt;
            float enter = Ease(t / 0.3f);
            _column.anchoredPosition = new Vector2(ColumnX - 40f * (1f - enter), 0f);
            _columnGroup.alpha = enter;
            float cardIn = Ease((t - 0.08f) / 0.3f);
            _card.anchoredPosition = new Vector2(-110f + 40f * (1f - cardIn), 10f);
            _cardGroup.alpha = cardIn;

            for (int i = 0; i < _items.Count; i++)
            {
                Item it = _items[i];
                float appear = Ease((t - 0.06f - 0.035f * i) / 0.24f);
                it.Group.alpha = appear;
                it.Hover = Mathf.MoveTowards(it.Hover, i == _selected ? 1f : 0f, dt * 9f);
                float h = Ease(it.Hover);
                it.Content.anchoredPosition = new Vector2(-22f * (1f - appear) + 10f * h, 0f);
                it.Highlight.color = new Color(1f, 1f, 1f, 0.11f * h);
                Color accent = it.Danger ? DangerHot : Amber;
                it.Accent.color = HudUI.WithAlpha(accent, 0.25f + 0.75f * h);
                it.Accent.rectTransform.sizeDelta = new Vector2(3f + 3f * h, ItemHeight - 16f);
                Color idle = it.Danger ? DangerIdle : Idle;
                Color hot = it.Danger ? DangerHot : Color.white;
                if (it.Danger && _abandonArmed && it == _abandon) idle = hot = HudUI.WithAlpha(DangerHot, HudUI.Pulse(7f, 0.55f));
                it.Text.color = Color.Lerp(idle, hot, h);
                if (it.Kind == ItemKind.Cycle)
                {
                    string v = it.Value != null ? it.Value() : "";
                    if (!ReferenceEquals(v, it.ShownValue)) { it.ShownValue = v; it.ValueText.text = v; }
                    Color arrow = Color.Lerp(HudUI.WithAlpha(Dim, 0.5f), Amber, h);
                    it.LeftArrow.color = arrow;
                    it.RightArrow.color = arrow;
                    it.ValueText.color = Color.Lerp(Idle, Color.white, h);
                }
            }

            UpdateDescription();
            if (_statusTimer > 0f)
            {
                _statusTimer -= dt;
                _status.color = HudUI.WithAlpha(_status.color, Mathf.Clamp01(_statusTimer / 0.5f));
                if (_statusTimer <= 0f) _status.enabled = false;
            }

            _cardTimer -= dt;
            if (_cardTimer <= 0f)
            {
                _cardTimer = 0.25f;
                RefreshCard();
                RefreshInfo();
            }
        }

        private void UpdateDescription()
        {
            if (_selected < 0 || _selected >= _items.Count) return;
            Item it = _items[_selected];
            string d;
            if (it == _abandon && _abandonArmed) d = "Counts as missing in action \u2014 everything you carry is lost. Click again within a few seconds to confirm.";
            else d = it.Description != null ? it.Description() : "";
            if (ReferenceEquals(d, _shownDescription)) return;
            _shownDescription = d;
            _description.text = d;
        }

        private static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            float inv = 1f - x;
            return 1f - inv * inv * inv;
        }

        // The small caps above the title: the mode and where you are.
        private void RefreshHeader()
        {
            string mode, where = null;
            if (GameSession.IsExtraction)
            {
                mode = GameSession.IsScavRun ? "Scav run" : GameSession.IsOpenZone ? "Open zone" : GameSession.IsMonsterRun ? "Hunt" : "Extraction raid";
                where = ZoneCatalog.Get(GameSession.ZoneId).Name;
            }
            else if (GameSession.IsHub)
            {
                CharacterProfile c = ProfileStore.ActiveCharacter;
                mode = "The hub";
                where = HubCatalog.Get(c != null ? c.Hub : HubCatalog.Kettle).Name;
            }
            else
            {
                GameManager gm = GameManager.Instance;
                mode = "Wave survival";
                if (gm != null && gm.Wave > 0) where = "Wave " + gm.Wave;
            }
            _eyebrow.text = string.IsNullOrEmpty(where) ? Spaced(mode) : Spaced(mode) + "     \u00b7     " + Spaced(where);
            RefreshInfo();
        }

        // "Clone Z · Day 3 · 8:02 PM · 40°F outside · Dec 17, 1993", refreshed a few times a second while open.
        private void RefreshInfo()
        {
            _sb.Length = 0;
            _sb.Append(SurvivorName());
            TimeOfDay tod = TimeOfDay.Instance;
            if (tod != null)
            {
                int h = Mathf.FloorToInt(tod.Hour) % 24;
                int m = Mathf.FloorToInt((tod.Hour - Mathf.Floor(tod.Hour)) * 60f);
                _sb.Append("  \u00b7  Day ").Append(tod.Day);
                _sb.Append("  \u00b7  ").Append(h % 12 == 0 ? 12 : h % 12).Append(':').Append(HudUI.Two(m)).Append(h < 12 ? " AM" : " PM");
            }
            _sb.Append("  \u00b7  ").Append(Mathf.RoundToInt(Climate.AirTemperatureF)).Append("\u00b0F outside");
            if (tod != null)
            {
                DateTime d = tod.Date;
                _sb.Append("  \u00b7  ").Append(d.ToString("MMM d, yyyy", System.Globalization.CultureInfo.InvariantCulture));
            }
            string s = _sb.ToString();
            if (s != _info.text) _info.text = s;
        }

        private static string SurvivorName()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null && (GameSession.IsHub || (GameSession.IsExtraction && !GameSession.IsBorrowedBody)) && !string.IsNullOrEmpty(c.Name)) return c.Name;
            return GameSession.SurvivorName;
        }

        private static string Spaced(string s) => HudUI.Spaced(s);
        #endregion

        #region Survivor card
        private void RefreshCard()
        {
            PlayerStats s = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (s != _cardFor)
            {
                _cardFor = s;
                _wounds = s != null ? s.GetComponent<PlayerWounds>() : null;
            }
            _cardName.text = SurvivorName();
            if (s == null)
            {
                _condition.text = "";
                _run.text = RunLine();
                return;
            }

            float hp = s.HealthNormalized;
            SetRow(_health, hp, HudUI.Condition(hp), Percent(hp));
            float e = Mathf.Clamp01(s.Endurance);
            SetRow(_stamina, e, e < CloneZConfig.EnduranceWarning ? HudUI.Amber : HudUI.Ink, Percent(e));
            SetRow(_food, 1f - s.Hunger, NeedColor(s.Hunger), Word(s.Hunger, "Full", "Peckish", "Hungry", "Starving"));
            SetRow(_water, 1f - s.Thirst, NeedColor(s.Thirst), Word(s.Thirst, "Quenched", "Thirsty", "Parched", "Dehydrated"));
            SetRow(_rest, 1f - s.Fatigue, NeedColor(s.Fatigue), Word(s.Fatigue, "Rested", "Drowsy", "Tired", "Exhausted"));

            float t = s.Temperature;
            string feel; Color fc;
            if (t < CloneZConfig.TempHypothermiaThreshold + 0.02f) { feel = "Freezing"; fc = Frost; }
            else if (t < 0.25f) { feel = "Very cold"; fc = Frost; }
            else if (t < 0.33f) { feel = "Cold"; fc = Frost; }
            else if (t < CloneZConfig.TempComfortLow) { feel = "Chilly"; fc = Frost; }
            else if (t > CloneZConfig.TempHyperthermiaThreshold - 0.01f) { feel = "Overheating"; fc = Bad; }
            else if (t > 0.78f) { feel = "Very hot"; fc = Bad; }
            else if (t > 0.7f) { feel = "Hot"; fc = HudUI.Amber; }
            else if (t > CloneZConfig.TempComfortHigh) { feel = "Warm"; fc = HudUI.Amber; }
            else { feel = "Comfortable"; fc = Good; }
            _warmth.text = feel + (s.Wetness >= CloneZConfig.WetnessDampThreshold ? (s.Wetness >= 0.55f ? ", wet" : ", damp") : "");
            _warmth.color = fc;

            int wounds = _wounds != null ? _wounds.WoundCount : 0;
            bool bleeding = _wounds != null && _wounds.IsBleeding;
            _sb.Length = 0;
            if (s.Infected) _sb.Append("Infected ").Append(Mathf.RoundToInt(s.Infection)).Append('%');
            if (wounds > 0)
            {
                if (_sb.Length > 0) _sb.Append("  \u00b7  ");
                _sb.Append(wounds).Append(wounds == 1 ? " wound" : " wounds");
            }
            if (bleeding) _sb.Append(_sb.Length > 0 ? ", bleeding" : "Bleeding");
            bool hurt = _sb.Length > 0;
            _condition.text = hurt ? _sb.ToString() : "No injuries";
            _condition.color = s.Infected || bleeding ? Bad : hurt ? HudUI.Amber : Good;
            _run.text = RunLine();
        }

        private string RunLine()
        {
            _sb.Length = 0;
            if (GameSession.IsExtraction)
            {
                ExtractionManager em = ExtractionManager.Instance;
                if (em != null)
                {
                    int secs = Mathf.CeilToInt(em.RaidSecondsRemaining);
                    if (secs > 0) _sb.Append("Dawn in ").Append(HudUI.Int(secs / 3600)).Append(':').Append(HudUI.Two(secs / 60 % 60)).Append(':').Append(HudUI.Two(secs % 60));
                    if (_sb.Length > 0) _sb.Append("  \u00b7  ");
                    int exits = em.Exits.Count;
                    _sb.Append(exits).Append(exits == 1 ? " exit known" : " exits known");
                }
            }
            else if (GameSession.IsHub)
            {
                CharacterProfile c = ProfileStore.ActiveCharacter;
                if (c != null) _sb.Append("Day ").Append(c.Day).Append("  \u00b7  ").Append(c.Raids).Append(" raids  \u00b7  ").Append(c.Extracts).Append(" extracts");
            }
            else
            {
                GameManager gm = GameManager.Instance;
                if (gm != null)
                    _sb.Append("Wave ").Append(gm.Wave).Append("  \u00b7  ").Append(gm.ZombieKills).Append(gm.ZombieKills == 1 ? " kill" : " kills")
                       .Append("  \u00b7  survived ").Append(HudUI.Clock(Mathf.FloorToInt(gm.TimeSurvived)));
            }
            return _sb.ToString();
        }

        private static void SetRow(CardRow row, float v01, Color color, string value)
        {
            row.Fill.fillAmount = Mathf.Clamp01(v01);
            row.Fill.color = color;
            if (row.Value.text != value) row.Value.text = value;
        }

        private static string Percent(float v01) => HudUI.Int(Mathf.RoundToInt(Mathf.Clamp01(v01) * 100f)) + "%";

        private static Color NeedColor(float bad01) => bad01 >= 0.85f ? Bad : bad01 >= 0.6f ? HudUI.Amber : HudUI.Ink;

        private static string Word(float bad01, string fine, string mild, string bad, string worst)
            => bad01 >= 0.92f ? worst : bad01 >= 0.6f ? bad : bad01 >= 0.35f ? mild : fine;
        #endregion

        #region UI construction
        private void Build()
        {
            Canvas canvas = UIKit.CreateCanvas("PauseMenu Canvas", 190, transform);   // above the HUD/panels, below the title screen (200)
            RectTransform root = (RectTransform)canvas.transform;

            RectTransform panel = HudUI.Stretch(root, "PausePanel");
            panel.gameObject.AddComponent<CanvasGroup>();
            _fader = panel.gameObject.AddComponent<UIFader>();   // starts hidden (Awake sets alpha 0)

            // Dim the frozen world, darken its edges, and lay a deep scrim down the left for the menu.
            Image dim = HudUI.Stretch(panel, "Dim").gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.025f, 0.035f, 0.5f);
            Image vignette = HudUI.Stretch(panel, "Vignette").gameObject.AddComponent<Image>();
            vignette.sprite = HudSprites.Vignette;
            vignette.color = new Color(0f, 0f, 0f, 0.8f);
            vignette.raycastTarget = false;
            RectTransform scrim = HudUI.Rect(panel, "Scrim", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1180f, 0f));
            scrim.anchorMin = Vector2.zero;
            scrim.anchorMax = new Vector2(0f, 1f);
            var scrimImg = scrim.gameObject.AddComponent<Image>();
            scrimImg.sprite = HudSprites.ScrimLeft;
            scrimImg.color = new Color(0.008f, 0.01f, 0.016f, 0.94f);
            scrimImg.raycastTarget = false;

            BuildColumn(panel);
            BuildCard(panel);
            BuildFooter(panel);
        }

        private void BuildColumn(RectTransform panel)
        {
            _column = HudUI.Rect(panel, "Menu", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(ColumnX, 0f), new Vector2(ItemWidth + 60f, 760f));
            _columnGroup = _column.gameObject.AddComponent<CanvasGroup>();
            var top = new Vector2(0f, 1f);

            _eyebrow = HudUI.Label(_column, "Eyebrow", 17, TextAnchor.UpperLeft, Amber, top, top, new Vector2(2f, 0f), new Vector2(ItemWidth + 60f, 24f), FontStyle.Bold);
            RectTransform bar = HudUI.Rect(_column, "TitleBar", top, top, new Vector2(-24f, -40f), new Vector2(6f, 74f));
            var barImg = bar.gameObject.AddComponent<Image>();
            barImg.color = Amber;
            barImg.raycastTarget = false;
            Text title = HudUI.Label(_column, "Title", 92, TextAnchor.UpperLeft, Color.white, top, top, new Vector2(-6f, -22f), new Vector2(ItemWidth + 60f, 110f), FontStyle.Bold);
            title.text = "PAUSED";
            _info = HudUI.Label(_column, "Info", 19, TextAnchor.UpperLeft, Dim, top, top, new Vector2(2f, -134f), new Vector2(ItemWidth + 260f, 26f));
            RectTransform rule = HudUI.Rect(_column, "Rule", top, top, new Vector2(0f, -176f), new Vector2(ItemWidth, 1f));
            var ruleImg = rule.gameObject.AddComponent<Image>();
            ruleImg.sprite = HudSprites.ScrimLeft;
            ruleImg.color = new Color(1f, 1f, 1f, 0.28f);
            ruleImg.raycastTarget = false;

            AddAction("RESUME", "ESC", Resume, () => "Back to it. The world stays frozen until you are.");
            if (GameSession.IsWaveSurvival)
                AddAction("SAVE GAME", "F5", OnSave, () => "Write this run to disk. F5 quick-saves anytime, too.");
            AddCycle("VIEW", () => ViewName(CurrentView), CycleView, () => ViewDescription(CurrentView));
            AddCycle("HUD STYLE", () => HudStyleNames[Mathf.Clamp((int)GameSettings.Current.Hud, 0, HudStyleNames.Length - 1)], CycleHud,
                () => HudDescription(GameSettings.Current.Hud));
            AddAction("SETTINGS", "", OpenScreen<SettingsScreen>, () => "Brightness, field of view, crosshair, subtitles, sound and mouse sensitivity.");
            AddAction("FIELD MANUAL", "", OpenScreen<ManualScreen>, () => "The controls of every view, survival, medicine, the Zone, combat and more.");
            if (GameSession.IsExtraction)
            {
                _abandon = AddAction("ABANDON RAID", "", OnAbandonRaid, () => "Give the raid up and return to the title screen. Counts as missing in action.");
                _abandon.Danger = true;
            }
            else if (GameSession.IsHub)
                AddAction("QUIT TO TITLE", "", OnQuitHub, () => "Back to the title screen. The bar keeps your kit and your stash.");
            else
                AddAction("SAVE & QUIT TO MENU", "", OnQuitToMenu, () => "Save this run and return to the title screen.");

            for (int i = 0; i < _items.Count; i++) BuildItem(_items[i], i);

            float below = ItemsTop - _items.Count * (ItemHeight + ItemGap) - 10f;
            _description = HudUI.Label(_column, "Description", 18, TextAnchor.UpperLeft, Dim, top, top, new Vector2(2f, below), new Vector2(ItemWidth, 66f));
            _description.horizontalOverflow = HorizontalWrapMode.Wrap;
            _status = HudUI.Label(_column, "Status", 19, TextAnchor.UpperLeft, Good, top, top, new Vector2(2f, below - 70f), new Vector2(ItemWidth, 26f), FontStyle.Bold);
            _status.enabled = false;
        }

        private Item AddAction(string label, string key, Action activate, Func<string> description)
        {
            var it = new Item { Kind = ItemKind.Action, Label = label, Key = key, Activate = activate, Description = description };
            _items.Add(it);
            return it;
        }

        private void AddCycle(string label, Func<string> value, Action<int> cycle, Func<string> description)
            => _items.Add(new Item { Kind = ItemKind.Cycle, Label = label, Value = value, Cycle = cycle, Description = description });

        private void BuildItem(Item it, int index)
        {
            var top = new Vector2(0f, 1f);
            RectTransform rt = HudUI.Rect(_column, "Item " + it.Label, top, top, new Vector2(0f, ItemsTop - index * (ItemHeight + ItemGap)), new Vector2(ItemWidth, ItemHeight));
            it.Group = rt.gameObject.AddComponent<CanvasGroup>();
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;   // catches the pointer across the whole row
            int captured = index;
            Pointer(rt.gameObject, () => Select(captured, true), () => ActivateItem(captured));

            it.Content = HudUI.Stretch(rt, "Content");
            it.Highlight = HudUI.Stretch(it.Content, "Highlight").gameObject.AddComponent<Image>();
            it.Highlight.sprite = HudSprites.ScrimLeft;
            it.Highlight.color = Color.clear;
            it.Highlight.raycastTarget = false;
RectTransform accent = HudUI.Rect(it.Content, "Accent", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3f, ItemHeight - 16f));
it.Accent = accent.gameObject.AddComponent<Image>();
it.Accent.raycastTarget = false;

var wearObj = new GameObject($"Wear{index}");
var wearRt = wearObj.AddComponent<RectTransform>();
wearRt.SetParent(it.Accent.transform, false);
wearRt.anchorMin = wearRt.anchorMax = new Vector2(0.5f, 0f);
wearRt.pivot = new Vector2(0.5f, 0f);
wearRt.anchoredPosition = new Vector2(0f, 0f);
wearRt.sizeDelta = new Vector2(0.8f * it.Accent.rectTransform.sizeDelta.x, 2f);
var wearImg = wearObj.AddComponent<Image>();
wearImg.color = HudUI.WithAlpha(Dim, 0.4f);
wearImg.raycastTarget = false;

                     // metal‑cap visual on the accent bar
                     var capObj = new GameObject($"Cap{index}");
                     var capRt = capObj.AddComponent<RectTransform>();
                     capRt.SetParent(it.Accent.transform, false);
                     capRt.anchorMin = capRt.anchorMax = new Vector2(0.5f, 1f);
                     capRt.pivot = new Vector2(0.5f, 1f);
                     capRt.anchoredPosition = new Vector2(0f, 0f);
                     float w = 5f + (index % 3) * 0.5f;
                     capRt.sizeDelta = new Vector2(w, 4f);
                     var capImg = capObj.AddComponent<Image>();
                     capImg.color = HudUI.WithAlpha(Dim, 0.6f);
                     capImg.raycastTarget = false;
            it.Text = HudUI.Label(it.Content, "Label", 23, TextAnchor.MiddleLeft, Idle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(26f, 0f), new Vector2(320f, 30f), FontStyle.Bold);
            // subtle rivet accent for the row
            var rivet = new GameObject($"Rivet{index}");
            var rrt = rivet.AddComponent<RectTransform>();
            rrt.SetParent(it.Content, false);
            rrt.anchorMin = rrt.anchorMax = new Vector2(0f, 1f);
            rrt.pivot = new Vector2(0f, 1f);
            rrt.anchoredPosition = new Vector2(2f, -4f);
            rrt.sizeDelta = new Vector2(4f, 4f);
            var rimg = rivet.AddComponent<Image>();
            rimg.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
            it.Text.text = it.Label;

            if (it.Kind == ItemKind.Action)
            {
                if (!string.IsNullOrEmpty(it.Key))
                {
                    it.KeyText = HudUI.Label(it.Content, "Key", 14, TextAnchor.MiddleRight, HudUI.WithAlpha(Dim, 0.8f), new Vector2(1f, 0.5f),
                        new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(90f, 22f), FontStyle.Bold);
                    it.KeyText.text = it.Key;
                }
                return;
            }

            it.ValueText = HudUI.Label(it.Content, "Value", 19, TextAnchor.MiddleCenter, Idle, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-128f, 0f), new Vector2(170f, 28f), FontStyle.Bold);
            it.LeftArrow = Arrow(it.Content, new Vector2(-232f, 0f), 90f, () => Select(captured, true), () => CycleItem(captured, -1));
            it.RightArrow = Arrow(it.Content, new Vector2(-24f, 0f), -90f, () => Select(captured, true), () => CycleItem(captured, 1));
        }

        // A clickable chevron for a cycler (an up-arrow sprite turned left or right) with a generous hit area.
        private static Image Arrow(RectTransform parent, Vector2 pos, float rotation, Action enter, Action click)
        {
            RectTransform hitRt = HudUI.Rect(parent, "ArrowHit", new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(36f, 44f));
            var hit = hitRt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Pointer(hitRt.gameObject, enter, click);
            Image icon = HudUI.Icon(hitRt, "Arrow", HudIcon.Arrow, Dim, new Vector2(0.5f, 0.5f), Vector2.zero, 15f);
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            return icon;
        }

        private static void Pointer(GameObject go, Action enter, Action click)
        {
            var p = go.AddComponent<PausePointer>();
            p.Enter = enter;
            p.Click = click;
        }

        private void BuildCard(RectTransform panel)
        {
            var mid = new Vector2(1f, 0.5f);
            _card = HudUI.Panel(panel, "Survivor", mid, mid, new Vector2(-110f, 10f), new Vector2(CardWidth, 440f), out Image frame);
            _card.GetComponent<Image>().color = new Color(0.025f, 0.03f, 0.04f, 0.84f);
            frame.color = new Color(1f, 1f, 1f, 0.1f);
            UITheme.AddTopAccent(_card, HudUI.WithAlpha(Amber, 0.85f), 3f);
            _cardGroup = _card.gameObject.AddComponent<CanvasGroup>();
            _cardGroup.blocksRaycasts = false;

            var top = new Vector2(0f, 1f);
            HudUI.Label(_card, "Caption", 14, TextAnchor.UpperLeft, Amber, top, top, new Vector2(24f, -20f), new Vector2(300f, 20f), FontStyle.Bold).text = Spaced("Survivor");
            _cardName = HudUI.Label(_card, "Name", 26, TextAnchor.UpperLeft, Color.white, top, top, new Vector2(24f, -42f), new Vector2(CardWidth - 48f, 34f), FontStyle.Bold);
            Rule(_card, -90f);

            _health = Row(_card, 0, "Health");
            _stamina = Row(_card, 1, "Stamina");
            _food = Row(_card, 2, "Food");
            _water = Row(_card, 3, "Water");
            _rest = Row(_card, 4, "Rest");
            HudUI.Label(_card, "WarmthLabel", 17, TextAnchor.MiddleLeft, Dim, top, new Vector2(0f, 0.5f), new Vector2(24f, RowY(5)), new Vector2(100f, 24f)).text = "Warmth";
            _warmth = HudUI.Label(_card, "Warmth", 17, TextAnchor.MiddleLeft, Good, top, new Vector2(0f, 0.5f), new Vector2(124f, RowY(5)), new Vector2(CardWidth - 148f, 24f), FontStyle.Bold);

            Rule(_card, RowY(5) - 26f);
            _condition = HudUI.Label(_card, "Condition", 17, TextAnchor.UpperLeft, Good, top, top, new Vector2(24f, RowY(5) - 40f), new Vector2(CardWidth - 48f, 24f), FontStyle.Bold);
            _run = HudUI.Label(_card, "Run", 16, TextAnchor.UpperLeft, Dim, top, top, new Vector2(24f, RowY(5) - 70f), new Vector2(CardWidth - 48f, 44f));
            _run.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        private static float RowY(int i) => -118f - i * 34f;

        private static CardRow Row(RectTransform card, int index, string label)
        {
            var top = new Vector2(0f, 1f);
            float y = RowY(index);
            HudUI.Label(card, label, 17, TextAnchor.MiddleLeft, Dim, top, new Vector2(0f, 0.5f), new Vector2(24f, y), new Vector2(100f, 24f)).text = label;
            Image fill = HudUI.Bar(card, label + "Bar", top, new Vector2(0f, 0.5f), new Vector2(124f, y), new Vector2(BarWidth, 6f),
                new Color(1f, 1f, 1f, 0.08f), HudUI.Ink, out _);
            Text value = HudUI.Label(card, label + "Value", 16, TextAnchor.MiddleRight, HudUI.Ink, new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-24f, y), new Vector2(110f, 24f), FontStyle.Bold);
            return new CardRow { Fill = fill, Value = value };
        }

        private static void Rule(RectTransform parent, float y)
        {
            RectTransform rt = HudUI.Rect(parent, "Rule", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(CardWidth - 48f, 1f));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            img.raycastTarget = false;
        }

        private void BuildFooter(RectTransform panel)
        {
            var bl = Vector2.zero;
            string hint = GameSession.IsExtraction ? "Raids can't be saved \u2014 reach an exit to keep your kit."
                : GameSession.IsHub ? "The bar keeps your kit and your stash between raids."
                : "F5 quick-saves anytime.";
            HudUI.Label(panel, "Hint", 15, TextAnchor.LowerLeft, HudUI.WithAlpha(Amber, 0.8f), bl, bl, new Vector2(ColumnX, 76f), new Vector2(1200f, 22f)).text = hint;
            const string key = "<color=#F2EED8><b>";
            const string end = "</b></color>";
            HudUI.Label(panel, "Controls", 15, TextAnchor.LowerLeft, Dim, bl, bl, new Vector2(ColumnX, 46f), new Vector2(1200f, 22f)).text =
                key + "\u2191 \u2193" + end + "  Select        " + key + "\u2190 \u2192" + end + "  Change        "
                + key + "Enter" + end + "  Confirm        " + key + "Esc" + end + "  Resume        " + key + "V" + end + "  Next view (in play)";
        }
        #endregion
    }

    /// <summary>Forwards pointer hover and clicks on a pause-menu row (or a cycler's arrow) to the menu.</summary>
    public sealed class PausePointer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        /// <summary>
        /// Event invoked when the pointer enters the UI element.
        /// </summary>
        public Action Enter;
        /// <summary>
        /// Event invoked when the pointer clicks the UI element.
        /// </summary>
        public Action Click;

        /// <summary>
        /// Called by Unity when the pointer enters the UI element; triggers the Enter action.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData) => Enter?.Invoke();

        /// <summary>
        /// Called by Unity when the pointer clicks the UI element; triggers the Click action on left mouse button.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Click?.Invoke();
        }
    }
}
