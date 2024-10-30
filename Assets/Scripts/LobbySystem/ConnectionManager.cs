using FishNet.Managing;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;
using Voicechat;
using FishNet.Managing.Scened;
using System.IO;

namespace LobbySystem
{
    public class ConnectionManager : MonoBehaviour
    {
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
        
        [SerializeField] [Scene] private string _globalScene;

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
        
        private NetworkManager _networkManager;

        public static string UserName { get; private set; }

        public event Action<ConnectionState> ConnectionState;
        public event Action GlobalSceneLoaded;

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

#if UNITY_SERVER
            StartServer();
#endif
        }


        public void StartServer()
        {
            if (State.HasFlag(LobbySystem.ConnectionState.Server))
            {
                return;
            }
            _networkManager.ServerManager.StartConnection();
        }
        
        public void LeaveServer()
        {
            if (State is LobbySystem.ConnectionState.Disconnected or LobbySystem.ConnectionState.Client)
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
        
        public void ConnectToServer((string ip, ushort port) fishnetAddress, (string ip, ushort port) liveKitAddress, string userName)
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
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            string scene = Path.GetFileNameWithoutExtension(_globalScene);
            SceneLoadData sld = new(scene); 
            _networkManager.SceneManager.LoadGlobalScenes(sld);
            ConnectionState?.Invoke(State);
        }

        private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs args)
        {
            if (args.LoadedScenes.Any(scene => _globalScene.Contains(scene.name)))
            {
                GlobalSceneLoaded?.Invoke();
            }
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

        public void StopServer()
        {
            _networkManager.ServerManager.StopConnection(false);
        }
    }

    [Flags]
    public enum ConnectionState
    {
        Disconnected = 0, Client = 1 << 0, Server = 1 << 1, 
    }
}