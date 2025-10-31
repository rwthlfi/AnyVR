using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    public class LobbyHandler : NetworkBehaviour
    {
        private LobbyState _state;

        internal void Init(GlobalLobbyState state)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Initializing LobbyHandler: {state.LobbyId}");
            Assert.IsFalse(state.LobbyId == Guid.Empty);

            SpawnLobbyState(state);

            SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;

            DateTime? expiration = state.ExpirationDate.Value;
            if (expiration.HasValue)
            {
                StartCoroutine(ExpireLobby(expiration.Value));
            }
        }

        private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs args)
        {
            if (args.Scene != gameObject.scene)
                return;

            if (args.Added)
            {
                SpawnPlayerState(args.Connection);
                SpawnPlayerController(args.Connection);
            }
            else
            {
                Assert.IsNotNull(GetGameState().GetPlayerState(args.Connection.ClientId));

                // Despawn player state
                LobbyPlayerState playerState = GetGameState().RemovePlayerState(args.Connection);
                Despawn(playerState.NetworkObject, DespawnType.Destroy);

                // Despawn player controller
                bool success = _playerControllers.TryGetValue(args.Connection, out LobbyPlayerController playerController);
                Assert.IsTrue(success);
                Despawn(playerController.NetworkObject, DespawnType.Destroy);
            }
        }

        private void SpawnPlayerState(NetworkConnection conn)
        {
            LobbyPlayerState ps = Instantiate(GetGameState().PlayerStatePrefab).GetComponent<LobbyPlayerState>();
            Spawn(ps.gameObject, conn, gameObject.scene);
            GetGameState().AddPlayerState(ps);
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
            _state = Instantiate(_lobbyStatePrefab);
            Spawn(_state.NetworkObject, null, gameObject.scene);
            _state.Init(globalState);

            _state.OnPlayerJoin += _ =>
            {
                if (_inactiveCoroutine != null)
                {
                    StopCoroutine(_inactiveCoroutine);
                }
            };

            _state.OnPlayerLeave += _ =>
            {
                if (!_state.GetPlayerStates().Any())
                {
                    _inactiveCoroutine = StartCoroutine(CloseInactiveLobby());
                }
            };
        }

        private IEnumerator ExpireLobby(DateTime expirationDate)
        {
            float timeUntilExpiration = (float)(expirationDate - DateTime.UtcNow).TotalSeconds;

            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Expire lobby {_state.LobbyInfo.ExpirationDate.Value} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyHandler), $"Lobby {_state.LobbyId} expired");
            LobbyManager.Instance.Internal.Server_CloseLobby(_state.LobbyId);
        }

        /// <summary>
        ///     Checks if the lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="duration">Duration in seconds until expiration</param>
        private IEnumerator CloseInactiveLobby(ushort duration = 10)
        {
            if (_state.GetPlayerStates().Any())
            {
                yield break;
            }

            yield return new WaitForSeconds(duration);

            if (_state.GetPlayerStates().Any())
            {
                yield break;
            }

            Logger.Log(LogLevel.Warning, nameof(LobbyHandler), $"Closing lobby {_state.LobbyId} due to inactivity.");
            LobbyManager.Instance.Internal.Server_CloseLobby(_state.LobbyId);
        }

        public LobbyState GetGameState()
        {
            return _state;
        }

        public T GetGameState<T>() where T : LobbyState
        {
            return _state as T;
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
