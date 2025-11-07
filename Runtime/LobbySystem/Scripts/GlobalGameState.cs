using System;
using System.Collections.Generic;
using AnyVR.LobbySystem.Internal;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the global game state that is replicated to all clients.
    ///     Maintains a synchronized collection of <see cref="GlobalPlayerState" /> instances indexed by client ID.
    ///     Inherit from this class to add additional synchronized properties as needed.
    ///     <seealso cref="LobbyState" />
    /// </summary>
    public class GlobalGameState : GameStateBase
    {
        protected void Awake()
        {
            InitSingleton();
        }

        /// <summary>
        ///     Returns an enumeration containing all player states with a specific type.
        /// </summary>
        /// <typeparam name="T">A derived type of <see cref="GlobalPlayerState" />.</typeparam>
        public new IEnumerable<T> GetPlayerStates<T>() where T : GlobalPlayerState
        {
            return base.GetPlayerStates<T>();
        }

        /// <summary>
        ///     Returns an enumeration containing all player states.
        /// </summary>
        public new IEnumerable<GlobalPlayerState> GetPlayerStates()
        {
            return GetPlayerStates<GlobalPlayerState>();
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <typeparam name="T">A derived type of <see cref="GlobalPlayerState" /> to cast the player state to.</typeparam>
        public new T GetPlayerState<T>(int clientId) where T : GlobalPlayerState
        {
            return base.GetPlayerState<T>(clientId);
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <returns></returns>
        public new GlobalPlayerState GetPlayerState(int clientId)
        {
            return GetPlayerState<GlobalPlayerState>(clientId);
        }

        // TODO
        internal GlobalLobbyState GetLobbyInfo(Guid lobbyId)
        {
            return LobbyManager.Instance.TryGetLobby(lobbyId, out ILobbyInfo lobby) ? (GlobalLobbyState)lobby : null;
        }


#region Singleton

        /// <summary>
        ///     The singleton instance of the <see cref="GlobalGameState" />.
        ///     Is not null if the local client is connected to a server.
        /// </summary>
        public static GlobalGameState Instance { get; private set; }

        private void InitSingleton()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

#endregion
    }
}
