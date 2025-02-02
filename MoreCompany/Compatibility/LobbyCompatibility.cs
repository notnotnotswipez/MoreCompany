using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;
using Newtonsoft.Json;
using Steamworks.Data;

namespace MoreCompany.Compatibility
{
    internal class LobbyCompatibility
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Init()
        {
            MainClass.StaticLogger.LogWarning("LobbyCompatibility detected, registering plugin with LobbyCompatibility.");

            Version pluginVersion = Version.Parse(PluginInformation.PLUGIN_VERSION);

            PluginHelper.RegisterPlugin(PluginInformation.PLUGIN_GUID, pluginVersion, CompatibilityLevel.Variable, VersionStrictness.Minor, variableCompatibilityCheck);
        }

        private static string GetData(IEnumerable<KeyValuePair<string, string>> kvpData, string keyName)
        {
            return kvpData.FirstOrDefault(x => x.Key.ToLower() == keyName).Value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CompatibilityLevel variableCompatibilityCheck(IEnumerable<KeyValuePair<string, string>> lobbyData)
        {
            if (int.TryParse(GetData(lobbyData, "maxplayers"), out int maxPlayers) && maxPlayers > 4)
            {
                return CompatibilityLevel.Everyone;
            }

            return CompatibilityLevel.ClientOnly;
        }
    }
}
