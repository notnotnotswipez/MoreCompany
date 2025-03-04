using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using DunGen;
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


    [HarmonyPatch(typeof(MenuManager), "ClickHostButton")]
    [HarmonyPriority(Priority.Last)]
    public static class MenuManagerHost
    {
        public static void UpdateTextBox(TMP_InputField inputField, string s)
        {
            if (inputField.text == MainClass.newPlayerCount.ToString())
                return;

            if (int.TryParse(s, out int result))
            {
                int originalCount = MainClass.newPlayerCount;
                MainClass.newPlayerCount = Mathf.Clamp(result, MainClass.minPlayerCount, MainClass.maxPlayerCount);
                inputField.text = MainClass.newPlayerCount.ToString();
                if (MainClass.newPlayerCount != originalCount)
                    MainClass.StaticLogger.LogInfo($"Changed Crew Count: {MainClass.newPlayerCount}");

                MainClass.playerCount.Value = MainClass.newPlayerCount;
                MainClass.StaticConfig.Save();
            }
            else if (s.Length != 0)
            {
                inputField.text = MainClass.newPlayerCount.ToString();
                inputField.caretPosition = 1;
            }
        }

        public static void SetupCrewCountInput(TMP_InputField inputField)
        {
            inputField.text = MainClass.newPlayerCount.ToString();

            if (!inputField.transform.Find("Registered"))
            {
                inputField.onSubmit.AddListener(s =>
                {
                    UpdateTextBox(inputField, s);
                });
                inputField.onDeselect.AddListener(s =>
                {
                    UpdateTextBox(inputField, s);
                });

                GameObject registered = new GameObject("Registered");
                registered.transform.parent = inputField.transform;
            }
        }

        public static void CreateCrewCountInput(MenuManager __instance)
        {
            if (GameObject.Find("MC_CrewCount"))
            {
                TMP_InputField inputField = GameObject.Find("MC_CrewCount").GetComponentInChildren<TMP_InputField>();
                SetupCrewCountInput(inputField);
            }
            else
            {
                Transform lobbyHostOptions = __instance.HostSettingsScreen.transform.Find("HostSettingsContainer/LobbyHostOptions");
                if (lobbyHostOptions != null)
                {
                    Transform parent = lobbyHostOptions.Find(__instance.HostSettingsOptionsLAN.activeSelf ? "LANOptions" : "OptionsNormal");
                    if (parent != null)
                    {
                        GameObject createdCrewUI = GameObject.Instantiate(MainClass.crewCountUI, parent);
                        createdCrewUI.name = "MC_CrewCount";
                        RectTransform rectTransform = createdCrewUI.GetComponent<RectTransform>();
                        rectTransform.localPosition = new Vector3(96.9f, -70f, -6.7f);

                        TMP_InputField inputField = createdCrewUI.transform.GetComponentInChildren<TMP_InputField>();
                        inputField.characterLimit = 3;
                        SetupCrewCountInput(inputField);
                    }
                }
            }
        }

        public static void Postfix(MenuManager __instance)
        {
            MainClass.newPlayerCount = Mathf.Max(MainClass.minPlayerCount, MainClass.playerCount.Value);
            CreateCrewCountInput(__instance);
        }
    }

    [HarmonyPatch]
    public static class MenuManagerLogoOverridePatch
    {
        [HarmonyPatch(typeof(MenuManager), "Awake")]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Awake_Postfix(MenuManager __instance)
        {
            if (__instance.isInitScene) return;

            // Add the MoreCompany logo
            try
            {
                Sprite logoImage = Sprite.Create(MainClass.mainLogo, new Rect(0, 0, MainClass.mainLogo.width, MainClass.mainLogo.height), new Vector2(0.5f, 0.5f));

                GameObject parent = __instance.transform.parent.gameObject;

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
            }
            catch (Exception e)
            {
                MainClass.StaticLogger.LogError(e);
            }

            try
            {
                LANMenu.InitializeMenu();
            }
            catch (Exception e)
            {
                MainClass.StaticLogger.LogError(e);
            }

            CosmeticRegistry.SpawnCosmeticGUI(true);
        }

        public static bool lanWarningShown = false;
        [HarmonyPatch(typeof(MenuManager), "Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(MenuManager __instance)
        {
            if (!__instance.isInitScene && GameNetworkManager.Instance.disableSteam)
            {
                if (lanWarningShown)
                    __instance.lanWarningContainer.SetActive(false);
                else
                    lanWarningShown = true;
            }
        }
    }

    [HarmonyPatch(typeof(QuickMenuManager), "Start")]
    public static class QuickmenuVisualInjectPatch
    {
        public static void Postfix(QuickMenuManager __instance)
        {
            CosmeticRegistry.SpawnCosmeticGUI(false);

            GameObject targetParent = __instance.playerListPanel.transform.Find("Image").gameObject;
            GameObject spawnedQuickmenu = Object.Instantiate(MainClass.quickMenuScrollParent, targetParent.transform);
            RectTransform rectTransform = spawnedQuickmenu.GetComponent<RectTransform>();
            rectTransform.localPosition = new Vector3(0, -31.2f, 0);
            rectTransform.localScale = Vector3.one;

            Transform quickMenuScrollHolder = spawnedQuickmenu.transform.Find("Holder");
            if (quickMenuScrollHolder != null)
            {
                Array.Resize(ref __instance.playerListSlots, MainClass.newPlayerCount);
                for (int i = 0; i < MainClass.newPlayerCount; i++)
                {
                    int finalIndex = i;

                    if (__instance.playerListSlots[finalIndex] == null)
                        __instance.playerListSlots[finalIndex] = new PlayerListSlot();

                    if (__instance.playerListSlots[finalIndex].slotContainer != null)
                        GameObject.Destroy(__instance.playerListSlots[finalIndex].slotContainer);

                    GameObject spawnedPlayer = Object.Instantiate(MainClass.playerEntry, quickMenuScrollHolder);
                    spawnedPlayer.SetActive(false);
                    spawnedPlayer.name = "PlayerListSlot" + finalIndex;
                    __instance.playerListSlots[finalIndex].slotContainer = spawnedPlayer;

                    RectTransform playerTransform = spawnedPlayer.GetComponent<RectTransform>();
                    playerTransform.localScale = Vector3.one;
                    playerTransform.localPosition = new Vector3(0, -playerTransform.localPosition.y, 0);

                    __instance.playerListSlots[finalIndex].usernameHeader = spawnedPlayer.transform.Find("PlayerNameButton").Find("PName").GetComponent<TextMeshProUGUI>();

                    __instance.playerListSlots[finalIndex].volumeSlider = spawnedPlayer.transform.Find("PlayerVolumeSlider").GetComponent<Slider>();
                    __instance.playerListSlots[finalIndex].volumeSliderContainer = __instance.playerListSlots[finalIndex].volumeSlider.gameObject;
                    __instance.playerListSlots[finalIndex].volumeSlider.onValueChanged.AddListener(async f =>
                    {
                        float num = (f - __instance.playerListSlots[finalIndex].volumeSlider.minValue) / (__instance.playerListSlots[finalIndex].volumeSlider.maxValue - __instance.playerListSlots[finalIndex].volumeSlider.minValue);
                        if (num <= 0f)
                        {
                            num = -70f;
                        }
                        SoundManager.Instance.playerVoiceVolumes[finalIndex] = num;
                        if (!GameNetworkManager.Instance.disableSteam && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != finalIndex)
                        {
                            // This prevents the saved data from being overwritten while loading
                            if (AddUserPlayerListPatch.loadingVolumeData) return;
                            await Coroutines.SaveVolumeAsync(StartOfRound.Instance.allPlayerScripts[finalIndex].playerSteamId.ToString(), num);
                        }
                    });

                    Button profileButton = spawnedPlayer.transform.Find("ProfileIcon").GetComponent<Button>();
                    profileButton.onClick.AddListener(() => __instance.OpenUserSteamProfile(finalIndex));

                    Button kickButton = spawnedPlayer.transform.Find("KickButton").GetComponent<Button>();
                    kickButton.onClick.AddListener(() => __instance.KickUserFromServer(finalIndex));
                    __instance.playerListSlots[finalIndex].KickUserButton = kickButton.gameObject;
                }
            }
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

    [HarmonyPatch(typeof(QuickMenuManager), "AddUserToPlayerList")]
    public static class AddUserPlayerListPatch
    {
        private static void Prefix(QuickMenuManager __instance, ulong steamId, string playerName, int playerObjectId)
        {
            if (playerObjectId <= 4) return;

            __instance.playerListSlots[playerObjectId].KickUserButton.SetActive(StartOfRound.Instance.IsServer);
            __instance.playerListSlots[playerObjectId].slotContainer.SetActive(true);
            __instance.playerListSlots[playerObjectId].isConnected = true;
            __instance.playerListSlots[playerObjectId].playerSteamId = steamId;
            __instance.playerListSlots[playerObjectId].usernameHeader.text = playerName;
        }
        public static bool loadingVolumeData = false;
        private static void Postfix(QuickMenuManager __instance, ulong steamId, string playerName, int playerObjectId)
        {
            if (playerObjectId < 0) return;

            Slider volumeSlider = __instance.playerListSlots[playerObjectId].volumeSlider;
            loadingVolumeData = true;

            float volumeValue = ES3.Load(steamId.ToString(), "morecompanyonlinevolumedata", SoundManager.Instance.playerVoiceVolumes[playerObjectId]);
            volumeValue = Mathf.Clamp(volumeValue * (volumeSlider.maxValue - volumeSlider.minValue) + volumeSlider.minValue, volumeSlider.minValue, volumeSlider.maxValue);

            volumeSlider.value = volumeValue;
            loadingVolumeData = false;

            __instance.StartCoroutine(Coroutines.UpdatePlayerListSlotVisibility(__instance, playerObjectId));
        }

    }
    public static class Coroutines
    {
        public static IEnumerator UpdatePlayerListSlotVisibility(QuickMenuManager __instance, int playerObjectId)
        {
            while (GameNetworkManager.Instance.localPlayerController == null)
            {
                yield return null;
            }

            bool IsLocalPlayer = playerObjectId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
            __instance.playerListSlots[playerObjectId].KickUserButton.SetActive(!IsLocalPlayer);
            __instance.playerListSlots[playerObjectId].volumeSliderContainer.SetActive(!IsLocalPlayer);
            __instance.playerListSlots[playerObjectId].slotContainer.transform.Find("Text (1)")?.gameObject.SetActive(!IsLocalPlayer);
        }

        public static async Task SaveVolumeAsync(string steamId, float volumeValue)
        {
            await Task.Run(() =>
            {
                try
                {
                    ES3.Save(steamId, volumeValue, "morecompanyonlinevolumedata");
                    // MainClass.StaticLogger.LogDebug($"Saved value: {volumeValue}, steamId: {steamId}");
                }
                catch (Exception e)
                {
                    MainClass.StaticLogger.LogWarning($"Error while trying to save volume data: {e}");
                }
            });
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
                    int row = (int)Math.Floor(((double)index / maxPerRow));
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
            if (index + 1 > MainClass.newPlayerCount)
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
