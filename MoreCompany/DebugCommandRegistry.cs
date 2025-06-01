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

                    Item selectedItem = null;

                    foreach (var itemType in Resources.FindObjectsOfTypeAll<Item>())
                    {
                        if (itemType.itemName.ToLower() == scrapName.ToLower())
                        {
                            selectedItem = itemType;
                            break;
                        }
                    }

                    GameObject gameObject = GameObject.Instantiate<GameObject>(selectedItem.spawnPrefab, vector, Quaternion.identity, null);
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

                    EnemyType selectedType = null;

                    foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>())
                    {
                        if (enemyType.enemyName.ToLower() == enemyName.ToLower())
                        {
                            selectedType = enemyType;
                            break;
                        }
                    }

                    RoundManager.Instance.SpawnEnemyGameObject(
                        localPlayer.transform.position + localPlayer.transform.forward * 2f, 0, -1,
                        selectedType);
                    break;
                case "listall":
                    MainClass.StaticLogger.LogInfo("-----------------");
                    MainClass.StaticLogger.LogInfo("Spawnable scrap:");
                    foreach (var item in Resources.FindObjectsOfTypeAll<Item>())
                    {
                        MainClass.StaticLogger.LogInfo(item.itemName);
                    }

                    MainClass.StaticLogger.LogInfo("-----------------");
                    MainClass.StaticLogger.LogInfo("Spawnable enemies:");
                    foreach (var enemyType in Resources.FindObjectsOfTypeAll<EnemyType>())
                    {
                        MainClass.StaticLogger.LogInfo(enemyType.enemyName);
                    }
                    break;
            }
        }
    }
}
