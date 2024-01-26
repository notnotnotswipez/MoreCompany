using System;
using System.Collections.Generic;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
			try
			{
				GameObject parent = __instance.transform.parent.gameObject;
				GameObject logo = parent.transform.Find("MenuContainer").Find("MainButtons").Find("HeaderImage").gameObject;
				Image image = logo.GetComponent<Image>();
				
				MainClass.ReadSettingsFromFile();

				image.sprite = Sprite.Create(MainClass.mainLogo, new Rect(0, 0, MainClass.mainLogo.width, MainClass.mainLogo.height), new Vector2(0.5f, 0.5f));
				
				CosmeticRegistry.SpawnCosmeticGUI();
			
				// Create the crew text display
				Transform serverOptionsContainer = parent.transform.Find("MenuContainer").Find("LobbyHostSettings").Find("HostSettingsContainer").Find("LobbyHostOptions").Find("OptionsNormal");

				GameObject createdCrewUI = GameObject.Instantiate(MainClass.crewCountUI, serverOptionsContainer);
				RectTransform rectTransform = createdCrewUI.GetComponent<RectTransform>();
				rectTransform.localPosition = new Vector3(96.9f, -70f, -6.7f);
			
				TMP_InputField inputField = createdCrewUI.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
				inputField.characterLimit = 3;
			
				inputField.text = MainClass.newPlayerCount.ToString();
			
				inputField.onValueChanged.AddListener((s =>
				{

					if (int.TryParse(s, out int result))
					{
						MainClass.newPlayerCount = result;
						MainClass.newPlayerCount = Mathf.Clamp(MainClass.newPlayerCount, 1, MainClass.maxPlayerCount);
						inputField.text = MainClass.newPlayerCount.ToString();
						MainClass.SaveSettingsToFile();
					}
					else 
					{
						if (s.Length != 0)
						{
							inputField.text = MainClass.newPlayerCount.ToString();
							inputField.caretPosition = 1;
						}
					}
				}));
			}
			catch (Exception e)
			{
				// Ignore
			}
		}
	}
	
	[HarmonyPatch(typeof(QuickMenuManager), "AddUserToPlayerList")]
	public static class AddUserPlayerListPatch
	{
		public static bool Prefix(QuickMenuManager __instance, ulong steamId, string playerName, int playerObjectId)
		{
			QuickmenuVisualInjectPatch.PopulateQuickMenu(__instance);
			MainClass.EnablePlayerObjectsBasedOnConnected();
			return false;
		}
	}
	
	[HarmonyPatch(typeof(QuickMenuManager), "RemoveUserFromPlayerList")]
	public static class RemoveUserPlayerListPatch
	{
		public static bool Prefix(QuickMenuManager __instance)
		{
			QuickmenuVisualInjectPatch.PopulateQuickMenu(__instance);
			return false;
		}
	}
	
	[HarmonyPatch(typeof(QuickMenuManager), "Update")]
	public static class QuickMenuUpdatePatch
	{
		public static bool Prefix(QuickMenuManager __instance)
		{
			return false;
		}
	}
	
	[HarmonyPatch(typeof(QuickMenuManager), "NonHostPlayerSlotsEnabled")]
	public static class QuickMenuDisplayPatch
	{
		public static bool Prefix(QuickMenuManager __instance, ref bool __result)
		{
			__result = true;
			return false;
		}
	}
	
	[HarmonyPatch(typeof(QuickMenuManager), "Start")]
	public static class QuickmenuVisualInjectPatch
	{
		public static GameObject quickMenuScrollInstance;
		
		public static void Postfix(QuickMenuManager __instance)
		{
			GameObject targetParent = __instance.playerListPanel.transform.Find("Image").gameObject;
			GameObject spawnedQuickmenu = Object.Instantiate(MainClass.quickMenuScrollParent);
			spawnedQuickmenu.transform.SetParent(targetParent.transform);
			RectTransform rectTransform = spawnedQuickmenu.GetComponent<RectTransform>();
			rectTransform.localPosition = new Vector3(0, -31.2f, 0);
			rectTransform.localScale = Vector3.one;

			quickMenuScrollInstance = spawnedQuickmenu;
		}

		public static void PopulateQuickMenu(QuickMenuManager __instance)
		{
			int childCount = quickMenuScrollInstance.transform.Find("Holder").childCount;
			
			List<GameObject> toDestroy = new List<GameObject>();
			for (int i = 0; i < childCount; i++)
			{
				toDestroy.Add(quickMenuScrollInstance.transform.Find("Holder").GetChild(i).gameObject);
			}
			foreach (var gameObject in toDestroy)
			{
				GameObject.Destroy(gameObject);
			}
			if (!StartOfRound.Instance)
			{
				return;
			}

			for  (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[i];
				if (playerScript.isPlayerControlled || playerScript.isPlayerDead)
				{
					GameObject spawnedPlayer = Object.Instantiate(MainClass.playerEntry, quickMenuScrollInstance.transform.Find("Holder"));
					RectTransform playerTransform = spawnedPlayer.GetComponent<RectTransform>();
					playerTransform.localScale = Vector3.one;
					playerTransform.localPosition = new Vector3(0, -playerTransform.localPosition.y, 0);
					
					TextMeshProUGUI playerName = spawnedPlayer.transform.Find("PlayerNameButton").Find("PName").GetComponent<TextMeshProUGUI>();
					playerName.text = playerScript.playerUsername;
					
					Slider playerVolume = spawnedPlayer.transform.Find("PlayerVolumeSlider").GetComponent<Slider>();
					int finalIndex = i;
					playerVolume.onValueChanged.AddListener(((f =>
					{
						if (playerScript.isPlayerControlled || playerScript.isPlayerDead)
						{
							float num = (f / playerVolume.maxValue) + 1f;
							if (num <= -1f)
							{
								SoundManager.Instance.playerVoiceVolumes[finalIndex] = -70f;
							}
							else
							{
								SoundManager.Instance.playerVoiceVolumes[finalIndex] = num;
							}
						}
					})));

					if (StartOfRound.Instance.localPlayerController.playerClientId == playerScript.playerClientId)
					{
						playerVolume.gameObject.SetActive(false);
						spawnedPlayer.transform.Find("Text (1)").gameObject.SetActive(false);
					}

					Button kickButton = spawnedPlayer.transform.Find("KickButton").GetComponent<Button>();
					kickButton.onClick.AddListener(() =>
					{
                        Debug.Log($"[Event]: {finalIndex}");
						__instance.KickUserFromServer(finalIndex);
					});
					
					if (!GameNetworkManager.Instance.isHostingGame)
					{
						kickButton.gameObject.SetActive(false);
					}
					
					Button profileButton = spawnedPlayer.transform.Find("ProfileIcon").GetComponent<Button>();
					profileButton.onClick.AddListener(() =>
					{
						if (GameNetworkManager.Instance.disableSteam)
						{
							return;
						}

						// There seems to be an issue with SteamFriends.OpenUserOverlay. It works technically, but it personally locked my game up. I couldn't exit
						// The overlay. I'm not sure if this is a bug with the game or with SteamFriends.OpenUserOverlay, but I'm going to disable it for now.
						
						 SteamFriends.OpenUserOverlay(playerScript.playerSteamId, "steamid");

					});
				}
			}
		}
				}


    [HarmonyPatch(typeof(QuickMenuManager), "ConfirmKickUserFromServer")]
    public static class TestPatch
	{

		[HarmonyPrefix]
        public static bool Prefix(QuickMenuManager __instance, int ___playerObjToKick)
		{
			Debug.Log(__instance.ConfirmKickUserPanel);
            Debug.Log(___playerObjToKick);
			if (___playerObjToKick > 0)
			{
				StartOfRound.Instance.KickPlayer(___playerObjToKick);
				__instance.ConfirmKickUserPanel.SetActive(value: false);
			} else
			{
				Debug.Log($"[FATAL]: ID: {___playerObjToKick} is the HOST!");
                __instance.ConfirmKickUserPanel.SetActive(value: false);
			}
			return false;
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
            
            // Set death screen to be above the endgamestats to render below it.
            megaPanel.transform.parent.Find("DeathScreen").SetSiblingIndex(3);
            
            backGroundBox.transform.localScale = new Vector3(2.5f, 1, 1);
            
            // Correctly fitting ui
            MakePlayerHolder(4, original, statUIElements, new Vector3(426.9556f, -0.7932f, 0));
            MakePlayerHolder(5, original, statUIElements, new Vector3(426.9556f, -115.4483f, 0));
            MakePlayerHolder(6, original, statUIElements, new Vector3(-253.6783f, -115.4483f, 0));
            MakePlayerHolder(7, original, statUIElements, new Vector3(-253.6783f, -0.7932f, 0));
            
            // We want to hide all other ui elements, this will change once we introduce a new ui
            for (int i = 8; i < MainClass.newPlayerCount; i++)
            {
	            MakePlayerHolder(i, original, statUIElements, new Vector3(10000f, 10000f, 0));
            }
        }

        public static void MakePlayerHolder(int index, GameObject original, EndOfGameStatUIElements uiElements, Vector3 localPosition)
        {
	        if (index+1 > MainClass.newPlayerCount)
	        {
		        return;
	        }

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
}