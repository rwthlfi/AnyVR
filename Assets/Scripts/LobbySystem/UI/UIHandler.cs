using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI
{
    public class UIHandler : MonoBehaviour
    {
        [SerializeField] private ClientListHandler _clientListHandler;

        [SerializeField] private PausePanelHandler _pausePanelHandler;

        [SerializeField] private Toggle _muteToggle;

        private readonly List<GameObject> _panels = new();
        private bool _isLocalAdmin;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            _muteToggle.SetIsOnWithoutNotify(true);
            _isLocalAdmin =
                LobbyRoomHandler
                    .IsLocalClientAdmin(); //TODO: Remove Fishnet from LobbySystem.UI Assembly Definition from 

            LobbyRoomHandler.ClientListReceived += list =>
            {
                UpdateClientList(list, LobbyRoomHandler.GetAdminId());
            };
            LobbyRoomHandler.ClientJoined += clientId =>
            {
                _clientListHandler.AddClient(clientId, PlayerNameTracker.GetPlayerName(clientId));
            };
            LobbyRoomHandler.ClientLeft += _clientListHandler.RemoveClient;

            _clientListHandler.gameObject.SetActive(false);
            _pausePanelHandler.gameObject.SetActive(false);

            _clientListHandler.ClientKick += ClientKick;
            _clientListHandler.ClientMuteChange += ClientMuteChange;

            _panels.Add(_clientListHandler.gameObject);
            _panels.Add(_pausePanelHandler.gameObject);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _muteToggle.onValueChanged.AddListener(b =>
            {
                SetMicrophoneEnableState?.Invoke(!b);
            });
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Tab))
            {
                return;
            }

            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        private void OnDestroy()
        {
            s_instance = null;
        }

        public event Action<int, bool> ClientMuteChange;

        public event Action<bool> MuteChange;

        public event Action<int> ClientKick;

        public event Action<bool> SetMicrophoneEnableState;

        internal bool IsLocalClientAdmin()
        {
            return _isLocalAdmin;
        }

        public void TogglePauseMenu()
        {
            TogglePanel(_pausePanelHandler.gameObject);
        }

        public void ToggleClientListPanel()
        {
            TogglePanel(_clientListHandler.gameObject);
        }

        private void TogglePanel(GameObject panel)
        {
            SetPanelActive(panel, !panel.activeSelf);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            foreach (GameObject e in _panels)
            {
                e.SetActive(false);
            }

            panel.SetActive(active);
        }

        private void UpdateClientList(IEnumerable<int> clientIds, int adminId)
        {
            _clientListHandler.UpdateClientList(clientIds, adminId);
        }

        #region Singleton

        public static UIHandler s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of UIHandler already exists!");
                return;
            }

            s_instance = this;
        }

        #endregion
    }
}