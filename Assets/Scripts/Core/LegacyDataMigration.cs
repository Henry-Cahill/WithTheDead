using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System.Runtime.InteropServices;
#endif

namespace CloneZ
{
    /// <summary>
    /// The game was called CLONE-Z before it was WithTheDead, and Unity keys the persistent data folder and (on Windows)
    /// the PlayerPrefs registry key by product name, so the rename moved both. On the first launch under the new name
    /// this copies the old folder's files (the profile, the saves, the content database, the economy) and the old
    /// PlayerPrefs (the settings) across. Anything the new places already hold is kept, and the old copies stay where
    /// they were. A marker file in the new folder makes it a one-off, so deleting the content database afterwards still
    /// rebuilds it rather than bringing the old one back.
    /// </summary>
    internal static class LegacyDataMigration
    {
        private const string OldProductName = "CLONE-Z";
        private const string MarkerFile = "migrated-from-clone-z.txt";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Run()
        {
            try
            {
                string now = (Application.persistentDataPath ?? "").TrimEnd('/', '\\');
                // Desktop players keep their data in <company>/<product>; anywhere else (a mobile sandbox) has nothing to carry over.
                if (now.Length == 0 || Application.productName == OldProductName || Path.GetFileName(now) != Application.productName) return;
                string marker = Path.Combine(now, MarkerFile);
                if (File.Exists(marker)) return;
                string old = Path.Combine(Path.GetDirectoryName(now), OldProductName);
                int files = Directory.Exists(old) ? CopyFiles(old, now) : 0;
                int prefs = CopyPlayerPrefs();
                Directory.CreateDirectory(now);
                File.WriteAllText(marker, "Carried over from " + old + " on " + DateTime.UtcNow.ToString("u") + ": "
                    + files + " file(s), " + prefs + " PlayerPrefs value(s).\n");
                if (files > 0 || prefs > 0)
                    Debug.Log("[CloneZ] Carried " + files + " save file(s) and " + prefs + " setting(s) over from " + OldProductName + ".");
            }
            catch (Exception e)
            {
                // No marker, so the next launch tries again, skipping whatever already made it across.
                Debug.LogWarning("[CloneZ] Couldn't carry the " + OldProductName + " saves over: " + e.Message);
            }
        }

        // The top-level files only: the Unity folder beside them is the engine's own analytics, and the logs are per run.
        private static int CopyFiles(string from, string to)
        {
            Directory.CreateDirectory(to);
            int copied = 0;
            foreach (string src in Directory.GetFiles(from))
            {
                string name = Path.GetFileName(src);
                if (name.EndsWith(".log", StringComparison.OrdinalIgnoreCase)) continue;
                string dst = Path.Combine(to, name);
                if (File.Exists(dst)) continue;
                File.Copy(src, dst);
                copied++;
            }
            return copied;
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // PlayerPrefs live in the registry: HKCU\Software\<company>\<product>, or under Software\Unity\UnityEditor for
        // the editor's. Values are copied as they are (the names carry Unity's hash suffix, the data its encoding).
        private static int CopyPlayerPrefs()
        {
            string root = (Application.isEditor ? @"Software\Unity\UnityEditor\" : @"Software\") + Application.companyName + @"\";
            if (RegOpenKeyEx(HkeyCurrentUser, root + OldProductName, 0, KeyRead, out IntPtr from) != 0) return 0;
            try
            {
                if (RegCreateKeyEx(HkeyCurrentUser, root + Application.productName, 0, null, 0, KeyRead | KeyWrite,
                        IntPtr.Zero, out IntPtr to, out _) != 0) return 0;
                try
                {
                    int copied = 0;
                    var name = new char[MaxValueName];
                    for (int i = 0; ; i++)
                    {
                        int length = name.Length;
                        int r = RegEnumValue(from, i, name, ref length, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                        if (r == ErrorNoMoreItems) break;
                        if (r != 0) continue;
                        string value = new string(name, 0, length);
                        int size = 0;
                        if (RegQueryValueEx(to, value, IntPtr.Zero, out _, null, ref size) == 0) continue;
                        if (RegQueryValueEx(from, value, IntPtr.Zero, out _, null, ref size) != 0) continue;
                        var data = new byte[size];
                        if (RegQueryValueEx(from, value, IntPtr.Zero, out int kind, data, ref size) != 0) continue;
                        if (RegSetValueEx(to, value, 0, kind, data, size) == 0) copied++;
                    }
                    return copied;
                }
                finally { RegCloseKey(to); }
            }
            finally { RegCloseKey(from); }
        }

        private static readonly IntPtr HkeyCurrentUser = new IntPtr(unchecked((int)0x80000001));
        private const int KeyRead = 0x20019, KeyWrite = 0x20006, ErrorNoMoreItems = 259, MaxValueName = 16384;

        [DllImport("advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegOpenKeyEx(IntPtr key, string subKey, int options, int access, out IntPtr result);

        [DllImport("advapi32.dll", EntryPoint = "RegCreateKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegCreateKeyEx(IntPtr key, string subKey, int reserved, string keyClass, int options, int access,
            IntPtr security, out IntPtr result, out int disposition);

        [DllImport("advapi32.dll", EntryPoint = "RegEnumValueW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegEnumValue(IntPtr key, int index, [Out] char[] name, ref int nameLength, IntPtr reserved,
            IntPtr type, IntPtr data, IntPtr dataLength);

        [DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegQueryValueEx(IntPtr key, string name, IntPtr reserved, out int type, byte[] data, ref int dataLength);

        [DllImport("advapi32.dll", EntryPoint = "RegSetValueExW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int RegSetValueEx(IntPtr key, string name, int reserved, int type, byte[] data, int dataLength);

        [DllImport("advapi32.dll", ExactSpelling = true)]
        private static extern int RegCloseKey(IntPtr key);
#else
        private static int CopyPlayerPrefs() => 0;
#endif
    }
}
