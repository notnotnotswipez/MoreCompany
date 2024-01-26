using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace MoreCompany
{
    [HarmonyPatch(typeof(SoundManager), "Start")]
    public static class SoundManagerStartPatch
    {
        public static void Postfix()
        {
            int difference = MainClass.newPlayerCount - 4;
            int originalLength = 4;
            for (int i = 0 ; i < difference; i++) {
                
                Array.Resize(ref SoundManager.Instance.playerVoicePitches, SoundManager.Instance.playerVoicePitches.Length + 1);
                Array.Resize(ref SoundManager.Instance.playerVoicePitchTargets, SoundManager.Instance.playerVoicePitchTargets.Length + 1);
                Array.Resize(ref SoundManager.Instance.playerVoiceVolumes, SoundManager.Instance.playerVoiceVolumes.Length + 1);
                Array.Resize(ref SoundManager.Instance.playerVoicePitchLerpSpeed, SoundManager.Instance.playerVoicePitchLerpSpeed.Length + 1);
                Array.Resize(ref SoundManager.Instance.playerVoiceMixers, SoundManager.Instance.playerVoiceMixers.Length + 1);
                
                // This means that the new player will have the same voice volume as the first player, I don't think AudioMixerGroups can be made at runtime so this is the best I can do
                AudioMixerGroup audioMixerGroup = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(x => x.name.Contains("Voice"));
                
                SoundManager.Instance.playerVoicePitches[originalLength + i] = 1f;
                SoundManager.Instance.playerVoicePitchTargets[originalLength + i] = 1f;
                SoundManager.Instance.playerVoiceVolumes[originalLength + i] = 0.5f;
                SoundManager.Instance.playerVoicePitchLerpSpeed[originalLength + i] = 3f;
                SoundManager.Instance.playerVoiceMixers[originalLength + i] = audioMixerGroup;
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "GetPlayerSpawnPosition")]
    public static class SpawnPositionClampPatch
    {
        public static void Prefix(ref int playerNum, bool simpleTeleport = false)
        {
            if (playerNum > 3)
            {
                playerNum = 3;
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
    public static class OnClientConnectedPatch
    {
	    public static bool Prefix(StartOfRound __instance, ulong clientId)
	    {
		    if (!__instance.IsServer)
		    {
			    return false;
		    }

		    Debug.Log("player connected");
		    Debug.Log(string.Format("connected players #: {0}", __instance.connectedPlayersAmount));
		    try
		    {
			    List<int> list = __instance.ClientPlayerList.Values.ToList<int>();
			    Debug.Log(string.Format("Connecting new player on host; clientId: {0}", clientId));
			    int num = 0;
			    for (int i = 1; i < MainClass.newPlayerCount; i++)
			    {
				    if (!list.Contains(i))
				    {
					    num = i;
					    break;
				    }
			    }

			    __instance.allPlayerScripts[num].actualClientId = clientId;
			    __instance.allPlayerObjects[num].GetComponent<NetworkObject>().ChangeOwnership(clientId);
			    Debug.Log(string.Format("New player assigned object id: {0}", __instance.allPlayerObjects[num]));
			    List<ulong> list2 = new List<ulong>();
			    for (int j = 0; j < __instance.allPlayerObjects.Length; j++)
			    {
				    NetworkObject component = __instance.allPlayerObjects[j].GetComponent<NetworkObject>();
				    if (!component.IsOwnedByServer)
				    {
					    list2.Add(component.OwnerClientId);
				    }
				    else if (j == 0)
				    {
					    list2.Add(NetworkManager.Singleton.LocalClientId);
				    }
				    else
				    {
					    list2.Add(999UL);
				    }
			    }

			    int groupCredits = Object.FindObjectOfType<Terminal>().groupCredits;
			    int profitQuota = TimeOfDay.Instance.profitQuota;
			    int quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
			    int num2 = (int)TimeOfDay.Instance.timeUntilDeadline;

			    ReflectionUtils.InvokeMethod(__instance, "OnPlayerConnectedClientRpc", new object[]
			    {
				    clientId, __instance.connectedPlayersAmount, list2.ToArray(), num,
				    groupCredits, __instance.currentLevelID, profitQuota, num2, quotaFulfilled,
				    __instance.randomMapSeed, 
				    __instance.isChallengeFile
			    });
			    __instance.ClientPlayerList.Add(clientId, num);
			    Debug.Log(string.Format("client id connecting: {0} ; their corresponding player object id: {1}",
				    clientId, num));
		    }
		    catch (Exception ex)
		    {
			    Debug.LogError(string.Format(
				    "Error occured in OnClientConnected! Shutting server down. clientId: {0}. Error: {1}", clientId,
				    ex));
			    GameNetworkManager.Instance.disconnectionReasonMessage =
				    "Error occured when a player attempted to join the server! Restart the application and please report the glitch!";
			    GameNetworkManager.Instance.Disconnect();
		    }

		    return false;
	    }
    }
}