using HarmonyLib;

namespace MoreCompany
{
    [HarmonyPatch(typeof(ShipTeleporter), "Awake")]
    public static class ShipTeleporterAwakePatch
    {
        public static void Postfix(ref ShipTeleporter __instance)
        {
            int[] newTeleporterIds = new int[MainClass.newPlayerCount];
            for (int i = 0; i < MainClass.newPlayerCount; i++)
            {
                newTeleporterIds[i] = -1;
            }
            
            ReflectionUtils.SetFieldValue(__instance, "playersBeingTeleported", newTeleporterIds);
        }
    }
}