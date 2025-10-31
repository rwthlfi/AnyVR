using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem.Internal
{
    /// <summary>
    ///     Provides a base implementation of the game state.
    ///     The *Game State* is responsible for enabling the clients to monitor the state of the game.
    ///     Maintains a synchronized collection of player states indexed by client ID.
    ///     <seealso cref="GlobalGameState" />
    ///     <seealso cref="LobbyState" />
    /// </summary>
    public abstract class BaseGameState<T> : NetworkBehaviour where T : NetworkBehaviour
    {
#region Serialized Fields

        [SerializeField] private T _playerStatePrefab;

#endregion

#region Replicated Fields

        private readonly SyncDictionary<int, NetworkBehaviour> _playerStates = new();

#endregion

#region Lifecycle Overrides

        private void Start()
        {
            Assert.IsNotNull(_playerStatePrefab);
            Assert.IsNotNull(_playerStatePrefab.GetComponent<GlobalPlayerState>());

            _playerStates.OnChange += PlayerStatesOnChange;
        }

#endregion

        private void PlayerStatesOnChange(SyncDictionaryOperation op, int playerId, NetworkBehaviour _, bool asServer)
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

#region Public API

        /// <summary>
        ///     Invoked after a player joins the game.
        /// </summary>
        public event Action<T> OnPlayerJoin;

        /// <summary>
        ///     Invoked after a player leaves the game.
        /// </summary>
        public event Action<int> OnPlayerLeave;

        /// <summary>
        ///     Returns an enumeration containing all player states with a specific type.
        /// </summary>
        /// <typeparam name="TDerived">A derived type of <typeparamref name="T" />.</typeparam>
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

        /// <summary>
        ///     Returns an enumeration containing all player states.
        /// </summary>
        public IEnumerable<T> GetPlayerStates()
        {
            return GetPlayerStates<T>();
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <typeparam name="TDerived">A derived type of <typeparamref name="T" /> to cast the player state to.</typeparam>
        public TDerived GetPlayerState<TDerived>(int clientId) where TDerived : T
        {
            if (!_playerStates.TryGetValue(clientId, out NetworkBehaviour ps) || ps == null)
                return null;

            if (ps is TDerived derived)
                return derived;
            return null;
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <returns></returns>
        public T GetPlayerState(int clientId)
        {
            return GetPlayerState<T>(clientId);
        }

        public T PlayerStatePrefab => _playerStatePrefab;

#endregion

#region Server Methods

        [Server]
        internal void AddPlayerState(T playerState)
        {
            _playerStates.Add(playerState.OwnerId, playerState);
        }

        [Server]
        internal T RemovePlayerState(NetworkConnection conn)
        {
            bool success = _playerStates.TryGetValue(conn.ClientId, out NetworkBehaviour ps);
            Assert.IsTrue(success);
            _playerStates.Remove(conn.ClientId);
            return ps as T;
        }

  #endregion
    }
}
