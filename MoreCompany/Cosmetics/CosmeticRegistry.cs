using System.Collections.Generic;
using System.Reflection;
using MoreCompany.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace MoreCompany.Cosmetics
{
    public class CosmeticRegistry
    {
        public static Dictionary<string, CosmeticInstance> cosmeticInstances = new Dictionary<string, CosmeticInstance>();
        
        public static GameObject cosmeticGUI;
        private static GameObject displayGuy;
        private static CosmeticApplication cosmeticApplication;
        public static List<string> locallySelectedCosmetics = new List<string>();

        public static void LoadCosmeticsFromAssembly(Assembly assembly, AssetBundle bundle)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsSubclassOf(typeof(CosmeticGeneric)))
                {
                    CosmeticGeneric cosmeticGeneric = (CosmeticGeneric) type.GetConstructor(new System.Type[] { }).Invoke(new object[] { });
                    cosmeticGeneric.LoadFromBundle(bundle);
                }
            }
        }

        public static void SpawnCosmeticGUI()
        {
            cosmeticGUI = GameObject.Instantiate(MainClass.cosmeticGUIInstance);
            cosmeticGUI.transform.Find("Canvas").Find("GlobalScale").transform.localScale = new Vector3(2, 2, 2);
            
            displayGuy = cosmeticGUI.transform.Find("Canvas").Find("GlobalScale").Find("CosmeticsScreen").Find("ObjectHolder")
                .Find("ScavengerModel").Find("metarig").gameObject;

            cosmeticApplication = displayGuy.AddComponent<CosmeticApplication>();
            
            GameObject enableCosmeticsButton = cosmeticGUI.transform.Find("Canvas").Find("GlobalScale").Find("CosmeticsScreen").Find("EnableButton").gameObject;
            enableCosmeticsButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                MainClass.showCosmetics = true;
                MainClass.SaveSettingsToFile();
            });
            
            GameObject disableCosmeticsButton = cosmeticGUI.transform.Find("Canvas").Find("GlobalScale").Find("CosmeticsScreen").Find("DisableButton").gameObject;
            disableCosmeticsButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                MainClass.showCosmetics = false;
                MainClass.SaveSettingsToFile();
            });
            
            if (MainClass.showCosmetics)
            {
                enableCosmeticsButton.SetActive(false);
                disableCosmeticsButton.SetActive(true);
            }
            else
            {
                enableCosmeticsButton.SetActive(true);
                disableCosmeticsButton.SetActive(false);
            }
            
            PopulateCosmetics();
            UpdateCosmeticsOnDisplayGuy(false);
        }

        public static void PopulateCosmetics()
        {
            GameObject contentHolder = cosmeticGUI.transform.Find("Canvas").Find("GlobalScale").Find("CosmeticsScreen").Find("CosmeticsHolder")
                .Find("Content").gameObject;
            
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < contentHolder.transform.childCount; i++)
            {
                children.Add(contentHolder.transform.GetChild(i));
            }
            
            foreach (var child in children)
            {
                GameObject.Destroy(child.gameObject);
            }

            foreach (var cosmeticInstance in cosmeticInstances)
            {
                GameObject spawnedCosmetic = GameObject.Instantiate(MainClass.cosmeticButton, contentHolder.transform);
                spawnedCosmetic.transform.localScale = Vector3.one;
                
                GameObject disabledOverlay = spawnedCosmetic.transform.Find("Deselected").gameObject;
                disabledOverlay.SetActive(true);
                
                GameObject enabledOverlay = spawnedCosmetic.transform.Find("Selected").gameObject;
                enabledOverlay.SetActive(true);

                if (IsEquipped(cosmeticInstance.Value.cosmeticId))
                {
                    enabledOverlay.SetActive(true);
                    disabledOverlay.SetActive(false);
                }
                else
                {
                    enabledOverlay.SetActive(false);
                    disabledOverlay.SetActive(true);
                }

                RawImage iconImage = spawnedCosmetic.transform.Find("Icon").GetComponent<RawImage>();
                iconImage.texture = cosmeticInstance.Value.icon;
                
                Button button = spawnedCosmetic.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    ToggleCosmetic(cosmeticInstance.Value.cosmeticId);
                    if (IsEquipped(cosmeticInstance.Value.cosmeticId))
                    {
                        enabledOverlay.SetActive(true);
                        disabledOverlay.SetActive(false);
                    }
                    else
                    {
                        enabledOverlay.SetActive(false);
                        disabledOverlay.SetActive(true);
                    }

                    MainClass.WriteCosmeticsToFile();
                    UpdateCosmeticsOnDisplayGuy(true);
                });
            }
        }
        
        private static Color HexToColor(string hex)
        {
            Color color = new Color();
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }

        public static void UpdateCosmeticsOnDisplayGuy(bool startEnabled)
        {
            cosmeticApplication.ClearCosmetics();
            foreach (var selected in locallySelectedCosmetics)
            {
                cosmeticApplication.ApplyCosmetic(selected, startEnabled);
            }

            foreach (var cosmeticSpawned in cosmeticApplication.spawnedCosmetics)
            {
                RecursiveLayerChange(cosmeticSpawned.transform, 5);
            }
        }
        
        private static void RecursiveLayerChange(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            for (int i = 0; i < transform.childCount; i++)
            {
                RecursiveLayerChange(transform.GetChild(i), layer);
            }
        }

        public static bool IsEquipped(string cosmeticId)
        {
            return locallySelectedCosmetics.Contains(cosmeticId);
        }
        
        public static void ToggleCosmetic(string cosmeticId)
        {
            if (locallySelectedCosmetics.Contains(cosmeticId))
            {
                locallySelectedCosmetics.Remove(cosmeticId);
            }
            else
            {
                locallySelectedCosmetics.Add(cosmeticId);
            }
        }
    }
}