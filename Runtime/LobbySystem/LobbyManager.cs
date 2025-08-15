using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public class LobbyManager : NetworkBehaviour
    {
        public delegate void PlayerCountEvent(Guid lobbyId, int playerCount);

        private const string Tag = nameof(LobbyManager);

        /// <summary>
        ///     Dictionary with all active lobbies on the server.
        ///     The keys are lobby ids.
        /// </summary>
        private readonly SyncDictionary<Guid, LobbyMetaData> _lobbies = new();

        #region ClientOnly

        /// <summary>
        ///     The meta data of the current lobby.
        ///     Can be null if local client is not connected to a lobby
        ///     Only initialized on the client.
        /// </summary>
        private LobbyMetaData _currentLobby;

        #endregion

        /// <summary>
        ///     Invoked when the player-count of a lobby changes.
        ///     The local user does not have to be connected to the lobby.
        /// </summary>
        public PlayerCountEvent PlayerCountUpdate;

        /// <summary>
        ///     All available scenes for a lobby
        /// </summary>
        public LobbySceneMetaData[] LobbyScenes => _lobbyScenes.ToArray();

        /// <summary>
        ///     This event is invoked after the (local) LobbyManager is spawned and initialized.
        /// </summary>
        public static event Action<LobbyManager> OnClientInitialized;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            OnLobbyClosed += lobbyId => { Logger.Log(LogLevel.Verbose, Tag, $"Lobby {lobbyId} closed"); };
            OnLobbyOpened += lobbyId => { Logger.Log(LogLevel.Verbose, Tag, $"Lobby {lobbyId} opened"); };
        }

        /// <summary>
        ///     Invoked when a remote client opened a new lobby
        /// </summary>
        public event Action<Guid> OnLobbyOpened;

        /// <summary>
        ///     Invoked when a remote client closed a lobby
        /// </summary>
        public event Action<Guid> OnLobbyClosed;

        /// <summary>
        ///     Invoked when the local client joined a lobby
        /// </summary>
        public event Action<Guid> OnLobbyJoined;

        /// <summary>
        ///     Invoked when the local client left a lobby
        /// </summary>
        public event Action<Guid> OnLobbyLeft;

        /// <summary>
        ///     Invoked when the local client starts loading a lobby scene
        /// </summary>
        public event Action ClientLobbyLoadStart;

        [Server]
        internal bool TryGetQuickConnectCode(Guid lobbyId, out uint quickConnectCode)
        {
            return _quickConnectHandler.TryGetQuickConnectCode(lobbyId, out quickConnectCode);
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyHandlers = new Dictionary<Guid, LobbyHandler>();
            _lobbyPasswordHashes = new Dictionary<Guid, byte[]>();
            _lobbyExpirationRoutines = new Dictionary<Guid, Coroutine>();
            _quickConnectHandler = new QuickConnectHandler();
            SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SceneManager.OnLoadStart += Client_OnLoadStart;
            SceneManager.OnUnloadEnd += Client_OnUnloadEnd;
            OnClientInitialized?.Invoke(this);
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
            if (args.QueueData.AsServer)
            {
                return;
            }

            if (_currentLobby == null)
            {
                return;
            }

            if (!IsUnloadingLobby(args.QueueData, false))
            {
                return;
            }
            AsyncOperation op = USceneManager.LoadSceneAsync(_offlineScene, LoadSceneMode.Additive);
            if (op != null)
            {
                op.completed += _ =>
                {
                    USceneManager.SetActiveScene(USceneManager.GetSceneByPath(_offlineScene));
                };
            }
        }

        private static bool IsUnloadingLobby(UnloadQueueData queueData, bool asServer)
        {
            object[] loadParams = asServer
                ? queueData.SceneUnloadData.Params.ServerParams
                : LobbyMetaData.DeserializeClientParams(queueData.SceneUnloadData.Params.ClientParams);

            if (loadParams.Length < 2 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }

            // Lobbies must have this flag
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.Lobby)
            {
                return false;
            }

            return loadParams[1] is Guid;
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
            Guid lobbyId = (Guid)loadParams[1];
            if (Guid.Empty == lobbyId)
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
            if (loadParams[2] is int)
                return true;
            
            errorMsg = "The clientId should be passed as an int.";
            return false;

        }

        [Server]
        private void TryRegisterLobbyHandler(SceneLoadEndEventArgs loadArgs)
        {
            if (!IsLoadingLobby(loadArgs.QueueData, true, out string errorMsg))
            {
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.Log(LogLevel.Warning, Tag, $"Can't register LobbyHandler. {errorMsg}");
                }

                return;
            }

            // Lobby scenes have to be loaded with exactly 0 clients
            if (loadArgs.QueueData.Connections.Length != 0)
            {
                Logger.Log(LogLevel.Warning, Tag, "Can't register LobbyHandler. The lobby must be empty.");
                return;
            }

            object[] serverParams = loadArgs.QueueData.SceneLoadData.Params.ServerParams;

            if (serverParams[1] is not Guid lobbyId)
            {
                return;
            }

            int adminId = (int)serverParams[2];

            if (!ServerManager.Clients.ContainsKey(adminId))
            {
                Logger.Log(LogLevel.Warning, Tag, "Can't register LobbyHandler. The passed clientId is not connected to the server.");
                return;
            }

            if (!TryGetQuickConnectCode(lobbyId, out uint quickConnectCode))
            {
                Logger.Log(LogLevel.Error, Tag, "Can't register LobbyHandler. Corresponding QuickConnectCode does not exist.");
                return;
            }

            // Spawn and register the LobbyHandler
            LobbyHandler lobbyHandler = Instantiate(_lobbyHandlerPrefab);
            Spawn(lobbyHandler.NetworkObject, null, loadArgs.LoadedScenes[0]);
            lobbyHandler.Init(lobbyId, quickConnectCode);
            lobbyHandler.OnPlayerJoin += _ =>
            {
                int currentPlayerCount = _lobbyHandlers[lobbyId].PlayerStates.Count();
                OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            };
            lobbyHandler.OnPlayerLeave += _ =>
            {
                int currentPlayerCount = _lobbyHandlers[lobbyId].PlayerStates.Count();
                OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            };

            _lobbies[lobbyId].SetSceneHandle(loadArgs.LoadedScenes[0].handle);
            _lobbyHandlers.Add(lobbyId, lobbyHandler);

            Logger.Log(LogLevel.Verbose, Tag, $"LobbyHandler with lobbyId '{lobbyId}' is registered");
            LobbyHandlerRegistered?.Invoke(lobbyId);
        }

        [ObserversRpc]
        private void OnLobbyPlayerCountUpdate(Guid lobby, ushort playerCount)
        {
            PlayerCountUpdate?.Invoke(lobby, playerCount);
        }

        /// <summary>
        ///     Initiates the creation of a new lobby on the server with a remote procedure call.
        /// </summary>
        /// <param name="lobbyName">The name of the lobby.</param>
        /// <param name="password">The password for the lobby (if any). Pass null or white space for no password.</param>
        /// <param name="sceneMetaData">The scene of the lobby</param>
        /// <param name="maxClients">The maximum number of clients allowed in the lobby.</param>
        /// <param name="expirationDate">Optional expiration date for the lobby.</param>
        public void Client_CreateLobby(string lobbyName, string password, LobbySceneMetaData sceneMetaData, ushort maxClients, DateTime? expirationDate = null)
        {
            CreateLobby(lobbyName, password, sceneMetaData.ScenePath, maxClients, sceneMetaData.Name, expirationDate, ClientManager.Connection);
        }

        /// <summary>
        ///     Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void CreateLobby(string lobbyName, string password, string scenePath, ushort maxClients, string sceneName, DateTime? expirationDate, NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = $"{sceneName}"; //TODO
            }
            maxClients = (ushort)Mathf.Max(1, maxClients);
            LobbyMetaData meta = new LobbyMetaData.Builder()
                .WithName(lobbyName)
                .WithCreator(conn.ClientId)
                .WithScene(scenePath, sceneName)
                .WithCapacity(maxClients)
                .WithPasswordProtection(!string.IsNullOrWhiteSpace(password))
                .WithExpiration(expirationDate)
                .Build();

            StartCoroutine(Co_CreateLobby(meta, password, expirationDate));
        }

        [Server]
        private IEnumerator Co_CreateLobby(LobbyMetaData lobbyMetaData, string password, DateTime? expirationDate)
        {
            _lobbies.Add(lobbyMetaData.LobbyId, lobbyMetaData);

            if (lobbyMetaData.IsPasswordProtected)
            {
                _lobbyPasswordHashes.Add(lobbyMetaData.LobbyId, ComputeSha256(password));
            }

            _quickConnectHandler.RegisterLobby(lobbyMetaData.LobbyId);

            // Starts the lobby scene without clients. When loaded, the LoadEnd callback will be called, and we spawn a LobbyHandler.
            Logger.Log(LogLevel.Verbose, Tag, "Loading lobby scene. Waiting for lobby handler");
            SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), lobbyMetaData.GetSceneLoadData());

            // Wait for Lobby Handler
            float timeout = 2f;
            bool receivedLobbyHandler = false;
            LobbyHandlerRegistered += Handler;

            while (!receivedLobbyHandler && timeout > 0)
            {
                yield return new WaitForFixedUpdate();
                timeout -= Time.fixedDeltaTime;
            }

            LobbyHandlerRegistered -= Handler;

            if (!receivedLobbyHandler)
            {
                Server_CloseLobby(lobbyMetaData.LobbyId);
                Logger.Log(LogLevel.Warning, Tag,
                    $"Lobby (id={lobbyMetaData.LobbyId}) could not be created. LobbyHandler was not received.");
                yield break;
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Lobby created. {lobbyMetaData}");

            InvokeLobbyOpened(lobbyMetaData.LobbyId);

            if (expirationDate.HasValue)
            {
                Coroutine routine = StartCoroutine(ExpireLobby(lobbyMetaData.LobbyId, expirationDate.Value));
                _lobbyExpirationRoutines.Add(lobbyMetaData.LobbyId, routine);
            }

            yield break;

            void Handler(Guid lobbyId)
            {
                if (lobbyId == lobbyMetaData.LobbyId)
                {
                    receivedLobbyHandler = true;
                }
            }
        }

        [Server]
        private IEnumerator ExpireLobby(Guid lobbyId, DateTime expirationDate)
        {
            float timeUntilExpiration = (float)(expirationDate - DateTime.UtcNow).TotalSeconds;

            Logger.Log(LogLevel.Verbose, Tag, $"Expire lobby {lobbyId} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Lobby {lobbyId} expired");
            _lobbyExpirationRoutines.Remove(lobbyId);
            Server_CloseLobby(lobbyId);
        }

        public void UpdateLobbyExpirationDate(Guid lobbyId, DateTime? expirationDate)
        {
            if (!_lobbyExpirationRoutines.TryGetValue(lobbyId, out Coroutine routine))
            {
                return;
            }

            StopCoroutine(routine);
            _lobbyExpirationRoutines.Remove(lobbyId);
            if (expirationDate.HasValue)
            {
                StartCoroutine(ExpireLobby(lobbyId, expirationDate.Value));
            }
        }

        [ObserversRpc(ExcludeServer = false)]
        private void InvokeLobbyOpened(Guid id)
        {
            OnLobbyOpened?.Invoke(id);
        }

        /// <summary>
        ///     Closes the current lobby of the client.
        ///     The calling client has to be the creator of the lobby.
        ///     Kicks all connected clients before closing.
        /// </summary>
        [Client]
        public void Client_CloseLobby()
        {
            if (_currentLobby == null)
            {
                Logger.Log(LogLevel.Warning, Tag, "Client cannot close lobby. Client has to be connected to the lobby.");
                return;
            }

            if (_currentLobby.CreatorId != ClientManager.Connection.ClientId)
            {
                Logger.Log(LogLevel.Warning, Tag, "Client cannot close lobby. Client is not the creator of the lobby.");
                return;
            }

            CloseLobbyRpc(_currentLobby.LobbyId);
        }

        public void Client_JoinLobby(Guid lobbyId, string password = null)
        {
            Logger.Log(LogLevel.Verbose, Tag, "Requesting to join lobby");
            JoinLobby(lobbyId, password, ClientManager.Connection);
        }

        [Client]
        public void Client_QuickConnect(string quickConnectCode)
        {
            Logger.Log(LogLevel.Warning, Tag, $"Code: {quickConnectCode}");
            if (uint.TryParse(quickConnectCode, out uint code))
            {
                Logger.Log(LogLevel.Warning, Tag, "Performing RPC");
                QuickConnectRpc(code, ClientManager.Connection);
            }
            else
            {
                Logger.Log(LogLevel.Warning, Tag, "Quick connect code is invalid.");
            }
        }
        
        [Client]
        public bool Client_QuickConnect(ushort quickConnectCode)
        {
            if (!_quickConnectHandler.TryGetLobbyId(quickConnectCode, out Guid lobbyId))
            {
                QuickConnectRpc(quickConnectCode, ClientManager.Connection);
            }
            return true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void QuickConnectRpc(uint quickConnect, NetworkConnection conn)
        {
            Logger.Log(LogLevel.Warning, Tag, "Received quick connect RPC");
            if (_quickConnectHandler.TryGetLobbyId(quickConnect, out Guid lobbyId))
            {
                Logger.Log(LogLevel.Verbose, Tag, $"{conn.ClientId} connecting to lobby '{lobbyId} via quickConnect");
                // TODO: handle password protected lobbies
                AddClientToLobby(lobbyId, string.Empty, conn);
            }
            Logger.Log(LogLevel.Warning, Tag, $"Error performing quickConnect with code {quickConnect}");
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
        { 
            AddClientToLobby(lobbyId, password, conn);
        }

        private static byte[] ComputeSha256(string s)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        [Server]
        private void AddClientToLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            Assert.IsNotNull(conn);

            if (!_lobbies.TryGetValue(lobbyId, out LobbyMetaData lobby))
            {
                Logger.Log(LogLevel.Warning, Tag,
                    $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Lobby was not found.");
                return;
            }

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler _))
            {
                Logger.Log(LogLevel.Warning, Tag,
                    $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. The corresponding lobby handler does not exist.");
                return;
            }

            //TODO: capacity

            if (lobby.IsPasswordProtected)
            {
                if (!_lobbyPasswordHashes.TryGetValue(lobbyId, out byte[] passwordHash))
                {
                    Logger.Log(LogLevel.Error, Tag,
                        $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Lobby '{lobbyId}' is password protected but the password hash is not available.");
                    return;
                }

                if (passwordHash == null)
                {
                    Logger.Log(LogLevel.Error, Tag,
                        $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Password hash is null");
                    return;
                }

                if (!ComputeSha256(password).SequenceEqual(passwordHash))
                {
                    Logger.Log(LogLevel.Verbose, Tag,
                        $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Password hashes do not match.");
                    return;
                }
            }

            Logger.Log(LogLevel.Verbose, Tag,
                $"Client '{conn.ClientId}' joined lobby '{lobbyId}'");
            OnLobbyJoinedRpc(conn, lobby);
            SceneManager.LoadConnectionScenes(conn, lobby.GetSceneLoadData());
        }

        /// <summary>
        ///     Callback for when the local client joins a lobby.
        /// </summary>
        [TargetRpc]
        private void OnLobbyJoinedRpc(NetworkConnection _, LobbyMetaData lmd)
        {
            _currentLobby = lmd;
            Logger.Log(LogLevel.Verbose, Tag, $"Lobby joined '{lmd.LobbyId}'");
            OnLobbyJoined?.Invoke(lmd.LobbyId);
        }

        /// <summary>
        ///     Callback for when the local client leaves a lobby.
        /// </summary>
        [TargetRpc]
        private void OnLobbyLeftRpc(NetworkConnection _)
        {
            VoiceChatManager.GetInstance()?.Disconnect();
            Logger.Log(LogLevel.Verbose, Tag, "Lobby left");
            OnLobbyLeft?.Invoke(_currentLobby.LobbyId);
            _currentLobby = null;
        }

        [ServerRpc(RequireOwnership = false)]
        private void CloseLobbyRpc(Guid lobbyId)
        {
            Server_CloseLobby(lobbyId);
        }

        [Server]
        private void Server_CloseLobby(Guid lobbyId)
        {
            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return;
            }

            // Kick all players from the lobby
            foreach (PlayerState player in handler.PlayerStates)
            {
                if (!ServerManager.Clients.TryGetValue(player.GetID(), out NetworkConnection clientConn))
                {
                    Logger.Log(LogLevel.Warning, Tag, $"Could not get NetworkConnection from client {player.GetID()}");
                    continue;
                }

                TryRemoveClientFromLobby(clientConn);
            }

            SceneUnloadData sud = CreateUnloadData(lobbyId);
            _lobbyHandlers.Remove(lobbyId);
            _lobbies.Remove(lobbyId);
            _lobbyPasswordHashes.Remove(lobbyId);
            _quickConnectHandler.UnregisterLobby(lobbyId);

            if (sud == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Can't unload connection scene. SceneHandle is null");
            }
            else
            {
                SceneManager.UnloadConnectionScenes(sud);
            }

            OnLobbyClosed?.Invoke(lobbyId);
            Logger.Log(LogLevel.Verbose, Tag, $"Lobby with id '{lobbyId}' is closed");
        }

        [ServerRpc(RequireOwnership = false)]
        internal void LeaveLobby(NetworkConnection conn = null)
        {
            if (!TryRemoveClientFromLobby(conn))
            {
                Logger.Log(LogLevel.Warning, Tag, "Client could not be removed from lobby");
            }
        }

        [Server]
        internal bool TryRemoveClientFromLobby(NetworkConnection conn)
        {
            if (conn == null)
            {
                return false;
            }

            LobbyHandler lobbyHandler = _lobbyHandlers.First(pair  => pair.Value.GetPlayerState(conn.ClientId) != null).Value;
            
            if (lobbyHandler == null)
            {
                // Player is not a participant of any lobby
                return false;
            }

            SceneUnloadData sud = CreateUnloadData(lobbyHandler.MetaData.LobbyId);
            if (sud == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Can't unload connection scene. SceneHandle is null");
            }
            else
            {
                SceneManager.UnloadConnectionScenes(conn, sud);
            }

            OnLobbyLeftRpc(conn);

            return true;
        }

        [CanBeNull]
        private SceneUnloadData CreateUnloadData(Guid guid)
        {
            if (!_lobbies.TryGetValue(guid, out LobbyMetaData lmd))
            {
                return null;
            }

            if (lmd.SceneHandle == null)
            {
                return null;
            }

            SceneLookupData sld = new()
            {
                Handle = lmd.SceneHandle.Value, Name = lmd.ScenePath
            };
            object[] unloadParams =
            {
                SceneLoadParam.Lobby, lmd.LobbyId
            };
            SceneUnloadData sud = new(new[]
            {
                sld
            })
            {
                Options =
                {
                    Mode = UnloadOptions.ServerUnloadMode.KeepUnused
                },
                Params =
                {
                    ServerParams = unloadParams, ClientParams = LobbyMetaData.SerializeObjects(unloadParams)
                }
            };
            return sud;
        }

        /// <summary>
        ///     Checks if a lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="lobbyId">The ID of the lobby to close.</param>
        /// <param name="duration">Duration in seconds until expiration</param>
        [Server]
        private IEnumerator CloseInactiveLobby(Guid lobbyId, ushort duration)
        {
            float elapsed = 0;
            const float interval = 1;

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                Logger.Log(LogLevel.Error, Tag, $"Can't close lobby {lobbyId} due to inactivity. LobbyHandler not found");
                yield break;
            }

            while (elapsed < duration)
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Closing lobby {lobbyId} in {duration - elapsed} seconds due to inactivity.");
                if (handler.PlayerStates.Any())
                {
                    Logger.Log(LogLevel.Verbose, Tag, $"Cancel inactive lobby closing. Lobby {lobbyId} is no longer inactive.");
                    yield break;
                }

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            Logger.Log(LogLevel.Warning, Tag, $"Closing lobby {lobbyId} due to inactivity.");
            Server_CloseLobby(lobbyId);
        }

        [Server]
        public bool TryGetLobbyHandlerById(Guid lobbyId, out LobbyHandler res)
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
            return Instance;
        }
        
        [CanBeNull]
        [Client]
        public LobbyMetaData Client_GetCurrentLobby()
        {
            return _currentLobby;
        }

        public Dictionary<Guid, LobbyMetaData> GetLobbies()
        {
            return _lobbies.Collection;
        }

        public bool TryGetLobby(Guid lobbyId, out LobbyMetaData lmd)
        {
            return _lobbies.TryGetValue(lobbyId, out lmd);
        }

        #region Singleton

        internal static LobbyManager Instance;

        private void InitSingleton()
        {
            if (Instance != null)
            {
                Logger.Log(LogLevel.Warning, Tag, "Instance of LobbyManager already exists!");
                Destroy(this);
            }

            Instance = this;
        }

        #endregion

        #region SerializedFields

        [Tooltip("The Scene to load when the local client leaves their current lobby")] [SerializeField] [Scene]
        private string _offlineScene;

        [SerializeField] private List<LobbySceneMetaData> _lobbyScenes;


        [Header("Prefab Setup")] [SerializeField]
        private LobbyHandler _lobbyHandlerPrefab;

        #endregion

        #region ServerOnly

        private event Action<Guid> LobbyHandlerRegistered;

        /// <summary>
        ///     The actual lobby handlers.
        ///     Only initialized on the server.
        /// </summary>
        private Dictionary<Guid, LobbyHandler> _lobbyHandlers;

        private Dictionary<Guid, byte[]> _lobbyPasswordHashes;

        private Dictionary<Guid, Coroutine> _lobbyExpirationRoutines;

        private QuickConnectHandler _quickConnectHandler;

        #endregion
    }
}
