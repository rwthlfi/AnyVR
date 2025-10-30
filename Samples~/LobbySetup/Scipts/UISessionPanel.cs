using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnyVR.LobbySystem;
using AnyVR.LobbySystem.Internal;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using LobbyPlayerState = AnyVR.LobbySystem.LobbyPlayerState;

namespace AnyVR.Sample
{
    public class UISessionPanel : MonoBehaviour
    {
        [SerializeField] private UIUserListEntry _entryPrefab;

        [SerializeField] private RectTransform _entryParent;

        [SerializeField] private TextMeshProUGUI _pingLabel;

        [SerializeField] private TextMeshProUGUI _lobbyNameLabel;

        [SerializeField] private TextMeshProUGUI _ownerLabel;

        [SerializeField] private TextMeshProUGUI _locationLabel;

        [SerializeField] private TextMeshProUGUI _quickConnectLabel;

        [SerializeField] private TextMeshProUGUI _capacityLabel;

        private readonly Dictionary<int, UIUserListEntry> _players = new();

        private LobbyState _lobbyState;

        private Coroutine _pingCoroutine;

        private void Start()
        {
            _lobbyState = LobbyState.GetInstance();
            if (_lobbyState == null)
            {
                Debug.LogWarning("LobbyHandler not found. Disabling UISessionPanel.");
                return;
            }

            _lobbyState.OnPlayerJoin += AddPlayerEntry;
            _lobbyState.OnPlayerLeave += RemovePlayerEntry;

            _pingCoroutine = StartCoroutine(Co_UpdatePingLabel());
        }

        private void OnEnable()
        {
            if (_lobbyState == null)
                return;

            UpdateSessionInfo();
            RemoveAllEntries();

            foreach (LobbyPlayerState player in _lobbyState.GetPlayerStates<LobbyPlayerState>())
            {
                AddPlayerEntry(player);
            }

            float playerCount = _lobbyState.GetPlayerStates().Count();
            float capacity = _lobbyState.Info.LobbyCapacity;

            _capacityLabel.text = $"({playerCount} / {capacity})";
        }

        private void OnDestroy()
        {
            if (_pingCoroutine != null)
                StopCoroutine(_pingCoroutine);
        }

        private IEnumerator Co_UpdatePingLabel()
        {
            const float pingInterval = 0.5f;
            if (_lobbyState == null)
            {
                yield return null;
            }

            ConnectionManager connectionManager = ConnectionManager.Instance;
            Assert.IsNotNull(connectionManager);

            while (_lobbyState != null)
            {
                _pingLabel.text = $"{connectionManager.Ping.ToString()} ms";
                yield return new WaitForSeconds(pingInterval);
            }
        }

        private void UpdateSessionInfo()
        {
            _lobbyNameLabel.text = _lobbyState.Info.Name.Value;
            _locationLabel.text = _lobbyState.Info.Scene.Name;
            _quickConnectLabel.text = _lobbyState.Info.QuickConnectCode.ToString();

            GlobalPlayerState owner = _lobbyState.Info.Creator;
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

        private void AddPlayerEntry(LobbyPlayerState lobbyPlayerState)
        {
            if (_players.ContainsKey(lobbyPlayerState.Global.GetID()))
                return;

            UIUserListEntry entry = Instantiate(_entryPrefab, _entryParent);
            entry.SetPlayerInfo(lobbyPlayerState);

            entry.OnKickButtonPressed += player => LobbyPlayerController.GetInstance().Kick(player);
            entry.OnPromoteToAdminButtonPressed += player => LobbyPlayerController.GetInstance().PromoteToAdmin(player);

            _players.Add(lobbyPlayerState.Global.GetID(), entry);
        }

        private void RemovePlayerEntry(int playerId)
        {
            _players.Remove(playerId, out UIUserListEntry entry);

            if (entry != null)
                Destroy(entry.gameObject);
        }
    }
}
