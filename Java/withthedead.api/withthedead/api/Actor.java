package withthedead.api;

/// Anything alive in the game: the survivor, the other players, the dead, the Zone's people, mutants and animals.
/// `name` is what the game shows for them (may be empty); `faction` is their faction's id.
public record Actor(String name, ActorKind kind, String faction) {
}
