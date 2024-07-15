using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using UnityEngine;

namespace MoreCompany
{
    [HarmonyPatch]
    public class CosmeticPatches
    {
        public static bool CloneCosmeticsToNonPlayer(Transform cosmeticRoot, int playerClientId, bool detachedHead = false)
        {
            if (MainClass.cosmeticsSyncOther.Value && MainClass.playerIdsAndCosmetics.ContainsKey(playerClientId))
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
                    cosmeticApplication.ApplyCosmetic(cosmetic, true);
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
        public static void Postfix(ref PlayerControllerB __instance, int playerId, Vector3 bodyVelocity, int causeOfDeath, PlayerControllerB deadPlayerController, int deathAnimation = 0, Transform overridePosition = null, Vector3 positionOffset = default(Vector3))
        {
            Transform cosmeticRoot = __instance.deadBody.transform;
            if (cosmeticRoot == null) return;
            bool detachedHead = __instance.deadBody.detachedHead;
            if (deathAnimation == 4 || deathAnimation == 5) detachedHead = true; // Masked
            CloneCosmeticsToNonPlayer(cosmeticRoot, (int)__instance.playerClientId, detachedHead: detachedHead);
        }

        // "Why this function? Why not Start/Awake/Another function?" Well, Start and Awake are called on clients that arent the host before the mimicking player is set.
        // And the other functions are called before the mimicking player is set... sometimes. This is the only function that gets called after the mimicking player is set on the client and host consistently.
        [HarmonyPatch(typeof(MaskedPlayerEnemy), "SetEnemyOutside")]
        [HarmonyPostfix]
        public static void SetEnemyOutside(ref MaskedPlayerEnemy __instance)
        {
            if (__instance.mimickingPlayer != null)
            {
                Transform cosmeticRoot = __instance.transform.Find("ScavengerModel").Find("metarig");
                CloneCosmeticsToNonPlayer(cosmeticRoot, (int)__instance.mimickingPlayer.playerClientId, detachedHead: true);
                __instance.skinnedMeshRenderers = __instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                __instance.meshRenderers = __instance.gameObject.GetComponentsInChildren<MeshRenderer>();
            }
        }
    }
}
