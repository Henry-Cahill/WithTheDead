using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// Builds zombies at runtime and drives escalating waves. Zombies spawn on the
    /// NavMesh in a ring around the player; tougher and faster variants appear in
    /// later waves. Waves match Project Zomboid's escalating horde pressure.
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        public Transform Player;
        public int MaxConcurrent = 45;
        public float SpawnInterval = 0.25f;
        /// <summary>Wave to resume from when continuing a saved game (0 = fresh start).</summary>
        public int StartWave;

        private readonly HashSet<ZombieHealth> _alive = new HashSet<ZombieHealth>();
        private int _wave;

        private void Start()
        {
            _wave = Mathf.Max(0, StartWave);
            StartCoroutine(RunWaves());
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

            GameObject go = BuildZombie(pos);
            var ai = go.GetComponent<ZombieAI>();
            var health = go.GetComponent<ZombieHealth>();

            ai.Configure(RollType());
            health.SetMaxHealth(RollHealth());

            _alive.Add(health);
            health.OnDeath += HandleZombieDeath;
        }

        private void HandleZombieDeath(ZombieHealth z)
        {
            _alive.Remove(z);
            z.OnDeath -= HandleZombieDeath;
            if (z != null)
                LootContainer.SpawnCorpse(z.transform.position, Random.Range(int.MinValue, int.MaxValue));
        }

        private ZombieType RollType()
        {
            float r = Random.value;
            if (_wave >= CloneZConfig.SprinterUnlockWave && r < 0.12f) return ZombieType.Sprinter;
            if (_wave >= CloneZConfig.ToughUnlockWave && r < 0.35f) return ZombieType.FastShambler;
            if (r < 0.08f) return ZombieType.Crawler;
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
            for (int i = 0; i < 12; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                float dist = Random.Range(CloneZConfig.SpawnRingMin, CloneZConfig.SpawnRingMax);
                Vector3 candidate = Player.position + new Vector3(dir.x, 0f, dir.y) * dist;
                candidate.x = Mathf.Clamp(candidate.x, -CloneZConfig.ArenaHalfSize, CloneZConfig.ArenaHalfSize);
                candidate.z = Mathf.Clamp(candidate.z, -CloneZConfig.ArenaHalfSize, CloneZConfig.ArenaHalfSize);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    pos = hit.position;
                    return true;
                }
            }
            pos = default;
            return false;
        }

        /// <summary>Assembles a zombie GameObject (visual + collider + agent + brain).</summary>
        public static GameObject BuildZombie(Vector3 pos)
        {
            var root = new GameObject("Zombie");
            root.transform.position = pos;

            // Visual body (capsule keeps its CapsuleCollider for hits & blocking).
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.85f, 1f, 0.85f);
            body.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.36f, 0.5f, 0.30f));

            // A small "face" marker so the player can read facing (for shove/back-crits).
            var face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = "Face";
            Object.Destroy(face.GetComponent<Collider>());
            face.transform.SetParent(body.transform);
            face.transform.localScale = new Vector3(0.5f, 0.25f, 0.25f);
            face.transform.localPosition = new Vector3(0f, 0.55f, 0.42f);
            face.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(new Color(0.1f, 0.05f, 0.05f));

            SetLayerRecursive(root.transform, CloneZConfig.IgnoreRaycastLayer);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 2f;
            agent.baseOffset = 0f;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            root.AddComponent<ZombieHealth>();
            root.AddComponent<ZombieAI>();
            return root;
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayerRecursive(c, layer);
        }
    }
}
