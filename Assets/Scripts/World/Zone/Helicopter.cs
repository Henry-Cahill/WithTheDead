using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>Builds the helicopter: fuselage, cockpit glass, tail boom and fin, skids, two spinning rotors, a
    /// searchlight and navigation lights. About fourteen metres nose to tail, all generated.</summary>
    public static class HelicopterModel
    {
        public static Transform Build(Transform parent)
        {
            var root = new GameObject("Helicopter").transform;
            if (parent != null) root.SetParent(parent, false);
            Color hull = new Color(0.24f, 0.28f, 0.21f);
            var m = new RaidMesh();
            m.Ellipsoid(new Vector3(0f, 1.7f, 0.5f), new Vector3(2.5f, 2.3f, 7.6f), DecorPalette.Metal(hull), 12, 7);
            // thin worn‑metal strip along the bottom of the fuselage
            m.Box(new Vector3(0f, 0.45f, 0f), new Vector3(2.5f, 0.05f, 7.6f), DecorPalette.Metal(hull * 0.5f));
            // side‑panel seams – thin worn metal strips
            m.Box(new Vector3(2.48f, 1.7f, 0f), new Vector3(0.02f, 1.2f, 6.0f), DecorPalette.Metal(hull * 0.6f));
            m.Box(new Vector3(-2.48f, 1.7f, 0f), new Vector3(0.02f, 1.2f, 6.0f), DecorPalette.Metal(hull * 0.6f));
            // Add subtle rivets along the side‑panel seams for period‑appropriate visual detail
            // We'll place 4 rivets spaced evenly along each strip
            const int rivetCount = 4;
            float rivetRadius = 0.012f;
            Color rivetColor = new Color(0.5f, 0.5f, 0.5f); // dull metal
            // Positive‑x seam
            for (int i = 0; i < rivetCount; i++)
            {
                float t = (i + 0.5f) / rivetCount; // center within segment
                float y = 1.7f; // same height as seam
                float z = Mathf.Lerp(-3.0f, 3.0f, t); // seam length is 6.0f
                m.Cylinder(new Vector3(2.48f, y, z), rivetRadius, rivetRadius, 6, DecorPalette.Matte(rivetColor));
            }
            // Negative‑x seam
            for (int i = 0; i < rivetCount; i++)
            {
                float t = (i + 0.5f) / rivetCount;
                float y = 1.7f;
                float z = Mathf.Lerp(-3.0f, 3.0f, t);
                m.Cylinder(new Vector3(-2.48f, y, z), rivetRadius, rivetRadius, 6, DecorPalette.Matte(rivetColor));
            }
            // Radio antenna: thin metal rod and dish perched on top of the fuselage
            m.Cylinder(new Vector3(0f, 2.5f, 0f), 0.02f, 0.8f, 8, DecorPalette.Metal(hull));
            m.Box(new Vector3(0f, 2.9f, 0f), new Vector3(0.2f, 0.05f, 0.2f), DecorPalette.Metal(hull * 0.8f));
            m.Ellipsoid(new Vector3(0f, 1.95f, 3.4f), new Vector3(1.9f, 1.4f, 2f), DecorPalette.Glass(new Color(0.25f, 0.32f, 0.36f)), 10, 6);
            m.CylinderX(new Vector3(0f, 2.1f, -5.2f), 0.42f, 6.4f, 10, DecorPalette.Metal(hull), 90f);
            m.Box(new Vector3(0f, 3f, -8.1f), new Vector3(0.12f, 1.9f, 1.1f), DecorPalette.Metal(hull));
            // Small worn‑metal exhaust pipe on the tail boom for period‑appropriate grit
            m.Cylinder(new Vector3(0f, 2.45f, -8.55f), 0.06f, 0.06f, 6, DecorPalette.Metal(hull * 0.8f));
            m.Box(new Vector3(0f, 2.9f, 0.3f), new Vector3(1.2f, 0.5f, 2.4f), DecorPalette.Metal(hull * 0.85f));
            for (int s = -1; s <= 1; s += 2)
            {
                m.Rod(new Vector3(s * 1.1f, 0.05f, 2.4f), new Vector3(s * 1.1f, 0.05f, -2.4f), 0.06f, DecorPalette.Metal(new Color(0.15f, 0.15f, 0.15f)), 6);
                m.Rod(new Vector3(s * 1.1f, 0.05f, 1.6f), new Vector3(s * 0.7f, 0.9f, 1.4f), 0.05f, DecorPalette.Metal(new Color(0.15f, 0.15f, 0.15f)), 5);
                m.Rod(new Vector3(s * 1.1f, 0.05f, -1.6f), new Vector3(s * 0.7f, 0.9f, -1.4f), 0.05f, DecorPalette.Metal(new Color(0.15f, 0.15f, 0.15f)), 5);
            }
            // Small worn‑metal caps on skid rod ends
            m.Cylinder(new Vector3( 1.1f, 0.07f,  2.4f), 0.10f, 0.04f, 6, DecorPalette.Metal(hull * 0.7f));
            m.Cylinder(new Vector3( 1.1f, 0.07f, -2.4f), 0.10f, 0.04f, 6, DecorPalette.Metal(hull * 0.7f));
            m.Cylinder(new Vector3(-1.1f, 0.07f,  2.4f), 0.10f, 0.04f, 6, DecorPalette.Metal(hull * 0.7f));
            m.Cylinder(new Vector3(-1.1f, 0.07f, -2.4f), 0.10f, 0.04f, 6, DecorPalette.Metal(hull * 0.7f));
            Emit(m, "Body", root, Vector3.zero);

            var main = new GameObject("MainRotor").transform;
            main.SetParent(root, false);
            main.localPosition = new Vector3(0f, 3.25f, 0.3f);
            var rm = new RaidMesh();
            for (int b = 0; b < 4; b++)
            {
                Quaternion q = Quaternion.Euler(0f, b * 90f, 0f);
                rm.Box(q * new Vector3(0f, 0f, 3.6f), q * new Vector3(0.3f, 0.04f, 7.2f), DecorPalette.Metal(new Color(0.1f, 0.1f, 0.1f)));
            }
            rm.Cylinder(new Vector3(0f, -0.1f, 0f), 0.25f, 0.25f, 8, DecorPalette.Metal(new Color(0.2f, 0.2f, 0.2f)));
            // hub rim – thin steel disc giving the hub a worn‑metal look
            rm.Cylinder(new Vector3(0f, -0.12f, 0f), 0.33f, 0.02f, 8, DecorPalette.Metal(new Color(0.15f, 0.15f, 0.15f)));
            Emit(rm, "Blades", main, Vector3.zero);

            var tail = new GameObject("TailRotor").transform;
            tail.SetParent(root, false);
            tail.localPosition = new Vector3(0.2f, 3.2f, -8.2f);
            var tm = new RaidMesh();
            tm.Box(Vector3.zero, new Vector3(0.04f, 2.2f, 0.2f), DecorPalette.Metal(new Color(0.1f, 0.1f, 0.1f)));
            tm.Box(Vector3.zero, new Vector3(0.04f, 0.2f, 2.2f), DecorPalette.Metal(new Color(0.1f, 0.1f, 0.1f)));
            Emit(tm, "TailBlades", tail, Vector3.zero);

            var spot = new GameObject("Searchlight").transform;
            spot.SetParent(root, false);
            spot.localPosition = new Vector3(0f, 0.6f, 3.6f);
            spot.localRotation = Quaternion.Euler(55f, 0f, 0f);
            var l = spot.gameObject.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 28f;
            l.range = 80f;
            l.intensity = 6f;
            l.color = new Color(1f, 0.97f, 0.9f);
            l.shadows = LightShadows.None;
            ZoneKit.Beam(spot, 45f, 28f, new Color(1f, 0.97f, 0.9f, 0.1f));

            NavLight(root, new Vector3(-1.3f, 1.8f, 0.5f), Color.red);
            NavLight(root, new Vector3(1.3f, 1.8f, 0.5f), Color.green);
            NavLight(root, new Vector3(0f, 3.9f, -8.1f), Color.white);
            return root;
        }

        private static void NavLight(Transform root, Vector3 local, Color c)
        {
            var go = new GameObject("NavLight");
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = 4f;
            l.intensity = 1.5f;
            l.shadows = LightShadows.None;
        }

        private static void Emit(RaidMesh m, string name, Transform parent, Vector3 worldOrigin)
        {
            GameObject go = m.Emit(name, parent, worldOrigin, ShadowCastingMode.On);
            if (go != null) go.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Flies a helicopter: along a line (a flyover), to a landing zone and down (a called extraction, an insertion),
    /// and back up and away. Its rotors are loud — while it's low, every few seconds the noise rolls out over the
    /// map and the dead turn towards it.
    /// </summary>
    public sealed class HelicopterFlight : MonoBehaviour
    {
        public bool Landed { get; private set; }
        public bool Busy { get; private set; }
        public event Action OnLanded;
        public event Action OnDeparted;

        private Transform _main, _tail;
        private AudioSource _rotor;
        private float _spin = 1f;



        public static HelicopterFlight Spawn(Vector3 position, float yaw)
        {
            Transform root = HelicopterModel.Build(null);
            root.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return root.gameObject.AddComponent<HelicopterFlight>();
        }

        private void Awake()
        {
            _main = transform.Find("MainRotor");
            _tail = transform.Find("TailRotor");
        }

        private void Start()
        {
            _rotor = Sfx.Loop(SfxIds.HeliRotor, transform, 1f, 160f);
            StartCoroutine(NoiseLoop());
        }

        private IEnumerator NoiseLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1.5f);
                if (transform.position.y < 70f && _spin > 0.5f)
                {
                    Vector3 ground = new Vector3(transform.position.x, 0f, transform.position.z);
                    NoiseSystem.Emit(ground, 55f);
                    GameEvents.RaiseWorldEvent(ground, 55f, "heli");
                }
            }
        }

        private void OnDestroy() { if (_rotor != null) Sfx.Stop(_rotor); }

        private void Update()
        {
            float rate = Mathf.Lerp(80f, 1500f, _spin);
            if (_main != null) _main.Rotate(0f, rate * Time.deltaTime, 0f, Space.Self);
            if (_tail != null) _tail.Rotate(rate * 1.7f * Time.deltaTime, 0f, 0f, Space.Self);
            if (_rotor != null) _rotor.pitch = Mathf.Lerp(0.6f, 1f, _spin);



        }

        public void FlyPath(Vector3 from, Vector3 to, float altitude, float seconds) => StartCoroutine(Path(from, to, altitude, seconds));

        public void FlyToAndLand(Vector3 landingPoint, float approachSeconds) => StartCoroutine(Land(landingPoint, approachSeconds));

        public void TakeOffAndLeave(Vector3 exitDirection) => StartCoroutine(Leave(exitDirection));

        /// <summary>Puts the helicopter down at once, rotors idling (an insertion's touchdown).</summary>
        public void SetLanded(Vector3 at)
        {
            transform.position = at;
            Landed = true;
            _spin = 0.7f;
        }

        private IEnumerator Path(Vector3 from, Vector3 to, float altitude, float seconds)
        {
            Busy = true;
            from.y = to.y = altitude;
            Face(to - from);
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, t / seconds) + Vector3.up * Mathf.Sin(t * 0.7f) * 1.5f;
                yield return null;
            }
            Busy = false;
            OnDeparted?.Invoke();
            Destroy(gameObject);
        }

        private IEnumerator Land(Vector3 pad, float seconds)
        {
            Busy = true;
            Landed = false;
            Vector3 start = transform.position;
            Vector3 hover = pad + Vector3.up * 28f;
            Face(hover - start);
            float cruise = seconds * 0.75f;
            for (float t = 0f; t < cruise; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / cruise);
                transform.position = Vector3.Lerp(start, hover, k);
                yield return null;
            }
            float descend = seconds - cruise;
            for (float t = 0f; t < descend; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(hover, pad, Mathf.SmoothStep(0f, 1f, t / descend));
                yield return null;
            }
            transform.position = pad;
            _spin = 0.75f;
            Landed = true;
            Busy = false;
            OnLanded?.Invoke();
        }

        private IEnumerator Leave(Vector3 dir)
        {
            Busy = true;
            Landed = false;
            _spin = 1f;
            Vector3 start = transform.position;
            for (float t = 0f; t < 4f; t += Time.deltaTime)
            {
                transform.position = start + Vector3.up * Mathf.SmoothStep(0f, 40f, t / 4f);
                yield return null;
            }
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();
            Face(dir);
            Vector3 from = transform.position, to = from + dir * 420f + Vector3.up * 30f;
            for (float t = 0f; t < 12f; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, t / 12f);
                yield return null;
            }
            Busy = false;
            OnDeparted?.Invoke();
            Destroy(gameObject);
        }

        private void Face(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    /// <summary>
    /// Gray Zone Warfare's helicopter insertion and Beautiful Light's flare-called extraction share one machine: the
    /// survivor is set down at the chosen landing zone, and a flare fired at a landing-zone exit brings it back — loud
    /// enough that everything nearby comes to see.
    /// </summary>
    public sealed class HelicopterService : IGameModule
    {
        public int Order => 45;
        private static readonly System.Collections.Generic.Dictionary<RaidExit, HelicopterFlight> s_Called =
            new System.Collections.Generic.Dictionary<RaidExit, HelicopterFlight>();

        public void Init()
        {
            RaidWorldRules.InsertionPresenter = Insert;
            ExtractionManager.HelicopterCalled += OnCalled;
            ExtractionManager.HelicopterLeft += OnLeft;
            GameEvents.RaidEnded += (o, e) => s_Called.Clear();
        }

        private static void Insert(PlayerStats player, Vector3 at)
        {
            Vector3 side = player.transform.right;
            Vector3 pad = at + side * 7f;
            pad.y = 0f;
            HelicopterFlight heli = HelicopterFlight.Spawn(pad, player.transform.eulerAngles.y);
            heli.SetLanded(pad);
            heli.StartCoroutine(LiftOff(heli, at));
            GameEvents.RaiseSubtitle("Pilot", "This is as far as I go. Good hunting — fire a flare at a landing zone if you want a ride out.");
        }

        private static IEnumerator LiftOff(HelicopterFlight heli, Vector3 from)
        {
            yield return new WaitForSeconds(3f);
            if (heli != null) heli.TakeOffAndLeave(-from);
        }

        private static void OnCalled(RaidExit exit)
        {
            Vector3 outward = exit.Position;
            outward.y = 0f;
            Vector3 start = exit.Position + (outward.sqrMagnitude > 1f ? outward.normalized : Vector3.forward) * 380f + Vector3.up * 70f;
            HelicopterFlight heli = HelicopterFlight.Spawn(start, 0f);
            heli.FlyToAndLand(new Vector3(exit.Position.x, 0f, exit.Position.z), Mathf.Max(10f, exit.HeliEta));
            s_Called[exit] = heli;
        }

        private static void OnLeft(RaidExit exit)
        {
            if (!s_Called.TryGetValue(exit, out HelicopterFlight heli) || heli == null) return;
            heli.TakeOffAndLeave(exit.Position);
            s_Called.Remove(exit);
        }
    }

    /// <summary>
    /// A world event (Project Zomboid's helicopter): once in a while a helicopter crosses the map low with its light
    /// sweeping, and the noise drags the dead along its path. Scheduled by the server; <see cref="Started"/> lets the
    /// netcode play the same flight on every client.
    /// </summary>
    public sealed class HeliFlyoverDirector : MonoBehaviour
    {
        public static HeliFlyoverDirector Instance { get; private set; }
        public static event Action<Vector3, Vector3> Started;

        private float _at = -1f;

        public static void BeginRaid(GameObject host, ZoneDef zone)
        {
            var d = host.GetComponent<HeliFlyoverDirector>();
            if (d == null) d = host.AddComponent<HeliFlyoverDirector>();
            Instance = d;
            if (NetAuthority.IsServer && UnityEngine.Random.value < SandboxSettings.Current.HelicopterEventChance)
                d._at = Time.time + UnityEngine.Random.Range(120f, 600f);
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Update()
        {
            if (_at < 0f || Time.time < _at) return;
            _at = -1f;
            Trigger();
        }

        /// <summary>Sends the helicopter over now (server; mirrored to clients through <see cref="Started"/>).</summary>
        public static void Trigger()
        {
            float half = CloneZConfig.ArenaHalfSize;
            Vector2 c = UnityEngine.Random.insideUnitCircle * half * 0.4f;
            float ang = UnityEngine.Random.value * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            Vector3 mid = new Vector3(c.x, 0f, c.y);
            Vector3 from = mid - dir * (half + 160f), to = mid + dir * (half + 160f);
            Play(from, to);
            Started?.Invoke(from, to);
        }

        /// <summary>Plays a flyover along a line (used directly by clients).</summary>
        public static void Play(Vector3 from, Vector3 to)
        {
            HelicopterFlight heli = HelicopterFlight.Spawn(from + Vector3.up * 35f, 0f);
            heli.FlyPath(from, to, 35f, 90f);
            GameEvents.RaiseToast("A helicopter. Low, and loud. Everything dead is turning to look.");
        }
    }
}
