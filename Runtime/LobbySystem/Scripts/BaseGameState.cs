using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T">The player state class</typeparam>
    public abstract class BaseGameState<T> : NetworkBehaviour where T : NetworkBehaviour
    {
        public delegate void PlayerJoinEvent(T playerState);

        public delegate void PlayerLeaveEvent(int playerId);

        [SerializeField] protected T _playerStatePrefab;

        private readonly SyncDictionary<int, NetworkBehaviour> _playerStates = new();

        public IEnumerable<T> GetPlayerStates<TDerived>() where TDerived : T
        {
            if (!_playerStates.IsInitialized)
                yield break;

            foreach (NetworkBehaviour playerState in _playerStates.Values)
            {
                if (playerState is TDerived derived)
                    yield return derived;
            }
        }

        public IEnumerable<T> GetPlayerStates()
        {
            return GetPlayerStates<T>();
        }

        public event PlayerJoinEvent OnPlayerJoin;
        public event PlayerLeaveEvent OnPlayerLeave;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _playerStates.OnChange += PlayerStatesOnOnChange;
        }

        private void PlayerStatesOnOnChange(SyncDictionaryOperation op, int playerId, NetworkBehaviour _, bool asServer)
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
        protected virtual T AddPlayerState(NetworkConnection conn, bool global = false)
        {
            T ps = Instantiate(_playerStatePrefab).GetComponent<T>();
            ps.NetworkObject.SetIsGlobal(global);
            Spawn(ps.gameObject, conn, gameObject.scene);

            _playerStates.Add(conn.ClientId, ps);
            return ps;

        }

        [Server]
        protected virtual void RemovePlayerState(NetworkConnection conn)
        {
            Assert.IsTrue(_playerStates.ContainsKey(conn.ClientId));
            if (_playerStates.TryGetValue(conn.ClientId, out NetworkBehaviour ps))
            {
                Despawn(ps.NetworkObject);
            }
            _playerStates.Remove(conn.ClientId);
        }

        public T GetPlayerState(int clientId)
        {
            return GetPlayerState<T>(clientId);
        }

        public TDerived GetPlayerState<TDerived>(int clientId) where TDerived : T
        {
            if (!_playerStates.TryGetValue(clientId, out NetworkBehaviour ps) || ps == null)
                return null;

            if (ps is TDerived derived)
                return derived;
            return null;
        }
    }
}
