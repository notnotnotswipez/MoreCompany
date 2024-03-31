using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using Steamworks;
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
        public static List<TMP_InputField> inputFields = new List<TMP_InputField>();

        public static void Postfix(MenuManager __instance)
		{
			try
            {
                MainClass.ReadSettingsFromFile();

                GameObject parent = __instance.transform.parent.gameObject;
                Sprite logoImage = Sprite.Create(MainClass.mainLogo, new Rect(0, 0, MainClass.mainLogo.width, MainClass.mainLogo.height), new Vector2(0.5f, 0.5f));

                Transform mainLogo = parent.transform.Find("MenuContainer/MainButtons/HeaderImage");
                if (mainLogo != null)
                {
                    mainLogo.gameObject.GetComponent<Image>().sprite = logoImage;
                }
                Transform loadingScreen = parent.transform.Find("MenuContainer/LoadingScreen");
                if (loadingScreen != null)
                {
                    loadingScreen.localScale = new Vector3(1.02f, 1.06f, 1.02f);
                    Transform loadingLogo = loadingScreen.Find("Image");
                    if (loadingLogo != null)
                    {
                        loadingLogo.GetComponent<Image>().sprite = logoImage;
                    }
                }

                CosmeticRegistry.SpawnCosmeticGUI();

                // Add the crew count input
                LANMenu.InitializeMenu();
                inputFields.Clear();
                Transform lobbyHostOptions = parent.transform.Find("MenuContainer/LobbyHostSettings/HostSettingsContainer/LobbyHostOptions");
                if (lobbyHostOptions != null)
                    CreateCrewCountInput(lobbyHostOptions.Find(GameNetworkManager.Instance.disableSteam ? "LANOptions" : "OptionsNormal"));
                Transform lobbyJoinOptions = parent.transform.Find("MenuContainer/LobbyJoinSettings/JoinSettingsContainer/LobbyJoinOptions");
                if (lobbyJoinOptions != null)
                    CreateCrewCountInput(lobbyJoinOptions.Find("LANOptions"));
            }
			catch (Exception e)
			{
                MainClass.StaticLogger.LogError(e);
			}
        }

        private static void CreateCrewCountInput(Transform parent)
        {
            GameObject createdCrewUI = GameObject.Instantiate(MainClass.crewCountUI, parent);
            RectTransform rectTransform = createdCrewUI.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(96.9f, -70f, -6.7f);

            TMP_InputField inputField = createdCrewUI.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
            inputField.characterLimit = 3;
            inputField.text = MainClass.newPlayerCount.ToString();
            inputFields.Add(inputField);
            inputField.onSubmit.AddListener(s => {
                UpdateTextBox(inputField, s);
            });
            inputField.onDeselect.AddListener(s => {
                UpdateTextBox(inputField, s);
            });
        }

        public static void UpdateTextBox(TMP_InputField inputField, string s)
        {
            if (inputField.text == MainClass.newPlayerCount.ToString())
                return;

            if (int.TryParse(s, out int result))
            {
                int originalCount = MainClass.newPlayerCount;
                MainClass.newPlayerCount = Mathf.Clamp(result, MainClass.minPlayerCount, MainClass.maxPlayerCount);
                foreach (TMP_InputField field in inputFields)
                    field.text = MainClass.newPlayerCount.ToString();
                MainClass.SaveSettingsToFile();
                if (MainClass.newPlayerCount != originalCount)
                    MainClass.StaticLogger.LogInfo($"Changed Crew Count: {MainClass.newPlayerCount}");
            }
            else if (s.Length != 0)
            {
                foreach (TMP_InputField field in inputFields)
                {
                    field.text = MainClass.newPlayerCount.ToString();
                    field.caretPosition = 1;
                }
            }
        }
    }

	[HarmonyPatch(typeof(QuickMenuManager), "AddUserToPlayerList")]
	public static class AddUserPlayerListPatch
    {
        private static bool Prefix(QuickMenuManager __instance, ulong steamId, string playerName, int playerObjectId)
        {
            QuickmenuVisualInjectPatch.PopulateQuickMenu(__instance);
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
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(QuickMenuManager), "NonHostPlayerSlotsEnabled")]
    public static class QuickMenuDisplayPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;

            for (int i = 1; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[i];
                if (playerScript.isPlayerControlled || playerScript.isPlayerDead)
                {
                    __result = true;
                    break;
                }
            }

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

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
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
                    playerVolume.onValueChanged.AddListener(f =>
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
                    });

                    if (StartOfRound.Instance.localPlayerController != null && StartOfRound.Instance.localPlayerController.playerClientId == playerScript.playerClientId)
                    {
                        playerVolume.gameObject.SetActive(false);
                        spawnedPlayer.transform.Find("Text (1)").gameObject.SetActive(false);
                    }

                    Button kickButton = spawnedPlayer.transform.Find("KickButton").GetComponent<Button>();
                    kickButton.onClick.AddListener(() =>
                    {
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
    public static class KickPatch
	{
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool foundCount = false;
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
			{
				if (!alreadyReplaced)
				{
					if (!foundCount && instruction.ToString() == "ldfld int QuickMenuManager::playerObjToKick")
					{
						foundCount = true;
					}
					else if (foundCount && instruction.ToString() == "ldc.i4.3 NULL")
					{
						alreadyReplaced = true;
						CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
						newInstructions.Add(codeInstruction);
                        continue;
					}
				}

				newInstructions.Add(instruction);
            }

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("KickPatch failed to replace newPlayerCount");

            return newInstructions.AsEnumerable();
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
