using System.Linq;
using HarmonyLib;
using MoreCompany.Utils;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace MoreCompany;

[HarmonyPatch(typeof(SteamLobbyManager), "LoadServerList")]
public static class LoadServerListPatch
{
    public static bool Prefix(SteamLobbyManager __instance)
    {
        OverrideMethod(__instance);
        return false;
    }

    private static async void OverrideMethod(SteamLobbyManager __instance)
    {
        if (!GameNetworkManager.Instance.waitingForLobbyDataRefresh)
        {
            ReflectionUtils.SetFieldValue(__instance, "refreshServerListTimer", 0f);
            __instance.serverListBlankText.text = "Loading server list...";

            Lobby[] currentLobbyList = ReflectionUtils.GetFieldValue<Lobby[]>(__instance, "currentLobbyList");

            currentLobbyList = null;
            LobbySlot[] array = Object.FindObjectsOfType<LobbySlot>();
            for (int i = 0; i < array.Length; i++)
            {
                Object.Destroy(array[i].gameObject);
            }

            switch (__instance.sortByDistanceSetting)
            {
                case 0:
                    SteamMatchmaking.LobbyList.FilterDistanceClose();
                    break;
                case 1:
                    SteamMatchmaking.LobbyList.FilterDistanceFar();
                    break;
                case 2:
                    SteamMatchmaking.LobbyList.FilterDistanceWorldwide();
                    break;
            }

            currentLobbyList = null;
            Debug.Log("Requested server list");
            GameNetworkManager.Instance.waitingForLobbyDataRefresh = true;
            Lobby[] array2;
            switch (__instance.sortByDistanceSetting)
            {
                case 0:
                    array2 = await SteamMatchmaking.LobbyList.FilterDistanceClose().WithSlotsAvailable(1)
                        .WithKeyValue("vers", GameNetworkManager.Instance.gameVersionNum.ToString())
                        .RequestAsync();
                    currentLobbyList = array2;
                    goto IL_2E8;
                case 1:
                    array2 = await SteamMatchmaking.LobbyList.FilterDistanceFar().WithSlotsAvailable(1)
                        .WithKeyValue("vers", GameNetworkManager.Instance.gameVersionNum.ToString())
                        .RequestAsync();
                    currentLobbyList = array2;
                    goto IL_2E8;
            }

            array2 = await SteamMatchmaking.LobbyList.FilterDistanceWorldwide().WithSlotsAvailable(1)
                .WithKeyValue("vers", GameNetworkManager.Instance.gameVersionNum.ToString())
                .RequestAsync();
            currentLobbyList = array2;
            IL_2E8:
            GameNetworkManager.Instance.waitingForLobbyDataRefresh = false;
            if (currentLobbyList != null)
            {
                Debug.Log("Got lobby list!");
                ReflectionUtils.InvokeMethod(__instance, "DebugLogServerList", null);
                if (currentLobbyList.Length == 0)
                {
                    __instance.serverListBlankText.text = "No available servers to join.";
                }
                else
                {
                    __instance.serverListBlankText.text = "";
                }

                ReflectionUtils.SetFieldValue(__instance, "lobbySlotPositionOffset", 0f);
                for (int j = 0; j < currentLobbyList.Length; j++)
                {
                    Friend[] array3 = SteamFriends.GetBlocked().ToArray<Friend>();
                    if (array3 != null)
                    {
                        for (int k = 0; k < array3.Length; k++)
                        {
                            Debug.Log(string.Format("blocked user: {0}; id: {1}", array3[k].Name, array3[k].Id));
                            if (currentLobbyList[j].IsOwnedBy(array3[k].Id))
                            {
                                Debug.Log("Hiding lobby by blocked user: " + array3[k].Name);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Blocked users list is null");
                    }

                    GameObject gameObject = Object.Instantiate<GameObject>(__instance.LobbySlotPrefab,
                        __instance.levelListContainer);
                    gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f,
                        0f + ReflectionUtils.GetFieldValue<float>(__instance, "lobbySlotPositionOffset"));
                    ReflectionUtils.SetFieldValue(__instance, "lobbySlotPositionOffset",
                        ReflectionUtils.GetFieldValue<float>(__instance, "lobbySlotPositionOffset") - 42f);
                    LobbySlot componentInChildren = gameObject.GetComponentInChildren<LobbySlot>();
                    componentInChildren.LobbyName.text = currentLobbyList[j].GetData("name");
                    componentInChildren.playerCount.text = string.Format("{0} / {1}",
                        currentLobbyList[j].MemberCount, currentLobbyList[j].MaxMembers);
                    componentInChildren.lobbyId = currentLobbyList[j].Id;
                    componentInChildren.thisLobby = currentLobbyList[j];

                    ReflectionUtils.SetFieldValue(__instance, "currentLobbyList", currentLobbyList);
                }
            }
            else
            {
                Debug.Log("Lobby list is null after request.");
                __instance.serverListBlankText.text = "No available servers to join.";
            }
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
        if (!AssemblyChecker.HasAssemblyLoaded("lc_api"))
        {
            __instance.gameVersionNum = 9999;
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