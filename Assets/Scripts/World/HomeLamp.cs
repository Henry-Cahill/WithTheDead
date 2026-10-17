using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Rendering;

namespace CloneZ
{
    /// <summary>
    /// A lamp of the wave-survival starter home — bedside lamp, standard lamp, wall sconce or porch
    /// lantern: a warm <see cref="RoomLight"/> that switches itself on from dusk to dawn, together with
    /// an optional glowing shell drawn just over the lamp's daytime shade or globe, shown only while the
    /// lamp is lit. Created with <see cref="Attach"/>; it lives on the light's own object, so it goes when
    /// the furniture or wall it hangs on is dismantled.
    /// </summary>
    [DisallowMultipleComponent]
    public class HomeLamp : MonoBehaviour
    {
        /// <summary>Daylight (<see cref="TimeOfDay.LightLevel"/>, 1 at noon) below which the lamps are on.</summary>
        public const float SwitchOnBelow = 0.45f;
        private const float CheckInterval = 0.5f;

        private RoomLight _light;
        private Renderer _glow;
        private float _next;

        /// <summary>True while the lamp is switched on.</summary>
        public bool IsOn => _light != null && _light.On;

        /// <summary>Hangs a lamp's light at <paramref name="bulb"/> (world) under <paramref name="parent"/>,
        /// with <paramref name="glow"/> (world-space geometry, may be null) as its lit shade.</summary>
        internal static HomeLamp Attach(Transform parent, Vector3 bulb, float range, float intensity, bool shadows, RaidMesh glow)
        {
            RoomLight light = RoomLight.Spawn(parent, bulb, RoomLight.DefaultColor, range, intensity, false, shadows);
            var lamp = light.gameObject.AddComponent<HomeLamp>();
            lamp._light = light;
            if (glow != null && !glow.IsEmpty)
            {
                GameObject g = glow.Emit("Glow", light.transform, bulb, ShadowCastingMode.Off);
                g.transform.rotation = Quaternion.identity;   // the geometry is in world axes
                g.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
                lamp._glow = g.GetComponent<Renderer>();
                lamp._glow.receiveShadows = false;
            }
            lamp.Refresh();
            return lamp;
        }

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + CheckInterval;
            Refresh();
        }

        private void Refresh()
        {
            TimeOfDay tod = TimeOfDay.Instance;
            bool on = tod == null || tod.LightLevel < SwitchOnBelow;
            if (_light != null && _light.On != on) _light.On = on;
            if (_glow != null && _glow.enabled != on) _glow.enabled = on;
        }
    }
}
