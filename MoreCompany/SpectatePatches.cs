using GameNetcodeStuff;
using HarmonyLib;

namespace MoreCompany
{
    [HarmonyPatch(typeof(PlayerControllerB), "SpectateNextPlayer")]
    public static class SpectatePatches
    {
        public static bool Prefix(PlayerControllerB __instance)
        {
            int num = 0;
            if (__instance.spectatedPlayerScript != null)
            {
                num = (int)__instance.spectatedPlayerScript.playerClientId;
            }
            for (int i = 0; i < MainClass.newPlayerCount; i++)
            {
                num = (num + 1) % MainClass.newPlayerCount;
                if (!__instance.playersManager.allPlayerScripts[num].isPlayerDead && __instance.playersManager.allPlayerScripts[num].isPlayerControlled && __instance.playersManager.allPlayerScripts[num] != __instance)
                {
                    __instance.spectatedPlayerScript = __instance.playersManager.allPlayerScripts[num];
                    __instance.SetSpectatedPlayerEffects(false);
                    return false;
                }
            }
            if (__instance.deadBody != null && __instance.deadBody.gameObject.activeSelf)
            {
                __instance.spectateCameraPivot.position = __instance.deadBody.bodyParts[0].position;
                ReflectionUtils.InvokeMethod(__instance, "RaycastSpectateCameraAroundPivot", null);
            }
            StartOfRound.Instance.SetPlayerSafeInShip();
            return false;
        }
    }
}