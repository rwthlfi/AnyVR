using AnyVr.LobbySystem;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace AnyVr.Samples.NewLobbySetup
{
    public class WelcomeSceneHandler : MonoBehaviour
    {
        [SerializeField] private TMP_InputField serverIpInputField;
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private Button connectButton;
        private ConnectionManager _connectionManager;

        private void Start()
        {
            Debug.Log("Welcome to AnyVr");
            _connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(_connectionManager);
            _connectionManager.ConnectionState += OnConnectionState;
            connectButton.onClick.AddListener(OnConnectBtnClicked);

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

        private void OnConnectionState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.k_client:
                    Debug.Log("Connected to server.");
                    break;
                case ConnectionState.k_server:
                    Debug.Log("Server started.");
                    break;
                case ConnectionState.k_disconnected:
                    Debug.Log("Disconnected from server.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private async void OnConnectBtnClicked()
        {
            string tokenServerIp = serverIpInputField.text;
            ServerAddressResponse result = await ConnectionManager.RequestServerIp(tokenServerIp);
            if (!result.success)
            {
                Debug.LogWarning("Failed to request server ip");
                return;
            }

            Debug.Log("Connecting to the server...");
            string userName = userNameInputField.text;
            if (!_connectionManager.ConnectToServer(result, userName, out string errorMessage))
            {
                Debug.LogWarning($"Failed to connect to the server: {errorMessage}");
            }
        }
    }
}