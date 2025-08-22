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
        public delegate void PlayerJoinEvent(PlayerState playerState);

        public delegate void PlayerLeaveEvent(int playerId);

        [SerializeField] protected NetworkObject _playerStatePrefab;

        private readonly SyncDictionary<int, NetworkObject> _playerStates = new();

        public IEnumerable<T> GetPlayerStates<T>() where T : PlayerState
        {
            if (!_playerStates.IsInitialized)
                yield break;

            foreach (NetworkObject netObj in _playerStates.Values)
            {
                if (netObj != null && netObj.TryGetComponent(out T ps))
                    yield return ps;
            }
        }

        public IEnumerable<PlayerState> GetPlayerStates()
        {
            return GetPlayerStates<PlayerState>();
        }

        public event PlayerJoinEvent OnPlayerJoin;
        public event PlayerLeaveEvent OnPlayerLeave;

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
                    OnPlayerLeave?.Invoke(playerId);
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
        protected virtual PlayerState AddPlayerState(NetworkConnection conn, bool global = false)
        {
            PlayerState ps = Instantiate(_playerStatePrefab).GetComponent<PlayerState>();
            ps.NetworkObject.SetIsGlobal(global);
            ps.PostServerInitialized += Handler;
            Spawn(ps.gameObject, conn, gameObject.scene);
            return ps;

            void Handler()
            {
                _playerStates.Add(ps.GetID(), ps.NetworkObject);
                ps.PostServerInitialized -= Handler;
            }
        }

        [Server]
        protected virtual void RemovePlayerState(NetworkConnection conn)
        {
            Assert.IsTrue(_playerStates.ContainsKey(conn.ClientId));
            _playerStates.TryGetValue(conn.ClientId, out NetworkObject playerState);
            Despawn(playerState);
            _playerStates.Remove(conn.ClientId);
        }

        public PlayerState GetPlayerState(int clientId)
        {
            return GetPlayerState<PlayerState>(clientId);
        }

        public T GetPlayerState<T>(int clientId) where T : PlayerState
        {
            if (_playerStates.TryGetValue(clientId, out NetworkObject netObj) && netObj != null)
                return netObj.GetComponent<T>();
            return null;
        }
    }
}
