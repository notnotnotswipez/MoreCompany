using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreCompany.Cosmetics
{
    public class CosmeticApplication : MonoBehaviour
    {
        public bool detachedHead = false;

        public Transform head;
        public Transform hip;
        public Transform lowerArmRight;
        public Transform shinLeft;
        public Transform shinRight;
        public Transform chest;
        public Transform upperArmLeft;
        public Transform upperArmRight;
        public Transform lowerArmLeft;
        public Transform thighLeft;
        public Transform thighRight;
        public Transform footLeft;
        public Transform footRight;

        public List<CosmeticInstance> spawnedCosmetics = new List<CosmeticInstance>();

        public void Awake()
        {
            Transform spine = transform.Find("spine") ?? transform;
            head = spine.Find("spine.001").Find("spine.002").Find("spine.003").Find("spine.004");
            chest = spine.Find("spine.001").Find("spine.002").Find("spine.003");
            lowerArmRight = spine.Find("spine.001").Find("spine.002").Find("spine.003").Find("shoulder.R").Find("arm.R_upper").Find("arm.R_lower");
            hip = spine;
            shinLeft = spine.Find("thigh.L").Find("shin.L");
            shinRight = spine.Find("thigh.R").Find("shin.R");
            upperArmLeft = spine.Find("spine.001").Find("spine.002").Find("spine.003").Find("shoulder.L").Find("arm.L_upper");
            upperArmRight = spine.Find("spine.001").Find("spine.002").Find("spine.003").Find("shoulder.R").Find("arm.R_upper");
            lowerArmLeft = spine.Find("spine.001").Find("spine.002").Find("spine.003").Find("shoulder.L").Find("arm.L_upper").Find("arm.L_lower");
            thighLeft = spine.Find("thigh.L");
            thighRight = spine.Find("thigh.R");
            footLeft = spine.Find("thigh.L").Find("shin.L").Find("foot.L");
            footRight = spine.Find("thigh.R").Find("shin.R").Find("foot.R");

            RefreshAllCosmeticPositions();
        }

        private void OnDisable()
        {
            foreach (var spawnedCosmetic in spawnedCosmetics)
            {
                spawnedCosmetic.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            foreach (var spawnedCosmetic in spawnedCosmetics)
            {
                if (spawnedCosmetic.cosmeticType == CosmeticType.HAT && detachedHead) continue;
                spawnedCosmetic.gameObject.SetActive(true);
            }
        }

        public void ClearCosmetics()
        {
            foreach (var spawnedCosmetic in spawnedCosmetics)
            {
                GameObject.Destroy(spawnedCosmetic.gameObject);
            }
            spawnedCosmetics.Clear();
        }
        
        public void ApplyCosmetic(string cosmeticId, bool startEnabled)
        {
            if (CosmeticRegistry.cosmeticInstances.ContainsKey(cosmeticId))
            {
                CosmeticInstance cosmeticInstance = CosmeticRegistry.cosmeticInstances[cosmeticId];

                if (startEnabled && cosmeticInstance.cosmeticType == CosmeticType.HAT && detachedHead) return;

                GameObject cosmeticInstanceGameObject = GameObject.Instantiate(cosmeticInstance.gameObject);
                cosmeticInstanceGameObject.SetActive(startEnabled);
                CosmeticInstance cosmeticInstanceBehavior = cosmeticInstanceGameObject.GetComponent<CosmeticInstance>();
                spawnedCosmetics.Add(cosmeticInstanceBehavior);
                ParentCosmetic(cosmeticInstanceBehavior);
            }
        }
        
        public void RefreshAllCosmeticPositions()
        {
            foreach (var spawnedCosmetic in spawnedCosmetics)
            {
                ParentCosmetic(spawnedCosmetic);
            }
        }

        private void ParentCosmetic(CosmeticInstance cosmeticInstance)
        {
            Transform targetTransform = null;
            switch (cosmeticInstance.cosmeticType)
            {
                case CosmeticType.HAT:
                    targetTransform = head;
                    break;
                case CosmeticType.R_LOWER_ARM:
                    targetTransform = lowerArmRight;
                    break;
                case CosmeticType.HIP:
                    targetTransform = hip;
                    break;
                case CosmeticType.L_SHIN:
                    targetTransform = shinLeft;
                    break;
                case CosmeticType.R_SHIN:
                    targetTransform = shinRight;
                    break;
                case CosmeticType.CHEST:
                    targetTransform = chest;
                    break;
                case CosmeticType.L_UPPER_ARM:
                    targetTransform = upperArmLeft;
                    break;
                case CosmeticType.R_UPPER_ARM:
                    targetTransform = upperArmRight;
                    break;
                case CosmeticType.L_LOWER_ARM:
                    targetTransform = lowerArmLeft;
                    break;
                case CosmeticType.L_THIGH:
                    targetTransform = thighLeft;
                    break;
                case CosmeticType.R_THIGH:
                    targetTransform = thighRight;
                    break;
                case CosmeticType.L_FOOT:
                    targetTransform = footLeft;
                    break;
                case CosmeticType.R_FOOT:
                    targetTransform = footRight;
                    break;
            }

            if (targetTransform == null)
            {
                MainClass.StaticLogger.LogError("Failed to find transform of type: " + cosmeticInstance.cosmeticType);
                return;
            }

            cosmeticInstance.transform.position = targetTransform.position;
            cosmeticInstance.transform.rotation = targetTransform.rotation;
            cosmeticInstance.transform.parent = targetTransform;
        }
    }
}
