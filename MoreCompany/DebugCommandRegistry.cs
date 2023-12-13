using System;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Object = System.Object;

namespace MoreCompany
{
    public class DebugCommandRegistry
    {
        public static bool commandEnabled = false;
        
        public static void HandleCommand(String[] args)
        {
            if (!commandEnabled)
            {
                return;
            }
            PlayerControllerB localPlayer = StartOfRound.Instance.localPlayerController;
            
            switch (args[0])
            {
                case "money":
                    int money = int.Parse(args[1]);
                    Terminal terminal = Resources.FindObjectsOfTypeAll<Terminal>().First();
                    terminal.groupCredits = money;
                    break;
                case "spawnscrap":
                    string scrapName = "";
                    for (int i = 1; i < args.Length; i++)
                    {
                        scrapName += args[i] + " ";
                    }

                    scrapName = scrapName.Trim();
                    
                    Vector3 vector = localPlayer.transform.position + localPlayer.transform.forward * 2f;
                    SpawnableItemWithRarity selectedItem = null;
                    foreach (var rarityItem in StartOfRound.Instance.currentLevel.spawnableScrap)
                    {
                        if (rarityItem.spawnableItem.itemName.ToLower() == scrapName.ToLower())
                        {
                            selectedItem = rarityItem;
                            break;
                        }
                    }

                    GameObject gameObject = GameObject.Instantiate<GameObject>(selectedItem.spawnableItem.spawnPrefab, vector, Quaternion.identity, null);
                    GrabbableObject component = gameObject.GetComponent<GrabbableObject>();
                    component.transform.rotation = Quaternion.Euler(component.itemProperties.restingRotation);
                    component.fallTime = 0f;
                    NetworkObject component2 = gameObject.GetComponent<NetworkObject>();
                    component2.Spawn();
                    break;
                case "spawnenemy":
                    string enemyName = "";
                    for (int i = 1; i < args.Length; i++)
                    {
                        enemyName += args[i] + " ";
                    }
                    
                    enemyName = enemyName.Trim();
                    
                    SpawnableEnemyWithRarity selectedEnemy = null;
                    
                    foreach (var enemyWithRarity in StartOfRound.Instance.currentLevel.Enemies)
                    {
                        if (enemyWithRarity.enemyType.enemyName.ToLower() == enemyName.ToLower())
                        {
                            selectedEnemy = enemyWithRarity;
                            break;
                        }
                    }

                    RoundManager.Instance.SpawnEnemyGameObject(
                        localPlayer.transform.position + localPlayer.transform.forward * 2f, 0, -1,
                        selectedEnemy.enemyType);
                    break;
                case "listall":
                    MainClass.StaticLogger.LogInfo("Spawnable scrap:");
                    foreach (SpawnableItemWithRarity spawnableItemWithRarity in StartOfRound.Instance.currentLevel.spawnableScrap)
                    {
                        MainClass.StaticLogger.LogInfo(spawnableItemWithRarity.spawnableItem.itemName);
                    }
                
                    MainClass.StaticLogger.LogInfo("Spawnable enemies:");
                    foreach (SpawnableEnemyWithRarity spawnableItemWithRarity in StartOfRound.Instance.currentLevel.Enemies)
                    {
                        MainClass.StaticLogger.LogInfo(spawnableItemWithRarity.enemyType.enemyName);
                    }
                    break;
            }
        }
    }
}