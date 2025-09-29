using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal class LobbyManagerInternal : NetworkBehaviour
    {
        private const string Tag = nameof(LobbyManagerInternal);

        [FormerlySerializedAs("_lobbyInfoPrefab")]
        [SerializeField] private LobbyState _lobbyStatePrefab;
        internal LobbyState LobbyStatePrefab => _lobbyStatePrefab;

        private TaskCompletionSource<JoinLobbyResult> _joinLobbyTcs;
        
        private TaskCompletionSource<CreateLobbyResult> _createLobbyTcs;

        private LobbyRegistry _lobbyRegistry;

        private LobbySceneService _sceneService;

        internal IReadOnlyDictionary<Guid, LobbyState> Lobbies => _lobbyRegistry.LobbyStates;

        public override void OnStartNetwork()
        {
            _lobbyRegistry = GetComponent<LobbyRegistry>();
            _sceneService = new LobbySceneService(this);
        }

        internal event Action OnClientInitialized;

        /// <summary>
        ///     Invoked when a remote client opened a new lobby
        /// </summary>
        internal event Action<Guid> OnLobbyOpened;

        /// <summary>
        ///     Invoked when a remote client closed a lobby
        /// </summary>
        internal event Action<Guid> OnLobbyClosed;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _quickConnectHandler = new QuickConnectHandler();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _lobbyRegistry.OnLobbyRegistered += OnLobbyOpened;
            _lobbyRegistry.OnLobbyUnregistered += OnLobbyClosed;

            OnClientInitialized?.Invoke();
        }

        /// <summary>
        ///     Initiates the creation of a new lobby on the server with a remote procedure call.
        /// </summary>
        /// <param name="lobbyName">The name of the lobby.</param>
        /// <param name="password">The password for the lobby (if any). Pass null or white space for no password.</param>
        /// <param name="sceneMetaData">The scene of the lobby</param>
        /// <param name="maxClients">The maximum number of clients allowed in the lobby.</param>
        /// <param name="expirationDate">Optional expiration date for the lobby.</param>
        /// <param name="timeout">Timeout for lobby creation</param>
        [Client]
        internal async Task<CreateLobbyResult> CreateLobby(string lobbyName, string password, LobbySceneMetaData sceneMetaData, ushort maxClients, DateTime? expirationDate = null, TimeSpan? timeout = null)
        {
            if (_createLobbyTcs != null && !_createLobbyTcs.Task.IsCompleted)
            {
                return new CreateLobbyResult(CreateLobbyStatus.CreationInProgress);
            }
            
            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                return new CreateLobbyResult(CreateLobbyStatus.InvalidParameters);
            }
            
            if (!sceneMetaData.IsValid())
            {
                return new CreateLobbyResult(CreateLobbyStatus.InvalidScene);
            }

            _createLobbyTcs = new TaskCompletionSource<CreateLobbyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            ServerRPC_CreateLobby(lobbyName, password, sceneMetaData.ID, maxClients, expirationDate);

            Task delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_createLobbyTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                _createLobbyTcs = null;
                return new CreateLobbyResult(CreateLobbyStatus.Timeout);
            }

            CreateLobbyResult result = await _createLobbyTcs.Task;
            _createLobbyTcs = null;

            return result;
        }

        /// <summary>
        ///     Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_CreateLobby(string lobbyName, string password, int sceneId, ushort maxClients, DateTime? expirationDate, NetworkConnection conn = null)
        {
            Server_CreateLobby(lobbyName, password, sceneId, maxClients, expirationDate, conn);
        }

        [Server]
        private async void Server_CreateLobby(string lobbyName, string password, int sceneId, ushort maxClients, DateTime? expirationDate, NetworkConnection creator)
        {
            if(GetLobbyStates().Any(lobbyState => lobbyState.Name.Value == lobbyName))
            {
                TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.LobbyNameTaken);
                return;
            }
            
            maxClients = (ushort)Mathf.Max(1, maxClients);
            LobbyState lobbyState = new LobbyFactory()
                .WithName(lobbyName)
                .WithCreator(creator.ClientId)
                .WithSceneID(sceneId)
                .WithCapacity(maxClients)
                .WithPasswordProtection(!string.IsNullOrWhiteSpace(password))
                .WithExpiration(expirationDate)
                .Create();
            
            Assert.IsNotNull(lobbyState);
            
            LobbyHandler handler = await _sceneService.StartConnectionScene(lobbyState);
            Assert.IsNotNull(handler, "Failed to load lobby scene");
            
            // Lobby scene successfully loaded.
            _lobbyRegistry.Register(lobbyState, handler, ComputeSha256(password));
            handler.Init(lobbyState.LobbyId, _quickConnectHandler.RegisterLobby(lobbyState.LobbyId));
            
            TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.Success, lobbyState.LobbyId);
        }

        [Client]
        private async Task<JoinLobbyResult> Client_JoinLobbyInternal(Action rpcCall, TimeSpan? timeout = null)
        {
            if (_joinLobbyTcs != null && !_joinLobbyTcs.Task.IsCompleted)
            {
                return new JoinLobbyResult(JoinLobbyStatus.AlreadyJoining);
            }

            _joinLobbyTcs = new TaskCompletionSource<JoinLobbyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            rpcCall?.Invoke();

            Task delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_joinLobbyTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                _joinLobbyTcs = null;
                return new JoinLobbyResult(JoinLobbyStatus.Timeout);
            }

            JoinLobbyResult result = await _joinLobbyTcs.Task;
            _joinLobbyTcs = null;

            return result;
        }

        [Client]
        internal Task<JoinLobbyResult> JoinLobby(Guid lobbyId, string password = null, TimeSpan? timeout = null)
        {
            return Client_JoinLobbyInternal(() => ServerRPC_JoinLobby(lobbyId, password, LocalConnection), timeout);
        }

        [Client]
        internal Task<JoinLobbyResult> QuickConnect(string quickConnectCode, TimeSpan? timeout = null)
        {
            quickConnectCode = quickConnectCode.Trim();

            if (!uint.TryParse(quickConnectCode, out uint code))
            {
                Logger.Log(LogLevel.Warning, Tag, $"QuickConnect failed: invalid code '{quickConnectCode}'");
                return Task.FromResult(new JoinLobbyResult(JoinLobbyStatus.InvalidFormat));
            }

            if (code >= 99999)
            {
                Logger.Log(LogLevel.Warning, Tag, $"QuickConnect failed: code out of range '{code}'");
                return Task.FromResult(new JoinLobbyResult(JoinLobbyStatus.OutOfRange));
            }

            return Client_JoinLobbyInternal(() => ServerRPC_QuickConnect(code, LocalConnection), timeout);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_QuickConnect(uint quickConnect, NetworkConnection conn)
        {
            Logger.Log(LogLevel.Warning, Tag, "Received quick connect RPC");
            if (_quickConnectHandler.TryGetLobbyId(quickConnect, out Guid lobbyId))
            {
                Logger.Log(LogLevel.Verbose, Tag, $"{conn.ClientId} connecting to lobby '{lobbyId} via quickConnect");
                // TODO: handle password protected lobbies
                JoinLobby_Internal(lobbyId, string.Empty, conn);
            }
            TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.LobbyDoesNotExist);
            Logger.Log(LogLevel.Warning, Tag, $"Error performing quickConnect with code {quickConnect}");
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            JoinLobby_Internal(lobbyId, password, conn);
        }

        private static byte[] ComputeSha256(string s)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        [Server]
        private void JoinLobby_Internal(Guid lobbyId, string password, NetworkConnection conn)
        {
            Assert.IsNotNull(conn);

            if (!_lobbyRegistry.LobbyStates.TryGetValue(lobbyId, out LobbyState lobby))
            {
                Logger.Log(LogLevel.Warning, Tag,
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Lobby was not found.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.LobbyDoesNotExist);
                return;
            }

            // TODO: capacity check
            // If capacity is full:
            // TargetRPC_OnJoinLobbyResult(conn, new JoinLobbyResult(JoinLobbyStatus.LobbyFull));
            // return;

            if (lobby.IsPasswordProtected.Value)
            {
                byte[] passwordHash = _lobbyRegistry.GetPasswordHash(lobbyId);
                Assert.IsTrue(password.Length > 0);

                if (!ComputeSha256(password).SequenceEqual(passwordHash))
                {
                    Logger.Log(LogLevel.Verbose, Tag,
                        $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Password mismatch.");
                    TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.PasswordMismatch);
                    return;
                }
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Client '{conn.ClientId}' joined lobby '{lobbyId}'.");

            TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.Success, lobbyId);

            // Only load scenes if successful
            SceneManager.LoadConnectionScenes(conn, LobbySceneService.CreateSceneLoadData(lobby)); // TODO move to LobbySceneService
        }

        [TargetRpc]
        private void TargetRPC_OnJoinLobbyResult(NetworkConnection _, JoinLobbyStatus status, Guid? lobbyId = null)
        {
            _joinLobbyTcs?.TrySetResult(new JoinLobbyResult(status, lobbyId));

            Assert.IsTrue(status != JoinLobbyStatus.Success || lobbyId.HasValue); // Success => HasValue
        }
        
        [TargetRpc]
        private void TargetRPC_OnCreateLobbyResult(NetworkConnection _, CreateLobbyStatus status, Guid? lobbyId = null)
        {
            _createLobbyTcs?.TrySetResult(new CreateLobbyResult(status, lobbyId));

            Assert.IsTrue(status != CreateLobbyStatus.Success || lobbyId.HasValue); // Success => HasValue
        }

        [Server]
        internal void Server_CloseLobby(Guid lobbyId)
        {
            LobbyHandler handler = _lobbyRegistry.GetLobbyHandler(lobbyId);
            Assert.IsNotNull(handler);

            // Kick all players from the lobby
            // TODO: Unloading lobby scene should auto remove players from lobbies.
            foreach (LobbyPlayerState player in handler.GetPlayerStates<LobbyPlayerState>())
            {
                handler.Server_RemovePlayer(player.ClientManager.Connection);
            }

            SceneUnloadData sud = LobbySceneService.CreateUnloadData(handler.LobbyInfo);
            Assert.IsNotNull(sud);

            _lobbyRegistry.Unregister(lobbyId);

            _quickConnectHandler.UnregisterLobby(lobbyId); // TODO move to lobby registry

            SceneManager.UnloadConnectionScenes(sud);

            Logger.Log(LogLevel.Verbose, Tag, $"Lobby with id '{lobbyId}' is closed");
        }

        internal LobbyState GetLobbyState(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyState(lobbyId);
        }
        
        internal IEnumerable<LobbyState> GetLobbyStates()
        {
            return _lobbyRegistry.LobbyStates.Values;
        }

        internal LobbyHandler GetLobbyHandler(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyHandler(lobbyId);
        }

        #region ServerOnly

        private event Action<Guid> LobbyHandlerRegistered;

        private QuickConnectHandler _quickConnectHandler;

        #endregion
    }
}
