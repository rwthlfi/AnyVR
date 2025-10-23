using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AnyVR.Logging;
using AnyVR.WebRequests;
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

        private static bool TryParseAddress(string address, out (string, ushort) res)
        {
            res = (null, 0);
            if (address == null)
            {
                return false;
            }

            const string pattern = @"^(?:\[(.+)\]|(.+)):(\d+)$";
            Match m = Regex.Match(address, pattern);
            if (!m.Success)
            {
                return false;
            }

            if (!ushort.TryParse(m.Groups[3].Value, out ushort port))
            {
                return false;
            }

            string ip = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            res = (ip, port);
            return true;
        }


        private async Task<ServerAddressResponse> RequestServerIp(Uri tokenServerUri, int timeoutSeconds = 10)
        {
            const string tokenURL = "{0}://{1}/requestServerIp";
            string url = string.Format(tokenURL, UseSecureProtocol ? "https" : "http", tokenServerUri);

            Logger.Log(LogLevel.Verbose, nameof(ConnectionManager), $"Requesting server ip from '{url}'");
            ServerAddressResponse res = await WebRequestHandler.GetAsync<ServerAddressResponse>(url, timeoutSeconds);

            if (!res.Success)
            {
                return res;
            }

            if (!TryParseAddress(res.fishnet_server_address, out (string host, ushort port) fishnetAddress))
            {
                res.Success = false;
                res.Error = $"Could not connect to server. Failed to parse FishNet server address ('{res.fishnet_server_address}').";
                return res;
            }
            res.FishnetHost = fishnetAddress.host;
            res.FishnetPort = fishnetAddress.port;

            if (!TryParseAddress(res.livekit_server_address, out (string host, ushort port) liveKitAddress))
            {
                res.Success = false;
                res.Error = $"Could not connect to server. Failed to parse LiveKit server address ('{res.livekit_server_address}').";
                return res;
            }
            res.LiveKitHost = liveKitAddress.host;
            res.LiveKitPort = liveKitAddress.port;

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

            Assert.IsTrue(_networkManager.TransportManager.Transport is Multipass);
            Multipass m = (Multipass)_networkManager.TransportManager.Transport;

            m.SetClientAddress(response.FishnetHost);
            m.SetPort(response.FishnetPort);
            m.GetTransport<Bayou>().SetUseWSS(UseSecureProtocol);

            Task<ConnectionStatus> task = _connectionAwaiter.WaitForResult(timeout);
            _networkManager.ClientManager.StartConnection();
            ConnectionStatus connectionStatus = await task;

            if (connectionStatus != ConnectionStatus.Connected)
                return new ConnectionResult(connectionStatus, null);

            await WaitUntilGameStateInitialized();

            Assert.IsNotNull(GlobalGameState.Instance);
            Assert.IsNotNull(GlobalPlayerState.LocalPlayer);

            LiveKitTokenServer = tokenServerUri;
            LiveKitVoiceServer = new Uri(response.livekit_server_address);

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
