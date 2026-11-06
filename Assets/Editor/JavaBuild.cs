using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CloneZ.Java;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Builds the game's Java scripts (Java/README.md) with the JDK that Tools/Java/Install-Jdk.ps1 installs. javac compiles
    /// every module under Java/ into Library/Java/classes whenever a source has changed: before each Play, before each
    /// build, and from CloneZ ▸ Java ▸ Build Scripts. The editor's <see cref="JavaScripting"/> runs the modules from there
    /// on the JDK's own JVM. A Windows build gets them as jars in WithTheDead_Data/Java, with a Java runtime linked for it
    /// from the JDK (only the JDK modules the game's modules use); the Linux dedicated server gets the jars and what its
    /// Docker image needs to link its runtime on Linux.
    /// </summary>
    [InitializeOnLoad]
    public static class JavaBuild
    {
        public const string BootModule = "withthedead.boot", SelfTestModule = "withthedead.selftest";

        // The modules a build keeps in Java/lib; the rest (the scripts) go in Java/scripts, where mods go too.
        private static readonly string[] EngineModules = { "withthedead.api", "withthedead.host" };

        static JavaBuild()
        {
            JavaScripting.EditorLayout = () => Layout(false);
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.ExitingEditMode) return;
                if (!BuildIfStale(out string problem)) Debug.LogWarning("[Java] " + problem);
            };
        }

        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        public static string Sources => Path.Combine(ProjectRoot, "Java");
        public static string Output => Path.Combine(ProjectRoot, "Library", "Java");
        public static string Classes => Path.Combine(Output, "classes");

        /// <summary>The JDK: CLONEZ_JDK when it's set, else the one Tools/Java/Install-Jdk.ps1 installs.</summary>
        public static string Jdk
        {
            get
            {
                string jdk = Environment.GetEnvironmentVariable("CLONEZ_JDK");
                if (!string.IsNullOrEmpty(jdk)) return jdk;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WithTheDead", "Java", Release, "windows");
            }
        }

        [Serializable]
        private class JdkPin
        {
            public string release = "";
            public JdkArchive linux = new JdkArchive();
        }

        [Serializable]
        private class JdkArchive
        {
            public string url = "", sha256 = "";
        }

        private static JdkPin Pin => JsonUtility.FromJson<JdkPin>(File.ReadAllText(Path.Combine(Sources, "jdk.json")));

        /// <summary>The JDK release Java/jdk.json pins, such as "jdk-27+35".</summary>
        private static string Release => Pin.release;

        /// <summary>The Java version the modules are compiled for: the pinned release's feature version.</summary>
        private static string Feature => Release.Substring("jdk-".Length).Split('+', '.')[0];

        private static bool OnWindows => Application.platform == RuntimePlatform.WindowsEditor;

        private static string Tool(string name) => Path.Combine(Jdk, "bin", OnWindows ? name + ".exe" : name);

        private static string NoJdk => "There's no JDK " + Feature + " at " + Jdk
            + ": run Tools\\Java\\Install-Jdk.ps1 (or set CLONEZ_JDK to a JDK " + Feature + ").";

        /// <summary>Where the editor's Java is: the JDK's JVM and the compiled modules, with the self-test's if asked.</summary>
        public static JavaLayout Layout(bool selfTest)
        {
            var layout = new JavaLayout
            {
                JvmLibrary = OnWindows ? Path.Combine(Jdk, "bin", "server", "jvm.dll") : Path.Combine(Jdk, "lib", "server", "libjvm.so"),
                BootPath = Path.Combine(Classes, BootModule),
                ModulePath = string.Join(Path.PathSeparator.ToString(), CompiledModules()
                    .Where(m => m != BootModule && (selfTest || m != SelfTestModule))
                    .Select(m => Path.Combine(Classes, m))),
                JvmLog = Path.Combine(ProjectRoot, "Logs", "Java.log"),
            };
            if (!File.Exists(layout.JvmLibrary)) layout.Missing = NoJdk;
            else if (!Directory.Exists(layout.BootPath)) layout.Missing = "the Java scripts haven't been built (see javac's errors in the Console)";
            return layout;
        }

        [MenuItem("CloneZ/Java/Build Scripts")]
        private static void BuildMenu()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Java] Stop playing first: the session's scripts load their classes from the build.");
                return;
            }
            if (BuildIfStale(out string problem, force: true)) Debug.Log("[Java] Built the Java scripts into " + Classes);
            else Debug.LogError("[Java] " + problem);
        }

        /// <summary>
        /// Compiles the modules under Java/ into Library/Java/classes when a source changed since the last build (or with
        /// <paramref name="force"/>). False, with <paramref name="problem"/> saying why, when there's no JDK or javac
        /// failed; the last good build stays.
        /// </summary>
        public static bool BuildIfStale(out string problem, bool force = false)
        {
            problem = null;
            string[] modules = SourceModules();
            if (modules.Length == 0) return true;
            if (!File.Exists(Tool("javac")))
            {
                problem = NoJdk;
                return false;
            }
            string stamp = Fingerprint();
            string stampFile = Path.Combine(Output, "classes.stamp");
            if (!force && Directory.Exists(Classes) && File.Exists(stampFile) && File.ReadAllText(stampFile) == stamp) return true;

            var watch = Stopwatch.StartNew();
            string fresh = Classes + ".new";
            DeleteDirectory(fresh);
            Directory.CreateDirectory(Output);
            int code = Run("javac", new[]
            {
                "--release", Feature, "-encoding", "UTF-8", "-g", "-Xlint:all",
                "-d", fresh, "--module-source-path", Sources, "--module", string.Join(",", modules),
            }, out string output);
            if (code != 0)
            {
                DeleteDirectory(fresh);
                problem = "javac couldn't compile the Java scripts:\n" + output;
                return false;
            }
            if (output.Length > 0) Debug.LogWarning("[Java] javac:\n" + output);
            DeleteDirectory(Classes);
            Directory.Move(fresh, Classes);
            File.WriteAllText(stampFile, stamp);
            Debug.Log("[Java] Compiled " + modules.Length + " Java modules in " + watch.ElapsedMilliseconds + " ms");
            return true;
        }

        /// <summary>
        /// Puts Java into the build whose data folder is <paramref name="dataDir"/>: withthedead.boot as Java/boot.jar,
        /// withthedead.api and withthedead.host in Java/lib and the scripts in Java/scripts (not the self-test's), and the
        /// Java runtime. A Windows build gets the runtime in Java/runtime, linked here (<see cref="LinkRuntime"/>). A Linux
        /// runtime can't be linked on Windows (Temurin ships no jmods, and jlink links from its own image only for its
        /// own platform), so a Linux build gets Java/runtime.env instead: the pinned Linux JDK and the JDK modules to link,
        /// which its Docker image links on Linux (Tools/Server/docker/Dockerfile).
        /// </summary>
        public static void Package(string dataDir, bool windows)
        {
            string java = Path.Combine(dataDir, "Java");
            DeleteDirectory(java);
            Directory.CreateDirectory(java);
            string[] modules = CompiledModules().Where(m => m != SelfTestModule).ToArray();
            string needed = RuntimeModules(modules);
            if (windows)
            {
                CopyDirectory(LinkRuntime(needed), Path.Combine(java, "runtime"));
            }
            else
            {
                JdkArchive linux = Pin.linux;
                File.WriteAllText(Path.Combine(java, "runtime.env"),
                    "JDK_URL=" + linux.url + "\nJDK_SHA256=" + linux.sha256 + "\nJAVA_MODULES=" + needed + "\n");
            }
            foreach (string module in modules)
            {
                string jar = module == BootModule ? Path.Combine(java, "boot.jar")
                    : Path.Combine(java, EngineModules.Contains(module) ? "lib" : "scripts", module + ".jar");
                Directory.CreateDirectory(Path.GetDirectoryName(jar));
                if (Run("jar", new[] { "--create", "--file", jar, "-C", Path.Combine(Classes, module), "." }, out string output) != 0)
                    throw new BuildFailedException("[Java] jar couldn't pack " + module + ":\n" + output);
            }
        }

        /// <summary>The JDK modules <paramref name="modules"/> need, as jdeps finds them, comma-separated.</summary>
        private static string RuntimeModules(string[] modules)
        {
            var jdeps = new List<string> { "--print-module-deps", "--ignore-missing-deps", "--multi-release", Feature, "--module-path", Classes };
            jdeps.AddRange(modules.Select(m => Path.Combine(Classes, m)));
            if (Run("jdeps", jdeps, out string deps) != 0) throw new BuildFailedException("[Java] jdeps couldn't read the modules:\n" + deps);
            return string.Join(",", deps.Split('\n').Last().Split(',').Select(m => m.Trim())
                .Append("java.base").Where(m => m.Length > 0 && !m.StartsWith("withthedead.", StringComparison.Ordinal))
                .Distinct().OrderBy(m => m, StringComparer.Ordinal));
        }

        /// <summary>
        /// A Java runtime for Windows builds with the JDK modules <paramref name="needed"/>, in Library/Java/runtime: linked
        /// from the JDK's own image (JEP 493), and again only when those modules or the JDK change.
        /// </summary>
        private static string LinkRuntime(string needed)
        {
            string runtime = Path.Combine(Output, "runtime");
            string stampFile = runtime + ".stamp", stamp = Release + " " + needed;
            if (File.Exists(Path.Combine(runtime, "release")) && File.Exists(stampFile) && File.ReadAllText(stampFile) == stamp) return runtime;

            DeleteDirectory(runtime);
            if (Run("jlink", new[]
                {
                    "--add-modules", needed, "--output", runtime, "--strip-debug", "--no-header-files", "--no-man-pages",
                    "--strip-native-commands", "--compress", "zip-6",
                }, out string output) != 0)
                throw new BuildFailedException("[Java] jlink couldn't link the Java runtime:\n" + output);
            File.WriteAllText(stampFile, stamp);
            Debug.Log("[Java] Linked a Java runtime with " + needed);
            return runtime;
        }

        private static string[] SourceModules()
            => Directory.Exists(Sources)
                ? Directory.GetDirectories(Sources).Where(d => File.Exists(Path.Combine(d, "module-info.java")))
                    .Select(Path.GetFileName).OrderBy(m => m, StringComparer.Ordinal).ToArray()
                : new string[0];

        private static string[] CompiledModules()
            => Directory.Exists(Classes)
                ? Directory.GetDirectories(Classes).Select(Path.GetFileName).OrderBy(m => m, StringComparer.Ordinal).ToArray()
                : new string[0];

        // Every source's name, size and time, and the JDK release: a change to any of them means a new build.
        private static string Fingerprint()
        {
            var text = new StringBuilder(Release);
            foreach (string file in Directory.GetFiles(Sources, "*.java", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                var info = new FileInfo(file);
                text.Append('\n').Append(file.Substring(Sources.Length)).Append(' ').Append(info.Length).Append(' ').Append(info.LastWriteTimeUtc.Ticks);
            }
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "");
        }

        // Runs a JDK tool. Paths go with forward slashes, which Java takes on Windows too, so quoting needs no escapes (and
        // jdeps, unlike javac, reads no @argfiles). Returns the exit code and what it printed.
        private static int Run(string tool, IEnumerable<string> args, out string output)
        {
            var info = new ProcessStartInfo(Tool(tool), string.Join(" ", args.Select(a => "\"" + a.Replace('\\', '/') + "\"")))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = ProjectRoot,
            };
            using (Process process = Process.Start(info))
            {
                var errors = process.StandardError.ReadToEndAsync();
                string printed = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                output = (printed + errors.Result).Replace("\r\n", "\n").Trim();
                return process.ExitCode;
            }
        }

        private static void DeleteDirectory(string dir)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        private static void CopyDirectory(string from, string to)
        {
            foreach (string dir in Directory.GetDirectories(from, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(to, dir.Substring(from.Length + 1)));
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(to, file.Substring(from.Length + 1)), true);
        }
    }

    /// <summary>
    /// Windows builds and the Linux dedicated server carry the game's Java (see <see cref="JavaBuild"/>): the scripts are
    /// compiled before the build (one that doesn't compile, or no JDK, fails it) and packed into its data folder once the
    /// player is written.
    /// </summary>
    internal sealed class JavaBuildSteps : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!CarriesJava(report)) return;
            if (!JavaBuild.BuildIfStale(out string problem)) throw new BuildFailedException("[Java] " + problem);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (!CarriesJava(report)) return;
            string exe = report.summary.outputPath;
            string data = Path.Combine(Path.GetDirectoryName(exe), Path.GetFileNameWithoutExtension(exe) + "_Data");
            JavaBuild.Package(data, report.summary.platform == BuildTarget.StandaloneWindows64);
            Debug.Log("[Java] Packed the Java scripts into " + Path.Combine(data, "Java"));
        }

        private static bool CarriesJava(BuildReport report)
            => report.summary.platform == BuildTarget.StandaloneWindows64 || report.summary.platform == BuildTarget.StandaloneLinux64;
    }
}
