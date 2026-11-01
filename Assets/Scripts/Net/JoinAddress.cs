using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Where the title screen's Join points first: <c>-clonez-join &lt;address[:port]&gt;</c> on the command line, else the
    /// <c>server.txt</c> beside the game (a co-op release writes the in-house server's address there; see
    /// Tools/Server/README.md), else the last server joined from the title screen, else this machine.
    /// </summary>
    public static class JoinAddress
    {
        /// <summary>The file beside the game that names the server to join: "address" or "address:port".</summary>
        public const string FileName = "server.txt";
        private const string AddressKey = "CloneZ.JoinAddress", PortKey = "CloneZ.JoinPort";

        /// <summary>The address and port the Join box starts with.</summary>
        public static void Default(out string address, out ushort port)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-clonez-join");
            if (i >= 0 && i + 1 < args.Length && TryParse(args[i + 1], out address, out port)) return;
            if (TryParse(ReadBeside(), out address, out port)) return;
            address = PlayerPrefs.GetString(AddressKey, "127.0.0.1");
            int saved = PlayerPrefs.GetInt(PortKey, DedicatedServer.DefaultPort);
            port = saved > 0 && saved <= ushort.MaxValue ? (ushort)saved : DedicatedServer.DefaultPort;
        }

        /// <summary>Keeps what was joined for next time (it's used when the game has no server.txt beside it).</summary>
        public static void Remember(string address, ushort port)
        {
            if (string.IsNullOrWhiteSpace(address)) return;
            PlayerPrefs.SetString(AddressKey, address.Trim());
            PlayerPrefs.SetInt(PortKey, port);
            PlayerPrefs.Save();
        }

        /// <summary>Reads "address", "address:port" or "[IPv6 address]:port"; without a port it's
        /// <see cref="DedicatedServer.DefaultPort"/>.</summary>
        public static bool TryParse(string text, out string address, out ushort port)
        {
            address = null;
            port = DedicatedServer.DefaultPort;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            int colon = text.LastIndexOf(':');
            bool bracketed = text.StartsWith("[", StringComparison.Ordinal);
            // A bare IPv6 address has colons of its own: only "[v6]:port" or a lone colon carries a port.
            if (colon > 0 && (bracketed ? text.IndexOf("]:", StringComparison.Ordinal) == colon - 1 : text.IndexOf(':') == colon))
            {
                if (!ushort.TryParse(text.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out port) || port == 0) return false;
                text = text.Substring(0, colon);
            }
            address = text.Trim('[', ']');
            return address.Length > 0;
        }

        // The first line of server.txt in the folder that holds the game's executable, or null when there's none.
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
