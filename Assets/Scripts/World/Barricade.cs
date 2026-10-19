using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace CloneZ
{
    /// <summary>
    /// A player-built wooden barricade (Project Zomboid's planked windows/doorways).
    /// Each plank nailed on adds height and health; a fully reinforced wall holds four.
    /// It carves the baked NavMesh (via a <see cref="NavMeshObstacle"/>) and blocks the
    /// player with a solid collider, so zombies must beat it down to get through. Keeps a
    /// static registry mirroring <see cref="SleepSpot"/> / <see cref="LootContainer"/>.
    /// </summary>
    public class Barricade : MonoBehaviour, IDamageable, IDemolishable
    {
        public static readonly List<Barricade> All = new List<Barricade>();

        public int Planks { get; private set; }
        public float Health { get; private set; }
        public float MaxHealth { get; private set; }

        public bool CanReinforce => Planks < CloneZConfig.BarricadeMaxPlanks;
        public float HealthNormalized => MaxHealth > 0f ? Mathf.Clamp01(Health / MaxHealth) : 0f;
        public Vector3 Position => transform.position;
        /// <summary>Per-plank health baked in at build time (captured/restored by the save system).</summary>
        public float PerPlankHealth => _perPlankHealth;

        private static readonly Color PlankColor = new Color(0.55f, 0.42f, 0.28f);
        private static readonly Color PostColor = new Color(0.40f, 0.30f, 0.20f);

        private float _perPlankHealth = CloneZConfig.BarricadeHealthPerPlank;
        private Transform _boardRoot;
        private CutawayOccluder _cutaway;
        private readonly List<GameObject> _boards = new List<GameObject>();

        bool IDamageable.IsDead => this == null || Planks <= 0;
        Transform IDamageable.Transform => transform;

        private void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        private void OnDisable() { All.Remove(this); }

        #region Queries
        /// <summary>Nearest barricade within <paramref name="range"/> (used by the builder to reinforce).</summary>
        public static Barricade Nearest(Vector3 p, float range, out float distance)
        {
            Barricade best = null;
            float bestSqr = range * range;
            distance = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                Barricade b = All[i];
                if (b == null) continue;
                float d = (b.transform.position - p).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = b; distance = Mathf.Sqrt(d); }
            }
            return best;
        }

        /// <summary>
        /// Nearest barricade within reach of <paramref name="from"/> that stands roughly between
        /// the searcher and <paramref name="towards"/> — i.e. a wall a zombie should bash to reach
        /// its target rather than one off to the side it can simply walk around.
        /// </summary>
        public static Barricade NearestObstructing(Vector3 from, Vector3 towards, float range)
        {
            Vector3 toTarget = CloneZUtil.Flat(towards - from);
            if (toTarget == Vector3.zero) return null;

            Barricade best = null;
            float bestSqr = range * range;
            for (int i = 0; i < All.Count; i++)
            {
                Barricade b = All[i];
                if (b == null || b.Planks <= 0) continue;
                Vector3 toB = b.transform.position - from;
                float sqr = toB.sqrMagnitude;
                if (sqr > bestSqr) continue;
                Vector3 flat = CloneZUtil.Flat(toB);
                if (Vector3.Dot(flat, toTarget) < 0.25f) continue;   // off to the side — not in the way
                bestSqr = sqr;
                best = b;
            }
            return best;
        }
        #endregion

        #region Building
        /// <summary>Builds a fresh single-plank barricade facing <paramref name="forward"/>.</summary>
        public static Barricade Spawn(Transform parent, Vector3 position, Vector3 forward, float healthMult)
        {
            forward = CloneZUtil.Flat(forward);
            if (forward == Vector3.zero) forward = Vector3.forward;

            var root = new GameObject("Barricade");
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            var b = root.AddComponent<Barricade>();
            b._perPlankHealth = Mathf.Max(1f, CloneZConfig.BarricadeHealthPerPlank * healthMult);
            b.BuildFrame();

            b.Planks = 1;
            b.MaxHealth = b._perPlankHealth;
            b.Health = b._perPlankHealth;
            b.RebuildBoards();
            return b;
        }

        /// <summary>Rebuilds a barricade from a saved game with its exact plank count and health.</summary>
        public static Barricade SpawnRestored(Transform parent, Vector3 position, Vector3 forward, int planks, float health, float maxHealth, float perPlank)
        {
            forward = CloneZUtil.Flat(forward);
            if (forward == Vector3.zero) forward = Vector3.forward;

            var root = new GameObject("Barricade");
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            var b = root.AddComponent<Barricade>();
            b._perPlankHealth = Mathf.Max(1f, perPlank);
            b.BuildFrame();

            b.Planks = Mathf.Clamp(planks, 1, CloneZConfig.BarricadeMaxPlanks);
            b.MaxHealth = maxHealth > 0f ? maxHealth : b.Planks * b._perPlankHealth;
            b.Health = Mathf.Clamp(health, 1f, b.MaxHealth);
            b.RebuildBoards();
            return b;
        }

        /// <summary>Nails on another plank, raising the wall's height and health. Returns false when full.</summary>
        public bool AddPlank()
        {
            if (!CanReinforce) return false;
            Planks++;
            MaxHealth += _perPlankHealth;
            Health += _perPlankHealth;
            RebuildBoards();
            return true;
        }

        private void BuildFrame()
        {
            float w = CloneZConfig.BarricadeWidth;
            float d = CloneZConfig.BarricadeDepth;
            float h = CloneZConfig.BarricadeHeight;

            // Two upright posts frame the wall and always stay while any plank remains.
            float postX = w * 0.5f - 0.08f;
            Plank(transform, new Vector3(-postX, h * 0.5f, 0f), new Vector3(0.16f, h, d), PostColor);
            Plank(transform, new Vector3(postX, h * 0.5f, 0f), new Vector3(0.16f, h, d), PostColor);

            // A collider so the player can shelter behind it, plus a carving obstacle so the
            // pre-baked NavMesh routes zombies around it (or dead-ends them into bashing it).
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, h * 0.5f, 0f);
            col.size = new Vector3(w, h, d);

            var obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, h * 0.5f, 0f);
            obstacle.size = new Vector3(w, h, d);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            // Blocks only by carving: a barricade restored from a save sits under the World, and its boards
            // would otherwise bake into the navmesh as a hole that outlives the barricade being beaten down.
            gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            // Tall enough for the iso camera to cut away, so it's marked like a house wall: when it's cut away the
            // build tool knows it's hidden and won't target it through the gap.
            _cutaway = gameObject.AddComponent<CutawayOccluder>();

            var holder = new GameObject("Boards");
            holder.transform.SetParent(transform, false);
            _boardRoot = holder.transform;
        }

        // Rebuilds the horizontal boards so their count matches the current plank total.
        private void RebuildBoards()
        {
            for (int i = 0; i < _boards.Count; i++)
                if (_boards[i] != null) Destroy(_boards[i]);
            _boards.Clear();

            float w = CloneZConfig.BarricadeWidth;
            float d = CloneZConfig.BarricadeDepth;
            float h = CloneZConfig.BarricadeHeight;
            int max = CloneZConfig.BarricadeMaxPlanks;

            float bottom = 0.28f;
            float top = h - 0.22f;
            float step = max > 1 ? (top - bottom) / (max - 1) : 0f;

            for (int i = 0; i < Planks; i++)
            {
                float y = bottom + step * i;
                var board = Plank(_boardRoot, new Vector3(0f, y, 0f), new Vector3(w, 0.2f, d * 0.75f), PlankColor);
                _boards.Add(board);
            }
            if (_cutaway != null) _cutaway.PartsChanged();   // while cut away, the new boards hide with the rest
        }

        // A single cube child with a URP material, its own collider stripped (the root
        // BoxCollider does the blocking); placed in the barricade's local space.
        private static GameObject Plank(Transform parent, Vector3 localPos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Board";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = CloneZUtil.Mat(color);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            return go;
        }
        #endregion

        #region Damage
        public void TakeDamage(in DamageInfo info)
        {
            if (Planks <= 0) return;
            Health -= Mathf.Max(0f, info.Amount);

            // Shed planks as the wall is beaten down.
            while (Planks > 0 && Health <= (Planks - 1) * _perPlankHealth + 0.01f)
                Planks--;

            if (Planks <= 0 || Health <= 0f)
            {
                Break();
                return;
            }

            RebuildBoards();
            // Splintering wood is noisy — a struggling barricade draws still more zombies.
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseMeleeHit);
        }

        private void Break()
        {
            Planks = 0;
            Health = 0f;
            NoiseSystem.Emit(transform.position, CloneZConfig.NoiseHammering);
            All.Remove(this);
            Removed?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Pries the barricade apart for salvage (the builder handles the material refund).
        /// Removes it from the registry at once so AI queries drop it immediately.</summary>
        public void Dismantle()
        {
            Planks = 0;
            Health = 0f;
            All.Remove(this);
            Removed?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Who raised it in the Zone (a camp owner's tag; null for the game's own).</summary>
        public string Owner;
        /// <summary>A barricade a runner raised, and one that broke or was torn down — camps and the netcode listen.</summary>
        public static event System.Action<Barricade> Raised, Removed;
        public static void RaiseRaised(Barricade b) { if (b != null) Raised?.Invoke(b); }
        /// <summary>Gone without a trace (mirroring another peer's loss).</summary>
        public void Vanish()
        {
            Planks = 0;
            Health = 0f;
            All.Remove(this);
            Destroy(gameObject);
        }
        #endregion

        #region IDemolishable
        public string DemolishName => "barricade";
        public bool CanDemolish(out string reason) { reason = null; return Planks > 0; }
        public void GetSalvage(out int planks, out int nails)
        {
            float f = CloneZConfig.HouseSalvageFraction;
            planks = Mathf.Max(0, Mathf.RoundToInt(Planks * CloneZConfig.BarricadePlankCost * f));
            nails = Mathf.Max(0, Mathf.RoundToInt(Planks * CloneZConfig.BarricadeNailCost * f));
        }
        public bool CarvesNavMesh => false;   // carves via a NavMeshObstacle (auto-freed on destroy), not a static bake
        #endregion
    }
}
