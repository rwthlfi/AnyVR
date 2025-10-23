using System;
using System.Threading.Tasks;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal partial class LobbyManagerInternal
    {
        private TaskCompletionSource<CreateLobbyResult> _createLobbyTcs;

        private TaskCompletionSource<JoinLobbyResult> _joinLobbyTcs;

        public override void OnStartClient()
        {
            base.OnStartClient();

            _lobbyRegistry.OnLobbyRegistered += OnLobbyOpened;
            _lobbyRegistry.OnLobbyUnregistered += OnLobbyClosed;

            OnClientInitialized?.Invoke();
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

#region RPCs

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
    }
}
