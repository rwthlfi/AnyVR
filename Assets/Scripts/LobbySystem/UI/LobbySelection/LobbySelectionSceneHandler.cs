using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbySystem.UI.LobbySelection
{
    public class LobbySelectionSceneHandler : MonoBehaviour
    {
        [SerializeField] private LobbyCardHandler _lobbyCardPrefab;
        [SerializeField] private Transform _lobbyCardParent;
        [SerializeField] private TextMeshProUGUI _pingLabel;

        [SerializeField] [Scene] private string _roomCreationScene;

        private bool _isRoomCreationSceneActive;

        private Dictionary<UILobbyMetaData, LobbyCardHandler> _lobbyCards;

        private LobbyManager _lobbyManager;

        private void Start()
        {
            InitSingleton();
            _lobbyCards = new Dictionary<UILobbyMetaData, LobbyCardHandler>();
            _isRoomCreationSceneActive = false;
            _lobbyManager = LobbyManager.TryGetInstance();
            if (_lobbyManager == null)
            {
                Debug.LogError("No instance of LobbyManager found");
                return;
            }
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
            SceneManager.LoadSceneAsync(_roomCreationScene, LoadSceneMode.Additive);
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
            SceneManager.UnloadSceneAsync(_roomCreationScene);
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

        public static LobbySelectionSceneHandler s_instance;

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