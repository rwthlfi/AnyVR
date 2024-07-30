using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using Voicechat;

namespace LobbySystem
{
    public class LobbyManager : NetworkBehaviour
    {
        #region Singleton

        internal static LobbyManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of LobbyManager already exists!");
                Destroy(this);
            }

            s_instance = this;
        }

        #region SerializedFields

        [SerializeField] private bool _loggingEnabled;
        [SerializeField] private LobbyHandler _lobbyHandlerPrefab;

        #endregion

        #endregion

        #region ServerOnly

        /// <summary>
        /// The actual lobby handlers.
        /// Only initialized on the server.
        /// </summary>
        private Dictionary<string, LobbyHandler> _lobbyHandlers;

        /// <summary>
        /// A dictionary mapping clients to their corresponding lobby
        /// Only initialized on the server.
        /// </summary>
        private Dictionary<int, string> _clientLobbyDict;

        #endregion
        
        #region ClientOnly
        
        /// <summary>
        /// The meta data of the current lobby.
        /// Can be null if local client is not connected to a lobby
        /// Only initialized on the client.
        /// </summary>
        private LobbyMetaData? _currentLobby;
        
        #endregion
        
        /// <summary>
        /// Dictionary with all active lobbies on the server.
        /// The keys are lobby ids.
        /// </summary>
        private readonly SyncDictionary<string, LobbyMetaData> _lobbies = new();

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
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyHandlers = new Dictionary<string, LobbyHandler>();
            _clientLobbyDict = new Dictionary<int, string>();
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
            string logging = _loggingEnabled ? "enabled" : "disabled";
            Debug.Log($"Server started. Logging {logging}");
        }

        [Server]
        private void TryRegisterLobbyHandler(SceneLoadEndEventArgs loadArgs)
        {
            // Lobbies only have one scene
            if(loadArgs.LoadedScenes.Length != 1)
            {
                return;
            }

            // Lobby scenes are always loaded with exactly one client (creator)
            if (loadArgs.QueueData.Connections.Length != 1)
            {
                return;
            }

            // Try get corresponding lobbyId
            if (!_clientLobbyDict.TryGetValue(loadArgs.QueueData.Connections[0].ClientId, out string lobbyId))
            {
                return;
            }

            // Check if lobby exists
            if (!_lobbies.ContainsKey(lobbyId))
            {
                return;
            }
            
            // Spawn and register the LobbyHandler
            LobbyHandler lobbyHandler = Instantiate(_lobbyHandlerPrefab);
            Spawn(lobbyHandler.NetworkObject, null, loadArgs.LoadedScenes[0]);
            lobbyHandler.Init(lobbyId, loadArgs.QueueData.Connections[0].ClientId);
            
            _lobbies[lobbyId].SetSceneHandle(loadArgs.LoadedScenes[0].handle);
            _lobbyHandlers.Add(lobbyId, lobbyHandler);
            
            Log($"LobbyHandler with lobbyId '{lobbyId}' is registered");
        }

        private string CreateUniqueLobbyId(string scene)
        {
            int i = 1;
            while (i > 0)
            {
                string id = scene + i.ToString();
                if (!_lobbies.ContainsKey(id))
                {
                    return id;
                }
                i++;
            }
            return null;
        }
        
        /// <summary>
        /// Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CreateLobby(string lobbyName, string scene, ushort maxClients, NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            LobbyMetaData lobbyMetaData = new(CreateUniqueLobbyId(scene), lobbyName, conn.ClientId, scene, maxClients);
            _lobbies.Add(lobbyMetaData.ID, lobbyMetaData);
            Log($"Lobby created. {lobbyMetaData.ToString()}");
            AddClientToLobby(lobbyMetaData.ID, conn); // Auto join lobby
        }
        
        /// <summary>
        /// Server Rpc to join an active lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void JoinLobby(string lobbyId, NetworkConnection conn = null)
        {
            AddClientToLobby(lobbyId, conn);
        }

        [Server]
        private void AddClientToLobby(string lobbyId, NetworkConnection conn)
        {
            if (conn == null)
            {
                return;
            }

            if (!_lobbies.TryGetValue(lobbyId, out LobbyMetaData lobby))
            {
                return;
            }
            
            if (_currentLobby != null)
            {
                LeaveLobby();
            }

            if (!_clientLobbyDict.TryAdd(conn.ClientId, lobbyId))
            {
                Debug.LogError($"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'");
                return;
            }
            
            Log($"Client '{conn.ClientId}' joined lobby with id '{lobbyId}");
            OnLobbyJoinedRpc(conn, lobby);
            SceneManager.LoadConnectionScenes(conn, lobby.GetSceneLoadData());
        }
        
        /// <summary>
        /// Callback for when the local client joins a lobby.
        /// </summary>
        [TargetRpc]
        private void OnLobbyJoinedRpc(NetworkConnection _, LobbyMetaData lmd)
        {
            _currentLobby = lmd;
            if (LiveKitManager.s_instance != null)
            {
                LiveKitManager.s_instance.TryConnectToRoom(lmd.ID, LoginManager.UserName);
            }
            else
            {
                Debug.LogWarning("LivKitManager is not initialized");
            }
            LobbyJoined?.Invoke(lmd);
        }
        
        /// <summary>
        /// Callback for when the local client leaves a lobby.
        /// </summary>
        [TargetRpc]
        private void OnLobbyLeftRpc(NetworkConnection _)
        {
            _currentLobby = null;
            LiveKitManager.s_instance.Disconnect(); // Disconnecting from voicechat
            LobbyLeft?.Invoke();
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

        [Server]
        private void CloseLobby(string lobbyId)
        {
            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return;
            }
            
            // Kick all players from the lobby
            foreach ((int id, string) client in handler.GetClients())
            {
                if (!ServerManager.Clients.TryGetValue(client.id, out NetworkConnection clientConn))
                {
                    Debug.LogWarning($"Could not get NetworkConnection from client {client.id}");
                    continue;
                }

                TryRemoveClientFromLobby(clientConn);
            }
            
            _lobbyHandlers.Remove(lobbyId);
            _lobbies.Remove(lobbyId);
            Log($"Lobby with id '{lobbyId}' is closed");
        }
        
        [ServerRpc(RequireOwnership = false)]
        internal void LeaveLobby(NetworkConnection conn = null)
        {
            if (!TryRemoveClientFromLobby(conn))
            {
                return;
            }

            SceneLoadData sld = new(new[] { "LobbySelectionScene" }) { ReplaceScenes = ReplaceOption.All };
            SceneManager.LoadConnectionScenes(conn, sld);
        }
        
        [Server]
        private bool TryRemoveClientFromLobby(NetworkConnection clientConnection)
        {
            if (clientConnection == null)
            {
                return false;
            }
            
            if (!TryGetLobbyOfClient(clientConnection.ClientId, out string lobbyId))
            {
                return false;
            }

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return false;
            }
            
            handler.Server_RemoveClient(clientConnection.ClientId);
            _clientLobbyDict.Remove(clientConnection.ClientId);
            OnLobbyLeftRpc(clientConnection);
            
            if (!handler.GetClients().Any())
            {
                CloseLobby(lobbyId);
            }
            return true;
        }

        [Server]
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case RemoteConnectionState.Stopped:
                    Debug.Log($"Client {conn.ClientId} left the server");
                    TryRemoveClientFromLobby(conn);
                    break;
                case RemoteConnectionState.Started:
                    Debug.Log($"Client {conn.ClientId} joined the server");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        [Server]
        private bool TryGetLobbyOfClient(int clientId, out string lobbyId)
        {
            foreach (KeyValuePair<string, LobbyHandler> lobbyPair in from lobbyPair in _lobbyHandlers
                     let clients = lobbyPair.Value.GetClients()
                     where clients.Any(client => client.Item1 == clientId)
                     select lobbyPair)
            {
                lobbyId = lobbyPair.Key;
                return true;
            }

            lobbyId = default;
            return false;
        }

        private void OnDestroy()
        {
            _lobbies.OnChange -= lobbies_OnChange;
        }

        [CanBeNull]
        public static LobbyManager GetInstance()
        {
            return s_instance;
        }
        
        /// <summary>
        /// Logs a common value if can log.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string value)
        {
            if (_loggingEnabled)
            {
                Debug.Log(value);
            }
        }

        public Dictionary<string, LobbyMetaData> GetAvailableLobbies()
        {
            return _lobbies.Collection;
        }
    }
}