using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace CloneZ.EditorTools
{
    /// <summary>
    /// Leaves App UI's native plugins out of Dedicated Server builds. App UI comes with com.unity.ai.inference, and its
    /// Linux plugin (a desktop's clipboard, loaded at startup) needs GTK, which a server has no use for and its container
    /// doesn't have: the server logged "Couldn't open libAppUINativePlugin.so ... libgtk-3.so.0" every time it started.
    /// Nothing in a build calls the plugins anyway (APP_UI_EDITOR_ONLY keeps App UI's runtime out). The package's import
    /// settings are read-only in the package cache, so each build decides, before it gathers its plugins.
    /// </summary>
    internal sealed class ServerBuildPlugins : IPreprocessBuildWithReport
    {
        private const string AppUI = "Packages/com.unity.dt.app-ui/";

        private static bool s_ServerBuild;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_ServerBuild = report.summary.platformGroup == BuildTargetGroup.Standalone
                && report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Server;
            foreach (PluginImporter plugin in PluginImporter.GetAllImporters())
                if (plugin.isNativePlugin && plugin.assetPath.StartsWith(AppUI, StringComparison.Ordinal))
                    plugin.SetIncludeInBuildDelegate(_ => !s_ServerBuild);
        }
    }
}
