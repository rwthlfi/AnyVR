using FishNet.Managing;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Voicechat;

#if UNITY_SERVER
using FishNet.Managing.Scened;
using System.IO;
#endif

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
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
            s_instance = this;
        }

        #endregion
        
        public bool IsConnected => _networkManager != null && _networkManager.ClientManager.Started;
        
        [SerializeField] [Scene] private string _globalScene;
        private NetworkManager _networkManager;

        public static string UserName { get; private set; }

        public event Action<bool> ConnectionState;
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
#if UNITY_SERVER
            Debug.Log("Starting server!");
            _networkManager.ServerManager.StartConnection();
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
#else
            _networkManager.ClientManager.OnClientConnectionState += state =>
            {
                switch (state.ConnectionState)
                {
                    case LocalConnectionState.Started:
                        ConnectionState?.Invoke(true);
                        break;
                    case LocalConnectionState.Stopped:
                        ConnectionState?.Invoke(false);
                        break;
                    case LocalConnectionState.Starting:
                        break;
                    case LocalConnectionState.Stopping:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            _networkManager.SceneManager.OnLoadEnd += args =>
            {
                if (args.LoadedScenes.Any(scene => _globalScene.Contains(scene.name)))
                {
                    GlobalSceneLoaded?.Invoke();
                }
            };
#endif
    }

        private void OnDestroy()
        {
#if UNITY_SERVER
            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
#endif
        }

        public void ConnectToServer((string ip, ushort port) fishnetAddress, (string ip, ushort port) liveKitAddress, string userName)
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.TransportManager.Transport.SetClientAddress(fishnetAddress.ip);
            _networkManager.TransportManager.Transport.SetPort(fishnetAddress.port);
            _networkManager.ClientManager.StartConnection();
            UserName = userName;
            LiveKitManager.s_instance.SetTokenServerAddress(liveKitAddress.ip, liveKitAddress.port);
        }

        public void LeaveServer()
        {
            if (_networkManager == null)
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

#if UNITY_SERVER
        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            string scene = Path.GetFileNameWithoutExtension(_globalScene);
            SceneLoadData sld = new(scene); 
            _networkManager.SceneManager.LoadGlobalScenes(sld);
            ConnectionState?.Invoke(true);
        }
#endif
    }
}