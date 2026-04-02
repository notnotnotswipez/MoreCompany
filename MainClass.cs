using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using MoreCompany.Cosmetics;
using MoreCompany.Utils;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoreCompany;

[BepInPlugin("me.swipez.melonloader.morecompany", "MoreCompany", "1.12.0")]
public class MainClass : BaseUnityPlugin
{
	public static readonly int defaultPlayerCount = 32;

	public static readonly int minPlayerCount = 4;

	public static readonly int maxPlayerCount = 50;

	public static int newPlayerCount = 32;

	public static ConfigFile StaticConfig;

	public static ConfigEntry<int> playerCount;

	public static ConfigEntry<bool> cosmeticsDeadBodies;

	public static ConfigEntry<bool> cosmeticsMaskedEnemy;

	public static ConfigEntry<bool> cosmeticsSyncOther;

	public static ConfigEntry<bool> defaultCosmetics;

	public static ConfigEntry<bool> cosmeticsPerProfile;

	public static ConfigEntry<string> disabledCosmetics;

	public static Texture2D mainLogo;

	public static GameObject quickMenuScrollParent;

	public static GameObject playerEntry;

	public static GameObject crewCountUI;

	public static GameObject cosmeticGUIInstance;

	public static GameObject cosmeticButton;

	public static ManualLogSource StaticLogger;

	public static Dictionary<int, List<string>> playerIdsAndCosmetics = new Dictionary<int, List<string>>();

	public static string dynamicCosmeticsPath;

	public static string cosmeticSavePath;

	private void Awake()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Expected O, but got Unknown
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Expected O, but got Unknown
		StaticLogger = Logger;
		StaticConfig = Config;
		playerCount = StaticConfig.Bind<int>("General", "Player Count", defaultPlayerCount, new ConfigDescription("How many players can be in your lobby?", (AcceptableValueBase)(object)new AcceptableValueRange<int>(minPlayerCount, maxPlayerCount), Array.Empty<object>()));
		newPlayerCount = Mathf.Max(minPlayerCount, playerCount.Value);
		cosmeticsSyncOther = StaticConfig.Bind<bool>("Cosmetics", "Show Cosmetics", true, "Should you be able to see cosmetics of other players?");
		cosmeticsDeadBodies = StaticConfig.Bind<bool>("Cosmetics", "Show On Dead Bodies", true, "Should you be able to see cosmetics on dead bodies?");
		cosmeticsMaskedEnemy = StaticConfig.Bind<bool>("Cosmetics", "Show On Masked Enemy", true, "Should you be able to see cosmetics on the masked enemy?");
		defaultCosmetics = StaticConfig.Bind<bool>("Cosmetics", "Default Cosmetics", true, "Should the default cosmetics be enabled?");
		cosmeticsPerProfile = StaticConfig.Bind<bool>("Cosmetics", "Per Profile Cosmetics", false, "Should the cosmetics be saved per-profile?");
		disabledCosmetics = StaticConfig.Bind<string>("Cosmetics", "Disabled Cosmetics", "", "Comma separated list of cosmetics to disable");
		cosmeticsSyncOther.SettingChanged += delegate
		{
			PlayerControllerB[] array = Object.FindObjectsByType<PlayerControllerB>((FindObjectsSortMode)0);
			foreach (PlayerControllerB val in array)
			{
				Transform val2 = ((Component)val).transform.Find("ScavengerModel").Find("metarig");
				if (!((Object)(object)val2 == (Object)null))
				{
					CosmeticApplication component = ((Component)val2).gameObject.GetComponent<CosmeticApplication>();
					if (!((Object)(object)component == (Object)null))
					{
						component.UpdateAllCosmeticVisibilities((int)val.playerClientId == StartOfRound.Instance.thisClientPlayerId);
					}
				}
			}
		};
		cosmeticsDeadBodies.SettingChanged += delegate
		{
			PlayerControllerB[] array = Object.FindObjectsByType<PlayerControllerB>((FindObjectsSortMode)0);
			foreach (PlayerControllerB val in array)
			{
				Transform transform = ((Component)val.deadBody).transform;
				if (!((Object)(object)transform == (Object)null))
				{
					CosmeticApplication component = ((Component)transform).GetComponent<CosmeticApplication>();
					if (!((Object)(object)component == (Object)null))
					{
						component.UpdateAllCosmeticVisibilities();
					}
				}
			}
		};
		cosmeticsMaskedEnemy.SettingChanged += delegate
		{
			MaskedPlayerEnemy[] array = Object.FindObjectsByType<MaskedPlayerEnemy>((FindObjectsSortMode)0);
			foreach (MaskedPlayerEnemy val in array)
			{
				Transform val2 = ((Component)val).transform.Find("ScavengerModel").Find("metarig");
				if (!((Object)(object)val2 == (Object)null))
				{
					CosmeticApplication component = ((Component)val2).GetComponent<CosmeticApplication>();
					if (!((Object)(object)component == (Object)null))
					{
						component.UpdateAllCosmeticVisibilities();
						((EnemyAI)val).skinnedMeshRenderers = ((Component)val).gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
						((EnemyAI)val).meshRenderers = ((Component)val).gameObject.GetComponentsInChildren<MeshRenderer>();
					}
				}
			}
		};
		Harmony harmony = new Harmony("me.swipez.melonloader.morecompany");
		try
		{
			harmony.PatchAll();
		}
		catch (Exception ex)
		{
			StaticLogger.LogError((object)("Failed to patch: " + ex));
		}
		StaticLogger.LogInfo((object)"Loading MoreCompany...");
		SteamFriends.OnGameLobbyJoinRequested += delegate(Lobby lobby, SteamId steamId)
		{
			newPlayerCount = lobby.MaxMembers;
		};
		SteamMatchmaking.OnLobbyEntered += delegate(Lobby lobby)
		{
			newPlayerCount = lobby.MaxMembers;
		};
		StaticLogger.LogInfo((object)"Loading SETTINGS...");
		dynamicCosmeticsPath = Paths.PluginPath + "/MoreCompanyCosmetics";
		if (cosmeticsPerProfile.Value)
		{
			cosmeticSavePath = Application.persistentDataPath + "/morecompanycosmetics-" + Directory.GetParent(Paths.BepInExRootPath).Name + ".txt";
		}
		else
		{
			cosmeticSavePath = Application.persistentDataPath + "/morecompanycosmetics.txt";
		}
		cosmeticsPerProfile.SettingChanged += delegate
		{
			if (cosmeticsPerProfile.Value)
			{
				cosmeticSavePath = Application.persistentDataPath + "/MCCosmeticsSave-" + Directory.GetParent(Paths.BepInExRootPath).Name + ".mcs";
			}
			else
			{
				cosmeticSavePath = Application.persistentDataPath + "/MCCosmeticsSave.mcs";
			}
		};
		StaticLogger.LogInfo((object)("Checking: " + dynamicCosmeticsPath));
		if (!Directory.Exists(dynamicCosmeticsPath))
		{
			StaticLogger.LogInfo((object)"Creating cosmetics directory");
			Directory.CreateDirectory(dynamicCosmeticsPath);
		}
		StaticLogger.LogInfo((object)"Loading COSMETICS...");
		ReadCosmeticsFromFile();
		if (defaultCosmetics.Value)
		{
			StaticLogger.LogInfo((object)"Loading DEFAULT COSMETICS...");
			AssetBundle cosmeticsBundle = BundleUtilities.LoadBundleFromInternalAssembly("morecompany.cosmetics", Assembly.GetExecutingAssembly());
			CosmeticRegistry.LoadCosmeticsFromBundle(cosmeticsBundle, "morecompany.cosmetics");
			cosmeticsBundle.Unload(false);
		}
		StaticLogger.LogInfo((object)"Loading USER COSMETICS...");
		RecursiveCosmeticLoad(Paths.PluginPath);
		AssetBundle bundle = BundleUtilities.LoadBundleFromInternalAssembly("morecompany.assets", Assembly.GetExecutingAssembly());
		LoadAssets(bundle);
		StaticLogger.LogInfo((object)"Loaded MoreCompany FULLY");
	}

	private void RecursiveCosmeticLoad(string directory)
	{
		string[] directories = Directory.GetDirectories(directory);
		foreach (string subDirectory in directories)
		{
			RecursiveCosmeticLoad(subDirectory);
		}
		string[] files = Directory.GetFiles(directory);
		foreach (string file in files)
		{
			if (file.EndsWith(".cosmetics"))
			{
				AssetBundle bundle = AssetBundle.LoadFromFile(file);
				CosmeticRegistry.LoadCosmeticsFromBundle(bundle, file);
				bundle.Unload(false);
			}
		}
	}

	private void ReadCosmeticsFromFile()
	{
		if (File.Exists(cosmeticSavePath))
		{
			string[] lines = File.ReadAllLines(cosmeticSavePath);
			string[] array = lines;
			foreach (string line in array)
			{
				CosmeticRegistry.locallySelectedCosmetics.Add(line);
			}
		}
	}

	public static void WriteCosmeticsToFile()
	{
		string built = "";
		foreach (string cosmetic in CosmeticRegistry.locallySelectedCosmetics)
		{
			built = built + cosmetic + "\n";
		}
		File.WriteAllText(cosmeticSavePath, built);
	}

	private static void LoadAssets(AssetBundle bundle)
	{
		if ((Object)(object)bundle != (Object)null)
		{
			mainLogo = bundle.LoadPersistentAsset<Texture2D>("assets/morecompanyassets/morecompanytransparentred.png");
			quickMenuScrollParent = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/quickmenuoverride.prefab");
			playerEntry = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/playerlistslot.prefab");
			cosmeticGUIInstance = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/testoverlay.prefab");
			cosmeticButton = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/cosmeticinstance.prefab");
			crewCountUI = bundle.LoadPersistentAsset<GameObject>("assets/morecompanyassets/crewcountfield.prefab");
			bundle.Unload(false);
		}
	}

	public static void ResizePlayerCache(Dictionary<uint, Dictionary<int, NetworkObject>> ScenePlacedObjects)
	{
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Expected O, but got Unknown
		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Expected O, but got Unknown
		StartOfRound round = StartOfRound.Instance;
		if (round.allPlayerObjects.Length != newPlayerCount)
		{
			int originalLength = round.allPlayerObjects.Length;
			int difference = newPlayerCount - originalLength;
			StaticLogger.LogInfo((object)$"Resizing arrays from {originalLength} to {newPlayerCount} with difference of {difference}");
			Array.Resize(ref round.allPlayerObjects, newPlayerCount);
			Array.Resize(ref round.allPlayerScripts, newPlayerCount);
			Array.Resize(ref round.gameStats.allPlayerStats, newPlayerCount);
			Array.Resize(ref round.playerSpawnPositions, newPlayerCount);
			if (difference > 0)
			{
				GameObject firstPlayerObject = round.allPlayerObjects[originalLength - 1];
				uint globalObjectIdHash = 10001u;
				for (int i = 0; i < difference; i++)
				{
					GameObject copy = Object.Instantiate<GameObject>(firstPlayerObject, firstPlayerObject.transform.parent);
					((Object)copy).name = $"Player ({originalLength + i})";
					NetworkObject[] componentsInChildren = copy.GetComponentsInChildren<NetworkObject>();
					foreach (NetworkObject copyNetworkObject in componentsInChildren)
					{
						ReflectionUtils.SetFieldValue(copyNetworkObject, "GlobalObjectIdHash", globalObjectIdHash);
						Scene scene = ((Component)copyNetworkObject).gameObject.scene;
						int handle = scene.handle;
						if (!ScenePlacedObjects.ContainsKey(globalObjectIdHash))
						{
							ScenePlacedObjects.Add(globalObjectIdHash, new Dictionary<int, NetworkObject>());
						}
						if (ScenePlacedObjects[globalObjectIdHash].ContainsKey(handle))
						{
							string text = (((Object)(object)ScenePlacedObjects[globalObjectIdHash][handle] != (Object)null) ? ((Object)ScenePlacedObjects[globalObjectIdHash][handle]).name : "Null Entry");
							throw new Exception($"{((Object)copyNetworkObject).name} tried to registered with ScenePlacedObjects which already contains the same GlobalObjectIdHash value {globalObjectIdHash} for {text}!");
						}
						ScenePlacedObjects[globalObjectIdHash].Add(handle, copyNetworkObject);
						globalObjectIdHash++;
					}
					PlayerControllerB newPlayerScript = copy.GetComponentInChildren<PlayerControllerB>();
					newPlayerScript.playerClientId = (ulong)(originalLength + i);
					newPlayerScript.playerUsername = $"Player #{newPlayerScript.playerClientId}";
					newPlayerScript.isPlayerControlled = false;
					newPlayerScript.isPlayerDead = false;
					newPlayerScript.DropAllHeldItems(itemsFall: false, disconnecting: false);
					newPlayerScript.TeleportPlayer(round.notSpawnedPosition.position, false, 0f, false, true);
					round.allPlayerObjects[originalLength + i] = copy;
					round.gameStats.allPlayerStats[originalLength + i] = new PlayerStats();
					round.allPlayerScripts[originalLength + i] = newPlayerScript;
					round.playerSpawnPositions[originalLength + i] = round.playerSpawnPositions[3];
					StartOfRound.Instance.mapScreen.radarTargets.Add(new TransformAndName(((Component)newPlayerScript).transform, newPlayerScript.playerUsername, false));
				}
			}
		}
		PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;
		foreach (PlayerControllerB newPlayerScript2 in allPlayerScripts)
		{
			((TMP_Text)newPlayerScript2.usernameBillboardText).text = newPlayerScript2.playerUsername;
		}
	}
}
