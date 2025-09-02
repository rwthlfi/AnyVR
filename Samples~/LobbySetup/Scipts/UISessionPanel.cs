using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.Sample
{
    public class UISessionPanel : MonoBehaviour
    {
        private LobbyHandler _lobbyHandler;

        private readonly Dictionary<int, UIUserListEntry> _players = new();
        
        [SerializeField] private UIUserListEntry _entryPrefab;
        
        [SerializeField] private RectTransform _entryParent;

        [SerializeField] private TextMeshProUGUI _pingLabel;
        
        [SerializeField] private TextMeshProUGUI _lobbyNameLabel;
        
        [SerializeField] private TextMeshProUGUI _ownerLabel;
        
        [SerializeField] private TextMeshProUGUI _locationLabel;
        
        [SerializeField] private TextMeshProUGUI _quickConnectLabel;
        
        [SerializeField] private TextMeshProUGUI _capacityLabel;
        
        private Coroutine _pingCoroutine;

        private void Start()
        {
            _lobbyHandler = LobbyHandler.GetInstance();
            if (_lobbyHandler == null)
            {
                Debug.LogWarning("LobbyHandler not found. Disabling UISessionPanel.");
                return;
            }
            
            _lobbyHandler.OnPlayerJoin += AddPlayerEntry;
            _lobbyHandler.OnPlayerLeave += RemovePlayerEntry;

            _pingCoroutine = StartCoroutine(Co_UpdatePingLabel());
        }

        private IEnumerator Co_UpdatePingLabel()
        {
            const float pingInterval = 0.5f;
            if (_lobbyHandler == null)
            {
                yield return null;
            }

            ConnectionManager connectionManager = ConnectionManager.GetInstance();
            Assert.IsNotNull(connectionManager);
            
            while (_lobbyHandler != null)
            {
                _pingLabel.text = $"{connectionManager.Latency.ToString()} ms";
                yield return new WaitForSeconds(pingInterval);
            }
        }

        private void OnEnable()
        {
            if (_lobbyHandler == null)
                return;

            UpdateSessionInfo();
            RemoveAllEntries();

            foreach (LobbyPlayerState player in _lobbyHandler.GetPlayerStates<LobbyPlayerState>())
            {
                AddPlayerEntry(player);
            }
            
            float playerCount = _lobbyHandler.GetPlayerStates().Count();
            float capacity = _lobbyHandler.MetaData.LobbyCapacity;
            
            _capacityLabel.text = $"({playerCount} / {capacity})";
        }
        
        private void UpdateSessionInfo()
        {
            _lobbyNameLabel.text = _lobbyHandler.MetaData.Name;
            _locationLabel.text = _lobbyHandler.MetaData.SceneName;
            _quickConnectLabel.text = _lobbyHandler.QuickConnectCode.ToString();

            PlayerState owner = _lobbyHandler.GetLobbyOwner();
            _ownerLabel.text = owner != null ? owner.GetName() : "N/A (disconnected)";
        }

        private void RemoveAllEntries()
        {
            foreach (UIUserListEntry entry in _players.Values)
            {
                Destroy(entry.gameObject);
            }
            _players.Clear();
        }

        private void AddPlayerEntry(PlayerState playerState)
        {
            if (_players.ContainsKey(playerState.GetID()))
                return;
            
            UIUserListEntry entry = Instantiate(_entryPrefab, _entryParent);
            entry.SetPlayerInfo((LobbyPlayerState) playerState);
            
            entry.OnKickButtonPressed += player => player.KickPlayer();
            entry.OnPromoteToAdminButtonPressed += player => player.PromoteToAdmin();
            
            _players.Add(playerState.GetID(), entry);
        }
        
        private void RemovePlayerEntry(int playerId)
        {
            _players.Remove(playerId, out UIUserListEntry entry);

            if (entry != null)
                Destroy(entry.gameObject);
        }

        private void OnDestroy()
        {
            if(_pingCoroutine != null)
                StopCoroutine(_pingCoroutine);
        }
    }
}
