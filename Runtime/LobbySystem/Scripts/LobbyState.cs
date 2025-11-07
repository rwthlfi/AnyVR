using System;
using System.Collections.Generic;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public class LobbyState : GameStateBase
    {
#region Replicated Properties

        private readonly SyncVar<Guid> _lobbyId = new();

#endregion

        [Server]
        internal void SetLobbyId(Guid lobbyId)
        {
            Assert.IsTrue(lobbyId != Guid.Empty);
            _lobbyId.Value = lobbyId;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            OnPlayerJoin += _ =>
            {
                ((GlobalLobbyState)LobbyInfo).SetPlayerNum((ushort)GetPlayerStates().Count());
            };
            OnPlayerLeave += _ =>
            {
                ((GlobalLobbyState)LobbyInfo).SetPlayerNum((ushort)GetPlayerStates().Count());
            };
        }


        /// <summary>
        ///     Returns an enumeration containing all player states with a specific type.
        /// </summary>
        /// <typeparam name="T">A derived type of <see cref="LobbyPlayerState" />.</typeparam>
        public new IEnumerable<T> GetPlayerStates<T>() where T : LobbyPlayerState
        {
            return base.GetPlayerStates<T>();
        }

        /// <summary>
        ///     Returns an enumeration containing all player states.
        /// </summary>
        public new IEnumerable<LobbyPlayerState> GetPlayerStates()
        {
            return GetPlayerStates<LobbyPlayerState>();
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <typeparam name="T">A derived type of <see cref="LobbyPlayerState" /> to cast the player state to.</typeparam>
        public new T GetPlayerState<T>(int clientId) where T : LobbyPlayerState
        {
            return base.GetPlayerState<T>(clientId);
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <returns></returns>
        public new PlayerStateBase GetPlayerState(int clientId)
        {
            return GetPlayerState<LobbyPlayerState>(clientId);
        }

#region Public API

        public Guid LobbyId => _lobbyId.Value;

        public ILobbyInfo LobbyInfo => GlobalGameState.Instance.GetLobbyInfo(LobbyId);

#endregion

#region Singleton

        private static LobbyState _instance;

        private void Awake()
        {
            _instance = this;
        }

        [Client]
        public static LobbyState GetInstance()
        {
            return _instance;
        }

#endregion
    }
}
