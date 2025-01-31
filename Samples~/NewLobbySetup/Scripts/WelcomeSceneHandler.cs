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

        private async void OnConnectBtnClicked()
        {
            string tokenServerIp = serverIpInputField.text;
            ServerAddressResponse result = await ConnectionManager.RequestServerIp(tokenServerIp);
            if (!result.success)
            {
                return;
            }

            string userName = userNameInputField.text;
            _connectionManager.ConnectToServer(result, userName);
        }
    }
}