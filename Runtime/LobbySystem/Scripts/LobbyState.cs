using System;
using System.Collections.Generic;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public class LobbyState : GameStateBase
    {
#region Internal Methods

        [Server]
        internal void SetLobbyId(Guid lobbyId)
        {
            Assert.IsTrue(lobbyId != Guid.Empty);
            _lobbyId.Value = lobbyId;
        }

#endregion

#region Livecycle

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

            string url = Environment.GetEnvironmentVariable("LIVEKIT_SERVER_URL");
            if (string.IsNullOrWhiteSpace(url))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyState), "The environmental variable 'LIVEKIT_SERVER_URL' is not set or white space.");
            }
            _liveKitServerUrl.Value = url;
        }

#endregion

#region Replicated Properties

        private readonly SyncVar<Guid> _lobbyId = new();

        private readonly SyncVar<string> _liveKitServerUrl = new();

#endregion

#region Public API

        public Guid LobbyId => _lobbyId.Value;

        public ILobbyInfo LobbyInfo => GlobalGameState.Instance.GetLobbyInfo(LobbyId);

        public string LiveKitServerUrl => _liveKitServerUrl.Value;

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
