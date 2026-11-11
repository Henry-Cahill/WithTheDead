using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Melee and firearms. Melee (left mouse) swings the weapon in hand as its <see cref="MeleeStats"/> say: a wind-up before
    /// the blow lands (whatever is in its reach and arc then is struck) and a recovery after it before the next; a stab or a
    /// short chop meets one body, a wider swing carries through two or three, each taking less of it; each blow may stagger
    /// one of the dead (spoiling its lunge) or knock it down, by the weapon's chances, else it only flinches and keeps
    /// coming. An attack with one of the dead on the ground nearest at the feet (knocked down, or a crawler) is a stomp
    /// instead, on its head if that's in reach, else on its body for less; with nothing standing in a swing, the swing comes
    /// down on the nearest one on the ground. The shove
    /// staggers and shoves back, and may knock the dead down for a stomp. Damage and crit maths follow Project Zomboid's
    /// CombatConfig values.
    ///
    /// Firearms (Tarkov's lesson: the round matters more than the gun): every shot leaves the muzzle — never the
    /// camera, so cover blocks a round even when the camera sees past it — and is resolved by
    /// <see cref="CombatResolver"/> with the loaded round's damage and penetration against whatever armour it meets.
    /// The gun's attachments, technician upgrades and fouling bend its spread, recoil, noise, range, magazine and
    /// jam rate; semi-automatic guns fire once per press, automatic ones while held; follow-up shots bloom with
    /// recoil. Any round of the gun's calibre chambers, including the military-grade money. In the shoulder and
    /// first-person views the right button aims down the sights (the shove moves to the middle button).
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        // Height of the shouldered gun's muzzle in the shoulder / first-person views.
        private const float ShoulderMuzzleHeight = 1.35f;
        private const float DirtPerShot = 0.0022f;

        [Header("Shove")]
        public float ShoveRange = 2.0f;
        public float ShoveArc = 120f;
        public float ShoveCooldown = 0.7f;
        public float ShoveKnockback = 5.5f;

        private PlayerController _controller;
        private PlayerStats _stats;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private PlayerClothing _clothing;
        private Actor _actor;
        private float _shoveTimer;
        private float _fireTimer;
        private float _reloadTimer;
        private float _bloom;
        private bool _attackPressOnUi;   // the held left button went down over the UI
        private bool _shovePressOnUi;    // the held right button went down over the UI
        private readonly HashSet<IDamageable> _hitThisAction = new HashSet<IDamageable>();
        private readonly List<Struck> _struck = new List<Struck>();
        // Cache for the last equipped item stack used to compute weapon build
        private ItemStack _lastEquippedStack;
        private float _lastDirt;
        private bool _buildDirty = true;

        // A melee blow under way: wound up at the press, it lands when _strikeTimer runs out (with the weapon it was swung
        // with), and nothing else — another blow, a stomp, a shove — can start until _busyTimer has run out too (its recovery).
        private enum Blow : byte { None, Swing, Stomp }
        private Blow _blow;
        private float _strikeTimer;
        private float _busyTimer;
        private MeleeStats _blowStats;
        private IDamageable _stompTarget;
        // A click while the last blow (or a shove) is still recovering isn't lost: it's kept this long and swings as soon as it can.
        private const float AttackBuffer = 0.3f;
        private float _attackQueued;

        /// <summary>How the melee weapon in hand handles (bare hands when there's none; see <see cref="MeleeStats"/>).</summary>
        public MeleeStats Melee { get; private set; } = MeleeStats.BareHands;
        /// <summary>Press to press of a swing with the melee weapon in hand.</summary>
        public float SwingCooldown => Melee.Cycle;
        /// <summary>The wind-up and the recovery of the last blow begun (a swing or a stomp), as quickened by skill and slowed
        /// by exhaustion: the avatar times its swing to them.</summary>
        public float BlowWindUp { get; private set; }
        public float BlowRecovery { get; private set; }
        /// <summary>True from the press until the blow lands.</summary>
        public bool WindingUp => _blow != Blow.None;
        /// <summary>True until the blow under way (or a shove) is over and another can begin.</summary>
        public bool MeleeBusy => _busyTimer > 0f;

        /// <summary>The equipped firearm, or null when using melee / bare hands.</summary>
        public ItemDef EquippedFirearm { get; private set; }
        /// <summary>Current ammo count loaded in the equipped firearm.</summary>
        public int LoadedAmmo { get; private set; }
        /// <summary>The kind of round in the magazine (any of the gun's calibre).</summary>
        /// <summary>Identifier of the ammunition type currently loaded, matching the firearm's calibre.</summary>
        public string LoadedAmmoId { get; private set; }
                /// <summary>Indicates whether the player is currently reloading their firearm.</summary>
        public bool IsReloading => _reloadTimer > 0f;
        /// <summary>Aiming down the sights (shoulder / first-person views, right button held).</summary>
        public bool AimingDownSights { get; private set; }
        /// <summary>The current gun build (attachments, upgrades, fouling).</summary>
        public WeaponBuild Build { get; private set; }

        public event System.Action OnSwing;
        /// <summary>Raised when a stomp begins (on one of the dead on the ground at the survivor's feet).</summary>
        public event System.Action OnStomp;
        public event System.Action OnShove;
        public event System.Action OnFire;
        public event System.Action OnReload;
        public event System.Action OnDryFire;
        /// <summary>Raised on every shot with its recoil kick (the camera and the animator read it).</summary>
        public event System.Action<float> OnRecoil;
        /// <summary>Raised when a shot jams.</summary>
        public event System.Action OnJam;
        /// <summary>Raised for each melee blow that lands (the target, and whether it killed): the HUD's hit marker.</summary>
        public event System.Action<IDamageable, bool> OnMeleeHit;
        /// <summary>Raised as a wound-up blow (a swing or a stomp) lands, with how many bodies it struck (0: thin air).</summary>
        public event System.Action<int> OnBlowLanded;

        private PlayerInventory Inv => _inventory != null ? _inventory : (_inventory = GetComponent<PlayerInventory>());
        private PlayerSkills Skills => _skills != null ? _skills : (_skills = GetComponent<PlayerSkills>());
        private PlayerClothing Clothing => _clothing != null ? _clothing : (_clothing = GetComponent<PlayerClothing>());
        private Actor Me => _actor != null ? _actor : (_actor = GetComponent<Actor>());

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
        }

        // PlayerInventory is added after this component. Its Changed event covers what alters the build in place:
        // fitting or stripping attachments, upgrades, wear and repairs.
        private void Start()
        {
            if (Inv != null) Inv.Changed += MarkBuildDirty;
        }

        private void OnDestroy()
        {
            if (_inventory != null) _inventory.Changed -= MarkBuildDirty;
        }

        private void MarkBuildDirty() => _buildDirty = true;

        private void Update()
        {
            float dt = Time.deltaTime;
            _shoveTimer -= dt;
            _busyTimer -= dt;
            _attackQueued -= dt;
            _fireTimer -= dt;
            _reloadTimer -= dt;
            _bloom = Mathf.Max(0f, _bloom - dt * 1.6f);

            Mouse mouse = Mouse.current;
            TrackUiPresses(mouse);                 // before the early-outs, so a click on a menu is remembered
            AimingDownSights = false;
            Actor self = Actor.LocalPlayer;
            bool handsTied = self != null && self.gameObject == gameObject && (self.Surrendered || self.Restrained);
            if (_stats.IsDead || _stats.Incapacitated || handsTied)
            {
                CancelBlow();
                return;
            }
            // A blow already wound up lands whatever the survivor does meanwhile (a menu opened, the build tool taken up).
            if (_blow != Blow.None)
            {
                _strikeTimer -= dt;
                if (_strikeTimer <= 0f) LandBlow();
            }
            if (UIState.GameplayBlocked) return;   // don't act while clicking in the inventory UI
            if (UIState.BuildMode) return;         // in build mode the mouse places barricades, not attacks
            if (HubRules.SafeZone) return;         // no fighting in the hub
            if (mouse == null) return;

            bool attackHeld = mouse.leftButton.isPressed && !_attackPressOnUi;
            bool attackPressed = mouse.leftButton.wasPressedThisFrame && !_attackPressOnUi;
            bool rightHeld = mouse.rightButton.isPressed && !_shovePressOnUi;
            bool shove = ViewState.MouseLook && EquippedFirearm != null ? mouse.middleButton.wasPressedThisFrame : rightHeld;

            if (EquippedFirearm != null)
            {
                UpdateBuildCache();
                AimingDownSights = ViewState.MouseLook && rightHeld;
                if (AimingDownSights && GameInput.Held(GameAction.HoldBreath)) _stats.TryConsumeEndurance(0.12f * Time.deltaTime);
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame) TryReload();
                bool trigger = Build.FullAuto ? attackHeld : attackPressed;
                if (trigger && _fireTimer <= 0f && _reloadTimer <= 0f) FireFirearm();
                else if (shove && _shoveTimer <= 0f) Shove();
                return;
            }

            // Held, the attack swings again as soon as the last blow is over; a click during its recovery is kept a moment.
            if (attackPressed && _busyTimer > 0f) _attackQueued = AttackBuffer;
            if ((attackHeld || _attackQueued > 0f) && _busyTimer <= 0f)
            {
                _attackQueued = 0f;
                Attack();
            }
            else if (shove && _shoveTimer <= 0f && _busyTimer <= 0f) Shove();
        }

        // A press that begins on the UI belongs to the UI, not to the weapon: clicking a HUD button under the
        // free isometric cursor (or anything while a menu owns the screen) never swings, fires or shoves,
        // even if the button is held afterwards. With the cursor locked for mouse-look the press always
        // counts — the crosshair is never "over" the HUD.
        private void TrackUiPresses(Mouse mouse)
        {
            if (mouse == null) return;
            bool left = mouse.leftButton.wasPressedThisFrame;
            bool right = mouse.rightButton.wasPressedThisFrame;
            if (left || right)
            {
                bool onUi = UIState.GameplayBlocked || (!ViewState.MouseLook && PointerOverUi());
                if (left) _attackPressOnUi = onUi;
                if (right) _shovePressOnUi = onUi;
            }
            if (!mouse.leftButton.isPressed) _attackPressOnUi = false;
            if (!mouse.rightButton.isPressed) _shovePressOnUi = false;
        }

        private static bool PointerOverUi()
        {
            EventSystem events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

        #region Melee
        /// <summary>Takes up a melee weapon (bare hands for null): leaves ranged mode, and drops a blow under way.</summary>
        public void EquipMelee(ItemDef weapon)
        {
            EquipMeleeMode();
            Melee = MeleeStats.For(weapon);
        }

        // One body a blow can reach: which way it is and how far (flat), how far off the line of the aim, whether it's on the
        // ground, and where a blow meets it (one on the ground: the nearest of its body).
        private struct Struck
        {
            public IDamageable Target;
            public Actor Body;
            public Vector3 To, Point;
            public float Distance, Angle;
            public bool Downed;
        }

        // An attack: a stomp when, of everything it could strike, the nearest is one of the dead on the ground at the feet;
        // else a swing of the weapon in hand.
        private void Attack()
        {
            MeleeStats m = Melee;
            Gather(Mathf.Max(m.Reach, CloneZConfig.StompReach), Mathf.Max(m.Arc, CloneZConfig.StompArc), false, _struck);
            int nearest = -1;
            for (int i = 0; i < _struck.Count; i++)
            {
                Struck s = _struck[i];
                bool reachable = s.Downed
                    ? s.Distance <= CloneZConfig.StompReach && s.Angle <= CloneZConfig.StompArc * 0.5f
                    : s.Distance <= m.Reach + 0.5f && s.Angle <= m.Arc * 0.5f;
                if (reachable && (nearest < 0 || s.Distance < _struck[nearest].Distance)) nearest = i;
            }
            if (nearest >= 0 && _struck[nearest].Downed) BeginStomp(_struck[nearest].Target);
            else BeginSwing(m);
        }

        private void BeginSwing(MeleeStats m)
        {
            float speed = BlowSpeed(m.Skill);
            BeginBlow(Blow.Swing, m, m.WindUp / speed, m.Recovery / speed, m.Endurance);
            OnSwing?.Invoke();
        }

        private void BeginStomp(IDamageable target)
        {
            float speed = BlowSpeed(PlayerSkills.Skill.Strength);
            _stompTarget = target;
            BeginBlow(Blow.Stomp, Melee, CloneZConfig.StompWindUp / speed, CloneZConfig.StompRecovery / speed, CloneZConfig.StompEnduranceCost);
            OnStomp?.Invoke();
        }

        private void BeginBlow(Blow kind, MeleeStats m, float windUp, float recovery, float endurance)
        {
            _blow = kind;
            _blowStats = m;
            BlowWindUp = windUp;
            BlowRecovery = recovery;
            _strikeTimer = windUp;
            _busyTimer = windUp + recovery;
            _stats.TryConsumeEndurance(endurance);
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeSwing);
        }

        // Skill with the weapon (Strength, for a stomp) quickens a blow; tired arms slow it.
        private float BlowSpeed(PlayerSkills.Skill skill)
        {
            float speed = 1f + (Skills != null ? Skills.GetLevel(skill) * CloneZConfig.MeleeSkillSpeedPerLevel : 0f);
            if (_stats.Endurance < CloneZConfig.EnduranceDanger) speed /= CloneZConfig.MeleeExhaustedSlowdown;
            return speed;
        }

        // A weapon put away, a gun taken up, the survivor downed: the blow under way never lands (nor one clicked for).
        private void CancelBlow()
        {
            _blow = Blow.None;
            _stompTarget = null;
            _attackQueued = 0f;
        }

        private void LandBlow()
        {
            Blow kind = _blow;
            _blow = Blow.None;
            if (kind == Blow.Stomp) StompLands();
            else SwingLands(_blowStats ?? Melee);
        }

        private void SwingLands(MeleeStats m)
        {
            Sfx.Play(SfxIds.MeleeSwing, transform.position + Vector3.up * 1.2f, 0.6f, Random.Range(0.9f, 1.1f), 12f);
            bool exhausted = _stats.Endurance < CloneZConfig.EnduranceDanger;
            float felling = exhausted ? 0.5f : 1f;                // a tired swing rocks nothing back
            float skillDamageMult = (Skills != null ? Skills.MeleeDamageMult(m.Skill) : 1f) * PlayerModifiers.Current.MeleeDamage;
            int strength = Skills != null ? Skills.GetLevel(PlayerSkills.Skill.Strength) : 0;

            Gather(m.Reach, m.Arc, false, _struck);
            int hits = MeetInSwing(m, _struck);
            for (int i = 0; i < hits; i++)
            {
                Struck s = _struck[i];
                IDamageable target = s.Target;
                Transform tt = target.Transform;
                float share = MeleeStats.Carry(i);   // each body further along the swing takes less of it

                // Crit chance: base 5%, +30% when striking from behind (PZ); a crit is a blow to the head. Brought down on one on
                // the ground, the blow finds the head if it's in reach (likelier to crit there; where it then lands), else the
                // nearest of the body, and no crit.
                Transform head = s.Downed ? HeadOf(s.Body) : null;
                bool atHead = s.Downed && (head == null || FlatDistance(head.position, transform.position) <= m.Reach);
                bool fromBehind = !s.Downed && Vector3.Dot(tt.forward, s.To) > 0.25f;
                float critChance = s.Downed && !atHead ? 0f
                    : CloneZConfig.CritChanceDefault + (fromBehind ? CloneZConfig.CritChanceFromBehind : 0f) + (atHead ? CloneZConfig.MeleeDownedCritBonus : 0f);
                bool crit = Random.value < critChance;

                float raw = m.Damage * skillDamageMult * share * Random.Range(0.85f, 1.15f);
                if (exhausted) raw *= 0.5f;                       // tired swings are feeble
                raw *= _stats.PainCombatMultiplier;               // pain saps the strength from a blow
                if (crit) raw *= CloneZConfig.HeadHitDamageSplitModifier;

                float knockdown = m.Knockdown > 0f ? (m.Knockdown + strength * CloneZConfig.KnockdownPerStrength) * share * felling : 0f;
                Vector3 at = !s.Downed ? tt.position + Vector3.up * 1.2f : OnTheGround(atHead && head != null ? head.position : s.Point, tt);
                var info = new DamageInfo(raw, DamageKind.Melee, at, s.To, gameObject)
                {
                    FromBehind = fromBehind,
                    Knockback = m.Knockback * share,
                    WeaponId = m.Id,
                    BleedChance = m.BleedChance,
                    FractureChance = m.FractureChance,
                    Stagger = Mathf.Clamp01(m.Stagger * share * felling),
                    Knockdown = Mathf.Clamp01(knockdown),
                }.WithZone(crit ? BodyZone.Head : BodyZones.RandomStrike());
                info.IsHeadshot = crit;
                bool killed = CombatResolver.Apply(target, info, Me);
                OnMeleeHit?.Invoke(target, killed);
                Sfx.Play(SfxIds.MeleeHit, info.HitPoint, 0.8f * Mathf.Lerp(0.6f, 1f, share), Random.Range(0.9f, 1.1f), 16f);

                if (Skills != null)
                {
                    Skills.AddXp(m.Skill, CloneZConfig.XpMeleeHit);
                    Skills.AddXp(PlayerSkills.Skill.Strength, CloneZConfig.XpStrengthPerHit);
                    if (killed) Skills.AddXp(m.Skill, CloneZConfig.XpMeleeKill);
                }
            }

            if (hits > 0) NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeHit);
            if (hits > 0 && Inv != null && m == Melee) Inv.WearEquippedWeapon();   // a connecting blow wears the weapon
            OnBlowLanded?.Invoke(hits);
        }

        // The bodies a swing meets, first to last, put at the front of `found`; returns how many. It carries through the
        // standing (the one nearest the line of the swing first, then outward) up to the weapon's count, passing over any on
        // the ground; with nothing standing in it, it comes down on the nearest one on the ground.
        private static int MeetInSwing(MeleeStats m, List<Struck> found)
        {
            s_SwingHalfArc = Mathf.Max(1f, m.Arc * 0.5f);
            found.Sort(s_SwingOrder);
            int standing = 0;
            while (standing < found.Count && !found[standing].Downed) standing++;
            if (standing > 0) return Mathf.Min(standing, Mathf.Max(1, m.MaxTargets));
            return found.Count > 0 ? 1 : 0;
        }

        private static float s_SwingHalfArc = 45f;
        private static readonly System.Comparison<Struck> s_SwingOrder = (a, b) =>
            a.Downed != b.Downed ? (a.Downed ? 1 : -1) : SwingScore(a).CompareTo(SwingScore(b));

        // Near and on the line of the swing first: a body at the edge of the arc counts half as far again.
        private static float SwingScore(in Struck s) => s.Distance * (1f + 0.5f * s.Angle / s_SwingHalfArc);

        // The heel comes down on the one picked at the press, if it's still on the ground under the foot (it may have got up,
        // or been knocked away, during the wind-up): on its head if that's in reach, stood over it; else on the nearest of its
        // body, a stomp that does StompBodyShare as much. Harder in boots and with Strength.
        private void StompLands()
        {
            IDamageable target = _stompTarget;
            _stompTarget = null;
            int hits = 0;
            Actor body = target != null && !target.IsDead && target.Transform != null ? target.Transform.GetComponent<Actor>() : null;
            if (body != null && body.Downed)
            {
                Vector3 feet = transform.position;
                Transform head = HeadOf(body);
                bool onHead = head == null || FlatDistance(head.position, feet) <= CloneZConfig.StompReach;
                Vector3 underfoot = onHead && head != null ? head.position : NearestOf(target, feet);
                Vector3 d = underfoot - feet;
                d.y = 0f;
                if (d.magnitude <= CloneZConfig.StompReach + 0.4f)
                {
                    hits = 1;
                    int strength = Skills != null ? Skills.GetLevel(PlayerSkills.Skill.Strength) : 0;
                    float raw = CloneZConfig.StompDamage * (1f + strength * CloneZConfig.StompStrengthPerLevel)
                        * PlayerModifiers.Current.MeleeDamage * Random.Range(0.9f, 1.1f);
                    if (!onHead) raw *= CloneZConfig.StompBodyShare;
                    if (Clothing != null && Clothing.GetWorn(ClothingSlot.Feet) != null) raw *= CloneZConfig.StompBootsMult;
                    if (_stats.Endurance < CloneZConfig.EnduranceDanger) raw *= 0.5f;
                    raw *= _stats.PainCombatMultiplier;
                    Vector3 at = OnTheGround(underfoot, target.Transform);
                    // Off the head, the heel lands on the chest or the belly near it, else on the legs.
                    BodyZone zone = onHead ? BodyZone.Head
                        : FlatDistance(underfoot, head.position) <= 0.8f ? BodyZone.Thorax
                        : FlatDistance(underfoot, head.position) <= 1.1f ? BodyZone.Stomach
                        : Random.value < 0.5f ? BodyZone.LeftLeg : BodyZone.RightLeg;
                    var info = new DamageInfo(raw, DamageKind.Melee, at, CloneZUtil.Flat(d), gameObject)
                    {
                        WeaponId = StompId,
                        FractureChance = 0.2f,
                        Stagger = 0f,
                    }.WithZone(zone);
                    bool killed = CombatResolver.Apply(target, info, Me);
                    OnMeleeHit?.Invoke(target, killed);
                    Sfx.Play(SfxIds.MeleeHit, at, 0.9f, Random.Range(0.68f, 0.78f), 16f);
                    NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeHit);
                    if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Strength, CloneZConfig.XpStrengthPerHit);
                }
            }
            OnBlowLanded?.Invoke(hits);
        }

        /// <summary>The "weapon" a stomp kills with (kill feeds, quests).</summary>
        public const string StompId = "stomp";

        /// <summary>Where a downed body's head is, if it has one to find: the actor's head bone (a networked body's), else its
        /// humanoid rig's (one of the dead's).</summary>
        public static Transform HeadOf(Actor body)
        {
            if (body == null) return null;
            if (body.Head != null) return body.Head;
            ZombieAvatar avatar = body.GetComponent<ZombieAvatar>();
            return avatar != null && avatar.Rig != null ? avatar.Rig.Bone(HumanBone.Head) : null;
        }

        // A point of a body lying on the ground, just above the ground it lies on (for where a blow is heard and shown).
        private static Vector3 OnTheGround(Vector3 p, Transform body) => new Vector3(p.x, body.position.y + 0.2f, p.z);

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // A shove staggers what's standing in front and shoves it back, and may put one of the dead down (more with Strength,
        // more from behind or into one already reeling): the opening for a stomp.
        private void Shove()
        {
            CancelBlow();
            _shoveTimer = ShoveCooldown;
            _busyTimer = Mathf.Max(_busyTimer, CloneZConfig.ShoveRecovery);
            _stats.TryConsumeEndurance(CloneZConfig.MeleeEnduranceCost * 0.5f);
            OnShove?.Invoke();
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeSwing * 0.5f);

            int strength = Skills != null ? Skills.GetLevel(PlayerSkills.Skill.Strength) : 0;
            float knockdown = CloneZConfig.ShoveKnockdownChance + strength * CloneZConfig.KnockdownPerStrength;
            if (_stats.Endurance < CloneZConfig.EnduranceDanger) knockdown *= 0.5f;
            Gather(ShoveRange, ShoveArc, true, _struck);
            for (int i = 0; i < _struck.Count; i++)
            {
                Struck s = _struck[i];
                var info = new DamageInfo(0f, DamageKind.Shove, s.Target.Transform.position, s.To, gameObject)
                {
                    Knockback = ShoveKnockback,
                    FromBehind = Vector3.Dot(s.Target.Transform.forward, s.To) > 0.25f,
                    Knockdown = Mathf.Clamp01(knockdown),
                };
                s.Target.TakeDamage(info);
            }
        }

        /// <summary>Living hostile things within <paramref name="range"/> inside a cone of <paramref name="arcDegrees"/> about the
        /// aim: the dead, mutants, animals, hostile stalkers — and other players when PvP is on. Each one on the ground is
        /// marked, or left out with <paramref name="standingOnly"/>.</summary>
        private void Gather(float range, float arcDegrees, bool standingOnly, List<Struck> into)
        {
            into.Clear();
            _hitThisAction.Clear();
            Vector3 origin = transform.position + Vector3.up * 1f;
            float cos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
            Vector3 aim = _controller.AimDirection;

            Collider[] hits = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider col in hits)
            {
                if (col.transform.IsChildOf(transform)) continue;
                IDamageable target = null;
                Hitbox box = col.GetComponent<Hitbox>();
                if (box != null && box.Owner != null) target = box.Owner.Damageable;
                if (target == null) target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead || !_hitThisAction.Add(target)) continue;
                if (target is BuildPiece piece && !ZoneCamps.CanBash(piece.Owner)) continue;   // walls are battered by the dead —
                if (target is Barricade planks && !ZoneCamps.CanBash(planks.Owner)) continue;  // and by raiders breaking into someone else's camp
                if (target is WallBreach) continue;   // a building's walls, and boards across a hole in one: the build tool's job
                Actor other = target.Transform != null ? target.Transform.GetComponent<Actor>() : null;
                if (other != null && Me != null && !CanHarm(other)) continue;
                bool downed = other != null && other.Downed;
                if (downed && standingOnly) continue;

                // One on the ground is measured to the nearest of its body (stood over its head, it's at the feet), the rest
                // to where they stand.
                Vector3 point = downed ? NearestOf(target, transform.position) : target.Transform.position;
                Vector3 d = point - transform.position;
                d.y = 0f;
                float distance = d.magnitude;
                Vector3 to = distance > 1e-3f ? d / distance : Vector3.zero;
                if (to != Vector3.zero && Vector3.Dot(aim, to) < cos) continue;
                into.Add(new Struck
                {
                    Target = target,
                    Body = other,
                    To = to,
                    Point = point,
                    Distance = distance,
                    Angle = to != Vector3.zero ? Vector3.Angle(aim, to) : 0f,
                    Downed = downed,
                });
            }
        }

        // The point of a body nearest `from`, on whichever of its colliders are on (one of the dead on the ground lies along a
        // box, see ZombieAI), else where it stands.
        private static Vector3 NearestOf(IDamageable target, Vector3 from)
        {
            Transform t = target.Transform;
            t.GetComponents(s_Colliders);
            Vector3 best = t.position;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < s_Colliders.Count; i++)
            {
                Collider c = s_Colliders[i];
                if (c == null || !c.enabled) continue;
                Vector3 q = c.ClosestPoint(from);
                Vector3 flat = q - from;
                flat.y = 0f;
                if (flat.sqrMagnitude < bestSqr)
                {
                    bestSqr = flat.sqrMagnitude;
                    best = q;
                }
            }
            return best;
        }

        private static readonly List<Collider> s_Colliders = new List<Collider>(4);

        // Squadmates are never hit unless friendly fire is on; other players only with PvP on.
        private bool CanHarm(Actor other)
        {
            if (other == Me) return false;
            bool sameSquad = !string.IsNullOrEmpty(Me.Squad) && Me.Squad == other.Squad;
            if (sameSquad) return ServerRules.FriendlyFire;
            if (other.IsPlayerControlled && other.Kind != ActorKind.PlayerMonster) return ServerRules.PvpEnabled;
            return true;
        }
        #endregion
        #region Firearm
        /// <summary>Switches to ranged mode with the given gun, chambering a magazine if ammo is on hand.</summary>
        public void EquipFirearm(ItemDef gun)
        {
            if (gun == null || !gun.IsFirearm) return;
            CancelBlow();
            ReturnLoadedAmmo();          // hand the old gun's rounds back to the pack first
            EquippedFirearm = gun;
            LoadedAmmo = 0;
            LoadedAmmoId = null;
            _fireTimer = 0f;
            _reloadTimer = 0f;
            TryReload();                 // top up from the pack
        }

        /// <summary>Leaves ranged mode (called when a melee weapon or bare hands are equipped); a blow under way never lands.</summary>
        public void EquipMeleeMode()
        {
            CancelBlow();
            ReturnLoadedAmmo();
            EquippedFirearm = null;
        }

        /// <summary>Restores a firearm and its loaded rounds from a saved game, without pulling
        /// from the pack (the saved magazine count is authoritative — see <see cref="SaveSystem"/>).</summary>
        public void RestoreFirearm(ItemDef gun, int loaded)
        {
            if (gun == null || !gun.IsFirearm) { EquipMelee(null); return; }
            CancelBlow();
            EquippedFirearm = gun;
            ItemStack st = Inv != null ? Inv.EquippedStack : null;
            string chosen = st != null && st.Extra != null ? st.Extra.AmmoChoice : null;
            LoadedAmmoId = Ballistics.Accepts(gun, chosen) ? chosen : gun.AmmoId;
            LoadedAmmo = Mathf.Clamp(loaded, 0, Mathf.Max(gun.MagazineSize, Ballistics.Build(st).MagSize));
            _fireTimer = 0f;
            _reloadTimer = 0f;
        }

        private void ReturnLoadedAmmo()
        {
            if (EquippedFirearm != null && LoadedAmmo > 0 && Inv != null)
                Inv.ReturnAmmo(string.IsNullOrEmpty(LoadedAmmoId) ? EquippedFirearm.AmmoId : LoadedAmmoId, LoadedAmmo);
            LoadedAmmo = 0;
            LoadedAmmoId = null;
        }

        /// <summary>The round to load: the gun's chosen ammunition if any is carried, else any round of its calibre.</summary>
        public string PickAmmo(ItemDef gun)
        {
            if (gun == null || Inv == null) return null;
            ItemStack st = Inv.EquippedStack;
            string chosen = st != null && st.Extra != null ? st.Extra.AmmoChoice : null;
            if (!string.IsNullOrEmpty(chosen) && Inv.AmmoReserve(chosen) > 0) return chosen;
            if (!string.IsNullOrEmpty(LoadedAmmoId) && Inv.AmmoReserve(LoadedAmmoId) > 0) return LoadedAmmoId;
            if (Inv.AmmoReserve(gun.AmmoId) > 0) return gun.AmmoId;
            List<ItemStack> stacks = Inv.Inventory.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].Def != null && stacks[i].Def.Category == ItemCategory.Ammo && Ballistics.Accepts(gun, stacks[i].Def.Id))
                    return stacks[i].Def.Id;
            return null;
        }

        /// <summary>Unloads the magazine and reloads with a different round of the same calibre.</summary>
        public void SwitchAmmo(string ammoId)
        {
            ItemDef gun = EquippedFirearm;
            if (gun == null || Inv == null || !Ballistics.Accepts(gun, ammoId)) return;
            ItemStack st = Inv.EquippedStack;
            if (st != null) st.Ensure().AmmoChoice = ammoId;
            ReturnLoadedAmmo();
            TryReload();
        }

        /// <summary>Pulls rounds from the pack into the magazine. Returns true if any were loaded.</summary>
        public bool TryReload()
        {
            UpdateBuildCache();
            ItemDef gun = EquippedFirearm;
            if (gun == null || Inv == null) return false;
            int mag = Mathf.Max(1, Build.MagSize);
            if (LoadedAmmo >= mag) return false;
            string ammo = LoadedAmmo > 0 ? LoadedAmmoId : PickAmmo(gun);
            if (string.IsNullOrEmpty(ammo)) return false;
            int got = Inv.TakeAmmo(ammo, mag - LoadedAmmo);
            if (got <= 0) return false;
            LoadedAmmo += got;
            LoadedAmmoId = ammo;
            float reloadMult = (Skills != null ? Skills.ReloadTimeMult : 1f) * Build.ReloadMult / Mathf.Max(0.3f, PlayerModifiers.Current.ReloadSpeed);
            _reloadTimer = gun.ReloadTime * reloadMult;
            OnReload?.Invoke();
            Sfx.Play(WeaponAudio.ReloadSound(gun), transform.position + Vector3.up * 1.2f, 0.7f, 1f, 16f);
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Reloading, CloneZConfig.XpReload);
             // ensure build reflects any dirt changes after reload
            return true;
        }

        private void FireFirearm()
        {
            ItemDef gun = EquippedFirearm;
                
                WeaponBuild build = Build;
                if (LoadedAmmo <= 0)
                {
                    if (!TryReload())        // out of rounds — try to reload, else dry-fire
                    {
                        NoiseSystem.Emit(transform.position, CloneZConfig.NoiseDryFire);
                        _fireTimer = 0.3f;
                        OnDryFire?.Invoke();
                        Sfx.Play(SfxIds.DryFire, transform.position + Vector3.up * 1.2f, 0.6f, 1f, 8f);
                    }
                    return;                  // a reload consumes this trigger pull
                }


            // A worn or fouled firearm can jam: the round doesn't fire and the action must be worked before
            // trying again (the chambered round is kept, so the trigger pull is simply wasted).
            if (Random.value < build.JamChance)
            {
                _fireTimer = Mathf.Max(gun.FireCooldown, 0.9f);
                NoiseSystem.Emit(transform.position, CloneZConfig.NoiseDryFire);
                OnDryFire?.Invoke();
                OnJam?.Invoke();
                Sfx.Play(SfxIds.Jam, transform.position + Vector3.up * 1.2f, 0.7f, 1f, 8f);
                GameEvents.RaiseToast("Jammed! Clear it and clean that gun.");
                return;
            }

            string ammoId = string.IsNullOrEmpty(LoadedAmmoId) ? gun.AmmoId : LoadedAmmoId;
            LoadedAmmo--;
            _fireTimer = gun.FireCooldown / Mathf.Max(0.3f, build.FireRateMult);
            _stats.TryConsumeEndurance(CloneZConfig.FirearmEnduranceCost);
            OnFire?.Invoke();
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Aiming, CloneZConfig.XpFirearmShot);
                // After fouling the barrel, the build may have changed (dirt affects stats)
                
                FoulBarrel();

            ShotLine(out Vector3 muzzle, out Vector3 shotAim);

            if (gun.Id == "flaregun") { FireFlare(muzzle, shotAim); return; }

            // Aiming skill tightens the cone, boosts damage and extends effective range (PZ).
            float dmgMult = (Skills != null ? Skills.FirearmDamageMult : 1f) * build.DamageMult;
            float spreadMult = (Skills != null ? Skills.FirearmSpreadMult : 1f) * build.Spread * PlayerModifiers.Current.AimSpread;
            if (AimingDownSights) spreadMult *= 0.55f * build.AdsSpread;
            // Holding your breath (first person / bodycam) steadies a sighted shot, for as long as the lungs last.
            if (AimingDownSights && GameInput.Held(GameAction.HoldBreath) && _stats.Endurance > 0.1f) spreadMult *= 0.6f;
            AmmoStats ammo = Ballistics.Ammo(ammoId);
            float range = gun.FirearmRange * (Skills != null ? Skills.FirearmRangeMult : 1f) * build.Range * (ammo != null ? ammo.RangeMult : 1f);
            float spread = (gun.FirearmSpread + _bloom +
                Mathf.Lerp(CloneZConfig.FirearmSpreadWide, CloneZConfig.FirearmSpreadTight, ComputeAimQuality())) * spreadMult;

            int pellets = ammo != null ? Mathf.Max(1, ammo.Pellets) : Mathf.Max(1, gun.PelletsPerShot);
            float pelletMult = pellets > 1 ? 1f : 1f;   // ammo / FirearmDamage is per pellet: every pellet hits in full
            bool anyHit = false, anyKill = false;
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ScatterDir(shotAim, spread);
                var req = new ShotRequest
                {
                    Origin = muzzle,
                    Direction = dir,
                    Range = range,
                    AmmoId = ammoId,
                    WeaponId = gun.Id,
                    DamageMult = dmgMult * pelletMult,
                    Shooter = gameObject,
                    ShooterActor = Me,
                    ShooterClient = NetAuthority.LocalClientId,
                    ViewTime = NetClock.ViewTime,
                };
                ShotResult r = CombatResolver.Fire(req);
                if (r.Target != null) anyHit = true;
                if (r.Killed) anyKill = true;
                if (ammo != null && ammo.Tracer) CombatFX.Tracer(muzzle, r.End);   // only a tracer round draws a trail
                if (NetAuthority.IsClientOnly) WeaponAudio.Shot(req, r);   // the server resolves it; hear where it went here
            }

            if (Skills != null)
            {
                if (anyHit) Skills.AddXp(PlayerSkills.Skill.Aiming, CloneZConfig.XpFirearmHit);
                if (anyKill) Skills.AddXp(PlayerSkills.Skill.Aiming, CloneZConfig.XpFirearmKill);
            }

            if (!build.Suppressed) CombatFX.MuzzleFlash(muzzle);
            float noise = gun.FirearmNoise * build.Noise;
            NoiseSystem.Emit(transform.position, noise);   // gunfire draws the horde
            GameEvents.RaiseWorldEvent(transform.position, noise, "shot");
            string report = WeaponAudio.ShotSound(gun, build.Suppressed);
            Sfx.Play(report, muzzle, build.Suppressed ? 0.45f : 1f, Random.Range(0.96f, 1.04f), WeaponAudio.Reach(report));

            float kick = build.Recoil * PlayerModifiers.Current.Recoil * (AimingDownSights ? 0.75f : 1f);
            _bloom = Mathf.Min(6f, _bloom + kick * 0.6f);
            OnRecoil?.Invoke(kick);
            if (Inv != null) Inv.WearEquippedWeapon();   // the shot may cost a condition point; at 0 the gun breaks
        }

        // Every shot fouls the barrel a little; a cleaning kit (or the workbench) clears it.
        private void FoulBarrel()
        {
            ItemStack st = Inv != null ? Inv.EquippedStack : null;
            if (st == null) return;
            float add = DirtPerShot * (WeatherSystem.Instance != null && WeatherSystem.Instance.PrecipitationIntensity > 0.2f ? 1.6f : 1f);
            ItemExtra x = st.Ensure();
            x.Dirt = Mathf.Min(1f, x.Dirt + add);
        }

        // A signal flare: up it goes, red and bright, and if a landing zone is near the helicopter answers.
        private void FireFlare(Vector3 muzzle, Vector3 aim)
        {
            Vector3 dir = (aim + Vector3.up * 2.2f).normalized;
            FlareProjectile.Launch(muzzle, dir);
            Sfx.Play(SfxIds.GunFlare, muzzle, 1f, Random.Range(0.97f, 1.03f), WeaponAudio.Reach(SfxIds.GunFlare));
            NoiseSystem.Emit(transform.position, 40f);
            GameEvents.RaiseWorldEvent(transform.position, 160f, "flare");
            bool answered = ExtractionManager.Instance != null && ExtractionManager.Instance.CallHelicopter(transform.position);
            if (!answered) GameEvents.RaiseToast("The flare burns red over the Zone. No landing zone in range — everyone saw it, though.");
        }

        /// <summary>Where the next shot leaves from and which way it goes, before spread: through the crosshair in the
        /// shoulder and first-person views, flat along the aim from just below chest height in the isometric one. A laser on
        /// the gun shows where it ends (<see cref="WeaponTactical"/>).</summary>
        public void ShotLine(out Vector3 muzzle, out Vector3 direction)
        {
            Vector3 aim = _controller.AimDirection;
            if (ViewState.MouseLook)
            {
                AimThroughCrosshair(aim, out muzzle, out direction);
                return;
            }
            // Cast from just below chest height so shots still connect with short crawlers,
            // while staying clear of the ground (aim is flat on the XZ plane).
            muzzle = transform.position + Vector3.up * 0.95f + aim * 0.7f;
            direction = aim;
        }

        private void AimThroughCrosshair(Vector3 flatAim, out Vector3 muzzle, out Vector3 shotAim)
        {
            muzzle = transform.position + Vector3.up * ShoulderMuzzleHeight + flatAim * 0.55f;
            shotAim = flatAim;
            ZombieHealth zombie = _controller.CrosshairZombie;
            Vector3 target = zombie != null && !zombie.IsDead ? _controller.CrosshairZombiePoint : _controller.AimPoint;
            Vector3 to = target - muzzle;
            if (to.sqrMagnitude < 0.25f) return; // pressed against it: keep the level aim
            Vector3 dir = to.normalized;
            if (Vector3.Dot(dir, flatAim) > 0.1f) shotAim = dir; // never back over the shoulder
        }

        /// <summary>
        /// Update cached weapon build if the equipped stack changed.
        /// </summary>
        // Removed per‑frame weapon‑build refresh – the build cache is now only updated when necessary elsewhere.
        private void UpdateBuildCache()
        {
            // Update cached weapon build based on currently equipped stack.
            ItemStack cur = Inv != null ? Inv.EquippedStack : null;
            float dirt = cur.DirtLevel();
            // Rebuilt only on a change: the build follows which stack is held, its fouling (shots, cleaning) and,
            // through the pack's Changed event, its mods, upgrades and wear.
            if (cur == _lastEquippedStack && dirt == _lastDirt && !_buildDirty) return;
            Build = Ballistics.Build(cur);
            _lastEquippedStack = cur;
            _lastDirt = dirt;
            _buildDirty = false;
        }

        /// <summary>Aim quality 0..1 from PZ's firearm to-hit model (worse while moving).</summary>
        private float ComputeAimQuality()
        {
            float aim = CloneZConfig.FirearmAimBase;
            switch (_stats.Exertion)
            {
                case Exertion.Sprinting: aim -= CloneZConfig.FirearmAimPenaltySprint; break;
                case Exertion.Running: aim -= CloneZConfig.FirearmAimPenaltyRun; break;
                case Exertion.Walking: aim -= CloneZConfig.FirearmAimPenaltyWalk; break;
            }
            if (_controller.IsSneaking) aim += CloneZConfig.FirearmAimSneakBonus;
            aim += Build.Ergonomics / 100f * 0.12f - 0.06f;                 // a handier build steadies faster
            aim -= _stats.PainNormalized * CloneZConfig.PainAimPenalty;    // pain makes the hands shake
            aim -= _stats.PanicNormalized * CloneZConfig.PanicAimPenalty;  // fear makes them shake harder
            aim -= _stats.ColdAimPenalty;                                  // shivering in the cold spoils the aim
            return Mathf.Clamp(aim, CloneZConfig.FirearmAimMin, CloneZConfig.FirearmAimMax);
        }

        private static Vector3 ScatterDir(Vector3 aim, float spreadDeg)
        {
            Vector3 dir = Quaternion.AngleAxis(Random.Range(-spreadDeg, spreadDeg), Vector3.up) * aim;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
            dir = Quaternion.AngleAxis(Random.Range(-spreadDeg, spreadDeg) * 0.5f, side.normalized) * dir;
            return dir.normalized;
        }
        #endregion
    }

    /// <summary>A signal flare in flight: a red light climbing, hanging, drifting down and burning out.</summary>
    public sealed class FlareProjectile : MonoBehaviour
    {
        private Vector3 _vel;
        private float _life = 22f;
        private Light _light;
        private AudioSource _burn;

        public static void Launch(Vector3 from, Vector3 dir)
        {
            var go = new GameObject("Flare");
            go.transform.position = from;
            var f = go.AddComponent<FlareProjectile>();
            f._vel = dir * 38f;
            f._light = go.AddComponent<Light>();
            f._light.type = LightType.Point;
            f._light.color = new Color(1f, 0.25f, 0.15f);
            f._light.range = 60f;
            f._light.intensity = 6f;
            f._light.shadows = LightShadows.None;
            ZoneKit.Plume(go.transform, from, true, 0.6f, 18f, 0.5f, new Color(0.9f, 0.4f, 0.35f, 0.5f));
            f._burn = Sfx.Loop(SfxIds.FlareBurn, go.transform, 0.6f, 50f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _life -= dt;
            _vel += Physics.gravity * (_vel.y > 0f ? 0.6f : 0.04f) * dt;
            _vel.y = Mathf.Max(_vel.y, -1.2f);
            _vel.x *= 1f - 0.4f * dt;
            _vel.z *= 1f - 0.4f * dt;
            transform.position += _vel * dt;
            if (_light != null) _light.intensity = 6f * Mathf.Clamp01(_life / 4f) * (0.85f + Mathf.PerlinNoise(Time.time * 9f, 0f) * 0.3f);
            if (_life <= 0f || transform.position.y < 0.2f)
            {
                if (_burn != null) Sfx.Stop(_burn);
                Destroy(gameObject);
            }
        }
    }

    /// <summary>The clock shots are stamped with for lag compensation (set by the netcode; 0 offline).</summary>
    public static class NetClock
    {
        public static System.Func<double> ViewTimeSource;
        public static double ViewTime => ViewTimeSource != null ? ViewTimeSource() : 0d;
    }
}
