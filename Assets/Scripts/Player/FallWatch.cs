using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CloneZ
{
    /// <summary>
    /// Catches the survivor dropping out of the world: further than <see cref="DropLimit"/> below both where they last
    /// stood and ground level, which no fall in the game reaches, only a fall through a floor with nothing under it. It
    /// logs an error (so the console and the smoke test show it) with what they stood on and what is under that spot
    /// now, the streamed map's snowfield (the collider meant to catch anyone the pieces don't), the body's collider and
    /// the frames before the fall, so the hole can be found. Then it sets the survivor back on solid ground.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FallWatch : MonoBehaviour
    {
        /// <summary>How far below their last footing, and below ground level (y 0), counts as out of the world.</summary>
        public const float DropLimit = 15f;

        private const int History = 150;   // frames kept for the report
        private const int FullReports = 3; // after these, a fall gets one line

        /// <summary>Falls caught this session.</summary>
        public static int Caught { get; private set; }

        private struct Frame
        {
            public int Index, FloorChanges;
            public float Time, Dt;
            public Vector3 Position;
            public bool Enabled, Grounded;
        }

        private readonly Frame[] _frames = new Frame[History];
        private readonly List<string> _floors = new List<string>();
        private int _next, _count, _floorChanges, _footingFrame;
        private CharacterController _cc;
        private Collider _floor;
        private Vector3 _footing;
        private bool _haveFooting;

        // Enter Play Mode without a domain reload keeps statics from the last session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Caught = 0;

        private void Awake() => _cc = GetComponent<CharacterController>();

        // The CharacterController reports everything it touches while it moves; what it stands on is the floor.
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < 0.5f || hit.collider == _floor) return;
            _floor = hit.collider;
            _floorChanges++;
            _floors.Add("frame " + Time.frameCount + " (#" + _floorChanges + "): " + Describe(hit.collider));
            if (_floors.Count > 8) _floors.RemoveAt(0);
        }

        private void LateUpdate()
        {
            Vector3 p = transform.position;
            bool on = _cc.enabled, grounded = on && _cc.isGrounded;
            _frames[_next] = new Frame
            {
                Index = Time.frameCount, FloorChanges = _floorChanges, Time = Time.time, Dt = Time.unscaledDeltaTime,
                Position = p, Enabled = on, Grounded = grounded,
            };
            _next = (_next + 1) % History;
            if (_count < History) _count++;

            // Seated or driving, the seat or the car carries the body; a fall counts from where it next stands.
            if (!on) { _haveFooting = false; return; }
            if (grounded)
            {
                _footing = p;
                _footingFrame = Time.frameCount;
                _haveFooting = true;
                return;
            }
            if (!_haveFooting || p.y > Mathf.Min(_footing.y, 0f) - DropLimit) return;

            Caught++;
            Debug.LogError(Caught <= FullReports ? Report(p)
                : "[CloneZ] The survivor fell out of the world again (" + Caught + " times now) from " + _footing.ToString("F2") + "; set back on solid ground.");
            Rescue();
        }

        private string Report(Vector3 now)
        {
            var sb = new StringBuilder(4096);
            sb.Append("[CloneZ] The survivor fell out of the world: now at ").Append(now.ToString("F1"))
              .Append(", last stood at ").Append(_footing.ToString("F2")).Append(" (frame ").Append(_footingFrame).Append(", ")
              .Append(Time.frameCount - _footingFrame).Append(" frames ago); ").Append(GameSession.Mode)
              .Append(KettleLobby.RaidUnderway ? ", raid under way" : "").Append(". Set back on solid ground.");
            sb.Append("\nUnderfoot, latest last:");
            foreach (string f in _floors) sb.Append("\n  ").Append(f);
            sb.Append("\nThat floor now: ").Append(Describe(_floor));
            sb.Append("\nUnder the last footing now (from 3 m above it, 60 m down):");
            int n = 0;
            foreach (RaycastHit h in Under(_footing + Vector3.up * 3f, 63f))
            {
                sb.Append("\n  y ").Append(h.point.y.ToString("F2")).Append(": ").Append(Describe(h.collider));
                if (++n == 8) break;
            }
            if (n == 0) sb.Append(" nothing");
            sb.Append("\nTouching the feet there:");
            n = 0;
            foreach (Collider c in Physics.OverlapSphere(_footing + Vector3.up * 0.2f, 0.5f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                sb.Append("\n  ").Append(Describe(c));
                if (++n == 8) break;
            }
            if (n == 0) sb.Append(" nothing");
            sb.Append("\nSnowfield: ").Append(Snowfield());
            sb.Append("\nColliders over that spot (by their bounds) that a ray through the scene misses:").Append(Unseen(_footing));
            sb.Append("\nSnowfield checks: ").Append(SnowfieldChecks(_footing));
            sb.Append("\nBody: controller ").Append(_cc.enabled ? "on" : "OFF").Append(", centre ").Append(_cc.center.ToString("F2"))
              .Append(", height ").Append(_cc.height.ToString("F2")).Append(", radius ").Append(_cc.radius.ToString("F2"))
              .Append(", skin ").Append(_cc.skinWidth.ToString("F3")).Append(", step ").Append(_cc.stepOffset.ToString("F2"))
              .Append(", collisions ").Append(_cc.detectCollisions ? "on" : "OFF").Append(", layer ").Append(LayerMask.LayerToName(gameObject.layer))
              .Append(_cc.excludeLayers.value != 0 ? ", excludes layers " + _cc.excludeLayers.value : "")
              .Append(", scale ").Append(transform.lossyScale.ToString("F2"));
            sb.Append("\nFrames before the fall (frame, time, frame ms, position, G grounded / a airborne, controller, floor #):");
            AppendFrames(sb);
            return sb.ToString();
        }

        private void AppendFrames(StringBuilder sb)
        {
            var frames = new List<Frame>(_count);
            for (int i = 0; i < _count; i++)
            {
                Frame f = _frames[(_next - _count + i + History) % History];
                if (f.Index >= _footingFrame - 10) frames.Add(f);
            }
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames.Count > 60 && i == 45)
                {
                    sb.Append("\n  ... ").Append(frames.Count - 55).Append(" frames ...");
                    i = frames.Count - 10;
                }
                Frame f = frames[i];
                sb.Append("\n  ").Append(f.Index).Append(' ').Append(f.Time.ToString("F2")).Append("s ")
                  .Append((f.Dt * 1000f).ToString("F0")).Append("ms ").Append(f.Position.ToString("F2")).Append(' ')
                  .Append(f.Grounded ? 'G' : 'a').Append(f.Enabled ? " on" : " OFF").Append(" #").Append(f.FloorChanges);
            }
        }

        // Back where they stood if something still holds that spot up, else onto ground level there.
        private void Rescue()
        {
            Vector3 at = new Vector3(_footing.x, Mathf.Max(0f, _footing.y) + 0.05f, _footing.z);
            foreach (RaycastHit h in Under(_footing + Vector3.up * 0.5f, 3.5f))
            {
                at = h.point + Vector3.up * 0.05f;
                break;
            }
            var interactor = GetComponent<PlayerInteractor>();
            if (interactor != null && interactor.Context != null) interactor.Context.Teleport(at);
            else
            {
                _cc.enabled = false;
                transform.position = at;
                _cc.enabled = true;
            }
            Physics.SyncTransforms();
            var controller = GetComponent<PlayerController>();
            if (controller != null) controller.StopFalling();
        }

        // Solid colliders (not the survivor's own) on a line down from a point, nearest first.
        private List<RaycastHit> Under(Vector3 from, float distance)
        {
            var hits = new List<RaycastHit>();
            foreach (RaycastHit h in Physics.RaycastAll(from, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore))
                if (!h.collider.transform.IsChildOf(transform)) hits.Add(h);
            hits.Sort((a, b) => a.distance.CompareTo(b.distance));
            return hits;
        }

        private static string Snowfield()
        {
            BoxCollider box = SnowfieldBox(out string why);
            return box != null ? Describe(box) : why;
        }

        private static BoxCollider SnowfieldBox(out string why)
        {
            why = null;
            KentuckyStreamer streamer = KentuckyStreamer.Active;
            if (streamer == null) { why = "no streamed map"; return null; }
            Transform ground = streamer.transform.Find("Ground");
            if (ground == null) { why = "MISSING"; return null; }
            var box = ground.GetComponent<BoxCollider>();
            if (box == null) why = "no collider on " + Path(ground);
            return box;
        }

        // Every enabled solid collider whose bounds lie over or under the spot, tested with a ray straight at it alone
        // (Collider.Raycast, the shape's own test) and with the same ray through the scene (what the body and every query
        // use): one the first hits and the second doesn't is in the game but missing from the physics scene.
        private string Unseen(Vector3 at)
        {
            var sb = new StringBuilder();
            int tested = 0, unseen = 0;
            foreach (Collider c in FindObjectsByType<Collider>(FindObjectsInactive.Exclude))
            {
                if (!c.enabled || c.isTrigger || c.transform.IsChildOf(transform)) continue;
                Bounds b = c.bounds;
                if (at.x < b.min.x || at.x > b.max.x || at.z < b.min.z || at.z > b.max.z) continue;
                var ray = new Ray(new Vector3(at.x, b.max.y + 1f, at.z), Vector3.down);
                float reach = b.size.y + 2f;
                if (!c.Raycast(ray, out _, reach)) continue;
                tested++;
                bool seen = false;
                foreach (RaycastHit h in Physics.RaycastAll(ray, reach, ~0, QueryTriggerInteraction.Ignore))
                    if (h.collider == c) { seen = true; break; }
                if (seen) continue;
                unseen++;
                if (unseen <= 6) sb.Append("\n  ").Append(Describe(c));
            }
            sb.Append(unseen == 0 ? " none" : "").Append(" (").Append(unseen).Append(" of ").Append(tested).Append(" that a ray straight at them hits)");
            return sb.ToString();
        }

        private string SnowfieldChecks(Vector3 at)
        {
            BoxCollider box = SnowfieldBox(out string why);
            if (box == null) return why;
            var ray = new Ray(new Vector3(at.x, 3f, at.z), Vector3.down);
            int mask = 1 << box.gameObject.layer;
            var sb = new StringBuilder(512);
            sb.Append("transform at ").Append(box.transform.position.ToString("F2")).Append(", centre ").Append(box.center.ToString("F2"))
              .Append(", size ").Append(box.size.ToString("F1")).Append(", scale ").Append(box.transform.lossyScale.ToString("F2"));
            sb.Append("; a ray at it alone ").Append(box.Raycast(ray, out RaycastHit direct, 10f) ? "hits at y " + direct.point.y.ToString("F2") : "MISSES");
            sb.Append(", through the scene ").Append(SceneHit(ray, mask, box));
            Scene s = box.gameObject.scene;
            sb.Append("; in scene '").Append(s.name).Append(s == gameObject.scene ? "' (the survivor's too)" : "' (the survivor's is '" + gameObject.scene.name + "')")
              .Append(s.GetPhysicsScene() == Physics.defaultPhysicsScene ? ", default physics scene" : ", ITS OWN PHYSICS SCENE");
            if (Physics.GetIgnoreCollision(_cc, box)) sb.Append(", IGNORED by the survivor's body");
            if (box.attachedRigidbody != null) sb.Append(", on the rigidbody of ").Append(Path(box.attachedRigidbody.transform));
            if (box.excludeLayers.value != 0) sb.Append(", excludes layers ").Append(box.excludeLayers.value);
            if (box.includeLayers.value != 0) sb.Append(", includes layers ").Append(box.includeLayers.value);
            Physics.SyncTransforms();
            sb.Append("; after a transform sync ").Append(SceneHit(ray, mask, box));
            // Switched off and on again, a collider is taken out of the physics scene and put back.
            box.enabled = false;
            box.enabled = true;
            sb.Append("; switched off and on ").Append(SceneHit(ray, mask, box));
            return sb.ToString();
        }

        private static string SceneHit(Ray ray, int mask, Collider want)
        {
            foreach (RaycastHit h in Physics.RaycastAll(ray, 10f, mask, QueryTriggerInteraction.Collide))
                if (h.collider == want) return "hits at y " + h.point.y.ToString("F2");
            return "MISSES";
        }

        private static string Describe(Collider c)
        {
            if (c == null) return "gone (destroyed)";
            var sb = new StringBuilder(160);
            sb.Append(Path(c.transform)).Append(" (").Append(c.GetType().Name);
            if (c is MeshCollider mc)
                sb.Append(" of ").Append(mc.sharedMesh != null ? "'" + mc.sharedMesh.name + "', " + mc.sharedMesh.vertexCount + " vertices" : "NO MESH");
            sb.Append(", layer ").Append(LayerMask.LayerToName(c.gameObject.layer));
            if (!c.enabled) sb.Append(", collider OFF");
            if (!c.gameObject.activeInHierarchy) sb.Append(", object INACTIVE");
            if (c.isTrigger) sb.Append(", trigger");
            Bounds b = c.bounds;
            sb.Append(", bounds ").Append(b.min.ToString("F2")).Append(" to ").Append(b.max.ToString("F2")).Append(')');
            return sb.ToString();
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            for (int i = 0; i < 5 && t.parent != null; i++)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
