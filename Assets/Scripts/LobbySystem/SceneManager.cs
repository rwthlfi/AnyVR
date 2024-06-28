using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using System.IO;
using UnityEngine;
using Voicechat;

namespace LobbySystem
{
    public class SceneManager : MonoBehaviour
    {
        [SerializeField] [Scene] private string _globalScene;

        [SerializeField] [Scene] private string _lobbyScene;

        private LocalConnectionState _clientState;

        private SceneLoadData _lobbySceneLoadData;

        private NetworkManager _networkManager;
        private LocalConnectionState _serverState;

        public static string EnteredUserName { get; private set; }

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
#if UNITY_SERVER
            if (_networkManager == null)
                return;
            
            Debug.Log("starting server!");
            _networkManager.ServerManager.StartConnection();
#endif
        }

        private void OnDestroy()
        {
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
        }

        public void Client_ConnectToServer(string ipAddress, ushort port, string liveKitIpAddress, ushort liveKitPort,
            string userName)
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.TransportManager.Transport.SetClientAddress(ipAddress);
            _networkManager.TransportManager.Transport.SetPort(port);
            _networkManager.ClientManager.StartConnection();
            EnteredUserName = userName;
            LiveKitManager.s_instance.SetTokenServerAddress(liveKitIpAddress, liveKitPort);
        }

        private void ServerManager_OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Started)
            {
                return;
            }

            _networkManager.SceneManager.LoadConnectionScenes(conn, _lobbySceneLoadData);
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs state)
        {
            if (state.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            string scene = GetSceneName(_globalScene);
            SceneLoadData sld = new(scene) { ReplaceScenes = ReplaceOption.All };
            _networkManager.SceneManager.LoadGlobalScenes(sld);
            _lobbySceneLoadData = new SceneLoadData(GetSceneName(_lobbyScene))
            {
                Options = { AutomaticallyUnload = false }
            };
            _networkManager.SceneManager.LoadConnectionScenes(_lobbySceneLoadData);
        }

        private static void LoadOfflineScene()
        {
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs state)
        {
            _clientState = state.ConnectionState;
            if (_clientState != LocalConnectionState.Stopped)
            {
                return;
            }

            if (!_networkManager.IsServerStarted)
            {
                LoadOfflineScene();
            }
        }

        private static string GetSceneName(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}