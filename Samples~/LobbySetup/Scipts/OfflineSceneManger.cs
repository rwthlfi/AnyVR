using System;
using System.Collections.Generic;
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

        [Header("UI/Server Panel/CreateLobby")]
        [SerializeField] private TMP_InputField _lobbyNameInputField;
        [SerializeField] private TMP_InputField _passwordInputField;
        [SerializeField] private Button _createLobbyBtn;

        [Header("UI/Server Panel/JoinLobby")]
        [SerializeField] private LobbyUIEntry _lobbyEntryPrefab;
        [SerializeField] private RectTransform _lobbyEntryParent;

        [Header("UI/Server Panel")]
        [SerializeField] private Button _leaveServerBtn;

        [Header("AnyVR")]
        [SerializeField] private ConnectionManager _connectionManager;
        [SerializeField] private LobbySceneMetaData _lobbySceneMetaData;

        private LobbyManager _lobbyManager;

        private Dictionary<Guid, LobbyUIEntry> _lobbyUIEntries;

        private RectTransform[] _panels;

        private void Start()
        {
#if UNITY_SERVER
            _connectionManager.StartServer();
#else
            _panels = new[]
            {
                _connectionPanel, _serverPanel
            };
            _connectionManager.OnClientConnectionState += OnClientConnectionStateChanged;
            _connectBtn.onClick.AddListener(ConnectToServer);
            _leaveServerBtn.onClick.AddListener(LeaveServer);
            OnClientConnectionStateChanged(_connectionManager.State);

            LobbyManager.OnClientInitialized += manager =>
            {
                _lobbyManager = manager;
                _lobbyManager.OnLobbyOpened += AddLobbyEntry;
                _lobbyManager.OnLobbyClosed += RemoveLobbyEntry;
            };

            _createLobbyBtn.onClick.AddListener(CreateLobby);
            _lobbyUIEntries = new Dictionary<Guid, LobbyUIEntry>();
#endif
        }

        private void OnDestroy()
        {
            _connectionManager.OnClientConnectionState -= OnClientConnectionStateChanged;
        }

        private void AddLobbyEntry(Guid lobbyId)
        {
            if (_lobbyUIEntries.ContainsKey(lobbyId))
            {
                return;
            }

            if (!_lobbyManager.TryGetLobby(lobbyId, out LobbyMetaData lobby))
                return;

            LobbyUIEntry entry = Instantiate(_lobbyEntryPrefab, _lobbyEntryParent);
            entry.SetLobby(lobbyId, lobby.Name, lobby.SceneName, lobby.CreatorId, lobby.LobbyCapacity);
            entry.OnJoinButtonPressed += id => _lobbyManager.Client_JoinLobby(id);
            _lobbyUIEntries.Add(lobbyId, entry);
        }

        private void RemoveLobbyEntry(Guid lobbyId)
        {
            if (!_lobbyUIEntries.Remove(lobbyId, out LobbyUIEntry entry))
                return;

            Destroy(entry.gameObject);
        }

        private void CreateLobby()
        {
            string lobbyName = _lobbyNameInputField.text;
            string password = _passwordInputField.text;

            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                return;
            }
            _lobbyManager.Client_CreateLobby(lobbyName, password, _lobbySceneMetaData, _lobbySceneMetaData.MaxUsers);
        }
        private void LeaveServer()
        {
            _connectionManager.LeaveServer();
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
