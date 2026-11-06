/// The bridge the game calls over JNI: it starts and stops the session's scripts, turns the game's events into
/// [withthedead.api.GameEvent]s and carries out the scripts' calls on the game's main thread. It exports nothing (JNI
/// needs no exports), so scripts see only withthedead.api.
module withthedead.host {
    requires withthedead.api;

    uses withthedead.api.GameScript;
}
