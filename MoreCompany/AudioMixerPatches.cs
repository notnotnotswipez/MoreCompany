using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;

namespace MoreCompany
{
    [HarmonyPatch(typeof(AudioMixer), "SetFloat")]
    public static class AudioMixerSetFloatPatch
    {
        public static bool Prefix(string name, ref float value)
        {
            if (name.StartsWith("PlayerVolume"))
            {
                string cutName = name.Replace("PlayerVolume", "");
                int playerObjectNumber = int.Parse(cutName);

                // Set the vanilla diageticMixer volumes for all player controllers to max
                if (!SoundManagerPatch.initialVolumeSet)
                {
                    MainClass.StaticLogger.LogInfo($"Setting initial volume for {playerObjectNumber} to {value}");
                    return true;
                }

                // Update the actual volume of the voice source since each player doesn't have it's own diagetic mixer group
                PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObjectNumber];
                if (playerControllerB != null)
                {
                    AudioSource voiceSource = playerControllerB.currentVoiceChatAudioSource;
                    if (voiceSource)
                    {
                        voiceSource.volume = value / 16;
                    }
                }

                return false;
            }
            else if (name.StartsWith("PlayerPitch"))
            {
                // Use vanilla pitch if max crew size <= 4 otherwise don't do any further logic
                return MainClass.newPlayerCount <= 4;
            }

            return true;
        }
    }
}
