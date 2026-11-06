using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CloneZ.Java
{
    /// <summary>A Java exception that reached C#. Its message is the Java stack trace.</summary>
    public sealed class JavaException : Exception
    {
        public JavaException(string message) : base(message) { }
    }

    /// <summary>
    /// The slice of the Java Native Interface the game uses (see Java/README.md). It loads a Java runtime's JVM library,
    /// makes the process's JVM or finds the one already running, and binds the JNI functions it calls from the JVM's
    /// function tables with <see cref="Marshal.GetDelegateForFunctionPointer(IntPtr, Type)"/>, so it needs no unsafe code.
    /// A process can only ever have one JVM: after a domain reload in the editor <see cref="Open"/> finds it again, still
    /// with the options it was first made with. Every call must come from the thread that opened it (the main thread).
    /// </summary>
    public sealed class Jni
    {
        /// <summary>JNI_VERSION_24, the newest JNI version (JDK 24 and later).</summary>
        public const int Version = 0x00180000;

        private const int Ok = 0, Detached = -2;

        // Slots in jni.h's JNINativeInterface_ and JNIInvokeInterface_ function tables.
        private const int FindClassSlot = 6, ExceptionOccurredSlot = 15, ExceptionClearSlot = 17, PushLocalFrameSlot = 19,
            PopLocalFrameSlot = 20, NewGlobalRefSlot = 21, DeleteGlobalRefSlot = 22, DeleteLocalRefSlot = 23,
            GetObjectClassSlot = 31, GetMethodIDSlot = 33, CallObjectMethodASlot = 36, GetStaticMethodIDSlot = 113,
            CallStaticObjectMethodASlot = 116, CallStaticVoidMethodASlot = 143, NewStringSlot = 163,
            GetStringLengthSlot = 164, GetStringCharsSlot = 165, ReleaseStringCharsSlot = 166, RegisterNativesSlot = 215,
            UnregisterNativesSlot = 216, ExceptionCheckSlot = 228;
        private const int AttachCurrentThreadSlot = 4, GetEnvSlot = 6;

        /// <summary>A JNI argument (jvalue).</summary>
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct Value
        {
            [FieldOffset(0)] public byte Z;
            [FieldOffset(0)] public int I;
            [FieldOffset(0)] public long J;
            [FieldOffset(0)] public double D;
            [FieldOffset(0)] public IntPtr L;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InitArgs
        {
            public int Version;
            public int OptionCount;
            public IntPtr Options;
            public byte IgnoreUnrecognized;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMethod
        {
            public IntPtr Name, Signature, Function;
        }

        private delegate int CreateJavaVMFn(out IntPtr vm, out IntPtr env, ref InitArgs args);
        private delegate int GetCreatedJavaVMsFn([Out] IntPtr[] vms, int length, out int count);
        private delegate int GetEnvFn(IntPtr vm, out IntPtr env, int version);
        private delegate int AttachCurrentThreadFn(IntPtr vm, out IntPtr env, IntPtr args);
        private delegate IntPtr FindClassFn(IntPtr env, [MarshalAs(UnmanagedType.LPStr)] string name);
        private delegate IntPtr EnvFn(IntPtr env);
        private delegate void EnvVoidFn(IntPtr env);
        private delegate byte EnvByteFn(IntPtr env);
        private delegate int PushLocalFrameFn(IntPtr env, int capacity);
        private delegate IntPtr RefFn(IntPtr env, IntPtr reference);
        private delegate void RefVoidFn(IntPtr env, IntPtr reference);
        private delegate int RefIntFn(IntPtr env, IntPtr reference);
        private delegate IntPtr GetMethodIDFn(IntPtr env, IntPtr type,
            [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string signature);
        private delegate IntPtr CallObjectFn(IntPtr env, IntPtr target, IntPtr method, [In] Value[] args);
        private delegate void CallVoidFn(IntPtr env, IntPtr target, IntPtr method, [In] Value[] args);
        private delegate IntPtr NewStringFn(IntPtr env, [MarshalAs(UnmanagedType.LPWStr)] string chars, int length);
        private delegate IntPtr GetStringCharsFn(IntPtr env, IntPtr text, IntPtr isCopy);
        private delegate void ReleaseStringCharsFn(IntPtr env, IntPtr text, IntPtr chars);
        private delegate int RegisterNativesFn(IntPtr env, IntPtr type, [In] NativeMethod[] methods, int count);

        private readonly IntPtr _env;
        private readonly FindClassFn _findClass;
        private readonly EnvFn _exceptionOccurred;
        private readonly EnvVoidFn _exceptionClear;
        private readonly EnvByteFn _exceptionCheck;
        private readonly PushLocalFrameFn _pushLocalFrame;
        private readonly RefFn _popLocalFrame, _newGlobalRef, _getObjectClass;
        private readonly RefVoidFn _deleteGlobalRef, _deleteLocalRef;
        private readonly RefIntFn _getStringLength, _unregisterNatives;
        private readonly GetMethodIDFn _getMethodID, _getStaticMethodID;
        private readonly CallObjectFn _callObjectMethod, _callStaticObjectMethod;
        private readonly CallVoidFn _callStaticVoidMethod;
        private readonly NewStringFn _newString;
        private readonly GetStringCharsFn _getStringChars;
        private readonly ReleaseStringCharsFn _releaseStringChars;
        private readonly RegisterNativesFn _registerNatives;
        private IntPtr _describeClass, _describeMethod;

        /// <summary>True when <see cref="Open"/> made the JVM; false when it found the one already running.</summary>
        public bool Created { get; }

        private Jni(IntPtr env, bool created)
        {
            _env = env;
            Created = created;
            IntPtr table = Marshal.ReadIntPtr(env);
            _findClass = Slot<FindClassFn>(table, FindClassSlot);
            _exceptionOccurred = Slot<EnvFn>(table, ExceptionOccurredSlot);
            _exceptionClear = Slot<EnvVoidFn>(table, ExceptionClearSlot);
            _exceptionCheck = Slot<EnvByteFn>(table, ExceptionCheckSlot);
            _pushLocalFrame = Slot<PushLocalFrameFn>(table, PushLocalFrameSlot);
            _popLocalFrame = Slot<RefFn>(table, PopLocalFrameSlot);
            _newGlobalRef = Slot<RefFn>(table, NewGlobalRefSlot);
            _getObjectClass = Slot<RefFn>(table, GetObjectClassSlot);
            _deleteGlobalRef = Slot<RefVoidFn>(table, DeleteGlobalRefSlot);
            _deleteLocalRef = Slot<RefVoidFn>(table, DeleteLocalRefSlot);
            _getStringLength = Slot<RefIntFn>(table, GetStringLengthSlot);
            _unregisterNatives = Slot<RefIntFn>(table, UnregisterNativesSlot);
            _getMethodID = Slot<GetMethodIDFn>(table, GetMethodIDSlot);
            _getStaticMethodID = Slot<GetMethodIDFn>(table, GetStaticMethodIDSlot);
            _callObjectMethod = Slot<CallObjectFn>(table, CallObjectMethodASlot);
            _callStaticObjectMethod = Slot<CallObjectFn>(table, CallStaticObjectMethodASlot);
            _callStaticVoidMethod = Slot<CallVoidFn>(table, CallStaticVoidMethodASlot);
            _newString = Slot<NewStringFn>(table, NewStringSlot);
            _getStringChars = Slot<GetStringCharsFn>(table, GetStringCharsSlot);
            _releaseStringChars = Slot<ReleaseStringCharsFn>(table, ReleaseStringCharsSlot);
            _registerNatives = Slot<RegisterNativesFn>(table, RegisterNativesSlot);
        }

        /// <summary>
        /// Loads the JVM library at <paramref name="library"/> (bin/server/jvm.dll, or lib/server/libjvm.so on Linux, in a
        /// Java runtime) and makes the process's JVM with <paramref name="options"/>, or attaches to the one already
        /// running, whose options can't change any more.
        /// </summary>
        public static Jni Open(string library, IList<string> options)
        {
            // HotSpot's generated code moves return addresses about, which a CET shadow stack kills the process for. Unity's
            // executables don't opt into shadow stacks; this catches one that does before the JVM can take it down.
            if (IsWindows && ShadowStacksEnforced())
                throw new JavaException("This process enforces CET shadow stacks, which the JVM's generated code breaks; Java can't run in it.");
            // HotSpot finds its own files from its library's path in the ANSI code page; one it can't write there would
            // make it exit the whole process as it starts.
            if (!Representable(library))
                throw new JavaException("The Java runtime's path (" + library + ") has characters this system's code page lacks; the JVM couldn't find its own files.");
            IntPtr module = LoadLibrary(library);
            var getCreated = Export<GetCreatedJavaVMsFn>(module, "JNI_GetCreatedJavaVMs");
            var vms = new IntPtr[1];
            int rc = getCreated(vms, 1, out int count);
            if (rc != Ok) throw new JavaException("JNI_GetCreatedJavaVMs failed: " + Error(rc));
            if (count > 0) return new Jni(Attach(vms[0]), false);

            // The JVM may hold on to its option strings, so they stay allocated: a few hundred bytes, once per process.
            IntPtr block = Marshal.AllocHGlobal(2 * IntPtr.Size * Math.Max(1, options.Count));
            for (int i = 0; i < options.Count; i++)
            {
                byte[] bytes = Encode(options[i], out bool lossy);
                if (lossy) throw new JavaException("The JVM option " + options[i] + " has characters this system's code page lacks.");
                IntPtr text = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, text, bytes.Length);
                Marshal.WriteIntPtr(block, 2 * i * IntPtr.Size, text);
                Marshal.WriteIntPtr(block, (2 * i + 1) * IntPtr.Size, IntPtr.Zero);
            }
            var args = new InitArgs { Version = Version, OptionCount = options.Count, Options = block };
            rc = Export<CreateJavaVMFn>(module, "JNI_CreateJavaVM")(out IntPtr _, out IntPtr env, ref args);
            if (rc != Ok) throw new JavaException("JNI_CreateJavaVM failed: " + Error(rc) + " (options: " + string.Join(" ", options) + ")");
            return new Jni(env, true);
        }

        /// <summary>A global reference to the class <paramref name="name"/> ("java/lang/String"), as the system class
        /// loader finds it.</summary>
        public IntPtr FindClass(string name)
        {
            IntPtr local = _findClass(_env, name);
            ThrowIfPending("FindClass " + name);
            IntPtr global = _newGlobalRef(_env, local);
            _deleteLocalRef(_env, local);
            return global;
        }

        public void DeleteGlobalRef(IntPtr global)
        {
            if (global != IntPtr.Zero) _deleteGlobalRef(_env, global);
        }

        /// <summary>The id of the static method <paramref name="name"/> with the JNI <paramref name="signature"/>.</summary>
        public IntPtr StaticMethod(IntPtr type, string name, string signature)
        {
            IntPtr id = _getStaticMethodID(_env, type, name, signature);
            ThrowIfPending(name + signature);
            return id;
        }

        /// <summary>Calls a static void method. Arguments may be strings (a Java String; null is null), ints, bools,
        /// longs, doubles and IntPtrs (references).</summary>
        public void CallStaticVoid(IntPtr type, IntPtr method, params object[] args)
        {
            PushFrame();
            try
            {
                _callStaticVoidMethod(_env, type, method, Values(args));
                ThrowIfPending();
            }
            finally
            {
                _popLocalFrame(_env, IntPtr.Zero);
            }
        }

        /// <summary>Calls a static method that returns a String.</summary>
        public string CallStaticString(IntPtr type, IntPtr method, params object[] args)
        {
            PushFrame();
            try
            {
                IntPtr result = _callStaticObjectMethod(_env, type, method, Values(args));
                ThrowIfPending();
                return ReadString(_env, result);
            }
            finally
            {
                _popLocalFrame(_env, IntPtr.Zero);
            }
        }

        /// <summary>Calls a static method that returns an object; returns a global reference to it (zero for null).</summary>
        public IntPtr CallStaticObject(IntPtr type, IntPtr method, params object[] args)
        {
            PushFrame();
            try
            {
                IntPtr result = _callStaticObjectMethod(_env, type, method, Values(args));
                ThrowIfPending();
                return result == IntPtr.Zero ? IntPtr.Zero : _newGlobalRef(_env, result);
            }
            finally
            {
                _popLocalFrame(_env, IntPtr.Zero);
            }
        }

        /// <summary>Binds native methods of <paramref name="type"/> to C# delegates, which the caller must keep alive.</summary>
        public void RegisterNatives(IntPtr type, params (string Name, string Signature, Delegate Function)[] methods)
        {
            var table = new NativeMethod[methods.Length];
            try
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    table[i].Name = Marshal.StringToHGlobalAnsi(methods[i].Name);
                    table[i].Signature = Marshal.StringToHGlobalAnsi(methods[i].Signature);
                    table[i].Function = Marshal.GetFunctionPointerForDelegate(methods[i].Function);
                }
                int rc = _registerNatives(_env, type, table, table.Length);
                ThrowIfPending("RegisterNatives");
                if (rc != Ok) throw new JavaException("RegisterNatives failed: " + Error(rc));
            }
            finally
            {
                foreach (NativeMethod m in table)
                {
                    Marshal.FreeHGlobal(m.Name);
                    Marshal.FreeHGlobal(m.Signature);
                }
            }
        }

        /// <summary>Unbinds every native method of <paramref name="type"/>: calling one throws UnsatisfiedLinkError.</summary>
        public void UnregisterNatives(IntPtr type)
        {
            _unregisterNatives(_env, type);
            ThrowIfPending("UnregisterNatives");
        }

        /// <summary>The C# string for a Java String reference (null for null). For native methods, with their env.</summary>
        public string ReadString(IntPtr env, IntPtr text)
        {
            if (text == IntPtr.Zero) return null;
            int length = _getStringLength(env, text);
            IntPtr chars = _getStringChars(env, text, IntPtr.Zero);
            if (chars == IntPtr.Zero) return null;
            try { return Marshal.PtrToStringUni(chars, length); }
            finally { _releaseStringChars(env, text, chars); }
        }

        /// <summary>Has Java exceptions described by the static method <paramref name="method"/> of <paramref name="type"/>
        /// (a global reference), which takes a Throwable and returns a String; otherwise they're described by toString.</summary>
        public void DescribeExceptionsWith(IntPtr type, IntPtr method)
        {
            _describeClass = type;
            _describeMethod = method;
        }

        private void PushFrame()
        {
            if (_pushLocalFrame(_env, 16) != Ok) ThrowIfPending("PushLocalFrame");
        }

        private Value[] Values(object[] args)
        {
            if (args == null || args.Length == 0) return null;
            var values = new Value[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case null:
                        break;
                    case string s:
                        values[i].L = _newString(_env, s, s.Length);
                        ThrowIfPending("NewString");
                        break;
                    case int n:
                        values[i].I = n;
                        break;
                    case bool b:
                        values[i].Z = b ? (byte)1 : (byte)0;
                        break;
                    case long l:
                        values[i].J = l;
                        break;
                    case double d:
                        values[i].D = d;
                        break;
                    case IntPtr reference:
                        values[i].L = reference;
                        break;
                    default:
                        throw new ArgumentException("No JNI value for a " + args[i].GetType().Name);
                }
            }
            return values;
        }

        private void ThrowIfPending(string what = null)
        {
            if (_exceptionCheck(_env) == 0) return;
            IntPtr failure = _exceptionOccurred(_env);
            _exceptionClear(_env);
            string text;
            try { text = Describe(failure); }
            finally { _deleteLocalRef(_env, failure); }
            throw new JavaException(what == null ? text : what + ": " + text);
        }

        private string Describe(IntPtr failure)
        {
            IntPtr text;
            if (_describeClass != IntPtr.Zero)
            {
                text = _callStaticObjectMethod(_env, _describeClass, _describeMethod, new[] { new Value { L = failure } });
            }
            else
            {
                IntPtr type = _getObjectClass(_env, failure);
                IntPtr toString = _getMethodID(_env, type, "toString", "()Ljava/lang/String;");
                text = toString == IntPtr.Zero ? IntPtr.Zero : _callObjectMethod(_env, failure, toString, null);
                _deleteLocalRef(_env, type);
            }
            if (_exceptionCheck(_env) != 0)
            {
                _exceptionClear(_env);
                return "a Java exception that couldn't be described";
            }
            string description = ReadString(_env, text);
            _deleteLocalRef(_env, text);
            return description;
        }

        private static IntPtr Attach(IntPtr vm)
        {
            IntPtr table = Marshal.ReadIntPtr(vm);
            int rc = Slot<GetEnvFn>(table, GetEnvSlot)(vm, out IntPtr env, Version);
            if (rc == Detached) rc = Slot<AttachCurrentThreadFn>(table, AttachCurrentThreadSlot)(vm, out env, IntPtr.Zero);
            if (rc != Ok) throw new JavaException("Couldn't attach to the JVM already running: " + Error(rc));
            return env;
        }

        private static T Slot<T>(IntPtr table, int slot) where T : Delegate
            => (T)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, slot * IntPtr.Size), typeof(T));

        private static T Export<T>(IntPtr module, string name) where T : Delegate
        {
            IntPtr function = IsWindows ? GetProcAddress(module, name) : dlsym(module, name);
            if (function == IntPtr.Zero) throw new JavaException("The JVM library has no " + name);
            return (T)Marshal.GetDelegateForFunctionPointer(function, typeof(T));
        }

        private static string Error(int code) => code switch
        {
            -1 => "JNI_ERR",
            -2 => "JNI_EDETACHED",
            -3 => "JNI_EVERSION",
            -4 => "JNI_ENOMEM",
            -5 => "JNI_EEXIST",
            -6 => "JNI_EINVAL",
            _ => "error " + code,
        };

        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private static IntPtr LoadLibrary(string path)
        {
            if (!File.Exists(path)) throw new JavaException("No JVM library at " + path);
            if (IsWindows)
            {
                // jvm.dll (bin/server) needs the C runtime the Java runtime carries in bin.
                AddDllDirectory(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                IntPtr module = LoadLibraryExW(path, IntPtr.Zero, LoadLibrarySearchDllLoadDir | LoadLibrarySearchDefaultDirs);
                if (module == IntPtr.Zero) throw new JavaException("Couldn't load " + path + " (Windows error " + Marshal.GetLastWin32Error() + ")");
                return module;
            }
            IntPtr handle = dlopen(path, RtldNow | RtldGlobal);
            if (handle == IntPtr.Zero) throw new JavaException("Couldn't load " + path + ": " + Marshal.PtrToStringAnsi(dlerror()));
            return handle;
        }

        /// <summary>
        /// Whether the JVM can read <paramref name="text"/> in an option, or find a file by it: on Windows HotSpot reads
        /// both in the system's ANSI code page (Unity's Mono marshals "ANSI" strings as UTF-8, which isn't it), so a
        /// character the code page lacks can't reach it. Always true elsewhere.
        /// </summary>
        public static bool Representable(string text)
        {
            Encode(text, out bool lossy);
            return !lossy;
        }

        /// <summary>
        /// <paramref name="path"/> (which must exist) as a JVM option can carry it: itself if the code page can write it,
        /// else its short 8.3 form if the volume keeps those; null when neither will do.
        /// </summary>
        public static string OptionPath(string path)
        {
            if (Representable(path)) return path;
            var shortPath = new StringBuilder(1024);
            int length = GetShortPathNameW(path, shortPath, shortPath.Capacity);
            return length > 0 && length < shortPath.Capacity && Representable(shortPath.ToString()) ? shortPath.ToString() : null;
        }

        // The zero-terminated bytes the JVM reads for text: the ANSI code page on Windows (lossy when it lacks a character
        // and had to put its default one), UTF-8 elsewhere and where the code page is UTF-8.
        private static byte[] Encode(string text, out bool lossy)
        {
            lossy = false;
            if (!IsWindows || GetACP() == Utf8CodePage) return Encoding.UTF8.GetBytes(text + "\0");
            int length = text.Length == 0 ? 0 : WideCharToMultiByte(0, NoBestFitChars, text, text.Length, null, 0, IntPtr.Zero, out _);
            var bytes = new byte[length + 1];
            if (length > 0)
            {
                WideCharToMultiByte(0, NoBestFitChars, text, text.Length, bytes, length, IntPtr.Zero, out int usedDefault);
                lossy = usedDefault != 0;
            }
            return bytes;
        }

        private const uint Utf8CodePage = 65001, NoBestFitChars = 0x400;

        private const uint LoadLibrarySearchDllLoadDir = 0x100, LoadLibrarySearchDefaultDirs = 0x1000;
        private const int RtldNow = 2, RtldGlobal = 0x100;
        private const int ProcessUserShadowStackPolicy = 15;

        private static bool ShadowStacksEnforced()
        {
            try
            {
                return GetProcessMitigationPolicy(GetCurrentProcess(), ProcessUserShadowStackPolicy, out uint flags, new IntPtr(sizeof(uint)))
                    && (flags & 1) != 0;   // EnableUserShadowStack
            }
            catch (EntryPointNotFoundException)
            {
                return false;   // a Windows older than any with shadow stacks
            }
        }

        [DllImport("kernel32")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32")]
        private static extern uint GetACP();

        [DllImport("kernel32", SetLastError = true)]
        private static extern int WideCharToMultiByte(uint codePage, uint flags, [MarshalAs(UnmanagedType.LPWStr)] string wide,
            int wideLength, [Out] byte[] multiByte, int multiByteLength, IntPtr defaultChar, out int usedDefaultChar);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetShortPathNameW(string longPath, StringBuilder shortPath, int length);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessMitigationPolicy(IntPtr process, int policy, out uint flags, IntPtr length);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryExW(string path, IntPtr file, uint flags);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr AddDllDirectory(string directory);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string name);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlopen(string path, int flags);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlsym(IntPtr handle, string name);

        [DllImport("libdl.so.2")]
        private static extern IntPtr dlerror();
    }
}
