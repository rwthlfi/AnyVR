using AnyVr.Voicechat;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AnyVr.LobbySystem
{
    [RequireComponent(typeof(NetworkManager))]
    public class ConnectionManager : MonoBehaviour
    {
        [FormerlySerializedAs("globalScene")]
        [SerializeField] [Scene] private string _globalScene;
        [FormerlySerializedAs("welcomeScene")]
        [SerializeField] [Scene] private string _welcomeScene;

        private bool _isServerInitialized;

        private NetworkManager _networkManager;

        private (string ip, ushort port) _tokenServerAddress;

        public ConnectionState State
        {
            get
            {
                if (_networkManager == null)
                {
                    return LobbySystem.ConnectionState.k_disconnected;
                }

                ConnectionState state = LobbySystem.ConnectionState.k_disconnected;
                if (_networkManager.ClientManager.Started)
                {
                    state |= LobbySystem.ConnectionState.k_client;
                }

                if (_networkManager.ServerManager.Started)
                {
                    state |= LobbySystem.ConnectionState.k_server;
                }

                return state;
            }
        }

        public static string UserName { get; private set; }

        private void Awake()
        {
            InitSingleton();
            _networkManager = GetComponent<NetworkManager>();
            Assert.IsNotNull(_networkManager);
        }

        private void Start()
        {
            if (!_networkManager.Initialized)
            {
                return;
            }

            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            _networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;
            _networkManager.SceneManager.OnLoadStart += SceneManager_OnLoadStart;
            ConnectionState += OnConnectionState;

            // The WelcomeScene gets only unloaded for clients
            LobbySceneLoadStart += asServer =>
            {
                if (!asServer)
                {
                    SceneManager.UnloadSceneAsync(_welcomeScene);
                }
            };
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
            _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
            ConnectionState -= OnConnectionState;
        }

        private static void OnConnectionState(ConnectionState state)
        {
            Logger.LogVerbose($"Updated connection state: {state.ToString()}");
        }

        private static void ServerManager_OnRemoteConnectionState(NetworkConnection conn,
            RemoteConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case RemoteConnectionState.Stopped:
                    Logger.LogVerbose($"Remote connection {conn} stopped.");
                    break;
                case RemoteConnectionState.Started:
                    Logger.LogVerbose($"Remote connection {conn} started.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public event Action<ConnectionState> ConnectionState;

        public event Action<bool> GlobalSceneLoaded;
        private event Action<bool> LobbySceneLoadStart;


        public void StartServer()
        {
            if (State.HasFlag(LobbySystem.ConnectionState.k_server))
            {
                Logger.LogWarning("The server is already started");
                return;
            }

            Logger.LogVerbose("Starting server...");
            _networkManager.ServerManager.StartConnection();
        }

        public void LeaveServer()
        {
            if (!State.HasFlag(LobbySystem.ConnectionState.k_client))
            {
                Logger.LogWarning("Not connected to a server");
                return;
            }

            Logger.LogVerbose("Stopping server.");
            _networkManager.ClientManager.StopConnection();
            VoiceChatManager.GetInstance()?.Disconnect();
        }

        [CanBeNull]
        public static ConnectionManager GetInstance()
        {
            return s_instance;
        }

        public bool ConnectToServer(ServerAddressResponse addressResponse, string userName)
        {
            if (_networkManager == null)
            {
                Logger.LogError("Could not connect to server. NetworkManager is null");
                return false;
            }

            if (State.HasFlag(LobbySystem.ConnectionState.k_client))
            {
                Logger.LogError("Could not connect to server. Already connected as a client");
                return false;
            }

            if (!TryParseAddress(addressResponse.fishnet_server_address, out (string ip, ushort port) fishnetAddress))
            {
                Logger.LogError(
                    $"Could not connect to server. Failed to parse FishNet server address ('{addressResponse.fishnet_server_address}').");
                return false;
            }

            if (!TryParseAddress(addressResponse.livekit_server_address, out (string ip, ushort port) liveKitAddress))
            {
                Logger.LogError(
                    $"Could not connect to server. Failed to parse LiveKit server address ('{addressResponse.livekit_server_address}').");
                return false;
            }

            Logger.LogVerbose("Connecting to server ...");
            UserName = userName;
            _networkManager.TransportManager.Transport.SetClientAddress(fishnetAddress.ip);
            _networkManager.TransportManager.Transport.SetPort(fishnetAddress.port);
            _networkManager.ClientManager.StartConnection();

            VoiceChatManager.GetInstance()?.SetTokenServerAddress(_tokenServerAddress.ip, _tokenServerAddress.port);

            return true;
        }

        public bool ConnectToServer((string ip, ushort port) fishnetAddress, (string ip, ushort port) liveKitAddress,
            string userName)
        {
            ServerAddressResponse sar = new(fishnetAddress.ip + ":" + fishnetAddress.port,
                liveKitAddress.ip + ":" + liveKitAddress.port);
            return ConnectToServer(sar, userName);
        }

        private static bool TryParseAddress(string address, out (string, ushort) res)
        {
            res = (null, 0);
            if (!Regex.IsMatch(address, ".+:[0-9]+"))
            {
                return false;
            }

            string[] arr = address.Split(':'); // [ip, port]

            if (!uint.TryParse(arr[1], out uint port))
            {
                return false;
            }

            if (port > ushort.MaxValue)
            {
                return false;
            }

            res = (arr[0], (ushort)port);
            return true;
        }


        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs state)
        {
            ConnectionState?.Invoke(State);

            if (state.ConnectionState == LocalConnectionState.Stopped)
            {
                SceneManager.UnloadSceneAsync("GlobalScene");
            }
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            if (state.ConnectionState is LocalConnectionState.Started or LocalConnectionState.Stopped)
            {
                ConnectionState?.Invoke(State);
            }

            // The OnServerConnectionState will be called for each transport. 
            // But we only want to load the global scene once
            if (_isServerInitialized)
            {
                return;
            }

            Logger.LogVerbose($"Server is {state.ConnectionState.ToString()}");
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            string scene = Path.GetFileNameWithoutExtension(_globalScene);
            SceneLoadData sld = new(scene)
            {
                Params =
                {
                    ServerParams = new[] { (object)SceneLoadParam.k_global },
                    ClientParams = LobbyMetaData.SerializeObjects(new[] { (object)SceneLoadParam.k_global })
                }
            };

            _isServerInitialized = true;

            Logger.LogVerbose("Loading global scene...");
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }

        [CanBeNull]
        private SceneLoadParam? GetSceneLoadParam(LoadQueueData queueData)
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
                case SceneLoadParam.k_global:
                    break;
                case SceneLoadParam.k_lobby:
                    LobbySceneLoadStart?.Invoke(args.QueueData.AsServer);
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
                return;
            }

            switch (param.Value)
            {
                case SceneLoadParam.k_global:
                    GlobalSceneLoaded?.Invoke(args.QueueData.AsServer);
                    break;
                case SceneLoadParam.k_lobby:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StopServer()
        {
            _networkManager.ServerManager.StopConnection(false);
        }

        public async Task<ServerAddressResponse> RequestServerIp()
        {
            const string tokenURL = "http://{0}:{1}/requestServerIp";
            string url = string.Format(tokenURL, _tokenServerAddress.ip, _tokenServerAddress.port);
            Logger.LogVerbose($"Requesting server ip from '{url}'");
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return await WEBGL_RequestServerIp(url);
            }

            return await StandaloneRequestServerIp(url);
        }

        private static async Task<ServerAddressResponse> StandaloneRequestServerIp(string url)
        {
            HttpResponseMessage response = await new HttpClient().GetAsync(url);
            ServerAddressResponse res = new();
            if (response.IsSuccessStatusCode)
            {
                res = JsonUtility.FromJson<ServerAddressResponse>(response.Content.ReadAsStringAsync().Result);
            }

            res.success = response.IsSuccessStatusCode;
            if (res.success)
            {
                Logger.LogVerbose("Received server ip");
            }
            else
            {
                Logger.LogWarning("Could not fetch server ip");
            }

            return res;
        }

        private static async Task<ServerAddressResponse> WEBGL_RequestServerIp(string url)
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            await webRequest.SendWebRequest();

            ServerAddressResponse res = new();
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                res = JsonUtility.FromJson<ServerAddressResponse>(webRequest.downloadHandler.text);
            }
            else
            {
                Debug.Log("Error: " + webRequest.error);
            }

            res.success = webRequest.result == UnityWebRequest.Result.Success;
            return res;
        }

        public void SetTokenServerIp(string tokenServerIp)
        {
            if (TryParseAddress(tokenServerIp, out (string, ushort) res))
            {
                _tokenServerAddress = res;
            }
        }

        #region Singleton

        private static ConnectionManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            s_instance = this;
        }

        #endregion
    }

    [Serializable]
    public class ServerAddressResponse
    {
        public bool success;
        public string fishnet_server_address;
        public string livekit_server_address;

        public ServerAddressResponse() { }

        public ServerAddressResponse(string fishnetAddressPort, string livekitServerAddress)
        {
            fishnet_server_address = fishnetAddressPort;
            livekit_server_address = livekitServerAddress;
            success = true;
        }
    }

    [Flags]
    public enum ConnectionState
    {
        k_disconnected = 0, k_client = 1 << 0, k_server = 1 << 1
    }

    public enum SceneLoadParam
    {
        k_global = 0, k_lobby
    }
}