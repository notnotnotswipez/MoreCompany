using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using Unity.Netcode;
using UnityEngine;

namespace MoreCompany
{
    
    [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
    public static class SendChatToServerPatch
    {
        public static bool Prefix(HUDManager __instance, string chatMessage, int playerId = -1)
        {
            if (StartOfRound.Instance.IsHost)
            {
                // DEBUG COMMANDS
                if (chatMessage.StartsWith("/mc") && DebugCommandRegistry.commandEnabled)
                {
                    String command = chatMessage.Replace("/mc ", "");
                    DebugCommandRegistry.HandleCommand(command.Split(' '));
                    return false;
                }
            }

            if (chatMessage.StartsWith("[morecompanycosmetics]"))
            {
                ReflectionUtils.InvokeMethod(__instance, "AddPlayerChatMessageServerRpc", new object[] {chatMessage, 99});
                return false;
            }
            
            return true;
        }
    }
    
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageServerRpc")]
    public static class ServerReceiveMessagePatch
    {
        public static string previousDataMessage = "";
        
        public static bool Prefix(HUDManager __instance, ref string chatMessage, int playerId)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return false;
            }
            
            if (chatMessage.StartsWith("[morecompanycosmetics]") && networkManager.IsServer)
            {
                previousDataMessage = chatMessage;
                chatMessage = "[replacewithdata]";
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
    public static class ConnectClientToPlayerObjectPatch
    {
        public static void Postfix(PlayerControllerB __instance)
        {
            string built = "[morecompanycosmetics]";
            built += ";" + __instance.playerClientId;
            foreach (var cosmetic in CosmeticRegistry.locallySelectedCosmetics)
            {
                built += ";" + cosmetic;
            }
            HUDManager.Instance.AddTextToChatOnServer(built);
        }
    }
    
    [HarmonyPatch(typeof(HUDManager), "AddChatMessage")]
    public static class AddChatMessagePatch
    {
        public static bool Prefix(HUDManager __instance, string chatMessage, string nameOfUserWhoTyped = "")
        {
            if (chatMessage.StartsWith("[replacewithdata]") || chatMessage.StartsWith("[morecompanycosmetics]"))
            {
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
    public static class ClientReceiveMessagePatch
    {
        public static bool ignoreSample = false;
        
        public static bool Prefix(HUDManager __instance, ref string chatMessage, int playerId)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
            {
                return false;
            }

            if (networkManager.IsServer)
            {
                if (chatMessage.StartsWith("[replacewithdata]"))
                {
                    chatMessage = ServerReceiveMessagePatch.previousDataMessage;
                    HandleDataMessage(chatMessage);
                }
                else if (chatMessage.StartsWith("[morecompanycosmetics]"))
                {
                    // The server already handled this when the server branch was dealing with it.
                    return false;
                }
            }
            else
            {
                if (chatMessage.StartsWith("[morecompanycosmetics]"))
                {
                    HandleDataMessage(chatMessage);
                }
            }

            return true;
        }
        
        private static void HandleDataMessage(string chatMessage)
        {
            if (ignoreSample)
            {
                return;
            }
            chatMessage = chatMessage.Replace("[morecompanycosmetics]", "");
            string[] splitMessage = chatMessage.Split(';');
            string playerIdString = splitMessage[1];

            int playerIdNumeric = int.Parse(playerIdString);
            
            CosmeticApplication existingCosmeticApplication = StartOfRound.Instance.allPlayerScripts[playerIdNumeric].transform.Find("ScavengerModel")
                .Find("metarig").gameObject.GetComponent<CosmeticApplication>();

            if (existingCosmeticApplication)
            {
                existingCosmeticApplication.ClearCosmetics();
                GameObject.Destroy(existingCosmeticApplication);
            }

            CosmeticApplication cosmeticApplication = StartOfRound.Instance.allPlayerScripts[playerIdNumeric].transform.Find("ScavengerModel")
                .Find("metarig").gameObject.AddComponent<CosmeticApplication>();
            
            cosmeticApplication.ClearCosmetics();
            
            List<string> cosmeticsToApply = new List<string>();
            
            foreach (var cosmeticId in splitMessage)
            {
                if (cosmeticId == playerIdString)
                {
                    continue;
                }
                cosmeticsToApply.Add(cosmeticId);

                if (MainClass.showCosmetics)
                {
                    cosmeticApplication.ApplyCosmetic(cosmeticId, true);
                }
            }
            
            if (playerIdNumeric == StartOfRound.Instance.thisClientPlayerId)
            {
                cosmeticApplication.ClearCosmetics();
            }
            
            foreach (var cosmeticSpawned in cosmeticApplication.spawnedCosmetics)
            {
                cosmeticSpawned.transform.localScale *= CosmeticRegistry.COSMETIC_PLAYER_SCALE_MULT;
            }

            MainClass.playerIdsAndCosmetics.Remove(playerIdNumeric);
            MainClass.playerIdsAndCosmetics.Add(playerIdNumeric, cosmeticsToApply);

            if (GameNetworkManager.Instance.isHostingGame && (playerIdNumeric != 0))
            {
                ignoreSample = true;
                foreach (var keyPair in MainClass.playerIdsAndCosmetics)
                {
                    string built = "[morecompanycosmetics]";
                    built += ";" + keyPair.Key;
                    foreach (var cosmetic in keyPair.Value)
                    {
                        built += ";" + cosmetic;
                    }
                    HUDManager.Instance.AddTextToChatOnServer(built);
                }
                
                ignoreSample = false;
            }
        }
    }
}