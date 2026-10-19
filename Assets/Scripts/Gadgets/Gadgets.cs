using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CloneZ
{
    public enum DroneKind { Fpv, Ground }

    /// <summary>
    /// Bodycam's drones: a flying FPV drone (fast, loud, can carry a grenade to drop) and a tracked ground drone
    /// (slow, quiet, long battery). Piloting one looks through its camera — a view that isn't your own eyes — while
    /// your body stands where you left it, defenceless. Anyone can hear it and shoot it down. Land it and pick it
    /// back up.
    /// </summary>
    public sealed class Drone : MonoBehaviour, IDamageable
    {
        public static readonly List<Drone> All = new List<Drone>();

        /// <summary>What kind of drone this is.</summary>
public DroneKind Kind { get; private set; }
        /// <summary>Current battery charge (0‑1).</summary>
public float Battery01 { get; private set; } = 1f;
        /// <summary>True if the drone carries a grenade (FPV only).</summary>
public bool HasGrenade { get; private set; }
        /// <summary>True when the drone has been destroyed.</summary>
public bool IsDead { get; private set; }
        /// <summary>Convenient access to this GameObject's Transform.</summary>
public Transform Transform => transform;
        /// <summary>The drone's camera, created on build.</summary>
public Camera Cam { get; private set; }
        public bool Piloted;
        public ulong Owner = ulong.MaxValue;
        /// <summary>True if this drone is a remote copy (network‑synced).</summary>
public bool Remote { get; private set; }
        /// <summary>Current speed magnitude of the drone.</summary>
public float Speed => _vel.magnitude;

        private float _hp = 60f, _yaw, _lift;
        private Vector3 _vel;
        private AudioSource _buzz;
        private Transform _payload;
        private readonly List<Transform> _rotors = new List<Transform>();
        private Vector3 _netTo;
        private float _netYaw;

        private float BatterySeconds => Kind == DroneKind.Fpv ? 150f : 420f;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() => All.Remove(this);

        /// <summary>Factory method that creates and configures a new Drone instance.</summary>
public static Drone Create(DroneKind kind, Vector3 at, float yaw, bool grenade, bool remote)
        {
            var go = new GameObject(kind == DroneKind.Fpv ? "FPV drone" : "Ground drone");
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            var d = go.AddComponent<Drone>();
            d.Kind = kind;
            d.HasGrenade = grenade;
            d.Remote = remote;
            d._yaw = yaw;
            d._netTo = at;
            d.Build();
            return d;
        }

        private void Build()
        {
            Color dark = new Color(0.12f, 0.13f, 0.12f);
            if (Kind == DroneKind.Fpv)
            {
                Part(new Vector3(0f, 0f, 0f), new Vector3(0.22f, 0.06f, 0.22f), dark);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 arm = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward * 0.2f;
                    Part(arm * 0.5f, new Vector3(0.03f, 0.02f, 0.22f), dark, Quaternion.Euler(0f, 45f + i * 90f, 0f));
                    Transform rotor = Part(arm + Vector3.up * 0.04f, new Vector3(0.2f, 0.005f, 0.02f), new Color(0.3f, 0.3f, 0.3f));
                    _rotors.Add(rotor);
                }
                if (HasGrenade) _payload = Part(new Vector3(0f, -0.07f, 0f), new Vector3(0.07f, 0.09f, 0.07f), new Color(0.22f, 0.27f, 0.18f));
                gameObject.AddComponent<SphereCollider>().radius = 0.22f;
            }
            else
            {
                Part(new Vector3(0f, 0.12f, 0f), new Vector3(0.36f, 0.12f, 0.5f), dark);
                Part(new Vector3(-0.2f, 0.07f, 0f), new Vector3(0.08f, 0.12f, 0.56f), new Color(0.07f, 0.07f, 0.07f));
                Part(new Vector3(0.2f, 0.07f, 0f), new Vector3(0.08f, 0.12f, 0.56f), new Color(0.07f, 0.07f, 0.07f));
                Part(new Vector3(0f, 0.24f, 0.18f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.05f, 0.05f, 0.08f));
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.13f, 0f);
                box.size = new Vector3(0.44f, 0.26f, 0.56f);
            }
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            _buzz = Sfx.Loop(SfxIds.DroneBuzz, transform, Kind == DroneKind.Fpv ? 0.8f : 0.35f, Kind == DroneKind.Fpv ? 45f : 15f, Kind == DroneKind.Fpv ? 1.2f : 0.6f);
            if (Remote) return;
            Cam = new GameObject("Drone camera").AddComponent<Camera>();
            Cam.transform.SetParent(transform, false);
            Cam.transform.localPosition = Kind == DroneKind.Fpv ? new Vector3(0f, 0.02f, 0.12f) : new Vector3(0f, 0.26f, 0.22f);
            Cam.transform.localRotation = Quaternion.Euler(Kind == DroneKind.Fpv ? 12f : 4f, 0f, 0f);
            Cam.fieldOfView = Kind == DroneKind.Fpv ? 105f : 80f;
            Cam.nearClipPlane = 0.03f;
            Cam.depth = 20f;
            Cam.enabled = false;
        }

        private Transform Part(Vector3 pos, Vector3 size, Color c, Quaternion? rot = null)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(transform, false);
            p.transform.localPosition = pos;
            p.transform.localRotation = rot ?? Quaternion.identity;
            p.transform.localScale = size;
            p.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(c, 0.4f);
            return p.transform;
        }

        /// <summary>One frame of piloting: move (x strafe/turn, y forward), lift (FPV only), yaw from the mouse.</summary>
        public void Fly(Vector2 move, float lift, float yawDelta, float dt)
        {
            if (IsDead) return;
            Battery01 = Mathf.Max(0f, Battery01 - dt / BatterySeconds);
            if (Battery01 <= 0f) { move = Vector2.zero; lift = Kind == DroneKind.Fpv ? -1f : 0f; }
            if (Kind == DroneKind.Fpv)
            {
                _yaw += yawDelta;
                Vector3 wish = Quaternion.Euler(0f, _yaw, 0f) * new Vector3(move.x, 0f, move.y) * 11f;
                _vel = Vector3.MoveTowards(_vel, wish, 18f * dt);
                _lift = Mathf.MoveTowards(_lift, lift * 4f, 10f * dt);
                Vector3 next = transform.position + (_vel + Vector3.up * _lift) * dt;
                next.y = Mathf.Clamp(next.y, 0.15f, 60f);
                if (Physics.SphereCast(transform.position, 0.2f, (next - transform.position).normalized, out RaycastHit hit, (next - transform.position).magnitude, ~(1 << CloneZConfig.IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                {
                    if (_vel.magnitude > 8f) { Crash(true); return; }
                    next = hit.point + hit.normal * 0.25f;
                    _vel = Vector3.zero;
                }
                transform.SetPositionAndRotation(next, Quaternion.Euler(move.y * 18f, _yaw, -move.x * 18f));
                foreach (Transform r in _rotors) r.Rotate(0f, 3000f * dt, 0f, Space.Self);
                if (_buzz != null) _buzz.pitch = 1.1f + _vel.magnitude / 22f;
            }
            else
            {
                _yaw += move.x * 70f * dt;
                float speed = move.y * 2.6f;
                Vector3 next = transform.position + Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward * speed * dt;
                if (Physics.SphereCast(transform.position + Vector3.up * 0.15f, 0.15f, (next - transform.position).normalized, out _, Mathf.Abs(speed * dt) + 0.05f, ~(1 << CloneZConfig.IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                    next = transform.position;
                if (GroundProbe.Below(next + Vector3.up, 3f, transform, out float groundY)) next.y = groundY;
                transform.SetPositionAndRotation(next, Quaternion.Euler(0f, _yaw, 0f));
                _vel = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward * speed;
            }
            if (Time.frameCount % 20 == 0) NoiseSystem.Emit(transform.position, Kind == DroneKind.Fpv ? 22f : 5f);
        }

        /// <summary>Lets the grenade go (FPV): it falls with the drone's speed.</summary>
        public bool DropGrenade(GameObject pilot)
        {
            if (!HasGrenade || IsDead) return false;
            HasGrenade = false;
            Grenade.Spawn(transform.position + Vector3.down * 0.15f, _vel, pilot);
            if (_payload != null) Destroy(_payload.gameObject);
            return true;
        }

        /// <summary>Handle incoming damage, delegating to network or local processing.</summary>
public void TakeDamage(in DamageInfo info)
        {
            if (IsDead) return;
            if (Remote) { DroneNet.Hit(Owner, info.AmountHp); return; }
            Hurt(info.AmountHp);
        }

        /// <summary>Apply hit‑points loss, potentially triggering a crash.</summary>
public void Hurt(float hp)
        {
            _hp -= hp;
            if (_hp <= 0f) Crash(false);
        }

        /// <summary>Down it goes: a spark and a clatter (a loaded FPV drone goes off).</summary>
        public void Crash(bool impact)
        {
            if (IsDead) return;
            IsDead = true;
            Sfx.Play(SfxIds.HitArmor, transform.position, 1f, 0.7f, 30f);
            if (HasGrenade && !Remote) NetExplosions.Detonate(transform.position, Explosives.GrenadeRadius, Explosives.GrenadeDamage, null, "drone");
            if (_buzz != null) { Sfx.Stop(_buzz); _buzz = null; }
            if (!Remote) DroneNet.Gone();
            Destroy(gameObject, 0.1f);
        }

        /// <summary>A remote drone's update from its pilot.</summary>
        public void Push(Vector3 pos, float yaw)
        {
            _netTo = pos;
            _netYaw = yaw;
        }

        private void Update()
        {
            if (Remote)
            {
                transform.position = Vector3.Lerp(transform.position, _netTo, Time.deltaTime * 12f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, _netYaw, 0f), Time.deltaTime * 12f);
                foreach (Transform r in _rotors) r.Rotate(0f, 3000f * Time.deltaTime, 0f, Space.Self);
                return;
            }
            if (Piloted || IsDead || Kind != DroneKind.Fpv) return;
            // Unpiloted, an FPV drone settles to the ground.
            Vector3 p = transform.position;
            if (p.y > 0.16f)
            {
                p.y = Mathf.MoveTowards(p.y, 0.15f, 2f * Time.deltaTime);
                transform.position = p;
            }
        }

        private void OnDestroy()
        {
            if (_buzz != null) Sfx.Stop(_buzz);
        }
    }

    /// <summary>A landed drone, waiting to be picked up.</summary>
    public sealed class DronePickup : Interactable
    {
        private Drone _d;
        private void Awake() => _d = GetComponent<Drone>();
        /// <summary>Human‑readable name of the drone when picked up.</summary>
public override string DisplayName => _d != null && _d.Kind == DroneKind.Fpv ? "FPV drone" : "Ground drone";
        /// <summary>How far the player can reach to interact with the drone.</summary>
public override float Reach => 2f;
        /// <summary>Whether the drone can be interacted with (not piloted, alive, and local).</summary>
public override bool IsAvailable => isActiveAndEnabled && _d != null && !_d.Piloted && !_d.IsDead && !_d.Remote;

        /// <summary>Populate the interaction menu for the picked‑up drone.</summary>
public override void GetActions(InteractContext ctx, List<InteractAction> into)
        {
            into.Add(new InteractAction("Pick up the drone", c =>
            {
                ItemDef def = ItemDatabase.Get(_d.Kind == DroneKind.Fpv ? "drone_fpv" : "drone_ground");
                if (def != null) c.Inventory.Inventory.Add(def, 1, enforceWeight: false);
                if (_d.HasGrenade) { ItemDef g = ItemDatabase.Get("grenade"); if (g != null) c.Inventory.Inventory.Add(g, 1, enforceWeight: false); }
                DroneNet.Gone();
                Destroy(_d.gameObject);
            }));
            into.Add(new InteractAction("Fly it again", c => DronePilot.Take(c.Stats, _d)).DisabledIf(_d.Battery01 <= 0.02f, "The battery's flat"));
        }
    }

    /// <summary>
    /// Flying (or driving) a drone: the screen is the drone's camera; your body stands still. FPV: WASD fly, mouse
    /// turns, Space climbs, Ctrl sinks, left mouse drops the grenade. Ground: W/S drive, A/D turn. E lands and hands
    /// the view back; getting hurt snaps you out of it.
    /// </summary>
    public sealed class DronePilot : MonoBehaviour
    {
        public static DronePilot Local { get; private set; }
        public Drone Drone { get; private set; }

        private PlayerController _ctrl;
        private PlayerCombat _combat;
        private PlayerInteractor _interactor;
        private PlayerStats _stats;
        private Camera _main;
        private CursorLockMode _lock;
        private bool _visible;
        private float _since;

        public static void Launch(PlayerInventory inv, DroneKind kind)
        {
            if (inv == null || HubRules.SafeZone || Local != null) return;
            string id = kind == DroneKind.Fpv ? "drone_fpv" : "drone_ground";
            if (inv.Consume(id, 1) <= 0) return;
            bool grenade = kind == DroneKind.Fpv && inv.Consume("grenade", 1) > 0;
            Transform t = inv.transform;
            Vector3 at = t.position + t.forward * 0.8f + Vector3.up * (kind == DroneKind.Fpv ? 1.2f : 0.05f);
            Drone d = Drone.Create(kind, at, t.eulerAngles.y, grenade, false);
            d.Owner = NetSession.NM != null ? NetSession.NM.LocalClientId : 0;
            d.gameObject.AddComponent<DronePickup>();
            Take(inv.GetComponent<PlayerStats>(), d);
        }

        public static void Take(PlayerStats who, Drone d)
        {
            if (who == null || d == null || Local != null) return;
            var p = who.gameObject.AddComponent<DronePilot>();
            p.Begin(d);
        }

        private void Begin(Drone d)
        {
            Local = this;
            Drone = d;
            d.Piloted = true;
            _since = Time.time;
            _stats = GetComponent<PlayerStats>();
            _ctrl = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _interactor = GetComponent<PlayerInteractor>();
            if (_ctrl != null) _ctrl.enabled = false;
            if (_combat != null) _combat.enabled = false;
            if (_interactor != null) _interactor.enabled = false;
            if (_stats != null) _stats.OnDamaged += Snap;
            _main = Camera.main;
            if (_main != null) _main.enabled = false;
            d.Cam.enabled = true;
            _lock = Cursor.lockState;
            _visible = Cursor.visible;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (UnityEngine.Object.FindAnyObjectByType<HudDrone>() == null) HudDrone.Create();
        }

        private void Snap() { GameEvents.RaiseToast("You're hit — you drop the controller."); End(); }

        private void Update()
        {
            if (Drone == null || Drone.IsDead) { End(); return; }
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null) return;
            bool blocked = UIScreen.Current != null;
            var move = new Vector2((kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f), (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
            float lift = (kb.spaceKey.isPressed ? 1f : 0f) - (kb.leftCtrlKey.isPressed ? 1f : 0f);
            float yaw = mouse != null ? mouse.delta.ReadValue().x * 0.12f * GameSettings.Current.MouseSensitivity : 0f;
            if (blocked) { move = Vector2.zero; lift = 0f; yaw = 0f; }
            Drone.Fly(move, lift, yaw, Time.deltaTime);
            DroneNet.Send(Drone);
            if (!blocked && mouse != null && mouse.leftButton.wasPressedThisFrame && Drone.DropGrenade(gameObject)) GameEvents.RaiseToast("Grenade away.");
            if (!blocked && Time.time - _since > 0.4f && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) End();
        }

        public void End()
        {
            if (Drone != null) Drone.Piloted = false;
            if (Drone != null && Drone.Cam != null) Drone.Cam.enabled = false;
            if (_main != null) _main.enabled = true;
            if (_ctrl != null) _ctrl.enabled = true;
            if (_combat != null && !GameSession.IsMonsterRun) _combat.enabled = true;
            if (_interactor != null) _interactor.enabled = true;
            if (_stats != null) _stats.OnDamaged -= Snap;
            Cursor.lockState = _lock;
            Cursor.visible = _visible;
            if (Local == this) Local = null;
            Drone = null;
            Destroy(this);
        }

        private void OnDestroy()
        {
            if (Local == this) Local = null;
            if (_main != null && !_main.enabled) _main.enabled = true;
        }
    }

    /// <summary>The drone's on-screen display: battery, height, speed, payload, the way back.</summary>
    public sealed class HudDrone : MonoBehaviour
    {
        private Text _text;
        private Image _frame;

        public static HudDrone Create()
        {
            RectTransform root = UIKit.Rect("Drone OSD", HudLayer.Root);
            UIKit.Stretch(root);
            var h = root.gameObject.AddComponent<HudDrone>();
            h._frame = UIKit.Panel(root, "Vignette", new Color(0f, 0f, 0f, 0f));
            UIKit.Stretch(h._frame.rectTransform);
            h._frame.raycastTarget = false;
            h._text = UIKit.Label(root, "", 18, new Color(0.75f, 1f, 0.75f), TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Place(h._text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(700f, 160f));
            return h;
        }

        private void LateUpdate()
        {
            DronePilot p = DronePilot.Local;
            bool on = p != null && p.Drone != null;
            _text.text = !on ? "" : (p.Drone.Kind == DroneKind.Fpv ? "FPV" : "UGV") + "  BAT " + Mathf.RoundToInt(p.Drone.Battery01 * 100f) + "%"
                + "   ALT " + p.Drone.transform.position.y.ToString("0.0") + " m   SPD " + Mathf.RoundToInt(p.Drone.Speed * 3.6f) + " km/h"
                + "\nDIST " + (Actor.LocalPlayer != null ? Mathf.RoundToInt(Vector3.Distance(Actor.LocalPlayer.transform.position, p.Drone.transform.position)) : 0) + " m"
                + (p.Drone.HasGrenade ? "   PAYLOAD ARMED [LMB]" : "") + "\n[E] land and hand back";
            _frame.color = on ? new Color(0f, 0.05f, 0f, 0.12f) : new Color(0f, 0f, 0f, 0f);
        }
    }

    /// <summary>Drones on the wire: the pilot's drone position (15 Hz) for everyone else's copy; shots on a copy go
    /// to its pilot.</summary>
    public static class DroneNet
    {
        private static readonly Dictionary<ulong, Drone> s_Remote = new Dictionary<ulong, Drone>();
        private static float s_SendAt;

        internal static void Register()
        {
            NetSession.Register("cz.drone", OnDrone);
            NetSession.Register("cz.dronegone", OnGone);
            NetSocial.Peer("drone.hit", (from, m) =>
            {
                float hp = m.F();
                DronePilot p = DronePilot.Local;
                if (p != null && p.Drone != null) p.Drone.Hurt(hp);
                else foreach (Drone d in Drone.All) if (d != null && !d.Remote) { d.Hurt(hp); break; }
            });
        }

        public static void Send(Drone d)
        {
            if (!NetSession.Active || Time.time < s_SendAt) return;
            s_SendAt = Time.time + 1f / 15f;
            using (var o = new NetOut(48))
            {
                o.UL(NetSession.NM.LocalClientId).By((byte)d.Kind).V(d.transform.position).F(d.transform.eulerAngles.y).B(d.HasGrenade);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.drone", o, NetworkDelivery.UnreliableSequenced);
                else NetSession.ToServer("cz.drone", o, NetworkDelivery.UnreliableSequenced);
            }
        }

        private static void OnDrone(ulong sender, NetIn m)
        {
            ulong owner = m.UL();
            var kind = (DroneKind)m.By();
            Vector3 pos = m.V();
            float yaw = m.F();
            bool grenade = m.B();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
            {
                owner = sender;
                using (var o = new NetOut(48)) { o.UL(owner).By((byte)kind).V(pos).F(yaw).B(grenade); NetSession.ToClients("cz.drone", o, NetworkDelivery.UnreliableSequenced, sender); }
            }
            if (owner == NetSession.NM.LocalClientId) return;
            if (!s_Remote.TryGetValue(owner, out Drone d) || d == null)
            {
                d = Drone.Create(kind, pos, yaw, grenade, true);
                d.Owner = owner;
                s_Remote[owner] = d;
            }
            d.Push(pos, yaw);
        }

        public static void Gone()
        {
            if (!NetSession.Active) return;
            using (var o = new NetOut(16))
            {
                o.UL(NetSession.NM.LocalClientId);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.dronegone", o);
                else NetSession.ToServer("cz.dronegone", o);
            }
        }

        private static void OnGone(ulong sender, NetIn m)
        {
            ulong owner = m.UL();
            if (NetSession.NM.IsServer && sender != NetSession.NM.LocalClientId)
            {
                owner = sender;
                using (var o = new NetOut(16)) { o.UL(owner); NetSession.ToClients("cz.dronegone", o, NetworkDelivery.ReliableSequenced, sender); }
            }
            if (s_Remote.TryGetValue(owner, out Drone d) && d != null) Object.Destroy(d.gameObject);
            s_Remote.Remove(owner);
        }

        /// <summary>A shot hit someone's drone (this machine has a copy): tell its pilot.</summary>
        public static void Hit(ulong owner, float hp)
        {
            if (owner == ulong.MaxValue) return;
            NetSocial.SendPeer(owner, "drone.hit", o => o.F(hp));
        }
    }

    /// <summary>
    /// The field tablet (Bodycam): the menus live on a cracked screen in your hands — your kit and its weight, the
    /// gun in your hands (its numbers, its parts: fit what you carry, strip what you found), saved loadouts (refilled
    /// from the stash in the bar), the map, and the drones.
    /// </summary>
    public sealed class TabletScreen : UIScreen
    {
        private static int s_Tab;
        private static readonly string[] Tabs = { "Kit", "Gun", "Loadouts", "Map", "Drones" };

        protected override string Title => "Field tablet";
        protected override Vector2 WindowSize => new Vector2(1280f, 800f);

        protected override void Build()
        {
            Image bezel = UIKit.Panel(Window, "Bezel", new Color(0.05f, 0.05f, 0.06f, 1f));
            UIKit.Stretch(bezel.rectTransform, -14f, -14f, -14f, -14f);
            bezel.transform.SetAsFirstSibling();
            RectTransform tabs = HubUI.Row(Body, 44f, new Color(1f, 1f, 1f, 0.04f));
            ((RectTransform)tabs).anchorMin = new Vector2(0f, 1f);
            for (int i = 0; i < Tabs.Length; i++)
            {
                int t = i;
                HubUI.Btn(tabs, Tabs[i], () =>
                {
                    Sfx.PlayUI(SfxIds.UiClick);
                    if (t == 3) { Close(); UIScreen.Show<ZoneMapScreen>(); return; }
                    s_Tab = t;
                    Rebuild();
                }, 150f, 34f, s_Tab == i ? UIKit.Accent : UIKit.ButtonColor);
            }
            RectTransform page = HubUI.Area(Body, 0f, 0f, 1f, 0.92f);
            switch (s_Tab)
            {
                case 1: GunPage(page); break;
                case 2: LoadoutPage(page); break;
                case 4: DronePage(page); break;
                default: KitPage(page); break;
            }
        }

        private static PlayerInventory Inv => HubKit.Carried;

        private void KitPage(RectTransform page)
        {
            RectTransform list = HubUI.Scroll(page);
            PlayerInventory inv = Inv;
            if (inv == null) return;
            PlayerClothing clothing = HubKit.Clothing;
            HubUI.Heading(list, "Worn", 20);
            if (clothing != null)
                foreach (ClothingSlot slot in System.Enum.GetValues(typeof(ClothingSlot)))
                {
                    ItemDef d = clothing.GetWorn(slot);
                    if (d == null) continue;
                    RectTransform r = HubUI.Row(list, 30f, new Color(1f, 1f, 1f, 0.03f));
                    HubUI.Cell(r, slot.ToString(), 120f, 15, UIKit.TextDim);
                    HubUI.Cell(r, d.DisplayName, -1f, 16);
                    HubUI.Cell(r, Mathf.RoundToInt(clothing.GetCondition01(slot) * 100f) + "%", 70f, 15, UIKit.TextDim, TextAnchor.MiddleRight);
                }
            HubUI.Heading(list, "Carried — " + inv.Inventory.CurrentWeight.ToString("0.0") + " / " + inv.Inventory.MaxWeight.ToString("0") + " kg", 20);
            int n = 0;
            foreach (ItemStack s in inv.Inventory.Stacks) if (s != null && s.Def != null) HubUI.ItemRow(list, s, null, n++);
        }

        private void GunPage(RectTransform page)
        {
            RectTransform col = HubUI.Scroll(page);
            PlayerInventory inv = Inv;
            ItemStack gun = inv != null ? inv.EquippedStack : null;
            if (gun == null || gun.Def == null || !gun.Def.IsFirearm)
            {
                HubUI.Para(col, "No gun in your hands. Take one out to see its build.", 17, UIKit.TextDim);
                return;
            }
            WeaponBuild b = Ballistics.Build(gun);
            HubUI.Heading(col, gun.Def.DisplayName, 24);
            HubUI.Para(col, "Condition " + (gun.Def.MaxCondition > 0 ? Mathf.RoundToInt(100f * gun.Condition / gun.Def.MaxCondition) + "%" : "—") + "   fouling " + Mathf.RoundToInt(gun.DirtLevel() * 100f) + "%   magazine " + b.MagSize
                + "\nSpread " + b.Spread.ToString("0.00") + "   recoil " + b.Recoil.ToString("0.00") + "   ergonomics " + b.Ergonomics.ToString("0") + "   range " + b.Range.ToString("0") + " m"
                + (b.Zoom > 1f ? "   optic " + b.Zoom.ToString("0.#") + "x" : "") + (b.Suppressed ? "   suppressed" : "") + (b.Laser ? "   laser" : "") + (b.Light ? "   light" : "")
                + "   jam " + Mathf.RoundToInt(b.JamChance * 100f) + "%", 16);
            HubUI.Heading(col, "Fitted", 19);
            if (gun.Extra == null || gun.Extra.Mods.Count == 0) HubUI.Para(col, "Nothing fitted.", 15, UIKit.TextDim);
            else
            {
                foreach (string id in gun.Extra.Mods)
                {
                    ModStats m = Ballistics.Mod(id);
                    ItemDef d = ItemDatabase.Get(id);
                    RectTransform r = HubUI.Row(col, 32f, new Color(1f, 1f, 1f, 0.03f));
                    HubUI.Cell(r, m != null ? m.Slot.ToString() : "", 140f, 15, UIKit.TextDim);
                    HubUI.Cell(r, d != null ? d.DisplayName : id, -1f, 16);
                }
                RectTransform strip = HubUI.Row(col, 40f);
                HubUI.Btn(strip, "Strip every part off", () => { int n = WeaponItems.Strip(inv.Inventory, gun); GameEvents.RaiseToast(n + " part(s) into your pack."); Rebuild(); }, 240f, 34f);
            }
            HubUI.Heading(col, "Parts you carry that fit", 19);
            int shown = 0;
            foreach (ItemStack s in new List<ItemStack>(inv.Inventory.Stacks))
            {
                ModStats m = s != null && s.Def != null ? Ballistics.Mod(s.Def.Id) : null;
                if (m == null || !Ballistics.ModFits(gun.Def, m)) continue;
                RectTransform r = HubUI.Row(col, 34f, new Color(1f, 1f, 1f, 0.03f));
                HubUI.Cell(r, m.Slot.ToString(), 140f, 15, UIKit.TextDim);
                HubUI.Cell(r, s.Def.DisplayName, -1f, 16);
                ItemStack pick = s;
                HubUI.Btn(r, "Fit", () => { WeaponItems.AttachTo(inv.Inventory, gun, pick, inv); Rebuild(); }, 90f, 28f);
                shown++;
            }
            if (shown == 0) HubUI.Para(col, "None. Strip parts off the guns you find (Metro Exodus) or buy them in the bar.", 15, UIKit.TextDim);
        }

        private void LoadoutPage(RectTransform page)
        {
            RectTransform col = HubUI.Scroll(page);
            CharacterProfile c = ProfileStore.ActiveCharacter;
            PlayerInventory inv = Inv;
            if (c == null || inv == null) return;
            HubUI.Para(col, GameSession.IsHub ? "Save what you're wearing and carrying as a loadout; load one to refill your kit from the stash." :
                "Loadouts can be saved anywhere, but only refilled from the stash in the bar.", 15, UIKit.TextDim);
            RectTransform save = HubUI.Row(col, 40f);
            HubUI.Btn(save, "Save current kit as loadout " + (c.Loadouts.Count + 1), () => { SaveLoadout(c, inv); Rebuild(); }, 340f, 34f, null, c.Loadouts.Count < 6);
            for (int i = 0; i < c.Loadouts.Count; i++)
            {
                LoadoutPreset p = c.Loadouts[i];
                RectTransform r = HubUI.Row(col, 44f, i % 2 == 0 ? UIKit.RowColor : UIKit.RowAlt);
                HubUI.Cell(r, p.Name, 220f, 17);
                HubUI.Cell(r, p.Worn.Count + " worn, " + p.Carried.Count + " carried", -1f, 15, UIKit.TextDim);
                int idx = i;
                HubUI.Btn(r, "Load", () => { string msg = ApplyLoadout(c, c.Loadouts[idx]); SetStatus(msg); Rebuild(); }, 100f, 32f, UIKit.Good, GameSession.IsHub);
                HubUI.Btn(r, "Delete", () => { c.Loadouts.RemoveAt(idx); ProfileStore.Save(); Rebuild(); }, 100f, 32f);
            }
        }

        private static void SaveLoadout(CharacterProfile c, PlayerInventory inv)
        {
            var p = new LoadoutPreset { Name = "Loadout " + (c.Loadouts.Count + 1) };
            PlayerClothing clothing = HubKit.Clothing;
            if (clothing != null)
                foreach (ClothingSlot slot in System.Enum.GetValues(typeof(ClothingSlot)))
                {
                    ItemDef d = clothing.GetWorn(slot);
                    if (d != null && slot != ClothingSlot.Pouch) p.Worn.Add(d.Id);
                }
            foreach (ItemStack s in inv.Inventory.Stacks)
                if (s != null && s.Def != null && s.Def.Id != Money.Id && !HubKit.InPouch(s)) p.Carried.Add(new StackSave { Id = s.Def.Id, Count = s.Count });
            c.Loadouts.Add(p);
            ProfileStore.Save();
            GameEvents.RaiseToast(p.Name + " saved.");
        }

        // Pulls what's missing out of the stash: worn pieces into empty slots, carried items up to the saved counts.
        private static string ApplyLoadout(CharacterProfile c, LoadoutPreset p)
        {
            Inventory stash = ProfileStore.StashInventory;
            PlayerInventory inv = Inv;
            PlayerClothing clothing = HubKit.Clothing;
            if (stash == null || inv == null) return "No stash here.";
            int moved = 0, short_ = 0;
            foreach (string id in p.Worn)
            {
                ItemDef d = ItemDatabase.Get(id);
                if (d == null || clothing == null || clothing.GetWorn(d.ClothingSlot) == d) continue;
                if (clothing.IsWearing(d.ClothingSlot)) continue;
                ItemStack s = stash.Stacks.Find(x => x.Def == d);
                if (s == null) { short_++; continue; }
                if (HubKit.ReplaceWorn(d.ClothingSlot, d, s.Condition, s.TimesRepaired, s.Extra)) { stash.Remove(s, 1); moved++; }
            }
            foreach (StackSave want in p.Carried)
            {
                ItemDef d = ItemDatabase.Get(want.Id);
                if (d == null) continue;
                int need = want.Count - inv.Count(want.Id);
                while (need > 0)
                {
                    ItemStack s = stash.Stacks.Find(x => x.Def == d);
                    if (s == null) { short_++; break; }
                    int n = Mathf.Min(need, s.Count);
                    int added = inv.Inventory.Add(d, n, enforceWeight: false, spawnHours: d.Perishable ? s.SpawnHours : float.NaN, condition: d.IsDamageable ? s.Condition : -1, timesRepaired: s.TimesRepaired, extra: s.Extra);
                    if (added <= 0) break;
                    stash.Remove(s, added);
                    need -= added;
                    moved += added;
                }
            }
            ProfileStore.CommitStash();
            HubKit.AfterChange();
            return moved + " item(s) from the stash" + (short_ > 0 ? "; " + short_ + " missing" : "") + ".";
        }

        private void DronePage(RectTransform page)
        {
            RectTransform col = HubUI.Stack(page, 10f, 16);
            PlayerInventory inv = Inv;
            if (inv == null) return;
            HubUI.Para(col, "A drone's camera is a view that isn't your own eyes. Your body stays where it is — and stays vulnerable.", 15, UIKit.TextDim);
            bool raid = GameSession.IsExtraction;
            RectTransform r1 = HubUI.Row(col, 46f);
            HubUI.Cell(r1, "FPV drone" + (inv.Has("grenade") ? " (will carry a grenade)" : ""), -1f, 17);
            HubUI.Btn(r1, "Launch", () => { Close(); DronePilot.Launch(Inv, DroneKind.Fpv); }, 140f, 36f, UIKit.Accent, raid && inv.Has("drone_fpv"));
            RectTransform r2 = HubUI.Row(col, 46f);
            HubUI.Cell(r2, "Ground drone", -1f, 17);
            HubUI.Btn(r2, "Deploy", () => { Close(); DronePilot.Launch(Inv, DroneKind.Ground); }, 140f, 36f, UIKit.Accent, raid && inv.Has("drone_ground"));
            if (!raid) HubUI.Para(col, "Drones fly in the Zone, not in the bar.", 15, UIKit.Warn);
        }
    }

    /// <summary>Hooks the drones and the tablet to their items.</summary>
    public sealed class GadgetsModule : IGameModule
    {
        public int Order => 96;

        public void Init()
        {
            ItemActions.RegisterUse("drone_fpv", (inv, s) => { DronePilot.Launch(inv, DroneKind.Fpv); return true; });
            ItemActions.RegisterUse("drone_ground", (inv, s) => { DronePilot.Launch(inv, DroneKind.Ground); return true; });
            ItemActions.RegisterUse("tablet", (inv, s) => { UIScreen.Show<TabletScreen>(); return true; });
            HubScreens.Register(HubScreens.Tablet, arg => UIScreen.Show<TabletScreen>());
            NetModuleHooks.All.Add(s => DroneNet.Register());
        }
    }
}
