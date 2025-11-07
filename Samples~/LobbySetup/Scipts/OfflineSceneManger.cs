using System;
using System.Collections.Generic;
using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class OfflineSceneManger : MonoBehaviour
    {
#region Serialized Fields
        
        [Header("UI/Connection Panel")]
        [SerializeField] private Button _connectBtn;

        [SerializeField] private RectTransform _connectionPanel;
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

#endregion

#region UI Fields

        // Maps each lobby to an ui element
        private Dictionary<Guid, LobbyUIEntry> _lobbyUIEntries;

#endregion

#region Lifecycle
        
        private void Start()
        {
            Assert.IsNotNull(ConnectionManager.Instance);
            
            ConnectionManager.Instance.OnClientConnectionState += OnClientConnectionStateChanged;
            OnClientConnectionStateChanged(ConnectionManager.Instance.State);
            
            _connectBtn.onClick.AddListener(ConnectToServer);
            _leaveServerBtn.onClick.AddListener(() => ConnectionManager.Instance.LeaveServer());
            _createLobbyBtn.onClick.AddListener( () => _ = GlobalPlayerController.Instance.CreateLobby(_lobbyNameInputField.text, _passwordInputField.text, _lobbySceneMetaData, _lobbySceneMetaData.MaxUsers));
            _quickConnectBtn.onClick.AddListener(() => _ = GlobalPlayerController.Instance.QuickConnect(_quickConnectInputField.text));

            _lobbyUIEntries = new Dictionary<Guid, LobbyUIEntry>();

            // Initialize lobby list if connected to server
            if (GlobalGameState.Instance != null)
                InitializeLobbyUIEntries();
        }
        
        private async void ConnectToServer()
        {
            Uri tokenServerUri = new($"http://{_serverAddressInputField.text}");
            await ConnectionManager.Instance.ConnectToServer(tokenServerUri, _usernameInputField.text);
            
            InitializeLobbyUIEntries();
        }

        private void InitializeLobbyUIEntries()
        {
            GlobalGameState.Instance.OnLobbyOpened += AddLobbyEntry;
            GlobalGameState.Instance.OnLobbyClosed += RemoveLobbyEntry;

            UpdateLobbyUIEntries();
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

        private void OnDestroy()
        {
            ConnectionManager.Instance.OnClientConnectionState -= OnClientConnectionStateChanged;

            GlobalGameState globalGameState = GlobalGameState.Instance;
            if (globalGameState == null)
                return;

            globalGameState.OnLobbyOpened -= AddLobbyEntry;
            globalGameState.OnLobbyClosed -= RemoveLobbyEntry;
        }
        
#endregion

#region UI Methods

        private void UpdateLobbyUIEntries()
        {
            foreach (Guid lmd in _lobbyUIEntries.Keys)
            {
                RemoveLobbyEntry(lmd);
            }

            foreach (ILobbyInfo lobbyInfo in GlobalGameState.Instance.GetLobbies())
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

            entry.OnJoinButtonPressed += id => _ = GlobalPlayerController.Instance.JoinLobby(id);
            _lobbyUIEntries.Add(lobbyInfo.LobbyId, entry);
        }

        private void RemoveLobbyEntry(Guid lobbyId)
        {
            if (!_lobbyUIEntries.Remove(lobbyId, out LobbyUIEntry entry))
                return;

            Destroy(entry.gameObject);
        }
        
        private void SetActivePanel(RectTransform activePanel)
        {
            foreach (RectTransform panel in new[] { _connectionPanel, _onlinePanel })
            {
                panel.gameObject.SetActive(false);
            }
            activePanel.gameObject.SetActive(true);
        }
        
#endregion
        
    }
}
