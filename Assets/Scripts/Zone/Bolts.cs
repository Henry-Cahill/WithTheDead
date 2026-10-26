using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A thrown nut with a strip of rag tied on: the oldest tool in the Zone. Anomalies react to it before they react
    /// to you — it is pressed flat, flung, sunk or sparked — and it stays where it fell, the rag marking the path
    /// that was tested. Bolts lie about for a couple of minutes (the oldest go first).
    /// </summary>
    public sealed class Bolt : MonoBehaviour
    {
        public static readonly List<Bolt> Live = new List<Bolt>();
        private const int MaxLive = 40;
        private const float Lifetime = 150f;

        /// <summary>True once an anomaly has reacted to this bolt (it won't trip another).</summary>
        public bool Spent { get; private set; }

private Rigidbody _rb;
            private Collider _col;
            private float _born, _restAt = -1f;
            private bool _sounded, _sinking;

                /// <summary>Creates and launches a new bolt from a point with given velocity, ignoring a specified collider.</summary>
                /// <summary>Creates and launches a new bolt from a point with given velocity, ignoring a specified collider.</summary>
        public static Bolt Throw(Vector3 from, Vector3 velocity, Collider ignore)
        {
            // Remove oldest bolts if we exceed the live limit to keep memory usage bounded
            while (Live.Count >= MaxLive)
            {
                // Take the oldest bolt (FIFO) and destroy its GameObject to free resources
                Bolt old = Live[0];
                Live.RemoveAt(0);
                if (old != null) Destroy(old.gameObject);
            }

            // Create a new GameObject for the bolt and position it at the throw origin
            var go = new GameObject("Bolt");
            go.transform.position = from;
            // Prevent Unity's static batching from affecting this temporary object
            ZoneKit.NoBake(go);
            // Build a simple mesh representing the bolt (cylinder + box) with appropriate colors
            var m = new RaidMesh();
            m.SetFrame(from, 0f);
            m.Cylinder(new Vector3(0f, -0.009f, 0f), 0.022f, 0.018f, 6, ZoneKit.Mt(new Color(0.55f, 0.55f, 0.52f)));
            m.Box(new Vector3(0f, 0f, -0.07f), new Vector3(0.012f, 0.003f, 0.12f), ZoneKit.M(new Color(0.92f, 0.9f, 0.84f)));
            // Apply mesh to the GameObject without casting shadows (bolts are small visual details)
            m.ApplyTo(go, from, ShadowCastingMode.Off);

            // Add a small sphere collider for interaction and optionally ignore a specific collider (e.g., the player)
            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.035f;
            if (ignore != null) Physics.IgnoreCollision(col, ignore);
            // Add a rigidbody so physics will move the bolt; configure it for lightweight fast-moving objects
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearVelocity = velocity;
            // Give the bolt a random spin for visual variety
            rb.angularVelocity = Random.insideUnitSphere * 18f;

            // Attach the Bolt component itself and store references for later updates
            var b = go.AddComponent<Bolt>();
            b._rb = rb;
            b._col = col;
            b._born = Time.time;
            Live.Add(b);
            return b;
        }

        private void OnDestroy() => Live.Remove(this);

        private void OnCollisionEnter(Collision c)
        {
            if (_sounded || c.relativeVelocity.magnitude < 1.2f) return;
            _sounded = true;
            Sfx.Play(SfxIds.BoltLand, transform.position, 0.75f, Random.Range(0.9f, 1.15f), 30f);
            NoiseSystem.Emit(transform.position, 4f);
        }

        private void Update()
        {
            if (Time.time - _born > Lifetime) { Destroy(gameObject); return; }
            if (_sinking)
            {
                transform.position += Vector3.down * (0.25f * Time.deltaTime);
                return;
            }
            // Once it settles, freeze it: a resting bolt costs nothing.
            if (_rb != null && !_rb.isKinematic && Time.time - _born > 0.6f && _rb.linearVelocity.sqrMagnitude < 0.01f)
            {
                if (_restAt < 0f) _restAt = Time.time;
                else if (Time.time - _restAt > 0.5f)
                {
                    _rb.isKinematic = true;
                    if (_col != null) _col.enabled = false;
                }
            }
            else _restAt = -1f;
        }

        /// <summary>An anomaly throws (or crushes) the bolt.</summary>
        public void Fling(Vector3 velocity, bool crush)
        {
            Spent = true;
            if (crush)
            {
                transform.localScale = new Vector3(1.4f, 0.3f, 1.4f);
                if (_rb != null) { _rb.isKinematic = true; }
                transform.position = new Vector3(transform.position.x, Mathf.Max(0.01f, transform.position.y - 0.05f), transform.position.z);
                return;
            }
            if (_rb == null) return;
            _rb.isKinematic = false;
            if (_col != null) _col.enabled = true;
            _rb.linearVelocity = velocity;
            _sounded = false;
        }

        /// <summary>The bolt sinks out of sight (grey syrup).</summary>
        public void Sink()
        {
            Spent = true;
            _sinking = true;
            if (_rb != null) _rb.isKinematic = true;
            if (_col != null) _col.enabled = false;
            _born = Time.time - Lifetime + 4f;
        }

                /// <summary>Marks this bolt as spent, preventing further anomaly interactions.</summary>
        public void MarkSpent() => Spent = true;
    }

    /// <summary>
    /// Throws bolts: the quick-throw key (X by default), or "Throw" on a bolt in the pack. They land where the
    /// survivor aims — the cursor in the isometric view, the crosshair otherwise — in a short arc.
    /// </summary>
    public sealed class BoltThrower : MonoBehaviour
    {
        public const string BoltId = "bolt";
        private float _ready;
        private PlayerInventory _inv;
        private PlayerController _controller;
        private PlayerStats _stats;
        private PlayerAvatar _avatar;
        private CharacterController _cc;
        private Camera _mainCam;

        private void Awake()
        {
            _inv = GetComponent<PlayerInventory>();
            _controller = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _avatar = GetComponent<PlayerAvatar>();
            _cc = GetComponent<CharacterController>();
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (GameInput.Pressed(GameAction.ThrowBolt)) TryThrow();
        }

                /// <summary>Attempts to throw a bolt if the player is ready, alive and has bolts available; consumes one from inventory.</summary>
        public bool TryThrow()
        {
            // Ensure the player can throw: respect cooldown, be alive, and have required components
            if (Time.time < _ready || _stats == null || _stats.IsDead || _stats.Incapacitated || _inv == null) return false;
            // Verify inventory contains at least one bolt; otherwise inform the player
            if (_inv.Count(BoltId) <= 0)
            {
                GameEvents.RaiseToast("No bolts left.");
                return false;
            }
            // Consume one bolt from inventory and set the next allowed throw time
            _inv.Consume(BoltId, 1);
            _ready = Time.time + 0.6f;

            // Compute the starting point at the player's chest height
            Vector3 chest = transform.position + Vector3.up * 1.45f;
            // Determine where the player is aiming (mouse look, controller aim, or fallback)
            Vector3 target = AimTarget(chest);
            // Offset the launch position slightly forward/right of the chest for a natural throw arc
            Vector3 from = chest + transform.forward * 0.35f + transform.right * 0.2f;
            // Calculate the desired flight vector components
            Vector3 delta = target - from;
            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            // Clamp distance to keep throws within reasonable range and avoid extreme velocities
            float dist = Mathf.Clamp(flat.magnitude, 2.5f, 16f);
            // If the horizontal component is near zero, default to forward direction
            if (flat.sqrMagnitude < 0.01f) flat = transform.forward;
            // Recompose target using clamped horizontal distance and original vertical offset
            target = from + flat.normalized * dist + Vector3.up * delta.y;
            delta = target - from;
            // Compute a time factor based on distance to shape the arc; enforce min/max for consistency
            float t = Mathf.Clamp(dist / 10f, 0.35f, 1.1f);
            // Solve projectile motion equation for initial velocity, factoring gravity
            Vector3 v = (delta - 0.5f * Physics.gravity * t * t) / t;
            // Limit speed to prevent unrealistic bolts
            if (v.magnitude > 18f) v = v.normalized * 18f;

            // Spawn the bolt with the calculated launch parameters, ignoring the player's collider
            Bolt.Throw(from, v, _cc);
            // Play throw sound effect with slight pitch variation
            Sfx.Play(SfxIds.BoltThrow, from, 0.5f, Random.Range(0.92f, 1.08f), 10f);
            // Trigger player swing animation if available
            if (_avatar != null && _avatar.Animator != null) _avatar.Animator.Swing(0.35f, false);
            return true;
        }

        /// <summary>Calculates the world point the player is aiming at for the bolt throw.</summary>
        private Vector3 AimTarget(Vector3 chest)
        {
            if (ViewState.MouseLook)
            {
                Camera cam = _mainCam;
                if (cam == null) cam = Camera.main;
                if (cam != null)
                {
                    Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, 30f, 1, QueryTriggerInteraction.Ignore)) return hit.point;
                    return cam.transform.position + cam.transform.forward * 12f;
                }
            }
            if (_controller != null && _controller.AimPoint != Vector3.zero) return _controller.AimPoint;
            return chest + transform.forward * 8f - Vector3.up * 1.4f;
        }
    }
}
