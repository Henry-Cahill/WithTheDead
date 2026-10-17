using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Melee combat: a wide swing (left mouse) that damages zombies in an arc,
    /// and a shove (right mouse) that staggers and shoves them back. Damage and
    /// crit maths follow Project Zomboid's CombatConfig values. With a firearm the
    /// left mouse shoots instead: level along the aim in the isometric view, or from
    /// the shouldered gun at whatever the crosshair is on in the mouse-look views.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        // Height of the shouldered gun's muzzle in the shoulder / first-person views.
        private const float ShoulderMuzzleHeight = 1.35f;

        [Header("Melee weapon")]
        public float WeaponDamage = 0.9f;   // raw; the zombie applies the x1.5 received multiplier
        public float WeaponRange = 2.4f;
        public float WeaponArc = 90f;        // degrees of the swing cone
        public float SwingCooldown = 0.55f;
        public float SwingKnockback = 2.5f;

        [Header("Shove")]
        public float ShoveRange = 2.0f;
        public float ShoveArc = 120f;
        public float ShoveCooldown = 0.7f;
        public float ShoveKnockback = 5.5f;

        private PlayerController _controller;
        private PlayerStats _stats;
        private PlayerInventory _inventory;
        private PlayerSkills _skills;
        private float _swingTimer;
        private float _shoveTimer;
        private float _fireTimer;
        private float _reloadTimer;
        private bool _attackPressOnUi;   // the held left button went down over the UI
        private bool _shovePressOnUi;    // the held right button went down over the UI
        private readonly HashSet<ZombieHealth> _hitThisAction = new HashSet<ZombieHealth>();

        /// <summary>The equipped firearm, or null when using melee / bare hands.</summary>
        public ItemDef EquippedFirearm { get; private set; }
        public int LoadedAmmo { get; private set; }
        public bool IsReloading => _reloadTimer > 0f;

        public event System.Action OnSwing;
        public event System.Action OnShove;
        public event System.Action OnFire;
        public event System.Action OnReload;
        public event System.Action OnDryFire;

        private PlayerInventory Inv => _inventory != null ? _inventory : (_inventory = GetComponent<PlayerInventory>());
        private PlayerSkills Skills => _skills != null ? _skills : (_skills = GetComponent<PlayerSkills>());

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            _swingTimer -= Time.deltaTime;
            _shoveTimer -= Time.deltaTime;
            _fireTimer -= Time.deltaTime;
            _reloadTimer -= Time.deltaTime;

            Mouse mouse = Mouse.current;
            TrackUiPresses(mouse);                 // before the early-outs, so a click on a menu is remembered
            if (_stats.IsDead) return;
            if (UIState.GameplayBlocked) return;   // don't act while clicking in the inventory UI
            if (UIState.BuildMode) return;         // in build mode the mouse places barricades, not attacks
            if (mouse == null) return;

            bool attack = mouse.leftButton.isPressed && !_attackPressOnUi;
            bool shove = mouse.rightButton.isPressed && !_shovePressOnUi;

            if (EquippedFirearm != null)
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame) TryReload();
                if (attack && _fireTimer <= 0f && _reloadTimer <= 0f) FireFirearm();
                else if (shove && _shoveTimer <= 0f) Shove();
                return;
            }

            if (attack && _swingTimer <= 0f) Swing();
            else if (shove && _shoveTimer <= 0f) Shove();
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

        private void Swing()
        {
            _swingTimer = SwingCooldown;
            _stats.TryConsumeEndurance(CloneZConfig.MeleeEnduranceCost);
            OnSwing?.Invoke();
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeSwing);

            bool exhausted = _stats.Endurance < CloneZConfig.EnduranceDanger;
            bool landedHit = false;
            _hitThisAction.Clear();

            PlayerSkills.Skill weaponSkill = PlayerSkills.WeaponSkillFor(Inv != null ? Inv.EquippedWeapon : null);
            float skillDamageMult = Skills != null ? Skills.MeleeDamageMult(weaponSkill) : 1f;

            foreach (ZombieHealth zombie in FindTargets(WeaponRange, WeaponArc))
            {
                landedHit = true;
                Vector3 toZombie = CloneZUtil.Flat(zombie.transform.position - transform.position);

                // Crit chance: base 5%, +30% when striking from behind (PZ).
                bool fromBehind = Vector3.Dot(zombie.transform.forward, toZombie) > 0.25f;
                float critChance = CloneZConfig.CritChanceDefault + (fromBehind ? CloneZConfig.CritChanceFromBehind : 0f);
                bool crit = Random.value < critChance;

                float raw = WeaponDamage * skillDamageMult * Random.Range(0.85f, 1.15f);
                if (exhausted) raw *= 0.5f;                       // tired swings are feeble
                raw *= _stats.PainCombatMultiplier;               // pain saps the strength from a blow
                if (crit) raw *= CloneZConfig.HeadHitDamageSplitModifier;

                var info = new DamageInfo(raw, DamageKind.Melee, zombie.transform.position, toZombie, gameObject)
                {
                    FromBehind = fromBehind,
                    IsHeadshot = crit,
                    Knockback = SwingKnockback
                };
                bool wasDead = zombie.IsDead;
                zombie.TakeDamage(info);

                if (Skills != null)
                {
                    Skills.AddXp(weaponSkill, CloneZConfig.XpMeleeHit);
                    Skills.AddXp(PlayerSkills.Skill.Strength, CloneZConfig.XpStrengthPerHit);
                    if (!wasDead && zombie.IsDead) Skills.AddXp(weaponSkill, CloneZConfig.XpMeleeKill);
                }
            }

            if (landedHit) NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeHit);
            if (landedHit && Inv != null) Inv.WearEquippedWeapon();   // a connecting blow wears the weapon
        }

        private void Shove()
        {
            _shoveTimer = ShoveCooldown;
            _stats.TryConsumeEndurance(CloneZConfig.MeleeEnduranceCost * 0.5f);
            OnShove?.Invoke();
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeSwing * 0.5f);

            _hitThisAction.Clear();
            foreach (ZombieHealth zombie in FindTargets(ShoveRange, ShoveArc))
            {
                Vector3 dir = CloneZUtil.Flat(zombie.transform.position - transform.position);
                var info = new DamageInfo(0f, DamageKind.Shove, zombie.transform.position, dir, gameObject)
                {
                    Knockback = ShoveKnockback
                };
                zombie.TakeDamage(info);
            }
        }

        #region Firearm
        /// <summary>Switches to ranged mode with the given gun, chambering a magazine if ammo is on hand.</summary>
        public void EquipFirearm(ItemDef gun)
        {
            if (gun == null || !gun.IsFirearm) return;
            ReturnLoadedAmmo();          // hand the old gun's rounds back to the pack first
            EquippedFirearm = gun;
            LoadedAmmo = 0;
            _fireTimer = 0f;
            _reloadTimer = 0f;
            TryReload();                 // top up from the pack
        }

        /// <summary>Leaves ranged mode (called when a melee weapon or bare hands are equipped).</summary>
        public void EquipMeleeMode()
        {
            ReturnLoadedAmmo();
            EquippedFirearm = null;
        }

        /// <summary>Restores a firearm and its loaded rounds from a saved game, without pulling
        /// from the pack (the saved magazine count is authoritative — see <see cref="SaveSystem"/>).</summary>
        public void RestoreFirearm(ItemDef gun, int loaded)
        {
            if (gun == null || !gun.IsFirearm) { EquipMeleeMode(); return; }
            EquippedFirearm = gun;
            LoadedAmmo = Mathf.Clamp(loaded, 0, gun.MagazineSize);
            _fireTimer = 0f;
            _reloadTimer = 0f;
        }

        private void ReturnLoadedAmmo()
        {
            if (EquippedFirearm != null && LoadedAmmo > 0 && Inv != null)
                Inv.ReturnAmmo(EquippedFirearm.AmmoId, LoadedAmmo);
            LoadedAmmo = 0;
        }

        /// <summary>Pulls rounds from the pack into the magazine. Returns true if any were loaded.</summary>
        public bool TryReload()
        {
            ItemDef gun = EquippedFirearm;
            if (gun == null || LoadedAmmo >= gun.MagazineSize || Inv == null) return false;
            int got = Inv.TakeAmmo(gun.AmmoId, gun.MagazineSize - LoadedAmmo);
            if (got <= 0) return false;
            LoadedAmmo += got;
            _reloadTimer = gun.ReloadTime * (Skills != null ? Skills.ReloadTimeMult : 1f);
            OnReload?.Invoke();
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Reloading, CloneZConfig.XpReload);
            return true;
        }

        private void FireFirearm()
        {
            ItemDef gun = EquippedFirearm;
            if (LoadedAmmo <= 0)
            {
                if (!TryReload())        // out of rounds — try to reload, else dry-fire
                {
                    NoiseSystem.Emit(transform.position, CloneZConfig.NoiseDryFire);
                    _fireTimer = 0.3f;
                    OnDryFire?.Invoke();
                }
                return;                  // a reload consumes this trigger pull
            }

            // A worn firearm can jam: the round doesn't fire and the action must be worked before
            // trying again (the chambered round is kept, so the trigger pull is simply wasted).
            if (Inv != null && Inv.EquippedFirearmJams())
            {
                _fireTimer = gun.FireCooldown;
                NoiseSystem.Emit(transform.position, CloneZConfig.NoiseDryFire);
                OnDryFire?.Invoke();
                return;
            }

            LoadedAmmo--;
            _fireTimer = gun.FireCooldown;
            _stats.TryConsumeEndurance(CloneZConfig.FirearmEnduranceCost);
            OnFire?.Invoke();
            if (Skills != null) Skills.AddXp(PlayerSkills.Skill.Aiming, CloneZConfig.XpFirearmShot);

            // Aiming skill tightens the cone, boosts damage and extends effective range (PZ).
            float dmgMult = Skills != null ? Skills.FirearmDamageMult : 1f;
            float spreadMult = Skills != null ? Skills.FirearmSpreadMult : 1f;
            float range = gun.FirearmRange * (Skills != null ? Skills.FirearmRangeMult : 1f);

            Vector3 aim = _controller.AimDirection;
            Vector3 muzzle, shotAim;
            if (ViewState.MouseLook) AimThroughCrosshair(aim, out muzzle, out shotAim);
            else
            {
                // Cast from just below chest height so shots still connect with short crawlers,
                // while staying clear of the ground (aim is flat on the XZ plane).
                muzzle = transform.position + Vector3.up * 0.95f + aim * 0.7f;
                shotAim = aim;
            }
            float spread = (gun.FirearmSpread +
                Mathf.Lerp(CloneZConfig.FirearmSpreadWide, CloneZConfig.FirearmSpreadTight, ComputeAimQuality())) * spreadMult;
            // Walls / cover (Default) block the round; zombies live on Ignore Raycast so include that layer.
            int mask = (1 << 0) | (1 << CloneZConfig.IgnoreRaycastLayer);
            var tracerColor = new Color(1f, 0.9f, 0.5f);

            bool anyHit = false, anyKill = false;
            int pellets = Mathf.Max(1, gun.PelletsPerShot);
            for (int i = 0; i < pellets; i++)
            {
                Vector3 dir = ScatterDir(shotAim, spread);
                Vector3 end = muzzle + dir * range;
                if (Physics.Raycast(muzzle, dir, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    var zombie = hit.collider.GetComponentInParent<ZombieHealth>();
                    if (zombie != null && !zombie.IsDead)
                    {
                        var info = new DamageInfo(gun.FirearmDamage * dmgMult, DamageKind.Bullet, hit.point, CloneZUtil.Flat(dir), gameObject)
                        {
                            Knockback = gun.Knockback
                        };
                        bool wasDead = zombie.IsDead;
                        zombie.TakeDamage(info);
                        anyHit = true;
                        if (!wasDead && zombie.IsDead) anyKill = true;
                    }
                }
                CombatFX.Tracer(muzzle, end, tracerColor);
            }

            if (Skills != null)
            {
                if (anyHit) Skills.AddXp(PlayerSkills.Skill.Aiming, CloneZConfig.XpFirearmHit);
                if (anyKill) Skills.AddXp(PlayerSkills.Skill.Aiming, CloneZConfig.XpFirearmKill);
            }

            CombatFX.MuzzleFlash(muzzle);
            NoiseSystem.Emit(transform.position, gun.FirearmNoise);   // gunfire draws the horde
            if (Inv != null) Inv.WearEquippedWeapon();   // the shot may cost a condition point; at 0 the gun breaks
        }

        /// <summary>
        /// Shoulder / first-person views: the round leaves the shouldered gun and flies at what the
        /// crosshair is on — a living zombie under it, else the surface behind — so a shot can rise or dip
        /// (over a counter, down at a crawler) instead of staying level like the isometric aim.
        /// </summary>
        private void AimThroughCrosshair(Vector3 flatAim, out Vector3 muzzle, out Vector3 shotAim)
        {
            muzzle = transform.position + Vector3.up * ShoulderMuzzleHeight + flatAim * 0.55f;
            shotAim = flatAim;
            ZombieHealth zombie = _controller.CrosshairZombie;
            Vector3 target = zombie != null && !zombie.IsDead ? _controller.CrosshairZombiePoint : _controller.AimPoint;
            Vector3 to = target - muzzle;
            if (to.sqrMagnitude < 0.25f) return;                // pressed against it: keep the level aim
            Vector3 dir = to.normalized;
            if (Vector3.Dot(dir, flatAim) > 0.1f) shotAim = dir;   // never back over the shoulder
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

        /// <summary>Yields living zombies inside a cone in front of the aim direction.</summary>
        private IEnumerable<ZombieHealth> FindTargets(float range, float arcDegrees)        {
            Vector3 origin = transform.position + Vector3.up * 1f;
            float cos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
            Vector3 aim = _controller.AimDirection;

            Collider[] hits = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Ignore);
            foreach (Collider col in hits)
            {
                var zombie = col.GetComponentInParent<ZombieHealth>();
                if (zombie == null || zombie.IsDead || !_hitThisAction.Add(zombie)) continue;

                Vector3 to = CloneZUtil.Flat(zombie.transform.position - transform.position);
                if (to == Vector3.zero || Vector3.Dot(aim, to) >= cos)
                    yield return zombie;
            }
        }
    }
}
