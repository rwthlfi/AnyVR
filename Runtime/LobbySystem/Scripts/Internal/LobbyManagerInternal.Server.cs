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
            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.InvalidLobbyName);
            }

            // TODO: check lobby name against a blacklist?

            if (GetLobbyStates().Any(lobbyState => lobbyState.Name.Value == lobbyName))
            {
                TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.LobbyNameTaken);
                return;
            }

            maxClients = (ushort)Mathf.Max(1, maxClients);

            GlobalLobbyState gls = new LobbyFactory()
                .WithName(lobbyName)
                .WithCreator(creator.ClientId)
                .WithSceneID(sceneId)
                .WithCapacity(maxClients)
                .WithPasswordProtection(!string.IsNullOrWhiteSpace(password))
                .WithExpiration(expirationDate)
                .Create();

            // Loading the lobby scene.
            LobbyGameMode gameMode = await _sceneService.StartLobbyScene(gls);

            Assert.IsNotNull(gameMode, "Failed to load lobby scene");

            bool success = _lobbyRegistry.RegisterLobby(gls, gameMode, password);
            Assert.IsTrue(success);

            gameMode.SetLobbyId(gls.LobbyId);
            gameMode.OnBeginPlay();

            TargetRPC_OnCreateLobbyResult(creator, CreateLobbyStatus.Success, gls.LobbyId);
        }

        [Server]
        private void Server_JoinLobby(Guid lobbyId, string password, NetworkConnection conn)
        {
            Assert.IsNotNull(conn);

            GlobalLobbyState state = _lobbyRegistry.GetLobbyState(lobbyId);
            if (state == null)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Lobby was not found.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyResult.LobbyDoesNotExist);
                return;
            }

            if (state.NumPlayers.Value >= state.LobbyCapacity)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Lobby is full.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyResult.LobbyIsFull);
                return;
            }

            if (!_lobbyRegistry.ValidatePassword(lobbyId, password))
            {
                Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal),
                    $"Client '{conn.ClientId}' could not be added to lobby '{lobbyId}'. Password mismatch.");
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyResult.PasswordMismatch);
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Client '{conn.ClientId}' joined lobby '{lobbyId}'.");

            TargetRPC_OnJoinLobbyResult(conn, JoinLobbyResult.Success);

            LobbyGameMode lobbyGameMode = _lobbyRegistry.GetLobbyGameMode(lobbyId);
            Assert.IsNotNull(lobbyGameMode);
            _sceneService.LoadLobbySceneForPlayer(conn, lobbyGameMode);
        }

        [Server]
        public void RemovePlayerFromLobby(LobbyPlayerState player)
        {
            LobbyGameMode lobbyGameMode = _lobbyRegistry.GetLobbyGameMode(player.LobbyId);
            Assert.IsNotNull(lobbyGameMode);

            _sceneService.UnloadLobbySceneForPlayer(player.Owner, lobbyGameMode);
        }

        [Server]
        internal void Server_CloseLobby(Guid lobbyId)
        {
            GlobalLobbyState state = _lobbyRegistry.GetLobbyState(lobbyId);
            Assert.IsNotNull(state);

            _lobbyRegistry.UnregisterLobby(state);
            Despawn(state.NetworkObject, DespawnType.Destroy);

            LobbyGameMode gameMode = _lobbyRegistry.GetLobbyGameMode(lobbyId);
            Assert.IsNotNull(gameMode);
            _sceneService.UnloadLobby(gameMode);

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Lobby with id '{lobbyId}' is closed");
        }

        [Server]
        internal LobbyGameMode GetLobbyGameMode(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyGameMode(lobbyId);
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
            GlobalLobbyState state = _lobbyRegistry.GetLobbyState(quickConnect);
            if (state == null)
            {
                TargetRPC_OnJoinLobbyResult(conn, JoinLobbyResult.LobbyDoesNotExist);
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
