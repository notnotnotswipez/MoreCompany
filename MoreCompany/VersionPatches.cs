using HarmonyLib;
using UnityEngine;

namespace MoreCompany
{
    [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter", MethodType.Enumerator)]
    public static class LoadLobbyListAndFilterPatch
    {
        private static void Postfix()
        {
            LobbySlot[] lobbySlots = Object.FindObjectsOfType<LobbySlot>();
            foreach (LobbySlot lobbySlot in lobbySlots)
            {
                lobbySlot.playerCount.text = string.Format("{0} / {1}", lobbySlot.thisLobby.MemberCount, lobbySlot.thisLobby.MaxMembers);
            }
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "Awake")]
    public static class GameNetworkAwakePatch
    {
        public static int originalVersion = 0;

        public static void Postfix(GameNetworkManager __instance)
        {
            originalVersion = __instance.gameVersionNum;

            // LC_API compatibility.
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("LC_API"))
            {
	            __instance.gameVersionNum = 9950 + originalVersion;
            }
        }
    }

    [HarmonyPatch(typeof(MenuManager), "Awake")]
    public static class MenuManagerVersionDisplayPatch
    {
        public static void Postfix(MenuManager __instance)
        {
            if (GameNetworkManager.Instance != null && __instance.versionNumberText != null)
            {
                __instance.versionNumberText.text = string.Format("v{0} (MC)", GameNetworkAwakePatch.originalVersion);
            }
        }
    }
}
