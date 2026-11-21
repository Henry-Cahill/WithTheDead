using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Marks a wall (or any tall piece of building) that the isometric camera may cut away when it
    /// stands between the camera and the survivor, PZ-style, so the inside of a room stays visible.
    /// The raid map adds it to its wall segments, and every house wall, doorway, window and roof
    /// (<see cref="BuildPiece"/>) and every <see cref="Barricade"/> carries one; the camera's cutaway
    /// (<see cref="IsoCutaway"/>) reads it, and the build tool won't pick a piece it has cut away through
    /// the gap. Every mesh renderer
    /// under the marked object that rises above the floor hides with it
    /// (wallpaper, wainscot, vines, posters — not skirting or rugs). A cut-away occluder stops drawing but
    /// keeps casting shadows and blocking light, and its colliders keep blocking movement, shots and
    /// sight. A disabled marker exempts its piece from the cutaway.
    /// </summary>
    [DisallowMultipleComponent]
    public class CutawayOccluder : MonoBehaviour
    {
        /// <summary>True while the isometric camera has this piece cut away.</summary>
        public bool IsCutAway { get; internal set; }

        /// <summary>Moves on with every <see cref="PartsChanged"/>, telling the cutaway to gather the piece's renderers again.</summary>
        internal int PartsVersion { get; private set; }

        /// <summary>Call after rebuilding the meshes under the marked object (a <see cref="Barricade"/> nailing on or
        /// losing a plank), so that while it's cut away its new parts hide with it.</summary>
        public void PartsChanged() => PartsVersion++;
    }
}
