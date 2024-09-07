using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace MoreCompany.LANDiscovery
{
    public class LANLobbySlot : LobbySlot
    {
        public TextMeshProUGUI HostName;

        public new LANLobby thisLobby;

        public new void JoinButton()
        {
            LANMenu.JoinLobbyByIP(thisLobby.IPAddress, thisLobby.Port);
        }
    }
}
