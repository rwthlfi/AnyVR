using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace AnyVR.LobbySystem.Internal
{
    /// <summary>
    ///     Provides a base implementation of the game state.
    ///     The *Game State* is responsible for enabling the clients to monitor the state of the game.
    ///     Maintains a synchronized collection of player states indexed by client ID.
    ///     <seealso cref="GlobalGameState" />
    ///     <seealso cref="LobbyState" />
    /// </summary>
    public abstract class GameStateBase : NetworkBehaviour
    {
#region Private Fields

        private static readonly Dictionary<Scene, GameStateBase> Instances = new();

#endregion

#region Replicated Fields

        private readonly SyncDictionary<int, PlayerStateBase> _playerStates = new();

#endregion

        internal static GameStateBase GetInstance(Scene scene)
        {
            return Instances.GetValueOrDefault(scene);
        }


#region Lifecycle

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _playerStates.OnChange += PlayerStatesOnChange;

            Assert.IsFalse(Instances.ContainsKey(gameObject.scene));
            Instances.Add(gameObject.scene, this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            Instances.Clear();
        }

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

#endregion

#region Public API

        /// <summary>
        ///     Invoked after a player joins the game.
        /// </summary>
        public event Action<PlayerStateBase> OnPlayerJoin;

        /// <summary>
        ///     Invoked after a player leaves the game.
        /// </summary>
        public event Action<int> OnPlayerLeave;

        /// <summary>
        ///     Returns an enumeration containing all player states with a specific type.
        /// </summary>
        /// <typeparam name="T">A derived type of <see cref="PlayerStateBase" />.</typeparam>
        public IEnumerable<T> GetPlayerStates<T>() where T : PlayerStateBase
        {
            if (!_playerStates.IsInitialized)
                yield break;

            foreach (PlayerStateBase playerState in _playerStates.Values)
            {
                if (playerState is T derived)
                    yield return derived;
            }
        }

        /// <summary>
        ///     Returns an enumeration containing all player states.
        /// </summary>
        public IEnumerable<PlayerStateBase> GetPlayerStates()
        {
            return GetPlayerStates<PlayerStateBase>();
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <typeparam name="T">A derived type of <see cref="PlayerStateBase" /> to cast the player state to.</typeparam>
        public T GetPlayerState<T>(int clientId) where T : PlayerStateBase
        {
            return _playerStates.GetValueOrDefault(clientId) as T;
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <returns></returns>
        public PlayerStateBase GetPlayerState(int clientId)
        {
            return _playerStates.GetValueOrDefault(clientId);
        }

#endregion

#region Server Methods

        [Server]
        internal void AddPlayerState(PlayerStateBase playerState)
        {
            _playerStates.Add(playerState.ID, playerState);
        }

        [Server]
        internal PlayerStateBase RemovePlayerState(NetworkConnection conn)
        {
            bool success = _playerStates.TryGetValue(conn.ClientId, out PlayerStateBase ps);
            Assert.IsTrue(success);
            _playerStates.Remove(conn.ClientId);
            return ps;
        }

  #endregion
    }
}
