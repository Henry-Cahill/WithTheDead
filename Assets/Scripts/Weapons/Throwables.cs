using Unity.Netcode;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Everything thrown from the pack, all on the grenade's arc: the frag and the pipe bomb (a fuse, then a blast:
    /// <see cref="Grenade"/>), the molotov (a bottle that bursts into a spreading fire where it lands: <see cref="MolotovBottle"/>,
    /// <see cref="FirePatch"/>) and the noise lures — firecrackers, a wind-up alarm clock and a radio on a kitchen timer — that
    /// call the dead to wherever they come down (<see cref="NoiseLure"/>). "Throw" on one in the pack throws it; the grenade key
    /// throws another of the last kind thrown (a frag until something else has been), else the first kind there is.
    /// In the isometric view a throw is a lob that comes down on the ground under the cursor, within <see cref="MaxReach"/>;
    /// through the eyes or over the shoulder it leaves along the view, lifted with the camera's pitch.
    /// </summary>
    public static class Throwables
    {
        public const string Grenade = "grenade", PipeBomb = "pipebomb", Molotov = "molotov",
            Firecrackers = "firecrackers", AlarmClock = "alarmclock", TimerRadio = "radio_timer";

        /// <summary>Every throwable, in the order the grenade key falls back through once the last kind thrown has run out.</summary>
        public static readonly string[] All = { Grenade, PipeBomb, Molotov, Firecrackers, AlarmClock, TimerRadio };

        /// <summary>The furthest a lob comes down, flat metres (about where the frag's one fixed throw used to land).</summary>
        public const float MaxReach = 16f;
        private const float MinReach = 1.5f, LobAngle = 38f, MaxSpeed = 17f, ViewSpeed = 13f;

        /// <summary>The kind the grenade key throws (the last kind thrown).</summary>
        public static string Readied { get; private set; } = Grenade;

        // Statics outlive a play session in the editor (domain reload is off).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void NewSession() => Readied = Grenade;

        /// <summary>True for an item that's thrown (one of <see cref="All"/>).</summary>
        public static bool Is(string id) => System.Array.IndexOf(All, id) >= 0;

        internal static void Register()
        {
            foreach (string id in All)
            {
                string kind = id;
                ItemActions.RegisterUse(kind, (inv, s) => { Throw(inv, kind); return true; });
            }
        }

        /// <summary>
        /// Throws one <paramref name="id"/> from the pack: toward <paramref name="at"/> when it's given (a lob that comes down
        /// there, within <see cref="MaxReach"/>), else where the survivor aims. It leaves the survivor's hand, or
        /// <paramref name="from"/> (a window it's tossed out of). False when there's none to throw, or nothing may be thrown
        /// here (the hub).
        /// </summary>
        public static bool Throw(PlayerInventory inv, string id, Vector3? at = null, Vector3? from = null)
        {
            if (inv == null || !Is(id) || HubRules.SafeZone) return false;
            if (inv.Consume(id, 1) <= 0)
            {
                GameEvents.RaiseToast("No " + Plural(id) + ".");
                return false;
            }
            Readied = id;
            Launch(inv, at, from, out Vector3 start, out Vector3 v);
            GameObject thrower = inv.gameObject;
            Vector3 hand = inv.transform.position + Vector3.up * 1.4f;
            switch (id)
            {
                case Grenade:
                    Sfx.Play(SfxIds.Grenade, hand, 0.8f, 1f, 12f);
                    CloneZ.Grenade.Spawn(start, v, thrower, Grenade);
                    return true;
                case PipeBomb:
                    Sfx.Play(SfxIds.Lighter, hand, 0.7f, 0.9f, 10f);
                    CloneZ.Grenade.Spawn(start, v, thrower, PipeBomb);
                    break;
                case Molotov:
                    Sfx.Play(SfxIds.Lighter, hand, 0.7f, 1f, 10f);
                    MolotovBottle.Spawn(start, v, thrower);
                    break;
                case Firecrackers:
                    Sfx.Play(SfxIds.Lighter, hand, 0.7f, 1.1f, 10f);
                    NoiseLure.Throw(id, start, v, thrower);
                    break;
                case AlarmClock:
                    Sfx.Play(SfxIds.Crank, hand, 0.35f, 2.2f, 8f);   // winding the key
                    NoiseLure.Throw(id, start, v, thrower);
                    break;
                default:
                    Sfx.Play(SfxIds.CarSwitch, hand, 0.5f, 1.3f, 8f);   // the timer's knob
                    NoiseLure.Throw(id, start, v, thrower);
                    break;
            }
            Sfx.Play(SfxIds.BoltThrow, hand, 0.55f, 0.85f, 12f);
            return true;
        }

        /// <summary>What the grenade key throws: another of the last kind thrown, else the first kind in the pack (in
        /// <see cref="All"/>'s order); with none of any, the last kind (there are none of it either).</summary>
        public static string NextFor(PlayerInventory inv)
        {
            if (inv == null || inv.Has(Readied)) return Readied;
            foreach (string id in All)
                if (inv.Has(id)) return id;
            return Readied;
        }

        /// <summary>The item's name as the pack shows it.</summary>
        public static string NameOf(string id)
        {
            ItemDef d = ItemDatabase.Get(id);
            return d != null ? d.DisplayName : id;
        }

        private static string Plural(string id)
        {
            switch (id)
            {
                case Grenade: return "grenades";
                case PipeBomb: return "pipe bombs";
                case Molotov: return "molotovs";
                case Firecrackers: return "firecrackers";
                case AlarmClock: return "alarm clocks";
                case TimerRadio: return "timer radios";
                default: return id;
            }
        }

        /// <summary>
        /// Where a throw leaves the hand (or <paramref name="from"/>) and how fast: the grenade's arc. A lob that comes down on
        /// <paramref name="at"/>, else in the isometric view on the ground under the cursor; through the eyes or over the
        /// shoulder, along the view, lifted with the camera's pitch.
        /// </summary>
        public static void Launch(PlayerInventory inv, Vector3? at, Vector3? from, out Vector3 start, out Vector3 velocity)
        {
            Transform t = inv.transform;
            PlayerController ctrl = inv.GetComponent<PlayerController>();
            Vector3 aim = at.HasValue ? at.Value - (from ?? t.position) : ctrl != null ? ctrl.AimDirection : t.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) aim = t.forward;
            aim.Normalize();
            start = from ?? t.position + Vector3.up * 1.6f + aim * 0.5f;
            if (at.HasValue || (!ViewState.MouseLook && ctrl != null))
            {
                velocity = Lob(start, at ?? ctrl.AimPoint, aim);
                return;
            }
            Camera cam = Camera.main;
            float lift = ViewState.MouseLook && cam != null ? Mathf.Clamp(cam.transform.forward.y + 0.35f, 0.1f, 0.9f) : 0.45f;
            velocity = (aim + Vector3.up * lift).normalized * ViewSpeed;
        }

        // A lob at LobAngle that comes down on `target` (its flat distance held within reach): the speed that lands it there.
        private static Vector3 Lob(Vector3 start, Vector3 target, Vector3 aim)
        {
            Vector3 flat = target - start;
            flat.y = 0f;
            float d = flat.magnitude;
            Vector3 dir = d > 0.05f ? flat / d : aim;
            d = Mathf.Clamp(d, MinReach, MaxReach);
            float h = Mathf.Clamp(target.y - start.y, -8f, 4f);
            float a = LobAngle * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            float g = Mathf.Max(0.1f, -Physics.gravity.y);
            float k = 2f * cos * cos * (d * Mathf.Tan(a) - h);
            float speed = k > 0.01f ? Mathf.Min(MaxSpeed, Mathf.Sqrt(g * d * d / k)) : ViewSpeed;
            return (dir * cos + Vector3.up * sin) * speed;
        }

        /// <summary>
        /// A thrown (or set-down) thing's body: on Ignore Raycast, drawn as the item's own baked model. In flight
        /// (<paramref name="physics"/>) it has a box round the model and a rigidbody, and its thrower's colliders let it pass;
        /// one that only lies there (another machine's copy) has neither, so it stops nothing.
        /// </summary>
        internal static GameObject Body(string id, Vector3 at, Vector3 velocity, GameObject thrower, float mass, bool physics)
        {
            var go = new GameObject("Thrown " + id);
            go.layer = CloneZConfig.IgnoreRaycastLayer;
            Vector3 heading = CloneZUtil.Flat(velocity);
            go.transform.SetPositionAndRotation(at, heading != Vector3.zero ? Quaternion.LookRotation(heading) : Quaternion.identity);
            ThrownLook look = go.AddComponent<ThrownLook>();
            look.Show(ItemDatabase.Get(id));
            if (!physics) return go;
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.Max(look.Size, Vector3.one * 0.05f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.02f;
            rb.angularDamping = 0.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Random.onUnitSphere * 7f;
            if (thrower != null)
                foreach (Collider c in thrower.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(box, c);
            return go;
        }
    }

    /// <summary>The look of something thrown or set down: the item's own baked model, centred on its object (held, so the mesh
    /// cache keeps it while it's out).</summary>
    public sealed class ThrownLook : MonoBehaviour
    {
        private ItemMeshBaker.Baked _baked;

        /// <summary>The model (null when there's nothing to draw).</summary>
        public Transform Model { get; private set; }
        /// <summary>The model's size, metres.</summary>
        public Vector3 Size { get; private set; } = Vector3.one * 0.12f;

        /// <summary>Draws <paramref name="def"/> here (nothing on a dedicated server, which draws nothing).</summary>
        public void Show(ItemDef def)
        {
            if (def == null || DedicatedServer.Requested) return;
            _baked = ItemMeshBaker.Get(def, ItemMeshBaker.Lod.Near);
            if (_baked == null || _baked.Mesh == null) return;
            ItemMeshBaker.Hold(_baked);
            var m = new GameObject("Model");
            m.layer = gameObject.layer;
            m.transform.SetParent(transform, false);
            m.transform.localPosition = -_baked.Bounds.center;
            m.AddComponent<MeshFilter>().sharedMesh = _baked.Mesh;
            m.AddComponent<MeshRenderer>().sharedMaterials = _baked.Materials;
            Model = m.transform;
            Size = _baked.Bounds.size;
        }

        private void OnDestroy() => ItemMeshBaker.Release(_baked);
    }

    /// <summary>
    /// Lures and fire go through the server, as blasts do (<see cref="NetExplosions"/>): it runs the dead, so it decides who
    /// hears a lure and who burns; everyone hears and sees them.
    /// </summary>
    public static class NetThrowables
    {
        // A client may only start them near where it stands.
        private const float Reach = 60f;

        internal static void Register()
        {
            NetSession.Register("cz.lure", OnLure);
            NetSession.Register("cz.lurefx", OnLureFx);
            NetSession.Register("cz.fire", OnFire);
            NetSession.Register("cz.firefx", OnFireFx);
        }

        /// <summary>A lure thrown on this machine has started sounding at <paramref name="at"/>: the server sounds its own
        /// copy there (the one that calls the dead), the other players hear one.</summary>
        public static void Sounding(string kind, Vector3 at, float seconds)
        {
            if (!GameSession.Networked || !NetSession.Active) return;
            using (var o = new NetOut(64))
            {
                o.S(kind).V(at).F(seconds);
                if (NetSession.NM.IsServer) NetSession.ToClients("cz.lurefx", o);
                else NetSession.ToServer("cz.lure", o);
            }
        }

        private static void OnLure(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            string kind = m.S();
            Vector3 at = m.V();
            float seconds = m.F();
            if (NoiseLure.Of(kind) == null || TooFar(sender, at)) return;
            NoiseLure.Remote(kind, at, seconds, true);
            using (var o = new NetOut(64)) { o.S(kind).V(at).F(seconds); NetSession.ToClients("cz.lurefx", o, NetworkDelivery.ReliableSequenced, sender); }
        }

        private static void OnLureFx(ulong sender, NetIn m)
        {
            string kind = m.S();
            Vector3 at = m.V();
            float seconds = m.F();
            if (GameSession.IsRaidWorld) NoiseLure.Remote(kind, at, seconds, false);
        }

        /// <summary>Sets a fire going at <paramref name="at"/> (a molotov burst there), started by <paramref name="source"/>.</summary>
        public static void Ignite(Vector3 at, GameObject source)
        {
            int seed = Random.Range(1, int.MaxValue);
            if (GameSession.Networked && NetSession.Active && !NetSession.NM.IsServer)
            {
                using (var o = new NetOut(32)) { o.V(at).I(seed); NetSession.ToServer("cz.fire", o); }
                return;
            }
            FirePatch.Ignite(at, source, true, seed);
            if (NetSession.Active) using (var o = new NetOut(32)) { o.V(at).I(seed); NetSession.ToClients("cz.firefx", o); }
        }

        private static void OnFire(ulong sender, NetIn m)
        {
            if (!NetSession.NM.IsServer) return;
            Vector3 at = m.V();
            int seed = m.I();
            if (TooFar(sender, at)) return;
            RemotePlayer p = NetPlayers.ProxyOf(sender);
            FirePatch.Ignite(at, p != null ? p.gameObject : null, true, seed);
            using (var o = new NetOut(32)) { o.V(at).I(seed); NetSession.ToClients("cz.firefx", o); }
        }

        private static void OnFireFx(ulong sender, NetIn m)
        {
            Vector3 at = m.V();
            int seed = m.I();
            if (GameSession.IsRaidWorld) FirePatch.Ignite(at, null, false, seed);
        }

        private static bool TooFar(ulong sender, Vector3 at)
        {
            RemotePlayer p = NetPlayers.ProxyOf(sender);
            return p != null && (p.transform.position - at).magnitude > Reach;
        }
    }

    /// <summary>Wires the throwables in: "Throw" on each in the pack, and their netcode.</summary>
    public sealed class ThrowablesModule : IGameModule
    {
        /// <summary>After the camps (which wire the explosives and the grenade key).</summary>
        public int Order => 95;

        public void Init()
        {
            Throwables.Register();
            NetModuleHooks.All.Add(s => NetThrowables.Register());
        }
    }
}
