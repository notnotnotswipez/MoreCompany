using HarmonyLib;

namespace MoreCompany
{
    // TODO: THIS IS NOT SUPPOSED TO STAY!!!!! DO NOT LET THIS STAY IN THE FINAL BUILD!!!!
    /*[HarmonyPatch(typeof(Terminal), "Start")]
    public static class TerminalStartDebugMoneyPatch
    {
        public static void Postfix(Terminal __instance)
        {
            if (__instance.IsServer)
            {
                __instance.groupCredits = 1000000;
            }
        }
    }*/
}