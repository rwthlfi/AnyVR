using AnyVr.LobbySystem;
using TMPro;
using UnityEngine;
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
            _connectionManager = ConnectionManager.GetInstance();
            connectButton.onClick.AddListener(OnConnectBtnClicked);
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
            _connectionManager.ConnectToServer(result);
        }
    }
}