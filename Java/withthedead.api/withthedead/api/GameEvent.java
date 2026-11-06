package withthedead.api;

import java.util.Optional;

/// Something that happened in the game. The set is closed, so a `switch` over it can be checked for completeness:
///
/// ```java
/// switch (event) {
///     case GameEvent.RaidStarted(var zone, var kind) -> game.log("Into " + zone + " as " + kind);
///     case GameEvent.ActorKilled kill when kill.victim().kind() == ActorKind.ZOMBIE -> zombies++;
///     default -> { }
/// }
/// ```
public sealed interface GameEvent {
    /// The survivor this machine plays spawned.
    record PlayerSpawned() implements GameEvent {
    }

    /// A raid began in the zone with the id `zone`.
    record RaidStarted(String zone, RaidKind kind) implements GameEvent {
    }

    /// The survivor's raid ended; `exit` names the way out they took (empty unless they extracted).
    record RaidEnded(RaidOutcome outcome, String exit) implements GameEvent {
    }

    /// `victim` died, by `killer`'s hand unless nobody did it (a fall, hunger, the Zone itself). `weapon` is the item id
    /// of what did it (empty if none); `surrendered` is true when the victim had their hands up.
    record ActorKilled(Actor victim, Optional<Actor> killer, String weapon, boolean surrendered) implements GameEvent {
    }

    /// The survivor took `count` of the item `item` (its id; `name` is what the game calls it) from `source`.
    record ItemLooted(String item, String name, int count, String source) implements GameEvent {
    }

    /// The survivor crafted `count` of the item `item`.
    record ItemCrafted(String item, int count) implements GameEvent {
    }

    /// The survivor reached the landmark or quest location `location`.
    record LocationVisited(String location) implements GameEvent {
    }

    /// An emission or psi storm began (`active`) or passed.
    record EmissionChanged(boolean active) implements GameEvent {
    }

    /// The survivor learned the property `property` of the artifact `artifact`.
    record ArtifactLearned(String artifact, String property) implements GameEvent {
    }

    /// A new day began in the hub.
    record DayAdvanced(int day) implements GameEvent {
    }
}
