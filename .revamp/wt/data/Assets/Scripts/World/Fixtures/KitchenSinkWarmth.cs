using UnityEngine;
using CloneZ;

namespace CloneZ
{
    // Provides a tiny warmth source when the sink’s tap is running.
    public class KitchenSinkWarmth : MonoBehaviour, IWarmthSource
    {
        private KitchenSink _sink;
        // Warmth radius – 2 m is enough to reach a standing survivor.


        public void Initialize(KitchenSink sink) => _sink = sink;

        // Warmth radius – 2 m is enough to reach a standing survivor.
        private const float kWarmthRadius = 2f;
        // Warmth amount – a small positive value that the HomeComfort system adds to the player.
        private const float kWarmthValue = 0.12f;
        
        // IWarmthSource implementation
        public Vector3 WarmthPosition => _sink != null ? _sink.transform.position : Vector3.zero;
        public float WarmthRadius => kWarmthRadius;
        public float Warmth => (_sink != null && _sink.IsRunning) ? kWarmthValue : 0f;

        private void OnEnable() => HomeComfort.Register(this);
        private void OnDisable() => HomeComfort.Unregister(this);
    }
}
