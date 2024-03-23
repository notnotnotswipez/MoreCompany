using HarmonyLib;
using System.Collections;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace MoreCompany
{
    public class LANMenu : MonoBehaviour
    {
        public static void InitializeMenu()
        {
            CreateUI();

            var startLAN_button = GameObject.Find("Canvas/MenuContainer/MainButtons/StartLAN");
            if (startLAN_button != null)
            {
                MainClass.StaticLogger.LogInfo("LANMenu startLAN Patched");
                startLAN_button.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                startLAN_button.GetComponent<Button>().onClick.AddListener(() =>
                {
                    TextMeshProUGUI footerText = GameObject.Find("Canvas/MenuContainer/LobbyJoinSettings/JoinSettingsContainer/PrivatePublicDescription").GetComponent<TextMeshProUGUI>();
                    if (footerText != null)
                        footerText.text = "The mod will attempt to auto-detect the crew size however you can manually specify it to reduce chance of failure.";

                    GameObject.Find("Canvas/MenuContainer/LobbyJoinSettings").gameObject.SetActive(true);
                });
            }
        }
        private static GameObject CreateUI()
        {
            if (GameObject.Find("Canvas/MenuContainer/LobbyJoinSettings") != null) return null;

            var menuContainer = GameObject.Find("Canvas/MenuContainer");
            if (menuContainer == null) return null;
            var LobbyHostSettings = GameObject.Find("Canvas/MenuContainer/LobbyHostSettings");
            if (LobbyHostSettings == null) return null;

            // Clone LobbyHostSettings
            GameObject menu = Instantiate(LobbyHostSettings, LobbyHostSettings.transform.position, LobbyHostSettings.transform.rotation, menuContainer.transform);
            menu.name = "LobbyJoinSettings";

            var lanSubMenu = menu.transform.Find("HostSettingsContainer");
            if (lanSubMenu != null)
            {
                lanSubMenu.name = "JoinSettingsContainer";
                lanSubMenu.transform.Find("LobbyHostOptions").name = "LobbyJoinOptions";

                Destroy(menu.transform.Find("ChallengeLeaderboard").gameObject);
                Destroy(menu.transform.Find("FilesPanel").gameObject);
                Destroy(lanSubMenu.transform.Find("LobbyJoinOptions/OptionsNormal").gameObject);
                Destroy(lanSubMenu.transform.Find("LobbyJoinOptions/LANOptions/AllowRemote").gameObject);
                Destroy(lanSubMenu.transform.Find("LobbyJoinOptions/LANOptions/Local").gameObject);

                var headerText = lanSubMenu.transform.Find("LobbyJoinOptions/LANOptions/Header");
                if (headerText != null)
                    headerText.GetComponent<TextMeshProUGUI>().text = "Join LAN Server:";

                var addressField = lanSubMenu.transform.Find("LobbyJoinOptions/LANOptions/ServerNameField");
                if (addressField != null)
                {
                    addressField.transform.localPosition = new Vector3(0f, 15f, -6.5f);
                    addressField.gameObject.SetActive(true);
                }

                TMP_InputField ip_field = addressField.GetComponent<TMP_InputField>();
                if (ip_field != null)
                {
                    TextMeshProUGUI ip_placeholder = ip_field.placeholder.GetComponent<TextMeshProUGUI>();
                    ip_placeholder.text = ES3.Load("LANIPAddress", "LCGeneralSaveData", "127.0.0.1");

                    Button confirmBut = lanSubMenu.transform.Find("Confirm")?.GetComponent<Button>();
                    if (confirmBut != null)
                    {
                        confirmBut.onClick = new Button.ButtonClickedEvent();
                        confirmBut.onClick.AddListener(() =>
                        {
                            string IP_Address = "127.0.0.1";
                            if (ip_field.text != "")
                                IP_Address = ip_field.text;
                            else
                                IP_Address = ip_placeholder.text;
                            ES3.Save("LANIPAddress", IP_Address, "LCGeneralSaveData");
                            GameObject.Find("Canvas/MenuContainer/LobbyJoinSettings").gameObject.SetActive(false);
                            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IP_Address;
                            MainClass.StaticLogger.LogInfo($"Listening to LAN server: {IP_Address}");
                            GameObject.Find("MenuManager").GetComponent<MenuManager>().StartAClient();
                        });
                    }
                }

                TextMeshProUGUI footerText = lanSubMenu.transform.Find("PrivatePublicDescription").GetComponent<TextMeshProUGUI>();
                if (footerText != null)
                    footerText.text = "The mod will attempt to auto-detect the crew size however you can manually specify it to reduce chance of failure.";

                lanSubMenu.transform.Find("LobbyJoinOptions/LANOptions").gameObject.SetActive(true);
            }

            return menu;
        }
    }

    // Crew Size Mismatch
    [HarmonyPatch(typeof(GameNetworkManager), "SetConnectionDataBeforeConnecting")]
    public static class ConnectionDataPatch
    {
        public static void Postfix(ref GameNetworkManager __instance)
        {
            if (__instance.disableSteam)
            {
                NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(__instance.gameVersionNum.ToString() + "," + MainClass.newPlayerCount);
            }
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

        private static void Prefix(ref GameNetworkManager __instance, ulong clientId)
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
        private static void Postfix(ref GameNetworkManager __instance, ulong clientId)
        {
            if (__instance.disableSteam && crewSizeMismatch != 0)
            {
                MainClass.newPlayerCount = Mathf.Clamp(crewSizeMismatch, MainClass.minPlayerCount, MainClass.maxPlayerCount);

                if (MainClass.newPlayerCount == crewSizeMismatch)
                {
                    GameObject.Find("MenuManager").GetComponent<MenuManager>().menuNotification.SetActive(false);

                    // Automatic Reconnect
                    Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: true);
                    __instance.StartCoroutine(delayedReconnect());

                    //// Manual Reconnect
                    //foreach (TMP_InputField field in MenuManagerLogoOverridePatch.inputFields)
                    //    field.text = MainClass.newPlayerCount.ToString();
                    //TextMeshProUGUI footerText = GameObject.Find("Canvas/MenuContainer/LobbyJoinSettings/JoinSettingsContainer/PrivatePublicDescription").GetComponent<TextMeshProUGUI>();
                    //if (footerText != null)
                    //    footerText.text = $"The crew size was mismatched and has automatically been changed to {crewSizeMismatch}. Please re-attempt the connection.";
                    //GameObject.Find("Canvas/MenuContainer/LobbyJoinSettings").gameObject.SetActive(true);
                }

                crewSizeMismatch = 0;
            }
        }
    }
}
