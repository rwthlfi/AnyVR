using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voicechat;

namespace LobbySystem
{
    public class LobbyManager : NetworkBehaviour
    {
        #region Singleton

        public static LobbyManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of LobbyManager already exists!");
                Destroy(this);
            }

            s_instance = this;
        }

        #endregion
        
        /// <summary>
        /// Dictionary with all active lobbies on the server.
        /// The keys are lobby ids.
        /// </summary>
        private readonly SyncDictionary<string, LobbyMetaData> _lobbies = new();

        /// <summary>
        /// The actual lobby handlers.
        /// Only initialized on the server.
        /// </summary>
        private Dictionary<string, LobbyHandler> _lobbyHandlers;
        
        /// <summary>
        /// The meta data of the current lobby.
        /// Can be null if local client is not connected to a lobby
        /// </summary>
        private LobbyMetaData? _currentLobby;
        
        private void Awake()
        {
            InitSingleton();
            _lobbies.OnChange += lobbies_OnChange;
        }

        /// <summary>
        /// Invoked when a remote client opened a new lobby
        /// </summary>
        public event Action<LobbyMetaData> LobbyOpened;
        /// <summary>
        /// Invoked when a remote client closed a lobby
        /// </summary>
        public event Action<LobbyMetaData> LobbyClosed;
        /// <summary>
        /// Invoked when the local client joined a lobby
        /// </summary>
        public event Action<LobbyMetaData> LobbyJoined;
        /// <summary>
        /// Invoked when the local client left a lobby
        /// </summary>
        public event Action LobbyLeft;
        /// <summary>
        /// Invoked when an remote client joined the lobby of the local client
        /// </summary>
        public event Action<int> ClientJoined;
        /// <summary>
        /// Invoked when an remote client left the lobby of thk local client
        /// </summary>
        public event Action<int> ClientLeft;
        public event Action<int[]> LobbyClientListReceived;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyHandlers = new Dictionary<string, LobbyHandler>();
            SceneManager.OnLoadEnd += OnSceneLoadEnd;
            ServerManager.OnRemoteConnectionState += Server_OnRemoteConnectionState;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            LobbyJoined += Client_OnLobbyJoined;
            LobbyLeft += Client_OnLobbyLeft;
        }
        
        /// <summary>
        /// Server Rpc to create a new lobby on the server.
        /// </summary>
        /// <param name="lobbyMetaData">The metadata of the lobby</param>
        [ServerRpc(RequireOwnership = false)]
        public void CreateLobby(LobbyMetaData lobbyMetaData, NetworkConnection conn = null)
        {
            _lobbies.Add(lobbyMetaData.ID, lobbyMetaData);
            JoinLobby(lobbyMetaData.ID, conn); // Auto join lobby
        }

        /// <summary>
        /// Server Rpc to join an active lobby on the server.
        /// </summary>
        /// <param name="id">The id of the lobby to join</param>
        [ServerRpc(RequireOwnership = false)]
        public void JoinLobby(string id, NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            if (!_lobbies.TryGetValue(id, out LobbyMetaData lobby))
            {
                return;
            }
            
            if (_currentLobby != null)
            {
                LeaveLobbyRpc();
            }
            SceneManager.LoadConnectionScenes(conn, lobby.GetSceneLoadData());
        }

        /// <summary>
        /// Callback for when the local client leaves a lobby.
        /// Registered to <see cref="LobbyLeft"/>
        /// </summary>
        private void Client_OnLobbyLeft()
        {
            _currentLobby = null;
            LiveKitManager.s_instance.Disconnect(); // Disconnecting from voicechat
        }

        /// <summary>
        /// Callback for when the local client joins a lobby.
        /// Registered to <see cref="LobbyJoined"/>
        /// </summary>
        private void Client_OnLobbyJoined(LobbyMetaData metaData)
        {
            _currentLobby = metaData;
            if (LiveKitManager.s_instance != null)
            {
                LiveKitManager.s_instance.TryConnectToRoom(metaData.ID, LobbySystem.LoginManager.UserName);
            }
            else
            {
                Debug.LogWarning("LivKitManager is not initialized");
            }
        }

        private void lobbies_OnChange(SyncDictionaryOperation op, string key, LobbyMetaData value, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    LobbyOpened?.Invoke(value);
                    break;
                case SyncDictionaryOperation.Clear:
                    break;
                case SyncDictionaryOperation.Remove:
                    LobbyClosed?.Invoke(value);
                    break;
                case SyncDictionaryOperation.Set:
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        private void OnSceneLoadEnd(SceneLoadEndEventArgs obj)
        {
            if (!obj.QueueData.AsServer)
            {
                return;
            }

            foreach (Scene scene in obj.LoadedScenes)
            {
                if (obj.QueueData.Connections.Length == 0)
                {
                    return;
                }

                if (!TryGetLobbyOfClient(obj.QueueData.Connections.First().ClientId, out string lobbyId))
                {
                    continue;
                }

                _lobbies[lobbyId].SetSceneHandle(scene);
            }
        }


        private void CloseLobby(string lobbyId)
        {
            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return;
            }

            // Kick all players from the lobby
            foreach (int client in handler.GetClients())
            {
                handler.RemoveClient(client);
                OnLobbyLeftRpc(ClientManager.Clients[client]);
            }

            _lobbyHandlers.Remove(lobbyId);
            _lobbies.Remove(lobbyId);
        }
        
        
        [TargetRpc]
        private void OnClientJoinedLobbyRpc(NetworkConnection _, int joinedClientId)
        {
            ClientJoined?.Invoke(joinedClientId);
        }

        [TargetRpc]
        private void OnClientLeftLobbyRpc(NetworkConnection _, int leftClientId)
        {
            ClientLeft?.Invoke(leftClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveLobbyRpc(NetworkConnection conn = null)
        {
            if (!TryRemoveClientFromLobby(conn))
            {
                return;
            }

            SceneLoadData sld = new(new[] { "LobbyScene" }) { ReplaceScenes = ReplaceOption.All };
            SceneManager.LoadConnectionScenes(conn, sld);
        }
        
        private bool TryRemoveClientFromLobby(NetworkConnection client)
        {
            if (client == null)
            {
                return false;
            }

            if (!TryGetLobbyOfClient(client.ClientId, out string lobbyId))
            {
                return false;
            }

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return false;
            }
            handler.RemoveClient(client.ClientId);
            OnLobbyLeftRpc(client);
            
            if (!handler.GetClients().Any())
            {
                CloseLobby(lobbyId);
                return true;
            }
            
            foreach (int clientId in handler.GetClients())
            {
                if (client.ClientId == clientId)
                {
                    continue;
                }

                if (!ServerManager.Clients.TryGetValue(clientId, out NetworkConnection c))
                {
                    continue;
                }

                OnClientLeftLobbyRpc(c, client.ClientId);
            }
            return true;
        }

        private void Server_OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped)
            {
                return;
            }

            Debug.Log($"Client {conn.ClientId} left.");
            TryRemoveClientFromLobby(conn);
        }

        [TargetRpc]
        private void OnLobbyLeftRpc(NetworkConnection _)
        {
            LobbyLeft?.Invoke();
        }

        [TargetRpc]
        private void OnLobbyJoinedRpc(NetworkConnection _, LobbyMetaData lmd)
        {
            LobbyJoined?.Invoke(lmd);
        }

        [Server]
        private bool TryGetLobbyOfClient(int clientId, out string lobbyId)
        {
            foreach (KeyValuePair<string, LobbyHandler> lobbyPair in from lobbyPair in _lobbyHandlers
                     let clients = lobbyPair.Value.GetClients()
                     where clients.Any(client => client == clientId)
                     select lobbyPair)
            {
                lobbyId = lobbyPair.Key;
                return true;
            }

            lobbyId = default;
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestLobbyClientListRpc(LobbyMetaData lmd, NetworkConnection conn = null)
        {
            int[] clients = { };
            if (_lobbyHandlers.TryGetValue(lmd.ID, out LobbyHandler handler))
            {
                clients = handler.GetClients().ToArray();
            }

            ReceiveLobbiesClientListRpc(conn, clients);
        }

        [TargetRpc]
        private void ReceiveLobbiesClientListRpc(NetworkConnection _, int[] clients)
        {
            LobbyClientListReceived?.Invoke(clients);
        }

        public LobbyMetaData? GetCurrentLobby()
        {
            return _currentLobby;
        }
        
        private void OnDestroy()
        {
            _lobbies.OnChange -= lobbies_OnChange;
        }


        public void SetLobbySceneHandle(string lobbyId, Scene scene)
        {
            _lobbies[lobbyId].SetSceneHandle(scene);
        }
    }
}