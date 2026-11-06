package withthedead.api;

/// A gameplay script. When a session starts (the game starting in a build, or Play in the editor) the game makes one of
/// each script it finds, through [java.util.ServiceLoader], so it needs a public no-argument constructor. The game then
/// calls [#start] once, [#on] for every [GameEvent] and [#stop] as the session ends. Every call comes on the game's main
/// thread, one at a time. A script that throws is logged and goes on getting events; the other scripts aren't affected.
public interface GameScript {
    /// The session started. Keep `game` to act on the game later.
    default void start(Game game) {
    }

    /// Something happened in the game; events come in the order the game raised them.
    default void on(GameEvent event) {
    }

    /// The session is ending. Stop any thread the script started: the session's classes are unloaded with it.
    default void stop() {
    }
}
