using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using MoreCompany.Utils;
using Steamworks;
using Unity.Netcode;
using UnityEngine;

namespace MoreCompany
{
    public static class PluginInformation
    {
        public const string PLUGIN_NAME = "MoreCompany";
        public const string PLUGIN_VERSION = "1.8.1";
        public const string PLUGIN_GUID = "me.swipez.melonloader.morecompany";
    }

    [BepInPlugin(PluginInformation.PLUGIN_GUID, PluginInformation.PLUGIN_NAME, PluginInformation.PLUGIN_VERSION)]
    public class MainClass : BaseUnityPlugin
    {
        public static int newPlayerCount = 32;
        public static int minPlayerCount = 4;
        public static int maxPlayerCount = 50;
        public static bool showCosmetics = true;

        public static Texture2D mainLogo;
        public static GameObject quickMenuScrollParent;

        public static GameObject playerEntry;
        public static GameObject crewCountUI;

        public static GameObject cosmeticGUIInstance;
        public static GameObject cosmeticButton;

        public static ManualLogSource StaticLogger;

        public static Dictionary<int, List<string>> playerIdsAndCosmetics = new Dictionary<int, List<string>>();

        public static string cosmeticSavePath = Application.persistentDataPath + "/morecompanycosmetics.txt";
        public static string moreCompanySave = Application.persistentDataPath + "/morecompanysave.txt";

        public static string dynamicCosmeticsPath = Paths.PluginPath + "/MoreCompanyCosmetics";

        private void Awake()
        {
            StaticLogger = Logger;
            Harmony harmony = new Harmony(PluginInformation.PLUGIN_GUID);

            try
            {
                harmony.PatchAll();
            }
            catch (Exception e)
            {
                StaticLogger.LogError("Failed to patch: " + e);
            }

            StaticLogger.LogInfo("Loading MoreCompany...");

            SteamFriends.OnGameLobbyJoinRequested += (lobby, steamId) =>
            {
                newPlayerCount = lobby.MaxMembers;
            };

            SteamMatchmaking.OnLobbyEntered += (lobby) =>
            {
                newPlayerCount = lobby.MaxMembers;
            };

            StaticLogger.LogInfo("Checking: " + dynamicCosmeticsPath);
            if (!Directory.Exists(dynamicCosmeticsPath))
            {
                StaticLogger.LogInfo("Creating cosmetics directory");
                Directory.CreateDirectory(dynamicCosmeticsPath);
            }

            ReadSettingsFromFile();
            ReadCosmeticsFromFile();
            StaticLogger.LogInfo("Read settings and cosmetics");

            AssetBundle bundle = BundleUtilities.LoadBundleFromInternalAssembly("morecompany.assets", Assembly.GetExecutingAssembly());
            AssetBundle cosmeticsBundle = BundleUtilities.LoadBundleFromInternalAssembly("morecompany.cosmetics", Assembly.GetExecutingAssembly());
            CosmeticRegistry.LoadCosmeticsFromBundle(cosmeticsBundle);
            cosmeticsBundle.Unload(false);

            StaticLogger.LogInfo("Loading USER COSMETICS...");
            RecursiveCosmeticLoad(Paths.PluginPath);

            LoadAssets(bundle);

            StaticLogger.LogInfo("Loaded MoreCompany FULLY");
        }

        private void RecursiveCosmeticLoad(string directory)
        {
            foreach (var subDirectory in Directory.GetDirectories(directory))
            {
                RecursiveCosmeticLoad(subDirectory);
            }

            foreach (var file in Directory.GetFiles(directory))
            {
                if (file.EndsWith(".cosmetics"))
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(file);
                    CosmeticRegistry.LoadCosmeticsFromBundle(bundle);
                    bundle.Unload(false);
                }
            }
        }

        private void ReadCosmeticsFromFile()
        {
            if (System.IO.File.Exists(cosmeticSavePath))
            {
                string[] lines = System.IO.File.ReadAllLines(cosmeticSavePath);
                foreach (var line in lines)
                {
                    CosmeticRegistry.locallySelectedCosmetics.Add(line);
                }
            }
        }

        public static void WriteCosmeticsToFile()
        {
            string built = "";
            foreach (var cosmetic in CosmeticRegistry.locallySelectedCosmetics)
            {
                built += cosmetic + "\n";
            }
            System.IO.File.WriteAllText(cosmeticSavePath, built);
        }

        public static void SaveSettingsToFile()
        {
            string built = "";
            built += newPlayerCount + "\n";
            built += showCosmetics + "\n";
            System.IO.File.WriteAllText(moreCompanySave, built);
        }

        public static void ReadSettingsFromFile()
        {
            if (System.IO.File.Exists(moreCompanySave))
            {
                string[] lines = System.IO.File.ReadAllLines(moreCompanySave);
                try
                {
                    newPlayerCount = Mathf.Clamp(int.Parse(lines[0]), minPlayerCount, maxPlayerCount);
                    showCosmetics = bool.Parse(lines[1]);
                }
                catch (Exception e)
                {
                    StaticLogger.LogError("Failed to read settings from file, resetting to default");
                    newPlayerCount = 32;
                    showCosmetics = true;
                }
            }
        }

        private static void LoadAssets(AssetBundle bundle)
        {
            if (bundle)
            {
                mainLogo = bundle.LoadPersistentAsset<Texture2D>("assets/morecompanyassets/morecompanytransparentred.png");
                quickMenuScrollParent = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/quickmenuoverride.prefab");
                playerEntry = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/playerlistslot.prefab");
                cosmeticGUIInstance = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/testoverlay.prefab");
                cosmeticButton = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/cosmeticinstance.prefab");
                crewCountUI = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/crewcountfield.prefab");
                bundle.Unload(false);
            }
        }

        public static void ResizePlayerCache(Dictionary<uint, Dictionary<int, NetworkObject>> ScenePlacedObjects)
        {
            StartOfRound round = StartOfRound.Instance;
            if (round.allPlayerObjects.Length != newPlayerCount)
            {
                StaticLogger.LogInfo($"ResizePlayerCache: {newPlayerCount}");
                playerIdsAndCosmetics.Clear();
                uint starting = 10000;

                int originalLength = round.allPlayerObjects.Length;

                int difference = newPlayerCount - originalLength;

                Array.Resize(ref round.allPlayerObjects, newPlayerCount);
                Array.Resize(ref round.allPlayerScripts, newPlayerCount);
                Array.Resize(ref round.gameStats.allPlayerStats, newPlayerCount);
                Array.Resize(ref round.playerSpawnPositions, newPlayerCount);

                StaticLogger.LogInfo($"Resizing player cache from {originalLength} to {newPlayerCount} with difference of {difference}");

                if (difference > 0)
                {
                    //GameObject playerPrefab = round.playerPrefab;
                    //GameObject firstPlayerObject = round.allPlayerObjects[0];
                    GameObject firstPlayerObject = round.allPlayerObjects[3];
                    for (int i = 0; i < difference; i++)
                    {
                        uint newId = starting + (uint)i;
                        //GameObject copy = GameObject.Instantiate(playerPrefab, firstPlayerObject.transform.parent);
                        GameObject copy = GameObject.Instantiate(firstPlayerObject, firstPlayerObject.transform.parent);
                        NetworkObject copyNetworkObject = copy.GetComponent<NetworkObject>();
                        ReflectionUtils.SetFieldValue(copyNetworkObject, "GlobalObjectIdHash", (uint) newId);
                        int handle = copyNetworkObject.gameObject.scene.handle;
                        uint globalObjectIdHash = newId;

                        if (!ScenePlacedObjects.ContainsKey(globalObjectIdHash))
                        {
                            ScenePlacedObjects.Add(globalObjectIdHash, new Dictionary<int, NetworkObject>());
                        }
                        if (ScenePlacedObjects[globalObjectIdHash].ContainsKey(handle))
                        {
                            string text = ((ScenePlacedObjects[globalObjectIdHash][handle] != null) ? ScenePlacedObjects[globalObjectIdHash][handle].name : "Null Entry");
                            throw new Exception(copyNetworkObject.name + " tried to registered with ScenePlacedObjects which already contains " + string.Format("the same {0} value {1} for {2}!", "GlobalObjectIdHash", globalObjectIdHash, text));
                        }
                        ScenePlacedObjects[globalObjectIdHash].Add(handle, copyNetworkObject);

                        copy.name = $"Player ({4 + i})";

                        PlayerControllerB newPlayerScript = copy.GetComponentInChildren<PlayerControllerB>();

                        // Reset
                        newPlayerScript.playerClientId = (ulong)(4 + i);
                        newPlayerScript.playerUsername = $"Player #{newPlayerScript.playerClientId}";
                        newPlayerScript.isPlayerControlled = false;
                        newPlayerScript.isPlayerDead = false;

                        newPlayerScript.DropAllHeldItems(false, false);
                        newPlayerScript.TeleportPlayer(round.notSpawnedPosition.position, false, 0f, false, true);
                        UnlockableSuit.SwitchSuitForPlayer(newPlayerScript, 0, false);

                        // Set new player object
                        round.allPlayerObjects[originalLength + i] = copy;
                        round.gameStats.allPlayerStats[originalLength + i] = new PlayerStats();
                        round.allPlayerScripts[originalLength + i] = newPlayerScript;
                        round.playerSpawnPositions[originalLength + i] = round.playerSpawnPositions[3];

                        StartOfRound.Instance.mapScreen.radarTargets.Add(new TransformAndName(newPlayerScript.transform, newPlayerScript.playerUsername, false));
                    }
                }
            }

            foreach (PlayerControllerB newPlayerScript in StartOfRound.Instance.allPlayerScripts) // Fix for billboards showing as Player # with no number in LAN (base game issue)
            {
                newPlayerScript.usernameBillboardText.text = newPlayerScript.playerUsername;
                newPlayerScript.gameObject.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Start")]
    public static class PlayerControllerBStartPatch
    {
        public static void Postfix(ref PlayerControllerB __instance)
        {
            Collider[] nearByPlayers = new Collider[MainClass.newPlayerCount];
            ReflectionUtils.SetFieldValue(__instance, "nearByPlayers", nearByPlayers);
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesServerRpc")]
    public static class SendNewPlayerValuesServerRpcPatch
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
                    if (!foundCount && instruction.ToString() == "callvirt virtual void System.Collections.Generic.List<ulong>::Add(ulong item)")
                    {
                        foundCount = true;
                    }
                    else if (foundCount && instruction.ToString() == "ldc.i4.4 NULL")
                    {
                        alreadyReplaced = true;
                        CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(HUDManager), "SyncAllPlayerLevelsServerRpc", new Type[] {})]
    public static class SyncAllPlayerLevelsPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            foreach (var instruction in instructions)
            {
                if (instruction.ToString() == "ldc.i4.4 NULL")
                {
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                    newInstructions.Add(codeInstruction);
                    continue;
                }

                newInstructions.Add(instruction);
            }

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch]
    public static class SyncShipUnlockablesPatch
    {
        [HarmonyPatch(typeof(StartOfRound), "SyncShipUnlockablesServerRpc")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ServerTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool foundCount = false;
            int alreadyReplaced = 0;
            foreach (var instruction in instructions)
            {
                if (alreadyReplaced != 2)
                {
                    if (!foundCount && instruction.ToString() == "callvirt bool Unity.Netcode.NetworkManager::get_IsHost()")
                    {
                        foundCount = true;
                    }
                    else if (instruction.ToString().StartsWith("ldc.i4.4 NULL"))
                    {
                        alreadyReplaced++;
                        CodeInstruction codeInstruction = new CodeInstruction(instruction);
                        codeInstruction.opcode = OpCodes.Ldsfld;
                        codeInstruction.operand = AccessTools.Field(typeof(MainClass), "newPlayerCount");
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            return newInstructions.AsEnumerable();
        }

        [HarmonyPatch(typeof(StartOfRound), "SyncShipUnlockablesClientRpc")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ClientTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool foundCount = false;
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced)
                {
                    if (!foundCount && instruction.ToString() == "callvirt void UnityEngine.Renderer::set_sharedMaterial(UnityEngine.Material value)")
                    {
                        foundCount = true;
                    }
                    else if (foundCount && instruction.ToString() == "ldc.i4.4 NULL")
                    {
                        alreadyReplaced = true;
                        CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(NetworkSceneManager), "PopulateScenePlacedObjects")]
    public static class ScenePlacedObjectsInitPatch
    {
        public static void Postfix(ref Dictionary<uint, Dictionary<int, NetworkObject>> ___ScenePlacedObjects)
        {
            MainClass.ResizePlayerCache(___ScenePlacedObjects);
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "LobbyDataIsJoinable")]
    public static class LobbyDataJoinablePatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>();
            bool foundCount = false;
            bool alreadyReplaced = false;
            foreach (var instruction in instructions)
            {
                if (!alreadyReplaced)
                {
                    if (!foundCount && instruction.ToString() == "call int Steamworks.Data.Lobby::get_MemberCount()")
                    {
                        foundCount = true;
                    }
                    else if (foundCount && instruction.ToString() == "ldc.i4.4 NULL")
                    {
                        alreadyReplaced = true;
                        CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "maxPlayerCount"));
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(SteamMatchmaking), "CreateLobbyAsync")]
    public static class LobbyThingPatch
    {
        public static void Prefix(ref int maxMembers)
        {
            MainClass.ReadSettingsFromFile();
            maxMembers = MainClass.newPlayerCount;
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "ConnectionApproval")]
    public static class ConnectionApproval
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
                    if (!foundCount && instruction.ToString() == "ldfld int GameNetworkManager::connectedPlayers")
                    {
                        foundCount = true;
                    }
                    else if (foundCount && instruction.ToString() == "ldc.i4.4 NULL")
                    {
                        alreadyReplaced = true;
                        CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MainClass), "newPlayerCount"));
                        newInstructions.Add(codeInstruction);
                        continue;
                    }
                }

                newInstructions.Add(instruction);
            }

            return newInstructions.AsEnumerable();
        }

        private static void Postfix(ref GameNetworkManager __instance, ref NetworkManager.ConnectionApprovalRequest request, ref NetworkManager.ConnectionApprovalResponse response)
        {
            // LAN Crew Size Mismatch
            if (response.Approved && __instance.disableSteam)
            {
                string @string = Encoding.ASCII.GetString(request.Payload);
                string[] array = @string.Split(",");
                if (!string.IsNullOrEmpty(@string) && (array.Length < 2 || array[1] != MainClass.newPlayerCount.ToString()))
                {
                    response.Reason = $"Crew size mismatch! Their size: {MainClass.newPlayerCount}. Your size: {array[1]}";
                    response.Approved = false;
                }
            }
        }
    }

    [HarmonyPatch]
    public static class TogglePlayerObjectsPatch
    {
        [HarmonyPatch(typeof(StartOfRound), "Update")]
        [HarmonyPrefix]
        private static void SORUpdate(StartOfRound __instance, bool ___hasHostSpawned)
        {
            if (GameNetworkManager.Instance != null && __instance.IsServer && !___hasHostSpawned)
            {
                __instance.allPlayerObjects[0].gameObject.SetActive(true);
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPrefix]
        private static void ConnectClientToPlayerObject()
        {
            foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
            {
                bool flag = playerControllerB.isPlayerControlled || playerControllerB.isPlayerDead;
                if (flag)
                {
                    playerControllerB.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerConnectedClientRpc")]
        [HarmonyPrefix]
        private static void OnPlayerConnectedClientRpc(StartOfRound __instance, ulong clientId, int connectedPlayers, ulong[] connectedPlayerIdsOrdered, int assignedPlayerObjectId, int serverMoneyAmount, int levelID, int profitQuota, int timeUntilDeadline, int quotaFulfilled, int randomSeed, bool isChallenge)
        {
            __instance.allPlayerScripts[assignedPlayerObjectId].gameObject.SetActive(true);
        }

        [HarmonyPatch(typeof(StartOfRound), "OnPlayerDC")]
        [HarmonyPostfix]
        private static void OnPlayerDC(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            __instance.allPlayerScripts[playerObjectNumber].gameObject.SetActive(false);
        }
    }
}
