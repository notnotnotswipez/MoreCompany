using BepInEx;
using HarmonyLib;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
            if (GameNetworkManager.Instance != null && __instance.versionNumberText != null)
            {
                __instance.versionNumberText.text = string.Format("v{0} (MC)", GameNetworkAwakePatch.originalVersion);
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

                Button LobbyCodeBtn = lobbySlot.GetComponentInChildren<Button>();
                if (LobbyCodeBtn != null)
                {
                    var CopyCodeButton = Object.Instantiate(LobbyCodeBtn, LobbyCodeBtn.transform.parent);
                    CopyCodeButton.name = "CopyCodeButton";
                    RectTransform rectTransform = CopyCodeButton.GetComponent<RectTransform>();
                    rectTransform.anchoredPosition -= new Vector2(78f, 0f);
                    var LobbyCodeTextMesh = CopyCodeButton.GetComponentInChildren<TextMeshProUGUI>();
                    LobbyCodeTextMesh.text = "Code";
                    CopyCodeButton.onClick = new Button.ButtonClickedEvent();
                    CopyCodeButton.onClick.AddListener(() => CopyLobbyCodeToClipboard(lobbySlot.lobbyId.Value.ToString(), LobbyCodeTextMesh, ["Code", "Copied", "Invalid"]));
                }
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
