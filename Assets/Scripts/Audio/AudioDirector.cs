using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CloneZ
{
    /// <summary>Installs the synthesiser as the game's sound backend and wires the sounds no single system owns.</summary>
    public sealed class AudioModule : IGameModule
    {
        public int Order => -50;

        public void Init()
        {
            var synth = new SfxSynth();
            Sfx.Backend = synth;
            _ = GameSettings.Current;   // loading applies the saved master volume
#if UNITY_EDITOR || DEBUG
            foreach (FieldInfo f in typeof(SfxIds).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.GetValue(null) is string id && !synth.Has(id))
                    Debug.LogWarning("[CloneZ] No sound recipe for '" + id + "'.");
#endif
            WorldHooks.PlayerBuilt += p =>
            {
                if (p == null) return;
                if (p.GetComponent<ListenerRig>() == null) p.gameObject.AddComponent<ListenerRig>();
                var controller = p.GetComponent<PlayerController>();
                Transform t = p.transform;
                if (controller != null) controller.Footstep += (sprint, sneak) => FootstepAudio.Play(t.position, sprint, sneak);
                var gear = p.GetComponent<PlayerGear>();
                if (gear != null) gear.Coughed += () => Sfx.Play(SfxIds.Cough, t.position + Vector3.up * 1.6f, 0.7f, Random.Range(0.9f, 1.1f), 18f);
            };
            CombatResolver.ShotResolved += (req, r) =>
            {
                if (r.Hit && (r.TargetActor != null || r.Target != null))
                    Sfx.Play(SfxIds.HitFlesh, r.End, 0.6f, Random.Range(0.9f, 1.1f), 18f);
            };
            GameEvents.ItemLooted += (stack, source) => Sfx.PlayUI(SfxIds.Pickup, 0.45f, Random.Range(0.92f, 1.08f));
            AudioDirector.Ensure();
        }
    }

    /// <summary>
    /// The survivor hears from their own head, not from the camera: in the isometric view the camera hangs high
    /// above, and a listener there would make every sound distant and every direction wrong. The ears ride the head
    /// bone; they turn with the camera's heading so left on screen is left in the speakers.
    /// </summary>
    public sealed class ListenerRig : MonoBehaviour
    {
        private Transform _ears;
        private AudioListener _listener;
        private Transform _head;
        private PlayerAvatar _avatar;
        private Camera _mutedCam;

        /// <summary>Current world position of the ears attached to the player's head.</summary>
        public static Vector3 Position { get; private set; }
        /// <summary>Whether the ListenerRig is currently active in the scene.</summary>
        public static bool Active { get; private set; }

        private void Start()
        {
            _avatar = GetComponent<PlayerAvatar>();
            var go = new GameObject("Ears");
            _ears = go.transform;
            _listener = go.AddComponent<AudioListener>();
            Active = true;
        }

        private void LateUpdate()
        {
            if (_ears == null) return;
            if (_head == null && _avatar != null && _avatar.Rig != null && _avatar.Rig.Bones != null)
                _head = _avatar.Rig.Bone(HumanBone.Head);

            Camera cam = Camera.main;
            if (cam != _mutedCam)
            {
                if (_mutedCam != null) SetListener(_mutedCam, true);
                _mutedCam = cam;
                if (cam != null) SetListener(cam, false);
            }
            if (!_listener.enabled) _listener.enabled = true;

            Vector3 pos = _head != null ? _head.position : transform.position + Vector3.up * 1.65f;
            Quaternion rot = transform.rotation;
            if (cam != null) rot = ViewState.FirstPersonLike ? cam.transform.rotation : Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            _ears.SetPositionAndRotation(pos, rot);
            Position = pos;
        }

        private static void SetListener(Camera cam, bool on)
        {
            var l = cam.GetComponent<AudioListener>();
            if (l != null) l.enabled = on;
        }

        private void OnDestroy()
        {
            Active = false;
            if (_mutedCam != null) SetListener(_mutedCam, true);
            if (_ears != null) Destroy(_ears.gameObject);
        }
    }

    /// <summary>Footsteps that sound like what is underfoot: snow, boards, concrete and asphalt, grass and earth.</summary>
    public static class FootstepAudio
    {
        /// <summary>Determines the surface type under the given foot position for appropriate footstep sounds.</summary>
        public static string SurfaceAt(Vector3 feet)
        {
            if (Physics.Raycast(feet + Vector3.up * 0.4f, Vector3.down, out RaycastHit hit, 1.4f, 1, QueryTriggerInteraction.Ignore))
            {
                string s = SurfaceName(hit.collider);
                if (s != null)
                {
                    if (Has(s, "snow")) return SfxIds.StepSnow;
                    if (Has(s, "asphalt") || Has(s, "concrete") || Has(s, "road") || Has(s, "pavement") || Has(s, "tile") || Has(s, "gravel") || Has(s, "slab") || Has(s, "stair"))
                        return SfxIds.StepConcrete;
                    if (Has(s, "wood") || Has(s, "plank") || Has(s, "floor") || Has(s, "deck") || Has(s, "porch")) return SfxIds.StepWood;
                }
            }
            if (Shelter.Count > 0 && Shelter.IsSheltered(feet + Vector3.up)) return SfxIds.StepWood;
            if (GameSession.IsHub) return SfxIds.StepWood;
            if (Climate.SnowFraction > 0.5f) return SfxIds.StepSnow;
            return SfxIds.StepGrass;
        }

        public static void Play(Vector3 feet, bool sprinting, bool sneaking, float loudness = 1f)
        {
            float vol = (sneaking ? 0.2f : sprinting ? 0.7f : 0.45f) * loudness;
            float range = sneaking ? 6f : sprinting ? 24f : 14f;
            Sfx.Play(SurfaceAt(feet), feet + Vector3.up * 0.05f, vol, Random.Range(0.9f, 1.1f), range);
        }

        private static string SurfaceName(Collider c)
        {
            var r = c.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                Texture tex = r.sharedMaterial.mainTexture;
                if (tex != null && !string.IsNullOrEmpty(tex.name)) return (tex.name + " " + c.name).ToLowerInvariant();
                return (r.sharedMaterial.name + " " + c.name).ToLowerInvariant();
            }
            return c.name.ToLowerInvariant();
        }

        private static bool Has(string s, string part) => s.IndexOf(part, System.StringComparison.Ordinal) >= 0;
    }

    /// <summary>
    /// The sound of the world around the survivor: a bed of wind that rises with the weather and drops indoors,
    /// birds in the trees by day, the murmur and glasses of the bar in the hub; the dead groaning nearby (more when
    /// they hunt); wolves far off at night; the rasp of breath inside a gas mask.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private static AudioDirector s_Instance;

        private AudioSource _wind, _forest, _bar, _mask;
        private float _groanTimer, _animalTimer = 40f;
        private readonly Dictionary<Actor, float> _nextGroan = new Dictionary<Actor, float>();
        private readonly List<Actor> _near = new List<Actor>();
        private Actor _localActor;
        private PlayerStats _localStats;
        private PlayerGear _localGear;

        /// <summary>Ensures a single AudioDirector instance exists in the scene.</summary>
        public static void Ensure()

        {
            if (s_Instance != null) return;
            var go = new GameObject("[AudioDirector]");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<AudioDirector>();
        }

        private AudioSource Bed(string id)
        {
            AudioSource s = Sfx.Loop(id, transform, 0f, 10f);
            if (s != null) s.spatialBlend = 0f;
            return s;
        }

        private void Start()
        {
            _wind = Bed(SfxIds.WindAmbience);
            _forest = Bed(SfxIds.ForestAmbience);
            _bar = Bed(SfxIds.BarAmbience);
            _mask = Bed(SfxIds.BreathMask);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Actor local = Actor.LocalPlayer;
            if (local != _localActor)
            {
                _localActor = local;
                _localStats = local != null ? local.GetComponent<PlayerStats>() : null;
                _localGear = local != null ? local.GetComponent<PlayerGear>() : null;
            }
            PlayerStats me = _localStats;
            Vector3 ears = ListenerRig.Active ? ListenerRig.Position : (me != null ? me.transform.position : Vector3.zero);
            bool paused = Time.timeScale <= 0f;

            float wind = 0.16f, forest = 0f, bar = 0f, mask = 0f;
            if (GameSession.IsHub && me != null)
            {
                wind = 0.04f;
                bar = 0.4f;
            }
            else if (me != null)
            {
                WeatherSystem w = WeatherSystem.Instance;
                if (w != null)
                {
                    switch (w.Current)
                    {
                        case Weather.Cloudy: wind = 0.22f; break;
                        case Weather.Overcast: wind = 0.28f; break;
                        case Weather.Fog: wind = 0.12f; break;
                        case Weather.Rain: wind = 0.34f; break;
                        case Weather.Storm: wind = 0.55f; break;
                    }
                }
                bool night = TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight;
                float trees = ZoneForest.Density(ears);
                forest = night ? 0f : Mathf.Clamp01(trees * 1.4f) * 0.4f + (GameSession.IsExtraction ? 0.05f : 0f);
                bool indoors = Shelter.Count > 0 && Shelter.IsSheltered(ears);
                if (indoors) { wind *= 0.3f; forest *= 0.25f; }

                if (_localGear != null && _localGear.HasGasMask && !me.IsDead) mask = 0.32f;
            }
            if (paused) { wind *= 0.3f; forest = 0f; bar *= 0.3f; mask = 0f; }

            Fade(_wind, wind, dt);
            Fade(_forest, forest, dt);
            Fade(_bar, bar, dt);
            Fade(_mask, mask, dt * 3f);

            if (paused || me == null || GameSession.IsHub) return;
            Groans(ears, dt);
            Animals(ears, dt);
        }

        private static void Fade(AudioSource s, float target, float dt)
        {
            if (s == null) return;
            s.volume = Mathf.MoveTowards(s.volume, target, dt * 0.25f);
        }

        // Each nearby corpse-walker groans now and then on its own clock; hunters more often, all of them throttled.
        private void Groans(Vector3 ears, float dt)
        {
            _groanTimer -= dt;
            if (_groanTimer > 0f) return;
            _groanTimer = 0.45f;

            _near.Clear();
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a == null || a.Kind != ActorKind.Zombie || !a.IsAlive) continue;
                if ((a.transform.position - ears).sqrMagnitude > 30f * 30f) continue;
                _near.Add(a);
            }
            if (_near.Count == 0) return;

            Actor pick = _near[Random.Range(0, _near.Count)];
            float now = Time.time;
            if (_nextGroan.TryGetValue(pick, out float next) && now < next) return;

            var ai = pick.GetComponent<ZombieAI>();
            bool hunting = ai != null && ai.IsHunting;
            if (_nextGroan.Count > 400) _nextGroan.Clear();
            _nextGroan[pick] = now + (hunting ? Random.Range(2.5f, 5f) : Random.Range(6f, 14f));
            Sfx.Play(SfxIds.ZombieGroan, pick.transform.position + Vector3.up * 1.6f, hunting ? 0.75f : 0.5f, Random.Range(0.85f, 1.12f), hunting ? 30f : 22f);
        }

        // Far-off animals: wolves at night, a deer by day in the trees. Placed well away from the survivor so they
        // read as the landscape, not a threat.
        private void Animals(Vector3 ears, float dt)
        {
            if (!GameSession.IsExtraction) return;
            _animalTimer -= dt;
            if (_animalTimer > 0f) return;
            _animalTimer = Random.Range(45f, 120f);
            bool night = TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight;
            Vector2 o = Random.insideUnitCircle.normalized * Random.Range(35f, 60f);
            Vector3 at = ears + new Vector3(o.x, 2f, o.y);
            if (night) Sfx.Play(SfxIds.WolfHowl, at, 0.45f, Random.Range(0.9f, 1.08f), 90f);
            else if (ZoneForest.Density(ears) > 0.25f) Sfx.Play(SfxIds.DeerCall, at, 0.35f, Random.Range(0.9f, 1.1f), 70f);
        }
    }
}
