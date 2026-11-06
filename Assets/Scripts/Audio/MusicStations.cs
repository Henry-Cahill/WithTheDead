using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CloneZ
{
    /// <summary>
    /// Real music on the radio. Each folder of audio files under <c>StreamingAssets/Music</c> (shipped with the game)
    /// or <c>&lt;persistentDataPath&gt;/Music</c> (the player's own tracks, never shipped) is a station: its .ogg /
    /// .mp3 / .wav files are the rotation, and an optional <c>station.json</c> names its place on the dial —
    /// <c>{"name":"98.3 FM","title":"Outlaw Country","color":"#E8A14C","spoken":false}</c>. The stations join the
    /// kitchen radio's dial after the scripted ones (<see cref="Broadcasts"/>), stay on the air for good (an automated
    /// transmitter outlasts its DJs), and caption whatever track they're playing. A station of spoken programmes
    /// (radio plays, not songs) keeps off the roadhouse jukebox. Tracks are streamed from disk, never imported, so
    /// the folders cost no memory beyond the track that's playing.
    /// </summary>
    public static class MusicStations
    {
        /// <summary>One folder of tracks on the dial.</summary>
        public sealed class Station
        {
            public string Folder;           // the folder's name
            public string Name;             // where it is on the dial: "98.3 FM"
            public string Title;            // what it calls itself
            public Color Caption;           // the colour its captions are credited in
            public bool Spoken;             // radio plays, not songs: kept off the jukebox
            public List<string> Tracks;     // full paths, sorted
            internal int[] Order;           // the rotation, shuffled
            internal int Cursor;            // where the rotation is
            internal string NowPlaying;     // the title of the track last put on
        }

        [Serializable]
        private sealed class StationJson
        {
            public string name;
            public string title;
            public string color;
            public bool spoken;
        }

        private static readonly BroadcastStation First = (BroadcastStation)((int)BroadcastStation.RadioEmergency + 1);
        private static Station[] s_Stations;
        private static BroadcastStation[] s_RadioDial;
        private static string[] s_JukeboxTracks;

        /// <summary>How many stations the folders hold.</summary>
        public static int Count => Load().Length;

        /// <summary>True when <paramref name="s"/> is one of the folders' stations rather than a scripted broadcast.</summary>
        public static bool IsMusic(BroadcastStation s)
        {
            int i = (int)s - (int)First;
            return i >= 0 && i < Load().Length;
        }

        /// <summary>The station behind <paramref name="s"/> (null when it isn't one).</summary>
        public static Station Of(BroadcastStation s)
        {
            Station[] all = Load();
            int i = (int)s - (int)First;
            return i >= 0 && i < all.Length ? all[i] : null;
        }

        /// <summary>Where it is on the dial: "98.3 FM".</summary>
        public static string NameOf(BroadcastStation s) => Of(s)?.Name ?? "?";

        /// <summary>What it calls itself: "Outlaw Country".</summary>
        public static string TitleOf(BroadcastStation s) => Of(s)?.Title ?? "?";

        /// <summary>The colour its captions are credited in.</summary>
        public static Color CaptionColour(BroadcastStation s) => Of(s)?.Caption ?? Color.white;

        /// <summary>True while the station has anything to play.</summary>
        public static bool OnAir(BroadcastStation s)
        {
            Station st = Of(s);
            return st != null && st.Tracks.Count > 0;
        }

        /// <summary>The caption for what's on: "♪ Coal Town Blues" (null: dead air).</summary>
        public static string NowPlayingLine(BroadcastStation s)
        {
            Station st = Of(s);
            if (st == null || st.Tracks.Count == 0) return null;
            if (st.NowPlaying != null) return "\u266a " + st.NowPlaying;
            return st.Spoken ? "*a broadcast plays*" : "*music plays*";
        }

        /// <summary>The radio's dial: the scripted stations, then the folders' (built once; stable for the session).</summary>
        public static BroadcastStation[] RadioDial(BroadcastStation[] builtIn)
        {
            if (s_RadioDial != null) return s_RadioDial;
            Station[] all = Load();
            var dial = new BroadcastStation[builtIn.Length + all.Length];
            Array.Copy(builtIn, dial, builtIn.Length);
            for (int i = 0; i < all.Length; i++) dial[builtIn.Length + i] = (BroadcastStation)((int)First + i);
            return s_RadioDial = dial;
        }

        /// <summary>The dial value of the <paramref name="index"/>-th music station (0 .. <see cref="Count"/> - 1).</summary>
        public static BroadcastStation StationAt(int index) => (BroadcastStation)((int)First + index);

        /// <summary>The index of the station whose folder is called <paramref name="folder"/> (-1: no such station).</summary>
        public static int IndexOf(string folder)
        {
            Station[] all = Load();
            for (int i = 0; i < all.Length; i++)
                if (string.Equals(all[i].Folder, folder, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        /// <summary>Every song the jukebox may put on (the spoken stations' programmes stay off it): full paths.</summary>
        public static IReadOnlyList<string> JukeboxTracks
        {
            get
            {
                if (s_JukeboxTracks != null) return s_JukeboxTracks;
                var list = new List<string>();
                foreach (Station st in Load())
                    if (!st.Spoken) list.AddRange(st.Tracks);
                return s_JukeboxTracks = list.ToArray();
            }
        }

        /// <summary>The next track in <paramref name="st"/>'s rotation (a reshuffle each time it comes round); null when empty.</summary>
        internal static string NextTrack(Station st)
        {
            int n = st.Tracks.Count;
            if (n == 0) return null;
            if (st.Order == null || st.Order.Length != n)
            {
                st.Order = new int[n];
                for (int i = 0; i < n; i++) st.Order[i] = i;
                Shuffle(st.Order);
                st.Cursor = 0;
            }
            int at = st.Cursor % n;
            string path = st.Tracks[st.Order[at]];
            st.Cursor = (st.Cursor + 1) % n;
            if (st.Cursor == 0 && n > 1)
            {
                int last = st.Order[n - 1];
                Shuffle(st.Order);
                if (st.Order[0] == last) (st.Order[0], st.Order[n - 1]) = (st.Order[n - 1], st.Order[0]);
            }
            return path;
        }

        /// <summary>Marks what <paramref name="st"/> has put on (for the captions).</summary>
        internal static void PutOn(Station st, string path) => st.NowPlaying = TrackTitle(path);

        #region Titles
        /// <summary>A track's display title: the file's name without its extension, rip clutter ("(Official Video)") pruned.</summary>
        public static string TrackTitle(string path)
        {
            string t = Path.GetFileNameWithoutExtension(path).Replace('_', ' ');
            t = StripClutter(t, '(', ')');
            t = StripClutter(t, '[', ']');
            t = t.Trim();
            while (t.Length > 0 && (t[0] == '-' || t[0] == '.')) t = t.Substring(1).TrimStart();
            return t.Length > 0 ? t : "Untitled";
        }

        private static readonly string[] s_Clutter = { "official", "video", "remaster", "lyric", "audio", "hd", "hq", "visualizer" };

        // Removes bracketed groups that only tell where the rip came from.
        private static string StripClutter(string t, char open, char close)
        {
            for (int a = t.IndexOf(open); a >= 0; a = t.IndexOf(open, a + 1))
            {
                int b = t.IndexOf(close, a + 1);
                if (b < 0) break;
                string inside = t.Substring(a + 1, b - a - 1).ToLowerInvariant();
                bool clutter = false;
                foreach (string word in s_Clutter)
                    if (inside.Contains(word)) { clutter = true; break; }
                if (!clutter) continue;
                t = (t.Substring(0, a) + t.Substring(b + 1)).Replace("  ", " ");
                a = -1; // start over: indices have moved
            }
            return t;
        }
        #endregion

        #region Streaming
        /// <summary>Starts streaming a track off the disk; poll <see cref="UnityWebRequest.isDone"/>, then
        /// <see cref="FinishLoad"/>. Keep the request alive (undisposed) for as long as the clip plays.</summary>
        public static UnityWebRequest BeginLoad(string path)
        {
            UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(FileUrl(path), TypeOf(path));
            if (req.downloadHandler is DownloadHandlerAudioClip h) h.streamAudio = true;
            req.SendWebRequest();
            return req;
        }

        /// <summary>The clip of a finished <see cref="BeginLoad"/> (null and an <paramref name="error"/> when it failed).</summary>
        public static AudioClip FinishLoad(UnityWebRequest req, out string error)
        {
            error = null;
            if (req.result != UnityWebRequest.Result.Success)
            {
                error = req.error ?? "failed";
                return null;
            }
            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null) error = "not a readable audio file";
            return clip;
        }

        private static AudioType TypeOf(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".mp3": return AudioType.MPEG;
                case ".wav": return AudioType.WAV;
                case ".aif":
                case ".aiff": return AudioType.AIFF;
                default: return AudioType.OGGVORBIS;
            }
        }

        // file:// URL by hand: System.Uri turns "Outlaw #1.ogg" into a fragment.
        private static string FileUrl(string path)
        {
            string p = path.Replace('\\', '/');
            var sb = new StringBuilder("file://", p.Length + 16);
            if (p.Length > 0 && p[0] != '/') sb.Append('/');
            foreach (char c in p)
            {
                switch (c)
                {
                    case '%': sb.Append("%25"); break;
                    case '#': sb.Append("%23"); break;
                    case '?': sb.Append("%3F"); break;
                    case '+': sb.Append("%2B"); break;
                    case ' ': sb.Append("%20"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
        #endregion

        #region The folders
        private static Station[] Load()
        {
            if (s_Stations != null) return s_Stations;
            var byFolder = new SortedDictionary<string, Station>(StringComparer.OrdinalIgnoreCase);
            try
            {
                Scan(Path.Combine(Application.streamingAssetsPath, "Music"), byFolder);
                Scan(Path.Combine(Application.persistentDataPath, "Music"), byFolder);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[CloneZ] Music folders could not be read: " + e.Message);
            }
            var all = new Station[byFolder.Count];
            byFolder.Values.CopyTo(all, 0);
            if (all.Length > 0)
            {
                var sb = new StringBuilder("[CloneZ] Music stations: ");
                for (int i = 0; i < all.Length; i++)
                    sb.Append(i > 0 ? ", " : "").Append(all[i].Title).Append(" (").Append(all[i].Tracks.Count).Append(")");
                Debug.Log(sb.ToString());
            }
            return s_Stations = all;
        }

        private static void Scan(string root, SortedDictionary<string, Station> into)
        {
            if (!Directory.Exists(root)) return;
            foreach (string dir in Directory.GetDirectories(root))
            {
                var tracks = new List<string>();
                foreach (string file in Directory.GetFiles(dir))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext == ".ogg" || ext == ".mp3" || ext == ".wav" || ext == ".aif" || ext == ".aiff")
                        tracks.Add(file);
                }
                if (tracks.Count == 0) continue;
                tracks.Sort(StringComparer.OrdinalIgnoreCase);

                string folder = Path.GetFileName(dir);
                if (!into.TryGetValue(folder, out Station st))
                {
                    st = new Station { Folder = folder, Title = folder, Caption = new Color(1f, 0.84f, 0.55f), Tracks = new List<string>() };
                    st.Name = Frequency(folder);
                    into.Add(folder, st);
                }
                st.Tracks.AddRange(tracks);

                string json = Path.Combine(dir, "station.json");
                if (!File.Exists(json)) continue;
                try
                {
                    StationJson j = JsonUtility.FromJson<StationJson>(File.ReadAllText(json));
                    if (j == null) continue;
                    if (!string.IsNullOrEmpty(j.name)) st.Name = j.name;
                    if (!string.IsNullOrEmpty(j.title)) st.Title = j.title;
                    if (!string.IsNullOrEmpty(j.color) && ColorUtility.TryParseHtmlString(j.color, out Color c)) st.Caption = c;
                    st.Spoken |= j.spoken;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[CloneZ] " + json + " could not be read: " + e.Message);
                }
            }
        }

        // A stable spot on the dial for a folder without a station.json.
        private static string Frequency(string folder)
        {
            uint h = 2166136261u;
            foreach (char c in folder) h = (h ^ c) * 16777619u;
            return (88.1f + (h % 100u) * 0.2f).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " FM";
        }

        private static void Shuffle(int[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }
        #endregion
    }
}
