using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace MoreCompany
{
    [HarmonyPatch(typeof(SoundManager), "Start")]
    public static class SoundManagerStartPatch
    {
        public static void Postfix(ref SoundManager __instance)
        {
            Array.Resize(ref __instance.playerVoicePitchLerpSpeed, MainClass.newPlayerCount);
            Array.Resize(ref __instance.playerVoicePitchTargets, MainClass.newPlayerCount);
            Array.Resize(ref __instance.playerVoicePitches, MainClass.newPlayerCount);
            Array.Resize(ref __instance.playerVoiceVolumes, MainClass.newPlayerCount);
            Array.Resize(ref __instance.playerVoiceMixers, MainClass.newPlayerCount);

            AudioMixerGroup audioMixerGroup = Resources.FindObjectsOfTypeAll<AudioMixerGroup>().FirstOrDefault(x => x.name.StartsWith("VoicePlayer"));
            for (int i = 0; i < MainClass.newPlayerCount; i++)
            {
                __instance.playerVoicePitchLerpSpeed[i] = 3f;
                __instance.playerVoicePitchTargets[i] = 1f;
                __instance.playerVoicePitches[i] = 1f;
                __instance.playerVoiceVolumes[i] = 0.5f;
				if (!__instance.playerVoiceMixers[i])
				{
                    AudioMixerGroup newAudioMixerGroup = GameObject.Instantiate(audioMixerGroup);
                    newAudioMixerGroup.name = $"VoicePlayer{i}";
                    __instance.playerVoiceMixers[i] = newAudioMixerGroup;
				}
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "GetPlayerSpawnPosition")]
    public static class SpawnPositionClampPatch
    {
        public static void Prefix(ref StartOfRound __instance, ref int playerNum, bool simpleTeleport = false)
        {
			if (!__instance.playerSpawnPositions[playerNum])
			{
				playerNum = __instance.playerSpawnPositions.Length - 1;
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "OnClientConnect")]
    public static class OnClientConnectedPatch
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
					if (!foundCount && instruction.ToString() == "callvirt virtual bool System.Collections.Generic.List<int>::Contains(int item)")
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

            if (!alreadyReplaced) MainClass.StaticLogger.LogWarning("OnClientConnect failed to replace newPlayerCount");

            return newInstructions.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(StartOfRound), "OnPlayerDC")]
    public static class OnPlayerDCPatch
    {
        public static void Postfix(int playerObjectNumber, ulong clientId)
        {
            if (MainClass.playerIdsAndCosmetics.ContainsKey(playerObjectNumber))
            {
                MainClass.playerIdsAndCosmetics.Remove(playerObjectNumber);
            }
        }
    }
}
