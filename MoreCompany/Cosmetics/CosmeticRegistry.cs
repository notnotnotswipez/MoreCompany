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

        public static Transform cosmeticGUIGlobalScale;
        private static GameObject displayGuy;
        private static CosmeticApplication cosmeticApplication;
        public static List<string> locallySelectedCosmetics = new List<string>();

        public const float COSMETIC_PLAYER_SCALE_MULT = 0.38f;

        public static void LoadCosmeticsFromBundle(AssetBundle bundle)
        {
            foreach (var potentialPrefab in bundle.GetAllAssetNames())
            {
                if (!potentialPrefab.EndsWith(".prefab"))
                {
                    continue;
                }
                GameObject cosmeticInstance = bundle.LoadPersistentAsset<GameObject>(potentialPrefab);
                CosmeticInstance cosmeticInstanceBehavior = cosmeticInstance.GetComponent<CosmeticInstance>();
                if (cosmeticInstanceBehavior == null)
                {
                    continue;
                }
                MainClass.StaticLogger.LogInfo("Loaded cosmetic: " + cosmeticInstanceBehavior.cosmeticId + " from bundle");
                if (cosmeticInstances.ContainsKey(cosmeticInstanceBehavior.cosmeticId))
                {
                    MainClass.StaticLogger.LogError("Duplicate cosmetic id: " + cosmeticInstanceBehavior.cosmeticId);
                    continue;
                }

                cosmeticInstances.Add(cosmeticInstanceBehavior.cosmeticId, cosmeticInstanceBehavior);
            }
        }

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

        public static void UpdateVisibilityCheckbox(GameObject enableCosmeticsButton, GameObject disableCosmeticsButton)
        {
            if (MainClass.cosmeticsSyncOther.Value)
            {
                enableCosmeticsButton.SetActive(false);
                disableCosmeticsButton.SetActive(true);
            }
            else
            {
                enableCosmeticsButton.SetActive(true);
                disableCosmeticsButton.SetActive(false);
            }
        }

        public static void SpawnCosmeticGUI(bool mainMenu)
        {
            if (cosmeticInstances.Count == 0) return; // Don't spawn the ui if no cosmetics are loaded

            GameObject cosmeticGUI = GameObject.Instantiate(MainClass.cosmeticGUIInstance);

            cosmeticGUIGlobalScale = cosmeticGUI.transform.Find("Canvas").Find("GlobalScale");

            if (mainMenu)
            {
                cosmeticGUIGlobalScale.transform.localScale = new Vector3(2, 2, 2);
            }
            else
            {
                cosmeticGUIGlobalScale.transform.parent = GameObject.Find("Systems/UI/Canvas/").transform;
                cosmeticGUIGlobalScale.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                GameObject.Destroy(cosmeticGUI);
            }

            displayGuy = cosmeticGUIGlobalScale.Find("CosmeticsScreen").Find("ObjectHolder")
                .Find("ScavengerModel").Find("metarig").gameObject;

            cosmeticApplication = displayGuy.AddComponent<CosmeticApplication>();

            GameObject enableCosmeticsButton = cosmeticGUIGlobalScale.Find("CosmeticsScreen").Find("EnableButton").gameObject;
            GameObject disableCosmeticsButton = cosmeticGUIGlobalScale.Find("CosmeticsScreen").Find("DisableButton").gameObject;
            enableCosmeticsButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                MainClass.cosmeticsSyncOther.Value = true;
                MainClass.StaticConfig.Save();
            });
            disableCosmeticsButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                MainClass.cosmeticsSyncOther.Value = false;
                MainClass.StaticConfig.Save();
            });

            UpdateVisibilityCheckbox(enableCosmeticsButton, disableCosmeticsButton);

            PopulateCosmetics();
            UpdateCosmeticsOnDisplayGuy(false);
        }

        public static void PopulateCosmetics()
        {
            GameObject contentHolder = cosmeticGUIGlobalScale.Find("CosmeticsScreen").Find("CosmeticsHolder")
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
            ColorUtility.TryParseHtmlString(hex, out Color color);
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
                cosmeticSpawned.transform.localScale *= COSMETIC_PLAYER_SCALE_MULT;
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

            if (StartOfRound.Instance != null && StartOfRound.Instance.localPlayerController != null)
            {
                CosmeticSyncPatch.SyncCosmeticsToOtherClients(StartOfRound.Instance.localPlayerController);
            }
        }
    }
}
