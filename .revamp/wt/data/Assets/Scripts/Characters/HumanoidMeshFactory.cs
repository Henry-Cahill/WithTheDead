using System.Collections.Generic;
using UnityEngine;

namespace CloneZ
{
    /// <summary>A baked humanoid mesh and the colour region drawn by each of its submeshes.</summary>
    internal sealed class HumanoidMesh
    {
        public HumanoidShape Shape;
        public Mesh Mesh;
        public HumanoidSlot[] Slots;
        public int RefCount;
        public LinkedListNode<HumanoidMesh> IdleNode;
    }

    /// <summary>
    /// Builds humanoid meshes on demand and shares them between every character of the same
    /// <see cref="HumanoidShape"/>. Meshes are reference counted: when the last character using one
    /// goes away it is parked rather than destroyed, so a horde that keeps respawning in the same
    /// clothes doesn't rebuild them, and only the oldest parked meshes are freed.
    /// </summary>
    internal static class HumanoidMeshFactory
    {
        private const int MaxIdle = 24;

        private static readonly Dictionary<HumanoidShape, HumanoidMesh> s_Cache = new Dictionary<HumanoidShape, HumanoidMesh>(64);
        private static readonly LinkedList<HumanoidMesh> s_Idle = new LinkedList<HumanoidMesh>();

        public static HumanoidMesh Acquire(HumanoidShape shape)
        {
            if (!s_Cache.TryGetValue(shape, out HumanoidMesh m) || m.Mesh == null)
            {
                if (m != null && m.IdleNode != null) { s_Idle.Remove(m.IdleNode); m.IdleNode = null; }
                m = new HumanoidMesh { Shape = shape };
                new HumanoidBodyBuilder(shape).Build(m);
                s_Cache[shape] = m;
            }

            if (m.IdleNode != null) { s_Idle.Remove(m.IdleNode); m.IdleNode = null; }
            m.RefCount++;
            return m;
        }

        public static void Release(HumanoidMesh m)
        {
            if (m == null || m.RefCount <= 0) return;
            if (--m.RefCount > 0) return;

            m.IdleNode = s_Idle.AddLast(m);
            while (s_Idle.Count > MaxIdle)
            {
                HumanoidMesh old = s_Idle.First.Value;
                s_Idle.RemoveFirst();
                old.IdleNode = null;
                if (s_Cache.TryGetValue(old.Shape, out HumanoidMesh current) && current == old) s_Cache.Remove(old.Shape);
                if (old.Mesh != null)
                {
                    // Runtime meshes are not garbage collected, so they have to be destroyed by hand.
                    if (Application.isPlaying) Object.Destroy(old.Mesh);
                    else Object.DestroyImmediate(old.Mesh);
                }
                old.Mesh = null;
            }
        }
    }
}
