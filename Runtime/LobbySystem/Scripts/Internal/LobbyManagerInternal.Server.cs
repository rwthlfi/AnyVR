using System;
using System.Linq;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal partial class LobbyManagerInternal
    {
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

            LobbyHandler handler = await _sceneService.StartLobbyScene(lobbyState);
            Assert.IsNotNull(handler, "Failed to load lobby scene");
            handler.Init(lobbyState);

            // Lobby scene successfully loaded.
            bool success = _lobbyRegistry.RegisterLobby(lobbyState, handler, password);
            Assert.IsTrue(success);

            TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.Success, lobbyState.LobbyId);
        }

        [Server]
        private void Server_JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
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

            LobbyHandler lobbyHandler = _lobbyRegistry.GetLobbyHandler(lobbyId);
            Assert.IsNotNull(lobbyHandler);

            if (lobbyHandler.GetPlayerStates().Count() >= state.LobbyCapacity)
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

            _sceneService.LoadLobbySceneForPlayer(conn, lobbyHandler);
        }

        [Server]
        public void RemovePlayerFromLobby(NetworkConnection conn, LobbyHandler lobbyHandler)
        {
            _sceneService.UnloadLobbySceneForPlayer(conn, lobbyHandler);
        }

        [Server]
        internal void Server_CloseLobby(Guid lobbyId)
        {
            LobbyHandler handler = _lobbyRegistry.GetLobbyHandler(lobbyId);
            Assert.IsNotNull(handler);

            _lobbyRegistry.UnregisterLobby(handler.State);
            Despawn(handler.State.NetworkObject, DespawnType.Destroy);

            _sceneService.UnloadLobby(handler);

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Lobby with id '{lobbyId}' is closed");
        }

        [Server]
        internal LobbyHandler GetLobbyHandler(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyHandler(lobbyId);
        }

#region RPCs

        /// <summary>
        ///     Server Rpc to create a new lobby on the server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_CreateLobby(string lobbyName, string password, int sceneId, ushort maxClients, DateTime? expirationDate, NetworkConnection conn = null)
        {
            Server_CreateLobby(lobbyName, password, sceneId, maxClients, expirationDate, conn);
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
            Server_JoinLobby(state.LobbyId, string.Empty, conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            Server_JoinLobby(lobbyId, password, conn);
        }

#endregion
    }
}
