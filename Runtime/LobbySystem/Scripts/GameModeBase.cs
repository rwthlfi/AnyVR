using System.Collections.Generic;
using AnyVR.LobbySystem.Internal;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Base class for game mode. Is only enabled on the server and is therefore server authoritative.
    ///     Spawns and despawns player states and player controllers on the server.
    ///     Also spawns the configured game state.
    ///     <seealso cref="GlobalGameMode" />
    ///     <seealso cref="LobbyGameMode" />
    /// </summary>
    public abstract class GameModeBase : NetworkBehaviour
    {
        protected virtual PlayerStateBase SpawnPlayerState(NetworkConnection conn)
        {
            PlayerStateBase ps = Instantiate(_playerStatePrefab).GetComponent<PlayerStateBase>();
            Spawn(ps.gameObject, null, gameObject.scene);
            ps.SetPlayerId(conn.ClientId);
            GetGameState().AddPlayerState(ps);
            return ps;
        }

        protected void SpawnPlayerController(NetworkConnection conn, PlayerStateBase playerState)
        {
            PlayerController playerController = Instantiate(_playerControllerPrefab);
            playerController.SetPlayerState(playerState);

            bool res = _playerControllers.TryAdd(conn, playerController);
            Assert.IsTrue(res);

            Spawn(playerController.NetworkObject, conn, gameObject.scene);
        }

#region Scene Instances

        private static readonly Dictionary<Scene, GameModeBase> Instances = new();

        internal static GameModeBase GetInstance(Scene scene)
        {
            return Instances.GetValueOrDefault(scene);
        }

#endregion

#region Serialized Fields

        [SerializeField] protected GameStateBase _gameStatePrefab;

        [SerializeField] protected PlayerStateBase _playerStatePrefab;

        [SerializeField] protected PlayerController _playerControllerPrefab;

#endregion

#region Private Fields

        private readonly Dictionary<NetworkConnection, PlayerController> _playerControllers = new();

        private GameStateBase _gameState;

#endregion

#region Lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();

            Assert.IsFalse(Instances.ContainsKey(gameObject.scene));
            Instances.Add(gameObject.scene, this);

            SpawnGameState();

            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;
        }

        private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs args)
        {
            if (args.Scene != gameObject.scene)
                return;

            if (args.Added)
            {
                OnPlayerLoadScene(args.Connection);
            }
            else
            {
                OnPlayerUnloadScene(args.Connection);
            }
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped)
                return;

            if (_playerControllers.ContainsKey(conn))
            {
                OnPlayerUnloadScene(conn);
            }
        }

        private void SpawnGameState()
        {
            _gameState = Instantiate(_gameStatePrefab);
            Spawn(_gameState.NetworkObject, null, gameObject.scene);
        }

        private void OnPlayerLoadScene(NetworkConnection conn)
        {
            PlayerStateBase ps = SpawnPlayerState(conn);
            SpawnPlayerController(conn, ps);
        }

        private void OnPlayerUnloadScene(NetworkConnection conn)
        {
            Assert.IsNotNull(GetGameState().GetPlayerState(conn.ClientId));

            // Despawn player state
            PlayerStateBase playerState = GetGameState().RemovePlayerState(conn);
            Despawn(playerState.NetworkObject, DespawnType.Destroy);

            // Despawn player controller
            bool success = _playerControllers.Remove(conn, out PlayerController playerController);
            Assert.IsTrue(success);
            Despawn(playerController.NetworkObject, DespawnType.Destroy);
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            SceneManager.OnClientPresenceChangeEnd -= OnClientPresenceChangeEnd;
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

#endregion

#region Public API

        public GameStateBase GetGameState()
        {
            return _gameState;
        }

        public T GetGameState<T>() where T : GameStateBase
        {
            return _gameState as T;
        }

#endregion
    }
}
