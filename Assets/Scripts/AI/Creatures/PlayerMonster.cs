using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    /// <summary>The shapes a hunter can take, each with one trick (enemies as puzzles — now with a human brain).</summary>
    public static class HunterForms
    {
        public static readonly string[] Ids = { CreatureCatalog.Shade, CreatureCatalog.Hound, CreatureCatalog.Puppeteer };
        /// <summary>The form picked at the door for the next hunter run.</summary>
        public static string Chosen = CreatureCatalog.Shade;

        public static string Describe(string id)
        {
            switch (id)
            {
                case CreatureCatalog.Hound: return "Hound — the fastest thing in the Zone, with a nose for blood at 40 m. Right mouse: howl, and two more come out of the dark.";
                case CreatureCatalog.Puppeteer: return "Puppeteer — slow, and the dead walk where it points. Right mouse: a psi pulse that hurts every mind within 12 m and turns the nearby dead on the runners.";
                default: return "Shade — a smear in the air while it waits (still for a moment, it cloaks). Right mouse: pounce seven metres and strike.";
            }
        }
    }

    /// <summary>
    /// A hunter run (the doc's Beautiful Light: "some of the monsters are other players"): instead of a runner, you
    /// go in as one of the Zone's things — hostile to every runner and stalker, ignored by the dead and the beasts.
    /// Left mouse strikes; right mouse is the form's trick; the hunter's senses pick out the living through walls.
    /// It eats nothing, fears no emission, and is paid for every runner it takes (into the stash). It brings nothing
    /// home and its death costs the runner nothing.
    /// </summary>
    public sealed class PlayerMonster : MonoBehaviour, IStatModSource
    {
        public const int PayPerRunner = 1500, PayPerStalker = 300;
        /// <summary>Singleton reference to the current player monster instance.</summary>
        public static PlayerMonster Local { get; private set; }

        /// <summary>Reference to the creature definition that determines this monster's abilities and stats.</summary>
        public CreatureDef Def { get; private set; }
        /// <summary>How ready the strike action is, expressed as 0‑1 where 1 means ready.</summary>
        public float StrikeReady01 => Mathf.Clamp01(1f - (_strikeAt - Time.time) / Mathf.Max(0.1f, StrikeCooldown));
        /// <summary>How ready the ability action is, expressed as 0‑1 where 1 means ready.</summary>
        public float AbilityReady01 => Mathf.Clamp01(1f - (_abilityAt - Time.time) / Mathf.Max(0.1f, AbilityCooldown));
        /// <summary>Whether the monster is currently in an attack animation.</summary>
        public bool Attacking => Time.time < _attackUntil;
        /// <summary>Total money earned from hunting runners and stalkers.</summary>
        public int Earned { get; private set; }
        /// <summary>Current sense range based on the monster type.</summary>
        public float SenseRange => Def != null && Def.Id == CreatureCatalog.Hound ? 40f : 25f;

        private float StrikeCooldown => Def.AttackCooldown * 0.8f;
        private float AbilityCooldown => Def.Id == CreatureCatalog.Hound ? 75f : Def.Id == CreatureCatalog.Puppeteer ? 9f : 7f;

        private Actor _actor;
        private PlayerStats _stats;
        private CharacterController _cc;
        private CreatureBody _body;
        private float _strikeAt, _abilityAt, _attackUntil, _stillSince, _pounceUntil;
        private Vector3 _pounceDir, _lastPos;

        /// <summary>Turns the freshly built survivor into a hunter of <paramref name="form"/>.</summary>
        /// <summary>Creates and returns a PlayerMonster for the given player stats and selected form.</summary>
        public static PlayerMonster Become(PlayerStats p, string form)
        {
            var m = p.gameObject.AddComponent<PlayerMonster>();
            m.Begin(form);
            return m;
        }

        /// <summary>Initialises the monster, setting up its definition, body, and UI.</summary>
        private void Begin(string form)
        {
            Local = this;
            _actor = GetComponent<Actor>();
            _stats = GetComponent<PlayerStats>();
            _cc = GetComponent<CharacterController>();
            Def = CreatureCatalog.Get(form) ?? CreatureCatalog.Get(CreatureCatalog.Shade);
            _actor.Kind = ActorKind.PlayerMonster;
            _actor.Faction = Factions.Zone;
            _actor.DisplayName = Def.Name;
            _actor.Height = Def.Height;

            // The human body goes; the creature's comes.
            PlayerAvatar avatar = GetComponent<PlayerAvatar>();
            if (avatar != null && avatar.Rig != null)
                foreach (Renderer r in avatar.Rig.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var go = new GameObject("Hunter body");
            go.transform.SetParent(transform, false);
            _body = go.AddComponent<CreatureBody>();
            _body.Build(Def);
            _body.ExternalSpeed01 = 0f;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
            if (_body.Head != null) _actor.Head = _body.Head;

            PlayerCombat combat = GetComponent<PlayerCombat>();
            if (combat != null) combat.enabled = false;
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv != null) inv.EquipBareHands();
            _lastPos = transform.position;
            _stillSince = Time.time;
            GameEvents.ActorKilled += OnKilled;
            GameEvents.RaiseToast("You are a " + Def.Name + ". Hunt the runners — and leave by any exit when you've had enough.");
            if (UnityEngine.Object.FindAnyObjectByType<HudHunter>() == null) HudHunter.Create();
        }

        /// <summary>Handles the monster's death cleanup and event unsubscription.</summary>
        private void OnDestroy()
        {
            GameEvents.ActorKilled -= OnKilled;
            if (Local == this) Local = null;
        }

        /// <summary>Per‑frame update handling movement, sense, input, and actions.</summary>
        private void Update()
        {
            if (Def == null || _stats == null || _stats.IsDead) { if (_body != null && _stats != null && _stats.IsDead) _body.Die(-transform.forward); return; }
            float dt = Time.deltaTime;
            Vector3 v = dt > 0f ? (transform.position - _lastPos) / dt : Vector3.zero;
            v.y = 0f;
            _lastPos = transform.position;
            if (v.sqrMagnitude > 0.2f || Attacking) _stillSince = Time.time;
            _body.ExternalSpeed01 = Mathf.Clamp01(v.magnitude / Mathf.Max(1f, Def.RunSpeed));
            _body.ExternalAttack = Attacking;
            // The Shade cloaks once it has kept still a moment (and drops it to strike).
            _actor.Cloaked = Def.Id == CreatureCatalog.Shade && Time.time - _stillSince > 1.5f;
            _body.ExternalCloak = _actor.Cloaked;

            if (Time.time < _pounceUntil)
            {
                if (_cc != null && _cc.enabled) _cc.Move(_pounceDir * (7f / 0.35f) * dt + Vector3.down * 2f * dt);
                if (Time.time + dt >= _pounceUntil) Strike(1.3f);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || GameInput.Blocked) return;
            if (mouse.leftButton.wasPressedThisFrame && Time.time >= _strikeAt) Strike(1f);
            if (mouse.rightButton.wasPressedThisFrame && Time.time >= _abilityAt) Ability();
        }

        /// <summary>Direction the monster is facing, flattened to the horizontal plane.</summary>
        private Vector3 Facing
        {
            get
            {
                Vector3 f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
            }
        }

        // Claws (or teeth): everything hostile in a short arc in front.
        /// <summary>Performs a strike attack with optional damage multiplier.</summary>
        private void Strike(float mult)
        {
            _strikeAt = Time.time + StrikeCooldown;
            _attackUntil = Time.time + 0.35f;
            _stillSince = Time.time;
            Sfx.Play(Def.Id == CreatureCatalog.Hound ? SfxIds.MutantGrowl : SfxIds.MutantScreech, transform.position, 0.9f, Random.Range(0.8f, 0.95f), 25f);
            float reach = Def.AttackRange + 0.4f;
            Vector3 f = Facing;
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || a == _actor || !a.IsAlive || !_actor.IsHostileTo(a)) continue;
                Vector3 to = a.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > reach + 0.3f || Vector3.Angle(f, to) > 70f) continue;
                var info = new DamageInfo(Def.AttackDamage * 1.4f * mult, Def.AttackKind == DamageKind.Explosion ? DamageKind.Melee : Def.AttackKind, a.Center, to.normalized, gameObject)
                {
                    HpScale = true, BleedChance = Def.Bleed, FractureChance = Def.Fracture, Knockback = Def.Knockback, WeaponId = "claws_" + Def.Id,
                }.WithZone(BodyZones.RandomStrike());
                CombatResolver.Apply(a.Damageable, info, _actor);
            }
            NoiseSystem.Emit(transform.position, 10f);
        }

        /// <summary>Activates the monster's special ability based on its form.</summary>
        private void Ability()
        {
            _abilityAt = Time.time + AbilityCooldown;
            _stillSince = Time.time;
            switch (Def.Id)
            {
                case CreatureCatalog.Hound:
                    Sfx.Play(SfxIds.WolfHowl, transform.position, 1f, 0.7f, 120f);
                    NoiseSystem.Emit(transform.position, 60f);
                    HunterNet.Call(0, transform.position);
                    GameEvents.RaiseToast("You howl. Something answers.");
                    break;
                case CreatureCatalog.Puppeteer:
                    PsiPulse();
                    HunterNet.Call(1, transform.position);
                    break;
                default:
                    _pounceDir = Facing;
                    _pounceUntil = Time.time + 0.35f;
                    _attackUntil = _pounceUntil + 0.3f;
                    Sfx.Play(SfxIds.MutantScreech, transform.position, 1f, 0.7f, 30f);
                    break;
            }
        }

        // Every mind within 12 m (that the pulse can reach) is hurt and shaken.
        /// <summary>Emits a psi pulse that hurts and shakes enemies within range.</summary>
        private void PsiPulse()
        {
            Sfx.Play(SfxIds.PsiWhisper, transform.position, 1f, 0.8f, 40f);
            for (int i = Actor.All.Count - 1; i >= 0; i--)
            {
                Actor a = Actor.All[i];
                if (a == null || a == _actor || !a.IsAlive || !a.IsHumanoid || a.Kind == ActorKind.Zombie || !_actor.IsHostileTo(a)) continue;
                Vector3 to = a.Center - _actor.EyePosition;
                if (to.magnitude > 12f) continue;
                var info = new DamageInfo(18f, DamageKind.Psi, a.Center, to.normalized, gameObject) { HpScale = true, WeaponId = "psi_" + Def.Id };
                CombatResolver.Apply(a.Damageable, info, _actor);
            }
        }

        /// <summary>Handles the monster killing another actor, paying the player and showing a toast.</summary>
        private void OnKilled(KillInfo k)
        {
            if (k.Killer != _actor || k.Victim == null) return;
            int pay = k.Victim.Kind == ActorKind.RemotePlayer ? PayPerRunner : k.Victim.Kind == ActorKind.Human || k.Victim.Kind == ActorKind.Companion ? PayPerStalker : 0;
            if (pay <= 0) return;
            Earned += pay;
            Money.Give(pay);
            Progression.AddXp(pay / 8f, "a hunt");
            GameEvents.RaiseToast("+" + Money.Format(pay) + " for " + k.Victim.DisplayName + ".");
        }

        /// <summary>Applies the monster's stat modifiers to the provided object.</summary>
        public void ApplyMods(StatMods m)
        {
            if (Def == null) return;
            m.HungerRate = 0f;
            m.ThirstRate = 0f;
            m.FatigueRate = 0f;
            m.ColdResist = 1f;
            m.InfectionResist = 1f;
            m.HealRate *= 3f;
            m.BleedRate *= 0.3f;
            m.NightVision = Mathf.Max(m.NightVision, 1f);
            m.Resist.Radiation += 0.85f;
            m.Resist.Psi += 0.85f;
            m.Resist.Chemical += 0.6f;
            switch (Def.Id)
            {
                case CreatureCatalog.Hound: m.MoveSpeed *= 1.5f; m.SprintSpeed *= 1.45f; m.Resist.Bullet += 0.25f; m.NoiseMult *= 0.6f; break;
                case CreatureCatalog.Puppeteer: m.MoveSpeed *= 0.75f; m.SprintSpeed *= 0.6f; m.Resist.Bullet += 0.45f; m.Resist.Explosion += 0.3f; break;
                default: m.MoveSpeed *= 1.2f; m.SprintSpeed *= 1.3f; m.Resist.Bullet += 0.55f; m.Resist.Explosion += 0.3f; m.NoiseMult *= 0.4f; break;
            }
        }
    }

    /// <summary>What a hunter asks of the Zone's AI (which lives on the server): a howl calls hounds; a Puppeteer's
    /// pulse turns the dead nearby on the nearest runner.</summary>
    public static class HunterNet
    {
        private static bool s_Registered;

        public static void Call(byte op, Vector3 at)
        {
            if (GameSession.Networked && !NetAuthority.IsServer)
            {
                using (var o = new NetOut(32)) { o.By(op).V(at); NetSession.ToServer("cz.hunter", o); }
                return;
            }
            Run(op, at, Actor.LocalPlayer);
        }

        internal static void Register()
        {
            NetSession.Register("cz.hunter", (s, m) =>
            {
                byte op = m.By();
                Vector3 at = m.V();
                if (!NetSession.NM.IsServer) return;
                RemotePlayer p = NetPlayers.ProxyOf(s);
                if (p == null || !p.Actor.IsPlayerControlled || p.Actor.Kind != ActorKind.PlayerMonster) return;
                if ((p.transform.position - at).magnitude > 6f) at = p.transform.position;
                Run(op, at, p.Actor);
            });
        }

        private static void Run(byte op, Vector3 at, Actor hunter)
        {
            if (op == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector3 p = at + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(10f, 16f);
                    if (UnityEngine.AI.NavMesh.SamplePosition(p, out UnityEngine.AI.NavMeshHit hit, 6f, UnityEngine.AI.NavMesh.AllAreas)) Creature.Spawn(CreatureCatalog.Hound, hit.position);
                }
                return;
            }
            // The dead within 25 m go for the runner nearest the hunter.
            Actor prey = Actor.Nearest(at, 40f, a => (a.Kind == ActorKind.Player || a.Kind == ActorKind.RemotePlayer || a.Kind == ActorKind.Human) && hunter != null && hunter.IsHostileTo(a));
            if (prey == null) return;
            foreach (ZombieAI z in UnityEngine.Object.FindObjectsByType<ZombieAI>())
                if (z != null && (z.transform.position - at).sqrMagnitude < 25f * 25f) z.Command(prey);
        }
    }

    /// <summary>The hunter's HUD: claws and trick cooldowns, the pay so far, and the living picked out through walls.</summary>
    public sealed class HudHunter : MonoBehaviour
    {
        private Text _text;
        private readonly List<RectTransform> _marks = new List<RectTransform>();
        private readonly List<Image> _markImages = new List<Image>();
        private RectTransform _markRoot;
        private Camera _cam;

        public static HudHunter Create()
        {
            RectTransform slot = HudLayer.Anchor("Hunter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(520f, 60f));
            var h = slot.gameObject.AddComponent<HudHunter>();
            h._text = UIKit.Label(slot, "", 16, new Color(0.95f, 0.55f, 0.5f), TextAnchor.MiddleCenter);
            UIKit.Stretch(h._text.rectTransform);
            h._markRoot = UIKit.Rect("Senses", HudLayer.Root);
            UIKit.Stretch(h._markRoot);
            return h;
        }

        private void LateUpdate()
        {
            PlayerMonster m = PlayerMonster.Local;
            if (m == null || m.Def == null) { _text.text = ""; Hide(0); return; }
            // Update the info line only when needed
            if (_lastDefName != m.Def.Name || _lastBar != Bar(m.StrikeReady01) || _lastAbility != Bar(m.AbilityReady01) || _lastEarned != Money.Format(m.Earned))
            {
                _text.text = m.Def.Name + "   claws " + Bar(m.StrikeReady01) + "   trick " + Bar(m.AbilityReady01) + "   earned " + Money.Format(m.Earned);
                _lastDefName = m.Def.Name;
                _lastBar = Bar(m.StrikeReady01);
                _lastAbility = Bar(m.AbilityReady01);
                _lastEarned = Money.Format(m.Earned);
            }
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            int n = 0;
            Actor me = Actor.LocalPlayer;
            float r2 = m.SenseRange * m.SenseRange;
            for (int i = 0; i < Actor.All.Count && n < 24; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a == me || !a.IsAlive || !a.IsHumanoid || a.Kind == ActorKind.Zombie || me == null || !me.IsHostileTo(a)) continue;
                if ((a.transform.position - me.transform.position).sqrMagnitude > r2) continue;
                Vector3 sp = _cam.WorldToScreenPoint(a.Center);
                if (sp.z <= 0f) continue;
                RectTransform mark = Mark(n++);
                mark.position = sp;
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 6f + i);
                // Use cached Image reference
                _markImages[n-1].color = new Color(0.9f, 0.1f, 0.1f, 0.35f + 0.4f * pulse);
            }
            Hide(n);
        }
        // cache last displayed values to avoid rebuilding every frame
        private string _lastDefName = "";
        private string _lastBar = "";
        private string _lastAbility = "";
        private string _lastEarned = "";

        private static string Bar(float f) => f >= 1f ? "ready" : Mathf.RoundToInt(f * 100f) + "%";

        private RectTransform Mark(int i)
        {
            while (_marks.Count <= i)
            {
                Image img = UIKit.Panel(_markRoot, "Heartbeat", new Color(0.9f, 0.1f, 0.1f, 0.6f));
                img.rectTransform.sizeDelta = new Vector2(18f, 18f);
                img.raycastTarget = false;
                _marks.Add(img.rectTransform);
                _markImages.Add(img);
            }
            _marks[i].gameObject.SetActive(true);
            _markImages[i].gameObject.SetActive(true);
            return _marks[i];
        }

        private void Hide(int from)
        {
            for (int i = from; i < _marks.Count; i++) if (_marks[i].gameObject.activeSelf) _marks[i].gameObject.SetActive(false);
        }
    }

    /// <summary>Turns the survivor into the chosen hunter on a hunter run.</summary>
    public sealed class HunterModule : IGameModule
    {
        public int Order => 93;

        public void Init()
        {
            WorldHooks.PlayerBuilt += p =>
            {
                if (p != null && GameSession.IsMonsterRun && p.GetComponent<PlayerMonster>() == null) PlayerMonster.Become(p, HunterForms.Chosen);
            };
            NetModuleHooks.All.Add(s => HunterNet.Register());
        }
    }
}
