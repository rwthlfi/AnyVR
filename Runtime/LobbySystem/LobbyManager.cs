using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        private readonly SyncDictionary<Guid, LobbyMetaData> _lobbyMeta = new();

        /// <summary>
        ///     Invoked when the player-count of a lobby changes.
        ///     The local user does not have to be connected to the lobby.
        /// </summary>
        public PlayerCountEvent PlayerCountUpdate;

        public IEnumerable<LobbyMetaData> Lobbies => _lobbyMeta.Values;

        /// <summary>
        ///     All available scenes for a lobby
        /// </summary>
        public LobbySceneMetaData[] LobbyScenes => _lobbyScenes.ToArray();

        private void Awake()
        {
            InitSingleton();
        }

        /// <summary>
        ///     This event is invoked after the (local) LobbyManager is spawned and initialized.
        /// </summary>
        public static event Action<LobbyManager> OnClientInitialized;

        /// <summary>
        ///     Invoked when a remote client opened a new lobby
        /// </summary>
        public event Action<LobbyMetaData> OnLobbyOpened;

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
        public event Action OnLobbyLeft;

        /// <summary>
        ///     Invoked when the local client starts loading a lobby scene
        /// </summary>
        public event Action ClientLobbyLoadStart;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _lobbyHandlers = new Dictionary<Guid, LobbyHandler>();
            _lobbyPasswordHashes = new Dictionary<Guid, byte[]>();
            _quickConnectHandler = new QuickConnectHandler();
            SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SceneManager.OnLoadStart += OnLoadStart;
            SceneManager.OnUnloadEnd += OnUnloadEnd;

            _lobbyMeta.OnChange += OnLobbyMetaChange;

            OnClientInitialized?.Invoke(this);
        }

        private void OnLobbyMetaChange(SyncDictionaryOperation op, Guid key, LobbyMetaData value, bool asServer)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    OnLobbyOpened?.Invoke(value);
                    break;
                case SyncDictionaryOperation.Clear:
                    break;
                case SyncDictionaryOperation.Remove:
                    OnLobbyClosed?.Invoke(key);
                    break;
                case SyncDictionaryOperation.Set:
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        [Client]
        private void OnLoadStart(SceneLoadStartEventArgs args)
        {
            if (IsLoadingLobby(args.QueueData, false, out _))
            {
                ClientLobbyLoadStart?.Invoke();
            }
        }

        [Client]
        private void OnUnloadEnd(SceneUnloadEndEventArgs args)
        {
            if (args.QueueData.AsServer)
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
            if (!_lobbyMeta.ContainsKey(lobbyId))
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

            if (!_quickConnectHandler.TryGetQuickConnectCode(lobbyId, out uint quickConnectCode))
            {
                Logger.Log(LogLevel.Error, Tag, "Can't register LobbyHandler. Corresponding QuickConnectCode does not exist.");
                return;
            }

            GameObject[] rootObjects = loadArgs.LoadedScenes[0].GetRootGameObjects();

            LobbyHandler lobbyHandler = null;
            foreach (GameObject root in rootObjects)
            {
                LobbyHandler comp = root.GetComponent<LobbyHandler>();
                if (comp == null)
                    continue;

                Debug.Log("Found: " + comp.name);
                lobbyHandler = comp;
                break;
            }

            Assert.IsNotNull(lobbyHandler);
            lobbyHandler.Init(lobbyId, quickConnectCode);
            lobbyHandler.OnPlayerJoin += _ =>
            {
                int currentPlayerCount = _lobbyHandlers[lobbyId].GetPlayerStates().Count();
                OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            };
            lobbyHandler.OnPlayerLeave += _ =>
            {
                int currentPlayerCount = _lobbyHandlers[lobbyId].GetPlayerStates().Count();
                OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            };

            _lobbyMeta[lobbyId].SetSceneHandle(loadArgs.LoadedScenes[0].handle);
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
        [Client]
        public void CreateLobby(string lobbyName, string password, LobbySceneMetaData sceneMetaData, ushort maxClients, DateTime? expirationDate = null)
        {
            ServerRPC_CreateLobby(lobbyName, password, sceneMetaData.ScenePath, maxClients, sceneMetaData.Name, expirationDate, ClientManager.Connection);
        }

        /// <summary>
        ///     Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_CreateLobby(string lobbyName, string password, string scenePath, ushort maxClients, string sceneName, DateTime? expirationDate, NetworkConnection conn = null)
        {
            StartCoroutine(Server_Co_CreateLobby(lobbyName, password, scenePath, maxClients, sceneName, expirationDate, conn));
        }

        [Server]
        private IEnumerator Server_Co_CreateLobby(string lobbyName, string password, string scenePath, ushort maxClients, string sceneName, DateTime? expirationDate, NetworkConnection creator)
        {
            if (string.IsNullOrEmpty(lobbyName))
            {
                lobbyName = $"{sceneName}"; //TODO
            }

            maxClients = (ushort)Mathf.Max(1, maxClients);
            LobbyMetaData lmd = new LobbyMetaData.Builder()
                .WithName(lobbyName)
                .WithCreator(creator.ClientId)
                .WithScene(scenePath, sceneName)
                .WithCapacity(maxClients)
                .WithPasswordProtection(!string.IsNullOrWhiteSpace(password))
                .WithExpiration(expirationDate)
                .Build();

            _lobbyMeta.Add(lmd.LobbyId, lmd);

            if (lmd.IsPasswordProtected)
            {
                _lobbyPasswordHashes.Add(lmd.LobbyId, ComputeSha256(password));
            }

            _quickConnectHandler.RegisterLobby(lmd.LobbyId);

            // Starts the lobby scene without clients. When loaded, the LoadEnd callback will be called, and we spawn a LobbyHandler.
            Logger.Log(LogLevel.Verbose, Tag, "Loading lobby scene. Waiting for lobby handler");
            SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), lmd.GetSceneLoadData());

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
                Server_CloseLobby(lmd.LobbyId);
                Logger.Log(LogLevel.Warning, Tag,
                    $"Lobby (id={lmd.LobbyId}) could not be created. LobbyHandler was not received.");
                yield break;
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Lobby created. {lmd}");

            yield break;

            void Handler(Guid lobbyId)
            {
                if (lobbyId == lmd.LobbyId)
                {
                    receivedLobbyHandler = true;
                }
            }
        }

        [Client]
        public void JoinLobby(Guid lobbyId, string password = null)
        {
            Logger.Log(LogLevel.Verbose, Tag, "Requesting to join lobby");
            JoinLobby(lobbyId, password, ClientManager.Connection);
        }

        [Client]
        public void QuickConnect(string quickConnectCode)
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
        public bool QuickConnect(ushort quickConnectCode)
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
                Server_AddClientToLobby(lobbyId, string.Empty, conn);
            }
            Logger.Log(LogLevel.Warning, Tag, $"Error performing quickConnect with code {quickConnect}");
        }

        [ServerRpc(RequireOwnership = false)]
        private void JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            Server_AddClientToLobby(lobbyId, password, conn);
        }

        private static byte[] ComputeSha256(string s)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        [Server]
        private void Server_AddClientToLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            Assert.IsNotNull(conn);

            if (!_lobbyMeta.TryGetValue(lobbyId, out LobbyMetaData lobby))
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
            TargetRPC_OnLobbyJoined(conn, lobby);
            SceneManager.LoadConnectionScenes(conn, lobby.GetSceneLoadData());
        }

        /// <summary>
        ///     Callback for when the local client joins a lobby.
        /// </summary>
        [TargetRpc]
        private void TargetRPC_OnLobbyJoined(NetworkConnection _, LobbyMetaData lmd)
        {
            Logger.Log(LogLevel.Verbose, Tag, $"Lobby joined '{lmd.LobbyId}'");
            OnLobbyJoined?.Invoke(lmd.LobbyId);
        }

        /// <summary>
        ///     Callback for when the local client leaves a lobby.
        /// </summary>
        [TargetRpc]
        private void TargetRPC_OnLobbyLeft(NetworkConnection _)
        {
            Logger.Log(LogLevel.Verbose, Tag, "Lobby left");
            OnLobbyLeft?.Invoke();
        }

        [Server]
        internal void Server_CloseLobby(Guid lobbyId)
        {
            if (!_lobbyHandlers.TryGetValue(lobbyId, out LobbyHandler handler))
            {
                return;
            }

            // Kick all players from the lobby
            foreach (PlayerState player in handler.GetPlayerStates())
            {
                if (!ServerManager.Clients.TryGetValue(player.GetID(), out NetworkConnection clientConn))
                {
                    Logger.Log(LogLevel.Warning, Tag, $"Could not get NetworkConnection from client {player.GetID()}");
                    continue;
                }

                Server_TryRemoveClientFromLobby(clientConn);
            }

            SceneUnloadData sud = CreateUnloadData(lobbyId);
            _lobbyHandlers.Remove(lobbyId);
            _lobbyMeta.Remove(lobbyId);
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

            Logger.Log(LogLevel.Verbose, Tag, $"Lobby with id '{lobbyId}' is closed");
        }

        [ServerRpc(RequireOwnership = false)]
        internal void ServerRPC_LeaveLobby(NetworkConnection conn = null)
        {
            if (!Server_TryRemoveClientFromLobby(conn))
            {
                Logger.Log(LogLevel.Warning, Tag, "Client could not be removed from lobby");
            }
        }

        [Server]
        internal bool Server_TryRemoveClientFromLobby(NetworkConnection conn)
        {
            if (conn == null)
            {
                return false;
            }

            LobbyHandler lobbyHandler = _lobbyHandlers.First(pair => pair.Value.GetPlayerState(conn.ClientId) != null).Value;

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

            TargetRPC_OnLobbyLeft(conn);

            return true;
        }

        [CanBeNull]
        private SceneUnloadData CreateUnloadData(Guid lobbyId)
        {
            if (!_lobbyMeta.TryGetValue(lobbyId, out LobbyMetaData lmd))
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


        [Server]
        internal bool Server_TryGetLobbyHandlerById(Guid lobbyId, out LobbyHandler res)
        {
            return _lobbyHandlers.TryGetValue(lobbyId, out res);
        }

        [Server]
        internal void Server_HandleClientDisconnect(NetworkConnection conn)
        {
            Server_TryRemoveClientFromLobby(conn);
        }

        [CanBeNull]
        public static LobbyManager GetInstance()
        {
            return Instance;
        }

        public bool TryGetLobbyMeta(Guid lobbyId, out LobbyMetaData lmd)
        {
            return _lobbyMeta.TryGetValue(lobbyId, out lmd);
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

        #endregion

        #region ServerOnly

        private event Action<Guid> LobbyHandlerRegistered;

        /// <summary>
        ///     The actual lobby handlers.
        ///     Only initialized on the server.
        /// </summary>
        private Dictionary<Guid, LobbyHandler> _lobbyHandlers;

        private Dictionary<Guid, byte[]> _lobbyPasswordHashes; // TODO: Move to LobbyHandler

        private QuickConnectHandler _quickConnectHandler;

        #endregion
    }
}
