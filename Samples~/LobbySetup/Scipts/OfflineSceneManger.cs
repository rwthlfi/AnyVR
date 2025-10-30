using System;
using System.Collections.Generic;
using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class OfflineSceneManger : MonoBehaviour
    {
        [SerializeField] private LobbyConfiguration _lobbyConfiguration;

        [Header("UI/Connection Panel")]
        [SerializeField] private Button _connectBtn;

        [SerializeField] private RectTransform _connectionPanel;
        [FormerlySerializedAs("_serverPanel")]
        [SerializeField] private RectTransform _onlinePanel;

        [SerializeField] private TMP_InputField _serverAddressInputField;

        [SerializeField] private TMP_InputField _usernameInputField;

        [Header("UI/Server Panel/CreateLobby")]
        [SerializeField] private TMP_InputField _lobbyNameInputField;
        [SerializeField] private TMP_InputField _passwordInputField;
        [SerializeField] private Button _createLobbyBtn;

        [Header("UI/Server Panel/JoinLobby")]
        [SerializeField] private LobbyUIEntry _lobbyEntryPrefab;
        [SerializeField] private RectTransform _lobbyEntryParent;

        [Header("UI/Server Panel/QuickConnect")]
        [SerializeField] private TMP_InputField _quickConnectInputField;
        [SerializeField] private Button _quickConnectBtn;

        [Header("UI/Server Panel")]
        [SerializeField] private Button _leaveServerBtn;

        [Header("AnyVR")]
        [SerializeField] private LobbySceneMetaData _lobbySceneMetaData;

        private ConnectionManager _connectionManager;

        private Dictionary<Guid, LobbyUIEntry> _lobbyUIEntries;

        private RectTransform[] _panels;

        private void Start()
        {
            _connectionManager = ConnectionManager.Instance;
            Assert.IsNotNull(_connectionManager);

            LobbyManager.LobbyConfiguration = _lobbyConfiguration;

#if !UNITY_SERVER
            _panels = new[]
            {
                _connectionPanel, _onlinePanel
            };

            _connectionManager.OnClientConnectionState += OnClientConnectionStateChanged;
            _connectBtn.onClick.AddListener(ConnectToServer);
            _leaveServerBtn.onClick.AddListener(LeaveServer);
            OnClientConnectionStateChanged(_connectionManager.State);

            LobbyManager.OnClientInitialized += SubscribeLobbyEvents;
            SubscribeLobbyEvents(LobbyManager.Instance);

            _createLobbyBtn.onClick.AddListener(CreateLobby);
            _lobbyUIEntries = new Dictionary<Guid, LobbyUIEntry>();

            _quickConnectBtn.onClick.AddListener(HandleQuickConnect);

            UpdateLobbyUIEntries();

            return;

            void SubscribeLobbyEvents(LobbyManager lobbyManager)
            {
                if (lobbyManager == null)
                {
                    return;
                }
                lobbyManager.OnLobbyOpened += AddLobbyEntry;
                lobbyManager.OnLobbyClosed += RemoveLobbyEntry;
            }
#endif
        }

        private void OnDestroy()
        {
            _connectionManager.OnClientConnectionState -= OnClientConnectionStateChanged;

            LobbyManager lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
                return;

            lobbyManager.OnLobbyOpened -= AddLobbyEntry;
            lobbyManager.OnLobbyClosed -= RemoveLobbyEntry;
        }

        private void HandleQuickConnect()
        {
            LobbyManager lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                return;
            }

            _ = lobbyManager.QuickConnect(_quickConnectInputField.text);
        }

        private static void JoinLobby(Guid id)
        {
            LobbyManager lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
            {
                return;
            }

            if (LobbyManager.Instance.TryGetLobby(id, out ILobbyInfo lobby))
            {
                _ = lobbyManager.JoinLobby(lobby);
            }
        }

        private void UpdateLobbyUIEntries()
        {
            foreach (Guid lmd in _lobbyUIEntries.Keys)
            {
                RemoveLobbyEntry(lmd);
            }

            LobbyManager lobbyManager = LobbyManager.Instance;
            if (lobbyManager == null)
                return;

            foreach (ILobbyInfo lobbyInfo in lobbyManager.GetLobbies())
            {
                AddLobbyEntry(lobbyInfo);
            }
        }

        private void AddLobbyEntry(ILobbyInfo lobbyInfo)
        {
            if (_lobbyUIEntries.ContainsKey(lobbyInfo.LobbyId))
            {
                return;
            }

            LobbyUIEntry entry = Instantiate(_lobbyEntryPrefab, _lobbyEntryParent);
            entry.SetLobby(lobbyInfo);

            Assert.IsNotNull(LobbyManager.Instance);

            entry.OnJoinButtonPressed += JoinLobby;
            _lobbyUIEntries.Add(lobbyInfo.LobbyId, entry);
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

            LobbyManager lobbyManager = LobbyManager.Instance;
            Assert.IsNotNull(lobbyManager);

            _ = lobbyManager.CreateLobby(lobbyName, password, _lobbySceneMetaData, _lobbySceneMetaData.MaxUsers);
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
                    SetActivePanel(_onlinePanel);
                    break;
                case ConnectionState.Server:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private async void ConnectToServer()
        {
            await _connectionManager.ConnectToServer(new Uri(_serverAddressInputField.text), _usernameInputField.text);
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
