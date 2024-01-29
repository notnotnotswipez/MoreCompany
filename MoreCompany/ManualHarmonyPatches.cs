using System;
using HarmonyLib;

namespace MoreCompany;

public class ManualHarmonyPatches
{
    public static void ManualPatch(Harmony HarmonyInstance)
    {
        HarmonyInstance.Patch(AccessTools.Method(typeof(HUDManager), "SyncAllPlayerLevelsServerRpc", new Type[0]), new HarmonyMethod(typeof(HUDManagerBullshitPatch).GetMethod("ManualPrefix")));
    }
}