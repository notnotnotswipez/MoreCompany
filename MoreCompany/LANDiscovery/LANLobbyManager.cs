using System.Collections;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using HarmonyLib;
using UnityEngine.UI;
using System.Linq;

namespace MoreCompany.LANDiscovery
{
    [HarmonyPatch]
    public static class LANLobbyManager
    {
        public static string[] offensiveWords = new string[] {
            "nigger", "faggot", "n1g", "nigers", "cunt", "pussies", "pussy", "minors", "children", "kids",
            "chink", "buttrape", "molest", "rape", "coon", "negro", "beastiality", "cocks", "cumshot", "ejaculate",
            "pedophile", "furfag", "necrophilia", "yiff", "sex", "porn"
        };

        internal static LANLobby[] currentLobbyList;

        internal static ClientDiscovery clientDiscovery;

        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPostfix]
        public static void MM_Start(MenuManager __instance)
        {
            if (GameNetworkManager.Instance.disableSteam)
            {
                __instance.lanButtonContainer?.SetActive(false);
                __instance.joinCrewButtonContainer?.SetActive(true);
            }
        }

        [HarmonyPatch(typeof(SteamLobbyManager), "LoadServerList")]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool LoadServerList(SteamLobbyManager __instance)
        {
            if (GameNetworkManager.Instance.disableSteam)
            {
                GameObject.Find("LobbyList/ListPanel/ToggleChallengeSort")?.SetActive(false); // Hide challenge mode toggle
                GameObject.Find("LobbyList/ListPanel/Dropdown")?.SetActive(false); // Hide sort dropdown
                LoadServerList_LAN(__instance);
                return false;
            }

            return true;
        }

        public static async void LoadServerList_LAN(SteamLobbyManager __instance)
        {
            if (GameNetworkManager.Instance.waitingForLobbyDataRefresh) return;

            if (!clientDiscovery)
                clientDiscovery = new ClientDiscovery();
            if (clientDiscovery.isListening) return;

            if (__instance.loadLobbyListCoroutine != null)
            {
                GameNetworkManager.Instance.StopCoroutine(__instance.loadLobbyListCoroutine);
            }

            __instance.refreshServerListTimer = 0f;
            __instance.serverListBlankText.text = "Loading server list...";
            currentLobbyList = null;
            LANLobbySlot[] array = Object.FindObjectsOfType<LANLobbySlot>();
            for (int i = 0; i < array.Length; i++)
            {
                Object.Destroy(array[i].gameObject);
            }
            GameNetworkManager.Instance.waitingForLobbyDataRefresh = true;
            clientDiscovery.listenPort = MainClass.lanDiscoveryPort.Value;
            LANLobby[] lobbiesArr = (await clientDiscovery.DiscoverLobbiesAsync(2f)).ToArray();
            currentLobbyList = lobbiesArr;
            GameNetworkManager.Instance.waitingForLobbyDataRefresh = false;
            if (currentLobbyList != null)
            {
                if (currentLobbyList.Length == 0)
                {
                    __instance.serverListBlankText.text = "No available servers to join.";
                }
                else
                {
                    __instance.serverListBlankText.text = "";
                }
                __instance.lobbySlotPositionOffset = 0f;
                __instance.loadLobbyListCoroutine = GameNetworkManager.Instance.StartCoroutine(loadLobbyListAndFilter(currentLobbyList, __instance));
            }
            else
            {
                Debug.Log("Lobby list is null after request.");
                __instance.serverListBlankText.text = "No available servers to join.";
            }
        }
        private static IEnumerator loadLobbyListAndFilter(LANLobby[] lobbyList, SteamLobbyManager __instance)
        {
            for (int i = 0; i < lobbyList.Length; i++)
            {
                string lobbyName = lobbyList[i].GetData("name");
                if (lobbyName.Length == 0)
                {
                    Debug.Log("lobby name is length of 0, skipping");
                    continue;
                }

                string lobbyNameNoCapitals = lobbyName.ToLower();
                if (__instance.censorOffensiveLobbyNames && offensiveWords.Any(x => lobbyNameNoCapitals.Contains(x)))
                {
                    Debug.Log("Lobby name is offensive: " + lobbyNameNoCapitals + "; skipping");
                    continue;
                }

                GameObject obj = Object.Instantiate(__instance.LobbySlotPrefab, __instance.levelListContainer);
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f + __instance.lobbySlotPositionOffset);
                __instance.lobbySlotPositionOffset -= 42f;
                LobbySlot originalSlot = obj.GetComponentInChildren<LobbySlot>();

                // NEW CODE
                LANLobbySlot componentInChildren = originalSlot.gameObject.AddComponent<LANLobbySlot>();
                GameObject.Destroy(originalSlot);

                componentInChildren.LobbyName = componentInChildren.transform.Find("ServerName")?.GetComponent<TextMeshProUGUI>();
                if (componentInChildren.LobbyName)
                    componentInChildren.LobbyName.text = lobbyName.Substring(0, Mathf.Min(lobbyName.Length, 40));

                componentInChildren.playerCount = componentInChildren.transform.Find("NumPlayers")?.GetComponent<TextMeshProUGUI>();
                if (componentInChildren.playerCount)
                    componentInChildren.playerCount.text = $"{lobbyList[i].MemberCount} / {lobbyList[i].MaxMembers}";

                componentInChildren.HostName = componentInChildren.transform.Find("HostName")?.GetComponent<TextMeshProUGUI>();
                if (componentInChildren.HostName)
                {
                    componentInChildren.HostName.GetComponent<TextMeshProUGUI>().text = $"Host: {lobbyList[i].IPAddress}";
                    componentInChildren.HostName.gameObject.SetActive(true);
                }

                Button JoinButton = componentInChildren.transform.Find("JoinButton")?.GetComponent<Button>();
                if (JoinButton)
                {
                    JoinButton.onClick = new Button.ButtonClickedEvent();
                    JoinButton.onClick.AddListener(componentInChildren.JoinButton);
                }

                componentInChildren.thisLobby = lobbyList[i];

                yield return null;
            }
        }
    }

    [HarmonyPatch]
    public static class LANHostPatches
    {
        [HarmonyPatch(typeof(MenuManager), "Update")]
        [HarmonyPostfix]
        public static void MenuManager_Update(MenuManager __instance)
        {
            if (GameNetworkManager.Instance == null || !GameNetworkManager.Instance.disableSteam) return;
            if (!__instance.lobbyTagInputField || !__instance.lobbyTagInputField.gameObject || !__instance.lobbyTagInputField.gameObject.activeSelf) return;
            __instance.lobbyTagInputField.gameObject.SetActive(false);
        }

        [HarmonyPatch(typeof(MenuManager), "ClickHostButton")]
        [HarmonyPrefix]
        public static void MenuManager_ClickHostButton(MenuManager __instance)
        {
            Transform lobbyHostOptions = __instance.HostSettingsScreen.transform.Find("HostSettingsContainer/LobbyHostOptions");
            if (lobbyHostOptions != null)
            {
                if (lobbyHostOptions.transform.Find("LANOptions/AllowRemote") && GameNetworkManager.Instance.disableSteam)
                {
                    Object.Destroy(lobbyHostOptions.transform.Find("LANOptions").gameObject);
                    GameObject OptionsNormal = lobbyHostOptions.transform.Find("OptionsNormal").gameObject;
                    GameObject menu = GameObject.Instantiate(OptionsNormal, OptionsNormal.transform.position, OptionsNormal.transform.rotation, OptionsNormal.transform.parent);
                    __instance.HostSettingsOptionsLAN = menu;
                    __instance.HostSettingsOptionsLAN.name = "LANOptions";

                    Transform accessRemoteBtnParent = __instance.HostSettingsOptionsLAN.transform.Find("Public");
                    __instance.lanSetAllowRemoteButtonAnimator = accessRemoteBtnParent.GetComponent<Animator>();
                    Button accessRemoteBtn = accessRemoteBtnParent.GetComponent<Button>();
                    accessRemoteBtn.onClick = new Button.ButtonClickedEvent();
                    accessRemoteBtn.onClick.AddListener(__instance.LAN_HostSetAllowRemoteConnections);
                    accessRemoteBtn.GetComponentInChildren<TextMeshProUGUI>().text = "REMOTE";

                    Transform accessLocalBtnParent = __instance.HostSettingsOptionsLAN.transform.Find("Private");
                    __instance.lanSetLocalButtonAnimator = accessLocalBtnParent.GetComponent<Animator>();
                    Button accessLocalBtn = accessLocalBtnParent.GetComponent<Button>();
                    accessLocalBtn.onClick = new Button.ButtonClickedEvent();
                    accessLocalBtn.onClick.AddListener(__instance.LAN_HostSetLocal);
                    accessLocalBtnParent.GetComponentInChildren<TextMeshProUGUI>().text = "LOCAL";

                    __instance.lobbyNameInputField = __instance.HostSettingsOptionsLAN.transform.Find("ServerNameField").GetComponent<TMP_InputField>();
                    __instance.lobbyTagInputField = __instance.HostSettingsOptionsLAN.transform.Find("ServerTagInputField").GetComponent<TMP_InputField>();
                }
            }
        }
    }
}
