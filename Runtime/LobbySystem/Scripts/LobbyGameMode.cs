using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Override this class to implement the server-side gameplay logic for a lobby.
    ///     Each lobby can be configured with its own custom LobbyGameMode.
    ///     Also manages the lifecycles of lobbies, closing inactive lobbies, and automatically expiring them at a set time.
    ///     <seealso cref="GameModeBase" />
    /// </summary>
    public class LobbyGameMode : GameModeBase
    {
        private readonly SyncVar<Guid> _lobbyId = new();

        private LiveKitTokenClient _tokenClient;

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
                if (_inactiveCoroutine == null)
                    return;

                StopCoroutine(_inactiveCoroutine);
                _inactiveCoroutine = null;
            };

            GetGameState().OnPlayerLeave += _ =>
            {
                if (GetGameState().GetPlayerStates().Any())
                    return;

                // Close lobbies due to inactivity if the lobby has no expiration date set.
                if (_expirationCoroutine == null)
                {
                    _inactiveCoroutine = StartCoroutine(CloseInactiveLobby());
                }
            };

            DateTime? expiration = LobbyInfo.ExpirationDate.Value;
            if (expiration.HasValue)
            {
                _expirationCoroutine = StartCoroutine(ExpireLobby(expiration.Value));
            }

            _tokenClient = new LiveKitTokenClient(LobbyInfo.LobbyId.ToString());
        }

        protected override PlayerStateBase SpawnPlayerState(NetworkConnection conn)
        {
            // We don't call base.SpawnPlayerState() because we want to set the lobby_id and the is_admin property before spawning.
            // This way these properties will be replicated in the OnBeginClient method. (Piggybacked with spawn message)
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
        ///     This coroutine is cancelled when a player joins the lobby.
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

        public Task<string> RequestLiveKitToken(LobbyPlayerState player)
        {
            string identity = player.ID.ToString();
            Assert.IsNotNull(identity);

            return _tokenClient.RequestToken(identity);
        }

#region Private Fields

        private Coroutine _expirationCoroutine;

        private Coroutine _inactiveCoroutine;

        private LobbySceneService _sceneService;

#endregion
    }
}
