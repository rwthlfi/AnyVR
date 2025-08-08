using System;
using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class OfflineSceneManger : MonoBehaviour
    {
        [Header("UI/Connection Panel")]
        [SerializeField] private Button _connectBtn;
        
        [SerializeField] private RectTransform _connectionPanel, _serverPanel;
        
        [SerializeField] private TMP_InputField _serverAddressInputField;
        
        [SerializeField] private TMP_InputField _usernameInputField;
        
        [Header("UI/Server Panel")]
        [SerializeField] private Button _leaveServerBtn;
        
        [Header("AnyVR")]
        [SerializeField] private ConnectionManager _connectionManager;


        private RectTransform[] _panels;
        
        private void Start()
        {
#if UNITY_SERVER
            _connectionManager.StartServer();
#else
            _panels = new[] { _connectionPanel, _serverPanel };
            _connectionManager.OnClientConnectionState += OnClientConnectionStateChanged;
            _connectBtn.onClick.AddListener(ConnectToServer);
            _leaveServerBtn.onClick.AddListener(LeaveServer);
            OnClientConnectionStateChanged(_connectionManager.State);
#endif
        }
        private void LeaveServer()
        {
            _connectionManager.LeaveServer();
        }

        private void OnDestroy()
        {
            _connectionManager.OnClientConnectionState -= OnClientConnectionStateChanged;
        }

        private void OnClientConnectionStateChanged(ConnectionState state)
        {
                switch (state)
                {
                    case ConnectionState.Disconnected:
                        SetActivePanel(_connectionPanel);
                        break;
                    case ConnectionState.Client:
                        SetActivePanel(_serverPanel);
                        break;
                    case ConnectionState.Server:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }
        }

        private async void ConnectToServer()
        {
            _connectionManager.UseSecureProtocol = false;
            _connectionManager.SetTokenServerIp(_serverAddressInputField.text);
            ServerAddressResponse res = await _connectionManager.RequestServerIp();

            if (!res.Success)
            {
                Debug.LogError(res.Error);
                return;
            }
            
            bool success = _connectionManager.ConnectToServer(res, _usernameInputField.text, out string error);
            if (success)
                return;
            
            Debug.LogError("Did not receive server address: " + error);
        }

        private void SetActivePanel(RectTransform activePanel)
        {
            foreach (RectTransform panel in _panels)
            {
                panel.gameObject.SetActive(false);
            }
            activePanel.gameObject.SetActive(true);
        }
    }
}
