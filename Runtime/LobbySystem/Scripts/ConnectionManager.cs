using System;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Managing.Timing;
using FishNet.Transporting;
using FishNet.Transporting.Bayou;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Responsible for managing the network connection for the client and server.
    ///     On the client side, it sets up the transportation layer (<c>Tugboat</c> or <c>Bayou</c>) and is primarily used to
    ///     start and stop a connection to a server.
    ///     On the server, it is responsible for loading the global scene after startup.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class ConnectionManager : MonoBehaviour
    {
#region Serialized Fields

        [SerializeField] [Scene]
        private string _globalScene = "Packages/rwth.lfi.anyvr/Runtime/LobbySystem/Scenes/GlobalScene.unity";

#endregion

        private static async Task WaitUntilClientStarted()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            DateTime start = DateTime.UtcNow;
            while (GlobalPlayerController.Instance == null || !GlobalPlayerController.Instance.IsClientStarted ||
                   GlobalPlayerState.LocalPlayer == null || !GlobalPlayerState.LocalPlayer.IsClientStarted)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    return;
                }

                await Task.Yield();
            }
        }

#region Private Fields

        private readonly RpcAwaiter<ConnectionStatus> _connectionAwaiter = new(ConnectionStatus.Timeout, ConnectionStatus.Cancelled);

        private NetworkManager _networkManager;

        private TimeManager _tm;

        private bool _isGlobalSceneLoaded;

#endregion

#region Public API

        /// <summary>
        ///     Current connection state
        /// </summary>
        public ConnectionState State
        {
            get
            {
                if (_networkManager == null)
                {
                    return ConnectionState.Disconnected;
                }

                ConnectionState state = ConnectionState.Disconnected;
                if (_networkManager.ClientManager.Started)
                {
                    state |= ConnectionState.Client;
                }

                if (_networkManager.ServerManager.Started)
                {
                    state |= ConnectionState.Server;
                }

                return state;
            }
        }


        /// <summary>
        ///     Current round-trip time of the connection in milliseconds.
        /// </summary>
        public uint Ping
        {
            get
            {
                if (_tm == null)
                {
                    return 0;
                }
                long ping = _tm.RoundTripTime;
                long deduction = (long)(_tm.TickDelta * 2000d);
                return (uint)Mathf.Max(1, ping - deduction);
            }
        }

        public string GlobalScene => _globalScene;

        /// <summary>
        ///     Called when the local client connection to the server has timed out.
        /// </summary>
        public event Action OnClientTimeout;

        /// <summary>
        ///     Called after the local client connection state changes.
        /// </summary>
        public event Action<ConnectionState> OnClientConnectionState;

#endregion

#region Lifecycle

        private void Awake()
        {
            InitSingleton();
            _networkManager = GetComponent<NetworkManager>();
            _tm = _networkManager.TimeManager;
            Assert.IsNotNull(_networkManager);
        }

        private void Start()
        {
            Assert.IsTrue(_networkManager.Initialized);

            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            _networkManager.ClientManager.OnClientTimeOut += OnClientTimeout;

            Multipass mp = GetComponent<Multipass>();
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                mp.SetClientTransport<Bayou>();
            }
            else
            {
                mp.SetClientTransport<Tugboat>();
            }

#if UNITY_EDITOR && UNITY_SERVER
            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), "Starting server ...");
            _networkManager.ServerManager.StartConnection();
#endif
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Started:
                    _connectionAwaiter.Complete(ConnectionStatus.Connected);
                    break;
                case LocalConnectionState.Stopped:
                    SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(_globalScene));
                    break;
                case LocalConnectionState.Stopping:
                    break;
                case LocalConnectionState.Starting:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            OnClientConnectionState?.Invoke(State);
        }

        // The ServerManager_OnServerConnectionState will be called for each transport (Tugboat & Bayou). 
        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Server is {state.ConnectionState.ToString()}");
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            if (_isGlobalSceneLoaded)
            {
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Server started on port: {_networkManager.TransportManager.Transport.GetPort()}");

            SceneLoadData sld = LobbySceneService.GlobalSceneLoadData(_globalScene);

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), "Loading global scene...");
            _networkManager.SceneManager.LoadGlobalScenes(sld);
            _isGlobalSceneLoaded = true;
        }

#endregion

#region Public API

        /// <summary>
        ///     Starts a connection to a fishnet server.
        ///     After awaiting the asynchronous result, the active <see cref="GlobalGameState" /> is replicated and safe to use.
        ///     If the passed username is null, whitespace or already taken, the server will kick the local player immediately.
        /// </summary>
        /// <param name="serverUri">
        ///     The uri of the fishnet server.
        /// </param>
        /// <param name="userName">The desired username.</param>
        /// <param name="timeout">Optionally, specify a timeout. If <c>null</c> the timeout defaults to 5 seconds.</param>
        /// <returns>An asynchronous <see cref="ConnectionResult" /> indicating whether the connection attempt succeeded or failed.</returns>
        public async Task<ConnectionResult> ConnectToServer(Uri serverUri, string userName, TimeSpan? timeout = null)
        {
            Assert.IsNotNull(_networkManager);

            if (State.HasFlag(ConnectionState.Client))
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), "Could not connect to server. Already connected as a client");
                return new ConnectionResult(ConnectionStatus.AlreadyConnected, null);
            }

            Multipass m = (Multipass)_networkManager.TransportManager.Transport;

            m.SetClientAddress(serverUri.Host);
            m.SetPort((ushort)serverUri.Port);
            // m.GetTransport<Bayou>().SetUseWSS(UseSecureProtocol);

            Task<ConnectionStatus> task = _connectionAwaiter.WaitForResult(timeout);
            _networkManager.ClientManager.StartConnection();

            ConnectionStatus connectionStatus = await task;

            if (connectionStatus != ConnectionStatus.Connected)
                return new ConnectionResult(connectionStatus, null);

            await WaitUntilClientStarted();

            Assert.IsNotNull(GlobalGameState.Instance);
            Assert.IsNotNull(GlobalPlayerState.LocalPlayer);

            // The server will kick the player if the name is already taken.
            PlayerNameUpdateResult nameResult = await GlobalPlayerController.Instance.SetName(userName);

            return new ConnectionResult(connectionStatus, nameResult);
        }

        /// <summary>
        ///     Stops the connection to the server.
        /// </summary>
        /// <returns><c>False</c>, if not connected to the server. Otherwise, <c>true</c>.</returns>
        public bool LeaveServer()
        {
            if (!State.HasFlag(ConnectionState.Client))
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), "Not connected to a server");
                return false;
            }

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), "Stopping server.");
            return _networkManager.ClientManager.StopConnection();
        }

#endregion

#region Singleton

        public static ConnectionManager Instance { get; private set; }

        private void InitSingleton()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
        }

#endregion
    }
}
