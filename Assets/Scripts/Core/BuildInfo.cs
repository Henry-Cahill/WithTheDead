using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Which build of the game this is. A co-op release (Tools/Server/Release.ps1) stamps the player's version with when
    /// it was built and the commit it came from ("0.1.0+20261101.1215.dfb11e17"), the same for the server and for the
    /// game handed to players, so a session can tell two builds apart. The editor keeps the project's plain version.
    /// </summary>
    public static class BuildInfo
    {
        private static string s_Version;

        /// <summary>This build's version (the stamped one in a release).</summary>
        public static string Version => s_Version ?? (s_Version = Application.version);

        /// <summary>True in a player a release built (its version carries the "+stamp").</summary>
        public static bool Stamped => !Application.isEditor && Version.IndexOf('+') >= 0;
    }
}
