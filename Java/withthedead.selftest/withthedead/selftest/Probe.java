package withthedead.selftest;

import module withthedead.api;

import java.time.Duration;
import java.util.ArrayList;
import java.util.List;
import withthedead.api.GameEvent.DayAdvanced;

/// Uses every part of [Game] the self-test checks for: a log line, a subtitle, System.out, a timer, a call from a
/// virtual thread, and on `DayAdvanced(1)` a toast listing every event it saw.
public final class Probe implements GameScript {
    private final List<String> seen = new ArrayList<>();
    private Game game;

    @Override
    public void start(Game game) {
        this.game = game;
        game.log("selftest: started on Java " + Runtime.version().feature() + " for " + game.version());
        game.subtitle("Probe", "selftest: subtitle");
        System.out.println("selftest: stdout");
        game.after(Duration.ofMillis(50), () -> game.toast("selftest: timer"));
        Thread.ofVirtual().name("selftest").start(() -> game.toast("selftest: virtual thread"));
    }

    @Override
    public void on(GameEvent event) {
        if (event instanceof DayAdvanced(var day) && day == 1) {
            game.toast("selftest: saw " + String.join(", ", seen));
        } else {
            seen.add(event.toString());
        }
    }

    @Override
    public void stop() {
        game.log("selftest: stopped");
    }
}
