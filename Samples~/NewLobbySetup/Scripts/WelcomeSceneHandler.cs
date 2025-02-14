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

        private Action<Guid> _refreshLobbyUI;

        private void Start()
        {
            Debug.Log("Welcome to AnyVr");
            _connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(_connectionManager);

            _connectionManager.ConnectionState += OnConnectionStateUpdate;

            // The global scene automatically loads locally when the client connects to a server.
            // The global scene contains the LobbyManager which is initialized at this point on.
            _connectionManager.GlobalSceneLoaded += InitializeLobbyPanel;

            _lobbyUI = GetComponent<LobbyUI>();
            _lobbyUI.OnConnectButtonPressed += OnConnectBtnClicked;
            _lobbyUI.OnLobbyOpenButtonPressed += OpenLobby;
            _lobbyUI.OnLobbyJoinButtonPressed += JoinLobby;

            // Activate lobby panel if the client is already connected ta a server.
            // E.g. when disconnecting from a lobby.
            if (_connectionManager.State == ConnectionState.k_client)
            {
                InitializeLobbyPanel(false);
            }

            // Autostart server for server builds
            // The ServerManager component on the NetworkManager prefab has the attribute '_startOnHeadless', which does the same thing.
#if UNITY_SERVER
            Debug.Log("Starting Server...");
            _connectionManager.StartServer();
#endif
        }

        private void Update()
        {
            // Refresh lobby list
            if (Input.GetKeyDown(KeyCode.R))
            {
                _refreshLobbyUI?.Invoke(Guid.Empty);
                _lobbyUI.SetAvailableLobbyScenes(LobbyManager.GetInstance()?.LobbyScenes);
            }
        }

        private void OnDestroy()
        {
            LobbyManager lobbyManager = LobbyManager.GetInstance();
            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.LobbyOpened -= _refreshLobbyUI;
            lobbyManager.LobbyClosed -= _refreshLobbyUI;
            _connectionManager.ConnectionState -= OnConnectionStateUpdate;
            _connectionManager.GlobalSceneLoaded -= InitializeLobbyPanel;
            _lobbyUI.OnConnectButtonPressed -= OnConnectBtnClicked;
            _lobbyUI.OnLobbyOpenButtonPressed -= OpenLobby;
            _lobbyUI.OnLobbyJoinButtonPressed -= JoinLobby;
        }


        private void InitializeLobbyPanel(bool isServer)
        {
            if (isServer)
            {
                return;
            }

            LobbyManager lobbyManager = LobbyManager.GetInstance();
            if (lobbyManager == null)
            {
                return;
            }

            _lobbyUI.SetAvailableLobbyScenes(lobbyManager.LobbyScenes);
            _lobbyUI.SetLobbies(lobbyManager.GetLobbies());
            _refreshLobbyUI = Handle;
            lobbyManager.LobbyOpened += Handle;
            lobbyManager.LobbyClosed += Handle;
            _lobbyUI.SetLobbyPanelActive(true);
            return;

            void Handle(Guid id)
            {
                _lobbyUI.SetLobbies(lobbyManager.GetLobbies());
            }
        }

        private static void JoinLobby(Guid lobbyId, string password)
        {
            LobbyManager.GetInstance()?.Client_JoinLobby(lobbyId, password);
        }

        private static void OpenLobby(string lobbyName, string password, string scenePath)
        {
            LobbyManager.GetInstance()?.Client_CreateLobby(lobbyName, password, scenePath, 16);
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