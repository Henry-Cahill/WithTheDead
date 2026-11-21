using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// Small helpers shared by the bathroom's fixtures (<see cref="ToiletFixture"/>, <see cref="BasinFixture"/>,
    /// <see cref="ShowerFixture"/>, <see cref="TowelRail"/>, <see cref="MirrorCabinet"/>): a piece of furniture's local
    /// points in the world, emitting a drawn piece the fixture shows and hides, which side of a wall the survivor is on,
    /// and nudging the survivor's nerves.
    /// </summary>
    internal static class BathroomFixtures
    {
        /// <summary>A point given in <paramref name="host"/>'s local frame (front toward +Z, origin on the floor at its
        /// middle), in the world.</summary>
        public static Vector3 Point(HomeFurniture host, Vector3 local)
            => host.transform.position + Quaternion.Euler(0f, host.Yaw, 0f) * local;

        /// <summary>A direction given in <paramref name="host"/>'s local frame, in the world.</summary>
        public static Vector3 Direction(HomeFurniture host, Vector3 local) => Quaternion.Euler(0f, host.Yaw, 0f) * local;

        /// <summary>A world point in <paramref name="host"/>'s local frame.</summary>
        public static Vector3 Local(HomeFurniture host, Vector3 world)
            => Quaternion.Inverse(Quaternion.Euler(0f, host.Yaw, 0f)) * (world - host.transform.position);

        /// <summary>A mesh to draw in <paramref name="host"/>'s local frame (the vertices land in world space, as a
        /// <see cref="FixturePart"/> wants them).</summary>
        public static RaidMesh LocalMesh(HomeFurniture host)
        {
            var m = new RaidMesh();
            m.SetFrame(host.transform.position, host.Yaw);
            return m;
        }

        /// <summary>Emits a drawn piece (world-space geometry) under <paramref name="parent"/> with its origin at
        /// <paramref name="origin"/>, square to the world and kept out of the navmesh bake, showing or not. Returns its
        /// renderer (null when nothing was drawn). Fixtures hide and show such pieces through <see cref="Renderer.enabled"/>
        /// (never SetActive: the camera's cutaway gathers a wall's renderers once).</summary>
        public static MeshRenderer Emit(RaidMesh mesh, string name, Transform parent, Vector3 origin, bool shadows, bool shown)
        {
            GameObject go = mesh.Emit(name, parent, origin, shadows ? ShadowCastingMode.On : ShadowCastingMode.Off);
            if (go == null) return null;
            go.transform.rotation = Quaternion.identity;
            go.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            DecorPalette.Apply();
            var r = go.GetComponent<MeshRenderer>();
            if (r != null) r.enabled = shown;
            return r;
        }

        /// <summary>True when the survivor stands in front of the plane through <paramref name="face"/> facing
        /// <paramref name="normal"/> (or there's no survivor): belt and braces with the interactor's sight line, so a
        /// wall fitting can't be used from the far side of its wall.</summary>
        public static bool SurvivorInFront(Vector3 face, Vector3 normal)
        {
            PlayerInteractor pi = PlayerInteractor.Current;
            if (pi == null) return true;
            Vector3 d = pi.transform.position - face;
            d.y = 0f;
            return Vector3.Dot(d, normal) > 0f;
        }

        /// <summary>Settles (<paramref name="delta"/> &lt; 0) or rattles the survivor's nerves, on the 0..100 panic scale.</summary>
        public static void Nerves(InteractContext ctx, float delta)
        {
            if (ctx.Stats != null) ctx.Stats.Panic = Mathf.Clamp(ctx.Stats.Panic + delta, 0f, CloneZConfig.PanicMax);
        }

        /// <summary>The distance between two points, ignoring height.</summary>
        public static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
