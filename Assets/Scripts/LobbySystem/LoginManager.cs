using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using LobbySystem.UI;
using System.IO;
using UnityEngine;
using Voicechat;

namespace LobbySystem
{
    public class LoginManager : MonoBehaviour
    {
        [SerializeField] [Scene] private string _globalScene;

        [SerializeField] [Scene] private string _lobbyScene;

        private LocalConnectionState _clientState;

        private SceneLoadData _lobbySceneLoadData;

        private NetworkManager _networkManager;
        private LocalConnectionState _serverState;

        public static string UserName { get; private set; }

        private void Start()
        {
            _networkManager = GetComponent<NetworkManager>();
#if UNITY_SERVER
            if (_networkManager == null)
                return;
            Debug.Log("Starting server!");
            _networkManager.ServerManager.StartConnection();
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
#else
            OfflineScene.OnLoginRequest += Client_ConnectToServer;
#endif
        }

        private void OnDestroy()
        {
#if UNITY_SERVER
            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
#endif
        }

        private void Client_ConnectToServer((string ip, ushort port) fishnetAddress, (string ip, ushort port) livekitAddress, string userName)
        {
            if (_networkManager == null)
            {
                return;
            }

            _networkManager.TransportManager.Transport.SetClientAddress(fishnetAddress.ip);
            _networkManager.TransportManager.Transport.SetPort(fishnetAddress.port);
            _networkManager.ClientManager.StartConnection();
            UserName = userName;
            LiveKitManager.s_instance.SetTokenServerAddress(livekitAddress.ip, livekitAddress.port);
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
            _networkManager.SceneManager.LoadConnectionScenes(_lobbySceneLoadData); // Preload lobby scene
        }

        private static string GetSceneName(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}