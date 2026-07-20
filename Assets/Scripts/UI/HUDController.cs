using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// A code-built heads-up display: vitals bars (health, endurance, hunger,
    /// thirst, infection), wave/kill/time readouts and a death screen. Everything
    /// is generated at runtime so no scene wiring is required.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private PlayerStats _stats;
        private Font _font;

        private Image _healthFill, _enduranceFill, _hungerFill, _thirstFill, _fatigueFill, _painFill, _panicFill, _tempFill, _sickFill, _infectionFill;
        private RectTransform _vitalsPanel;
        private Image _damageVignette;
        private float _damageFlash;
        private int _barLayoutSig = -1;
        private readonly System.Collections.Generic.List<VitalRow> _vitalRows = new System.Collections.Generic.List<VitalRow>();
        private readonly System.Collections.Generic.Dictionary<Image, Text> _barValue = new System.Collections.Generic.Dictionary<Image, Text>();
        private readonly System.Collections.Generic.Dictionary<Image, float> _barShown = new System.Collections.Generic.Dictionary<Image, float>();
        private const float BarLerpSpeed = 3f;
        private Text _waveText, _killsText, _timeText, _bittenText, _bleedText, _panicAlert, _thermalAlert, _sickAlert, _enduranceLabel;
        private Text _weightText, _promptText, _ammoText;
        private RectTransform _hintBar;
        private Text _clockText, _weatherText, _sleepPromptText, _sleepText;
        private Text _waterText, _waterNoticeText;
        private float _waterNoticeTimer;
        private bool _waterWasOn = true;
        private GameObject _sleepOverlay;
        private PlayerInventory _inventory;
        private PlayerInteractor _interactor;
        private PlayerCombat _combat;
        private PlayerSleep _sleep;
        private PlayerSkills _skills;
        private PlayerWounds _wounds;
        private PlayerPanic _panic;
        private PlayerFoodSickness _sickness;
        private GameObject _searchRow;
        private Image _searchFill;
        private Text _searchLabel;
        private PlayerBuilder _builder;
        private PlayerFarming _farming;
        private PlayerHydration _hydration;
        private Text _buildPromptText;
        private GameObject _buildRow;
        private Image _buildFill;
        private Text _buildLabel;
        private Text _levelUpText;
        private float _levelUpTimer;
        private GameObject _gameOverPanel;
        private Text _gameOverText;

        public void Bind(PlayerStats stats, PlayerInventory inventory = null, PlayerInteractor interactor = null, PlayerCombat combat = null, PlayerSleep sleep = null, PlayerSkills skills = null, PlayerBuilder builder = null, PlayerWounds wounds = null, PlayerPanic panic = null, PlayerFoodSickness sickness = null, PlayerFarming farming = null, PlayerHydration hydration = null)
        {
            _stats = stats;
            if (_stats != null) _stats.OnDamaged += OnPlayerDamaged;
            _inventory = inventory;
            _interactor = interactor;
            _combat = combat;
            _sleep = sleep;
            _skills = skills;
            _builder = builder;
            _farming = farming;
            _hydration = hydration;
            _wounds = wounds;
            _panic = panic;
            _sickness = sickness;
            if (_skills != null) _skills.OnLevelUp += OnSkillLevelUp;
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += ShowGameOver;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameOver -= ShowGameOver;
            if (_skills != null) _skills.OnLevelUp -= OnSkillLevelUp;
            if (_stats != null) _stats.OnDamaged -= OnPlayerDamaged;
        }

        private void Update()
        {
            bool blocked = UIState.GameplayBlocked;
            if (_hintBar != null) _hintBar.gameObject.SetActive(!blocked);

            if (_stats != null)
            {
                float hn = _stats.HealthNormalized;
                SetFill(_healthFill, hn);
                // Health flashes between deep and bright red once it turns critical.
                _healthFill.color = hn < 0.25f
                    ? Color.Lerp(new Color(0.45f, 0.06f, 0.06f), new Color(0.95f, 0.2f, 0.2f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f))
                    : new Color(0.85f, 0.15f, 0.15f);
                SetFill(_enduranceFill, _stats.Endurance);
                SetFill(_hungerFill, _stats.Hunger);
                SetFill(_thirstFill, _stats.Thirst);
                SetFill(_fatigueFill, _stats.Fatigue);
                SetFill(_painFill, _stats.PainNormalized);

                // Pain reads calm green until it starts to bite, then amber and red.
                _painFill.color = _stats.PainNormalized > 0.6f ? new Color(0.9f, 0.25f, 0.3f)
                    : _stats.PainNormalized > 0.3f ? new Color(0.92f, 0.6f, 0.3f)
                    : new Color(0.68f, 0.74f, 0.5f);

                // Fatigue creeps from calm blue to an alarming red as sleep is needed.
                _fatigueFill.color = _stats.Fatigue > CloneZConfig.FatigueExhaustedThreshold ? new Color(0.9f, 0.2f, 0.3f)
                    : _stats.Fatigue > 0.6f ? new Color(0.7f, 0.42f, 0.8f)
                    : new Color(0.45f, 0.55f, 0.85f);

                // Endurance bar turns amber/red as it drops (PZ warnings 0.5 / 0.25).
                _enduranceFill.color = _stats.Endurance < CloneZConfig.EnduranceDanger ? new Color(0.9f, 0.2f, 0.1f)
                    : _stats.Endurance < CloneZConfig.EnduranceWarning ? new Color(0.95f, 0.65f, 0.1f)
                    : new Color(0.95f, 0.85f, 0.2f);

                if (_stats.Infected)
                {
                    SetFill(_infectionFill, _stats.InfectionNormalized);
                    _bittenText.enabled = true;
                    _bittenText.text = "INFECTED — " + Mathf.RoundToInt(_stats.Infection) + "%";
                    _bittenText.color = Color.Lerp(new Color(0.6f, 0.9f, 0.4f), new Color(0.9f, 0.1f, 0.7f), _stats.InfectionNormalized);
                }
                else
                {
                    _bittenText.enabled = false;
                }
            }

            if (_bleedText != null)
            {
                bool bleeding = _wounds != null && _wounds.IsBleeding;
                _bleedText.enabled = bleeding;
                if (bleeding)
                {
                    // A pulsing alert — you're losing blood until you bandage up.
                    float a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
                    _bleedText.color = new Color(0.86f, 0.12f, 0.12f, a);
                }
            }

            if (_panicFill != null)
            {
                float pn = _stats != null ? _stats.PanicNormalized : 0f;
                SetFill(_panicFill, pn);
                int lvl = _panic != null ? _panic.Level
                    : (pn >= 0.8f ? 4 : pn >= 0.65f ? 3 : pn >= 0.3f ? 2 : pn >= 0.06f ? 1 : 0);
                _panicFill.color = lvl >= 3 ? new Color(0.9f, 0.25f, 0.35f)
                    : lvl >= 2 ? new Color(0.85f, 0.55f, 0.35f)
                    : new Color(0.55f, 0.5f, 0.8f);

                if (_panicAlert != null)
                {
                    bool alarm = lvl >= 3;
                    _panicAlert.enabled = alarm;
                    if (alarm)
                    {
                        // A pulsing alert while fear grips you — steady your nerves or make space.
                        float a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
                        _panicAlert.text = (_panic != null ? _panic.Descriptor.ToUpper() : "PANICKED") + "!";
                        _panicAlert.color = new Color(0.86f, 0.35f, 0.55f, a);
                    }
                }
            }

            if (_tempFill != null && _stats != null)
            {
                float t = _stats.Temperature;
                SetFill(_tempFill, t);
                // Blue when cold, green when comfortable, amber/red as it overheats.
                _tempFill.color = t < 0.30f ? new Color(0.35f, 0.6f, 0.95f)
                    : t < CloneZConfig.TempComfortLow ? new Color(0.5f, 0.75f, 0.95f)
                    : t > 0.80f ? new Color(0.95f, 0.35f, 0.25f)
                    : t > CloneZConfig.TempComfortHigh ? new Color(0.95f, 0.6f, 0.3f)
                    : new Color(0.55f, 0.8f, 0.6f);

                if (_thermalAlert != null)
                {
                    string msg = null;
                    Color col = default;
                    if (t < CloneZConfig.TempHypothermiaThreshold + 0.02f) { msg = "FREEZING"; col = new Color(0.5f, 0.75f, 1f); }
                    else if (t > CloneZConfig.TempHyperthermiaThreshold - 0.01f) { msg = "OVERHEATING"; col = new Color(1f, 0.5f, 0.35f); }
                    else if (_stats.Wetness > 0.75f) { msg = "SOAKED"; col = new Color(0.6f, 0.8f, 1f); }

                    _thermalAlert.enabled = msg != null;
                    if (msg != null)
                    {
                        // A gentle pulse while the elements have their grip on you.
                        col.a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.5f));
                        _thermalAlert.text = msg;
                        _thermalAlert.color = col;
                    }
                }
            }

            if (_sickFill != null && _stats != null)
            {
                float sn = _stats.SicknessNormalized;
                SetFill(_sickFill, sn);
                int lvl = _sickness != null ? _sickness.Level
                    : (_stats.Sickness >= CloneZConfig.SicknessLevel4 ? 4 : _stats.Sickness >= CloneZConfig.SicknessLevel3 ? 3
                        : _stats.Sickness >= CloneZConfig.SicknessLevel2 ? 2 : _stats.Sickness >= CloneZConfig.SicknessLevel1 ? 1 : 0);
                // Calm green while fine, sickly yellow-green to a bilious red as it climbs.
                _sickFill.color = lvl >= 3 ? new Color(0.55f, 0.75f, 0.2f)
                    : lvl >= 2 ? new Color(0.7f, 0.8f, 0.25f)
                    : new Color(0.55f, 0.7f, 0.55f);

                if (_sickAlert != null)
                {
                    bool alarm = lvl >= 3;
                    _sickAlert.enabled = alarm;
                    if (alarm)
                    {
                        float a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.5f));
                        _sickAlert.text = (_sickness != null ? _sickness.Descriptor.ToUpper() : "SICK") + "!";
                        _sickAlert.color = new Color(0.7f, 0.85f, 0.3f, a);
                    }
                }
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                _waveText.text = "WAVE " + gm.Wave;
                _killsText.text = "KILLS  " + gm.ZombieKills;
                _timeText.text = "TIME  " + FormatTime(gm.TimeSurvived);
            }

            if (TimeOfDay.Instance != null)
                _clockText.text = "DAY " + TimeOfDay.Instance.Day + "   " + TimeOfDay.Instance.ClockText;
            if (WeatherSystem.Instance != null)
                _weatherText.text = WeatherSystem.Instance.Label.ToUpper();

            if (_waterText != null && WaterMains.Instance != null)
            {
                bool on = WaterMains.Instance.IsOn;
                _waterText.text = "WATER  " + WaterMains.Instance.StatusText;
                _waterText.color = on ? new Color(0.55f, 0.8f, 0.95f) : new Color(0.92f, 0.42f, 0.35f);
                if (_waterWasOn && !on) _waterNoticeTimer = 5f;   // the mains just ran dry — announce it
                _waterWasOn = on;
            }

            if (_waterNoticeText != null)
            {
                if (_waterNoticeTimer > 0f)
                {
                    _waterNoticeTimer -= Time.deltaTime;
                    var c = _waterNoticeText.color;
                    c.a = _waterNoticeTimer > 1f ? 1f : Mathf.Clamp01(_waterNoticeTimer);
                    _waterNoticeText.color = c;
                    _waterNoticeText.enabled = true;
                }
                else _waterNoticeText.enabled = false;
            }

            if (_sleep != null)
            {
                _sleepPromptText.enabled = !blocked && !_sleep.IsSleeping && !string.IsNullOrEmpty(_sleep.Prompt);
                if (_sleepPromptText.enabled) _sleepPromptText.text = _sleep.Prompt;

                _sleepOverlay.SetActive(_sleep.IsSleeping);
                if (_sleep.IsSleeping)
                    _sleepText.text = "Sleeping\u2026\n<size=26>" +
                        (TimeOfDay.Instance != null ? "Day " + TimeOfDay.Instance.Day + "   " + TimeOfDay.Instance.ClockText : "") +
                        "</size>";
            }

            if (_inventory != null && _weightText != null)
            {
                float load = _inventory.Load;
                _weightText.text = "WEIGHT  " + _inventory.Weight.ToString("0.0") + " / " + _inventory.Capacity.ToString("0.0") + " kg";
                _weightText.color = load >= 1f ? new Color(0.95f, 0.3f, 0.2f)
                    : load >= CloneZConfig.EncumbranceStart ? new Color(0.95f, 0.7f, 0.2f)
                    : new Color(0.85f, 0.85f, 0.9f);
            }
            if (_interactor != null && _promptText != null)
            {
                _promptText.text = blocked ? "" : _interactor.PromptText;
                _promptText.enabled = !blocked;
            }

            if (_ammoText != null)
            {
                ItemDef gun = _combat != null ? _combat.EquippedFirearm : null;
                if (gun != null)
                {
                    _ammoText.enabled = true;
                    if (_combat.IsReloading)
                    {
                        _ammoText.text = gun.DisplayName.ToUpper() + "   RELOADING…";
                        _ammoText.color = new Color(0.95f, 0.7f, 0.2f);
                    }
                    else
                    {
                        int reserve = _inventory != null ? _inventory.AmmoReserve(gun.AmmoId) : 0;
                        _ammoText.text = gun.DisplayName.ToUpper() + "   " + _combat.LoadedAmmo + " / " + reserve;
                        _ammoText.color = _combat.LoadedAmmo > 0 ? new Color(0.9f, 0.88f, 0.7f) : new Color(0.95f, 0.35f, 0.25f);
                    }
                }
                else _ammoText.enabled = false;
            }

            if (_searchRow != null)
            {
                bool searching = _interactor != null && _interactor.IsSearching;
                _searchRow.SetActive(searching);
                if (searching)
                {
                    SetFill(_searchFill, _interactor.SearchProgress);
                    _searchLabel.text = "Searching " + _interactor.SearchTargetName + "…";
                }
            }

            if (_buildPromptText != null)
            {
                // The build and farming prompts share this line — whichever is active shows.
                string prompt = null;
                bool busy = (_farming != null && _farming.IsBusy) || (_builder != null && _builder.IsBuilding);
                if (!busy)
                {
                    if (_farming != null && !string.IsNullOrEmpty(_farming.Prompt)) prompt = _farming.Prompt;
                    else if (_builder != null && !string.IsNullOrEmpty(_builder.Prompt)) prompt = _builder.Prompt;
                    else if (_hydration != null && !string.IsNullOrEmpty(_hydration.Prompt)) prompt = _hydration.Prompt;
                }
                _buildPromptText.enabled = !blocked && prompt != null;
                if (prompt != null) _buildPromptText.text = prompt;
            }

            if (_buildRow != null)
            {
                // Same shared progress bar for a build job or a farming job.
                bool building = false; float progress = 0f; string label = "";
                if (_farming != null && _farming.IsBusy) { building = true; progress = _farming.WorkProgress; label = _farming.WorkLabel; }
                else if (_builder != null && _builder.IsBuilding) { building = true; progress = _builder.BuildProgress; label = _builder.BuildLabel; }

                _buildRow.SetActive(building);
                if (building)
                {
                    SetFill(_buildFill, progress);
                    _buildLabel.text = label + "\u2026";
                }
            }

            if (_levelUpText != null)
            {
                if (_levelUpTimer > 0f)
                {
                    _levelUpTimer -= Time.deltaTime;
                    var c = _levelUpText.color;
                    c.a = _levelUpTimer > 0.6f ? 1f : Mathf.Clamp01(_levelUpTimer / 0.6f);
                    _levelUpText.color = c;
                    _levelUpText.enabled = true;
                }
                else _levelUpText.enabled = false;
            }

            if (_damageVignette != null)
            {
                if (_damageFlash > 0f)
                    _damageFlash = Mathf.MoveTowards(_damageFlash, 0f, Time.unscaledDeltaTime / 0.6f);
                float hn = _stats != null ? _stats.HealthNormalized : 1f;
                float lowHp = hn < 0.35f
                    ? Mathf.InverseLerp(0.35f, 0.08f, hn) * (0.34f + 0.16f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)))
                    : 0f;
                float a = Mathf.Max(lowHp, _damageFlash * 0.55f);
                _damageVignette.enabled = a > 0.004f;
                if (_damageVignette.enabled)
                    _damageVignette.color = new Color(0.82f, 0.06f, 0.06f, a);
            }

            LayoutBars();
        }

        private void OnSkillLevelUp(PlayerSkills.Skill skill, int level)
        {
            if (_levelUpText == null) return;
            _levelUpText.text = "LEVEL UP\n<size=26>" + PlayerSkills.NameOf(skill) + "  Lv " + level + "</size>";
            _levelUpTimer = 3f;
        }

        private void OnPlayerDamaged() => _damageFlash = 1f;

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

        private static string FormatTime(float t)
        {
            int m = Mathf.FloorToInt(t / 60f);
            int s = Mathf.FloorToInt(t % 60f);
            return string.Format("{0:0}:{1:00}", m, s);
        }

        private void SetFill(Image fill, float amount)
        {
            amount = Mathf.Clamp01(amount);
            float shown = amount;
            // Vitals bars (registered in Bar) ease toward their target; progress
            // bars aren't registered and snap immediately.
            if (_barShown.TryGetValue(fill, out float cur))
            {
                cur = Mathf.MoveTowards(cur, amount, Time.unscaledDeltaTime * BarLerpSpeed);
                _barShown[fill] = cur;
                shown = cur;
            }
            Vector3 scale = fill.rectTransform.localScale;
            scale.x = shown;
            fill.rectTransform.localScale = scale;
            if (_barValue.TryGetValue(fill, out Text valueText) && valueText != null)
                valueText.text = Mathf.RoundToInt(amount * 100f) + "%";
        }

        private void ShowGameOver()
        {
            _gameOverPanel.SetActive(true);
            var gm = GameManager.Instance;
            _gameOverText.text = "YOU DIED\n" +
                "<size=20>" + (gm != null ? gm.CauseOfDeath : "") + "</size>\n\n" +
                "Survived " + (gm != null ? FormatTime(gm.TimeSurvived) : "0:00") +
                "    Wave " + (gm != null ? gm.Wave : 0) +
                "    Kills " + (gm != null ? gm.ZombieKills : 0) + "\n\n" +
                "<size=22>Press  R  to try again</size>";
        }

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

            // Full-screen damage vignette (behind the HUD) — flashes red on hits, glows near death.
            var vign = new GameObject("DamageVignette");
            var vignRt = vign.AddComponent<RectTransform>();
            vignRt.SetParent(root, false);
            vignRt.anchorMin = Vector2.zero; vignRt.anchorMax = Vector2.one;
            vignRt.offsetMin = Vector2.zero; vignRt.offsetMax = Vector2.zero;
            _damageVignette = vign.AddComponent<Image>();
            _damageVignette.sprite = VignetteSprite();
            _damageVignette.raycastTarget = false;
            _damageVignette.color = new Color(0.82f, 0.06f, 0.06f, 0f);
            _damageVignette.enabled = false;

            // Vitals panel backdrop (top-left) — height is trimmed to the visible rows in LayoutBars.
            _vitalsPanel = Panel(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(300f, 382f), new Color(0.05f, 0.06f, 0.08f, 0.55f));
            var vitalsAccent = new GameObject("VitalsAccent");
            var vitalsAccentRt = vitalsAccent.AddComponent<RectTransform>();
            vitalsAccentRt.SetParent(_vitalsPanel, false);
            vitalsAccentRt.anchorMin = new Vector2(0f, 1f); vitalsAccentRt.anchorMax = new Vector2(1f, 1f); vitalsAccentRt.pivot = new Vector2(0.5f, 1f);
            vitalsAccentRt.anchoredPosition = Vector2.zero;
            vitalsAccentRt.sizeDelta = new Vector2(0f, 3f);
            vitalsAccent.AddComponent<Image>().color = new Color(0.5f, 0.7f, 0.9f, 0.55f);

            _healthFill = Bar(root, "Health", -34f, "HEALTH", new Color(0.85f, 0.15f, 0.15f));
            _enduranceFill = Bar(root, "Endurance", -60f, "STAMINA", new Color(0.95f, 0.85f, 0.2f), out _enduranceLabel);
            _hungerFill = Bar(root, "Hunger", -86f, "HUNGER", new Color(0.9f, 0.55f, 0.15f));
            _thirstFill = Bar(root, "Thirst", -112f, "THIRST", new Color(0.25f, 0.6f, 0.95f));
            _fatigueFill = Bar(root, "Fatigue", -138f, "FATIGUE", new Color(0.45f, 0.55f, 0.85f));
            _painFill = Bar(root, "Pain", -164f, "PAIN", new Color(0.85f, 0.45f, 0.5f));
            _panicFill = Bar(root, "Panic", -190f, "PANIC", new Color(0.55f, 0.5f, 0.8f));
            _tempFill = Bar(root, "Temp", -216f, "TEMP", new Color(0.55f, 0.8f, 0.6f));
            _sickFill = Bar(root, "Sickness", -242f, "STOMACH", new Color(0.55f, 0.7f, 0.55f));
            _infectionFill = Bar(root, "Infection", -268f, "INFECT", new Color(0.6f, 0.9f, 0.4f));

            _weightText = Label(root, "Weight", new Vector2(0f, 1f), new Vector2(32f, -294f), new Vector2(260f, 20f), 15, TextAnchor.MiddleLeft, new Color(0.85f, 0.85f, 0.9f));
            _weightText.text = "WEIGHT  0.0 / 0.0 kg";

            _ammoText = Label(root, "Ammo", new Vector2(0f, 1f), new Vector2(32f, -316f), new Vector2(260f, 20f), 16, TextAnchor.MiddleLeft, new Color(0.9f, 0.88f, 0.7f));
            _ammoText.text = "";
            _ammoText.enabled = false;

            // Register the vitals rows so idle ones (pain/panic/stomach/infection) hide and reflow.
            RegisterVital(_healthFill, null);
            RegisterVital(_enduranceFill, null);
            RegisterVital(_hungerFill, null);
            RegisterVital(_thirstFill, null);
            RegisterVital(_fatigueFill, null);
            RegisterVital(_painFill, () => _stats != null && _stats.Pain > 0.5f);
            RegisterVital(_panicFill, () => _stats != null && _stats.Panic > 0.5f);
            RegisterVital(_tempFill, null);
            RegisterVital(_sickFill, () => _stats != null && _stats.Sickness > 0.5f);
            RegisterVital(_infectionFill, () => _stats != null && _stats.Infected);
            LayoutBars();

            // Top-right readouts.
            _waveText = Label(root, "Wave", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(260f, 34f), 28, TextAnchor.UpperRight, Color.white);
            _killsText = Label(root, "Kills", new Vector2(1f, 1f), new Vector2(-24f, -60f), new Vector2(260f, 28f), 22, TextAnchor.UpperRight, new Color(0.9f, 0.9f, 0.9f));
            _timeText = Label(root, "Time", new Vector2(1f, 1f), new Vector2(-24f, -88f), new Vector2(260f, 28f), 22, TextAnchor.UpperRight, new Color(0.9f, 0.9f, 0.9f));
            _clockText = Label(root, "Clock", new Vector2(1f, 1f), new Vector2(-24f, -118f), new Vector2(260f, 30f), 24, TextAnchor.UpperRight, new Color(0.96f, 0.92f, 0.7f));
            _clockText.text = "DAY 1   08:30";
            _weatherText = Label(root, "Weather", new Vector2(1f, 1f), new Vector2(-24f, -148f), new Vector2(260f, 26f), 20, TextAnchor.UpperRight, new Color(0.75f, 0.85f, 0.95f));
            _weatherText.text = "CLEAR";
            _waterText = Label(root, "Water", new Vector2(1f, 1f), new Vector2(-24f, -174f), new Vector2(260f, 24f), 18, TextAnchor.UpperRight, new Color(0.55f, 0.8f, 0.95f));
            _waterText.text = "WATER  ON";

            // Infection warning (center-top).
            _bittenText = Label(root, "Bitten", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 34f), 26, TextAnchor.UpperCenter, new Color(0.9f, 0.2f, 0.6f));
            _bittenText.enabled = false;

            // Bleeding warning (just below the infection line).
            _bleedText = Label(root, "Bleeding", new Vector2(0.5f, 1f), new Vector2(0f, -64f), new Vector2(600f, 30f), 22, TextAnchor.UpperCenter, new Color(0.86f, 0.12f, 0.12f));
            _bleedText.text = "BLEEDING  \u2014  bandage your wounds  (H)";
            _bleedText.enabled = false;

            // Panic alert (pulses under the bleed line once fear takes hold).
            _panicAlert = Label(root, "PanicAlert", new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(600f, 30f), 22, TextAnchor.UpperCenter, new Color(0.82f, 0.45f, 0.95f));
            _panicAlert.text = "PANICKED";
            _panicAlert.enabled = false;

            // Thermal alert (freezing / overheating / soaked — pulses below the panic line).
            _thermalAlert = Label(root, "ThermalAlert", new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(600f, 30f), 22, TextAnchor.UpperCenter, new Color(0.55f, 0.75f, 1f));
            _thermalAlert.text = "FREEZING";
            _thermalAlert.enabled = false;

            // Sickness alert (nausea — pulses below the thermal line once food poisoning takes hold).
            _sickAlert = Label(root, "SickAlert", new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(600f, 30f), 22, TextAnchor.UpperCenter, new Color(0.7f, 0.85f, 0.3f));
            _sickAlert.text = "NAUSEOUS";
            _sickAlert.enabled = false;

            // Interaction prompt (bottom-centre, above the hint).
            _promptText = Label(root, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(900f, 30f), 22, TextAnchor.LowerCenter, new Color(0.95f, 0.9f, 0.55f));
            _promptText.text = "";

            // Sleep prompt (just above the interaction prompt).
            _sleepPromptText = Label(root, "SleepPrompt", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(900f, 28f), 20, TextAnchor.LowerCenter, new Color(0.7f, 0.85f, 1f));
            _sleepPromptText.text = "";
            _sleepPromptText.enabled = false;

            // Build prompt (crafting barricades), one line above the sleep prompt.
            _buildPromptText = Label(root, "BuildPrompt", new Vector2(0.5f, 0f), new Vector2(0f, 148f), new Vector2(900f, 28f), 20, TextAnchor.LowerCenter, new Color(0.72f, 0.9f, 0.55f));
            _buildPromptText.text = "";
            _buildPromptText.enabled = false;

            // Controls hint (bottom) — key-cap chips, hidden while a modal panel is open.
            BuildControlHints(root);

            // Skill level-up toast (upper-centre, fades after a few seconds).
            _levelUpText = Label(root, "LevelUp", new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(700f, 80f), 34, TextAnchor.MiddleCenter, new Color(0.5f, 0.95f, 0.55f));
            _levelUpText.enabled = false;

            // Water shut-off notice (centre, fades once the mains run dry).
            _waterNoticeText = Label(root, "WaterNotice", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(900f, 60f), 30, TextAnchor.MiddleCenter, new Color(0.55f, 0.8f, 0.98f));
            _waterNoticeText.text = "THE WATER HAS BEEN SHUT OFF";
            _waterNoticeText.enabled = false;

            BuildSearchBar(root);
            BuildBuildBar(root);
            BuildSleepOverlay(root);
            BuildGameOver(root);
        }

        private void BuildSleepOverlay(RectTransform root)
        {
            _sleepOverlay = new GameObject("SleepOverlay");
            var rt = _sleepOverlay.AddComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var bg = _sleepOverlay.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);

            _sleepText = Label(rt, "SleepText", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 220f), 46, TextAnchor.MiddleCenter, new Color(0.8f, 0.85f, 0.95f));
            _sleepText.text = "Sleeping\u2026";
            _sleepOverlay.SetActive(false);
        }

        private void BuildSearchBar(RectTransform root)
        {
            _searchRow = new GameObject("SearchRow");
            var srRt = _searchRow.AddComponent<RectTransform>();
            srRt.SetParent(root, false);
            srRt.anchorMin = new Vector2(0.5f, 0f); srRt.anchorMax = new Vector2(0.5f, 0f); srRt.pivot = new Vector2(0.5f, 0f);
            srRt.anchoredPosition = new Vector2(0f, 132f);
            srRt.sizeDelta = new Vector2(360f, 34f);

            _searchLabel = Label(srRt, "SearchLabel", new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(360f, 20f), 16, TextAnchor.LowerCenter, new Color(0.95f, 0.9f, 0.7f));
            _searchLabel.text = "Searching…";

            var sbg = new GameObject("SearchBg");
            var sbgRt = sbg.AddComponent<RectTransform>();
            sbgRt.SetParent(srRt, false);
            sbgRt.anchorMin = new Vector2(0.5f, 0f); sbgRt.anchorMax = new Vector2(0.5f, 0f); sbgRt.pivot = new Vector2(0.5f, 0f);
            sbgRt.anchoredPosition = new Vector2(0f, 0f);
            sbgRt.sizeDelta = new Vector2(320f, 12f);
            sbg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var sfill = new GameObject("SearchFill");
            var sfillRt = sfill.AddComponent<RectTransform>();
            sfillRt.SetParent(sbgRt, false);
            sfillRt.anchorMin = new Vector2(0f, 0f); sfillRt.anchorMax = new Vector2(0f, 1f); sfillRt.pivot = new Vector2(0f, 0.5f);
            sfillRt.sizeDelta = new Vector2(316f, -4f);
            sfillRt.anchoredPosition = new Vector2(2f, 0f);
            _searchFill = sfill.AddComponent<Image>();
            _searchFill.color = new Color(0.95f, 0.85f, 0.35f);

            _searchRow.SetActive(false);
        }

        private void BuildBuildBar(RectTransform root)
        {
            _buildRow = new GameObject("BuildRow");
            var brRt = _buildRow.AddComponent<RectTransform>();
            brRt.SetParent(root, false);
            brRt.anchorMin = new Vector2(0.5f, 0f); brRt.anchorMax = new Vector2(0.5f, 0f); brRt.pivot = new Vector2(0.5f, 0f);
            brRt.anchoredPosition = new Vector2(0f, 176f);
            brRt.sizeDelta = new Vector2(360f, 34f);

            _buildLabel = Label(brRt, "BuildLabel", new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(360f, 20f), 16, TextAnchor.LowerCenter, new Color(0.72f, 0.9f, 0.55f));
            _buildLabel.text = "Building…";

            var bbg = new GameObject("BuildBg");
            var bbgRt = bbg.AddComponent<RectTransform>();
            bbgRt.SetParent(brRt, false);
            bbgRt.anchorMin = new Vector2(0.5f, 0f); bbgRt.anchorMax = new Vector2(0.5f, 0f); bbgRt.pivot = new Vector2(0.5f, 0f);
            bbgRt.anchoredPosition = new Vector2(0f, 0f);
            bbgRt.sizeDelta = new Vector2(320f, 12f);
            bbg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var bfill = new GameObject("BuildFill");
            var bfillRt = bfill.AddComponent<RectTransform>();
            bfillRt.SetParent(bbgRt, false);
            bfillRt.anchorMin = new Vector2(0f, 0f); bfillRt.anchorMax = new Vector2(0f, 1f); bfillRt.pivot = new Vector2(0f, 0.5f);
            bfillRt.sizeDelta = new Vector2(316f, -4f);
            bfillRt.anchoredPosition = new Vector2(2f, 0f);
            _buildFill = bfill.AddComponent<Image>();
            _buildFill.color = new Color(0.55f, 0.85f, 0.4f);

            _buildRow.SetActive(false);
        }

        private void BuildGameOver(RectTransform root)
        {
            _gameOverPanel = new GameObject("GameOver");
            var rt = _gameOverPanel.AddComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var bg = _gameOverPanel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0f, 0f, 0.72f);

            _gameOverText = Label(rt, "GameOverText", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 400f), 56, TextAnchor.MiddleCenter, new Color(0.85f, 0.1f, 0.1f));
            _gameOverText.supportRichText = true;
            _gameOverPanel.SetActive(false);
        }

        private Image Bar(RectTransform parent, string name, float y, string label, Color fillColor)
            => Bar(parent, name, y, label, fillColor, out _);

        private Image Bar(RectTransform parent, string name, float y, string label, Color fillColor, out Text labelText)
        {
            var row = new GameObject(name + "Row");
            var rowRt = row.AddComponent<RectTransform>();
            rowRt.SetParent(parent, false);
            rowRt.anchorMin = new Vector2(0f, 1f); rowRt.anchorMax = new Vector2(0f, 1f); rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(32f, y);
            rowRt.sizeDelta = new Vector2(280f, 18f);

            labelText = Label(rowRt, "Label", new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(80f, 18f), 14, TextAnchor.MiddleLeft, Color.white);
            labelText.rectTransform.pivot = new Vector2(0f, 0.5f);
            labelText.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            labelText.text = label;

            // Bar background
            var barBg = new GameObject("BarBg");
            var bgRt = barBg.AddComponent<RectTransform>();
            bgRt.SetParent(rowRt, false);
            bgRt.anchorMin = new Vector2(0f, 0.5f); bgRt.anchorMax = new Vector2(0f, 0.5f); bgRt.pivot = new Vector2(0f, 0.5f);
            bgRt.anchoredPosition = new Vector2(86f, 0f);
            bgRt.sizeDelta = new Vector2(180f, 16f);
            barBg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            // Fill (scaled from the left to represent 0..1).
            var fillGo = new GameObject("Fill");
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.SetParent(bgRt, false);
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(176f, -4f);
            fillRt.anchoredPosition = new Vector2(2f, 0f);
            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;

            // Numeric readout, right-aligned on top of the fill.
            var valueText = Label(bgRt, "Value", new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(60f, 16f), 12, TextAnchor.MiddleRight, new Color(1f, 1f, 1f, 0.85f));
            valueText.text = "";
            _barValue[fill] = valueText;
            _barShown[fill] = 0f;
            return fill;
        }

        private Text Label(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            UITheme.AddShadow(text);
            return text;
        }

        private void BuildControlHints(RectTransform root)
        {
            _hintBar = new GameObject("HintBar").AddComponent<RectTransform>();
            _hintBar.SetParent(root, false);
            _hintBar.anchorMin = new Vector2(0.5f, 0f); _hintBar.anchorMax = new Vector2(0.5f, 0f); _hintBar.pivot = new Vector2(0.5f, 0f);
            _hintBar.anchoredPosition = new Vector2(0f, 16f);
            _hintBar.sizeDelta = new Vector2(1900f, 26f);

            (string key, string action)[] hints =
            {
                ("WASD", "Move"), ("LMB", "Attack"), ("RMB", "Shove"), ("R", "Reload"),
                ("Shift", "Sprint"), ("Ctrl", "Sneak"), ("E", "Search"), ("I", "Inventory"),
                ("B", "Build"), ("F", "Farm"), ("G", "Water"), ("K", "Skills"),
                ("H", "Health"), ("T", "Sleep")
            };

            const float gap = 10f;
            var chips = new System.Collections.Generic.List<(RectTransform rt, float w)>(hints.Length);
            float totalWidth = 0f;
            foreach (var h in hints)
            {
                var rt = BuildKeyChip(_hintBar, h.key, h.action, out float w);
                chips.Add((rt, w));
                totalWidth += w + gap;
            }
            totalWidth -= gap;

            float cursor = -totalWidth * 0.5f;
            foreach (var (rt, w) in chips)
            {
                rt.anchoredPosition = new Vector2(cursor, 0f);
                cursor += w + gap;
            }
        }

        private RectTransform BuildKeyChip(RectTransform parent, string key, string action, out float width)
        {
            var chip = new GameObject("Chip").AddComponent<RectTransform>();
            chip.SetParent(parent, false);
            chip.anchorMin = new Vector2(0.5f, 0.5f); chip.anchorMax = new Vector2(0.5f, 0.5f); chip.pivot = new Vector2(0f, 0.5f);

            var cap = new GameObject("Cap").AddComponent<RectTransform>();
            cap.SetParent(chip, false);
            cap.anchorMin = new Vector2(0f, 0.5f); cap.anchorMax = new Vector2(0f, 0.5f); cap.pivot = new Vector2(0f, 0.5f);
            cap.anchoredPosition = Vector2.zero;
            var capImg = cap.gameObject.AddComponent<Image>();
            capImg.color = new Color(0.20f, 0.22f, 0.28f, 0.92f);
            capImg.raycastTarget = false;

            var keyText = Label(cap, "Key", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 20f), 13, TextAnchor.MiddleCenter, new Color(0.96f, 0.95f, 0.82f));
            keyText.text = key;
            keyText.raycastTarget = false;
            float keyW = Mathf.Ceil(keyText.preferredWidth) + 12f;
            cap.sizeDelta = new Vector2(keyW, 22f);
            keyText.rectTransform.sizeDelta = new Vector2(keyW, 20f);

            var actText = Label(chip, "Action", new Vector2(0f, 0.5f), new Vector2(keyW + 6f, 0f), new Vector2(60f, 20f), 14, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.72f));
            actText.text = action;
            actText.raycastTarget = false;
            float actW = Mathf.Ceil(actText.preferredWidth);
            actText.rectTransform.sizeDelta = new Vector2(actW, 20f);

            width = keyW + 6f + actW;
            return chip;
        }

        private sealed class VitalRow
        {
            public RectTransform Row;
            public System.Func<bool> Visible;
        }

        private void RegisterVital(Image fill, System.Func<bool> visible)
        {
            if (fill == null) return;
            var row = fill.transform.parent.parent as RectTransform;   // fill -> barBg -> row
            _vitalRows.Add(new VitalRow { Row = row, Visible = visible });
        }

        // Hides idle rows (pain/panic/stomach/infection at rest) and reflows the rest,
        // trimming the backdrop to fit. Only rebuilds when the visible set changes.
        private void LayoutBars()
        {
            int sig = 0, bit = 1;
            foreach (var v in _vitalRows) { if (v.Visible == null || v.Visible()) sig |= bit; bit <<= 1; }
            bool ammo = _ammoText != null && _ammoText.enabled;
            if (ammo) sig |= bit;
            if (sig == _barLayoutSig) return;
            _barLayoutSig = sig;

            const float top = -34f, step = 26f;
            float y = top;
            foreach (var v in _vitalRows)
            {
                if (v.Row == null) continue;
                bool vis = v.Visible == null || v.Visible();
                if (v.Row.gameObject.activeSelf != vis) v.Row.gameObject.SetActive(vis);
                if (vis) { v.Row.anchoredPosition = new Vector2(32f, y); y -= step; }
            }

            float weightY = y - 6f;
            if (_weightText != null) _weightText.rectTransform.anchoredPosition = new Vector2(32f, weightY);
            float lastY = weightY;
            if (_ammoText != null)
            {
                float ammoY = weightY - 22f;
                _ammoText.rectTransform.anchoredPosition = new Vector2(32f, ammoY);
                if (ammo) lastY = ammoY;
            }

            if (_vitalsPanel != null)
                _vitalsPanel.sizeDelta = new Vector2(300f, -6f - lastY);
        }

        private RectTransform Panel(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject("Panel");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return rt;
        }
        #endregion
    }
}
