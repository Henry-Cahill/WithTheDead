package withthedead.boot;

import java.io.File;
import java.io.IOException;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.module.Configuration;
import java.lang.module.ModuleFinder;
import java.lang.module.ModuleReference;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Optional;
import java.util.Set;
import java.util.stream.Stream;

/// What the game calls first, over JNI. JNI's `FindClass` only sees the JVM's class path, which is why this is on it;
/// the game calls the class [#open] returns from then on.
public final class Boot {
    private static List<String> leftOut = List.of();

    private Boot() {
    }

    /// Opens a session: loads the modules on `modulePath` (entries separated by [File#pathSeparator], each a module,
    /// packaged or exploded, or a directory of them) into a new layer above the boot layer, with a class loader per
    /// module, and returns `className` from the module `moduleName` in it. Once nothing refers to the layer any more, the
    /// JVM can unload all of its classes.
    ///
    /// `moduleName` and what it needs come first, and fail the session if they can't be loaded. Every other module joins
    /// only if it can be read, isn't one already there, and resolves (it may need a module the runtime lacks, say);
    /// [#leftOut] says why a module didn't, and the session goes on without it.
    public static Class<?> open(String modulePath, String moduleName, String className) throws ClassNotFoundException {
        var reasons = new ArrayList<String>();
        ModuleLayer boot = ModuleLayer.boot();
        Map<String, ModuleReference> found = new LinkedHashMap<>();
        for (Path candidate : candidates(modulePath)) {
            try {
                for (ModuleReference reference : ModuleFinder.of(candidate).findAll()) {
                    String name = reference.descriptor().name();
                    if (boot.findModule(name).isPresent()) {
                        continue;
                    }
                    if (found.putIfAbsent(name, reference) != null) {
                        reasons.add(name + " (" + candidate + "): another " + name + " came first");
                    }
                }
            } catch (RuntimeException failure) {
                reasons.add(candidate + ": " + failure.getMessage());
            }
        }

        ModuleFinder finder = finderOf(found);
        Configuration parent = boot.configuration();
        Set<String> roots = new LinkedHashSet<>(List.of(moduleName));
        Configuration configuration = parent.resolve(finder, ModuleFinder.of(), roots);
        for (String name : found.keySet()) {
            if (configuration.findModule(name).isPresent()) {
                continue;
            }
            var withIt = new LinkedHashSet<>(roots);
            withIt.add(name);
            try {
                configuration = parent.resolve(finder, ModuleFinder.of(), withIt);
                roots = withIt;
            } catch (RuntimeException failure) {
                reasons.add(name + ": " + failure.getMessage());
            }
        }
        leftOut = List.copyOf(reasons);

        ModuleLayer layer = boot.defineModulesWithManyLoaders(configuration, ClassLoader.getSystemClassLoader());
        Module module = layer.findModule(moduleName)
                .orElseThrow(() -> new ClassNotFoundException("No module " + moduleName + " on " + modulePath));
        Class<?> type = Class.forName(module, className);
        if (type == null) {
            throw new ClassNotFoundException("No class " + className + " in " + moduleName);
        }
        return type;
    }

    /// The modules the last [#open] left out, and why, one per line; empty when it left none out.
    public static String leftOut() {
        return String.join("\n", leftOut);
    }

    /// A Java exception as the game logs it: the stack trace with its causes, lines ending in `\n`.
    public static String describe(Throwable failure) {
        var text = new StringWriter();
        failure.printStackTrace(new PrintWriter(text));
        return text.toString().stripTrailing().replace("\r\n", "\n");
    }

    // Each module the path names, in its order: an exploded module, a jar, or each of those in a directory (by name).
    private static List<Path> candidates(String modulePath) {
        var candidates = new ArrayList<Path>();
        for (String entry : modulePath.split(File.pathSeparator)) {
            if (entry.isBlank()) {
                continue;
            }
            Path path = Path.of(entry);
            if (isModule(path)) {
                candidates.add(path);
            } else if (Files.isDirectory(path)) {
                try (Stream<Path> children = Files.list(path)) {
                    children.filter(Boot::isModule).sorted().forEach(candidates::add);
                } catch (IOException failure) {
                    throw new IllegalStateException("Couldn't list " + path, failure);
                }
            }
        }
        return candidates;
    }

    private static boolean isModule(Path path) {
        return Files.isDirectory(path) ? Files.isRegularFile(path.resolve("module-info.class"))
                : Files.isRegularFile(path) && path.getFileName().toString().endsWith(".jar");
    }

    private static ModuleFinder finderOf(Map<String, ModuleReference> references) {
        Set<ModuleReference> all = Set.copyOf(references.values());
        return new ModuleFinder() {
            @Override
            public Optional<ModuleReference> find(String name) {
                return Optional.ofNullable(references.get(name));
            }

            @Override
            public Set<ModuleReference> findAll() {
                return all;
            }
        };
    }
}
