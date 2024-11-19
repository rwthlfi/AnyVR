using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
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

        #endregion
        
        #region SerializedFields

        [Tooltip("The Scene to load when the local client leaves their current lobby")]
        [SerializeField] [Scene] private string _offlineScene;
        [SerializeField] private bool _loggingEnabled;
        
        [Header("Prefab Setup")]
        [SerializeField] private LobbyHandler _lobbyHandlerPrefab;

        #endregion

        #region ServerOnly

        private event Action<string> LobbyHandlerRegistered;

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
        private LobbyMetaData _currentLobby;

        #endregion
        
        /// <summary>
        /// Dictionary with all active lobbies on the server.
        /// The keys are lobby ids.
        /// </summary>
        private readonly SyncDictionary<string, LobbyMetaData> _lobbies = new();

        private void Awake()
        {
            InitSingleton();
        }

        /// <summary>
        /// Invoked when a remote client opened a new lobby
        /// </summary>
        public event Action<LobbyMetaData> LobbyOpened;
        /// <summary>
        /// Invoked when a remote client closed a lobby
        /// </summary>
        public event Action<string> LobbyClosed;

        /// <summary>
        /// Invoked when the local client starts loading a lobby scene
        /// </summary>
        public event Action ClientLobbyLoadStart;
        
        /// <summary>
        /// Invoked when the player-count of a lobby changes.
        /// The local user does not have to be connected to the lobby.
        /// (string: lobbyId, int: playerCount)
        /// </summary>
        public event Action<string, int> PlayerCountUpdate;
        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyHandlers = new Dictionary<string, LobbyHandler>();
            _clientLobbyDict = new Dictionary<int, string>();
            SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
            string logging = _loggingEnabled ? "enabled" : "disabled";
            Debug.Log($"Server started. Logging {logging}");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SceneManager.OnLoadStart += Client_OnLoadStart;
            SceneManager.OnUnloadEnd += Client_OnUnloadEnd;
        }

        [Client]
        private void Client_OnLoadStart(SceneLoadStartEventArgs args)
        {
            if (IsLoadingLobby(args.QueueData, false, out _))
            {
                ClientLobbyLoadStart?.Invoke();
            }
        }

        [Client]
        private void Client_OnUnloadEnd(SceneUnloadEndEventArgs args)
        {
            if(args.QueueData.AsServer)
            {
                return;
            }

            if(_currentLobby == null)
            {
                return;
            }

            if (IsUnloadingLobby(args.QueueData, false, out string _))
            {
                StartCoroutine(LoadWelcomeScene());
            }
        }

        private static bool IsUnloadingLobby(UnloadQueueData queueData, bool asServer, out string lobbyId)
        {
            object[] loadParams = asServer
                ? queueData.SceneUnloadData.Params.ServerParams
                : LobbyMetaData.DeserializeClientParams(queueData.SceneUnloadData.Params.ClientParams);
            
            lobbyId = string.Empty;
            
            if (loadParams.Length < 2 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }
            
            // Lobbies must have this flag
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.Lobby)
            {
                return false;
            }

            if (loadParams[1] is not string)
            {
                return false;
            }
            
            lobbyId = loadParams[1] as string;
            return true;
        }
        
        private bool IsLoadingLobby(LoadQueueData queueData, bool asServer, out string errorMsg)
        {
            object[] loadParams = asServer
                ? queueData.SceneLoadData.Params.ServerParams
                : LobbyMetaData.DeserializeClientParams(queueData.SceneLoadData.Params.ClientParams);
            
            errorMsg = string.Empty;
            
            if (loadParams.Length < 3 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }
            
            // Lobbies must have this flag
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.Lobby)
            {
                return false;
            }
            
            // Try get corresponding lobbyId
            string lobbyId = loadParams[1] as string;
            if (string.IsNullOrEmpty(lobbyId))
            {
                errorMsg = "The passed lobbyId is null.";
                return false;
            }

            // Check if lobby exists
            if (!_lobbies.ContainsKey(lobbyId))
            {
                errorMsg = $"Lobby with ID '{lobbyId}' does not exist.";
                return false;
            }
            
            // Check that the creating client is passed as param
            if (loadParams[2] is not int)
            {
                errorMsg = "The clientId should be passed as an int.";
                return false;
            }

            return true;
        }

        [Server]
        private void TryRegisterLobbyHandler(SceneLoadEndEventArgs loadArgs)
        {

            if (!IsLoadingLobby(loadArgs.QueueData, true, out string errorMsg))
            {
                if(!string.IsNullOrEmpty(errorMsg))
                {
                    Debug.LogWarning($"Can't register LobbyHandler. {errorMsg}");
                }

                return;
            }
            
            // Lobby scenes have to be loaded with exactly 0 clients
            if (loadArgs.QueueData.Connections.Length != 0)
            {
                Debug.LogWarning($"Can't register LobbyHandler. The lobby scene must be empty.");
                return;
            }
            
            object[] serverParams = loadArgs.QueueData.SceneLoadData.Params.ServerParams;

            if (serverParams[1] is not string lobbyId)
            {
                return; 
            }
            
            int adminId = (int)serverParams[2];

            if (!ServerManager.Clients.ContainsKey(adminId))
            {
                Debug.LogWarning("Can't register LobbyHandler. The passed clientId is not connected to the server.");
                return;
            }

            // Spawn and register the LobbyHandler
            LobbyHandler lobbyHandler = Instantiate(_lobbyHandlerPrefab);
            Spawn(lobbyHandler.NetworkObject, null, loadArgs.LoadedScenes[0]);
            lobbyHandler.Init(lobbyId, adminId);
            lobbyHandler.ClientJoin += (_, _) =>
            {
                int currentPlayerCount = _clientLobbyDict.Count(pair => pair.Value == lobbyId);
                OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            };
            lobbyHandler.ClientLeft += _ =>
            {
                int currentPlayerCount = _clientLobbyDict.Count(pair => pair.Value == lobbyId);
                OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            };
            
            _lobbies[lobbyId].SetSceneHandle(loadArgs.LoadedScenes[0].handle);
            _lobbyHandlers.Add(lobbyId, lobbyHandler);
            
            Log($"LobbyHandler with lobbyId '{lobbyId}' is registered");
            LobbyHandlerRegistered?.Invoke(lobbyId);
        }

        [ObserversRpc]
        private void OnLobbyPlayerCountUpdate(string lobby, ushort playerCount)
        {
            PlayerCountUpdate?.Invoke(lobby, playerCount);
        }

        [Server]
        private string CreateUniqueLobbyId(string scene)
        {
            int i = 1;
            while (i > 0)
            {
                string id = scene + i;
                if (!_lobbies.ContainsKey(id))
                {
                    return id;
                }
                i++;
            }
            return null;
        }
        
        public void Client_CreateLobby(string lobbyName, string scene, ushort maxClients)
        {
            CreateLobby(lobbyName, scene, maxClients, ClientManager.Connection);
        }

        /// <summary>
        /// Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void CreateLobby(string lobbyName, string scene, ushort maxClients, NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            StartCoroutine(Co_CreateLobby(lobbyName, scene, maxClients, conn));
        }

        [Server]
        private IEnumerator Co_CreateLobby(string lobbyName, string scene, ushort maxClients,
            NetworkConnection conn = null)
        {
            if (conn is null)
            {
                yield break;
            }
            
            maxClients = (ushort)Mathf.Max(1, maxClients);
            LobbyMetaData lobbyMetaData = new(CreateUniqueLobbyId(scene), lobbyName, conn.ClientId, scene, maxClients);
            _lobbies.Add(lobbyMetaData.LobbyId, lobbyMetaData);
            
            // Starts the lobby scene without clients. When loaded, the LoadEnd callback will be called and we spawn a LobbyHandler. After that clients are able to join.
            Log("Loading lobby scene. Waiting for lobby handler");
            SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), lobbyMetaData.GetSceneLoadData());

            // Wait for Lobby Handler
            float timeout = 20f;
            bool receivedLobbyHandler = false;
            LobbyHandlerRegistered += lobbyId =>
            {
                if (lobbyId == lobbyMetaData.LobbyId)
                {
                    receivedLobbyHandler = true;
                }
            };
            
            while (!receivedLobbyHandler && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            if (!receivedLobbyHandler)
            {
                CloseLobby(lobbyMetaData.LobbyId);
                Debug.LogWarning($"Lobby (id={lobbyMetaData.LobbyId}) could not be created. LobbyHandler was not received.");
                yield break;
            }

            Log($"Lobby created. {lobbyMetaData.ToString()}");
            AddClientToLobby(lobbyMetaData.LobbyId, conn); // Auto join lobby
            InvokeLobbyOpened(lobbyMetaData);
        }

        [ObserversRpc(ExcludeServer = false)]
        private void InvokeLobbyOpened(LobbyMetaData lobbyMetaData)
        {
            LobbyOpened?.Invoke(lobbyMetaData);
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
                Debug.LogWarning($"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Lobby was not found.");
                return;
            }
            
            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler _))
            {
                Debug.LogWarning($"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. The corresponding lobby handler does not exist.");
                return;
            }

            int currentPlayerCount = _clientLobbyDict.Count(pair => pair.Value == lobbyId);
            if (currentPlayerCount >= lobby.LobbyCapacity)
            {
                Debug.LogWarning($"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. The lobby is already full.");
                return;
            }

            if (!_clientLobbyDict.TryAdd(conn.ClientId, lobbyId))
            {
                Debug.LogWarning($"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'.");
                return;
            }
            
            Log($"Client '{PlayerNameTracker.GetPlayerName(conn.ClientId)}' joined lobby with id '{lobbyId}");
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
                LiveKitManager.s_instance.TryConnectToRoom(lmd.LobbyId, ConnectionManager.UserName);
            }
            else
            {
                Debug.LogWarning("LivKitManager is not initialized");
            }
        }
        
        /// <summary>
        /// Callback for when the local client leaves a lobby.
        /// </summary>
        [TargetRpc]
        private void OnLobbyLeftRpc(NetworkConnection _)
        {
            LiveKitManager.s_instance.Disconnect(); // Disconnecting from voicechat
        }

        private IEnumerator LoadWelcomeScene()
        {
            AsyncOperation op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("UIScene");
            while (!op.isDone)
            {
                yield return null;
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(_offlineScene, LoadSceneMode.Additive);
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
            LobbyClosed?.Invoke(lobbyId);
            Log($"Lobby with id '{lobbyId}' is closed");
        }
        
        [ServerRpc(RequireOwnership = false)]
        internal void LeaveLobby(NetworkConnection conn = null)
        {
            if (!TryRemoveClientFromLobby(conn))
            {
                Debug.LogWarning("Client could not be removed from lobby");
            }
        }

        [Server]
        private bool TryRemoveClientFromLobby(NetworkConnection clientConnection)
        {
            if (clientConnection == null)
            {
                return false;
            }

            if (!TryGetLobbyIdOfClient(clientConnection.ClientId, out string lobbyId))
            {
                return false;
            }

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return false;
            }
            
            handler.Server_RemoveClient(clientConnection.ClientId);
            _clientLobbyDict.Remove(clientConnection.ClientId);

            if (_lobbies.TryGetValue(lobbyId, out LobbyMetaData lmd))
            {
                SceneUnloadData sud = new(new[] { lmd.Scene }) { Params = { ServerParams = new object[] { SceneLoadParam.Lobby, lmd.LobbyId } } };
                sud.Params.ClientParams = LobbyMetaData.SerializeObjects(sud.Params.ServerParams);
                Debug.LogWarning("Unloading Connection scenes");
                SceneManager.UnloadConnectionScenes(clientConnection, sud);
            }

            OnLobbyLeftRpc(clientConnection);
            
            if (!handler.GetClients().Any())
            {
                CloseLobby(lobbyId);
            }
            return true;
        }

        [Server]
        public bool TryGetLobbyIdOfClient(int clientId, out string lobbyId)
        {
            return _clientLobbyDict.TryGetValue(clientId, out lobbyId);
        }

        [Server]
        public bool TryGetLobbyHandlerById(string lobbyId, out LobbyHandler res)
        {
            return _lobbyHandlers.TryGetValue(lobbyId, out res);
        }

        [Server]
        public void HandleClientDisconnect(NetworkConnection conn)
        {
            TryRemoveClientFromLobby(conn);
        }
        
        [CanBeNull]
        public static LobbyManager GetInstance()
        {
            return s_instance;
        }

        [CanBeNull, Client]
        public LobbyMetaData Client_GetCurrentLobby()
        {
            return _currentLobby;
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