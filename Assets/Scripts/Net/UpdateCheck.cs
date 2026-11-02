using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace CloneZ
{
    /// <summary>
    /// Tells a release's players when a new build is out. A co-op release (Tools/Server/Release.ps1) puts
    /// <see cref="FileName"/> beside the game: the address of the download page's <c>latest.json</c> (the version the page
    /// hands out, which is the one the server runs, and the page). The title screen asks once a run, and offers the page
    /// when that version isn't this one; the server lets in only its own build.
    /// </summary>
    public static class UpdateCheck
    {
        /// <summary>The file beside the game that holds the address of the download page's latest.json.</summary>
        public const string FileName = "updates.txt";

        [Serializable]
        private class Latest
        {
            public string version;
            public string page;
        }

        /// <summary>The build the download page hands out, when it isn't this one (else null).</summary>
        public static string NewVersion { get; private set; }
        /// <summary>Where to get it.</summary>
        public static string Page { get; private set; }
        /// <summary>Raised (on the main thread) when the check finds a new build.</summary>
        public static event Action Found;

        private static bool s_Asked;

        // An editor that skips the domain reload keeps statics from one play session to the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_Asked = false;
            NewVersion = Page = null;
        }

        /// <summary>Asks the download page for its build, once a run, in a release that names one.</summary>
        public static void Begin()
        {
            if (s_Asked || !BuildInfo.Stamped) return;
            s_Asked = true;
            string url = ReadBeside();
            if (string.IsNullOrEmpty(url)) return;
            UnityWebRequest req = null;
            try
            {
                req = UnityWebRequest.Get(url);
                req.timeout = 10;
                // Throws at once for an address the player won't fetch (plain http, which builds refuse by default).
                req.SendWebRequest().completed += _ =>
                {
                    try { Answer(url, req); }
                    catch (Exception e) { Debug.LogWarning("[CloneZ] Couldn't read " + url + ": " + e.Message); }
                    finally { req.Dispose(); }
                };
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Couldn't ask " + url + " for the newest build: " + e.Message);
                req?.Dispose();
            }
        }

        private static void Answer(string url, UnityWebRequest req)
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("[CloneZ] Couldn't ask " + url + " for the newest build: " + req.error);
                return;
            }
            Latest latest = JsonUtility.FromJson<Latest>(req.downloadHandler.text);
            if (latest == null || string.IsNullOrEmpty(latest.version)) return;
            if (latest.version == BuildInfo.Version)
            {
                Debug.Log("[CloneZ] This is the build the download page hands out (" + latest.version + ").");
                return;
            }
            NewVersion = latest.version;
            Page = string.IsNullOrEmpty(latest.page) ? url.Substring(0, url.LastIndexOf('/') + 1) : latest.page;
            Debug.Log("[CloneZ] A new build is out: " + NewVersion + " (this is " + BuildInfo.Version + "), at " + Page);
            Found?.Invoke();
        }

        // The first line of updates.txt in the folder that holds the game's executable, or null when there's none.
        private static string ReadBeside()
        {
            try
            {
                string dir = Path.GetDirectoryName(Application.dataPath);
                string path = string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, FileName);
                if (path == null || !File.Exists(path)) return null;
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length > 0 && !t.StartsWith("#", StringComparison.Ordinal)) return t;
                }
            }
            catch (Exception e) { Debug.LogWarning("[CloneZ] Could not read " + FileName + ": " + e.Message); }
            return null;
        }
    }
}
