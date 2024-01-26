using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Audio;

namespace MoreCompany
{
    [HarmonyPatch(typeof(AudioMixer), "SetFloat")]
    public static class AudioMixerSetFloatPatch
    {
        public static bool Prefix(string name, float value)
        {
            if (name.StartsWith("PlayerVolume") || name.StartsWith("PlayerPitch"))
            {
                string cutName = name.Replace("PlayerVolume", "").Replace("PlayerPitch", "");
                int playerObjectNumber = int.Parse(cutName);
                
            

                PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObjectNumber];
                AudioSource voiceSource = playerControllerB.currentVoiceChatAudioSource;
                if (playerControllerB != null && voiceSource)
                {
                    if (name.StartsWith("PlayerVolume"))
                    {
                        voiceSource.volume = value/16;
                    }
                    else if (name.StartsWith("PlayerPitch"))
                    {
                        voiceSource.pitch = value;
                    }
                }
             
                return false;
            }

            return true;
        }
    }
}