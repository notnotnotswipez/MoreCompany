using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using Newtonsoft.Json;
using Unity.Collections;
using Unity.Netcode;

namespace MoreCompany
{
    [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
    public static class SendChatToServerPatch
    {
        public static bool Prefix(string chatMessage, int playerId = -1)
        {
            if (DebugCommandRegistry.commandEnabled && StartOfRound.Instance.IsHost && chatMessage.StartsWith("/mc"))
            {
                String command = chatMessage.Replace("/mc ", "");
                DebugCommandRegistry.HandleCommand(command.Split(' '));
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch]
    public static class CosmeticSyncPatch
    {
        // This method runs whenever a player's cosmetics are updated
        public static void UpdateCosmeticsForPlayer(int playerClientId, List<string> splitMessage)
        {
            CosmeticApplication cosmeticApplication = StartOfRound.Instance.allPlayerScripts[playerClientId].transform.Find("ScavengerModel")
                .Find("metarig").gameObject.GetComponent<CosmeticApplication>();

            if (!cosmeticApplication)
            {
                cosmeticApplication = StartOfRound.Instance.allPlayerScripts[playerClientId].transform.Find("ScavengerModel")
                .Find("metarig").gameObject.AddComponent<CosmeticApplication>();
            }

            cosmeticApplication.parentType = ParentType.Player;

            cosmeticApplication.ClearCosmetics();
            List<string> cosmeticsToApply = new List<string>();
            foreach (string cosmeticId in splitMessage)
            {
                if (cosmeticApplication.ApplyCosmetic(cosmeticId, false))
                {
                    cosmeticsToApply.Add(cosmeticId);
                }
            }

            foreach (var cosmeticSpawned in cosmeticApplication.spawnedCosmetics)
            {
                cosmeticSpawned.transform.localScale *= CosmeticRegistry.COSMETIC_PLAYER_SCALE_MULT;
            }

            cosmeticApplication.UpdateAllCosmeticVisibilities(playerClientId == StartOfRound.Instance.thisClientPlayerId);

            if (MainClass.playerIdsAndCosmetics.ContainsKey(playerClientId))
            {
                MainClass.playerIdsAndCosmetics[playerClientId] = cosmeticsToApply;
            }
            else
            {
                MainClass.playerIdsAndCosmetics.Add(playerClientId, cosmeticsToApply);
            }
        }

        public static void SV_ReceiveCosmetics(ulong senderId, FastBufferReader messagePayload)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                messagePayload.ReadValueSafe(out ulong playerClientId);
                messagePayload.ReadValueSafe(out string cosmeticsStr);
                messagePayload.ReadValueSafe(out bool requestAll);

                List<string> cosmetics = cosmeticsStr.Split(',').ToList();
                UpdateCosmeticsForPlayer((int)playerClientId, cosmetics);
                MainClass.StaticLogger.LogInfo($"Server received {cosmetics.Count} cosmetics from {playerClientId}");

                // Sync the sender's cosmetics to all clients
                int writeSize = FastBufferWriter.GetWriteSize(playerClientId) + FastBufferWriter.GetWriteSize(cosmeticsStr);
                var writer = new FastBufferWriter(writeSize, Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(playerClientId);
                    writer.WriteValueSafe(cosmeticsStr);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("MC_CL_ReceiveCosmetics", writer, NetworkDelivery.Reliable);
                }

                // Sync cosmetics of all clients back to the newly joined client
                if (senderId != NetworkManager.ServerClientId && requestAll)
                {
                    string allCosmeticsStr = JsonConvert.SerializeObject(MainClass.playerIdsAndCosmetics);
                    int writeSizeAll = FastBufferWriter.GetWriteSize(allCosmeticsStr);
                    var writerAll = new FastBufferWriter(writeSizeAll, Allocator.Temp);
                    using (writerAll)
                    {
                        writerAll.WriteValueSafe(allCosmeticsStr);
                        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("MC_CL_ReceiveAllCosmetics", senderId, writerAll, NetworkDelivery.Reliable);
                    }
                }
            }
        }

        public static void CL_ReceiveAllCosmetics(ulong senderId, FastBufferReader messagePayload)
        {
            messagePayload.ReadValueSafe(out string cosmeticsStr);
            Dictionary<int, List<string>> tmpPlayerIdsAndCosmetics = JsonConvert.DeserializeObject<Dictionary<int, List<string>>>(cosmeticsStr);
            foreach (var tmpVal in tmpPlayerIdsAndCosmetics)
            {
                UpdateCosmeticsForPlayer(tmpVal.Key, tmpVal.Value);
            }
            MainClass.StaticLogger.LogInfo($"Client received {tmpPlayerIdsAndCosmetics.Sum(x => x.Value.Count)} cosmetics from {tmpPlayerIdsAndCosmetics.Keys.Count} players");
        }

        public static void CL_ReceiveCosmetics(ulong senderId, FastBufferReader messagePayload)
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                messagePayload.ReadValueSafe(out ulong playerClientId);
                messagePayload.ReadValueSafe(out string cosmeticsStr);
                List<string> cosmetics = cosmeticsStr.Split(',').ToList();
                UpdateCosmeticsForPlayer((int)playerClientId, cosmetics);
                MainClass.StaticLogger.LogInfo($"Client received {cosmetics.Count} cosmetics from {playerClientId}");
            }
        }

        public static void SyncCosmeticsToOtherClients(PlayerControllerB playerControllerTmp = null, bool disabled = false, bool requestAll = false)
        {
            PlayerControllerB playerController = playerControllerTmp ?? StartOfRound.Instance?.localPlayerController;
            if (playerController != null)
            {
                List<string> cosmetics = CosmeticRegistry.GetCosmeticsToSync();
                string cosmeticsStr = disabled ? "" : string.Join(',', cosmetics);
                int writeSize = FastBufferWriter.GetWriteSize(playerController.playerClientId) + FastBufferWriter.GetWriteSize(cosmeticsStr) + FastBufferWriter.GetWriteSize(requestAll);
                var writer = new FastBufferWriter(writeSize, Allocator.Temp);
                using (writer)
                {
                    writer.WriteValueSafe(playerController.playerClientId);
                    writer.WriteValueSafe(cosmeticsStr);
                    writer.WriteValueSafe(requestAll);
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("MC_SV_SyncCosmetics", NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
                }
                MainClass.StaticLogger.LogInfo($"Sending {cosmetics.Count} cosmetics to the server | Request All: {requestAll}");
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject(PlayerControllerB __instance)
        {
            MainClass.playerIdsAndCosmetics.Clear();

            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("MC_SV_SyncCosmetics", SV_ReceiveCosmetics);
            }
            else
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("MC_CL_ReceiveCosmetics", CL_ReceiveCosmetics);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("MC_CL_ReceiveAllCosmetics", CL_ReceiveAllCosmetics);
            }

            SyncCosmeticsToOtherClients(playerControllerTmp: __instance, requestAll: true);
        }

        [HarmonyPatch(typeof(GameNetworkManager), "SetInstanceValuesBackToDefault")]
        [HarmonyPostfix]
        public static void SetInstanceValuesBackToDefault()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("MC_CL_ReceiveCosmetics");
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("MC_CL_ReceiveAllCosmetics");
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("MC_SV_SyncCosmetics");
                MainClass.StaticLogger.LogInfo("Unregistered Named Message Handlers");
            }
        }
    }

    [HarmonyPatch]
    public static class PreventOldVersionChatSpamPatch
    {
        [HarmonyPatch(typeof(HUDManager), "AddChatMessage")]
        [HarmonyPrefix]
        public static bool AddChatMessage_Prefix(string chatMessage, string nameOfUserWhoTyped = "")
        {
            if (chatMessage.StartsWith("[replacewithdata]") || chatMessage.StartsWith("[morecompanycosmetics]"))
            {
                return false;
            }

            return true;
        }
    }
}
