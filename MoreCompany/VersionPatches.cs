using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace MoreCompany
{
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
            if (!__instance.isInitScene && __instance.versionNumberText != null)
            {
                __instance.versionNumberText.text = string.Format("{0} (MC)", __instance.versionNumberText.text.Replace($"v{9950 + GameNetworkAwakePatch.originalVersion}", $"v{GameNetworkAwakePatch.originalVersion}"));
            }
        }
    }

    [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
    public static class LoadLobbyListAndFilterPatch
    {
        public static IEnumerator Postfix(IEnumerator result)
        {
            while (result.MoveNext())
                yield return result.Current;

            LobbySlot[] lobbySlots = Object.FindObjectsOfType<LobbySlot>();
            foreach (LobbySlot lobbySlot in lobbySlots)
            {
                lobbySlot.playerCount.text = string.Format("{0} / {1}", lobbySlot.thisLobby.MemberCount, lobbySlot.thisLobby.MaxMembers);
            }
        }
    }
}
