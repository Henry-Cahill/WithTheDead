using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CloneZ
{
    /// <summary>
    /// Melee and firearms. Melee: a wide swing (left mouse) that damages whatever hostile thing is in the arc, and a
    /// shove that staggers and shoves back. Damage and crit maths follow Project Zomboid's CombatConfig values.
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
        private Actor _actor;
        private float _swingTimer;
        private float _shoveTimer;
        private float _fireTimer;
        private float _reloadTimer;
        private float _bloom;
        private bool _attackPressOnUi;   // the held left button went down over the UI
        private bool _shovePressOnUi;    // the held right button went down over the UI
        private readonly HashSet<IDamageable> _hitThisAction = new HashSet<IDamageable>();
        private readonly List<IDamageable> _targets = new List<IDamageable>();
        // Cache for the last equipped item stack used to compute weapon build
        private ItemStack _lastEquippedStack;

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

        private PlayerInventory Inv => _inventory != null ? _inventory : (_inventory = GetComponent<PlayerInventory>());
        private PlayerSkills Skills => _skills != null ? _skills : (_skills = GetComponent<PlayerSkills>());
        private Actor Me => _actor != null ? _actor : (_actor = GetComponent<Actor>());

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _swingTimer -= dt;
            _shoveTimer -= dt;
            _fireTimer -= dt;
            _reloadTimer -= dt;
            _bloom = Mathf.Max(0f, _bloom - dt * 1.6f);

            Mouse mouse = Mouse.current;
            TrackUiPresses(mouse);                 // before the early-outs, so a click on a menu is remembered
            AimingDownSights = false;
            if (_stats.IsDead || _stats.Incapacitated) return;
            if (UIState.GameplayBlocked) return;   // don't act while clicking in the inventory UI
            if (UIState.BuildMode) return;         // in build mode the mouse places barricades, not attacks
            if (HubRules.SafeZone) return;         // no fighting in the hub
            Actor self = Actor.LocalPlayer;
            if (self != null && self.gameObject == gameObject && (self.Surrendered || self.Restrained)) return;   // hands up or cuffed
            if (mouse == null) return;

            bool attackHeld = mouse.leftButton.isPressed && !_attackPressOnUi;
            bool attackPressed = mouse.leftButton.wasPressedThisFrame && !_attackPressOnUi;
            bool rightHeld = mouse.rightButton.isPressed && !_shovePressOnUi;
            bool shove = ViewState.MouseLook && EquippedFirearm != null ? mouse.middleButton.wasPressedThisFrame : rightHeld;

            if (EquippedFirearm != null)
            {
                
                AimingDownSights = ViewState.MouseLook && rightHeld;
                if (AimingDownSights && GameInput.Held(GameAction.HoldBreath)) _stats.TryConsumeEndurance(0.12f * Time.deltaTime);
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame) TryReload();
                bool trigger = Build.FullAuto ? attackHeld : attackPressed;
                if (trigger && _fireTimer <= 0f && _reloadTimer <= 0f) FireFirearm();
                else if (shove && _shoveTimer <= 0f) Shove();
                return;
            }

            if (attackHeld && _swingTimer <= 0f) Swing();
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

        #region Melee
        private void Swing()
        {
            _swingTimer = SwingCooldown;
            _stats.TryConsumeEndurance(CloneZConfig.MeleeEnduranceCost);
            OnSwing?.Invoke();
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeSwing);
            Sfx.Play(SfxIds.MeleeSwing, transform.position + Vector3.up * 1.2f, 0.6f, Random.Range(0.9f, 1.1f), 12f);

            bool exhausted = _stats.Endurance < CloneZConfig.EnduranceDanger;
            bool landedHit = false;
            _hitThisAction.Clear();

            PlayerSkills.Skill weaponSkill = PlayerSkills.WeaponSkillFor(Inv != null ? Inv.EquippedWeapon : null);
            float skillDamageMult = Skills != null ? Skills.MeleeDamageMult(weaponSkill) : 1f;
            skillDamageMult *= PlayerModifiers.Current.MeleeDamage;

            FindTargets(WeaponRange, WeaponArc, _targets);
            for (int i = 0; i < _targets.Count; i++)
            {
                IDamageable target = _targets[i];
                landedHit = true;
                Transform tt = target.Transform;
                Vector3 toTarget = CloneZUtil.Flat(tt.position - transform.position);

                // Crit chance: base 5%, +30% when striking from behind (PZ).
                bool fromBehind = Vector3.Dot(tt.forward, toTarget) > 0.25f;
                float critChance = CloneZConfig.CritChanceDefault + (fromBehind ? CloneZConfig.CritChanceFromBehind : 0f);
                bool crit = Random.value < critChance;

                float raw = WeaponDamage * skillDamageMult * Random.Range(0.85f, 1.15f);
                if (exhausted) raw *= 0.5f;                       // tired swings are feeble
                raw *= _stats.PainCombatMultiplier;               // pain saps the strength from a blow
                if (crit) raw *= CloneZConfig.HeadHitDamageSplitModifier;

                var info = new DamageInfo(raw, DamageKind.Melee, tt.position + Vector3.up * 1.2f, toTarget, gameObject)
                {
                    FromBehind = fromBehind,
                    Knockback = SwingKnockback,
                    WeaponId = Inv != null && Inv.EquippedWeapon != null ? Inv.EquippedWeapon.Id : "fists",
                    BleedChance = weaponSkill == PlayerSkills.Skill.Blade ? 0.35f : 0.1f,
                    FractureChance = weaponSkill == PlayerSkills.Skill.Blunt ? 0.15f : 0.05f,
                }.WithZone(crit ? BodyZone.Head : BodyZones.RandomStrike());
                info.IsHeadshot = crit;
                bool killed = CombatResolver.Apply(target, info, Me);
                OnMeleeHit?.Invoke(target, killed);
                Sfx.Play(SfxIds.MeleeHit, tt.position + Vector3.up * 1.2f, 0.8f, Random.Range(0.9f, 1.1f), 16f);

                if (Skills != null)
                {
                    Skills.AddXp(weaponSkill, CloneZConfig.XpMeleeHit);
                    Skills.AddXp(PlayerSkills.Skill.Strength, CloneZConfig.XpStrengthPerHit);
                    if (killed) Skills.AddXp(weaponSkill, CloneZConfig.XpMeleeKill);
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
            FindTargets(ShoveRange, ShoveArc, _targets);
            for (int i = 0; i < _targets.Count; i++)
            {
                IDamageable target = _targets[i];
                Vector3 dir = CloneZUtil.Flat(target.Transform.position - transform.position);
                var info = new DamageInfo(0f, DamageKind.Shove, target.Transform.position, dir, gameObject)
                {
                    Knockback = ShoveKnockback
                };
                target.TakeDamage(info);
            }
        }

        /// <summary>Living hostile things inside a cone in front of the aim direction: the dead, mutants, animals,
        /// hostile stalkers — and other players when PvP is on.</summary>
        private void FindTargets(float range, float arcDegrees, List<IDamageable> into)
        {
            into.Clear();
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
                Actor other = target.Transform != null ? target.Transform.GetComponent<Actor>() : null;
                if (other != null && Me != null && !CanHarm(other)) continue;

                Vector3 to = CloneZUtil.Flat(target.Transform.position - transform.position);
                if (to == Vector3.zero || Vector3.Dot(aim, to.normalized) >= cos)
                    into.Add(target);
            }
        }

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
            ReturnLoadedAmmo();          // hand the old gun's rounds back to the pack first
            EquippedFirearm = gun;
            LoadedAmmo = 0;
            LoadedAmmoId = null;
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
            Sfx.Play(SfxIds.Reload, transform.position + Vector3.up * 1.2f, 0.7f, 1f, 14f);
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
                CombatFX.Tracer(muzzle, r.End, build.Suppressed ? new Color(1f, 0.9f, 0.5f, 0.4f) : new Color(1f, 0.9f, 0.5f));
                if (r.Hit && r.Target == null && Random.value < 0.15f) Sfx.Play(SfxIds.Ricochet, r.End, 0.5f, Random.Range(0.9f, 1.2f), 25f);
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
            Sfx.Play(GunSound(gun, build), muzzle, build.Suppressed ? 0.45f : 1f, Random.Range(0.96f, 1.04f), build.Suppressed ? 30f : 140f);

            float kick = build.Recoil * PlayerModifiers.Current.Recoil * (AimingDownSights ? 0.75f : 1f);
            _bloom = Mathf.Min(6f, _bloom + kick * 0.6f);
            OnRecoil?.Invoke(kick);
            if (Inv != null) Inv.WearEquippedWeapon();   // the shot may cost a condition point; at 0 the gun breaks
        }

        private static string GunSound(ItemDef gun, WeaponBuild build)
        {
            if (build.Suppressed) return SfxIds.GunSuppressed;
            switch (Ballistics.CaliberOfGun(gun))
            {
                case "12g": return SfxIds.GunShotgun;
                case "545": return SfxIds.GunRifle;
                case "762r": return SfxIds.GunSniper;
                case "flare": return SfxIds.GunFlare;
                default: return SfxIds.GunPistol;
            }
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
            Sfx.Play(SfxIds.GunFlare, muzzle, 1f, 1f, 80f);
            NoiseSystem.Emit(transform.position, 40f);
            GameEvents.RaiseWorldEvent(transform.position, 160f, "flare");
            bool answered = ExtractionManager.Instance != null && ExtractionManager.Instance.CallHelicopter(transform.position);
            if (!answered) GameEvents.RaiseToast("The flare burns red over the Zone. No landing zone in range — everyone saw it, though.");
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
            // Rebuilt every time: the build follows the gun's mods, upgrades, wear and fouling, not only which stack is held.
            Build = Ballistics.Build(cur);
            _lastEquippedStack = cur;
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
