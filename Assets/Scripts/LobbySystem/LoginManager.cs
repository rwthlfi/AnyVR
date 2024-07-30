using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Voicechat;

namespace LobbySystem
{
    public class LoginManager : MonoBehaviour
    {
        [SerializeField] [Scene] private string _globalScene;
        private NetworkManager _networkManager;
        
        public static string UserName { get; private set; }

        public event Action<bool> ConnectionState;

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
#if UNITY_SERVER
            if (_networkManager == null)
            {
                return;
            }

            Debug.Log("Starting server!");
            _networkManager.ServerManager.StartConnection();
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
#else
            _networkManager.ClientManager.OnClientConnectionState += state =>
            {
                if (state.ConnectionState == LocalConnectionState.Stopped)
                {
                    ConnectionState?.Invoke(false);
                }
            };
#endif
            _networkManager.SceneManager.OnLoadEnd += args =>
            {
                if (args.LoadedScenes.Any(scene => _globalScene.Contains(scene.name)))
                {
                    ConnectionState?.Invoke(true);
                }
            };
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