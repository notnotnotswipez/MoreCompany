using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoreCompany.Cosmetics
{
    public class CosmeticApplication : MonoBehaviour
    {
        public Transform head;
        public Transform hip;
        public Transform lowerArmRight;
        public Transform shinLeft;
        public Transform shinRight;
        public List<CosmeticInstance> spawnedCosmetics = new List<CosmeticInstance>();

        public void Awake()
        {
            head = transform.Find("spine").Find("spine.001").Find("spine.002").Find("spine.003").Find("spine.004");
            lowerArmRight = transform.Find("spine").Find("spine.001").Find("spine.002").Find("spine.003").Find("shoulder.R").Find("arm.R_upper").Find("arm.R_lower");
            hip = transform.Find("spine");
            shinLeft = transform.Find("spine").Find("thigh.L").Find("shin.L");
            shinRight = transform.Find("spine").Find("thigh.R").Find("shin.R");
        }

        private void Update()
        {
            foreach (var spawnedCosmetic in spawnedCosmetics)
            {
                if (spawnedCosmetic.cosmeticType == CosmeticType.HAT)
                {
                    spawnedCosmetic.transform.position = head.position;
                    spawnedCosmetic.transform.rotation = head.rotation;
                }
                else if (spawnedCosmetic.cosmeticType == CosmeticType.R_LOWER_ARM)
                {
                    spawnedCosmetic.transform.position = lowerArmRight.position;
                    spawnedCosmetic.transform.rotation = lowerArmRight.rotation;
                }
                else if (spawnedCosmetic.cosmeticType == CosmeticType.HIP)
                {
                    spawnedCosmetic.transform.position = hip.position;
                    spawnedCosmetic.transform.rotation = hip.rotation;
                }
                else if (spawnedCosmetic.cosmeticType == CosmeticType.L_Shin)
                {
                    spawnedCosmetic.transform.position = shinLeft.position;
                    spawnedCosmetic.transform.rotation = shinLeft.rotation;
                }
                else if (spawnedCosmetic.cosmeticType == CosmeticType.R_Shin)
                {
                    spawnedCosmetic.transform.position = shinRight.position;
                    spawnedCosmetic.transform.rotation = shinRight.rotation;
                }
            }
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
                GameObject cosmeticInstanceGameObject = GameObject.Instantiate(cosmeticInstance.gameObject);
                cosmeticInstanceGameObject.SetActive(startEnabled);
                CosmeticInstance cosmeticInstanceBehavior = cosmeticInstanceGameObject.GetComponent<CosmeticInstance>();
                spawnedCosmetics.Add(cosmeticInstanceBehavior);
            }
            else
            {
                MainClass.StaticLogger.LogError("Cosmetic with id: " + cosmeticId + " does not exist!");
            }
        }
    }
}