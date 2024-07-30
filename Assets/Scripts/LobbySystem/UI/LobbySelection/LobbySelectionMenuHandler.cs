using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbySystem.UI.LobbySelection
{
    public class LobbySelectionMenuHandler : MonoBehaviour
    {
        [SerializeField] private LoginManager _loginManager;
        [Header("PrefabSetup")]
        [SerializeField] private LobbyCardHandler _lobbyCardPrefab;
        [SerializeField] private Transform _lobbyCardParent;
        
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _pingLabel;
        [SerializeField] private RoomCreationManager _roomCreationManager;

        private bool _isRoomCreationSceneActive;

        private Dictionary<UILobbyMetaData, LobbyCardHandler> _lobbyCards;

        private LobbyManager _lobbyManager;

        private void Awake()
        {
            InitSingleton();
            _lobbyCards = new Dictionary<UILobbyMetaData, LobbyCardHandler>();
            _isRoomCreationSceneActive = false;

            _loginManager.ConnectionState += OnConnectionState;
        }

        private void OnConnectionState(bool isConnected)
        {
            Debug.Log(isConnected);
            if (isConnected)
            {
                InitLobby(LobbyManager.GetInstance());
            }
        }

        private void InitLobby(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
            if (_lobbyManager == null)
            {
                Debug.LogError("Passed LobbyManager is null");
                return;
            }
            Debug.Log(_lobbyManager);
            _lobbyManager.LobbyOpened += AddLobbyCard;
            _lobbyManager.LobbyClosed += RemoveLobbyCard;

            Dictionary<string, LobbyMetaData> lobbies =  _lobbyManager.GetAvailableLobbies();
            foreach (LobbyMetaData lobby in lobbies.Values)
            {
                AddLobbyCard(lobby);
            }
        }

        // TODO
        // private void FixedUpdate()
        // {
        //     long ping = _tm == null ? 0 : _tm.RoundTripTime;
        //     long deduction = (long)(_tm.TickDelta * 2000d);
        //     ping = (long)Mathf.Max(1, ping - deduction);
        //     _pingLabel.text = $"{ping}ms";
        // }

        public void UpdatePingValue(ushort ms)
        {
            _pingLabel.text = $"{ms}ms";
        }

        public void Client_OpenCreateRoomScene()
        {
            if (_isRoomCreationSceneActive)
            {
                return;
            }

            _isRoomCreationSceneActive = true;
            _roomCreationManager.gameObject.SetActive(true);
        }

        public void CloseCreateRoomScene(UILobbyMetaData uiLobbyMetaData)
        {
            if (!_isRoomCreationSceneActive)
            {
                return;
            }

            CloseCreateRoomScene();
            _lobbyManager.CreateLobby(uiLobbyMetaData.Name, uiLobbyMetaData.Location, uiLobbyMetaData.MaxClients);
        }

        public void CloseCreateRoomScene()
        {
            if (!_isRoomCreationSceneActive)
            {
                return;
            }

            _isRoomCreationSceneActive = false;
            _roomCreationManager.gameObject.SetActive(false);
        }

        private void AddLobbyCard(LobbyMetaData lobby)
        {
            UILobbyMetaData uiLobby = new(lobby);
            LobbyCardHandler card = Instantiate(_lobbyCardPrefab, _lobbyCardParent);
            card.SetLobbyMeta(uiLobby);
            card.JoinBtn += () =>
            {   
                _lobbyManager.JoinLobby(card.MetaData.ID);
            };
            _lobbyCards.Add(uiLobby, card);
        }

        private void RemoveLobbyCard(LobbyMetaData lobby)
        {
            UILobbyMetaData uiLobby = new(lobby);
            if (!_lobbyCards.TryGetValue(uiLobby, out LobbyCardHandler card))
            {
                return;
            }

            if (card != null)
            {
                Destroy(_lobbyCards[uiLobby].gameObject);
            }

            _lobbyCards.Remove(uiLobby);
        }

        public void JoinLobby(UILobbyMetaData metaData)
        {
            _lobbyManager.JoinLobby(metaData.ID);
        }

        private void OnDestroy()
        {
            _lobbyManager.LobbyOpened -= AddLobbyCard;
            _lobbyManager.LobbyClosed -= RemoveLobbyCard;
        }

        #region Singleton

        public static LobbySelectionMenuHandler s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of LobbySceneHandler already exists!");
                Destroy(this);
            }

            s_instance = this;
        }

        #endregion

    }
}