using HarmonyLib;
using System.Collections;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MoreCompany
{
    public class LANMenu : MonoBehaviour
    {
        public static bool TryParseIpAndPort(string input, out IPAddress ipAddress, out int port)
        {
            ipAddress = null;
            port = 0;

            string[] parts = input.Split(':');
            if (parts.Length > 2)
            {
                return false;
            }

            if (!IPAddress.TryParse(parts[0], out ipAddress))
            {
                return false;
            }

            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[1], out port) || port < 0 || port > 65535)
                {
                    return false;
                }
            }

            return true;
        }

        public static void JoinLobbyByIP(string IP_Address, ushort Port = 0)
        {
            if (Port == 0)
                Port = (ushort)MainClass.lanDefaultPort.Value;

            MainClass.actualPlayerCount = 4;
            MainClass.newPlayerCount = MainClass.actualPlayerCount;
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IP_Address;
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = Port;
            MainClass.StaticLogger.LogInfo($"Listening to LAN server: {IP_Address}:{Port}");
            GameObject.Find("MenuManager").GetComponent<MenuManager>().StartAClient();
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), "OnLocalClientConnectionDisapproved")]
    public static class ConnectionDisapprovedPatch
    {
        private static int crewSizeMismatch = 0;
        private static IEnumerator delayedReconnect()
        {
            yield return new WaitForSeconds(0.5f);
            GameObject.Find("MenuManager").GetComponent<MenuManager>().StartAClient();
            yield break;
        }

        private static void Prefix(GameNetworkManager __instance, ulong clientId)
        {
            crewSizeMismatch = 0;
            if (__instance.disableSteam)
            {
                try
                {
                    if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason) && NetworkManager.Singleton.DisconnectReason.StartsWith("Crew size mismatch!"))
                    {
                        crewSizeMismatch = int.Parse(NetworkManager.Singleton.DisconnectReason.Split("Their size: ")[1].Split(". ")[0]);
                    }
                }
                catch { }
            }
        }
        private static void Postfix(GameNetworkManager __instance, ulong clientId)
        {
            if (__instance.disableSteam && crewSizeMismatch != 0)
            {
                if (MainClass.actualPlayerCount != crewSizeMismatch)
                {
                    GameObject.Find("MenuManager").GetComponent<MenuManager>().menuNotification.SetActive(false);

                    // Automatic Reconnect
                    Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: true);
                    MainClass.actualPlayerCount = crewSizeMismatch;
                    MainClass.newPlayerCount = Mathf.Max(4, MainClass.actualPlayerCount);
                    __instance.StartCoroutine(delayedReconnect());
                }

                crewSizeMismatch = 0;
            }
        }
    }


    [HarmonyPatch]
    public static class LANHostPatches
    {
        [HarmonyPatch(typeof(MenuManager), "LAN_HostSetAllowRemoteConnections")]
        [HarmonyPostfix]
        private static void LAN_HostSetAllowRemoteConnections(MenuManager __instance)
        {
            __instance.hostSettings_LobbyPublic = true;
            __instance.privatePublicDescription.text = "REMOTE means your game will be joinable by anyone on your network.";
        }

        [HarmonyPatch(typeof(MenuManager), "LAN_HostSetLocal")]
        [HarmonyPostfix]
        private static void LAN_HostSetLocal(MenuManager __instance)
        {
            __instance.hostSettings_LobbyPublic = false;
            __instance.privatePublicDescription.text = "LOCAL means your game will only be joinable from your local machine.";
        }

        [HarmonyPatch(typeof(MenuManager), "HostSetLobbyPublic")]
        [HarmonyPostfix]
        private static void HostSetLobbyPublic(MenuManager __instance, bool setPublic)
        {
            if (GameNetworkManager.Instance.disableSteam)
            {
                __instance.hostSettings_LobbyPublic = setPublic;
                __instance.lanSetLocalButtonAnimator.SetBool("isPressed", !setPublic);
                __instance.lanSetAllowRemoteButtonAnimator.SetBool("isPressed", setPublic);
                if (setPublic)
                {
                    __instance.LAN_HostSetAllowRemoteConnections();
                }
                else
                {
                    __instance.LAN_HostSetLocal();
                }
            }
        }

        [HarmonyPatch(typeof(MenuManager), "StartAClient")]
        [HarmonyPrefix]
        private static void StartAClient(MenuManager __instance)
        {
            if (GameNetworkManager.Instance.disableSteam)
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = (ushort)MainClass.lanDefaultPort.Value;
            }
        }
    }
}
