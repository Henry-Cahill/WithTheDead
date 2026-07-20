using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// A smooth, fixed-angle overhead camera reminiscent of Project Zomboid's
    /// isometric view, translated into 3D. Follows the target without rotating.
    /// </summary>
    public class IsoCameraFollow : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 22f, -12f);
        public float FollowDamping = 8f;
        public float LookHeight = 1.0f;

        private Vector3 _velocity;

        public void Snap()
        {
            if (Target == null) return;
            transform.position = Target.position + Offset;
            transform.LookAt(Target.position + Vector3.up * LookHeight);
        }

        private void LateUpdate()
        {
            if (Target == null) return;

            Vector3 desired = Target.position + Offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 1f / Mathf.Max(0.01f, FollowDamping));
            transform.rotation = Quaternion.LookRotation((Target.position + Vector3.up * LookHeight) - transform.position, Vector3.up);
        }
    }
}
