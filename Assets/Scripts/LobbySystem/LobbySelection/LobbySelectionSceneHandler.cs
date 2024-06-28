using FishNet;
using FishNet.Managing.Timing;
using GameKit.Dependencies.Utilities.Types;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
//TODO: Remove TMPro from the LobbySystem Assembly Definition. 

namespace LobbySystem.LobbySelection
{
    public class LobbySelectionSceneHandler : MonoBehaviour
    {
        [SerializeField] private LobbyCardHandler _lobbyCardPrefab;
        [SerializeField] private Transform _lobbyCardParent;
        [SerializeField] private TextMeshProUGUI _pingLabel;

        [SerializeField] [Scene] private string _roomCreationScene;

        private bool _isRoomCreationSceneActive;

        private Dictionary<LobbyMetaData, LobbyCardHandler> _lobbyCards;

        private TimeManager _tm;

        private void Start()
        {
            InitSingleton();
            _lobbyCards = new Dictionary<LobbyMetaData, LobbyCardHandler>();
            _tm = InstanceFinder.TimeManager;
            _isRoomCreationSceneActive = false;
            LobbyManager.s_instance.LobbyOpened += _ =>
            {
                LobbyManager.s_instance.RequestCurrentLobbiesRpc();
            };
            LobbyManager.s_instance.LobbyClosed += _ =>
            {
                LobbyManager.s_instance.RequestCurrentLobbiesRpc();
            };
            LobbyManager.s_instance.ReceivedCurrentLobbies += UpdateLobbyList;
        }

        private void FixedUpdate()
        {
            long ping = _tm == null ? 0 : _tm.RoundTripTime;
            long deduction = (long)(_tm.TickDelta * 2000d);
            ping = (long)Mathf.Max(1, ping - deduction);
            _pingLabel.text = $"{ping}ms";
        }

        private void OnDestroy()
        {
            LobbyManager.s_instance.LobbyOpened -= AddLobbyCard;
            LobbyManager.s_instance.LobbyClosed -= RemoveLobbyCard;
        }

        private void UpdateLobbyList(LobbyMetaData[] lobbies)
        {
            while (_lobbyCards.Count > 0)
            {
                RemoveLobbyCard(_lobbyCards.First().Key);
            }

            foreach (LobbyMetaData lobby in lobbies)
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
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_roomCreationScene, LoadSceneMode.Additive);
        }

        public void CloseCreateRoomScene(LobbyMetaData lobbyMetaData)
        {
            if (!_isRoomCreationSceneActive)
            {
                return;
            }

            CloseCreateRoomScene();
            LobbyManager.s_instance.CreateLobbyRpc(lobbyMetaData);
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

        private void AddLobbyCard(LobbyMetaData lobby)
        {
            LobbyCardHandler card = Instantiate(_lobbyCardPrefab, _lobbyCardParent);
            card.SetLobbyMeta(lobby);
            _lobbyCards.Add(lobby, card);
        }

        private void RemoveLobbyCard(LobbyMetaData lobby)
        {
            if (!_lobbyCards.TryGetValue(lobby, out LobbyCardHandler card))
            {
                return;
            }

            if (card != null)
            {
                Destroy(_lobbyCards[lobby].gameObject);
            }

            _lobbyCards.Remove(lobby);
        }

        public void RefreshLobbies()
        {
            LobbyManager.s_instance.RequestCurrentLobbiesRpc();
        }

        public static void JoinLobby(LobbyMetaData metaData)
        {
            LobbyManager.s_instance.JoinLobbyRpc(metaData);
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