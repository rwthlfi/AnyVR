using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public class LobbyHandler : NetworkBehaviour
    {
        internal LobbyState State;

        internal void Init(GlobalLobbyState state)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Initializing LobbyHandler: {state.LobbyId}");
            Assert.IsFalse(state.LobbyId == Guid.Empty);

            SpawnLobbyState(state);

            SceneManager.OnClientPresenceChangeEnd += args =>
            {
                if (args.Added && args.Scene == gameObject.scene)
                {
                    SpawnPlayerController(args.Connection);
                }
            };

            DateTime? expiration = state.ExpirationDate.Value;
            if (expiration.HasValue)
            {
                StartCoroutine(ExpireLobby(expiration.Value));
            }
        }
        private void SpawnPlayerController(NetworkConnection conn)
        {
            LobbyPlayerController playerController = Instantiate(_playerControllerPrefab);

            bool res = _playerControllers.TryAdd(conn, playerController);
            Assert.IsTrue(res);

            Spawn(playerController.NetworkObject, conn, gameObject.scene);
        }

        private void SpawnLobbyState(GlobalLobbyState globalState)
        {
            State = Instantiate(_lobbyStatePrefab);
            Spawn(State.NetworkObject, null, gameObject.scene);
            State.Init(globalState);

            State.OnPlayerJoin += _ =>
            {
                if (_inactiveCoroutine != null)
                {
                    StopCoroutine(_inactiveCoroutine);
                }
            };

            State.OnPlayerLeave += _ =>
            {
                if (!State.GetPlayerStates().Any())
                {
                    _inactiveCoroutine = StartCoroutine(CloseInactiveLobby());
                }
            };
        }

        private IEnumerator ExpireLobby(DateTime expirationDate)
        {
            float timeUntilExpiration = (float)(expirationDate - DateTime.UtcNow).TotalSeconds;

            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Expire lobby {State.Global.ExpirationDate.Value} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Lobby {State.LobbyId} expired");
            LobbyManager.Instance.Internal.Server_CloseLobby(State.LobbyId);
        }

        /// <summary>
        ///     Checks if the lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="duration">Duration in seconds until expiration</param>
        private IEnumerator CloseInactiveLobby(ushort duration = 10)
        {
            if (State.GetPlayerStates().Any())
            {
                yield break;
            }

            yield return new WaitForSeconds(duration);

            if (State.GetPlayerStates().Any())
            {
                yield break;
            }

            Logger.Log(LogLevel.Warning, nameof(LobbyHandler), $"Closing lobby {State.LobbyId} due to inactivity.");
            LobbyManager.Instance.Internal.Server_CloseLobby(State.LobbyId);
        }

#region Serialized Fields

        [SerializeField] private LobbyState _lobbyStatePrefab;

        [SerializeField] private LobbyPlayerController _playerControllerPrefab;

#endregion

#region Private Fields

        private Coroutine _expirationCoroutine;

        private Coroutine _inactiveCoroutine;

        private LobbySceneService _sceneService;

        private readonly Dictionary<NetworkConnection, LobbyPlayerController> _playerControllers = new();

#endregion
    }
}
