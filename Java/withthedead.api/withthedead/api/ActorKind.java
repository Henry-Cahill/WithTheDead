package withthedead.api;

/// What sort of creature an [Actor] is.
public enum ActorKind {
    /// The survivor this machine plays.
    PLAYER,
    /// Another player's survivor.
    REMOTE_PLAYER,
    ZOMBIE,
    /// The Zone's people: stalkers, bandits, traders.
    HUMAN,
    MUTANT,
    ANIMAL,
    /// A player playing one of the Zone's horrors.
    PLAYER_MONSTER,
    /// Someone the survivor hired to come along.
    COMPANION,
}
