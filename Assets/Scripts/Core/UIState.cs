namespace CloneZ
{
    /// <summary>
    /// Global UI/input flags. When <see cref="GameplayBlocked"/> is set (e.g. while
    /// the inventory panel is open) the player scripts stop reading movement and
    /// attack input, so mouse clicks land on the UI instead of swinging a weapon.
    /// <see cref="BuildMode"/> holds combat (but not movement) while the survivor is
    /// placing barricades, so a left-click builds instead of attacking.
    /// <see cref="ActionMenuOpen"/> is set while a fixture's action menu is up, and
    /// <see cref="EscapeConsumedFrame"/> marks a frame whose Esc already closed something,
    /// so the pause menu doesn't pop open on the same press.
    /// </summary>
    public static class UIState
    {
        public static bool GameplayBlocked;
        public static bool BuildMode;
        public static bool ActionMenuOpen;
        public static int EscapeConsumedFrame = -1;
    }
}
