using AnyVr.LobbySystem;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVr.Samples.LobbySetup
{
    public class UIHandler : MonoBehaviour
    {
        [SerializeField] private ClientListHandler _clientListHandler;

        [SerializeField] private PausePanelHandler _pausePanelHandler;

        [SerializeField] private Toggle _muteToggle;

        private readonly List<GameObject> _panels = new();

        private bool _isLocalAdmin;

        private LobbyHandler _lobbyHandler;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            _muteToggle.SetIsOnWithoutNotify(true);

            _clientListHandler.gameObject.SetActive(false);
            _pausePanelHandler.gameObject.SetActive(false);

            _panels.Add(_clientListHandler.gameObject);
            _panels.Add(_pausePanelHandler.gameObject);

            foreach (GameObject panel in _panels)
            {
                panel.SetActive(false);
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _lobbyHandler = LobbyHandler.GetInstance();
            if (_lobbyHandler == null)
            {
                Debug.LogError("UIScene could not find the LobbyHandler");
                return;
            }

            UpdateClientList(_lobbyHandler.GetClients(), _lobbyHandler.GetAdminId());

            _lobbyHandler.ClientJoin += (clientId, clientName) =>
            {
                _clientListHandler.AddClient(clientId, clientName);
            };
            _lobbyHandler.ClientLeft += clientId =>
            {
                _clientListHandler.RemoveClient(clientId);
            };

            _muteToggle.onValueChanged.AddListener(b =>
            {
                _lobbyHandler.SetMuteSelf(b);
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePanel(Panel.PausePanel);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                TogglePanel(Panel.ClientListPanel);
            }
        }

        private void OnDestroy()
        {
            s_instance = null;
        }

        public void LeaveLobby()
        {
            _lobbyHandler.Leave();
        }

        public void SetLocalClientIsAdmin(bool b)
        {
            _isLocalAdmin = b;
        }

        internal bool IsLocalClientAdmin()
        {
            return _isLocalAdmin;
        }

        internal void TogglePanel(Panel panel)
        {
            switch (panel)
            {
                case Panel.PausePanel:
                    SetPanelActive(panel, !_pausePanelHandler.gameObject.activeSelf);
                    break;
                case Panel.ClientListPanel:
                    SetPanelActive(panel, !_clientListHandler.gameObject.activeSelf);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(panel), panel, null);
            }
        }

        internal void SetPanelActive(Panel panel, bool active)
        {
            foreach (GameObject e in _panels)
            {
                e.SetActive(false);
            }

            Cursor.visible = active;
            Cursor.lockState = active ? CursorLockMode.Confined : CursorLockMode.Locked;

            switch (panel)
            {
                case Panel.PausePanel:
                    _pausePanelHandler.gameObject.SetActive(active);
                    break;
                case Panel.ClientListPanel:
                    _clientListHandler.gameObject.SetActive(active);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(panel), panel, null);
            }
        }

        private void UpdateClientList((int, string)[] clientIds, int adminId)
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

    internal enum Panel
    {
        PausePanel, ClientListPanel
    }
}