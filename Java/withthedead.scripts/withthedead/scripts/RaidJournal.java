package withthedead.scripts;

import module withthedead.api;

import java.time.Duration;
import java.time.Instant;
import java.util.HashSet;
import java.util.Set;
import withthedead.api.GameEvent.ActorKilled;
import withthedead.api.GameEvent.ItemLooted;
import withthedead.api.GameEvent.LocationVisited;
import withthedead.api.GameEvent.RaidEnded;
import withthedead.api.GameEvent.RaidStarted;

/// Keeps a journal of each raid (what the survivor killed, how much they picked up, the places they reached) and sums
/// it up on the HUD as the raid ends.
public final class RaidJournal implements GameScript {
    private Game game;
    private Raid raid;   // the raid under way; null between raids

    private static final class Raid {
        private final Instant started = Instant.now();
        private final Set<String> places = new HashSet<>();
        private int killed, items;
    }

    @Override
    public void start(Game game) {
        this.game = game;
        game.log("RaidJournal is keeping the journal, on Java " + Runtime.version());
    }

    @Override
    public void on(GameEvent event) {
        switch (event) {
            case RaidStarted _ -> raid = new Raid();
            case ActorKilled(_, var killer, _, _) when raid != null
                    && killer.filter(actor -> actor.kind() == ActorKind.PLAYER).isPresent() -> raid.killed++;
            case ItemLooted(_, _, var count, _) when raid != null -> raid.items += count;
            case LocationVisited(var place) when raid != null -> raid.places.add(place);
            case RaidEnded(var outcome, var exit) when raid != null -> {
                game.toast(summary(raid, outcome, exit));
                raid = null;
            }
            default -> {
            }
        }
    }

    @Override
    public void stop() {
        raid = null;
    }

    private static String summary(Raid raid, RaidOutcome outcome, String exit) {
        String ending = switch (outcome) {
            case EXTRACTED -> exit.isEmpty() ? "Extracted" : "Out through " + exit;
            case KILLED -> "Killed";
            case MISSING_IN_ACTION -> "Missing in action";
            case ABANDONED -> "Raid abandoned";
        };
        long minutes = Duration.between(raid.started, Instant.now()).toMinutes();
        return "%s: %d killed, %d picked up, %d %s found, %d min".formatted(ending, raid.killed, raid.items,
                raid.places.size(), raid.places.size() == 1 ? "place" : "places", minutes);
    }
}
