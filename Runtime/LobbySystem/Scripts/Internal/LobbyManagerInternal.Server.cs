using System;
using System.Collections.Generic;
using System.Linq;
using AnyVR.Logging;
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
        internal async void Server_CreateLobby(string lobbyName, string password, int sceneId, ushort maxClients, DateTime? expirationDate, GlobalPlayerController creator)
        {
            if (string.IsNullOrWhiteSpace(lobbyName))
            {
                creator.ObserverRPC_OnCreateLobbyResult(CreateLobbyStatus.InvalidLobbyName);
            }

            // TODO: check lobby name against a blacklist?

            Assert.IsNotNull(GlobalGameState.Instance);

            if (GlobalGameState.Instance.GetLobbies().Any(lobbyState => lobbyState.Name.Value == lobbyName))
            {
                creator.ObserverRPC_OnCreateLobbyResult(CreateLobbyStatus.LobbyNameTaken);
                return;
            }

            Assert.IsNotNull(GlobalGameState.Instance.LobbyConfiguration);
            Assert.IsNotNull(GlobalGameState.Instance.LobbyConfiguration.LobbyScenes);
            
            LobbySceneMetaData scene = GlobalGameState.Instance.LobbyConfiguration.LobbyScenes.FirstOrDefault(s => s.ID == sceneId);
            if (scene == null)
            {
                creator.ObserverRPC_OnCreateLobbyResult(CreateLobbyStatus.InvalidScene);
                return;
            }

            maxClients = (ushort)Mathf.Max(1, maxClients);

            GlobalLobbyState gls = new LobbyFactory()
                .WithName(lobbyName)
                .WithCreator(creator.OwnerId)
                .WithScene(scene)
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

            creator.ObserverRPC_OnCreateLobbyResult(CreateLobbyStatus.Success, gls.LobbyId);
        }

        [Server]
        internal void Server_JoinLobby(Guid lobbyId, string password, GlobalPlayerController player)
        {
            Assert.IsNotNull(player);

            ILobbyInfo lobbyInfo = GlobalGameState.Instance.GetLobbyInfo(lobbyId);
            if (lobbyInfo == null)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{player.OwnerId}' could not be added to lobby '{lobbyId}'. Lobby was not found.");
                player.ObserverRPC_OnJoinLobbyResult(JoinLobbyResult.LobbyDoesNotExist);
                return;
            }

            IEnumerable<LobbyPlayerState> players = _lobbyRegistry.GetLobbyGameMode(lobbyId).GetGameState<LobbyState>().GetPlayerStates();
            if (players.Any(other => other.ID == player.OwnerId))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{player.OwnerId}' is already a participant in the lobby '{lobbyId}'.");
                player.ObserverRPC_OnJoinLobbyResult(JoinLobbyResult.AlreadyConnected);
                return;
            }

            if (lobbyInfo.NumPlayers.Value >= lobbyInfo.LobbyCapacity)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyManagerInternal),
                    $"Client '{player.OwnerId}' could not be added to lobby '{lobbyId}'. Lobby is full.");
                player.ObserverRPC_OnJoinLobbyResult(JoinLobbyResult.LobbyIsFull);
                return;
            }

            if (!_lobbyRegistry.ValidatePassword(lobbyId, password))
            {
                Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal),
                    $"Client '{player.OwnerId}' could not be added to lobby '{lobbyId}'. Password mismatch.");
                player.ObserverRPC_OnJoinLobbyResult(JoinLobbyResult.PasswordMismatch);
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Client '{player.OwnerId}' joined lobby '{lobbyId}'.");

            player.ObserverRPC_OnJoinLobbyResult(JoinLobbyResult.Success);

            LobbyGameMode lobbyGameMode = _lobbyRegistry.GetLobbyGameMode(lobbyId);
            Assert.IsNotNull(lobbyGameMode);
            _sceneService.LoadLobbySceneForPlayer(player.Owner, lobbyGameMode);
        }

        [Server]
        public void RemovePlayerFromLobby(LobbyPlayerState player)
        {
            LobbyGameMode lobbyGameMode = _lobbyRegistry.GetLobbyGameMode(player.LobbyId);
            Assert.IsNotNull(lobbyGameMode);

            _sceneService.UnloadLobbySceneForPlayer(ServerManager.Clients[player.ID], lobbyGameMode);
        }

        [Server]
        internal void Server_CloseLobby(Guid lobbyId)
        {
            GlobalLobbyState state = (GlobalLobbyState)GlobalGameState.Instance.GetLobbyInfo(lobbyId);
            Assert.IsNotNull(state);

            LobbyGameMode gameMode = _lobbyRegistry.GetLobbyGameMode(lobbyId);
            Assert.IsNotNull(gameMode);
            _sceneService.UnloadLobby(gameMode);

            _lobbyRegistry.UnregisterLobby(state);
            Despawn(state.NetworkObject, DespawnType.Destroy);

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"Lobby with id '{lobbyId}' is closed");
        }
    }
}
