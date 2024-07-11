using GameKit.Dependencies.Utilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public event Action<UILobbyMetaData> OnCreateLobbyBtn;

        public event Action<UILobbyMetaData> OnJoinLobbyBtn;

        public event Action OnRefreshLobbyBtn;

        private void Start()
        {
            InitSingleton();
            _lobbyCards = new Dictionary<UILobbyMetaData, LobbyCardHandler>();
            _isRoomCreationSceneActive = false;
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

        private void UpdateLobbyList(IEnumerable<UILobbyMetaData> lobbies)
        {
            while (_lobbyCards.Count > 0)
            {
                RemoveLobbyCard(_lobbyCards.First().Key);
            }

            foreach (UILobbyMetaData lobby in lobbies)
            {
                AddLobbyCard(lobby);
            }
        }

        public void Client_OpenCreateRoomScene()
        {
            if (_isRoomCreationSceneActive)
            {
                return;
            }

            _isRoomCreationSceneActive = true;
            AsyncOperation op =
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_roomCreationScene, LoadSceneMode.Additive);
        }

        public void CloseCreateRoomScene(UILobbyMetaData uiLobbyMetaData)
        {
            if (!_isRoomCreationSceneActive)
            {
                return;
            }

            CloseCreateRoomScene();
            OnCreateLobbyBtn?.Invoke(uiLobbyMetaData);
        }

        public void CloseCreateRoomScene()
        {
            if (!_isRoomCreationSceneActive)
            {
                return;
            }

            _isRoomCreationSceneActive = false;
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_roomCreationScene);
        }

        private void AddLobbyCard(UILobbyMetaData uiLobby)
        {
            LobbyCardHandler card = Instantiate(_lobbyCardPrefab, _lobbyCardParent);
            card.SetLobbyMeta(uiLobby);
            _lobbyCards.Add(uiLobby, card);
        }

        private void RemoveLobbyCard(UILobbyMetaData uiLobby)
        {
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

        public void RefreshLobbies()
        {
            OnRefreshLobbyBtn?.Invoke();
        }

        public void JoinLobby(UILobbyMetaData metaData)
        {
            OnJoinLobbyBtn?.Invoke(metaData);
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