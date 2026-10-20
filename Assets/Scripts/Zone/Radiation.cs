using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// Radiation as a dose the body keeps (0..1; 1 is lethal) rather than a bar that refills: the Zone's background
    /// out in the open, hot spots, artifacts on the belt or loose in the pack, the emission. The Geiger clicks with
    /// the intake. Past a third the survivor retches; past half it eats health; a full dose kills. Anti-rad flushes
    /// it, iodine shields for a while, vodka helps a little and makes you sloppy. The dose leaves the raid with the
    /// survivor and fades over days in the hub (the medic can clear it).
    /// </summary>
    public sealed class PlayerRadiation : MonoBehaviour, IStatModSource
    {
        /// <summary>Converts the per-second radiation figures of zones and stat mods into dose per second.</summary>
        public const float Scale = 0.05f;

        /// <summary>Singleton instance of the player radiation component.</summary>
    public static PlayerRadiation Local { get; private set; }

        /// <summary>Current radiation dose (0..1) the survivor carries.</summary>
    public float Dose { get; private set; }
        /// <summary>Current radiation intake rate per second.</summary>
    public float Intake { get; private set; }
        /// <summary>Whether the survivor is currently protected by iodine (reduces intake).</summary>
public bool Shielded => Time.time < _iodineUntil;

        private PlayerStats _stats;
        private PlayerLimbs _limbs;
        private float _iodineUntil, _drunk, _clickAt, _retchAt;

        private void Awake()
        {
            Local = this;
            _stats = GetComponent<PlayerStats>();
            _limbs = GetComponent<PlayerLimbs>();
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c != null && c.Health != null && !GameSession.IsBorrowedBody) Dose = Mathf.Clamp01(c.Health.Radiation);
            PlayerLimbs.RadiationReceived += OnRadiationHit;
            GameEvents.RaidEnded += OnRaidEnded;
        }

        private void OnDestroy()
        {
            if (Local == this) Local = null;
            PlayerLimbs.RadiationReceived -= OnRadiationHit;
            GameEvents.RaidEnded -= OnRaidEnded;
        }

        private void OnRadiationHit(PlayerLimbs limbs, float hp)
        {
            if (limbs != _limbs) return;
            Add(hp / 400f);
        }

        /// <summary>Adds dose directly (an emission, a burst), shielded by iodine and suit resistance.</summary>
        /// <summary>Adds radiation dose (emission or burst), applying iodine and suit resistance.</summary>
public void Add(float dose)
        {
            if (dose <= 0f || _stats == null || _stats.IsDead) return;
            dose *= 1f - Mathf.Clamp01(PlayerModifiers.Current.Resist.Radiation);
            if (Shielded) dose *= 0.4f;
            Dose = Mathf.Clamp01(Dose + dose);
            ZoneExposure.Add(dose * 0.6f);
        }

        private void OnRaidEnded(RaidOutcome outcome, string exit) => SaveToProfile();

        /// <summary>Removes dose (the medic's flush).</summary>
        /// <summary>Removes a specified amount of radiation dose (medic's flush).</summary>
public void Purge(float amount)
        {
            Dose = Mathf.Max(0f, Dose - amount);
            SaveToProfile();
        }

        /// <summary>Writes the current dose into the active character profile for persistence.</summary>
public void SaveToProfile()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || GameSession.IsBorrowedBody) return;
            if (c.Health == null) c.Health = new HealthCarry();
            c.Health.Radiation = Dose;
        }

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;
            StatMods mods = PlayerModifiers.Current;
            Vector3 p = transform.position;

            float rate = 0f;
            if (!HubRules.SafeZone)
            {
                ZoneDef zone = GameSession.IsExtraction ? ZoneCatalog.Current : null;
                if (zone != null && zone.Radiation > 0f)
                {
                    bool indoors = Shelter.Count > 0 && Shelter.IsSheltered(p + Vector3.up);
                    rate += zone.Radiation * Scale * (indoors ? 0.35f : 1f);
                }
                rate += RadiationSpot.IntakeAt(p);
                rate += Mathf.Max(0f, mods.RadiationPerSec) * Scale;
                rate *= 1f - Mathf.Clamp01(mods.Resist.Radiation);
                if (Shielded) rate *= 0.4f;
            }
            Intake = rate;
            float purge = 0.00004f + Mathf.Max(0f, mods.RadiationPurge) * Scale + (HubRules.SafeZone ? 0.0004f : 0f);
            Dose = Mathf.Clamp01(Dose + (rate - purge) * dt);
            if (rate > 0f) ZoneExposure.Add(rate * dt * 0.6f);
            if (_drunk > 0f) _drunk = Mathf.Max(0f, _drunk - dt / 150f);

            Geiger(dt);
            Sickness(dt);
        }

        private void Geiger(float dt)
        {
            if (Intake <= 0.00001f) return;
            float perSec = Mathf.Clamp(Intake * 6000f, 0.4f, 40f);
            if (Time.time < _clickAt) return;
            _clickAt = Time.time + Random.Range(0.3f, 1.7f) / perSec;
            Sfx.PlayUI(SfxIds.Geiger, Mathf.Clamp(0.15f + perSec * 0.01f, 0.15f, 0.5f), Random.Range(0.9f, 1.15f));
        }

        private void Sickness(float dt)
        {
            if (Dose > 0.3f && Time.time > _retchAt)
            {
                _retchAt = Time.time + Random.Range(60f, 130f) * Mathf.Lerp(1f, 0.4f, Mathf.InverseLerp(0.3f, 0.9f, Dose));
                if (_retchAt > 0f && Dose > 0.3f)
                {
                    _stats.Hunger = Mathf.Clamp01(_stats.Hunger + 0.08f);
                    _stats.Thirst = Mathf.Clamp01(_stats.Thirst + 0.06f);
                    Sfx.Play(SfxIds.Cough, transform.position + Vector3.up * 1.5f, 0.8f, 0.7f, 18f);
                    GameEvents.RaiseToast("Radiation sickness: you retch.");
                }
            }
            if (Dose > 0.5f) _stats.EnvironmentDrain((Dose - 0.5f) * 0.6f * dt, "died of radiation sickness");
            if (Dose >= 0.999f) _stats.Kill("died of radiation sickness");
        }

        /// <summary>Applies radiation‑related stat modifiers based on current dose and drunkenness.</summary>
public void ApplyMods(StatMods mods)
        {
            if (_drunk > 0f)
            {
                mods.AimSpread *= 1f + _drunk * 0.6f;
                mods.PanicRate *= 1f - _drunk * 0.4f;
                mods.ColdResist += _drunk * 0.15f;
            }
            if (Dose > 0.3f)
            {
                float k = Mathf.InverseLerp(0.3f, 1f, Dose);
                mods.StaminaRegen *= 1f - k * 0.4f;
                mods.HealRate *= 1f - k * 0.7f;
            }
        }

        #region Medicine
        /// <summary>Consumes an anti‑rad item, reducing dose and increasing sickness resistance.</summary>
public void TakeAntirad()
        {
            Dose = Mathf.Max(0f, Dose - 0.35f);
            if (_stats != null) _stats.Sickness = Mathf.Min(CloneZConfig.SicknessMax, _stats.Sickness + 22f);
            GameEvents.RaiseToast("The anti-rad burns going in. Your stomach turns.");
        }

        /// <summary>Consumes iodine, granting temporary intake reduction.</summary>
public void TakeIodine()
        {
            _iodineUntil = Time.time + 600f;
            GameEvents.RaiseToast("Iodine taken: radiation intake reduced for a while.");
        }

        /// <summary>Drinks vodka, slightly reducing dose and applying drunk effects.</summary>
public void DrinkVodka()
        {
            Dose = Mathf.Max(0f, Dose - 0.04f);
            _drunk = Mathf.Clamp01(_drunk + 0.5f);
            if (_stats != null)
            {
                _stats.Panic = Mathf.Max(0f, _stats.Panic - 15f);
                _stats.Drink(0.05f);
            }
            Sfx.PlayUI(SfxIds.Drink, 0.6f, 0.9f);
        }

        internal static void RegisterItems()
        {
            ItemActions.RegisterUse("antirad", (inv, s) => Medicate(inv, s, r => r.TakeAntirad(), SfxIds.Injector));
            ItemActions.RegisterUse("iodine", (inv, s) => Medicate(inv, s, r => r.TakeIodine(), SfxIds.Eat));
            ItemActions.RegisterUse("vodka", (inv, s) => Medicate(inv, s, r => r.DrinkVodka(), null));
        }

        private static bool Medicate(PlayerInventory inv, ItemStack s, System.Action<PlayerRadiation> act, string sfx)
        {
            var r = inv.GetComponent<PlayerRadiation>();
            if (r == null) return false;
            act(r);
            if (sfx != null) Sfx.PlayUI(sfx, 0.6f);
            inv.Inventory.Remove(s, 1);
            return true;
        }
        #endregion
    }

    /// <summary>A radiation hot spot: invisible but for dead ground and the Geiger's chatter.</summary>
    public sealed class RadiationSpot : MonoBehaviour
    {
        /// <summary>List of all active radiation spots.</summary>
public static readonly List<RadiationSpot> All = new List<RadiationSpot>();
        /// <summary>Radius of the radiation spot in metres.</summary>
public float Radius = 8f;
        /// <summary>Dose per second at the centre.</summary>
        /// <summary>Peak dose per second at the centre of the spot.</summary>
public float Peak = 0.002f;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Calculates total radiation intake at a world position from all spots.</summary>
public static float IntakeAt(Vector3 p)
        {
            float sum = 0f;
            for (int i = 0; i < All.Count; i++)
            {
                RadiationSpot s = All[i];
                Vector3 c = s.transform.position;
                float d = new Vector2(p.x - c.x, p.z - c.z).magnitude;
                if (d >= s.Radius) continue;
                float k = 1f - d / s.Radius;
                sum += s.Peak * k * k;
            }
            return sum;
        }

        /// <summary>Spawns a new radiation spot at a given position.</summary>
public static RadiationSpot Spawn(Transform parent, Vector3 at, float radius, float peak, System.Random rng)
        {
            var go = new GameObject("RadiationSpot");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            ZoneKit.NoBake(go);
            var s = go.AddComponent<RadiationSpot>();
            s.Radius = radius;
            s.Peak = peak;
            var m = new RaidMesh();
            m.SetFrame(at, 0f);
            m.FlatBlob(new Vector3(0f, 0.008f, 0f), radius * 0.45f, radius * 0.4f, ZoneKit.R(rng, 0f, 360f), 0.35f, rng,
                ZoneKit.M(new Color(0.24f, 0.23f, 0.17f)));
            ZoneKit.Emit(m, "DeadGround", go.transform, at, UnityEngine.Rendering.ShadowCastingMode.Off);
            return s;
        }
    }

    /// <summary>The dose readout beside the detector (hidden when clean; the diegetic HUD leaves it to the clicks).</summary>
    public sealed class HudRadiation : MonoBehaviour
    {
        private Text _text;
        private Image _fill;

        /// <summary>Factory method to create the HUD element showing radiation dose.</summary>
    public static HudRadiation Create()
        {
            RectTransform slot = HudLayer.Anchor("Radiation", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 360f), new Vector2(200f, 40f));
            var h = slot.gameObject.AddComponent<HudRadiation>();
            Image bg = UIKit.Panel(slot, "Bg", new Color(0.08f, 0.07f, 0.03f, 0.8f));
            UIKit.Stretch(bg.rectTransform);
            h._text = UIKit.Label(slot, "", 14, new Color(0.95f, 0.85f, 0.3f), TextAnchor.UpperLeft);
            UIKit.Place(h._text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -2f), new Vector2(190f, 20f));
            h._fill = UIKit.Bar(slot, new Color(0.95f, 0.75f, 0.2f), 0f, 6f);
            UIKit.Place((RectTransform)h._fill.transform.parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 6f), new Vector2(184f, 6f));
            return h;
        }

    private bool _prevShow;
    private string _prevDose = "";
    private string _prevTrend = "";
    private bool _prevShielded;

    private void LateUpdate()
    {
        PlayerRadiation r = PlayerRadiation.Local;
        bool show = r != null && (r.Dose > 0.01f || r.Intake > 0.00002f);
        if (show != _prevShow)
        {
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(show);
            _prevShow = show;
        }
        if (!show) return;
        string trend = r.Intake > 0.0002f ? "  \u25B2\u25B2" : r.Intake > 0.00002f ? "  \u25B2" : "";
        string doseStr = (r.Dose * 6f).ToString("0.00");
        bool shielded = r.Shielded;
        if (doseStr != _prevDose || trend != _prevTrend || shielded != _prevShielded)
        {
            _text.text = "\u2622 " + doseStr + " Sv" + trend + (shielded ? "  (iodine)" : "");
            UIKit.SetBar(_fill, r.Dose);
            _prevDose = doseStr;
            _prevTrend = trend;
            _prevShielded = shielded;
        }
    }
    }
}
