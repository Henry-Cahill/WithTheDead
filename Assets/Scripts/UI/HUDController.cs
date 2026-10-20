using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The code-built heads-up display, with its own layout for each camera view (<see cref="HudViewLayout"/>):
    /// <list type="bullet">
    /// <item><description><b>Isometric</b>, the tactical HUD of a Project Zomboid HUD mod: hand slots top-left, gas-mask
    /// filter / activity / digital watch / alarm bell top-right, moodle tiles down the right edge, the mode box with the
    /// status icon row and exits panel bottom-right, the movement icon and stamina bar bottom-left.</description></item>
    /// <item><description><b>Over the shoulder</b>, an action HUD (<see cref="HudShoulderView"/>): vitals and supplies
    /// bottom-left, the weapon card bottom-right, hit markers and damage arcs (<see cref="HudCombatFeedback"/>).</description></item>
    /// <item><description><b>First person</b>, an immersive HUD (<see cref="HudFirstPersonView"/>): hairline vitals and a
    /// compact magazine count that fade back while all is well, the moodles as a row of small tiles.</description></item>
    /// <item><description><b>Bodycam</b>, a 1993 camcorder's display (<see cref="HudBodycamView"/>).</description></item>
    /// </list>
    /// The HUD style setting (full / minimal / diegetic / hardcore) trims each view's HUD. Every view shares the raid
    /// feedback (spawn protection, extraction, dawn), interaction prompts, the focus label and a fixture's action menu,
    /// progress bars, toasts, broadcast captions, the water / power cut notices, key hints, the sleep overlay, the
    /// damage vignette and the wave survival death screen. Everything is generated at runtime so no scene wiring is
    /// required; the pieces live in the <c>Hud*</c> widget classes and this component just builds and ticks them.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private readonly HudContext _ctx = new HudContext();
        private readonly HudViewLayout _layout = new HudViewLayout();

        private HudHands _hands;
        private HudWatchRow _watch;
        private HudMoodles _moodles;
        private HudStamina _stamina;
        private HudStatusRow _status;
        private HudRaidOverlay _raid;
        private HudToasts _toasts;
        private HudHints _hints;
        private HudInteraction _interaction;
        private HudSubtitles _subtitles;
        private HudShoulderView _shoulder;
        private HudFirstPersonView _visor;
        private HudBodycamView _bodycam;
        private HudCombatFeedback _feedback;

        private HudGroup _handsGroup, _watchGroup, _moodleGroup, _staminaGroup, _iconsGroup, _boxGroup, _hintsGroup;
        private HudGroup _shoulderGroup, _visorGroup, _bodycamGroup, _feedbackGroup;

        private Image _damageVignette;
        private float _damageFlash;
        private Text _promptText, _sleepPromptText, _buildPromptText;
        private GameObject _searchRow, _buildRow;
        private Image _searchFill, _buildFill;
        private Text _searchLabel, _buildLabel;
        private string _shownSearchTarget, _shownBuildLabel;
        private Text _levelUpText;
        private float _levelUpTimer;
        private Text _waterNoticeText;
        private float _waterNoticeTimer;
        private bool _waterWasOn = true;
        private Text _powerNoticeText;
        private float _powerNoticeTimer;
        private bool _powerWasOn = true;
        private bool _powerSeen;
        private WaterMains _waterMains;
        private PowerGrid _powerGrid;
        private GameObject _sleepOverlay;
        private Text _sleepText;
        private int _sleepMinute = -1;
        private float _sleepTextTimer;
        private GameObject _gameOverPanel;
        private Text _gameOverText;
        private Text _viewBanner;
        private float _viewBannerTimer;
        private bool _viewSeen, _bannerPending;

        /// <summary>Hooks the HUD up to the survivor. Anything not passed is fetched from the
        /// <paramref name="stats"/> object (gear, movement, temperature and clothing always are).</summary>
        public void Bind(PlayerStats stats, PlayerInventory inventory = null, PlayerInteractor interactor = null, PlayerCombat combat = null, PlayerSleep sleep = null, PlayerSkills skills = null, PlayerBuilder builder = null, PlayerWounds wounds = null, PlayerPanic panic = null, PlayerFoodSickness sickness = null, PlayerFarming farming = null, PlayerHydration hydration = null)
        {
            Unbind();
            _ctx.Stats = stats;
            _ctx.Inventory = inventory != null ? inventory : Find<PlayerInventory>(stats);
            _ctx.Interactor = interactor != null ? interactor : Find<PlayerInteractor>(stats);
            _ctx.Combat = combat != null ? combat : Find<PlayerCombat>(stats);
            _ctx.Sleep = sleep != null ? sleep : Find<PlayerSleep>(stats);
            _ctx.Skills = skills != null ? skills : Find<PlayerSkills>(stats);
            _ctx.Builder = builder != null ? builder : Find<PlayerBuilder>(stats);
            _ctx.Wounds = wounds != null ? wounds : Find<PlayerWounds>(stats);
            _ctx.Panic = panic != null ? panic : Find<PlayerPanic>(stats);
            _ctx.Sickness = sickness != null ? sickness : Find<PlayerFoodSickness>(stats);
            _ctx.Farming = farming != null ? farming : Find<PlayerFarming>(stats);
            _ctx.Hydration = hydration != null ? hydration : Find<PlayerHydration>(stats);
            _ctx.Controller = Find<PlayerController>(stats);
            _ctx.Temperature = Find<PlayerTemperature>(stats);
            _ctx.Clothing = Find<PlayerClothing>(stats);
            _ctx.GearComponent = Find<PlayerGear>(stats);

            if (_ctx.Stats != null) _ctx.Stats.OnDamaged += OnPlayerDamaged;
            if (_ctx.Skills != null) _ctx.Skills.OnLevelUp += OnSkillLevelUp;
            if (_ctx.Inventory != null) _ctx.Inventory.Notice += OnNotice;
            if (_ctx.GearComponent != null) _ctx.GearComponent.Notice += OnNotice;
            if (_ctx.Interactor != null) _ctx.Interactor.Notice += OnNotice;
            if (_feedback != null) _feedback.BindCombat(_ctx.Combat);
            GameEvents.Toast += OnNotice;
            GameEvents.Subtitle += OnSubtitle;
        }

        private static T Find<T>(PlayerStats stats) where T : Component => stats != null ? stats.GetComponent<T>() : null;

        // Spoken lines from the world (NPCs, the radio, the Zone) go to the caption bar, if captions are on.
        private static void OnSubtitle(string speaker, string line)
        {
            if (!GameSettings.Current.Subtitles) return;
            Subtitles.Show(speaker, line, new Color(0.85f, 0.75f, 0.5f), Mathf.Clamp(2.5f + line.Length * 0.05f, 3f, 9f));
        }

        private void Unbind()
        {
            if (_ctx.Stats != null) _ctx.Stats.OnDamaged -= OnPlayerDamaged;
            if (_ctx.Skills != null) _ctx.Skills.OnLevelUp -= OnSkillLevelUp;
            if (_ctx.Inventory != null) _ctx.Inventory.Notice -= OnNotice;
            if (_ctx.GearComponent != null) _ctx.GearComponent.Notice -= OnNotice;
            if (_ctx.Interactor != null) _ctx.Interactor.Notice -= OnNotice;
            if (_feedback != null) _feedback.BindCombat(null);
            GameEvents.Toast -= OnNotice;
            GameEvents.Subtitle -= OnSubtitle;
        }

        private void Awake()
        {
            Build();
            _waterMains = WaterMains.Instance;
            _powerGrid = PowerGrid.Instance;
        }

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameOver += ShowGameOver;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameOver -= ShowGameOver;
            Unbind();
            _subtitles?.Dispose();
            _feedback?.Dispose();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float unscaledDt = Time.unscaledDeltaTime;
            bool blocked = UIState.GameplayBlocked;

            Keyboard kb = Keyboard.current;
            if (!blocked && kb != null && kb.mKey.wasPressedThisFrame) _status.ToggleInfo();

            // The exits / run panel stays up once asked for, whatever the HUD style.
            _boxGroup.ForceVisible = _status.InfoOpen;
            _layout.Tick(ViewState.Current, GameSettings.Current.Hud, unscaledDt);

            _hands.Tick(dt);
            _watch.Tick(dt);
            _moodles.Tick(dt);
            _stamina.Tick(dt);
            _status.Tick(unscaledDt);
            _raid.Tick(dt);
            _toasts.Tick(unscaledDt);
            _hints.Tick(dt, blocked);
            _interaction.Tick(blocked);
            _subtitles.Tick(dt, blocked);
            if (_shoulderGroup.Live) _shoulder.Tick(dt);
            if (_visorGroup.Live) _visor.Tick(dt, unscaledDt);
            if (_bodycamGroup.Live) _bodycam.Tick(dt, unscaledDt, _moodles.Summary(2, 3), _watch.Hunted);
            _feedback.Tick(unscaledDt);

            UpdatePrompts(blocked);
            UpdateProgressBars();
            UpdateNotices(dt);
            UpdateSleep(blocked);
            UpdateVignette();
            UpdateViewBanner(unscaledDt, blocked);
        }

        // The HUD has just been laid out for a view: re-dock what moves and reset what restarts.
        private void OnViewApplied(CameraView view)
        {
            _moodles.SetDock(view == CameraView.FirstPerson ? MoodleDock.Row : MoodleDock.Column, new Vector2(30f, 100f));
            _feedback.Arcs = view != CameraView.Isometric;
            _feedback.Markers = view == CameraView.ThirdPerson || view == CameraView.FirstPerson;
            if (view == CameraView.FirstPerson) _visor.Wake();
            if (view == CameraView.Bodycam) _bodycam.Wake();
            if (_viewSeen) _bannerPending = true;   // not for the view a run opens in; shown once play resumes
            _viewSeen = true;
        }

        // Names the view just switched to and its own controls, for a few seconds (full HUD style only).
        private void ShowViewBanner(CameraView view)
        {
            if (!HudViewLayout.StyleShows(GameSettings.Current.Hud, HudTier.Secondary)) return;
            string name, keys;
            switch (view)
            {
                case CameraView.ThirdPerson:
                    name = "Over the shoulder";
                    keys = Key("RMB") + "  Aim     " + Key(GameInput.KeyName(GameAction.SwapShoulder)) + "  Swap shoulder";
                    break;
                case CameraView.FirstPerson:
                    name = "First person";
                    keys = Key("RMB") + "  Aim     " + Key(GameInput.KeyName(GameAction.HoldBreath)) + "  Hold breath";
                    break;
                case CameraView.Bodycam:
                    name = "Bodycam";
                    keys = Key("RMB") + "  Aim     " + Key(GameInput.KeyName(GameAction.HoldBreath)) + "  Hold breath     No crosshair";
                    break;
                default:
                    name = "Isometric";
                    keys = Key("Wheel") + "  Zoom     " + Key(GameInput.KeyName(GameAction.RotateView)) + "  Rotate     "
                        + Key(GameInput.KeyName(GameAction.AimLine)) + "  Aim line";
                    break;
            }
            _viewBanner.text = "<size=20><b><color=#F2B544>" + HudUI.Spaced(name) + "</color></b></size>\n" + keys + "     " + Key("V") + "  Next view";
            _viewBannerTimer = 3.2f;
        }

        private static string Key(string k) => "<color=#F2EED8><b>" + k + "</b></color>";

        private void UpdateViewBanner(float unscaledDt, bool blocked)
        {
            if (_bannerPending && !blocked)
            {
                _bannerPending = false;
                ShowViewBanner(_layout.View);
            }
            if (_viewBannerTimer > 0f && !blocked) _viewBannerTimer -= unscaledDt;
            bool on = _viewBannerTimer > 0f && !blocked
                && HudViewLayout.StyleShows(GameSettings.Current.Hud, HudTier.Secondary);
            if (_viewBanner.enabled != on) _viewBanner.enabled = on;
            if (on) _viewBanner.color = HudUI.WithAlpha(HudReadouts.Dim, Mathf.Clamp01(_viewBannerTimer / 0.6f) * Mathf.Clamp01((3.2f - _viewBannerTimer) / 0.2f));
        }

        #region Per-frame
        private void UpdatePrompts(bool blocked)
        {
            PlayerInteractor interactor = _ctx.Interactor;
            bool showPrompt = !blocked && interactor != null;
            if (_promptText.enabled != showPrompt) _promptText.enabled = showPrompt;
            if (showPrompt) _promptText.text = interactor.PromptText;

            // The build, farming and water prompts share one line — whichever is active shows.
            PlayerFarming farming = _ctx.Farming;
            PlayerBuilder builder = _ctx.Builder;
            PlayerHydration hydration = _ctx.Hydration;
            string prompt = null;
            bool busy = (farming != null && farming.IsBusy) || (builder != null && builder.IsBuilding);
            if (!busy)
            {
                if (farming != null && !string.IsNullOrEmpty(farming.Prompt)) prompt = farming.Prompt;
                else if (builder != null && !string.IsNullOrEmpty(builder.Prompt)) prompt = builder.Prompt;
                else if (hydration != null && !string.IsNullOrEmpty(hydration.Prompt)) prompt = hydration.Prompt;
            }
            bool showBuild = !blocked && prompt != null;
            if (_buildPromptText.enabled != showBuild) _buildPromptText.enabled = showBuild;
            if (showBuild) _buildPromptText.text = prompt;
        }

        private void UpdateProgressBars()
        {
            // One bar for rummaging through a container or working at a fixture (washing up, a shower…).
            PlayerInteractor interactor = _ctx.Interactor;
            bool searching = interactor != null && interactor.IsSearching;
            bool job = !searching && interactor != null && interactor.IsWorking;
            HudUI.Show(_searchRow, searching || job);
            if (searching)
            {
                _searchFill.fillAmount = Mathf.Clamp01(interactor.SearchProgress);
                if (!ReferenceEquals(_shownSearchTarget, interactor.SearchTargetName))
                {
                    _shownSearchTarget = interactor.SearchTargetName;
                    _searchLabel.text = "Searching " + _shownSearchTarget + "\u2026";
                }
            }
            else if (job)
            {
                _searchFill.fillAmount = Mathf.Clamp01(interactor.JobProgress);
                if (!ReferenceEquals(_shownSearchTarget, interactor.JobLabel))
                {
                    _shownSearchTarget = interactor.JobLabel;
                    _searchLabel.text = _shownSearchTarget + "\u2026";
                }
            }

            // One shared bar for a build job or a farming job.
            PlayerFarming farming = _ctx.Farming;
            PlayerBuilder builder = _ctx.Builder;
            bool working = false;
            float progress = 0f;
            string label = null;
            if (farming != null && farming.IsBusy) { working = true; progress = farming.WorkProgress; label = farming.WorkLabel; }
            else if (builder != null && builder.IsBuilding) { working = true; progress = builder.BuildProgress; label = builder.BuildLabel; }
            HudUI.Show(_buildRow, working);
            if (working)
            {
                _buildFill.fillAmount = Mathf.Clamp01(progress);
                if (!ReferenceEquals(_shownBuildLabel, label))
                {
                    _shownBuildLabel = label;
                    _buildLabel.text = label + "\u2026";
                }
            }
        }

        private void UpdateNotices(float dt)
        {
            if (_levelUpTimer > 0f)
            {
                _levelUpTimer -= dt;
                _levelUpText.color = HudUI.WithAlpha(_levelUpText.color, _levelUpTimer > 0.6f ? 1f : Mathf.Clamp01(_levelUpTimer / 0.6f));
                if (!_levelUpText.enabled) _levelUpText.enabled = true;
            }
            else if (_levelUpText.enabled) _levelUpText.enabled = false;

            WaterMains water = _waterMains;
            if (water != null)
            {
                bool on = water.IsOn;
                if (_waterWasOn && !on) _waterNoticeTimer = 5f;   // the mains just ran dry — announce it
                _waterWasOn = on;
            }
            if (_waterNoticeTimer > 0f)
            {
                _waterNoticeTimer -= dt;
                _waterNoticeText.color = HudUI.WithAlpha(_waterNoticeText.color, _waterNoticeTimer > 1f ? 1f : Mathf.Clamp01(_waterNoticeTimer));
                if (!_waterNoticeText.enabled) _waterNoticeText.enabled = true;
            }
            else if (_waterNoticeText.enabled) _waterNoticeText.enabled = false;

            PowerGrid power = _powerGrid;
            if (power != null)
            {
                bool on = power.IsOn;
                // The grid just failed — announce it (not a cut a continued run was already living with).
                if (_powerSeen && _powerWasOn && !on) _powerNoticeTimer = 5f;
                _powerWasOn = on;
                _powerSeen = true;
            }
            if (_powerNoticeTimer > 0f)
            {
                _powerNoticeTimer -= dt;
                _powerNoticeText.color = HudUI.WithAlpha(_powerNoticeText.color, _powerNoticeTimer > 1f ? 1f : Mathf.Clamp01(_powerNoticeTimer));
                if (!_powerNoticeText.enabled) _powerNoticeText.enabled = true;
            }
            else if (_powerNoticeText.enabled) _powerNoticeText.enabled = false;
        }

        private void UpdateSleep(bool blocked)
        {
            PlayerSleep sleep = _ctx.Sleep;
            bool asleep = sleep != null && sleep.IsSleeping;
            bool showPrompt = sleep != null && !blocked && !asleep && !string.IsNullOrEmpty(sleep.Prompt);
            if (_sleepPromptText.enabled != showPrompt) _sleepPromptText.enabled = showPrompt;
            if (showPrompt) _sleepPromptText.text = sleep.Prompt;

            HudUI.Show(_sleepOverlay, asleep);
            if (!asleep) { _sleepMinute = -1; return; }
            // The clock races while asleep, so refresh the readout at most ten times a second.
            _sleepTextTimer -= Time.unscaledDeltaTime;
            TimeOfDay tod = TimeOfDay.Instance;
            int minute = tod != null ? tod.Day * 1440 + Mathf.FloorToInt(tod.Hour * 60f) : 0;
            if (minute == _sleepMinute || (_sleepMinute >= 0 && _sleepTextTimer > 0f)) return;
            _sleepMinute = minute;
            _sleepTextTimer = 0.1f;
            _sleepText.text = "Sleeping\u2026\n<size=26>" + (tod != null ? "Day " + tod.Day + "   " + tod.ClockText : "") + "</size>";
        }

        private void UpdateVignette()
        {
            if (_damageFlash > 0f)
                _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, Time.unscaledDeltaTime / 0.6f);
            float hn = _ctx.Stats != null ? _ctx.Stats.HealthNormalized : 1f;
            float lowHp = hn < 0.35f
                ? Mathf.InverseLerp(0.35f, 0.08f, hn) * (0.34f + 0.16f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)))
                : 0f;
            float a = Mathf.Max(lowHp, _damageFlash * 0.55f);
            bool on = a > 0.004f;
            if (_damageVignette.enabled != on) _damageVignette.enabled = on;
            if (on) _damageVignette.color = new Color(0.82f, 0.06f, 0.06f, a);
        }
        #endregion

        #region Events
        private void OnSkillLevelUp(PlayerSkills.Skill skill, int level)
        {
            if (_levelUpText == null) return;
            _levelUpText.text = "LEVEL UP\n<size=26>" + PlayerSkills.NameOf(skill) + "  Lv " + level + "</size>";
            _levelUpTimer = 3f;
        }

        private void OnPlayerDamaged()
        {
            _damageFlash = 1f;
            _feedback?.OnDamaged();
        }

        private void OnNotice(string message) => ShowToast(message);

        /// <summary>Shows a short fading message above the prompts (the inventory's and the gear's notices
        /// arrive here; other systems can route theirs through it too).</summary>
        public void ShowToast(string message)
        {
            if (_toasts != null) _toasts.Show(message);
        }

        private void ShowGameOver()
        {
            // A raid ends on the extraction summary instead; only wave survival uses this death screen.
            if (GameSession.IsExtraction) return;
            _gameOverPanel.SetActive(true);
            GameManager gm = GameManager.Instance;
            _gameOverText.text = "YOU DIED\n" +
                "<size=20>" + (gm != null ? gm.CauseOfDeath : "") + "</size>\n\n" +
                "Survived " + (gm != null ? FormatTime(gm.TimeSurvived) : "0:00") +
                "    Wave " + (gm != null ? gm.Wave : 0) +
                "    Kills " + (gm != null ? gm.ZombieKills : 0) + "\n\n" +
                "<size=22>Press  R  to try again</size>";
        }

        private static string FormatTime(float t) => HudUI.Clock(Mathf.FloorToInt(t));
        #endregion

        #region UI construction
        private void Build()
        {
            var canvasGo = new GameObject("HUD Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform root = canvas.GetComponent<RectTransform>();
            _ctx.Root = root;

            // Full-screen damage vignette (behind the HUD) — flashes red on hits, glows near death.
            RectTransform vign = HudUI.Stretch(root, "DamageVignette");
            _damageVignette = vign.gameObject.AddComponent<Image>();
            _damageVignette.sprite = VignetteSprite();
            _damageVignette.raycastTarget = false;
            _damageVignette.color = new Color(0.82f, 0.06f, 0.06f, 0f);
            _damageVignette.enabled = false;

            // World-anchored labels sit under every panel.
            RectTransform worldLayer = HudUI.Stretch(root, "WorldLabels");

            // The bodycam's display goes under the rest of the HUD (its scanlines cover the whole screen).
            _bodycamGroup = _layout.Add(root, "BodycamHud", new Vector2(0.5f, 0.5f), HudTier.Essential);
            _bodycam = new HudBodycamView(_ctx, _bodycamGroup.Rect);

            _handsGroup = _layout.Add(root, "Hands", new Vector2(0f, 1f), HudTier.Essential);
            _hands = new HudHands(_ctx, _handsGroup.Rect);
            _watchGroup = _layout.Add(root, "Watch", new Vector2(1f, 1f), HudTier.Secondary);
            _watch = new HudWatchRow(_ctx, _watchGroup.Rect);
            _moodleGroup = _layout.Add(root, "Moodles", new Vector2(1f, 1f), HudTier.Essential);
            _moodles = new HudMoodles(_ctx, _moodleGroup.Rect);
            _staminaGroup = _layout.Add(root, "Stamina", Vector2.zero, HudTier.Essential);
            _stamina = new HudStamina(_ctx, _staminaGroup.Rect);
            _shoulderGroup = _layout.Add(root, "ShoulderHud", new Vector2(0.5f, 0f), HudTier.Essential);
            _shoulder = new HudShoulderView(_ctx, _shoulderGroup.Rect);
            _visorGroup = _layout.Add(root, "FirstPersonHud", new Vector2(0.5f, 0f), HudTier.Essential);
            _visor = new HudFirstPersonView(_ctx, _visorGroup.Rect);
            _raid = new HudRaidOverlay(_ctx, root, worldLayer);
            _feedbackGroup = _layout.Add(root, "CombatFeedback", new Vector2(0.5f, 0.5f), HudTier.Essential);
            _feedback = new HudCombatFeedback(_ctx, _feedbackGroup.Rect);

            BuildPrompts(root);
            BuildProgressBar(root, "SearchRow", 132f, new Color(0.95f, 0.9f, 0.7f), new Color(0.95f, 0.85f, 0.35f), out _searchRow, out _searchLabel, out _searchFill);
            BuildProgressBar(root, "BuildRow", 176f, new Color(0.72f, 0.9f, 0.55f), new Color(0.55f, 0.85f, 0.4f), out _buildRow, out _buildLabel, out _buildFill);
            _toasts = new HudToasts(root);
            _hintsGroup = _layout.Add(root, "Hints", new Vector2(0.5f, 0f), HudTier.Secondary);
            _hints = new HudHints(_hintsGroup.Rect);
            _iconsGroup = _layout.Add(root, "StatusIcons", new Vector2(1f, 0f), HudTier.Essential);
            _boxGroup = _layout.Add(root, "StatusBox", new Vector2(1f, 0f), HudTier.Secondary);
            _status = new HudStatusRow(_ctx, _iconsGroup.Rect, _boxGroup.Rect);

            // Skill level-up toast (upper-centre, fades after a few seconds).
            _levelUpText = HudUI.Label(root, "LevelUp", 34, TextAnchor.MiddleCenter, new Color(0.5f, 0.95f, 0.55f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(700f, 80f));
            _levelUpText.enabled = false;

            // Water shut-off notice (centre, fades once the mains run dry).
            _waterNoticeText = HudUI.Label(root, "WaterNotice", 30, TextAnchor.MiddleCenter, new Color(0.55f, 0.8f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 60f));
            _waterNoticeText.text = "THE WATER HAS BEEN SHUT OFF";
            _waterNoticeText.enabled = false;

            // Power cut notice (just under the water notice, so both fit if they come together).
            _powerNoticeText = HudUI.Label(root, "PowerNotice", 30, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.45f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 105f), new Vector2(900f, 60f));
            _powerNoticeText.text = "THE POWER HAS GONE OUT";
            _powerNoticeText.enabled = false;

            // The view banner (upper-centre, above the level-up toast): names a newly chosen view and its controls.
            _viewBanner = HudUI.Label(root, "ViewBanner", 15, TextAnchor.MiddleCenter, HudReadouts.Dim,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1100f, 60f));
            _viewBanner.enabled = false;

            // The focus label and a fixture's action menu; captions for the radio and TV.
            _interaction = new HudInteraction(_ctx, root, worldLayer);
            _subtitles = new HudSubtitles(root);

            BuildSleepOverlay(root);
            BuildGameOver(root);
            ConfigureViews();
            _layout.ViewApplied += OnViewApplied;
        }

        // Which clusters each camera view shows, and where (offsets in reference pixels; scales about each cluster's corner).
        private void ConfigureViews()
        {
            const CameraView iso = CameraView.Isometric, shoulder = CameraView.ThirdPerson, eyes = CameraView.FirstPerson, body = CameraView.Bodycam;

            // Isometric: the tactical HUD — hand slots, the watch, moodles down the right, movement and stamina, the
            // status icons and the mode box. Nothing points at the screen centre: there's no crosshair from above.
            _handsGroup.In(iso);
            _watchGroup.In(iso);
            _moodleGroup.In(iso);
            _staminaGroup.In(iso);
            _iconsGroup.In(iso);
            _boxGroup.In(iso);
            _hintsGroup.In(iso);

            // Over the shoulder: the action HUD — vitals and the weapon card in the bottom corners, moodles down the right,
            // a smaller watch, the mode box lifted over the weapon card, hit markers and damage arcs.
            _shoulderGroup.In(shoulder);
            _watchGroup.In(shoulder, Vector2.zero, 0.9f);
            _moodleGroup.In(shoulder);
            _boxGroup.In(shoulder, new Vector2(0f, 122f));
            _feedbackGroup.In(shoulder);
            _hintsGroup.In(shoulder);

            // First person: the immersive HUD — hairline vitals and a compact magazine that fade back while all is well,
            // the moodles as a row of small tiles, a small watch.
            _visorGroup.In(eyes);
            _watchGroup.In(eyes, Vector2.zero, 0.8f);
            _moodleGroup.In(eyes);
            _boxGroup.In(eyes, new Vector2(0f, 96f), 0.9f);
            _feedbackGroup.In(eyes);
            _hintsGroup.In(eyes);

            // Bodycam: the camcorder's on-screen display stands in for the HUD; damage arcs but no hit markers
            // (no crosshair through a bodycam).
            _bodycamGroup.In(body);
            _boxGroup.In(body, new Vector2(0f, 108f), 0.9f);
            _feedbackGroup.In(body);
            _hintsGroup.In(body);
        }

        private void BuildPrompts(RectTransform root)
        {
            // Interaction prompt (bottom-centre, above the key hints).
            _promptText = HudUI.Label(root, "Prompt", 22, TextAnchor.LowerCenter, new Color(0.95f, 0.9f, 0.55f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(900f, 30f));
            _promptText.text = "";

            // Sleep prompt (just above the interaction prompt).
            _sleepPromptText = HudUI.Label(root, "SleepPrompt", 20, TextAnchor.LowerCenter, new Color(0.7f, 0.85f, 1f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 28f));
            _sleepPromptText.enabled = false;

            // Build / farm / water prompt, one line above the sleep prompt (the build palette makes it wide).
            _buildPromptText = HudUI.Label(root, "BuildPrompt", 19, TextAnchor.LowerCenter, new Color(0.72f, 0.9f, 0.55f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 148f), new Vector2(1300f, 28f));
            _buildPromptText.enabled = false;
        }

        private static void BuildProgressBar(RectTransform root, string name, float y, Color labelColor, Color fillColor, out GameObject row, out Text label, out Image fill)
        {
            RectTransform rt = HudUI.Rect(root, name, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, y), new Vector2(360f, 34f));
            label = HudUI.Label(rt, "Label", 16, TextAnchor.LowerCenter, labelColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, 2f), new Vector2(360f, 20f));
            fill = HudUI.Bar(rt, "Bar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(320f, 8f),
                new Color(0f, 0f, 0f, 0.6f), fillColor, out RectTransform track);
            UITheme.AddShadow(track.GetComponent<Image>());
            row = rt.gameObject;
            row.SetActive(false);
        }

        private void BuildSleepOverlay(RectTransform root)
        {
            RectTransform rt = HudUI.Stretch(root, "SleepOverlay");
            _sleepOverlay = rt.gameObject;
            var bg = _sleepOverlay.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
            var outline = _sleepOverlay.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.08f, 0.12f, 0.8f);
            outline.effectDistance = new Vector2(3f, 3f);
            _sleepText = HudUI.Label(rt, "SleepText", 46, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 0.95f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 220f));
            _sleepText.text = "Sleeping\u2026";
            _sleepOverlay.SetActive(false);
        }

        private void BuildGameOver(RectTransform root)
        {
            RectTransform rt = HudUI.Stretch(root, "GameOver");
            _gameOverPanel = rt.gameObject;
            var bg = _gameOverPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0f, 0f, 0.72f);
            _gameOverText = HudUI.Label(rt, "GameOverText", 56, TextAnchor.MiddleCenter, new Color(0.85f, 0.1f, 0.1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 400f));

            // Add subtle cracked‑glass overlay: four thin white rectangles with low opacity and slight rotation.
            const float crackLength = 450f;
            // Crack 1
            var crack1 = HudUI.Rect(_gameOverPanel.transform as RectTransform, "Crack1", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -30f), new Vector2(crackLength, 3f));
            var img1 = crack1.gameObject.AddComponent<Image>();
            img1.color = new Color(1f, 1f, 1f, 0.35f);
            crack1.localEulerAngles = new Vector3(0f, 0f, -12f);
            // Crack 2
            var crack2 = HudUI.Rect(_gameOverPanel.transform as RectTransform, "Crack2", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(80f, -60f), new Vector2(crackLength, 3f));
            var img2 = crack2.gameObject.AddComponent<Image>();
            img2.color = new Color(1f, 1f, 1f, 0.35f);
            crack2.localEulerAngles = new Vector3(0f, 0f, 8f);
            // Crack 3
            var crack3 = HudUI.Rect(_gameOverPanel.transform as RectTransform, "Crack3", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-50f, 120f), new Vector2(crackLength, 3f));
            var img3 = crack3.gameObject.AddComponent<Image>();
            img3.color = new Color(1f, 1f, 1f, 0.35f);
            crack3.localEulerAngles = new Vector3(0f, 0f, -5f);
            // Crack 4
            var crack4 = HudUI.Rect(_gameOverPanel.transform as RectTransform, "Crack4", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, 80f), new Vector2(crackLength, 3f));
            var img4 = crack4.gameObject.AddComponent<Image>();
            img4.color = new Color(1f, 1f, 1f, 0.35f);
            crack4.localEulerAngles = new Vector3(0f, 0f, 15f);
            _gameOverPanel.SetActive(false);
        }

        private static Sprite _vignetteSprite;
        private static Sprite VignetteSprite()
        {
            if (_vignetteSprite != null) return _vignetteSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Vector2 c = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxD = c.magnitude;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / maxD;    // 0 centre .. 1 corner
                    float a = Mathf.Pow(Mathf.InverseLerp(0.35f, 1f, d), 1.6f); // clear centre, ramps in at the edges
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _vignetteSprite;
        }
        #endregion
    }
}
