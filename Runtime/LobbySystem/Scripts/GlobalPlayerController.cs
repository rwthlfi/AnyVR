using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     TODO
    /// </summary>
    public partial class GlobalPlayerController : PlayerController
    {
        private readonly RpcAwaiter<PlayerNameUpdateResult> _playerNameUpdateAwaiter = new(PlayerNameUpdateResult.Timeout, PlayerNameUpdateResult.Cancelled);

        private TaskCompletionSource<CreateLobbyResult> _createLobbyTcs;

        private TaskCompletionSource<JoinLobbyResult> _joinLobbyTcs;

        public static GlobalPlayerController Instance { get; private set; }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsNull(Instance);
            Instance = this;

            SceneManager.OnLoadStart += Client_OnLoadStart;
            SceneManager.OnLoadEnd += Client_OnLoadEnd;
            SceneManager.OnUnloadEnd += Client_OnUnloadEnd;
        }

        [TargetRpc]
        private void TargetRPC_OnNameChange(NetworkConnection _, PlayerNameUpdateResult playerNameUpdateResult)
        {
            _playerNameUpdateAwaiter?.Complete(playerNameUpdateResult);
        }

        [Client]
        internal async Task<CreateLobbyResult> Client_CreateLobby(string lobbyName, string password, LobbySceneMetaData sceneMetaData, ushort maxClients, DateTime? expirationDate = null, TimeSpan? timeout = null)
        {
            if (_createLobbyTcs != null && !_createLobbyTcs.Task.IsCompleted)
            {
                return new CreateLobbyResult(CreateLobbyStatus.CreationInProgress);
            }

            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                return new CreateLobbyResult(CreateLobbyStatus.InvalidLobbyName);
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
        private async Task<JoinLobbyResult> Client_JoinLobby(Action rpcCall, TimeSpan? timeout = null)
        {
            if (_joinLobbyTcs != null && !_joinLobbyTcs.Task.IsCompleted)
            {
                return JoinLobbyResult.AlreadyJoining;
            }

            _joinLobbyTcs = new TaskCompletionSource<JoinLobbyResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            rpcCall?.Invoke();

            Task delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_joinLobbyTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                _joinLobbyTcs = null;
                return JoinLobbyResult.Timeout;
            }

            JoinLobbyResult result = await _joinLobbyTcs.Task;
            _joinLobbyTcs = null;

            return result;
        }

        [Client]
        internal Task<JoinLobbyResult> Client_QuickConnect(string quickConnectCode, TimeSpan? timeout = null)
        {
            quickConnectCode = quickConnectCode.Trim();

            if (!uint.TryParse(quickConnectCode, out uint code))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal), $"QuickConnect failed: invalid code '{quickConnectCode}'");
                return Task.FromResult(JoinLobbyResult.InvalidQuickConnectFormat);
            }

            if (code >= 99999)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal), $"QuickConnect failed: code out of range '{code}'");
                return Task.FromResult(JoinLobbyResult.QuickConnectOutOfRange);
            }

            return Client_JoinLobby(() => ServerRPC_QuickConnect(code), timeout);
        }

#region Public API

        /// <summary>
        ///     Updates the player's name.
        ///     Does not succeed if this is not the local player's global state.
        /// </summary>
        /// <param name="playerName">The desired name</param>
        /// <returns>A PlayerNameUpdateResult indicating the result of the update.</returns>
        [Client]
        public Task<PlayerNameUpdateResult> SetName(string playerName)
        {
            Task<PlayerNameUpdateResult> task = _playerNameUpdateAwaiter.WaitForResult();
            ServerRPC_SetName(playerName);
            return task;
        }

        /// <summary>
        ///     Initiates the creation of a new lobby on the server via a remote procedure call.
        ///     Lobby creation will fail if the given name is already in use.
        /// </summary>
        /// <param name="lobbyName">The desired name of the lobby.</param>
        /// <param name="password">The password for the lobby. Pass null or white space for no password.</param>
        /// <param name="sceneMeta">The scene metadata of the lobby.</param>
        /// <param name="maxClients">The maximum number of clients allowed in the lobby.</param>
        /// <returns>An asynchronous task that returns the result of the lobby creation.</returns>
        public async Task<CreateLobbyResult> CreateLobby(string lobbyName, string password, LobbySceneMetaData sceneMeta, ushort maxClients)
        {
            CreateLobbyResult result = await Client_CreateLobby(lobbyName, password, sceneMeta, maxClients);
            LogCreateLobbyResult(result);
            return result;
        }

        /// <summary>
        ///     Attempts to join an existing lobby on the server.
        ///     <param name="lobby">The lobby to join.</param>
        ///     <param name="password">Pass a password if the target lobby is protected by one.</param>
        ///     <returns>An asynchronous task that returns the result of the join process.</returns>
        /// </summary>
        public Task<JoinLobbyResult> JoinLobby(ILobbyInfo lobby, string password = null)
        {
            return JoinLobby(lobby.LobbyId, password);
        }

        /// <summary>
        ///     Attempts to join an existing lobby on the server.
        ///     <param name="lobbyId">The id of the lobby to join.</param>
        ///     <param name="password">Pass a password if the target lobby is protected by one.</param>
        ///     <returns>An asynchronous task that returns the result of the join process.</returns>
        /// </summary>
        public async Task<JoinLobbyResult> JoinLobby(Guid lobbyId, string password = null)
        {
            JoinLobbyResult result = await Client_JoinLobby(() => ServerRPC_JoinLobby(lobbyId, password));
            LogJoinLobbyResult(result);
            return result;
        }

        /// <summary>
        ///     Attempts to join an existing lobby on the server using the lobby's quick connect code.
        ///     <param name="quickConnectCode">The quick connect code of the target lobby.</param>
        ///     <returns>An asynchronous task that returns the result of the join process.</returns>
        /// </summary>
        [Client]
        public async Task<JoinLobbyResult> QuickConnect(string quickConnectCode)
        {
            JoinLobbyResult result = await Client_QuickConnect(quickConnectCode);
            LogJoinLobbyResult(result);
            return result;
        }

#endregion

#region RPCs

        // The owner is the only observer
        [ObserversRpc]
        internal void ObserverRPC_OnCreateLobbyResult(CreateLobbyStatus status, Guid? lobbyId = null)
        {
            _createLobbyTcs?.TrySetResult(new CreateLobbyResult(status, lobbyId));
            Assert.IsTrue(status != CreateLobbyStatus.Success || lobbyId.HasValue); // Success => HasValue
        }

        // The owner is the only observer
        [ObserversRpc]
        internal void ObserverRPC_OnJoinLobbyResult(JoinLobbyResult result)
        {
            _joinLobbyTcs?.TrySetResult(result);
        }

#endregion

#region Logs

        [Conditional("ANY_VR_LOG")]
        private static void LogJoinLobbyResult(JoinLobbyResult result)
        {
            Logger.Log(LogLevel.Verbose, nameof(GlobalPlayerController), result.ToFriendlyString());
        }

        [Conditional("ANY_VR_LOG")]
        private static void LogCreateLobbyResult(CreateLobbyResult result)
        {
            Logger.Log(LogLevel.Verbose, nameof(GlobalPlayerController), result.ToFriendlyString());
        }

        [Conditional("ANY_VR_LOG")]
        private static void LogPlayerNameUpdateResult(PlayerNameUpdateResult result)
        {
            Logger.Log(LogLevel.Verbose, nameof(GlobalPlayerController), result.ToFriendlyString());
        }

#endregion
    }
}
