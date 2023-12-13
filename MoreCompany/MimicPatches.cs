using System.Collections.Generic;
using HarmonyLib;
using MoreCompany.Cosmetics;
using UnityEngine;

namespace MoreCompany
{
    public class MimicPatches
    {
        
        // "Why this function? Why not Start/Awake/Another function?" Well, Start and Awake are called on clients that arent the host before the mimicking player is set.
        // And the other functions are called before the mimicking player is set... sometimes. This is the only function that gets called after the mimicking player is set on the client and host consistently.
        [HarmonyPatch(typeof(MaskedPlayerEnemy), "SetEnemyOutside")]
        public class MaskedPlayerEnemyOnEnablePatch
        {
            public static void Postfix(MaskedPlayerEnemy __instance)
            {
                if (__instance.mimickingPlayer != null)
                {
                    List<string> cosmetics = MainClass.playerIdsAndCosmetics[(int)__instance.mimickingPlayer.playerClientId];
                    Transform cosmeticRoot = __instance.transform.Find("ScavengerModel").Find("metarig");
                    CosmeticApplication cosmeticApplication = cosmeticRoot.GetComponent<CosmeticApplication>();
                    if (cosmeticApplication)
                    {
                        cosmeticApplication.ClearCosmetics();
                        GameObject.Destroy(cosmeticApplication);
                    }
                    
                    cosmeticApplication = cosmeticRoot.gameObject.AddComponent<CosmeticApplication>();
                    foreach (var cosmetic in cosmetics)
                    {
                        cosmeticApplication.ApplyCosmetic(cosmetic, true);
                    }

                    foreach (var cosmetic in cosmeticApplication.spawnedCosmetics)
                    {
                        cosmetic.transform.localScale *= CosmeticRegistry.COSMETIC_PLAYER_SCALE_MULT;
                    }
                }
            }
        }
    }
}