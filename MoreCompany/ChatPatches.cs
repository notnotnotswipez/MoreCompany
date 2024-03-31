using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using Unity.Netcode;

namespace MoreCompany
{
    [HarmonyPatch(typeof(HUDManager), "AddTextToChatOnServer")]
    public static class SendChatToServerPatch
    {
        public static bool Prefix(string chatMessage, int playerId = -1)
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

            return true;
        }
    }

    [HarmonyPatch]
    public static class ClientReceiveMessagePatch
    {
        internal static MethodInfo AddTextMessageServerRpc = AccessTools.Method(typeof(HUDManager), "AddTextMessageServerRpc");
        internal static FieldInfo __rpc_exec_stage = AccessTools.Field(typeof(NetworkBehaviour), "__rpc_exec_stage");
        internal enum __RpcExecStage
        {
            None,
            Server,
            Client
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void ConnectClientToPlayerObject_Postfix(PlayerControllerB __instance)
        {
            MainClass.playerIdsAndCosmetics.Clear();

            string built = $"[morecompanycosmetics];{__instance.playerClientId};-1";
            foreach (var cosmetic in CosmeticRegistry.locallySelectedCosmetics)
            {
                if (CosmeticRegistry.cosmeticInstances.ContainsKey(cosmetic))
                {
                    built += ";" + cosmetic;
                }
            }
            AddTextMessageServerRpc?.Invoke(HUDManager.Instance, new object[] { built });
        }

        [HarmonyPatch(typeof(HUDManager), "AddTextMessageServerRpc")]
        [HarmonyPostfix]
        public static void AddTextMessageServerRpc_Postfix(HUDManager __instance, string chatMessage)
        {
            if (chatMessage.StartsWith("[morecompanycosmetics]"))
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                {
                    return;
                }

                if ((__RpcExecStage)__rpc_exec_stage.GetValue(__instance) != __RpcExecStage.Server && networkManager.IsHost)
                {
                    string[] splitMessage = chatMessage.Split(';');
                    int senderId = int.Parse(splitMessage[1]);
                    int targetId = int.Parse(splitMessage[2]);
                    if (targetId == -1)
                    {
                        foreach (var keyPair in MainClass.playerIdsAndCosmetics.ToList())
                        {
                            if (keyPair.Key == senderId) { continue; }

                            string built = $"[morecompanycosmetics];{keyPair.Key};{senderId}";
                            foreach (var cosmetic in keyPair.Value)
                            {
                                built += ";" + cosmetic;
                            }
                            AddTextMessageServerRpc?.Invoke(__instance, new object[] { built });
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(HUDManager), "AddTextMessageClientRpc")]
        [HarmonyPrefix]
        public static void AddTextMessageClientRpc_Prefix(HUDManager __instance, string chatMessage)
        {
            if (chatMessage.StartsWith("[morecompanycosmetics]"))
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                {
                    return;
                }

                if ((__RpcExecStage)__rpc_exec_stage.GetValue(__instance) == __RpcExecStage.Client && (networkManager.IsClient || networkManager.IsHost))
                {
                    HandleDataMessage(chatMessage);
                }
            }
        }

        internal static void HandleDataMessage(string chatMessage)
        {
            string[] splitMessage = chatMessage.Split(';');
            int senderId = int.Parse(splitMessage[1]);
            int targetId = int.Parse(splitMessage[2]);
            splitMessage = splitMessage.Skip(3).ToArray();

            if (targetId != -1 && targetId != StartOfRound.Instance.thisClientPlayerId) { return; }

            CosmeticApplication cosmeticApplication = StartOfRound.Instance.allPlayerScripts[senderId].transform.Find("ScavengerModel")
                .Find("metarig").gameObject.GetComponent<CosmeticApplication>();

            if (!cosmeticApplication)
            {
                cosmeticApplication = StartOfRound.Instance.allPlayerScripts[senderId].transform.Find("ScavengerModel")
                .Find("metarig").gameObject.AddComponent<CosmeticApplication>();
            }

            cosmeticApplication.ClearCosmetics();
            
            List<string> cosmeticsToApply = new List<string>();
            foreach (string cosmeticId in splitMessage)
            {
                cosmeticsToApply.Add(cosmeticId);

                if (MainClass.cosmeticsSyncOther.Value)
                {
                    cosmeticApplication.ApplyCosmetic(cosmeticId, true);
                }
            }

            if (senderId == StartOfRound.Instance.thisClientPlayerId)
            {
                cosmeticApplication.ClearCosmetics();
            }

            foreach (var cosmeticSpawned in cosmeticApplication.spawnedCosmetics)
            {
                cosmeticSpawned.transform.localScale *= CosmeticRegistry.COSMETIC_PLAYER_SCALE_MULT;
            }

            if (MainClass.playerIdsAndCosmetics.ContainsKey(senderId))
            {
                MainClass.playerIdsAndCosmetics[senderId] = cosmeticsToApply;
            }
            else
            {
                MainClass.playerIdsAndCosmetics.Add(senderId, cosmeticsToApply);
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

        [HarmonyPatch(typeof(HUDManager), "AddPlayerChatMessageClientRpc")]
        [HarmonyPrefix]
        public static bool AddPlayerChatMessageClientRpc_Prefix(string chatMessage, int playerId)
        {
            if (chatMessage.StartsWith("[replacewithdata]") || chatMessage.StartsWith("[morecompanycosmetics]"))
            {
                return false;
            }

            return true;
        }
    }
}
