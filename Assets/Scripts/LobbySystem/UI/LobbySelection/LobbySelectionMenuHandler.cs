using FishNet.Managing.Client;
using FishNet.Managing.Timing;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbySystem.UI.LobbySelection
{
    public class LobbySelectionMenuHandler : MonoBehaviour
    {
        [Header("PrefabSetup")]
        [SerializeField] private LobbyCardHandler _lobbyCardPrefab;
        [SerializeField] private Transform _lobbyCardParent;
        
        [Header("UI")]
        [SerializeField] private TextMeshProUGUI _pingLabel;
        [SerializeField] private RoomCreationManager _roomCreationManager;
        
        private ConnectionManager _connectionManager;

        private bool _isRoomCreationSceneActive;

        private Dictionary<UILobbyMetaData, LobbyCardHandler> _lobbyCards;

        private LobbyManager _lobbyManager;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            _lobbyCards = new Dictionary<UILobbyMetaData, LobbyCardHandler>();
            _isRoomCreationSceneActive = false;

            _connectionManager = ConnectionManager.GetInstance();
            
            if (_connectionManager == null)
            {
                Debug.LogError("Instance of ConnectionManager not found");
                return;    
            }
            
            if (LobbyManager.GetInstance() != null)
            {
                InitLobby(LobbyManager.GetInstance());
            }

            _connectionManager.GlobalSceneLoaded += GlobalSceneLoaded;
        }

        private void GlobalSceneLoaded()
        {
            InitLobby(LobbyManager.GetInstance());
        }

        private void InitLobby(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
            if (_lobbyManager == null)
            {
                Debug.LogError("Passed LobbyManager is null");
                return;
            }
            _lobbyManager.LobbyOpened += AddLobbyCard;
            _lobbyManager.LobbyClosed += RemoveLobbyCard;
            
            RefreshLobbyList();

            _lobbyManager.ClientJoin += (s, i) =>
            {
                Debug.Log("Client Joint");
            };
            _lobbyManager.ClientLeave += (s, i) =>
            {
                Debug.Log("Client Leave");
            };
        }

        public void RefreshLobbyList()
        {
            foreach (LobbyCardHandler card in _lobbyCards.Values)
            {
                Destroy(card.gameObject);
            }
            _lobbyCards.Clear();
            
            Dictionary<string, LobbyMetaData> lobbies =  _lobbyManager.GetAvailableLobbies();
            foreach (LobbyMetaData lobby in lobbies.Values)
            {
                AddLobbyCard(lobby);
            }
        }

        public void LeaveServerBtn()
        {
            _connectionManager.LeaveServer();
        }

        private void FixedUpdate()
        {
            if (_lobbyManager == null || _lobbyManager.TimeManager == null)
            {
                return;
            }

            long ping = _lobbyManager.TimeManager == null ? 0 : _lobbyManager.TimeManager.RoundTripTime;
            long deduction = (long)(_lobbyManager.TimeManager.TickDelta * Time.fixedDeltaTime);
            ping = (long)Mathf.Max(1, ping - deduction);
            _pingLabel.text = $"{ping}ms";
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

        private void OnDestroy()
        {
            if (_lobbyManager == null)
            {
                return;
            }
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