using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MoreCompany
{
	[HarmonyPatch(typeof(HUDManager), "AddChatMessage")]
	public static class HudChatPatch
	{
		public static void Prefix(HUDManager __instance, ref string chatMessage, string nameOfUserWhoTyped = "")
		{
			if (__instance.lastChatMessage == chatMessage)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(chatMessage);
			for (int i = 0; i < MainClass.newPlayerCount; i++)
			{
				string targetReplacement = $"[playerNum{i}]";
				string replacement = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
				stringBuilder.Replace(targetReplacement, replacement);
			}
			chatMessage = stringBuilder.ToString();
		}
	}

	[HarmonyPatch(typeof(MenuManager), "Awake")]
	public static class MenuManagerLogoOverridePatch
	{
		public static void Postfix(MenuManager __instance)
		{
			GameObject parent = __instance.transform.parent.gameObject;
			GameObject logo = parent.transform.Find("MenuContainer").Find("MainButtons").Find("HeaderImage").gameObject;
			Image image = logo.GetComponent<Image>();
			image.sprite = Sprite.Create(MainClass.mainLogo, new Rect(0, 0, MainClass.mainLogo.width, MainClass.mainLogo.height), new Vector2(0.5f, 0.5f));
		}
	}

	[HarmonyPatch(typeof(HUDManager), "UpdateBoxesSpectateUI")]
	public static class SpectatorBoxUpdatePatch
	{
		public static void Postfix(HUDManager __instance)
		{
			Dictionary<Animator, PlayerControllerB> specPrivateDict = ReflectionUtils.GetFieldValue<Dictionary<Animator, PlayerControllerB>>(__instance, "spectatingPlayerBoxes");
			int xVal = -64;
			int yVal = 0;
			int index = 0;
			
			int ySpacing = -70;
			int xSpacing = 230;
			
			int maxPerRow = 4;
			foreach (var animator in specPrivateDict)
			{

				if (animator.Key.gameObject.activeInHierarchy)
				{
					GameObject gameObject = animator.Key.gameObject;
					RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
					// Use index to determine position
					int row = (int) Math.Floor(((double)index / maxPerRow));
					int col = index % maxPerRow;

					int nextY = col * ySpacing;
					int nextX = row * (xSpacing);
					rectTransform.anchoredPosition = new Vector3(xVal + nextX, yVal + nextY, 0);

					index++;
				}
			}
		}
	}

	[HarmonyPatch(typeof(HUDManager), "FillEndGameStats")]
    public static class HudFillEndGameFix
    {
	    public static bool Prefix(HUDManager __instance, EndOfGameStats stats)
	    {
		    int num = 0;
		    int num2 = 0;
		    for (int i = 0; i < __instance.statsUIElements.playerNamesText.Length; i++)
		    {
			    PlayerControllerB playerControllerB = __instance.playersManager.allPlayerScripts[i];
			    __instance.statsUIElements.playerNamesText[i].text = "";
			    __instance.statsUIElements.playerStates[i].enabled = false;
			    __instance.statsUIElements.playerNotesText[i].text = "Notes: \n";
			    if (playerControllerB.disconnectedMidGame || playerControllerB.isPlayerDead ||
			        playerControllerB.isPlayerControlled)
			    {
				    if (playerControllerB.isPlayerDead)
				    {
					    num++;
				    }
				    else if (playerControllerB.isPlayerControlled)
				    {
					    num2++;
				    }

				    __instance.statsUIElements.playerNamesText[i].text = 
					    __instance.playersManager.allPlayerScripts[i].playerUsername;
				    __instance.statsUIElements.playerStates[i].enabled = true;
				    if (__instance.playersManager.allPlayerScripts[i].isPlayerDead)
				    {
					    if (__instance.playersManager.allPlayerScripts[i].causeOfDeath == CauseOfDeath.Abandoned)
					    {
						    __instance.statsUIElements.playerStates[i].sprite = __instance.statsUIElements.missingIcon;
					    }
					    else
					    {
						    __instance.statsUIElements.playerStates[i].sprite = __instance.statsUIElements.deceasedIcon;
					    }
				    }
				    else
				    {
					    __instance.statsUIElements.playerStates[i].sprite = __instance.statsUIElements.aliveIcon;
				    }

				    for (int j = 0; j < 3; j++)
				    {
					    if (j >= stats.allPlayerStats[i].playerNotes.Count)
					    {
						    break;
					    }

					    TextMeshProUGUI textMeshProUGUI = __instance.statsUIElements.playerNotesText[i];
					    textMeshProUGUI.text =
						    textMeshProUGUI.text + "* " + stats.allPlayerStats[i].playerNotes[j] + "\n";
				    }
			    }
			    else
			    {
				    __instance.statsUIElements.playerNotesText[i].text = "";
			    }
		    }

		    __instance.statsUIElements.quotaNumerator.text = RoundManager.Instance.scrapCollectedInLevel.ToString();
		    __instance.statsUIElements.quotaDenominator.text = RoundManager.Instance.totalScrapValueInLevel.ToString();
		    if (StartOfRound.Instance.allPlayersDead)
		    {
			    __instance.statsUIElements.allPlayersDeadOverlay.enabled = true;
			    __instance.statsUIElements.gradeLetter.text = "F";
			    return false;
		    }

		    __instance.statsUIElements.allPlayersDeadOverlay.enabled = false;
		    int num3 = 0;
		    float num4 = (float)RoundManager.Instance.scrapCollectedInLevel /
		                 RoundManager.Instance.totalScrapValueInLevel;
		    if (num2 == StartOfRound.Instance.connectedPlayersAmount + 1)
		    {
			    num3++;
		    }
		    else if (num > 1)
		    {
			    num3--;
		    }

		    if (num4 >= 0.99f)
		    {
			    num3 += 2;
		    }
		    else if (num4 >= 0.6f)
		    {
			    num3++;
		    }
		    else if (num4 <= 0.25f)
		    {
			    num3--;
		    }

		    switch (num3)
		    {
			    case -1:
				    __instance.statsUIElements.gradeLetter.text = "D";
				    return false;
			    case 0:
				    __instance.statsUIElements.gradeLetter.text = "C";
				    return false;
			    case 1:
				    __instance.statsUIElements.gradeLetter.text = "B";
				    return false;
			    case 2:
				    __instance.statsUIElements.gradeLetter.text = "A";
				    return false;
			    case 3:
				    __instance.statsUIElements.gradeLetter.text = "S";
				    return false;
			    default:
				    return false;
		    }
	    }
    }

    [HarmonyPatch(typeof(HUDManager), "Start")]
    public static class HudStartPatch
    {
        public static void Postfix(HUDManager __instance)
        {
            EndOfGameStatUIElements statUIElements = __instance.statsUIElements;
            GameObject original = statUIElements.playerNamesText[0].gameObject.transform.parent.gameObject;
            GameObject megaPanel = original.transform.parent.parent.gameObject;
            GameObject backGroundBox = megaPanel.transform.Find("BGBoxes").gameObject;
            backGroundBox.transform.localScale = new Vector3(2.5f, 1, 1);
            
            MakePlayerHolder(4, original, statUIElements, new Vector3(426.9556f, -0.7932f, 0));
            MakePlayerHolder(5, original, statUIElements, new Vector3(426.9556f, -115.4483f, 0));
            MakePlayerHolder(6, original, statUIElements, new Vector3(-253.6783f, -115.4483f, 0));
            MakePlayerHolder(7, original, statUIElements, new Vector3(-253.6783f, -0.7932f, 0));
        }

        public static void MakePlayerHolder(int index, GameObject original, EndOfGameStatUIElements uiElements, Vector3 localPosition)
        {
            GameObject spawned = Object.Instantiate(original);
            RectTransform rectTransform = spawned.GetComponent<RectTransform>();
            RectTransform originalRectTransform = original.GetComponent<RectTransform>();
            rectTransform.SetParent(originalRectTransform.parent);
            rectTransform.localScale = new Vector3(1, 1, 1);
            rectTransform.localPosition = localPosition;
            
            GameObject playerName = spawned.transform.Find("PlayerName1").gameObject;
            GameObject playerNotes = spawned.transform.Find("Notes").gameObject;
            playerNotes.GetComponent<RectTransform>().localPosition = new Vector3(-95.7222f, 43.3303f, 0);
            GameObject playerState = spawned.transform.Find("Symbol").gameObject;
            
            // Resize arrays if they are too small
            if (index >= uiElements.playerNamesText.Length)
            {
                Array.Resize(ref uiElements.playerNamesText, index + 1);
                Array.Resize(ref uiElements.playerStates, index + 1);
                Array.Resize(ref uiElements.playerNotesText, index + 1);
            }

            uiElements.playerNamesText[index] = playerName.GetComponent<TextMeshProUGUI>();
            uiElements.playerNotesText[index] = playerNotes.GetComponent<TextMeshProUGUI>();
            uiElements.playerStates[index] = playerState.GetComponent<Image>();
        }
    }

    [HarmonyPatch(typeof(QuickMenuManager), "AddUserToPlayerList")]
    public static class QuickMenuStupidFix
    {
        public static void Prefix(ulong steamId, string playerName, ref int playerObjectId)
        {
            // Should be 3, hes checking 4 for the index in the real game. Silly.
            if (playerObjectId < 0 || playerObjectId > 3)
            {
                playerObjectId = 3;
            }
        }
    }
}