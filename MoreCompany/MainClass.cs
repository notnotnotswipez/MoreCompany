using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using MelonLoader;
using MoreCompany.Utils;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace MoreCompany
{
    public class MainClass : MelonMod
    {
        public static int newPlayerCount = 8;
        public static List<PlayerControllerB> notSupposedToExistPlayers = new List<PlayerControllerB>();

        public static Texture2D mainLogo;

        public override void OnInitializeMelon()
        {
            
        }

        public override void OnLateInitializeMelon()
        {
            AssetBundle bundle = BundleUtilities.LoadBundleFromInternalAssembly("morecompany.logo");
            Texture2D logo = bundle.LoadPersistentAsset<Texture2D>("assets/morecompanyassets/morecompanytransparentred.png");
            bundle.Unload(false);

            mainLogo = logo;
        }


        public static void ResizePlayerCache(Dictionary<uint, Dictionary<int, NetworkObject>> ScenePlacedObjects)
        {
            StartOfRound round = StartOfRound.Instance;
            if (round.allPlayerObjects.Length != MainClass.newPlayerCount)
            {
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

    

    [HarmonyPatch(typeof(HUDManager), "SyncAllPlayerLevelsServerRpc")]
    public static class HUDManagerBullshitPatch
    {
        public static bool Prefix(HUDManager __instance)
        {
            return false;
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
            maxMembers = MainClass.newPlayerCount;
        }
    }
}
