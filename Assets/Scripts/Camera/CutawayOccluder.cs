using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Marks a wall (or any tall piece of building) that the isometric camera may cut away when it
    /// stands between the camera and the survivor, PZ-style, so the inside of a room stays visible.
    /// The raid map adds it to its wall segments; the camera's cutaway (<see cref="IsoCutaway"/>) reads
    /// it. Every mesh renderer under the marked object that rises above the floor hides with it
    /// (wallpaper, wainscot, vines, posters — not skirting or rugs). A cut-away occluder stops drawing but
    /// keeps casting shadows and blocking light, and its colliders keep blocking movement, shots and
    /// sight. A disabled marker exempts its piece from the cutaway.
    /// </summary>
    [DisallowMultipleComponent]
    public class CutawayOccluder : MonoBehaviour
    {
        /// <summary>True while the isometric camera has this piece cut away.</summary>
        public bool IsCutAway { get; internal set; }
    }
}
