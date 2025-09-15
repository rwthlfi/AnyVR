using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public class GlobalGameState : GameState
    {
        public override void OnStartServer()
        {
            base.OnStartServer();

            Assert.IsNotNull(_playerStatePrefab);
            Assert.IsNotNull(_playerStatePrefab.GetComponent<PlayerState>());

            SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            AddPlayerState(conn, true);
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                RemovePlayerState(conn);
            }
        }

#region Singleton

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
