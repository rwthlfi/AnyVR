using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities.Types;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AnyVR.LobbySystem
{
    public class LobbyManager : NetworkBehaviour
    {
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
        ///     All available scenes for a lobby
        /// </summary>
        public LobbySceneMetaData[] LobbyScenes => lobbyScenes.ToArray();

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            LobbyClosed += lobbyId => { Logger.LogVerbose($"Lobby {lobbyId} closed"); };
            LobbyOpened += lobbyId => { Logger.LogVerbose($"Lobby {lobbyId} opened"); };
        }

        /// <summary>
        ///     Invoked when a remote client opened a new lobby
        /// </summary>
        public event Action<Guid> LobbyOpened;

        /// <summary>
        ///     Invoked when a remote client closed a lobby
        /// </summary>
        public event Action<Guid> LobbyClosed;

        /// <summary>
        ///     Invoked when the local client starts loading a lobby scene
        /// </summary>
        public event Action ClientLobbyLoadStart;

        /// <summary>
        ///     Invoked when the player-count of a lobby changes.
        ///     The local user does not have to be connected to the lobby.
        ///     (string: lobbyId, int: playerCount)
        /// </summary>
        public event Action<Guid, int> PlayerCountUpdate;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyHandlers = new Dictionary<Guid, LobbyHandler>();
            _clientLobbyDict = new Dictionary<int, Guid>();
            _lobbyPasswordHashes = new Dictionary<Guid, byte[]>();
            SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                TryRemoveClientFromLobby(conn);
            }
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
            if (args.QueueData.AsServer)
            {
                return;
            }

            if (_currentLobby == null)
            {
                return;
            }

            if (!IsUnloadingLobby(args.QueueData, false, out Guid _))
            {
                return;
            }
            AsyncOperation op = USceneManager.LoadSceneAsync(offlineScene, LoadSceneMode.Additive);
            if (op != null)
            {
                op.completed += _ =>
                {
                    USceneManager.SetActiveScene(USceneManager.GetSceneByPath(offlineScene));
                };
            }
        }

        private static bool IsUnloadingLobby(UnloadQueueData queueData, bool asServer, out Guid lobbyId)
        {
            object[] loadParams = asServer
                ? queueData.SceneUnloadData.Params.ServerParams
                : LobbyMetaData.DeserializeClientParams(queueData.SceneUnloadData.Params.ClientParams);

            lobbyId = Guid.Empty;

            if (loadParams.Length < 2 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }

            // Lobbies must have this flag
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.k_lobby)
            {
                return false;
            }

            if (loadParams[1] is not Guid)
            {
                return false;
            }

            lobbyId = (Guid)loadParams[1];
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
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.k_lobby)
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
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.LogWarning($"Can't register LobbyHandler. {errorMsg}");
                }

                return;
            }

            // Lobby scenes have to be loaded with exactly 0 clients
            if (loadArgs.QueueData.Connections.Length != 0)
            {
                Logger.LogWarning("Can't register LobbyHandler. The lobby must be empty.");
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
                Logger.LogWarning("Can't register LobbyHandler. The passed clientId is not connected to the server.");
                return;
            }

            // Spawn and register the LobbyHandler
            LobbyHandler lobbyHandler = Instantiate(lobbyHandlerPrefab);
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

            Logger.LogVerbose($"LobbyHandler with lobbyId '{lobbyId}' is registered");
            LobbyHandlerRegistered?.Invoke(lobbyId);
        }

        [ObserversRpc]
        private void OnLobbyPlayerCountUpdate(Guid lobby, ushort playerCount)
        {
            PlayerCountUpdate?.Invoke(lobby, playerCount);
        }

        [Server]
        private static Guid CreateLobbyId()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        ///     Initiates the creation of a new lobby on the server with a remote procedure call.
        /// </summary>
        /// <param name="lobbyName">The name of the lobby.</param>
        /// <param name="password">The password for the lobby (if any). Pass null or white space for no password.</param>
        /// <param name="scenePath">The scene to be loaded for the lobby.</param>
        /// <param name="maxClients">The maximum number of clients allowed in the lobby.</param>
        /// <param name="sceneName">The name of the scene to be loaded.</param>
        /// <param name="expirationDate">Optional expiration date for the lobby.</param>
        /// <param name="autoJoin">If the client should automatically join the lobby.</param>
        public void Client_CreateLobby(string lobbyName, string password, string scenePath, ushort maxClients, string sceneName,
            DateTime? expirationDate = null, bool autoJoin = false)
        {
            CreateLobby(lobbyName, password, scenePath, maxClients, sceneName, expirationDate, ClientManager.Connection, autoJoin);
        }

        /// <summary>
        ///     Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void CreateLobby(string lobbyName, string password, string scenePath, ushort maxClients, string sceneName,
            DateTime? expirationDate, NetworkConnection conn = null, bool autoJoin = false)
        {
            if (conn == null)
            {
                return;
            }

            StartCoroutine(Co_CreateLobby(lobbyName, password, scenePath, maxClients, sceneName, expirationDate, conn, autoJoin));
        }

        [Server]
        private IEnumerator Co_CreateLobby(string lobbyName, string password, string scenePath, ushort maxClients, string sceneName,
            DateTime? expirationDate, NetworkConnection conn = null, bool autoJoin = false)
        {
            if (conn is null)
            {
                yield break;
            }

            maxClients = (ushort)Mathf.Max(1, maxClients);
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = $"{PlayerNameTracker.GetPlayerName(conn.ClientId)}'s {sceneName}";
            }
            LobbyMetaData lobbyMetaData = new(CreateLobbyId(), lobbyName, conn.ClientId, scenePath, sceneName, maxClients,
                !string.IsNullOrWhiteSpace(password), expirationDate);
            _lobbies.Add(lobbyMetaData.LobbyId, lobbyMetaData);

            if (lobbyMetaData.IsPasswordProtected)
            {
                _lobbyPasswordHashes.Add(lobbyMetaData.LobbyId, ComputeSha256(password));
            }

            // Starts the lobby scene without clients. When loaded, the LoadEnd callback will be called and we spawn a LobbyHandler. After that clients are able to join.
            Logger.LogVerbose("Loading lobby scene. Waiting for lobby handler");
            SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), lobbyMetaData.GetSceneLoadData());

            // Wait for Lobby Handler
            float timeout = 20f;
            bool receivedLobbyHandler = false;
            LobbyHandlerRegistered += Handler;

            while (!receivedLobbyHandler && timeout > 0)
            {
                yield return null;
                timeout -= Time.deltaTime;
            }

            LobbyHandlerRegistered -= Handler;

            if (!receivedLobbyHandler)
            {
                CloseLobby(lobbyMetaData.LobbyId);
                Logger.LogWarning(
                    $"Lobby (id={lobbyMetaData.LobbyId}) could not be created. LobbyHandler was not received.");
                yield break;
            }

            Logger.LogVerbose($"Lobby created. {lobbyMetaData}");

            if (autoJoin)
            {
                AddClientToLobby(lobbyMetaData.LobbyId, password, conn);
            }

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

            Logger.LogVerbose($"Expire lobby {lobbyId} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.LogVerbose($"Lobby {lobbyId} expired");
            _lobbyExpirationRoutines.Remove(lobbyId);
            CloseLobby(lobbyId);
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
            LobbyOpened?.Invoke(id);
        }

        public void Client_JoinLobby(Guid lobbyId, string password = null)
        {
            Logger.LogVerbose("Requesting to join lobby");
            JoinLobby(lobbyId, password, ClientManager.Connection);
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
                Logger.LogWarning(
                    $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Lobby was not found.");
                return;
            }

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler _))
            {
                Logger.LogWarning(
                    $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. The corresponding lobby handler does not exist.");
                return;
            }

            int currentPlayerCount = _clientLobbyDict.Count(pair => pair.Value == lobbyId);
            if (currentPlayerCount >= lobby.LobbyCapacity)
            {
                Logger.LogWarning(
                    $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. The lobby is already full.");
                return;
            }

            if (!_clientLobbyDict.TryAdd(conn.ClientId, lobbyId))
            {
                Logger.LogWarning(
                    $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'.");
                return;
            }

            if (lobby.IsPasswordProtected)
            {
                if (!_lobbyPasswordHashes.TryGetValue(lobbyId, out byte[] passwordHash))
                {
                    Logger.LogError(
                        $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Lobby '{lobbyId}' is password protected but the password hash is not available.");
                    return;
                }

                if (passwordHash == null)
                {
                    Logger.LogError(
                        $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Password hash is null");
                    return;
                }

                if (!ComputeSha256(password).SequenceEqual(passwordHash))
                {
                    Logger.LogVerbose(
                        $"Client '{conn.ClientId}' could not be added to the lobby with lobbyId '{lobbyId}'. Password hashes do not match.");
                    return;
                }
            }

            Logger.LogVerbose(
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
            VoiceChatManager.GetInstance()?.TryConnectToRoom(lmd.LobbyId, ConnectionManager.UserName);
        }

        /// <summary>
        ///     Callback for when the local client leaves a lobby.
        /// </summary>
        [TargetRpc]
        private void OnLobbyLeftRpc(NetworkConnection _)
        {
            VoiceChatManager.GetInstance()?.Disconnect();
        }

        // private IEnumerator LoadWelcomeScene()
        // {
        //     AsyncOperation op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync("UIScene");
        //     while (op is { isDone: false })
        //     {
        //         yield return null;
        //     }
        //
        //     UnityEngine.SceneManagement.SceneManager.LoadScene(offlineScene, LoadSceneMode.Additive);
        // }

        [Server]
        private void CloseLobby(Guid lobbyId)
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
                    Logger.LogWarning($"Could not get NetworkConnection from client {client.id}");
                    continue;
                }

                TryRemoveClientFromLobby(clientConn);
            }

            SceneUnloadData sud = CreateUnloadData(lobbyId);
            _lobbyHandlers.Remove(lobbyId);
            _lobbies.Remove(lobbyId);
            _lobbyPasswordHashes.Remove(lobbyId);

            if (sud == null)
            {
                Logger.LogError("Can't unload connection scene. SceneHandle is null");
            }
            else
            {
                SceneManager.UnloadConnectionScenes(sud);
            }

            LobbyClosed?.Invoke(lobbyId);
            Logger.LogVerbose($"Lobby with id '{lobbyId}' is closed");
        }

        [ServerRpc(RequireOwnership = false)]
        internal void LeaveLobby(NetworkConnection conn = null)
        {
            if (!TryRemoveClientFromLobby(conn))
            {
                Logger.LogWarning("Client could not be removed from lobby");
            }
        }

        [Server]
        private bool TryRemoveClientFromLobby(NetworkConnection clientConnection)
        {
            if (clientConnection == null)
            {
                return false;
            }

            if (!TryGetLobbyIdOfClient(clientConnection.ClientId, out Guid lobbyId))
            {
                return false;
            }

            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return false;
            }

            handler.Server_RemoveClient(clientConnection.ClientId);
            _clientLobbyDict.Remove(clientConnection.ClientId);

            SceneUnloadData sud = CreateUnloadData(lobbyId);
            if (sud == null)
            {
                Logger.LogError("Can't unload connection scene. SceneHandle is null");
            }
            else
            {
                SceneManager.UnloadConnectionScenes(clientConnection, sud);
            }

            OnLobbyLeftRpc(clientConnection);

            if (!handler.GetClients().Any())
            {
                StartCoroutine(CloseInactiveLobby(lobbyId, 10));
            }

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

            SceneLookupData sld = new() { Handle = lmd.SceneHandle.Value, Name = lmd.ScenePath };
            object[] unloadParams = { SceneLoadParam.k_lobby, lmd.LobbyId };
            SceneUnloadData sud = new(new[] { sld })
            {
                Options = { Mode = UnloadOptions.ServerUnloadMode.KeepUnused },
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
                Logger.LogError($"Can't close lobby {lobbyId} due to inactivity. LobbyHandler not found");
                yield break;
            }

            while (elapsed < duration)
            {
                Logger.LogVerbose($"Closing lobby {lobbyId} in {duration - elapsed} seconds due to inactivity.");
                if (handler.GetClients().Length > 0)
                {
                    Logger.LogVerbose($"Cancel inactive lobby closing. Lobby {lobbyId} is no longer inactive.");
                    yield break;
                }

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            Logger.LogWarning($"Closing lobby {lobbyId} due to inactivity.");
            CloseLobby(lobbyId);
        }

        [Server]
        public bool TryGetLobbyIdOfClient(int clientId, out Guid lobbyId)
        {
            return _clientLobbyDict.TryGetValue(clientId, out lobbyId);
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
            return s_instance;
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

        public bool TryGetLobby(Guid lobbyId, out LobbyMetaData lmd) => _lobbies.TryGetValue(lobbyId, out lmd);

        #region Singleton

        internal static LobbyManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Logger.LogWarning("Instance of LobbyManager already exists!");
                Destroy(this);
            }

            s_instance = this;
        }

        #endregion

        #region SerializedFields

        [Tooltip("The Scene to load when the local client leaves their current lobby")] [SerializeField] [Scene]
        private string offlineScene;

        [SerializeField] private List<LobbySceneMetaData> lobbyScenes;


        [Header("Prefab Setup")] [SerializeField]
        private LobbyHandler lobbyHandlerPrefab;

        #endregion

        #region ServerOnly

        private event Action<Guid> LobbyHandlerRegistered;

        /// <summary>
        ///     The actual lobby handlers.
        ///     Only initialized on the server.
        /// </summary>
        private Dictionary<Guid, LobbyHandler> _lobbyHandlers;

        /// <summary>
        ///     A dictionary mapping clients to their corresponding lobby
        ///     Only initialized on the server.
        /// </summary>
        private Dictionary<int, Guid> _clientLobbyDict;

        private Dictionary<Guid, byte[]> _lobbyPasswordHashes;

        private Dictionary<Guid, Coroutine> _lobbyExpirationRoutines;

        #endregion
    }
}