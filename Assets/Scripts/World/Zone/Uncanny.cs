using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// Fear without a health bar. Every few minutes (more often at night, and deeper in) something happens that is
    /// not a fight: the Returned walk home from the cemetery and sit at a door; lamps gutter for no reason; a radio
    /// switches itself on; a figure stands at the edge of the fog and is gone when you get there; in Mirelock's woods
    /// at night, something in the dark makes a sound. These play locally on every peer (they change nothing that
    /// matters) and respect the sandbox switch.
    /// </summary>
    public sealed class UncannyDirector : MonoBehaviour
    {
        private ZoneDef _zone;
        private float _next;
        private readonly System.Random _rng = new System.Random();

        private static readonly string[] s_RadioLines =
        {
            "...the water is warm today, the water is warm today, the water is...",
            "...counting down from nine. Nine. Nine. Nine...",
            "...if you can hear this, don't go home. It isn't your home any more...",
            "...this is the Institute. Please remain where you are. Someone is coming...",
            "...we are all of us here, down at the bottom, singing...",
            "...Mama? Mama, the door won't open...",
        };

        public static void BeginRaid(GameObject host, ZoneDef zone)
        {
            if (!SandboxSettings.Current.UncannyEvents) return;
            var d = host.GetComponent<UncannyDirector>() ?? host.AddComponent<UncannyDirector>();
            d._zone = zone;
            d._next = Time.time + Random.Range(60f, 140f);
        }

        private void Update()
        {
            if (_zone == null || Time.time < _next) return;
            Actor me = Actor.LocalPlayer;
            if (me == null || !me.IsAlive) return;
            bool night = TimeOfDay.Instance != null && TimeOfDay.Instance.IsNight;
            float gap = Random.Range(90f, 220f) / (night ? 1.6f : 1f) / (0.8f + _zone.Depth * 0.2f);
            _next = Time.time + gap;

            float forest = ZoneForest.Density(me.transform.position);
            int roll = _rng.Next(100);
            if (_zone.Id == "mirelock" && night && forest > 0.35f && roll < 30) { Sfx.PlayUI(SfxIds.Stinger, 0.55f); return; }
            if (roll < 30 && ReturnedFigure.Count < 4) { ReturnedFigure.Spawn(_rng); return; }
            if (roll < 50) { StartCoroutine(Flicker(me.transform.position)); return; }
            if (roll < 70) { Radio(me.transform.position); return; }
            DistantFigure.Spawn(me);
        }

        private IEnumerator Flicker(Vector3 around)
        {
            var lights = new List<Light>();
            var original = new List<float>();
            foreach (Light l in FindObjectsByType<Light>())
            {
                if (l == null || !l.enabled || l.type == LightType.Directional) continue;
                if ((l.transform.position - around).sqrMagnitude > 25f * 25f) continue;
                if (Actor.LocalPlayer != null && l.transform.IsChildOf(Actor.LocalPlayer.transform)) continue;
                lights.Add(l);
                original.Add(l.intensity);
            }
            if (lights.Count == 0) yield break;
            float dur = Random.Range(2.5f, 4f);
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                for (int i = 0; i < lights.Count; i++)
                {
                    if (lights[i] == null) continue;
                    float k = Mathf.PerlinNoise(Time.time * 14f, i * 3.1f) > 0.55f ? 1f : 0.05f;
                    ZoneFlameLight f = lights[i].GetComponent<ZoneFlameLight>();
                    if (f != null) f.Disturb = k;
                    else lights[i].intensity = original[i] * k;
                }
                yield return null;
            }
            for (int i = 0; i < lights.Count; i++)
            {
                if (lights[i] == null) continue;
                ZoneFlameLight f = lights[i].GetComponent<ZoneFlameLight>();
                if (f != null) f.Disturb = 1f;
                else lights[i].intensity = original[i];
            }
        }

        private void Radio(Vector3 around)
        {
            Vector2 o = Random.insideUnitCircle * 8f;
            Sfx.Play(SfxIds.RadioStatic, around + new Vector3(o.x, 1f, o.y), 0.7f, 1f, 18f);
            GameEvents.RaiseSubtitle("A radio, somewhere", s_RadioLines[_rng.Next(s_RadioLines.Length)]);
        }
    }

    /// <summary>
    /// One of the Returned: the dead who walk home from the Zone's cemeteries and simply sit, harmless. Bullets only
    /// make them stumble; standing near one is hard on the nerves. Shooting one is remembered (quietly).
    /// </summary>
    public sealed class ReturnedFigure : MonoBehaviour, IDamageable
    {
        private static readonly List<ReturnedFigure> s_All = new List<ReturnedFigure>();
        private static bool s_ShotOne;

        public static int Count => s_All.Count;
        public bool IsDead => false;
        public Transform Transform => transform;

        private NavMeshAgent _agent;
        private HumanoidAnimator _anim;
        private bool _arrived;
        private Vector3 _door;
        private Vector3 _lastPos;
        private float _whisper;

        public static void ResetForRaid()
        {
            s_All.Clear();
            s_ShotOne = false;
        }

        /// <summary>Remembers (once per raid) when the local survivor shoots one of the Returned.</summary>
        public static void OnShotResolved(ShotRequest req, ShotResult res)
        {
            if (s_ShotOne || !(res.Target is ReturnedFigure)) return;
            if (req.ShooterActor == null || req.ShooterActor != Actor.LocalPlayer) return;
            s_ShotOne = true;
            MoralLedger.Record("shot one of the Returned", MoralLedger.ShotTheReturned);
        }

        public static ReturnedFigure Spawn(System.Random rng)
        {
            Vector3 from;
            if (!WorldMarkers.TryGetFirst(WorldMarkers.Cemetery, out from))
            {
                float h = CloneZConfig.ArenaHalfSize - 6f;
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                from = new Vector3(Mathf.Cos(a) * h, 0f, Mathf.Sin(a) * h);
            }
            if (!NavMesh.SamplePosition(from, out NavMeshHit hit, 12f, NavMesh.AllAreas)) return null;

            var go = new GameObject("Returned");
            go.transform.position = hit.position;
            go.layer = CloneZConfig.IgnoreRaycastLayer;
            var f = go.AddComponent<ReturnedFigure>();
            HumanoidLook look = HumanoidLook.RandomZombie(rng);
            look.SkinColor = new Color(0.62f, 0.68f, 0.74f);
            look.Shape.Gore = 0;
            HumanoidRig rig = HumanoidRig.Create(go.transform, look);
            f._anim = rig.gameObject.AddComponent<HumanoidAnimator>();
            f._anim.AutoUpdate = false;
            f._anim.Gait = HumanoidGait.Shambler;
            f._anim.Limp = 0.2f;
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.9f, 0f);
            col.height = 1.8f;
            col.radius = 0.3f;
            f._agent = go.AddComponent<NavMeshAgent>();
            f._agent.speed = 0.55f;
            f._agent.radius = 0.3f;
            f._agent.angularSpeed = 180f;
            var actor = go.AddComponent<Actor>();
            actor.Kind = ActorKind.Human;
            actor.Faction = Factions.Zone;
            actor.DisplayName = "One of the Returned";
            actor.Untargetable = true;
            f._door = f.PickDoor(rng, hit.position);
            f._agent.SetDestination(f._door);
            return f;
        }

        // A door to sit at: a building's entrance, else somewhere inside the town.
        private Vector3 PickDoor(System.Random rng, Vector3 from)
        {
            var doors = new List<Vector3>();
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it is RaidDoor) doors.Add(it.InteractPoint);
            }
            Vector3 target = doors.Count > 0 ? doors[rng.Next(doors.Count)] : new Vector3((float)rng.NextDouble() * 40f - 20f, 0f, (float)rng.NextDouble() * 40f - 20f);
            return NavMesh.SamplePosition(target, out NavMeshHit h, 6f, NavMesh.AllAreas) ? h.position : from;
        }

        private void OnEnable() { if (!s_All.Contains(this)) s_All.Add(this); }
        private void OnDisable() => s_All.Remove(this);

        public void TakeDamage(in DamageInfo info)
        {
            if (_anim != null) _anim.Stagger(info.HitDirection * 0.4f, 0.6f);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (_anim == null || dt <= 0f) return;
            Vector3 v = (transform.position - _lastPos) / dt;
            _lastPos = transform.position;
            if (!_arrived && _agent != null && _agent.enabled && !_agent.pathPending && _agent.remainingDistance < 1.2f)
            {
                _arrived = true;
                _agent.isStopped = true;
                _anim.Seated = true;
                _anim.SeatPoint = transform.position + Vector3.up * 0.3f;
            }
            if (_agent != null && _agent.velocity.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(_agent.velocity.normalized), 4f * dt);
            _anim.Velocity = _arrived ? Vector3.zero : (v.sqrMagnitude < 400f ? v : Vector3.zero);
            _anim.ArmsOut = false;
            _anim.Evaluate(dt);

            Actor me = Actor.LocalPlayer;
            if (me == null) return;
            float d = Vector3.Distance(me.transform.position, transform.position);
            if (d < 8f)
            {
                var stats = me.GetComponent<PlayerStats>();
                if (stats != null && stats.Panic < 55f) stats.Panic += 4f * dt;
                _whisper -= dt;
                if (_whisper <= 0f)
                {
                    _whisper = Random.Range(5f, 9f);
                    Sfx.Play(SfxIds.UncannyWhisper, transform.position + Vector3.up * 1.6f, 0.5f, Random.Range(0.85f, 1.1f), 10f);
                }
            }
        }
    }

    /// <summary>A figure at the edge of the fog that isn't there when you arrive.</summary>
    public sealed class DistantFigure : MonoBehaviour
    {
        private float _life = 15f;
        private float _stare;

        public static void Spawn(Actor viewer)
        {
            Camera cam = Camera.main;
            Vector3 fwd = cam != null ? cam.transform.forward : viewer.transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            for (int i = 0; i < 6; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * fwd;
                Vector3 p = viewer.transform.position + dir * Random.Range(32f, 44f);
                if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 4f, NavMesh.AllAreas)) continue;
                if (Physics.Linecast(viewer.EyePosition, hit.position + Vector3.up * 1.5f, Actor.SightMask, QueryTriggerInteraction.Ignore)) continue;
                var go = new GameObject("DistantFigure");
                go.transform.position = hit.position;
                go.transform.rotation = Quaternion.LookRotation(-dir);
                var m = new RaidMesh();
                Color black = new Color(0.02f, 0.02f, 0.025f);
                m.Ellipsoid(new Vector3(0f, 1.1f, 0f), new Vector3(0.45f, 1.5f, 0.3f), DecorPalette.Matte(black), 8, 5);
                m.Ellipsoid(new Vector3(0f, 2.05f, 0f), new Vector3(0.24f, 0.3f, 0.26f), DecorPalette.Matte(black), 8, 5);
                m.Rod(new Vector3(-0.2f, 1.6f, 0f), new Vector3(-0.28f, 0.6f, 0f), 0.06f, DecorPalette.Matte(black), 5);
                m.Rod(new Vector3(0.2f, 1.6f, 0f), new Vector3(0.28f, 0.6f, 0f), 0.06f, DecorPalette.Matte(black), 5);
                GameObject body = m.Emit("Silhouette", go.transform, Vector3.zero);
                if (body != null) body.transform.localPosition = Vector3.zero;
                go.AddComponent<DistantFigure>();
                return;
            }
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            Actor me = Actor.LocalPlayer;
            if (_life <= 0f || me == null) { Destroy(gameObject); return; }
            Vector3 to = transform.position - me.transform.position;
            if (to.magnitude < 22f) { Vanish(); return; }
            Camera cam = Camera.main;
            if (cam != null && Vector3.Dot(cam.transform.forward, (transform.position + Vector3.up * 1.5f - cam.transform.position).normalized) > 0.985f)
            {
                _stare += Time.deltaTime;
                if (_stare > 1.4f) Vanish();
            }
        }

        private void Vanish()
        {
            Sfx.Play(SfxIds.UncannyWhisper, transform.position + Vector3.up * 1.5f, 0.35f, 0.8f, 40f);
            Destroy(gameObject);
        }
    }
}
