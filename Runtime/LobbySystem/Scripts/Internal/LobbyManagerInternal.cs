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
        public LobbyState LobbyStatePrefab => _lobbyStatePrefab;

        private TaskCompletionSource<JoinLobbyResult> _lobbyJoinTcs;

        private LobbyRegistry _lobbyRegistry;

        private LobbySceneService _sceneService;

        public IReadOnlyDictionary<Guid, LobbyState> Lobbies => _lobbyRegistry.Lobbies;

        public override void OnStartNetwork()
        {
            _lobbyRegistry = GetComponent<LobbyRegistry>();
            _sceneService = new LobbySceneService(this);
        }

        internal event Action OnClientInitialized;

        /// <summary>
        ///     Invoked when a remote client opened a new lobby
        /// </summary>
        public event Action<Guid> OnLobbyOpened;

        /// <summary>
        ///     Invoked when a remote client closed a lobby
        /// </summary>
        public event Action<Guid> OnLobbyClosed;

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

        [ObserversRpc]
        private void OnLobbyPlayerCountUpdate(Guid lobby, ushort playerCount)
        {
            //PlayerCountUpdate?.Invoke(lobby, playerCount);
            // TODO
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
            // todo check that lobbyName not null or whitespace
            ServerRPC_CreateLobby(lobbyName, password, sceneMetaData.ID, maxClients, expirationDate, LocalConnection);
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
            Debug.Log(lobbyState.LobbyId);
            Debug.Log(lobbyState.Scene.ScenePath);

            LobbyHandler handler = await _sceneService.StartConnectionScene(lobbyState);

            if (handler == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Failed to start lobby scene");
                return;
            }

            // Lobby scene successfully loaded.
            _lobbyRegistry.Register(lobbyState, handler, ComputeSha256(password));
            handler.Init(lobbyState.LobbyId, _quickConnectHandler.RegisterLobby(lobbyState.LobbyId));
        }

        [Client]
        private async Task<JoinLobbyResult> Client_JoinLobbyInternal(Action rpcCall, TimeSpan? timeout = null)
        {
            if (_lobbyJoinTcs != null && !_lobbyJoinTcs.Task.IsCompleted)
            {
                return new JoinLobbyResult(JoinLobbyStatus.AlreadyJoining);
            }

            _lobbyJoinTcs = new TaskCompletionSource<JoinLobbyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            rpcCall?.Invoke();

            Task delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_lobbyJoinTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                _lobbyJoinTcs = null;
                return new JoinLobbyResult(JoinLobbyStatus.Timeout);
            }

            JoinLobbyResult result = await _lobbyJoinTcs.Task;
            _lobbyJoinTcs = null;

            if (result.Status.Equals(JoinLobbyStatus.Success))
            {
                Assert.IsTrue(result.LobbyId.HasValue);
            }

            LogJoinResult(result);

            return result;
        }

        [Conditional("ANY_VR_LOG")]
        private static void LogJoinResult(JoinLobbyResult result)
        {
            string message = result.Status switch
            {
                JoinLobbyStatus.Success => $"Successfully joined lobby {result.LobbyId.GetValueOrDefault()}.",
                JoinLobbyStatus.AlreadyConnected => "You are already connected.",
                JoinLobbyStatus.LobbyDoesNotExist => "The lobby does not exist.",
                JoinLobbyStatus.LobbyIsFull => "The lobby is full.",
                JoinLobbyStatus.PasswordMismatch => "Incorrect lobby password.",
                JoinLobbyStatus.AlreadyJoining => "Already attempting to join a lobby.",
                JoinLobbyStatus.Timeout => "Server did not handle join request (timeout).",
                JoinLobbyStatus.InvalidFormat => "Quick connect code has an invalid format.",
                JoinLobbyStatus.OutOfRange => "Quick connect code is out of range.",
                _ => throw new ArgumentOutOfRangeException()
            };

            Logger.Log(LogLevel.Verbose, Tag, message);
        }

        [Client]
        public Task<JoinLobbyResult> JoinLobby(Guid lobbyId, string password = null, TimeSpan? timeout = null)
        {
            Logger.Log(LogLevel.Verbose, Tag, $"Requesting to join lobby {lobbyId}.");
            return Client_JoinLobbyInternal(() => ServerRPC_JoinLobby(lobbyId, password, LocalConnection), timeout);
        }

        [Client]
        public Task<JoinLobbyResult> QuickConnect(string quickConnectCode, TimeSpan? timeout = null)
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

            if (!_lobbyRegistry.Lobbies.TryGetValue(lobbyId, out LobbyState lobby))
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
            _lobbyJoinTcs?.TrySetResult(new JoinLobbyResult(status, lobbyId));

            Assert.IsTrue(status != JoinLobbyStatus.Success || lobbyId.HasValue); // Success => HasValue
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

        internal LobbyState GetLobbyMeta(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyMetaData(lobbyId);
        }

        public LobbyHandler GetLobbyHandler(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyHandler(lobbyId);
        }

        #region ServerOnly

        private event Action<Guid> LobbyHandlerRegistered;

        private QuickConnectHandler _quickConnectHandler;

        #endregion
    }
}
