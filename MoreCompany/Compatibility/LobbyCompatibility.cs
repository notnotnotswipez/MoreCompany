using System;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace MoreCompany.Compatibility
{
    internal class LobbyCompatibility
    {
        public static void Init()
        {
            MainClass.StaticLogger.LogWarning("LobbyCompatibility detected, registering plugin with LobbyCompatibility.");

            Version pluginVersion = Version.Parse(PluginInformation.PLUGIN_VERSION);

            PluginHelper.RegisterPlugin(PluginInformation.PLUGIN_GUID, pluginVersion, CompatibilityLevel.Everyone, VersionStrictness.Minor);
        }
    }
}
