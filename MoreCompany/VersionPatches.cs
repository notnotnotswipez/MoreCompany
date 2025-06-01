using BepInEx.Bootstrap;
using HarmonyLib;
using Steamworks.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreCompany
{
    [HarmonyPatch(typeof(MenuManager), "Awake")]
    public static class MenuManagerVersionDisplayPatch
    {
        public static void Postfix(MenuManager __instance)
        {
            if (!__instance.isInitScene && __instance.versionNumberText != null)
            {
                __instance.versionNumberText.text = string.Format("{0} (MC)", __instance.versionNumberText.text);
            }
        }
    }

    [HarmonyPatch]
    public static class OnLobbyCreatedPatch
    {
        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPrefix]
        private static void SteamMatchmaking_OnLobbyCreated_Prefix(GameNetworkManager __instance, Steamworks.Result result, ref Lobby lobby)
        {
            if (result != Steamworks.Result.OK)
                return;

            lobby.SetData("maxplayers", lobby.MaxMembers.ToString());
        }

        [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
        [HarmonyPostfix]
        private static void SteamMatchmaking_OnLobbyCreated_Postfix(GameNetworkManager __instance, Steamworks.Result result, ref Lobby lobby)
        {
            if (result != Steamworks.Result.OK)
                return;

            if (lobby.GetData("tag") == "none" && !Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                lobby.SetData("tag", "morecompany");
            }
        }
    }

    [HarmonyPatch(typeof(LobbyQuery))]
    public static class LobbyQueryPatch
    {
        [HarmonyPatch("ApplyFilters")]
        [HarmonyPrefix]
        public static void ApplyFilters_Prefix(ref LobbyQuery __instance)
        {
            if (!Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                bool shouldReplaceTag = false;
                SteamLobbyManager steamLobbyManager = Object.FindFirstObjectByType<SteamLobbyManager>();
                if (steamLobbyManager != null)
                {
                    shouldReplaceTag = steamLobbyManager.serverTagInputField.text == string.Empty;
                }
                else if (__instance.stringFilters == null || !__instance.stringFilters.ContainsKey("tag") || __instance.stringFilters["tag"] == "none")
                {
                    shouldReplaceTag = true;
                }
                if (shouldReplaceTag)
                {
                    if (__instance.stringFilters != null && __instance.stringFilters.ContainsKey("tag"))
                        __instance.stringFilters.Remove("tag");

                    __instance.WithKeyValue("tag", "morecompany");
                }
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
