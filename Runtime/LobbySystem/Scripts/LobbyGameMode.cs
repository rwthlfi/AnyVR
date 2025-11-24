using System;
using System.Collections;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Override this class to implement server-side gameplay logic of a lobby.
    ///     Each lobby can be configured with its own custom LobbyGameMode.
    ///     Manages the lobby lifecycle, closing inactive lobbies, and automatically expiring lobbies at a set time.
    ///     <seealso cref="GameModeBase" />
    /// </summary>
    public class LobbyGameMode : GameModeBase
    {
        private readonly SyncVar<Guid> _lobbyId = new();

        internal ILobbyInfo LobbyInfo => GlobalGameState.Instance.GetLobbyInfo(_lobbyId.Value);

        internal void SetLobbyId(Guid lobbyId)
        {
            Assert.IsFalse(lobbyId == Guid.Empty);
            _lobbyId.Value = lobbyId;
            GetGameState<LobbyState>().SetLobbyId(lobbyId);
        }

        public virtual void OnBeginPlay()
        {
            GetGameState().OnPlayerJoin += _ =>
            {
                if (_inactiveCoroutine != null)
                {
                    StopCoroutine(_inactiveCoroutine);
                }
            };

            GetGameState().OnPlayerLeave += _ =>
            {
                if (!GetGameState().GetPlayerStates().Any())
                {
                    _inactiveCoroutine = StartCoroutine(CloseInactiveLobby());
                }
            };

            DateTime? expiration = LobbyInfo.ExpirationDate.Value;
            if (expiration.HasValue)
            {
                StartCoroutine(ExpireLobby(expiration.Value));
            }
        }

        protected override PlayerStateBase SpawnPlayerState(NetworkConnection conn)
        {
            // We don't call base.SpawnPlayerState() because we want to set the lobby_id and the is_admin property before spawning.
            // This way these properties will be replicated in the OnBeginClient method.
            LobbyPlayerState ps = Instantiate(_playerStatePrefab).GetComponent<LobbyPlayerState>();

            ps.SetPlayerId(conn.ClientId);
            ps.SetLobbyId(GetGameState<LobbyState>().LobbyId);
            ps.SetIsAdmin(GetGameState<LobbyState>().LobbyInfo.CreatorId == conn.ClientId);

            Spawn(ps.gameObject, null, gameObject.scene);

            GetGameState().AddPlayerState(ps);
            return ps;
        }

        private IEnumerator ExpireLobby(DateTime expirationDate)
        {
            float timeUntilExpiration = (float)(expirationDate - DateTime.UtcNow).TotalSeconds;

            Logger.Log(LogLevel.Verbose, nameof(LobbyGameMode), $"Expire lobby {GetGameState<LobbyState>().LobbyInfo.ExpirationDate.Value} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.Log(LogLevel.Info, nameof(LobbyGameMode), $"Lobby {GetGameState<LobbyState>().LobbyId} expired");
            LobbyManagerInternal.Instance.Server_CloseLobby(GetGameState<LobbyState>().LobbyId);
        }

        /// <summary>
        ///     Checks if the lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="duration">Duration in seconds until expiration</param>
        private IEnumerator CloseInactiveLobby(ushort duration = 10)
        {
            if (GetGameState().GetPlayerStates().Any())
            {
                yield break;
            }

            yield return new WaitForSeconds(duration);

            if (GetGameState().GetPlayerStates().Any())
            {
                yield break;
            }

            Logger.Log(LogLevel.Info, nameof(LobbyGameMode), $"Closing lobby {GetGameState<LobbyState>().LobbyId} due to inactivity.");
            LobbyManagerInternal.Instance.Server_CloseLobby(GetGameState<LobbyState>().LobbyId);
        }

#region Private Fields

        private Coroutine _expirationCoroutine;

        private Coroutine _inactiveCoroutine;

        private LobbySceneService _sceneService;

#endregion
    }
}
