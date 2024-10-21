using System.Text.RegularExpressions;
using UnityEngine;

namespace LobbySystem.UI
{
    public class WelcomeScene : MonoBehaviour
    {
        [Header("Platform Setup")] 
        [SerializeField] private GameObject _pcParent;
        [SerializeField] private GameObject _vrParent;
        [SerializeField] private bool _forceVr;
        
        [Header("PC Menu Panels")] 
        [SerializeField] private GameObject _pcConnectionPanel;
        [SerializeField] private GameObject _pcLobbySelectionPanel;
        [SerializeField] private GameObject _pcCreateLobbyPanel;
        
        [Header("VR Menu Panels")] 
        [SerializeField] private GameObject _vrConnectionPanel;
        [SerializeField] private GameObject _vrLobbySelectionPanel;
        [SerializeField] private GameObject _vrCreateLobbyPanel;

        private ConnectionManager _connectionManager;
        private GameObject _connectionPanel;
        private GameObject _lobbySelectionPanel;
        private GameObject _createLobbyPanel;

        private void Start()
        {
            bool isServer = false;
#if UNITY_SERVER
            isServer = true;
#endif
            bool isVr = (_forceVr || Application.platform == RuntimePlatform.Android) && !isServer;
            _connectionPanel = isVr ? _vrConnectionPanel : _pcConnectionPanel;
            _lobbySelectionPanel = isVr ? _vrLobbySelectionPanel : _pcLobbySelectionPanel;
            _createLobbyPanel = isVr ? _vrCreateLobbyPanel : _pcCreateLobbyPanel;
           
            _vrParent.SetActive(isVr);
            _pcParent.SetActive(!isVr);

            _connectionManager = ConnectionManager.GetInstance();

            if (_connectionManager == null)
            {
                Debug.LogError("Instance of ConnectionManager not found");
                return;    
            }
            
            _connectionManager.ConnectionState += OnConnectionState;
            _connectionPanel.SetActive(true);
            _createLobbyPanel.SetActive(false);
            _lobbySelectionPanel.SetActive(false);
            
            OnConnectionState(_connectionManager.IsConnected);
        }

        private void OnConnectionState(bool isConnected)
        {
            _connectionPanel.SetActive(!isConnected);
            _lobbySelectionPanel.SetActive(isConnected);
        }

        private void OnDestroy()
        {
            _connectionManager.ConnectionState -= OnConnectionState;
        }

        internal void OnConnectBtn(string fishnetAddress, string liveKitAddress, string userName)
        {
            if (string.IsNullOrEmpty(userName))
            {
                return;
            }

            if (TryParseAddress(fishnetAddress, out (string ip, ushort port) fishnetRes))
            {
                if (TryParseAddress(liveKitAddress, out (string ip, ushort port) liveKitRes))
                {
                    _connectionManager.ConnectToServer(fishnetRes, liveKitRes, userName);
                    return;
                }
            }

            Debug.LogError(
                "Invalid client address! Address has to match the pattern <ip>:<port>"); //TODO: display msg graphically
        }

        private static bool TryParseAddress(string address, out (string, ushort) res)
        {
            res = (null, 0);
            if (!Regex.IsMatch(address, ".+:[0-9]+"))
            {
                return false;
            }

            string[] arr = address.Split(':'); // [ip, port]

            uint port = uint.Parse(arr[1]);
            if (port > ushort.MaxValue)
            {
                return false;
            }

            res = (arr[0], (ushort)port);
            return true;
        }
    }
}