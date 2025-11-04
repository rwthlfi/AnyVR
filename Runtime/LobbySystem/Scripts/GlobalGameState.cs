using AnyVR.LobbySystem.Internal;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the global game state that is replicated to all clients.
    ///     Maintains a synchronized collection of <see cref="GlobalPlayerState" /> instances indexed by client ID.
    ///     Inherit from this class to add additional synchronized properties as needed.
    ///     <seealso cref="LobbyState" />
    /// </summary>
    public class GlobalGameState : BaseGameState<GlobalPlayerState>
    {
        private void Awake()
        {
            InitSingleton();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            GlobalPlayerState ps = Instantiate(PlayerStatePrefab).GetComponent<GlobalPlayerState>();
            Spawn(ps.gameObject, conn, gameObject.scene);
            AddPlayerState(ps);
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                Despawn(RemovePlayerState(conn).NetworkObject, DespawnType.Destroy);
            }
        }

#region Singleton

        /// <summary>
        ///     The singleton instance of the <see cref="GlobalGameState" />.
        ///     Is not null if the local client is connected to a server.
        /// </summary>
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
