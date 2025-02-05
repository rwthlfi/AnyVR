using AnyVr.LobbySystem;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVr.Samples.NewLobbySetup
{
    [RequireComponent(typeof(LobbyUI))]
    public class WelcomeSceneHandler : MonoBehaviour
    {
        private ConnectionManager _connectionManager;

        private LobbyUI _lobbyUI;

        private void Start()
        {
            Debug.Log("Welcome to AnyVr");
            _connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(_connectionManager);

            _connectionManager.ConnectionState += OnConnectionStateUpdate;
            _connectionManager.GlobalSceneLoaded += OnGlobalSceneLoaded;

            _lobbyUI = GetComponent<LobbyUI>();
            _lobbyUI.OnConnectButtonPressed += OnConnectBtnClicked;
            _lobbyUI.OnLobbyOpenButtonPressed += OpenLobby;
            _lobbyUI.OnLobbyJoinButtonPressed += JoinLobby;

            // Autostart server for server builds
            // The ServerManager component on the NetworkManager prefab has the attribute '_startOnHeadless', which does the same thing.
            if (Application.platform != RuntimePlatform.LinuxServer &&
                Application.platform != RuntimePlatform.WindowsServer)
            {
                return;
            }

            Debug.Log("Starting Server...");
            _connectionManager.StartServer();
        }

        private void OnGlobalSceneLoaded(bool obj)
        {
            _lobbyUI.SetAvailableLobbyScenes(LobbyManager.GetInstance()?.LobbyScenes);
        }

        private static void JoinLobby(Guid lobbyId)
        {
            LobbyManager.GetInstance()?.Client_JoinLobby(lobbyId, null);
        }

        private static void OpenLobby(string lobbyName, string password, string scenePath)
        {
            LobbyManager.GetInstance()?.Client_CreateLobby(lobbyName, password, scenePath, 16, null);
        }

        private void OnConnectionStateUpdate(ConnectionState state)
        {
            _lobbyUI.SetLobbyPanelActive(state == ConnectionState.k_client);
        }

        private async void OnConnectBtnClicked(string tokenServerIp, string username)
        {
            ServerAddressResponse result = await ConnectionManager.RequestServerIp(tokenServerIp);
            if (result.success)
            {
                _connectionManager.ConnectToServer(result, username);
            }
        }
    }
}