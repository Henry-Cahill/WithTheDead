# Java scripting (Java 27)

The game runs gameplay scripts written in **Java 27** inside its own process. A HotSpot JVM (Eclipse Temurin, the
release pinned in [jdk.json](jdk.json)) is embedded through JNI: C# loads the JVM library, makes the JVM once per process
and calls into it on the main thread; Java calls back through native methods bound to C# delegates. Scripts see a small,
typed API: a `GameScript` gets the game's events as sealed records and acts on the game through `Game`.

| Where | What |
|---|---|
| [withthedead.api](withthedead.api) | What scripts compile against: `GameScript`, `Game`, the sealed `GameEvent` records, `Actor` and the enums. |
| [withthedead.scripts](withthedead.scripts) | The game's own scripts: `RaidJournal` sums up each raid on the HUD as it ends. |
| [withthedead.host](withthedead.host) | The bridge the game calls: starts and stops the scripts, turns the game's events into records, carries out the scripts' calls on the main thread. Exports nothing. |
| [withthedead.boot](withthedead.boot) | The one class the JVM starts with (on its class path): opens each session's module layer. |
| [withthedead.selftest](withthedead.selftest) | Scripts for the self-test only; never in a session or a build. |
| [Assets/Scripts/Java](../Assets/Scripts/Java) | `Jni` (the JNI binding, no unsafe code), `JavaSession` (one session), `JavaScripting` (starts it with the game, forwards `GameEvents`). |
| [Assets/Editor/JavaBuild.cs](../Assets/Editor/JavaBuild.cs) | Compiles `Java/` before Play and before builds; packs builds. `JavaSelfTest.cs` checks the host. |
| [Tools/Java](../Tools/Java) | `Install-Jdk.ps1` (the JDK) and `SelfTest.ps1` (the self-test, headless). |

## Setting up

```powershell
powershell -ExecutionPolicy Bypass -File Tools\Java\Install-Jdk.ps1
```

installs the pinned JDK, checked against its SHA-256, into `%LOCALAPPDATA%\WithTheDead\Java\<release>\windows`; set
`CLONEZ_JDK` to use another JDK of the same version. Without one the editor warns at each Play and the game plays on
without its scripts. `Tools\Server\Release.ps1` installs it itself.

## How it runs

- **In the editor**, Play compiles `Java/` with javac (`--release 27`) into `Library/Java/classes` if a source changed
  (about a second), and `JavaScripting` starts a session on the JDK's own JVM. A process gets one JVM, ever: it's made at
  the first Play and lives until the editor closes, so its options (and `withthedead.boot`) change only with a restart.
  Everything else is loaded into a fresh module layer each session, so every Play runs the latest build; a C# script
  reload leaves the JVM running and the next Play finds it again. CloneZ ▸ Java ▸ Build Scripts compiles by hand.
- **A Windows build** carries `WithTheDead_Data/Java`: `runtime/` (linked by jlink from the JDK's own image with only the
  JDK modules the game's modules use, about 31 MB for `java.base`), `boot.jar`, `lib/` (the API and the host) and
  `scripts/`. A build that can't compile its Java fails.
- **The Linux dedicated server** carries the same jars and `Java/runtime.env`. A Linux runtime can't be linked on Windows
  (Temurin ships no jmods), so the server's Docker image ([Dockerfile](../Tools/Server/docker/Dockerfile)) links it on
  Linux from the pinned Linux JDK. Run outside that image, the server plays on without its scripts.
- Scripts get every event on the main thread, one at a time. What a script asks of the game from another thread (a
  virtual thread it started, say) is carried out on the main thread at the next frame, as are `Game.after` timers. A
  script that throws is logged and goes on getting events; the others don't notice.
- The JVM gets the serial collector and a 16–256 MB heap (no collector threads competing with the game's), `-Xrs` (the
  game keeps its signals), and writes its own warnings to `Logs/Java.log` in the editor or `Java.log` beside
  `Player.log` in a build. Scripts' log lines and `System.out` show as `[Java] ...` in the game's log.
- `-clonez-no-java` turns it off.

## Writing a script

A script is a module that requires `withthedead.api` and provides a `GameScript`. Put it in `Java/<module name>/` and
press Play:

```java
module my.mod {
    requires withthedead.api;

    provides withthedead.api.GameScript with my.mod.Welcome;
}
```

```java
package my.mod;

import module withthedead.api;

public final class Welcome implements GameScript {
    private Game game;

    @Override
    public void start(Game game) {
        this.game = game;
    }

    @Override
    public void on(GameEvent event) {
        switch (event) {
            case GameEvent.RaidStarted(var zone, var kind) -> game.toast("Into " + zone + " (" + kind + ")");
            case GameEvent.ActorKilled kill when kill.victim().kind() == ActorKind.ZOMBIE -> game.log("One less");
            default -> { }
        }
    }
}
```

The events are `PlayerSpawned`, `RaidStarted`, `RaidEnded`, `ActorKilled`, `ItemLooted`, `ItemCrafted`,
`LocationVisited`, `EmissionChanged`, `ArtifactLearned` and `DayAdvanced`, from the game's `GameEvents`. `Game` offers
`toast`, `subtitle`, `log`, `after(Duration, Runnable)` and `version`. A new event takes a record in
[GameEvent.java](withthedead.api/withthedead/api/GameEvent.java), a method in
[Host.java](withthedead.host/withthedead/host/Host.java), one in `JavaSession` and the forwarding in `JavaScripting`.

**Mods**: a modular jar dropped into a build's `WithTheDead_Data/Java/scripts` runs like the game's own scripts. A mod
that can't be read, is there twice, or doesn't resolve is left out with a warning in the log ("Left out of the
session: ..."), and the rest run. The runtime holds only the JDK modules the game's own modules need (`java.base`
today), so a mod that requires another (`java.net.http`, say) is left out of builds until one of the game's modules
needs it too; the editor's JDK has them all. Scripts must stop any thread they start in `stop()`: the session's
classes are unloaded with it.

## Debugging

Set `CLONEZ_JAVA_DEBUG=5005` before starting the editor (or pass `-clonez-java-debug 5005` to a game run on a JDK) and
attach a Java debugger (VS Code, IntelliJ IDEA) to `127.0.0.1:5005`. Builds' runtimes carry no debugger agent.

## Checking it

- `powershell -ExecutionPolicy Bypass -File Tools\Java\SelfTest.ps1` builds the scripts and runs a session in a batch
  editor's own process with the self-test's scripts: every event, every part of `Game`, a script that throws, two mods
  that can't join, a session that can't open, then a C# domain reload and the same again on the JVM that outlived it.
  Exits 0 when everything passed;
  `-Project` runs it in a copy of the project while this one is open. CloneZ ▸ Java ▸ Self Test runs a round in an open
  editor.
- Every smoke test (`Tools\Smoke\Smoke.ps1`) ends with a `java scripts` check: running, the events they got, no errors.
- `-clonez-java-check` makes Mono catch a `NullReferenceException` and collect garbage with the JVM running, then calls
  Java again, at startup: Mono and HotSpot share the process's signal handlers, which matters most on Linux.

## Things to know

- **Java 27 isn't an LTS release**: its updates end in March 2027, when Java 28 ships (Java 25 is the LTS). To move on,
  pin the new release in `jdk.json` (the release name, and each archive's URL and SHA-256 from
  [api.adoptium.net](https://api.adoptium.net)) and run `Install-Jdk.ps1`; the scripts compile for the new version at the
  next Play.
- HotSpot can't run in a process that enforces CET shadow stacks. Unity's executables don't; `Jni.Open` refuses rather
  than crash if one ever does.
- Temurin is GPLv2 with the Classpath Exception; the runtime ships its notices in `runtime/legal`.
