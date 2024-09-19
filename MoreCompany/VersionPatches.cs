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

    [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
    [HarmonyPriority(Priority.Last)]
    public static class OnLobbyCreatedPatch
    {
        private static void Postfix(Steamworks.Result result, ref Lobby lobby)
        {
            if (result != Steamworks.Result.OK)
                return;

            if (lobby.GetData("tag") == "none" && !Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                lobby.SetData("tag", "morecompany");
            }

            lobby.SetData("maxplayers", lobby.MaxMembers.ToString());
        }
    }

    [HarmonyPatch(typeof(LobbyQuery))]
    public static class LobbyQueryPatch
    {
        [HarmonyPatch("ApplyFilters")]
        [HarmonyPrefix]
        public static void ApplyFilters_Prefix(ref LobbyQuery __instance)
        {
            if (!Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility") &&
                !Chainloader.PluginInfos.ContainsKey("Dev1A3.LobbyImprovements"))
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
