/// The one module of ours the JVM starts with. Everything else (the scripting API, the host the game calls and every
/// script) is loaded per session by [withthedead.boot.Boot], in a module layer of its own, so each session runs the
/// latest build of them without restarting the JVM: in the editor, every press of Play.
module withthedead.boot {
}
