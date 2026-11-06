package withthedead.selftest;

import module withthedead.api;

import withthedead.api.GameEvent.DayAdvanced;

/// Throws on `DayAdvanced(13)`: the self-test checks the failure is logged and the other scripts still get their events.
public final class Faulty implements GameScript {
    @Override
    public void on(GameEvent event) {
        if (event instanceof DayAdvanced(var day) && day == 13) {
            throw new IllegalStateException("selftest: a deliberate failure");
        }
    }
}
