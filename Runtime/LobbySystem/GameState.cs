using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public class GameState : NetworkBehaviour
    {
        [SerializeField] protected NetworkObject _playerStatePrefab;
        
        public delegate void PlayerJoinEvent(PlayerState playerState);
        public delegate void PlayerLeaveEvent(PlayerState playerState);
        
        public event PlayerJoinEvent OnPlayerJoin;
        public event PlayerLeaveEvent OnPlayerLeave;

        private readonly SyncDictionary<int, NetworkObject> _playerStates = new();

        public IEnumerable<PlayerState> PlayerStates
        {
            get {
                foreach (NetworkObject netObj in _playerStates.Values)
                {
                    if (netObj != null && netObj.TryGetComponent(out PlayerState ps))
                        yield return ps;
                }
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _playerStates.OnChange += PlayerStatesOnOnChange;
        }
        
        private void PlayerStatesOnOnChange(SyncDictionaryOperation op, int playerId, NetworkObject _, bool asServer)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    OnPlayerJoin?.Invoke(GetPlayerState(playerId));
                    break;
                case SyncDictionaryOperation.Remove:
                    OnPlayerLeave?.Invoke(GetPlayerState(playerId));
                    break;
                case SyncDictionaryOperation.Clear:
                    break;
                case SyncDictionaryOperation.Set:
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }
        
        [Server]
        protected virtual void AddPlayerState(NetworkConnection conn)
        {
            PlayerState ps = NetworkManager.GetPooledInstantiated(_playerStatePrefab, asServer: true).GetComponent<PlayerState>();
            Spawn(ps.gameObject, conn, gameObject.scene);
            _playerStates.Add(ps.GetID(), ps.NetworkObject);
        }
        
        [Server]
        protected virtual void RemovePlayerState(NetworkConnection conn)
        {
            Assert.IsTrue(_playerStates.ContainsKey(conn.ClientId));
            _playerStates.TryGetValue(conn.ClientId, out NetworkObject playerState);
            Despawn(playerState);
            _playerStates.Remove(conn.ClientId);
        }
        
        public PlayerState GetPlayerState(int clientId) => GetPlayerState<PlayerState>(clientId);
        
        public PlayerState GetPlayerState<T>(int clientId) where T : PlayerState
        {
            if (_playerStates.TryGetValue(clientId, out NetworkObject netObj) && netObj != null)
                return netObj.GetComponent<T>();
            return null;
        }
    }
}
