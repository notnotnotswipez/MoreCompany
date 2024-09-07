using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MoreCompany.LANDiscovery
{
    public class LANLobbySlot : LobbySlot
    {
        public TextMeshProUGUI HostName;

        public new Lobby_LAN thisLobby;

        public new void JoinButton()
        {
            MainClass.actualPlayerCount = 4;
            MainClass.newPlayerCount = MainClass.actualPlayerCount;
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = thisLobby.IPAddress;
            MainClass.StaticLogger.LogInfo($"Listening to LAN server: {thisLobby.IPAddress}");
            GameObject.Find("MenuManager").GetComponent<MenuManager>().StartAClient();
        }
    }
}
