using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Voicechat;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace LobbySystem
{
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] [Scene] private string _globalScene;

        private bool _isServerInitialized;

        private NetworkManager _networkManager;

        public ConnectionState State
        {
            get
            {
                if (_networkManager == null)
                {
                    return LobbySystem.ConnectionState.Disconnected;
                }

                ConnectionState state = LobbySystem.ConnectionState.Disconnected;
                if (_networkManager.ClientManager.Started)
                {
                    state |= LobbySystem.ConnectionState.Client;
                }

                if (_networkManager.ServerManager.Started)
                {
                    state |= LobbySystem.ConnectionState.Server;
                }

                return state;
            }
        }

        public static string UserName { get; private set; }

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
            if (State.HasFlag(LobbySystem.ConnectionState.Server))
            {
                return;
            }

            Debug.LogWarning("Starting Server");
            _networkManager.ServerManager.StartConnection();
        }

        public void LeaveServer()
        {
            if (!State.HasFlag(LobbySystem.ConnectionState.Client))
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

        public void ConnectToServer((string ip, ushort port) fishnetAddress, (string ip, ushort port) liveKitAddress,
            string userName)
        {
            if (_networkManager == null)
            {
                return;
            }

            if (State.HasFlag(LobbySystem.ConnectionState.Client))
            {
                return;
            }

            _networkManager.TransportManager.Transport.SetClientAddress(fishnetAddress.ip);
            _networkManager.TransportManager.Transport.SetPort(fishnetAddress.port);
            _networkManager.ClientManager.StartConnection();
            UserName = userName;
            LiveKitManager.s_instance.SetTokenServerAddress(liveKitAddress.ip, liveKitAddress.port);
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

            string scene = Path.GetFileNameWithoutExtension(_globalScene);
            SceneLoadData sld = new(scene)
            {
                Params =
                {
                    ServerParams = new[] { (object)SceneLoadParam.Global },
                    ClientParams = LobbyMetaData.SerializeObjects(new[] { (object)SceneLoadParam.Global })
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
                case SceneLoadParam.Global:
                    GlobalSceneLoaded?.Invoke(args.QueueData.AsServer);
                    break;
                case SceneLoadParam.Lobby:
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

    [Flags]
    public enum ConnectionState
    {
        Disconnected = 0, Client = 1 << 0, Server = 1 << 1
    }

    public enum SceneLoadParam
    {
        Global = 0, Lobby
    }
}