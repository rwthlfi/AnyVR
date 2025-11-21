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

#region Serialized Fields

        [SerializeField] [Scene]
        private string _globalScene = "Packages/rwth.lfi.anyvr/Runtime/LobbySystem/Scenes/GlobalScene.unity";

        public string GlobalScene => _globalScene;

#endregion

#region Private Fields

        private readonly RpcAwaiter<ConnectionStatus> _connectionAwaiter = new(ConnectionStatus.Timeout, ConnectionStatus.Cancelled);

        private NetworkManager _networkManager;

        private TimeManager _tm;

        private bool _isGlobalSceneLoaded;

#endregion

#region Public Fields

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

        /// <summary>
        ///     The uri of the (LiveKit) tokenserver.
        /// </summary>
        public Uri LiveKitTokenServer { get; private set; }

        /// <summary>
        ///     The uri of the LiveKit server.
        /// </summary>
        public Uri LiveKitVoiceServer { get; private set; }

        /// <summary>
        ///     The host and port of the Fishnet server.
        /// </summary>
        public (string Host, ushort Port) FishnetServer { get; private set; }

        /// <summary>
        ///     Called when the local client connection to the server has timed out.
        /// </summary>
        public event Action OnClientTimeout;

        /// <summary>
        ///     Called after the local client connection state changes.
        /// </summary>
        public event Action<ConnectionState> OnClientConnectionState;

        /// <summary>
        ///     If <c>true</c>/<c>false</c>, use <c>https</c>/<c>http</c> and <c>wss</c>/<c>ws</c> internally.
        ///     This is set when connecting to the server in <see cref="ConnectToServer" />.
        /// </summary>
        public bool UseSecureProtocol { get; private set; }

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
#if UNITY_WEBGL
            mp.SetClientTransport<Bayou>();
#else
            mp.SetClientTransport<Tugboat>();
#endif

#if UNITY_EDITOR && UNITY_SERVER
            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), "Starting server ...");
            _networkManager.ServerManager.StartConnection();
#endif
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                _connectionAwaiter.Complete(ConnectionStatus.Connected);
            }

            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                SceneManager.UnloadSceneAsync(SceneManager.GetSceneByPath(_globalScene));
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

        //TODO: Hide token server from user. Connect using fishnet address instead.
        /// <summary>
        ///     Starts a connection to a server.
        ///     After awaiting the asynchronous result, the active <see cref="GlobalGameState" /> is replicated and safe to use.
        ///     If the passed username is null, whitespace or already taken, the server will kick the local player immediately.
        /// </summary>
        /// <param name="tokenServerUri">
        ///     The uri of the token server. The uri of the fishnet server is fetched from the token
        ///     server internally.
        /// </param>
        /// <param name="userName">The desired username.</param>
        /// <param name="timeout">Optionally, specify a timeout. If <c>null</c> the timeout defaults to 5 seconds.</param>
        /// <returns>An asynchronous <see cref="ConnectionResult" /> indicating whether the connection attempt succeeded or failed.</returns>
        public async Task<ConnectionResult> ConnectToServer(Uri tokenServerUri, string userName, TimeSpan? timeout = null)
        {
            Assert.IsNotNull(_networkManager);

            if (State.HasFlag(ConnectionState.Client))
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), "Could not connect to server. Already connected as a client");
                return new ConnectionResult(ConnectionStatus.AlreadyConnected, null);
            }

            LiveKitTokenServer = tokenServerUri;
            ((string host, ushort port), Uri liveKitVoiceServer)? res = await FetchServerAddresses(tokenServerUri);

            if (!res.HasValue)
            {
                return new ConnectionResult(ConnectionStatus.ServerIpRequestFailed, null);
            }
            FishnetServer = res.Value.Item1;
            LiveKitVoiceServer = res.Value.Item2;

            Multipass m = (Multipass)_networkManager.TransportManager.Transport;

            m.SetClientAddress(FishnetServer.Host);
            m.SetPort(FishnetServer.Port);
            m.GetTransport<Bayou>().SetUseWSS(UseSecureProtocol);

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

        private async Task<((string host, ushort port), Uri liveKitVoiceServer)?> FetchServerAddresses(Uri tokenServerUri)
        {
            UseSecureProtocol = tokenServerUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

            Uri requestUri = new(tokenServerUri, "requestServerIp");
            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Requesting server ip from '{requestUri}'");

            ServerAddressResponse response = await WebRequestHandler.GetAsync<ServerAddressResponse>(requestUri.ToString());
            if (response.Success)
            {
                Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Received server ip: {{FishNet: {response.fishnet_server_address}, LiveKit: {response.livekit_server_address}}}");
            }
            else
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), response.Error);
                return null;
            }

            Uri liveKitVoiceServer = new($"{(UseSecureProtocol ? Uri.UriSchemeHttps : Uri.UriSchemeHttp)}://{response.livekit_server_address}");

            string[] parts = response.fishnet_server_address.Split(':');
            if (parts.Length != 2)
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), $"Invalid FishNet server address: '{response.fishnet_server_address}'");
                return null;
            }


            string host = parts[0];
            if (ushort.TryParse(parts[1], out ushort port))
                return ((host, port), liveKitVoiceServer);

            Logger.Log(LogLevel.Warning, nameof(ConnectionManager), $"Invalid port in FishNet server address: '{response.fishnet_server_address}'");
            return null;
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
