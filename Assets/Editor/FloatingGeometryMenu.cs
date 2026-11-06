using UnityEditor;
using UnityEngine;

namespace CloneZ.EditorTools
{
    /// <summary>Dev aid: <c>CloneZ &gt; Debug &gt; Report Floating Geometry</c> (Play Mode), see <see cref="global::CloneZ.FloatingGeometryReport"/>.</summary>
    internal static class FloatingGeometryMenu
    {
        private const string MenuPath = "CloneZ/Debug/Report Floating Geometry";

        [MenuItem(MenuPath, true)]
        private static bool CanRun() => Application.isPlaying;

        [MenuItem(MenuPath)]
        private static void Run() => global::CloneZ.FloatingGeometryReport.Run();
    }
}
