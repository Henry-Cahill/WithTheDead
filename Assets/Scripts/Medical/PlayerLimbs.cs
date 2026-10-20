using UnityEngine;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>
    /// The survivor's seven hit zones (Tarkov): head, thorax, stomach, both arms, both legs, each with its own hit
    /// points. A head or thorax at zero is death; any other zone at zero is "blacked out" — useless until surgery —
    /// and further hits there spread across the body. Every hit is first tested against the armour covering that
    /// zone (class versus the round's penetration, durability wearing down) and against the Zone's damage
    /// resistances, then opens the right wound: gunshots, heavy bleeds, fragments, burns, fractures. The overall
    /// <see cref="PlayerStats.Health"/> pool falls with the hit points lost, so every other system (bleeding,
    /// starvation, infection) keeps working on top.
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerLimbs : MonoBehaviour, IStatModSource
    {
        public static readonly float[] BaseMax = { 35f, 85f, 70f, 60f, 60f, 65f, 65f };
        public const float TotalMaxHp = 440f;
        private const float HealPerSec = 0.08f;
        private const float SpreadFactor = 0.7f;

        private readonly float[] _hp = new float[BodyZones.Count];
        private readonly float[] _max = new float[BodyZones.Count];
        private readonly bool[] _fractured = new bool[BodyZones.Count];

        private PlayerStats _stats;
        private PlayerWounds _wounds;
        private PlayerClothing _clothing;
        private PlayerBlood _blood;
        private PlayerSleep _sleep;
        private bool _loaded;

        /// <summary>Raised on every hit that lands (zone, hit points lost) — the HUD flashes, the camera shakes.</summary>
        public event System.Action<BodyZone, float> Hit;
                /// <summary>Raised when any limb data changes (e.g., health, fracture) and UI should update.</summary>
        public event System.Action Changed;

        private static readonly ClothingSlot[] s_ArmorSlots = { ClothingSlot.Armor, ClothingSlot.Head, ClothingSlot.Torso };

                /// <summary>Gets the current hit points of the specified limb.</summary>
        public float Hp(BodyZone z) => _hp[(int)z];
                /// <summary>Gets the maximum hit points for the specified limb.</summary>
        public float Max(BodyZone z) => _max[(int)z];
                /// <summary>Normalized hit points (0‑1) of a limb, or 0 when the limb has no max.</summary>
        public float Hp01(BodyZone z) => _max[(int)z] > 0f ? Mathf.Clamp01(_hp[(int)z] / _max[(int)z]) : 0f;
                /// <summary>Whether the specified limb is completely disabled (hit points ≤ 0).</summary>
        public bool IsBlackedOut(BodyZone z) => _hp[(int)z] <= 0f;
                /// <summary>Whether the specified limb is fractured.</summary>
        public bool IsFractured(BodyZone z) => _fractured[(int)z];

                /// <summary>Total hit points across all limbs (ignores negatives).</summary>
        public float TotalHp
        {
            get
            {
                float t = 0f;
                for (int i = 0; i < _hp.Length; i++) t += Mathf.Max(0f, _hp[i]);
                return t;
            }
        }

                /// <summary>Number of limbs currently fractured.</summary>
        public int FractureCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _fractured.Length; i++) if (_fractured[i]) n++;
                return n;
            }
        }

        private bool Painkilled => _wounds != null && _wounds.PainkillersActive;

        /// <summary>False with a broken or blacked-out leg (unless painkillers mask it).</summary>
                /// <summary>Whether the survivor can sprint (no broken or blacked‑out legs, unless painkillers hide it).</summary>
        public bool CanSprint
        {
            get
            {
                if (Painkilled) return true;
                return !(_fractured[(int)BodyZone.LeftLeg] || _fractured[(int)BodyZone.RightLeg]
                         || IsBlackedOut(BodyZone.LeftLeg) || IsBlackedOut(BodyZone.RightLeg));
            }
        }

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _wounds = GetComponent<PlayerWounds>();
            _clothing = GetComponent<PlayerClothing>();
            _blood = GetComponent<PlayerBlood>();
            _sleep = GetComponent<PlayerSleep>();
            for (int i = 0; i < _hp.Length; i++) { _max[i] = BaseMax[i]; _hp[i] = BaseMax[i]; }
        }

        private void Start()
        {
            if (!_loaded && UsesProfileHealth) LoadFromProfile();
            GameEvents.RaidEnded += OnRaidEnded;
            if (_stats.IsLocal) HudLimbs.Create(this);
        }

        private void OnDestroy() => GameEvents.RaidEnded -= OnRaidEnded;

        // A character's own body carries its injuries between raids and the hub; a scav or a monster is a fresh body.
        private static bool UsesProfileHealth =>
            ProfileStore.ActiveCharacter != null && (GameSession.IsHub || (GameSession.IsExtraction && !GameSession.IsScavRun && !GameSession.IsMonsterRun));

        private void OnRaidEnded(RaidOutcome outcome, string exit)
        {
            if (!UsesProfileHealth) return;
            if (outcome == RaidOutcome.Extracted) SaveToProfile();
            else ProfileStore.ActiveCharacter.Health = new HealthCarry();   // a new body (or a corpse) — injuries don't carry
            ProfileStore.Save();
        }

        #region Persistence
                /// <summary>Loads limb data from the saved profile if applicable.</summary>
        public void LoadFromProfile()
        {
            _loaded = true;
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null || c.Health == null) return;
            HealthCarry h = c.Health;
            for (int i = 0; i < _hp.Length; i++)
            {
                _max[i] = BaseMax[i];
                _hp[i] = i < h.LimbHp.Count ? Mathf.Clamp(h.LimbHp[i], 0f, _max[i]) : _max[i];
                _fractured[i] = i < h.Fractured.Count && h.Fractured[i];
            }
            if (_hp[(int)BodyZone.Head] <= 0f) _hp[(int)BodyZone.Head] = 1f;
            if (_hp[(int)BodyZone.Thorax] <= 0f) _hp[(int)BodyZone.Thorax] = 1f;
            SyncHealth();
            Changed?.Invoke();
        }

                /// <summary>Saves current limb data back to the player profile.</summary>
        public void SaveToProfile()
        {
            CharacterProfile c = ProfileStore.ActiveCharacter;
            if (c == null) return;
            HealthCarry h = c.Health ?? (c.Health = new HealthCarry());
            h.LimbHp.Clear();
            h.Fractured.Clear();
            bool hurt = false;
            for (int i = 0; i < _hp.Length; i++) if (_hp[i] < _max[i] - 0.5f || _fractured[i]) hurt = true;
            if (hurt)
                for (int i = 0; i < _hp.Length; i++) { h.LimbHp.Add(_hp[i]); h.Fractured.Add(_fractured[i]); }
            h.Hunger = _stats.Hunger;
            h.Thirst = _stats.Thirst;
            if (_blood != null) h.Blood = _blood.Blood;
        }
        #endregion

        #region Taking hits
        /// <summary>Applies any blow, bullet, blast or Zone effect to the body.</summary>
                /// <summary>Applies a damage hit (bullet, melee, etc.) to the survivor's body.</summary>
        public void ApplyHit(in DamageInfo info)
        {
            if (_stats == null || _stats.IsDead) return;
            BodyZone zone = info.ZoneKnown ? info.Zone
                : (info.HitPoint != Vector3.zero ? BodyZones.FromHit(transform, info.HitPoint) : BodyZones.RandomStrike());

            float hp = info.AmountHp;
            if (!info.HpScale) hp *= CloneZConfig.PlayerReceivedDamageMultiplier;
            if (info.Kind == DamageKind.Melee || info.Kind == DamageKind.Bite || info.Kind == DamageKind.Shove)
            {
                var skills = GetComponent<PlayerSkills>();
                if (skills != null) hp *= skills.IncomingDamageMult;
            }
            hp *= 1f - ResistFor(info.Kind);
            if (hp <= 0.01f) return;

            bool penetrated = true;
            if (info.Kind == DamageKind.Bullet || info.Kind == DamageKind.Melee || info.Kind == DamageKind.Bite || info.Kind == DamageKind.Explosion)
                hp = ThroughArmor(info, zone, hp, out penetrated);

            if (info.Kind == DamageKind.Radiation)
            {
                RadiationReceived?.Invoke(this, hp);
                return;
            }
            if (info.Kind == DamageKind.Psi)
            {
                _stats.Panic = Mathf.Min(CloneZConfig.PanicMax, _stats.Panic + hp * 0.8f);
                if (_blood != null) _blood.AddShock(hp * 0.6f);
                zone = BodyZone.Head;
            }

            DamageZone(zone, hp, CauseFor(info.Kind, zone));
            if (_stats.IsDead) return;
            OpenWounds(info, zone, hp, penetrated);
            if (info.Kind == DamageKind.Bullet || info.Kind == DamageKind.Explosion || info.Kind == DamageKind.Melee) GearCollateral(zone, hp, info.Kind);
            if (_blood != null && (info.Kind == DamageKind.Bullet || info.Kind == DamageKind.Explosion || info.Kind == DamageKind.Shock))
                _blood.AddShock(hp * (penetrated ? 0.45f : 0.25f));
            _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + hp * (penetrated ? 0.35f : 0.5f));
            Hit?.Invoke(zone, hp);
        }

        /// <summary>Radiation dose arriving as damage (the radiation system turns it into a dose).</summary>
                /// <summary>Registers a listener for radiation damage events.</summary>
        public static event System.Action<PlayerLimbs, float> RadiationReceived;

        /// <summary>A zombie's teeth or nails on one zone: armour there stops them cold. Returns true when it got through.</summary>
                /// <summary>Handles a zombie bite or claw strike, applying damage and possibly breaking the limb.</summary>
        public bool ZombieStrike(bool bite, float healthDamage, out BodyZone zone)
        {
            zone = BodyZones.RandomStrike();
            if (FindArmor(zone, out ClothingSlot slot, out ArmorStats armor, out PlayerClothing.WornPiece piece) && armor.Class >= 1)
            {
                WearArmor(slot, piece, 1f);
                DamageZone(zone, healthDamage * 0.15f / 100f * TotalMaxHp, "was torn apart");
                return false;
            }
            DamageZone(zone, healthDamage / 100f * TotalMaxHp, "was torn apart");
            return true;
        }

        private float ThroughArmor(in DamageInfo info, BodyZone zone, float hp, out bool penetrated)
        {
            penetrated = true;
            if (!FindArmor(zone, out ClothingSlot slot, out ArmorStats armor, out PlayerClothing.WornPiece piece)) return hp;
            float dur01 = piece.Condition01;
            switch (info.Kind)
            {
                case DamageKind.Bullet:
                {
                    float pen = info.Penetration > 0f ? info.Penetration : 20f;
                    float result = Ballistics.ResolveArmor(hp, pen, info.ArmorDamage, armor, dur01, out float loss, out penetrated);
                    WearArmor(slot, piece, loss);
                    if (!penetrated) Sfx.Play(SfxIds.HitArmor, transform.position + Vector3.up * 1.2f, 0.9f);
                    return result;
                }
                case DamageKind.Explosion:
                    WearArmor(slot, piece, hp * 0.1f);
                    return hp * (1f - Mathf.Min(0.6f, armor.Class * 0.08f * dur01));
                default:
                    WearArmor(slot, piece, Random.value < 0.5f ? 1f : 0f);
                    penetrated = armor.Class < 2;
                    return hp * (1f - Mathf.Min(0.85f, (0.2f + armor.Class * 0.12f) * dur01));
            }
        }

        /// <summary>The highest-class worn armour that covers <paramref name="zone"/> and still has durability.</summary>
                /// <summary>Finds the highest‑class armor covering a zone that still has durability.</summary>
        public bool FindArmor(BodyZone zone, out ClothingSlot slot, out ArmorStats armor, out PlayerClothing.WornPiece piece)
        {
            slot = ClothingSlot.Armor;
            armor = null;
            piece = default;
            if (_clothing == null) return false;
            for (int s = 0; s < s_ArmorSlots.Length; s++)
            {
                PlayerClothing.WornPiece p = _clothing.GetPiece(s_ArmorSlots[s]);
                if (!p.Exists || p.Condition <= 0) continue;
                ArmorStats a = WeaponUpgrades.Effective(Ballistics.Armor(p.Def.Id), p.Extra);
                if (a == null || a.Class <= 0) continue;
                bool covers = false;
                for (int z = 0; z < a.Zones.Length; z++) if (a.Zones[z] == zone) { covers = true; break; }
                if (!covers || (armor != null && a.Class <= armor.Class)) continue;
                armor = a;
                slot = s_ArmorSlots[s];
                piece = p;
            }
            return armor != null;
        }

        private void WearArmor(ClothingSlot slot, PlayerClothing.WornPiece piece, float loss)
        {
            if (loss <= 0f || _clothing == null || !piece.Exists) return;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(loss * WeaponUpgrades.WearMult(piece.Extra)));
            _clothing.SetCondition(slot, Mathf.Max(0, piece.Condition - dmg), true);
        }

        // DayZ: a hit damages what's worn there — and a round through the back can ruin what's in the bag.
        private void GearCollateral(BodyZone zone, float hp, DamageKind kind)
        {
            if (_clothing == null) return;
            ClothingSlot worn = zone == BodyZone.Head ? ClothingSlot.Head
                : zone == BodyZone.LeftLeg || zone == BodyZone.RightLeg ? ClothingSlot.Legs : ClothingSlot.Torso;
            PlayerClothing.WornPiece p = _clothing.GetPiece(worn);
            if (p.Exists && p.Def != null && p.Def.MaxCondition > 0 && Random.value < 0.6f)
                _clothing.SetCondition(worn, Mathf.Max(0, p.Condition - Mathf.Max(1, Mathf.RoundToInt(hp * 0.05f))), true);
            if (zone != BodyZone.Thorax && zone != BodyZone.Stomach && kind != DamageKind.Explosion) return;
            if (Random.value > (kind == DamageKind.Explosion ? 0.6f : 0.3f)) return;
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv == null) return;
            var carried = new System.Collections.Generic.List<ItemStack>();
            foreach (ItemStack s in inv.Inventory.Stacks)
                if (s != null && s.Def != null && s.Def.IsDamageable && s.Condition > 0 && s.Section != null && s.Section.Source != null
                    && (s.Section.Slot == ClothingSlot.Back || s.Section.Slot == ClothingSlot.Rig))
                    carried.Add(s);
            if (carried.Count == 0) return;
            ItemStack hit = carried[Random.Range(0, carried.Count)];
            hit.Condition = Mathf.Max(0, hit.Condition - Mathf.Max(1, Mathf.RoundToInt(hit.Def.MaxCondition * Random.Range(0.05f, 0.2f))));
            inv.Inventory.RaiseChanged();
            if (hit.Condition == 0) GameEvents.RaiseToast("The hit ruined the " + hit.Def.DisplayName + " in your pack.");
        }

        /// <summary>Total resistance to a damage type: the survivor's own (traits, artifacts, Zone marks) plus worn suits.</summary>
                /// <summary>Calculates total resistance to a specific damage kind from traits, artifacts and armor.</summary>
        public float ResistFor(DamageKind kind)
        {
            float r = PlayerModifiers.Current.Resist.For(kind);
            if (_clothing != null)
            {
                for (int s = 0; s < s_ArmorSlots.Length; s++)
                {
                    PlayerClothing.WornPiece p = _clothing.GetPiece(s_ArmorSlots[s]);
                    if (!p.Exists) continue;
                    ArmorStats a = WeaponUpgrades.Effective(Ballistics.Armor(p.Def.Id), p.Extra);
                    if (a != null) r += a.Resist.For(kind) * Mathf.Lerp(0.4f, 1f, p.Condition01);
                }
                // A gas mask with a live filter keeps the chemicals out of the lungs.
                if (kind == DamageKind.Chemical && PlayerGear.Instance != null && PlayerGear.Instance.HasGasMask && PlayerGear.Instance.FilterCharge01 > 0f) r += 0.4f;
            }
            return Mathf.Clamp(r, -0.5f, Resistances.Cap);
        }

        /// <summary>Takes hit points off one zone. A blacked-out zone passes the damage on across the rest of the body.</summary>
                /// <summary>Applies damage to a limb, spreading excess to others if the limb is blacked out.</summary>
        public void DamageZone(BodyZone zone, float hp, string cause)
        {
            if (hp <= 0f || _stats.IsDead) return;
            int z = (int)zone;
            float applied = 0f;
            if (_hp[z] > 0f)
            {
                float take = Mathf.Min(_hp[z], hp);
                _hp[z] -= take;
                applied += take;
                hp -= take;
            }
            if (hp > 0f && !BodyZones.IsVital(zone)) applied += Spread(hp * SpreadFactor, z);
            _stats.LimbDamage(applied / TotalMaxHp * CloneZConfig.HealthMax, cause);
            if (_hp[(int)BodyZone.Head] <= 0f) _stats.Kill(cause == "was torn apart" ? "had their skull opened" : "was killed by a shot to the head");
            else if (_hp[(int)BodyZone.Thorax] <= 0f) _stats.Kill(cause);
            Changed?.Invoke();
        }

        private float Spread(float hp, int from)
        {
            int alive = 0;
            for (int i = 0; i < _hp.Length; i++) if (i != from && _hp[i] > 0f) alive++;
            if (alive == 0) return 0f;
            float each = hp / alive, applied = 0f;
            for (int i = 0; i < _hp.Length; i++)
            {
                if (i == from || _hp[i] <= 0f) continue;
                float take = Mathf.Min(_hp[i], each);
                _hp[i] -= take;
                applied += take;
            }
            return applied;
        }

        private void OpenWounds(in DamageInfo info, BodyZone zone, float hp, bool penetrated)
        {
            if (_wounds == null) return;
            bool limb = BodyZones.IsArm(zone) || BodyZones.IsLeg(zone);
            switch (info.Kind)
            {
                case DamageKind.Bullet:
                    if (!penetrated) break;
                    _wounds.Inflict(Random.value < 0.25f + info.BleedChance * 0.6f ? WoundType.HeavyBleed : WoundType.Gunshot, zone);
                    if (limb && Random.value < info.FractureChance + hp / 220f) Fracture(zone);
                    break;
                case DamageKind.Explosion:
                    _wounds.Inflict(WoundType.Shard, zone);
                    if (Random.value < 0.35f) _wounds.Inflict(WoundType.HeavyBleed, BodyZones.RandomStrike());
                    if (limb && Random.value < 0.3f + info.FractureChance) Fracture(zone);
                    break;
                case DamageKind.Melee:
                    if (!penetrated) break;
                    _wounds.Inflict(hp > 55f ? WoundType.Deep : WoundType.Laceration, zone);
                    if (limb && Random.value < info.FractureChance) Fracture(zone);
                    break;
                case DamageKind.Bite:
                    if (penetrated) _wounds.Inflict(WoundType.Bite, zone);
                    break;
                case DamageKind.Burn:
                case DamageKind.Chemical:
                    if (hp > 4f) _wounds.Inflict(WoundType.Burn, zone);
                    break;
                case DamageKind.Fall:
                    zone = Random.value < 0.5f ? BodyZone.LeftLeg : BodyZone.RightLeg;
                    if (Random.value < hp / 60f) Fracture(zone);
                    break;
            }
            if (info.Knockback > 0f)
            {
                var cc = GetComponent<CharacterController>();
                Vector3 push = info.HitDirection;
                push.y = 0f;
                if (cc != null && cc.enabled && push.sqrMagnitude > 0.001f) cc.Move(push.normalized * Mathf.Min(0.6f, info.Knockback * 0.08f));
            }
        }

                /// <summary>Fractures a limb, updates pain and UI.</summary>
        public void Fracture(BodyZone zone)
        {
            if (!(BodyZones.IsArm(zone) || BodyZones.IsLeg(zone)) || _fractured[(int)zone]) return;
            _fractured[(int)zone] = true;
            _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + 30f);
            GameEvents.RaiseToast("Your " + BodyZones.Name(zone).ToLowerInvariant() + " is broken. Splint it.");
            Changed?.Invoke();
        }

        private static string CauseFor(DamageKind kind, BodyZone zone)
        {
            switch (kind)
            {
                case DamageKind.Bullet: return zone == BodyZone.Head ? "was killed by a shot to the head" : "was shot dead";
                case DamageKind.Explosion: return "was caught in a blast";
                case DamageKind.Burn: return "burned to death";
                case DamageKind.Shock: return "was electrocuted";
                case DamageKind.Chemical: return "dissolved in something the Zone left behind";
                case DamageKind.Psi: return "lost their mind";
                case DamageKind.Fall: return "was crushed";
                case DamageKind.Bite: return "was torn apart";
                default: return "was torn apart";
            }
        }
        #endregion

        #region Treatment
        /// <summary>Sets the worst fracture (legs first). False when nothing is broken.</summary>
                /// <summary>Attempts to splint the worst fracture (legs first). Returns true if a fracture was repaired.</summary>
        public bool Splint()
        {
            BodyZone[] order = { BodyZone.LeftLeg, BodyZone.RightLeg, BodyZone.LeftArm, BodyZone.RightArm };
            foreach (BodyZone z in order)
            {
                if (!_fractured[(int)z]) continue;
                _fractured[(int)z] = false;
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>Restores hit points to the most damaged zones that aren't blacked out. Returns the amount used.</summary>
                /// <summary>Heals the most damaged, non‑blacked‑out limbs up to the given amount of health.</summary>
        public float HealLimbs(float amount)
        {
            float used = 0f;
            for (int guard = 0; guard < 16 && amount > 0.01f; guard++)
            {
                int worst = -1;
                float worstFrac = 1f;
                for (int i = 0; i < _hp.Length; i++)
                {
                    if (_hp[i] <= 0f) continue;
                    float f = _hp[i] / _max[i];
                    if (f < worstFrac - 0.001f) { worstFrac = f; worst = i; }
                }
                if (worst < 0) break;
                float give = Mathf.Min(amount, _max[worst] - _hp[worst]);
                _hp[worst] += give;
                amount -= give;
                used += give;
            }
            if (used > 0f) { _stats.Heal(used / TotalMaxHp * CloneZConfig.HealthMax); Changed?.Invoke(); }
            return used;
        }

        /// <summary>Surgery on a blacked-out limb: it works again, but never as well (its maximum drops by a quarter).</summary>
                /// <summary>Performs surgery on a blacked‑out limb, restoring some functionality.</summary>
        public bool Surgery()
        {
            for (int i = 0; i < _hp.Length; i++)
            {
                if (_hp[i] > 0f || BodyZones.IsVital((BodyZone)i)) continue;
                _max[i] = Mathf.Max(BaseMax[i] * 0.4f, _max[i] * 0.75f);
                _hp[i] = Mathf.Min(_max[i], 1f + _max[i] * 0.25f);
                _stats.Pain = Mathf.Min(CloneZConfig.PainMax, _stats.Pain + 35f);
                Changed?.Invoke();
                return true;
            }
            return false;
        }

                /// <summary>Whether any limb is currently blacked out.</summary>
        public bool AnyBlackedOut
        {
            get
            {
                for (int i = 0; i < _hp.Length; i++) if (_hp[i] <= 0f) return true;
                return false;
            }
        }

                /// <summary>Restores all limbs to full health and clears fractures.</summary>
        public void FullHeal()
        {
            for (int i = 0; i < _hp.Length; i++) { _max[i] = BaseMax[i]; _hp[i] = BaseMax[i]; _fractured[i] = false; }
            SyncHealth();
            Changed?.Invoke();
        }

        // Keeps the overall health pool in step after loading injuries.
        private void SyncHealth()
        {
            float frac = TotalHp / TotalMaxHp;
            _stats.Health = Mathf.Min(_stats.Health, Mathf.Max(5f, frac * CloneZConfig.HealthMax));
        }
        #endregion

        private void Update()
        {
            if (_stats == null || _stats.IsDead) return;
            float dt = Time.deltaTime;
            float rate = HealPerSec * PlayerModifiers.Current.HealRate * (_sleep != null && _sleep.IsSleeping ? 4f : 1f);
            if (_stats.Hunger > 0.85f || _stats.Thirst > 0.85f) rate = 0f;
            bool changed = false;
            for (int i = 0; i < _hp.Length; i++)
            {
                if (_hp[i] <= 0f || _hp[i] >= _max[i] || _fractured[i]) continue;
                if (_wounds != null && _wounds.IsBleedingOn((BodyZone)i)) continue;
                _hp[i] = Mathf.Min(_max[i], _hp[i] + rate * dt);
                changed = true;
            }
            // A tourniquet left on starves the limb below it.
            if (_wounds != null && _wounds.TourniquetsOn > 0)
                for (int w = 0; w < _wounds.Wounds.Count; w++)
                {
                    Wound wd = _wounds.Wounds[w];
                    if (!wd.Tourniquet) continue;
                    int z = (int)wd.Zone;
                    if (_hp[z] > 0f) { _hp[z] = Mathf.Max(0f, _hp[z] - 0.06f * dt); changed = true; }
                }
            if (changed && Time.frameCount % 30 == 0) Changed?.Invoke();
        }

                /// <summary>Applies stat‑modifiers based on current limb health and injuries.</summary>
        public void ApplyMods(StatMods m)
        {
            bool pk = Painkilled;
            for (int i = 0; i < _hp.Length; i++)
            {
                var z = (BodyZone)i;
                bool black = _hp[i] <= 0f;
                if (BodyZones.IsLeg(z))
                {
                    if (black) m.MoveSpeed *= pk ? 0.85f : 0.7f;
                    else if (_fractured[i]) m.MoveSpeed *= pk ? 0.92f : 0.8f;
                }
                else if (BodyZones.IsArm(z))
                {
                    if (black) { m.AimSpread *= pk ? 1.3f : 1.6f; m.MeleeDamage *= 0.6f; m.ReloadSpeed *= 0.7f; }
                    else if (_fractured[i]) { m.AimSpread *= pk ? 1.1f : 1.25f; m.Recoil *= 1.2f; m.ReloadSpeed *= 0.85f; }
                }
                else if (z == BodyZone.Stomach && black) { m.HungerRate *= 3f; m.ThirstRate *= 3f; }
                else if (z == BodyZone.Head && Hp01(z) < 0.5f) m.AimSpread *= 1.1f;
            }
        }
    }

    /// <summary>The body-zone doll on the HUD: each zone coloured by its hit points, broken bones and bleeds marked.</summary>
    public class HudLimbs : MonoBehaviour
    {
        private PlayerLimbs _limbs;
        private PlayerWounds _wounds;
        private readonly Image[] _parts = new Image[BodyZones.Count];
        private readonly Text[] _marks = new Text[BodyZones.Count];
        private float _flash;

        public static HudLimbs Create(PlayerLimbs limbs)
        {
            RectTransform root = HudLayer.Anchor("Limbs", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 150f), new Vector2(84f, 150f));
            var h = root.gameObject.AddComponent<HudLimbs>();
            h._limbs = limbs;
            h._wounds = limbs.GetComponent<PlayerWounds>();
            // Head, thorax, stomach, arms, legs laid out as a little silhouette.
            h.Part(BodyZone.Head, new Vector2(32f, 124f), new Vector2(20f, 22f));
            h.Part(BodyZone.Thorax, new Vector2(26f, 88f), new Vector2(32f, 34f));
            h.Part(BodyZone.Stomach, new Vector2(26f, 62f), new Vector2(32f, 24f));
            h.Part(BodyZone.LeftArm, new Vector2(10f, 62f), new Vector2(14f, 60f));
            h.Part(BodyZone.RightArm, new Vector2(60f, 62f), new Vector2(14f, 60f));
            h.Part(BodyZone.LeftLeg, new Vector2(26f, 2f), new Vector2(15f, 58f));
            h.Part(BodyZone.RightLeg, new Vector2(43f, 2f), new Vector2(15f, 58f));
            limbs.Hit += h.OnHit;
            return h;
        }

        private void Part(BodyZone z, Vector2 pos, Vector2 size)
        {
            Image img = UIKit.Panel(transform, z.ToString(), Color.green);
            UIKit.Place(img.rectTransform, Vector2.zero, Vector2.zero, pos, size);
            img.raycastTarget = false;
            _parts[(int)z] = img;
            Text t = UIKit.Label(img.rectTransform, "", 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Stretch(t.rectTransform);
            _marks[(int)z] = t;
        }

        private void OnHit(BodyZone zone, float hp) { _flash = 0.35f; }

        private void OnDestroy() { if (_limbs != null) _limbs.Hit -= OnHit; }

        private void Update()
        {
            if (_limbs == null) return;
            _flash = Mathf.Max(0f, _flash - Time.deltaTime);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
            for (int i = 0; i < _parts.Length; i++)
            {
                var z = (BodyZone)i;
                float f = _limbs.Hp01(z);
                Color c = f <= 0f ? new Color(0.08f, 0.08f, 0.08f, 0.9f)
                    : f > 0.6f ? Color.Lerp(UIKit.Warn, UIKit.Good, (f - 0.6f) / 0.4f)
                    : Color.Lerp(UIKit.Bad, UIKit.Warn, f / 0.6f);
                c.a = 0.85f;
                bool bleeding = _wounds != null && _wounds.IsBleedingOn(z);
                if (bleeding) c = Color.Lerp(c, new Color(0.9f, 0.05f, 0.05f, 0.95f), pulse * 0.6f);
                if (_flash > 0f) c = Color.Lerp(c, Color.white, _flash);
                _parts[i].color = c;
                _marks[i].text = _limbs.IsFractured(z) ? "F" : f <= 0f ? "X" : "";
            }
        }
    }
}
