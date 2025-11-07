using System.Collections.Generic;
using AnyVR.LobbySystem.Internal;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public abstract class GameModeBase : NetworkBehaviour
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnGameState();
            SceneManager.OnClientPresenceChangeEnd += OnClientPresenceChangeEnd;
        }

        private void SpawnGameState()
        {
            _gameState = Instantiate(_gameStatePrefab);
            Spawn(_gameState.NetworkObject, null, gameObject.scene);
        }

        private void OnClientPresenceChangeEnd(ClientPresenceChangeEventArgs args)
        {
            if (args.Scene != gameObject.scene)
                return;

            if (args.Added)
            {
                PlayerStateBase ps = SpawnPlayerState(args.Connection);
                SpawnPlayerController(args.Connection, ps);
            }
            else
            {
                Assert.IsNotNull(GetGameState().GetPlayerState(args.Connection.ClientId));

                // Despawn player state
                PlayerStateBase playerState = GetGameState().RemovePlayerState(args.Connection);
                Despawn(playerState.NetworkObject, DespawnType.Destroy);

                // Despawn player controller
                bool success = _playerControllers.TryGetValue(args.Connection, out PlayerController playerController);
                Assert.IsTrue(success);
                Despawn(playerController.NetworkObject, DespawnType.Destroy);
            }
        }


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

        public GameStateBase GetGameState()
        {
            return _gameState;
        }

        public T GetGameState<T>() where T : GameStateBase
        {
            return _gameState as T;
        }

#region Serialized Fields

        [SerializeField] protected GameStateBase _gameStatePrefab;

        [SerializeField] protected PlayerStateBase _playerStatePrefab;

        [SerializeField] protected PlayerController _playerControllerPrefab;

#endregion

#region Private Fields

        private readonly Dictionary<NetworkConnection, PlayerController> _playerControllers = new();

        private GameStateBase _gameState;

#endregion
    }
}
