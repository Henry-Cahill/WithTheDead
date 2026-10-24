using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Runs the Kentucky map's isometric cutaway (<see cref="MapCutaway"/>) once a frame, after the player camera has
    /// placed itself for the frame (<see cref="IsoCameraFollow"/> runs at 50). Made when the first site registers; one
    /// per scene.
    /// </summary>
    [DefaultExecutionOrder(60)]
    [DisallowMultipleComponent]
    public sealed class MapCutawayDriver : MonoBehaviour
    {
        private void LateUpdate() => MapCutaway.Tick();

        private void OnDisable() => MapCutaway.RestoreAll();

        private void OnDestroy() => MapCutaway.LogSession();
    }
}
