package withthedead.api;

import java.time.Duration;

/// What a script can do to the game. Any thread may call these: a call from a thread other than the game's main thread
/// (a virtual thread the script started, say) is carried out on the main thread at its next frame.
public interface Game {
    /// Shows a short message on the HUD.
    void toast(String message);

    /// Shows a subtitle: `line`, spoken by `speaker` (an empty speaker shows the line alone).
    void subtitle(String speaker, String line);

    /// Writes a line to the game's log: the editor's Console, or Player.log in a build.
    void log(String message);

    /// Runs `action` on the game's main thread once `delay` (real time, not game time) has passed.
    void after(Duration delay, Runnable action);

    /// The game's version, as its build reports it.
    String version();
}
