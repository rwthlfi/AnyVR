using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal class LobbyManagerInternal : NetworkBehaviour
    {
#region Serialized Fields

        [SerializeField] internal LobbyState _lobbyStatePrefab;

#endregion

#region Lobby Accessors

        internal LobbyState GetLobbyState(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyState(lobbyId);
        }

        internal IEnumerable<LobbyState> GetLobbyStates()
        {
            return _lobbyRegistry.GetLobbyStates();
        }

        [Server]
        internal LobbyHandler GetLobbyHandler(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyHandler(lobbyId);
        }

#endregion

#region Private Fields

        private TaskCompletionSource<CreateLobbyResult> _createLobbyTcs;

        private TaskCompletionSource<JoinLobbyResult> _joinLobbyTcs;

        private LobbyRegistry _lobbyRegistry;

        private LobbySceneService _sceneService;

#endregion

#region Internal Callbacks

        internal event Action OnClientInitialized;

        internal event Action<Guid> OnLobbyOpened;

        internal event Action<Guid> OnLobbyClosed;

#endregion

#region Lifecycle Overrides

        public override void OnStartNetwork()
        {
            _lobbyRegistry = GetComponent<LobbyRegistry>();
            _sceneService = new LobbySceneService(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _lobbyRegistry.OnLobbyRegistered += OnLobbyOpened;
            _lobbyRegistry.OnLobbyUnregistered += OnLobbyClosed;

            OnClientInitialized?.Invoke();
        }

#endregion

#region Client Methods

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
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal), $"QuickConnect failed: invalid code '{quickConnectCode}'");
                return Task.FromResult(new JoinLobbyResult(JoinLobbyStatus.InvalidFormat));
            }

            if (code >= 99999)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal), $"QuickConnect failed: code out of range '{code}'");
                return Task.FromResult(new JoinLobbyResult(JoinLobbyStatus.OutOfRange));
            }

            return Client_JoinLobbyInternal(() => ServerRPC_QuickConnect(code, LocalConnection), timeout);
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

#endregion

#region Server Methods

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
            if (GetLobbyStates().Any(lobbyState => lobbyState.Name.Value == lobbyName))
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
            handler.Init(lobbyState);

            // Lobby scene successfully loaded.
            bool success = _lobbyRegistry.Register(lobbyState, handler, password);
            Assert.IsTrue(success);

            TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.Success, lobbyState.LobbyId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_QuickConnect(uint quickConnect, NetworkConnection conn)
        {
            LobbyState state = _lobbyRegistry.GetLobbyState(quickConnect);
            if (state == null)
            {
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.LobbyDoesNotExist);
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"{conn.ClientId} connecting to lobby '{state.LobbyId} via quickConnect");
            // TODO: handle password protected lobbies
            JoinLobby_Internal(state.LobbyId, string.Empty, conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            JoinLobby_Internal(lobbyId, password, conn);
        }

        [Server]
        private void JoinLobby_Internal(Guid lobbyId, string password, NetworkConnection conn)
        {
            Assert.IsNotNull(conn);

            LobbyState state = _lobbyRegistry.GetLobbyState(lobbyId);
            if (state == null)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Lobby was not found.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.LobbyDoesNotExist);
                return;
            }

            LobbyHandler handler = _lobbyRegistry.GetLobbyHandler(lobbyId);
            Assert.IsNotNull(handler);

            if (handler.GetPlayerStates().Count() >= state.LobbyCapacity)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Lobby is full.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.LobbyIsFull);
                return;
            }

            if (!_lobbyRegistry.ValidatePassword(lobbyId, password))
            {
                Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal),
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Password mismatch.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.PasswordMismatch);
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Client '{conn.ClientId}' joined lobby '{lobbyId}'.");

            TargetRPC_OnJoinLobbyResult(conn, JoinLobbyStatus.Success, lobbyId);

            _sceneService.LoadPlayerIntoLobby(conn, state);
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

            _lobbyRegistry.Unregister(handler.State);

            SceneManager.UnloadConnectionScenes(sud);

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Lobby with id '{lobbyId}' is closed");
        }

#endregion
    }
}
