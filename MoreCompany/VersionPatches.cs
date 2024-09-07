using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Steamworks.Data;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoreCompany
{
    [HarmonyPatch(typeof(MenuManager), "Awake")]
    public static class MenuManagerVersionDisplayPatch
    {
        public static void Postfix(MenuManager __instance)
        {
            if (__instance.versionNumberText != null)
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
        }
    }

    [HarmonyPatch(typeof(LobbyQuery))]
    public static class LobbyQueryPatch
    {
        [HarmonyPatch("ApplyFilters")]
        [HarmonyPrefix]
        public static void ApplyFilters_Prefix(Dictionary<string, string> ___stringFilters)
        {
            if (!Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            {
                bool shouldReplaceTag = false;
                SteamLobbyManager steamLobbyManager = Object.FindFirstObjectByType<SteamLobbyManager>();
                if (steamLobbyManager != null)
                {
                    shouldReplaceTag = steamLobbyManager.serverTagInputField.text == string.Empty;
                }
                else if (!___stringFilters.ContainsKey("tag") || ___stringFilters["tag"] == "none")
                {
                    shouldReplaceTag = true;
                }
                if (shouldReplaceTag)
                {
                    ___stringFilters.Remove("tag");
                    ___stringFilters.Add("tag", "morecompany");
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
                AddButtonToCopyLobbyCode(lobbySlot.GetComponentInChildren<Button>(), lobbySlot.lobbyId.Value.ToString(), ["Code", "Copied", "Invalid"]);
            }
        }

        internal static void AddButtonToCopyLobbyCode(Button LobbyJoinBtn, string lobbyCodeStr, string[] textLabels)
        {
            if (LobbyJoinBtn != null)
            {
                var CopyCodeButton = Object.Instantiate(LobbyJoinBtn, LobbyJoinBtn.transform.parent);
                CopyCodeButton.name = "CopyCodeButton";
                RectTransform rectTransform = CopyCodeButton.GetComponent<RectTransform>();
                rectTransform.anchoredPosition -= new Vector2(78f, 0f);
                var LobbyCodeTextMesh = CopyCodeButton.GetComponentInChildren<TextMeshProUGUI>();
                LobbyCodeTextMesh.text = textLabels[0];
                CopyCodeButton.onClick = new Button.ButtonClickedEvent();
                CopyCodeButton.onClick.AddListener(() => CopyLobbyCodeToClipboard(lobbyCodeStr, LobbyCodeTextMesh, textLabels));
            }
        }

        internal static void CopyLobbyCodeToClipboard(string lobbyCode, TextMeshProUGUI textMesh, string[] textLabels)
        {
            if (textMesh.text != textLabels[0]) return;
            GameNetworkManager.Instance.StartCoroutine(LobbySlotCopyCode(lobbyCode, textMesh, textLabels));
        }
        internal static IEnumerator LobbySlotCopyCode(string lobbyCode, TextMeshProUGUI textMesh, string[] textLabels)
        {
            if (!lobbyCode.IsNullOrWhiteSpace())
            {
                GUIUtility.systemCopyBuffer = lobbyCode;
                MainClass.StaticLogger.LogInfo("Lobby code copied to clipboard: " + lobbyCode);
                textMesh.text = textLabels[1];
            }
            else
            {
                textMesh.text = textLabels[2];
            }
            yield return new WaitForSeconds(1.2f);
            textMesh.text = textLabels[0];
            yield break;
        }
    }
}
