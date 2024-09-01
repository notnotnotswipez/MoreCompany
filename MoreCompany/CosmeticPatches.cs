using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using UnityEngine;

namespace MoreCompany
{
    [HarmonyPatch]
    public class CosmeticPatches
    {
        public static bool CloneCosmeticsToNonPlayer(Transform cosmeticRoot, int playerClientId, bool detachedHead = false, bool startEnabled = true)
        {
            if (MainClass.playerIdsAndCosmetics.ContainsKey(playerClientId))
            {
                List<string> cosmetics = MainClass.playerIdsAndCosmetics[playerClientId];
                CosmeticApplication cosmeticApplication = cosmeticRoot.GetComponent<CosmeticApplication>();
                if (cosmeticApplication)
                {
                    cosmeticApplication.ClearCosmetics();
                    GameObject.Destroy(cosmeticApplication);
                }

                cosmeticApplication = cosmeticRoot.gameObject.AddComponent<CosmeticApplication>();
                cosmeticApplication.detachedHead = detachedHead;
                foreach (var cosmetic in cosmetics)
                {
                    cosmeticApplication.ApplyCosmetic(cosmetic, startEnabled);
                }

                foreach (var cosmetic in cosmeticApplication.spawnedCosmetics)
                {
                    cosmetic.transform.localScale *= CosmeticRegistry.COSMETIC_PLAYER_SCALE_MULT;
                }

                return true;
            }

            return false;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SpawnDeadBody")]
        [HarmonyPostfix]
        public static void SpawnDeadBody(ref PlayerControllerB __instance, int deathAnimation = 0)
        {
            Transform cosmeticRoot = __instance.deadBody.transform;
            if (cosmeticRoot == null) return;
            bool detachedHead = __instance.deadBody.detachedHead;
            if (deathAnimation == 4 || deathAnimation == 5) detachedHead = true; // Masked
            CloneCosmeticsToNonPlayer(cosmeticRoot, (int)__instance.playerClientId, detachedHead: detachedHead, startEnabled: MainClass.cosmeticsDeadBodies.Value);
        }

        // "Why this function? Why not Start/Awake/Another function?" Well, Start and Awake are called on clients that arent the host before the mimicking player is set.
        // And the other functions are called before the mimicking player is set... sometimes. This is the only function that gets called after the mimicking player is set on the client and host consistently.
        [HarmonyPatch(typeof(MaskedPlayerEnemy), "SetEnemyOutside")]
        [HarmonyPostfix]
        public static void SetEnemyOutside(MaskedPlayerEnemy __instance)
        {
            if (__instance.mimickingPlayer == null) return;
            Transform cosmeticRoot = __instance.transform.Find("ScavengerModel").Find("metarig");
            if (cosmeticRoot == null) return;
            CloneCosmeticsToNonPlayer(cosmeticRoot, (int)__instance.mimickingPlayer.playerClientId, detachedHead: false, startEnabled: MainClass.cosmeticsMaskedEnemy.Value);
            __instance.skinnedMeshRenderers = __instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            __instance.meshRenderers = __instance.gameObject.GetComponentsInChildren<MeshRenderer>();
        }

        [HarmonyPatch(typeof(QuickMenuManager), "OpenQuickMenu")]
        [HarmonyPatch(typeof(QuickMenuManager), "CloseQuickMenu")]
        [HarmonyPostfix]
        public static void ToggleQuickMenu(QuickMenuManager __instance)
        {
            if (CosmeticRegistry.menuIsInGame && CosmeticRegistry.cosmeticGUIGlobalScale != null)
            {
                CosmeticRegistry.cosmeticGUIGlobalScale.Find("ActivateButton").gameObject.SetActive(__instance.isMenuOpen);
                if (!__instance.isMenuOpen)
                {
                    CosmeticRegistry.cosmeticGUIGlobalScale.Find("CosmeticsScreen").gameObject.SetActive(false);
                }
            }
        }
    }
}
