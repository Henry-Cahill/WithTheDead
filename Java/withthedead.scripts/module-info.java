/// The game's own Java scripts. A mod is a module like this one: its jar goes in beside this one's (Java/scripts in a
/// build), and every GameScript it provides runs in each session.
module withthedead.scripts {
    requires withthedead.api;

    provides withthedead.api.GameScript with withthedead.scripts.RaidJournal;
}
