using System;
using System.Collections;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.TextChat;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyHandler
    {
#region RPCs

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_LeaveLobby(NetworkConnection conn)
        {
            Assert.IsNotNull(GetPlayerState(conn.ClientId));
            LobbyManager.Instance.Internal.RemovePlayerFromLobby(conn, this);
        }

#endregion

        [Server]
        internal void Init(LobbyState state)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Initializing LobbyHandler: {state.LobbyId}");
            Assert.IsFalse(state.LobbyId == Guid.Empty);

            _lobbyId.Value = state.LobbyId;
            _quickConnectCode.Value = state.QuickConnectCode;

            DateTime? expiration = state.ExpirationDate.Value;
            if (expiration.HasValue)
            {
                StartCoroutine(ExpireLobby(expiration.Value));
            }
        }

        [Server]
        public override void OnSpawnServer(NetworkConnection conn)
        {
            base.OnSpawnServer(conn);

            OnPlayerJoin += _ =>
            {
                State.SetPlayerNum((ushort)GetPlayerStates().Count());
            };
            OnPlayerLeave += _ =>
            {
                State.SetPlayerNum((ushort)GetPlayerStates().Count());
                StartCoroutine(CloseInactiveLobby());
            };

            AddPlayerState(conn);
        }

        [Server]
        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            RemovePlayerState(conn);
        }

        [Server]
        private IEnumerator ExpireLobby(DateTime expirationDate)
        {
            float timeUntilExpiration = (float)(expirationDate - DateTime.UtcNow).TotalSeconds;

            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Expire lobby {_lobbyId.Value} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Lobby {_lobbyId.Value} expired");
            LobbyManager.Instance.Internal.Server_CloseLobby(_lobbyId.Value);
        }

        /// <summary>
        ///     Checks if the lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="duration">Duration in seconds until expiration</param>
        [Server]
        private IEnumerator CloseInactiveLobby(ushort duration = 10)
        {
            if (GetPlayerStates().Any())
            {
                yield break;
            }

            float elapsed = 0;
            const float interval = 1;

            while (elapsed < duration)
            {
                Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Closing lobby {_lobbyId.Value} in {duration - elapsed} seconds due to inactivity.");
                if (GetPlayerStates().Any())
                {
                    Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Cancel inactive lobby closing. Lobby {_lobbyId.Value} is no longer inactive.");
                    yield break;
                }

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            Logger.Log(LogLevel.Warning, nameof(LobbyHandler), $"Closing lobby {_lobbyId.Value} due to inactivity.");
            LobbyManager.Instance.Internal.Server_CloseLobby(_lobbyId.Value);
        }

#region Private Fields

        private Coroutine _expirationCoroutine;

        private LobbySceneService _sceneService;

#endregion
    }
}
