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

        private Dictionary<string, LobbyCardHandler> _lobbyCards;

        private LobbyManager _lobbyManager;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            _lobbyCards = new Dictionary<string, LobbyCardHandler>();
            _isRoomCreationSceneActive = false;

            _connectionManager = ConnectionManager.GetInstance();
            
            if (_connectionManager == null)
            {
                Debug.LogError("Instance of ConnectionManager not found");
                return;    
            }
            
            // LobbyManager.GetInstance() != null iff the client is unloading from a lobby.
            if (LobbyManager.GetInstance() != null)
            {
                // So here we initialize the lobby when reloading the WelcomeScene
                InitLobbyManager(LobbyManager.GetInstance());
            }

            // The global scene never unloads (on server and on client)
            // When the GlobalSceneLoaded callback fires 
            //      - on the server on startup
            //      - on the client when connecting to a server.
            // The LobbyManager is in the GlobalScene.
            // So here we wait for the LobbyManager to be instantiated and then init the selection menu.
            _connectionManager.GlobalSceneLoaded += asServer =>
            {
                if (!asServer) // On the server we don't need to update the ui
                {
                    InitLobbyManager(LobbyManager.GetInstance());
                }
            };
        }

        private void InitLobbyManager(LobbyManager lobbyManager)
        {
            if (_lobbyManager == lobbyManager)
            {
                Debug.LogWarning("The LobbyManager is already initialized");
                return;
            }
            
            _lobbyManager = lobbyManager;
            if (_lobbyManager == null)
            {
                Debug.LogError("Passed LobbyManager is null");
                return;
            }
            _lobbyManager.LobbyOpened += AddLobbyCard;
            _lobbyManager.LobbyClosed += RemoveLobbyCard;

            _lobbyManager.PlayerCountUpdate += (lobbyId, count) =>
            {
                if(_lobbyCards.TryGetValue(lobbyId, out LobbyCardHandler cardHandler))
                {
                    cardHandler.SetCurrentPlayerCount(count);
                }
            };
            
            RefreshLobbyList();
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
            _lobbyManager.Client_CreateLobby(uiLobbyMetaData.Name, uiLobbyMetaData.Location, uiLobbyMetaData.MaxClients);
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
            if (_lobbyCards.ContainsKey(uiLobby.ID))
            {
                Debug.LogWarning("A card with the same id has already been added.");
                return;
            }
            LobbyCardHandler card = Instantiate(_lobbyCardPrefab, _lobbyCardParent);
            card.SetLobbyMeta(uiLobby);
            card.JoinBtn += () =>
            {   
                _lobbyManager.JoinLobby(card.MetaData.ID);
            };
            _lobbyCards.Add(uiLobby.ID, card);
        }
        
        private void RemoveLobbyCard(string lobbyId)
        {
            if (!_lobbyCards.TryGetValue(lobbyId, out LobbyCardHandler card))
            {
                return;
            }

            if (card != null)
            {
                Destroy(_lobbyCards[lobbyId].gameObject);
            }

            _lobbyCards.Remove(lobbyId);
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