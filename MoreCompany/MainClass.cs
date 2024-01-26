using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using MoreCompany.Utils;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using Logger = BepInEx.Logging.Logger;
using Object = UnityEngine.Object;

namespace MoreCompany;

public static class PluginInformation
{
    public const string PLUGIN_NAME = "MoreCompany";
    public const string PLUGIN_VERSION = "1.7.6";
    public const string PLUGIN_GUID = "me.swipez.melonloader.morecompany";
}

[BepInPlugin(PluginInformation.PLUGIN_GUID, PluginInformation.PLUGIN_NAME, PluginInformation.PLUGIN_VERSION)]
public class MainClass : BaseUnityPlugin
{
    public static int newPlayerCount = 32;
    public static int maxPlayerCount = 50;
    public static bool showCosmetics = true;
        
    public static List<PlayerControllerB> notSupposedToExistPlayers = new List<PlayerControllerB>();

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
            
        ManualHarmonyPatches.ManualPatch(harmony);

        StaticLogger.LogInfo("Loading MoreCompany...");
            
        StaticLogger.LogInfo("Checking: "+dynamicCosmeticsPath);
            
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
            
        SteamFriends.OnGameLobbyJoinRequested += (lobby, steamId) =>
        {
            newPlayerCount = lobby.MaxMembers;
        };
            
        SteamMatchmaking.OnLobbyEntered += (lobby) =>
        {
            newPlayerCount = lobby.MaxMembers;
        };
            
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
                newPlayerCount = int.Parse(lines[0]);
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
        if (round.allPlayerObjects.Length != MainClass.newPlayerCount)
        {
            playerIdsAndCosmetics.Clear();
            uint starting = 10000;

            int difference = MainClass.newPlayerCount - round.allPlayerObjects.Length;
            int originalLength = round.allPlayerObjects.Length;
            GameObject firstPlayerObject = round.allPlayerObjects[3];
                
            for (int i = 0; i < difference; i++){
                uint newId = starting + (uint) i;
                GameObject copy = GameObject.Instantiate(firstPlayerObject);
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

                copy.name = $"Player ({(4+i)})";

                copy.transform.parent = null;

                PlayerControllerB newPlayerScript = copy.GetComponentInChildren<PlayerControllerB>();
                    

                notSupposedToExistPlayers.Add(newPlayerScript);
                    
                // Reset
                newPlayerScript.playerClientId = (ulong)(4+i);
                newPlayerScript.isPlayerControlled = false;
                newPlayerScript.isPlayerDead = false;
                    
                newPlayerScript.DropAllHeldItems(false, false);
                newPlayerScript.TeleportPlayer(round.notSpawnedPosition.position, false, 0f, false, true);
                UnlockableSuit.SwitchSuitForPlayer(newPlayerScript, 0, false);

                // Resize arrays
                Array.Resize(ref round.allPlayerObjects, round.allPlayerObjects.Length + 1);
                Array.Resize(ref round.allPlayerScripts, round.allPlayerScripts.Length + 1);
                Array.Resize(ref round.gameStats.allPlayerStats, round.gameStats.allPlayerStats.Length + 1);
                Array.Resize(ref round.playerSpawnPositions, round.playerSpawnPositions.Length + 1);
                    
                // Set new player object
                round.allPlayerObjects[originalLength + i] = copy;
                round.gameStats.allPlayerStats[originalLength + i] = new PlayerStats();
                round.allPlayerScripts[originalLength + i] = newPlayerScript;
                round.playerSpawnPositions[originalLength + i] = round.playerSpawnPositions[3];
                    
            }
        }
    }

    public static void EnablePlayerObjectsBasedOnConnected()
    {
        int connectedPlayers = StartOfRound.Instance.connectedPlayersAmount;
        foreach (var playerScript in StartOfRound.Instance.allPlayerScripts)
        {
            for (int j = 0; j < connectedPlayers + 1; j++)
            {
                if (!playerScript.isPlayerControlled)
                {
                    playerScript.gameObject.SetActive(false);
                }
                else
                {
                    playerScript.gameObject.SetActive(true);
                }
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesServerRpc")]
public static class SendNewPlayerValuesServerRpcPatch
{
    public static ulong lastId = 0;
        
    public static void Prefix(PlayerControllerB __instance, ulong newPlayerSteamId)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
        {
            return;
        }

        if (networkManager.IsServer)
        {
            lastId = newPlayerSteamId;
        }
    }
}
    
[HarmonyPatch(typeof(PlayerControllerB), "SendNewPlayerValuesClientRpc")]
public static class SendNewPlayerValuesClientRpcPatch
{
    public static void Prefix(PlayerControllerB __instance, ref ulong[] playerSteamIds)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
        {
            return;
        }
            
        if (StartOfRound.Instance.mapScreen.radarTargets.Count != MainClass.newPlayerCount)
        {
            List<PlayerControllerB> useless = new List<PlayerControllerB>();
            foreach (var notSupposedToBeHerePlayer in MainClass.notSupposedToExistPlayers)
            {
                if (notSupposedToBeHerePlayer)
                {
                    StartOfRound.Instance.mapScreen.radarTargets.Add(new TransformAndName(notSupposedToBeHerePlayer.transform, notSupposedToBeHerePlayer.playerUsername, false));
                }
                else
                {
                    useless.Add(notSupposedToBeHerePlayer);
                }
            }

            MainClass.notSupposedToExistPlayers.RemoveAll(x => useless.Contains(x));
        }

        if (networkManager.IsServer)
        {
            List<ulong> list = new List<ulong>();
            for (int i = 0; i < MainClass.newPlayerCount; i++)
            {
                if (i == (int)__instance.playerClientId)
                {
                    list.Add(SendNewPlayerValuesServerRpcPatch.lastId);
                }
                else
                {
                    list.Add(__instance.playersManager.allPlayerScripts[i].playerSteamId);
                }
            }
            playerSteamIds = list.ToArray();
        }
    }
}

    
public static class HUDManagerBullshitPatch
{
    public static bool ManualPrefix(HUDManager __instance)
    {
        return false;
    }
}
    
[HarmonyPatch(typeof(StartOfRound), "SyncShipUnlockablesClientRpc")]
public static class SyncShipUnlockablePatch
{
    public static void Prefix(StartOfRound __instance, ref int[] playerSuitIDs, bool shipLightsOn, Vector3[] placeableObjectPositions, Vector3[] placeableObjectRotations, int[] placeableObjects, int[] storedItems, int[] scrapValues, int[] itemSaveData)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
        {
            return;
        }

        if (networkManager.IsServer)
        {
            int[] array = new int[MainClass.newPlayerCount];
            for (int i = 0; i < MainClass.newPlayerCount; i++)
            {
                array[i] = __instance.allPlayerScripts[i].currentSuitID;
            }
                
            playerSuitIDs = array;
        }
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
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}
    
[HarmonyPatch(typeof(NetworkConnectionManager), "HandleConnectionApproval")]
public static class ConnectionApprovalTest
{
    public static void Prefix(ulong ownerClientId, NetworkManager.ConnectionApprovalResponse response)
    {
        if (StartOfRound.Instance)
        {
            if (StartOfRound.Instance.connectedPlayersAmount >= MainClass.newPlayerCount)
            {
                response.Approved = false;
                response.Reason = "Server is full";
            }
            else
            {
                response.Approved = true;
            }
        }
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