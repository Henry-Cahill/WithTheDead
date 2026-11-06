package withthedead.host;

import java.io.ByteArrayOutputStream;
import java.io.OutputStream;
import java.io.PrintStream;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Locale;
import java.util.Objects;
import java.util.Optional;
import java.util.PriorityQueue;
import java.util.Queue;
import java.util.ServiceLoader;
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.function.Consumer;
import java.util.function.Supplier;
import java.util.regex.Pattern;
import withthedead.api.Actor;
import withthedead.api.ActorKind;
import withthedead.api.Game;
import withthedead.api.GameEvent;
import withthedead.api.GameScript;
import withthedead.api.RaidKind;
import withthedead.api.RaidOutcome;

/// A session, seen from Java. The game (Assets/Scripts/Java/JavaSession.cs) calls the private static methods here over
/// JNI, always on its main thread, and implements the native ones. Every session loads this class afresh, in a module
/// layer of its own (see withthedead.boot), so its statics are the session's state.
final class Host {
    private static final int INFO = 0, WARNING = 1, ERROR = 2;
    private static final Pattern WORD_START = Pattern.compile("(?<=[a-z0-9])(?=[A-Z])");

    private record Script(String name, GameScript script) {
    }

    private record Timer(long due, long order, Runnable action) {
    }

    private static final List<Script> scripts = new ArrayList<>();
    private static final Queue<Runnable> fromOtherThreads = new ConcurrentLinkedQueue<>();
    private static final PriorityQueue<Timer> timers =
            new PriorityQueue<>(Comparator.comparingLong(Timer::due).thenComparingLong(Timer::order));
    private static long timersMade;
    private static volatile Thread gameThread;
    private static PrintStream outBefore, errBefore;
    private static Thread.UncaughtExceptionHandler uncaughtBefore;

    private Host() {
    }

    // The game's side (JavaSession.cs registers them).
    private static native void log(int level, String message);

    private static native void toast(String message);

    private static native void subtitle(String speaker, String line);

    /// Starts the session: makes every script its modules provide and starts them, in the order of their class names.
    /// Returns the scripts' names, one per line.
    private static String start(String version) {
        gameThread = Thread.currentThread();
        outBefore = System.out;
        errBefore = System.err;
        uncaughtBefore = Thread.getDefaultUncaughtExceptionHandler();
        System.setOut(new PrintStream(new Lines(INFO), true, StandardCharsets.UTF_8));
        System.setErr(new PrintStream(new Lines(WARNING), true, StandardCharsets.UTF_8));
        Thread.setDefaultUncaughtExceptionHandler(
                (thread, failure) -> report(ERROR, "Uncaught in the thread \"" + thread.getName() + "\"", failure));

        ServiceLoader.load(Host.class.getModule().getLayer(), GameScript.class).stream()
                .sorted(Comparator.comparing(provider -> provider.type().getName()))
                .forEach(provider -> {
                    String name = provider.type().getModule().getName() + "/" + provider.type().getSimpleName();
                    try {
                        scripts.add(new Script(name, provider.get()));
                    } catch (Throwable failure) {
                        report(ERROR, name + " could not be made", failure);
                    }
                });
        for (Script script : scripts) {
            Game game = new GameApi(version, script.name());
            call(script, () -> "start", s -> s.start(game));
        }
        return String.join("\n", scripts.stream().map(Script::name).toList());
    }

    /// Ends the session: stops the scripts, the last started first, and puts back what [#start] changed.
    private static void stop() {
        for (Script script : scripts.reversed()) {
            call(script, () -> "stop", GameScript::stop);
        }
        gameThread = null;
        scripts.clear();
        timers.clear();
        fromOtherThreads.clear();
        System.setOut(outBefore);
        System.setErr(errBefore);
        Thread.setDefaultUncaughtExceptionHandler(uncaughtBefore);
    }

    /// Carries out what other threads asked of the game, then the timers that are due. The game calls it every frame.
    private static void pump() {
        for (Runnable call; (call = fromOtherThreads.poll()) != null; ) {
            guarded("Java", "a call from another thread", call);
        }
        // Timers set while these run wait for the next frame, even with no delay.
        long now = System.nanoTime(), made = timersMade;
        while (!timers.isEmpty() && timers.peek().due() - now <= 0 && timers.peek().order() < made) {
            timers.poll().action().run();
        }
    }

    private static void playerSpawned() {
        dispatch(GameEvent.PlayerSpawned::new);
    }

    private static void raidStarted(String zone, String kind) {
        dispatch(() -> new GameEvent.RaidStarted(zone, constant(RaidKind.class, kind)));
    }

    private static void raidEnded(String outcome, String exit) {
        dispatch(() -> new GameEvent.RaidEnded(constant(RaidOutcome.class, outcome), exit));
    }

    // An empty killerKind: nobody did it.
    private static void actorKilled(String victimName, String victimKind, String victimFaction,
                                    String killerName, String killerKind, String killerFaction,
                                    String weapon, boolean surrendered) {
        dispatch(() -> new GameEvent.ActorKilled(
                new Actor(victimName, constant(ActorKind.class, victimKind), victimFaction),
                killerKind.isEmpty()
                        ? Optional.empty()
                        : Optional.of(new Actor(killerName, constant(ActorKind.class, killerKind), killerFaction)),
                weapon, surrendered));
    }

    private static void itemLooted(String item, String name, int count, String source) {
        dispatch(() -> new GameEvent.ItemLooted(item, name, count, source));
    }

    private static void itemCrafted(String item, int count) {
        dispatch(() -> new GameEvent.ItemCrafted(item, count));
    }

    private static void locationVisited(String location) {
        dispatch(() -> new GameEvent.LocationVisited(location));
    }

    private static void emissionChanged(boolean active) {
        dispatch(() -> new GameEvent.EmissionChanged(active));
    }

    private static void artifactLearned(String artifact, String property) {
        dispatch(() -> new GameEvent.ArtifactLearned(artifact, property));
    }

    private static void dayAdvanced(int day) {
        dispatch(() -> new GameEvent.DayAdvanced(day));
    }

    private static void dispatch(Supplier<GameEvent> make) {
        GameEvent event;
        try {
            event = make.get();
        } catch (RuntimeException failure) {
            report(ERROR, "An event from the game didn't match withthedead.api", failure);
            return;
        }
        for (Script script : scripts) {
            call(script, () -> "on(" + event + ")", s -> s.on(event));
        }
    }

    /// The Java constant named like a C# enum value: `OpenZone` is `OPEN_ZONE`.
    private static <E extends Enum<E>> E constant(Class<E> type, String name) {
        return Enum.valueOf(type, WORD_START.matcher(name).replaceAll("_").toUpperCase(Locale.ROOT));
    }

    private static void onGameThread(Runnable call) {
        Thread thread = gameThread;
        if (thread == null) {
            return;   // the session is over
        }
        if (Thread.currentThread() == thread) {
            call.run();
        } else {
            fromOtherThreads.add(call);
        }
    }

    private static void call(Script script, Supplier<String> what, Consumer<GameScript> action) {
        try {
            action.accept(script.script());
        } catch (Throwable failure) {
            report(ERROR, script.name() + " failed in " + what.get(), failure);
        }
    }

    private static void guarded(String who, String what, Runnable action) {
        try {
            action.run();
        } catch (Throwable failure) {
            report(ERROR, who + " failed in " + what, failure);
        }
    }

    private static void report(int level, String message, Throwable failure) {
        var trace = new StringWriter();
        failure.printStackTrace(new PrintWriter(trace));
        String text = message + ": " + trace.toString().stripTrailing().replace("\r\n", "\n");
        onGameThread(() -> log(level, text));
    }

    /// The [Game] a script gets. Its failures in timers are logged under the script's name.
    private record GameApi(String version, String script) implements Game {
        @Override
        public void toast(String message) {
            Objects.requireNonNull(message, "message");
            onGameThread(() -> Host.toast(message));
        }

        @Override
        public void subtitle(String speaker, String line) {
            Objects.requireNonNull(speaker, "speaker");
            Objects.requireNonNull(line, "line");
            onGameThread(() -> Host.subtitle(speaker, line));
        }

        @Override
        public void log(String message) {
            Objects.requireNonNull(message, "message");
            onGameThread(() -> Host.log(INFO, message));
        }

        @Override
        public void after(Duration delay, Runnable action) {
            Objects.requireNonNull(action, "action");
            long due = System.nanoTime() + delay.toNanos();
            onGameThread(() -> timers.add(new Timer(due, timersMade++, () -> guarded(script, "a timer", action))));
        }
    }

    /// What a session's System.out and System.err write to: each line goes to the game's log.
    private static final class Lines extends OutputStream {
        private final int level;
        private final ByteArrayOutputStream line = new ByteArrayOutputStream();

        Lines(int level) {
            this.level = level;
        }

        @Override
        public synchronized void write(int b) {
            if (b == '\n') {
                String text = line.toString(StandardCharsets.UTF_8);
                line.reset();
                onGameThread(() -> log(level, text));
            } else if (b != '\r') {
                line.write(b);
            }
        }
    }
}
