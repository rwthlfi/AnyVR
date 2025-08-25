using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AnyVR.Logging;
using AnyVR.Voicechat;
using AnyVR.WebRequests;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Managing.Timing;
using FishNet.Transporting;
using FishNet.Transporting.Bayou;
using FishNet.Transporting.Multipass;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AnyVR.LobbySystem
{
    [RequireComponent(typeof(NetworkManager))]
    public class ConnectionManager : MonoBehaviour
    {
        private const string Tag = nameof(ConnectionManager);

        [SerializeField] [Scene] private string _welcomeScene;

        private NetworkManager _networkManager;
        private TimeManager _tm;
        private string _tokenServerAddress;
        public static bool IsInitialized { get; private set; }

        public bool UseSecureProtocol { get; set; } = true;
        
        private const string GlobalScene = "Packages/rwth.lfi.anyvr/Runtime/LobbySystem/Scenes/GlobalScene.unity";

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
            _networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            _networkManager.ClientManager.OnClientTimeOut += ClientManager_OnClientTimeout;
            _networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;
            _networkManager.SceneManager.OnLoadStart += SceneManager_OnLoadStart;
            
            // The WelcomeScene gets only unloaded for clients
            LobbySceneLoadStart += asServer =>
            {
                if (!asServer)
                {
                    SceneManager.UnloadSceneAsync(_welcomeScene);
                }
            };
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (_networkManager == null)
            {
                return;
            }

            if (!_networkManager.Initialized)
            {
                return;
            }

            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            _networkManager.ClientManager.OnClientTimeOut -= ClientManager_OnClientTimeout;
            _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
            _networkManager.SceneManager.OnLoadStart -= SceneManager_OnLoadStart;
        }

        private void ClientManager_OnClientTimeout()
        {
            OnClientTimeout?.Invoke();
        }

        private static void ServerManager_OnRemoteConnectionState(NetworkConnection conn,
            RemoteConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case RemoteConnectionState.Stopped:
                    Logger.Log(LogLevel.Verbose, Tag, $"Remote connection {conn} stopped.");
                    break;
                case RemoteConnectionState.Started:
                    Logger.Log(LogLevel.Verbose, Tag, $"Remote connection {conn} started.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StartServer()
        {
            if (State.HasFlag(ConnectionState.Server))
            {
                Logger.Log(LogLevel.Warning, Tag, "The server is already started");
                return;
            }

            Logger.Log(LogLevel.Verbose, Tag, "Starting server...");
            _networkManager.ServerManager.StartConnection();
        }

        public void LeaveServer()
        {
            if (!State.HasFlag(ConnectionState.Client))
            {
                Logger.Log(LogLevel.Warning, Tag, "Not connected to a server");
                return;
            }

            Logger.Log(LogLevel.Verbose, Tag, "Stopping server.");
            _networkManager.ClientManager.StopConnection();
            VoiceChatManager.GetInstance()?.Disconnect();
        }

        public bool ConnectToServer(ServerAddressResponse addressResponse, string userName, out string errorMessage)
        {
            Assert.IsNotNull(_networkManager);

            if (State.HasFlag(ConnectionState.Client))
            {
                errorMessage = "Could not connect to server. Already connected as a client";
                return false;
            }

            if (!TryParseAddress(addressResponse.fishnet_server_address, out (string host, ushort port) fishnetAddress))
            {
                errorMessage = $"Could not connect to server. Failed to parse FishNet server address ('{addressResponse.fishnet_server_address}').";
                return false;
            }

            if (!TryParseAddress(addressResponse.livekit_server_address, out (string host, ushort port) _))
            {
                errorMessage = $"Could not connect to server. Failed to parse LiveKit server address ('{addressResponse.livekit_server_address}').";
                return false;
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Connecting to server (host: {fishnetAddress.host}, port: {fishnetAddress.port}) ...");
            UserName = userName;
            if (_networkManager.TransportManager.Transport is Multipass m)
            {
                m.SetClientAddress(fishnetAddress.host);
                m.SetPort(fishnetAddress.port);
                m.GetTransport<Bayou>().SetUseWSS(UseSecureProtocol);
            }
            else
            {
                Logger.Log(LogLevel.Error, Tag, "Transport should be Multipass");
            }

            _networkManager.ClientManager.StartConnection();

            VoiceChatManager.GetInstance()?.SetLiveKitServerAddress(addressResponse.livekit_server_address);
            VoiceChatManager.GetInstance()?.SetTokenServerAddress(_tokenServerAddress);

            errorMessage = null;
            return true;
        }

        private static bool TryParseAddress(string address, out (string, ushort) res)
        {
            res = (null, 0);

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


        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs state)
        {
            OnClientConnectionState?.Invoke(State);
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            // if (state.ConnectionState is LocalConnectionState.Started or LocalConnectionState.Stopped)
            // {
            //     ConnectionState?.Invoke(State);
            // }

            // The OnServerConnectionState will be called for each transport (Tugboat & Bayou). 
            // But we only want to load the global scene once
            if (_networkManager.ServerManager.IsOnlyOneServerStarted())
            {
                return;
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Server is {state.ConnectionState.ToString()}");
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Server started on port: {_networkManager.TransportManager.Transport.GetPort()}");


            string scene = Path.GetFileNameWithoutExtension(GlobalScene);
            SceneLoadData sld = new(scene)
            {
                Params =
                {
                    ServerParams = new[]
                    {
                        (object)SceneLoadParam.Global
                    },
                    ClientParams = LobbyMetaData.SerializeObjects(new[]
                    {
                        (object)SceneLoadParam.Global
                    })
                }
            };

            Logger.Log(LogLevel.Verbose, Tag, "Loading global scene...");
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }

        [CanBeNull]
        private static SceneLoadParam? GetSceneLoadParam(LoadQueueData queueData)
        {
            SceneLoadParam param;
            if (queueData.AsServer)
            {
                object[] serverParams = queueData.SceneLoadData.Params.ServerParams;
                if (serverParams.Length < 1 || serverParams[0] is not SceneLoadParam)
                {
                    return null;
                }

                param = (SceneLoadParam)serverParams[0];
            }
            else
            {
                byte[] arr = queueData.SceneLoadData.Params.ClientParams;
                object[] clientParams = LobbyMetaData.DeserializeClientParams(arr);

                if (clientParams.Length < 1)
                {
                    return null;
                }

                param = (SceneLoadParam)clientParams[0];
            }

            return param;
        }

        private void SceneManager_OnLoadStart(SceneLoadStartEventArgs args)
        {
            SceneLoadParam? param = GetSceneLoadParam(args.QueueData);
            if (param == null)
            {
                return;
            }
            
            switch (param.Value)
            {
                case SceneLoadParam.Global:
                    break;
                case SceneLoadParam.Lobby:
                    LobbySceneLoadStart?.Invoke(State.HasFlag(ConnectionState.Server));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs args)
        {
            SceneLoadParam? param = GetSceneLoadParam(args.QueueData);
            if (param == null)
            {
                Logger.Log(LogLevel.Error, Tag, "SceneLoadParam is null");
                return;
            }

            switch (param.Value)
            {
                case SceneLoadParam.Global:
                    Logger.Log(LogLevel.Verbose, Tag, "Global scene loaded.");
                    OnGlobalSceneLoaded?.Invoke(args.QueueData.AsServer);
                    break;
                case SceneLoadParam.Lobby:
                    Logger.Log(LogLevel.Verbose, Tag, "Lobby scene loaded.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StopServer()
        {
            _networkManager.ServerManager.StopConnection(false);
        }

        public async Task<ServerAddressResponse> RequestServerIp(int timeoutSeconds = 10)
        {
            const string tokenURL = "{0}://{1}/requestServerIp";
            string url = string.Format(tokenURL, UseSecureProtocol ? "https" : "http", _tokenServerAddress);

            Logger.Log(LogLevel.Verbose, Tag, $"Requesting server ip from '{url}'");
            ServerAddressResponse res = await WebRequestHandler.GetAsync<ServerAddressResponse>(url, timeoutSeconds);

            if (res.Success)
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Received server ip: {{FishNet: {res.fishnet_server_address}, LiveKit: {res.livekit_server_address}}}");
            }
            else
            {
                Logger.Log(LogLevel.Warning, Tag, res.Error);
            }
            return res;
        }

        public void SetTokenServerIp(string tokenServerAddress)
        {
            _tokenServerAddress = tokenServerAddress;
        }

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

        /// <summary>
        ///     The username of the local client
        /// </summary>
        public static string UserName { get; private set; }

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

        public delegate void ConnectionStateEvent(ConnectionState state);

        public ConnectionStateEvent OnClientConnectionState;

        public delegate void GlobalSceneLoadedEvent(bool asServer);

        public GlobalSceneLoadedEvent OnGlobalSceneLoaded;

        public delegate void LobbySceneLoadedEvent(bool asServer);

        public LobbySceneLoadedEvent LobbySceneLoadStart;

        public event Action OnClientTimeout;

        [CanBeNull]
        public static ConnectionManager GetInstance()
        {
            return _instance;
        }

#endregion

#region Singleton

        private static ConnectionManager _instance;

        private void InitSingleton()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            _instance = this;
        }

#endregion
    }
}
