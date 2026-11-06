package withthedead.api;

/// What sort of raid a run into the Zone is.
public enum RaidKind {
    /// Your own character with your own kit: everything carried is at risk.
    OPERATIVE,
    /// A free run with random gear among the scavengers; the stash is never at risk.
    SCAV,
    /// The persistent open zone: no dawn deadline.
    OPEN_ZONE,
    /// Playing one of the Zone's horrors, invading someone else's raid.
    MONSTER,
}
