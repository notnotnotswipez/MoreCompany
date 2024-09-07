using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MoreCompany.LANDiscovery
{
    public class LANLobby
    {
        public string IPAddress;
        public int MemberCount;
        public int MaxMembers;
        public Dictionary<string, string> Data;
        public string GetData(string key)
        {
            return Data.ContainsKey(key) ? Data[key] : null;
        }
        public void SetData(string key, string value)
        {
            if (Data.ContainsKey(key))
            {
                Data[key] = value;
            }
            else
            {
                Data.Add(key, value);
            }
        }
    }

    public class ClientDiscovery : MonoBehaviour
    {
        private UdpClient udpClient;
        public int listenPort = 47777;
        internal bool isListening = false;

        private List<LANLobby> discoveredLobbies = new List<LANLobby>();

        public async Task<List<LANLobby>> DiscoverLobbiesAsync(float discoveryTime)
        {
            discoveredLobbies.Clear();

            udpClient = new UdpClient(listenPort);
            isListening = true;

            Task listenTask = Task.Run(() => StartListening());

            await Task.Delay(TimeSpan.FromSeconds(discoveryTime));

            isListening = false;
            udpClient.Close();

            return discoveredLobbies;
        }

        private void StartListening()
        {
            try
            {
                while (isListening)
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, listenPort);
                    byte[] data = udpClient.Receive(ref ipEndPoint); // Synchronous receive (non-async here)

                    string message = Encoding.UTF8.GetString(data);
                    ParseAndStoreLobby(ipEndPoint.Address.ToString(), message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error receiving UDP broadcast: " + ex.Message);
            }
        }

        private void ParseAndStoreLobby(string ipAddress, string message)
        {
            try
            {
                string[] parts = message.Split(';');
                if (parts.Length == 4 && parts[0] == "LC_MC_LAN")
                {
                    string lobbyName = parts[1];
                    int currentPlayers = int.Parse(parts[2]);
                    int maxPlayers = int.Parse(parts[3]);

                    LANLobby existingLobby = discoveredLobbies.Find(lobby =>
                        lobby.IPAddress == ipAddress);

                    if (existingLobby != null)
                    {
                        // Overwrite the existing lobby's data
                        existingLobby.MemberCount = currentPlayers;
                        existingLobby.MaxMembers = maxPlayers;
                        MainClass.StaticLogger.LogDebug($"Updated Lobby: {existingLobby.GetData("name")} at {existingLobby.IPAddress} with {existingLobby.MemberCount}/{existingLobby.MaxMembers} players.");
                    }
                    else
                    {
                        LANLobby lobby = new LANLobby
                        {
                            IPAddress = ipAddress,
                            Data = new Dictionary<string, string>() {
                                { "name", lobbyName }
                            },
                            MemberCount = currentPlayers,
                            MaxMembers = maxPlayers
                        };

                        discoveredLobbies.Add(lobby);
                        MainClass.StaticLogger.LogInfo($"Discovered Lobby: {lobby.GetData("name")} at {lobby.IPAddress} with {lobby.MemberCount}/{lobby.MaxMembers} players.");
                    }
                }
                else
                {
                    MainClass.StaticLogger.LogWarning("Invalid broadcast format received.");
                }
            }
            catch (Exception ex)
            {
                MainClass.StaticLogger.LogError(ex);
            }
        }
    }
}
