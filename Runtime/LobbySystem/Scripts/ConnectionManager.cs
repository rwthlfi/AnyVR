using System;
using System.Threading.Tasks;
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

namespace AnyVR.LobbySystem
{
    [RequireComponent(typeof(NetworkManager))]
    public class ConnectionManager : MonoBehaviour
    {
#region Serialized Fields

        [SerializeField] [Scene] private string _welcomeScene;

#endregion

        private async Task<ServerAddressResponse> RequestServerIp(Uri tokenServerUri, int timeoutSeconds = 10)
        {
            const string tokenURL = "{0}://{1}/requestServerIp";
            string url = string.Format(tokenURL, UseSecureProtocol ? "https" : "http", tokenServerUri);

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Requesting server ip from '{url}'");
            ServerAddressResponse res = await WebRequestHandler.GetAsync<ServerAddressResponse>(url, timeoutSeconds);

            return res;
        }

        private static async Task WaitUntilGameStateInitialized()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            DateTime start = DateTime.UtcNow;
            while (GlobalGameState.Instance == null || GlobalPlayerState.LocalPlayer == null)
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    return;
                }

                await Task.Delay(10);
            }
        }

#region Private Fields

        private readonly RpcAwaiter<ConnectionStatus> _connectionAwaiter = new(ConnectionStatus.Timeout, ConnectionStatus.Cancelled);

        private NetworkManager _networkManager;

        private TimeManager _tm;

#endregion

#region Public Fields

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


        public uint Latency
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

        public Uri LiveKitTokenServer { get; private set; }

        public Uri LiveKitVoiceServer { get; private set; }

        public Uri FishnetServer { get; private set; }

        public event Action OnClientTimeout;

        public event Action<ConnectionState> OnClientConnectionState;

        public bool UseSecureProtocol { get; set; } = true;

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

#if UNITY_EDITOR && UNITY_SERVER
            Debug.Log("starting server");
            _networkManager.ServerManager.StartConnection();
#endif
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                _connectionAwaiter.Complete(ConnectionStatus.Connected);
            }

            OnClientConnectionState?.Invoke(State);
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            // The OnServerConnectionState will be called for each transport (Tugboat & Bayou). 
            if (_networkManager.ServerManager.IsOnlyOneServerStarted())
            {
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Server is {state.ConnectionState.ToString()}");
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Server started on port: {_networkManager.TransportManager.Transport.GetPort()}");

            SceneLoadData sld = LobbySceneService.GlobalSceneLoadData();

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), "Loading global scene...");
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }

#endregion

#region Public API

        public async Task<ConnectionResult> ConnectToServer(Uri tokenServerUri, string userName, TimeSpan? timeout = null)
        {
            Assert.IsNotNull(_networkManager);

            if (State.HasFlag(ConnectionState.Client))
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), "Could not connect to server. Already connected as a client");
                return new ConnectionResult(ConnectionStatus.AlreadyConnected, null);
            }

            ServerAddressResponse response = await RequestServerIp(tokenServerUri);
            if (response.Success)
            {
                Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Received server ip: {{FishNet: {response.fishnet_server_address}, LiveKit: {response.livekit_server_address}}}");
            }
            else
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), response.Error);
                return new ConnectionResult(ConnectionStatus.ServerIpRequestFailed, null);
            }

            LiveKitTokenServer = tokenServerUri;
            LiveKitVoiceServer = new Uri(response.livekit_server_address);
            FishnetServer = new Uri(response.fishnet_server_address);

            Assert.IsTrue(_networkManager.TransportManager.Transport is Multipass);
            Multipass m = (Multipass)_networkManager.TransportManager.Transport;

            m.SetClientAddress(FishnetServer.Host);
            m.SetPort((ushort)FishnetServer.Port);
            m.GetTransport<Bayou>().SetUseWSS(UseSecureProtocol);

            Task<ConnectionStatus> task = _connectionAwaiter.WaitForResult(timeout);
            _networkManager.ClientManager.StartConnection();
            ConnectionStatus connectionStatus = await task;

            if (connectionStatus != ConnectionStatus.Connected)
                return new ConnectionResult(connectionStatus, null);

            await WaitUntilGameStateInitialized();

            Assert.IsNotNull(GlobalGameState.Instance);
            Assert.IsNotNull(GlobalPlayerState.LocalPlayer);

            // The server will kick the player if the name is already taken.
            PlayerNameUpdateResult nameResult = await GlobalPlayerState.LocalPlayer.SetName(userName);

            return new ConnectionResult(connectionStatus, nameResult);
        }

        public void LeaveServer()
        {
            if (!State.HasFlag(ConnectionState.Client))
            {
                Logger.Log(LogLevel.Warning, nameof(ConnectionManager), "Not connected to a server");
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), "Stopping server.");
            _networkManager.ClientManager.StopConnection();
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
