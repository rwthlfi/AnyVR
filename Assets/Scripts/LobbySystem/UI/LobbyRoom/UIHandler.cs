using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI.LobbyRoom
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
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            _lobbyHandler = LobbyHandler.TryGetInstance();
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

        public void LeaveLobby()
        {
            _lobbyHandler.Leave();
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

        public void SetLocalClientIsAdmin(bool b)
        {
            _isLocalAdmin = b;
        }

        private void OnDestroy()
        {
            s_instance = null;
        }

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

        private void UpdateClientList((int, string)[] clientIds, int adminId) =>
            _clientListHandler.UpdateClientList(clientIds, adminId);

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