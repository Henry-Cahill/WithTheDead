using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>How the spawner fills the map: escalating waves (wave survival) or a standing
    /// population of wanderers (an extraction raid).</summary>
    public enum SpawnerMode { Waves, Ambient }

    /// <summary>
    /// Builds zombies at runtime. In <see cref="SpawnerMode.Waves"/> it drives escalating waves: zombies
    /// spawn on the NavMesh at random points anywhere in the arena (kept a short distance off the
    /// player) and tougher, faster variants appear in later waves, matching Project Zomboid's escalating
    /// horde pressure. In <see cref="SpawnerMode.Ambient"/> (a raid) it scatters a standing population
    /// of wanderers over the whole map and quietly replaces the fallen, out of the survivor's sight. On the
    /// streamed Kentucky map the population follows where people lived (<see cref="MapGameplay.DensityAt"/>):
    /// thin in the fields, thick in town, the dead placed where the density is.
    /// Spawns are only ever placed at ground level (never on a roof or a high shelf).
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        public Transform Player;
        public int MaxConcurrent = 45;
        public float SpawnInterval = 0.25f;
        /// <summary>Wave to resume from when continuing a saved game (0 = fresh start).</summary>
        public int StartWave;

        /// <summary>Waves (default) or an ambient raid population — set before Start.</summary>
        public SpawnerMode Mode = SpawnerMode.Waves;
        /// <summary>Ambient mode: how many of the dead roam the map at once.</summary>
        public int AmbientPopulation = 40;

        // Navmesh points above this height are roofs, counters or shelves — never spawn up there.
        private const float MaxSpawnHeight = 0.3f;

        // Ambient (raid) population.
        private const float AmbientStartDistance = 22f;     // the opening spread keeps clear of the survivor
        private const float AmbientRespawnDistance = 35f;   // replacements appear well away...
        private const float AmbientRespawnMinDelay = 20f;   // ...a while after a death
        private const float AmbientRespawnMaxDelay = 40f;
        private const float AmbientRetrySeconds = 3f;       // no hidden spot right now: look again shortly
        private const int AmbientSpawnsPerFrame = 4;        // spread the opening spawn over a few frames

        private readonly HashSet<ZombieHealth> _alive = new HashSet<ZombieHealth>();
        private readonly List<float> _respawnDue = new List<float>();   // Time.time each replacement is due
        private static readonly System.Predicate<ZombieHealth> s_Gone = z => z == null || z.IsDead;
        private int _wave;

        // The streamed Kentucky map: the dead are where people lived (MapGameplay.DensityAt). The standing population
        // follows how thick the Zone is around the survivors (a fifth of the usual out in the fields, twice it in a
        // city's core), and the dead are placed where the density is.
        private const float DensityInterval = 2f;           // seconds between looks at the country around
        private const float MinDensityScale = 0.2f, MaxDensityScale = 2f;
        private const float DensityFloor = 0.1f;            // the odd straggler out in the open even so
        private const float RefillMinDelay = 3f, RefillMaxDelay = 9f;   // a rising density fills in soon
        private float _densityScale = 1f, _densityPeak = 1f, _nextDensity;
        private int _deathsToReplace;

        /// <summary>How many of the dead should roam now: <see cref="AmbientPopulation"/>, scaled on the streamed map
        /// by how thick the Zone is around the survivors.</summary>
        public int AmbientTarget => KentuckyStreamer.Active != null
            ? Mathf.RoundToInt(Mathf.Max(0, AmbientPopulation) * _densityScale) : Mathf.Max(0, AmbientPopulation);

        /// <summary>The density scale the streamed map's population follows (1 elsewhere).</summary>
        public float DensityScale => _densityScale;

        private void Start()
        {
            _wave = Mathf.Max(0, StartWave);
            if (Mode == SpawnerMode.Ambient) StartCoroutine(RunAmbient());
            else StartCoroutine(RunWaves());
        }

        private IEnumerator RunWaves()
        {
            // Small grace period so the player can get their bearings.
            yield return new WaitForSeconds(2f);

            while (true)
            {
                if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                {
                    yield return null;
                    continue;
                }

                _wave++;
                GameManager.Instance.SetWave(_wave);
                int count = CloneZConfig.WaveBaseZombies + (_wave - 1) * CloneZConfig.WavePerWaveIncrease;

                for (int i = 0; i < count; i++)
                {
                    while (_alive.Count >= MaxConcurrent) yield return new WaitForSeconds(0.5f);
                    if (GameManager.Instance.State != GameState.Playing) break;
                    SpawnOne();
                    yield return new WaitForSeconds(SpawnInterval);
                }

                // Wait for the wave to be cleared before the next one begins.
                while (_alive.Count > 0 && GameManager.Instance.State == GameState.Playing)
                    yield return null;

                yield return new WaitForSeconds(CloneZConfig.WaveBreakSeconds);
            }
        }

        private void SpawnOne()
        {
            if (Player == null) return;
            if (!TryFindSpawnPoint(out Vector3 pos)) return;
            Spawn(pos, RollType(), RollHealth());
        }

        private void Spawn(Vector3 pos, ZombieType type, float health)
        {
            GameObject go = BuildZombie(pos);
            var ai = go.GetComponent<ZombieAI>();
            var zombieHealth = go.GetComponent<ZombieHealth>();

            ai.Configure(type);
            zombieHealth.SetMaxHealth(health);

            _alive.Add(zombieHealth);
            zombieHealth.OnDeath += HandleZombieDeath;
        }

        private void HandleZombieDeath(ZombieHealth z)
        {
            _alive.Remove(z);
            z.OnDeath -= HandleZombieDeath;
            _deathsToReplace++;
            if (z != null)
                LootContainer.SpawnCorpse(z.transform.position, Random.Range(int.MinValue, int.MaxValue));
        }

        private ZombieType RollType()
        {
            float r = Random.value;
            // Sprinter is the rarest, keep its original threshold.
            if (_wave >= CloneZConfig.SprinterUnlockWave && r < 0.12f) return ZombieType.Sprinter;
            // Crawler should be available in all waves, even after ToughUnlockWave.
            if (r < 0.08f) return ZombieType.Crawler;
            // FastShambler appears from the tough unlock wave onward.
            if (_wave >= CloneZConfig.ToughUnlockWave && r < 0.35f) return ZombieType.FastShambler;
            return ZombieType.Shambler;
        }

        private float RollHealth()
        {
            float variance = Random.Range(0f, CloneZConfig.ZombieHealthVariance);
            float r = Random.value;
            if (_wave >= CloneZConfig.ToughUnlockWave && r < 0.2f) return CloneZConfig.ZombieHealthTough + variance;
            if (r < 0.15f) return CloneZConfig.ZombieHealthFragile + variance;
            return CloneZConfig.ZombieHealthNormal + variance;
        }

        private bool TryFindSpawnPoint(out Vector3 pos)
        {
            float half = CloneZConfig.ArenaHalfSize;
            Vector3 centre = CloneZConfig.ArenaCenter;
            float minSqr = CloneZConfig.SpawnMinPlayerDistance * CloneZConfig.SpawnMinPlayerDistance;
            for (int i = 0; i < 24; i++)
            {
                // A random point anywhere in the box, not a ring around the player.
                var candidate = new Vector3(centre.x + Random.Range(-half, half), 0f, centre.z + Random.Range(-half, half));
                // Never pop a zombie right on top of the survivor.
                if (Player != null && (candidate - Player.position).sqrMagnitude < minSqr) continue;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas) && hit.position.y < MaxSpawnHeight)
                {
                    pos = hit.position;
                    return true;
                }
            }
            pos = default;
            return false;
        }

        #region Ambient (raid) population
        // Scatters the population over the map, then keeps it topped up: every death (or a shortfall in
        // the opening spread) books a replacement 20-40 s out, placed far off and out of the survivor's sight.
        private IEnumerator RunAmbient()
        {
            if (KentuckyStreamer.Active != null) LookAtDensity(true);
            int target = AmbientTarget;
            int attempts = 0;
            while (_alive.Count < target && attempts < target * 2)
            {
                attempts++;
                SpawnAmbient(AmbientStartDistance, false);
                if (attempts % AmbientSpawnsPerFrame == 0) yield return null;
            }

            var tick = new WaitForSeconds(1f);
            while (true)
            {
                yield return tick;
                if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing) continue;
                if (ExtractionManager.Instance != null && ExtractionManager.Instance.RaidOver) continue;

                _alive.RemoveWhere(s_Gone);
                if (KentuckyStreamer.Active != null)
                {
                    LetGoOfStragglers();
                    if (Time.time >= _nextDensity) LookAtDensity(false);
                }
                int goal = AmbientTarget;
                int missing = Mathf.Max(0, goal - _alive.Count - _respawnDue.Count);
                // The fallen come back a while later; a rise in the density (walking into town) fills in soon.
                for (int i = 0; i < missing; i++)
                {
                    bool replacement = _deathsToReplace > 0;
                    if (replacement) _deathsToReplace--;
                    _respawnDue.Add(Time.time + (replacement || KentuckyStreamer.Active == null
                        ? Random.Range(AmbientRespawnMinDelay, AmbientRespawnMaxDelay)
                        : Random.Range(RefillMinDelay, RefillMaxDelay)));
                }
                if (missing == 0) _deathsToReplace = 0;

                float now = Time.time;
                for (int i = _respawnDue.Count - 1; i >= 0; i--)
                {
                    if (now < _respawnDue[i]) continue;
                    if (_alive.Count >= goal || SpawnAmbient(AmbientRespawnDistance, true)) _respawnDue.RemoveAt(i);
                    else _respawnDue[i] = now + AmbientRetrySeconds;
                }
            }
        }

        // How thick the Zone is around the survivors: the mean of a grid of samples over the arena round each (the
        // population follows it, eased over half a minute unless it's the opening spread) and its peak (the spawns'
        // yardstick).
        /// <summary>Updates density sampling for ambient spawning.</summary>
        private void LookAtDensity(bool immediate)
        {
            // Schedule the next density sample after the defined interval.
            _nextDensity = Time.time + DensityInterval;
            // Refresh survivor list for density or straggler checks.
            GatherSurvivors();
            // Determine the sampling grid size based on arena dimensions, ensuring a minimum radius.
            float half = Mathf.Max(8f, CloneZConfig.ArenaHalfSize * 0.85f);
            float sum = 0f, peak = 0f;
            int n = 0;
            // Accumulate density values across the grid and track the highest sample.
            for (int s = 0; s < _survivors.Count; s++)
                for (int iz = -2; iz <= 2; iz++)
                    for (int ix = -2; ix <= 2; ix++)
                    {
                        Vector3 c = _survivors[s] + new Vector3(ix * half * 0.5f, 0f, iz * half * 0.5f);
                        float d = MapGameplay.DensityAt(c);
                        sum += d;
                        peak = Mathf.Max(peak, d);
                        n++;
                    }
            // Compute the average density and clamp it to sensible limits for scaling.
            float mean = n > 0 ? sum / n : 1f;
            float scale = Mathf.Clamp(mean, MinDensityScale, MaxDensityScale);
            // Smoothly transition to the new scale unless an immediate update is requested.
            _densityScale = immediate ? scale : Mathf.Lerp(_densityScale, scale, 0.15f);
            // Ensure a minimum peak value to avoid division by zero elsewhere.
            _densityPeak = Mathf.Max(0.05f, peak);
        }

        private bool SpawnAmbient(float minDistance, bool hiddenFromPlayer)
        {
            if (!TryFindAmbientPoint(minDistance, hiddenFromPlayer, out Vector3 pos)) return false;
            Spawn(pos, RollAmbientType(), CloneZConfig.ZombieHealthNormal + Random.Range(0f, CloneZConfig.ZombieHealthVariance));
            return true;
        }

        // On the streamed Kentucky map the arena square follows the survivors across the state; the dead left well behind
        // every one of them (where the map is let go of, out of sight) go quietly, and the top-up books their replacements
        // around the survivors again.
        private const float StragglerMargin = 48f;
        private readonly List<ZombieHealth> _stragglers = new List<ZombieHealth>();
        private readonly List<Vector3> _survivors = new List<Vector3>(4);

        // Where the survivors are: the arena's own centre (the local one's) first, then every other player's.
        private void GatherSurvivors()
        {
            _survivors.Clear();
            _survivors.Add(CloneZConfig.ArenaCenter);
            for (int i = 0; i < Actor.All.Count; i++)
            {
                Actor a = Actor.All[i];
                if (a != null && a.IsPlayerControlled && a != Actor.LocalPlayer) _survivors.Add(a.transform.position);
            }
        }

        /// <summary>Removes zombies that have strayed too far from any survivor.</summary>
        private void LetGoOfStragglers()
        {
            // Refresh survivor list for density or straggler checks.
            GatherSurvivors();
            // Determine the maximum distance from arena centre at which a zombie is still considered nearby.
            float reach = CloneZConfig.ArenaHalfSize + StragglerMargin;
            // Clear the temporary list before gathering stragglers.
            _stragglers.Clear();
            // Iterate over all alive zombies to find any that lie outside the extended arena bounds.
            foreach (ZombieHealth z in _alive)
            {
                // Skip null entries which may appear after destruction.
                if (z == null) continue;
                // Compute squared distance on XZ plane to each survivor to decide if within reach.
                Vector3 p = z.transform.position;
                bool near = false;
                for (int i = 0; i < _survivors.Count && !near; i++)
                    near = Mathf.Abs(p.x - _survivors[i].x) <= reach && Mathf.Abs(p.z - _survivors[i].z) <= reach;
                // If a zombie is beyond the reach of all survivors, mark it for removal.
                if (!near) _stragglers.Add(z);
            }
            // Destroy each zombie that has strayed too far, removing it from tracking and unsubscribing events.
            for (int i = 0; i < _stragglers.Count; i++)
            {
                ZombieHealth z = _stragglers[i];
                _alive.Remove(z);
                z.OnDeath -= HandleZombieDeath;
                Destroy(z.gameObject);
            }
        }

        // A random ground-level navmesh point anywhere on the map, at least minDistance (flat) from the
        // survivor and, when asked, with something solid between it and their eyes. On the streamed map the
        // point is around a survivor picked at random, kept off (and out of sight of) the nearest one.
        private bool TryFindAmbientPoint(float minDistance, bool hiddenFromPlayer, out Vector3 pos)
        {
            float half = Mathf.Max(1f, CloneZConfig.ArenaHalfSize - 1f);
            bool streamed = KentuckyStreamer.Active != null;
            Vector3 centre = CloneZConfig.ArenaCenter;
            if (streamed)
            {
                // Refresh survivor positions to use as sample centres for density calculations.
                // Refresh survivor list for density or straggler checks.
                GatherSurvivors();
                centre = _survivors[Random.Range(0, _survivors.Count)];
            }
            float minSqr = minDistance * minDistance;
            bool hasPlayer = Player != null;
            Vector3 player = hasPlayer ? Player.position : Vector3.zero;
            for (int i = 0; i < (streamed ? 40 : 30); i++)
            {
                var candidate = new Vector3(centre.x + Random.Range(-half, half), 0f, centre.z + Random.Range(-half, half));
                // Where people lived: a point is kept as often as its density is to the peak around (a few anywhere).
                if (streamed && Random.value * _densityPeak > Mathf.Max(DensityFloor * _densityPeak, MapGameplay.DensityAt(candidate))) continue;
                if (streamed && hasPlayer)
                {
                    Actor nearest = HumanUtil.NearestPlayer(candidate, out float _);
                    if (nearest != null) player = nearest.transform.position;
                }
                if (hasPlayer && FlatSqr(candidate - player) < minSqr) continue;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas)) continue;
                if (hit.position.y >= MaxSpawnHeight) continue;
                if (hasPlayer && FlatSqr(hit.position - player) < minSqr) continue;
                if (hiddenFromPlayer && hasPlayer && InPlayerSight(player, hit.position)) continue;
                pos = hit.position;
                return true;
            }
            pos = default;
            return false;
        }

        // Same line-of-sight test the zombies use: walls and props block it, other zombies don't.
        private static bool InPlayerSight(Vector3 player, Vector3 point)
        {
            Vector3 eye = player + Vector3.up * 1.6f;
            Vector3 chest = point + Vector3.up * 1.0f;
            int mask = ~(1 << CloneZConfig.IgnoreRaycastLayer);
            return !Physics.Linecast(eye, chest, mask, QueryTriggerInteraction.Ignore);
        }

        private static float FlatSqr(Vector3 v) => v.x * v.x + v.z * v.z;

        // The raid mix: mostly shamblers, some faster ones, the odd crawler and a rare sprinter.
        private static ZombieType RollAmbientType()
        {
            float r = Random.value;
            if (r < 0.03f) return ZombieType.Sprinter;
            if (r < 0.08f) return ZombieType.Crawler;
            if (r < 0.30f) return ZombieType.FastShambler;
            return ZombieType.Shambler;
        }
        #endregion

        /// <summary>Assembles a zombie GameObject (body + collider + agent + brain).</summary>
        public static GameObject BuildZombie(Vector3 pos)
        {
            var root = new GameObject("Zombie");
            root.transform.position = pos;

            // Hit and blocking volume: the same capsule the old placeholder body carried.
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = 1;
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.425f;

            // The body: a randomly dressed humanoid, animated from the brain below. Its face shows the
            // player which way it's facing (for shoves and back-crits).
            root.AddComponent<ZombieAvatar>();

            SetLayerRecursive(root.transform, CloneZConfig.IgnoreRaycastLayer);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.baseOffset = 0f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            root.AddComponent<ZombieHealth>();
            root.AddComponent<ZombieAI>();
            root.AddComponent<ZombieVisibility>();   // hidden while outside the survivor's line of sight
            return root;
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayerRecursive(c, layer);
        }
    }
}
