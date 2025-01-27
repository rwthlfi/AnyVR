using AnyVr.Voicechat;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AnyVr.LobbySystem
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] [Scene] private string globalScene;

        private bool _isServerInitialized;

        private NetworkManager _networkManager;

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

        public static string UserName { get; }

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            if (NetworkManager.Instances.Any())
            {
                _networkManager = NetworkManager.Instances.First();
            }

            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            _networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;

#if UNITY_SERVER && !UNITY_EDITOR
            StartServer(); // Autostart server in server builds
#else
            // The WelcomeScene gets only unloaded for clients
            LobbySceneLoaded += asServer =>
            {
                if (!asServer)
                {
                    SceneManager.UnloadSceneAsync("WelcomeScene");
                }
            };
#endif
        }

        private void OnDestroy()
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
        }

        public event Action<ConnectionState> ConnectionState;
        public event Action<bool> GlobalSceneLoaded;
        private event Action<bool> LobbySceneLoaded;


        public void StartServer()
        {
            if (State.HasFlag(LobbySystem.ConnectionState.k_server))
            {
                return;
            }

            Debug.LogWarning("Starting Server");
            _networkManager.ServerManager.StartConnection();
        }

        public void LeaveServer()
        {
            if (!State.HasFlag(LobbySystem.ConnectionState.k_client))
            {
                return;
            }

            _networkManager.ClientManager.StopConnection();
            LiveKitManager.s_instance.Disconnect();
        }

        [CanBeNull]
        public static ConnectionManager GetInstance()
        {
            return s_instance;
        }

        public bool ConnectToServer(ServerAddressResponse addressResponse, string userName, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (_networkManager == null)
            {
                errorMessage = "NetworkManager is null.";
                return false;
            }

            if (State.HasFlag(LobbySystem.ConnectionState.k_client))
            {
                errorMessage = "Already connected as a client.";
                return false;
            }

            if (!TryParseAddress(addressResponse.fishnet_server_address, out (string ip, ushort port) fishnetAddress))
            {
                errorMessage = $"Failed to parse FishNet server address ('{addressResponse.fishnet_server_address}').";
                return false;
            }

            if (!TryParseAddress(addressResponse.livekit_server_address, out (string ip, ushort port) liveKitAddress))
            {
                errorMessage = $"Failed to parse LiveKit server address ('{addressResponse.livekit_server_address}').";
                return false;
            }

            _networkManager.TransportManager.Transport.SetClientAddress(fishnetAddress.ip);
            _networkManager.TransportManager.Transport.SetPort(fishnetAddress.port);
            _networkManager.ClientManager.StartConnection();

            LiveKitManager.s_instance.SetTokenServerAddress(liveKitAddress.ip, liveKitAddress.port);

            return true;
        }

        public bool ConnectToServer((string ip, ushort port) fishnetAddress, (string ip, ushort port) liveKitAddress,
            string userName, out string errorMessage)
        {
            ServerAddressResponse sar = new(fishnetAddress.ip + ":" + fishnetAddress.port,
                liveKitAddress.ip + ":" + liveKitAddress.port);
            return ConnectToServer(sar, userName, out errorMessage);
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
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            // The OnServerConnectionState will be called for each transport. 
            // But we only want to load the global scene once
            if (_isServerInitialized)
            {
                return;
            }

            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            string scene = Path.GetFileNameWithoutExtension(globalScene);
            SceneLoadData sld = new(scene)
            {
                Params =
                {
                    ServerParams = new[] { (object)SceneLoadParam.k_global },
                    ClientParams = LobbyMetaData.SerializeObjects(new[] { (object)SceneLoadParam.k_global })
                }
            };

            _isServerInitialized = true;

            _networkManager.SceneManager.LoadGlobalScenes(sld);
            ConnectionState?.Invoke(State);
        }

        private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs args)
        {
            SceneLoadParam param;
            if (args.QueueData.AsServer)
            {
                object[] serverParams = args.QueueData.SceneLoadData.Params.ServerParams;
                if (serverParams.Length < 1 || serverParams[0] is not SceneLoadParam)
                {
                    return;
                }

                param = (SceneLoadParam)serverParams[0];
            }
            else
            {
                byte[] arr = args.QueueData.SceneLoadData.Params.ClientParams;
                object[] clientParams = LobbyMetaData.DeserializeClientParams(arr);

                if (clientParams.Length < 1)
                {
                    return;
                }

                param = (SceneLoadParam)clientParams[0];
            }

            switch (param)
            {
                case SceneLoadParam.k_global:
                    GlobalSceneLoaded?.Invoke(args.QueueData.AsServer);
                    break;
                case SceneLoadParam.k_lobby:
                    LobbySceneLoaded?.Invoke(args.QueueData.AsServer);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void StopServer()
        {
            _networkManager.ServerManager.StopConnection(false);
        }

        public static async Task<ServerAddressResponse> RequestServerIp(string tokenServerIp)
        {
            const string tokenURL = "http://{0}/requestServerIp";
            string url = string.Format(tokenURL, tokenServerIp);
            Debug.Log($"Request server ip from {url}");
            HttpResponseMessage response = await new HttpClient().GetAsync(url);
            ServerAddressResponse res = new();
            if (response.IsSuccessStatusCode)
            {
                res = JsonUtility.FromJson<ServerAddressResponse>(response.Content.ReadAsStringAsync().Result);
            }

            res.success = response.IsSuccessStatusCode;
            return res;
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