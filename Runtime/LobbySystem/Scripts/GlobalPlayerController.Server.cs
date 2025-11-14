using System;
using System.Collections;
using System.Text.RegularExpressions;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class GlobalPlayerController
    {
        private Coroutine _kickPlayerCoroutine;

        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetName(string playerName)
        {
            PlayerNameUpdateResult result = SetName_Internal(playerName);

            LogPlayerNameUpdateResult(result);

            TargetRPC_OnNameChange(Owner, result);

            if (result == PlayerNameUpdateResult.Success || GetPlayerState<GlobalPlayerState>().Name != "null")
                return;

            if (_kickPlayerCoroutine != null)
            {
                StopCoroutine(_kickPlayerCoroutine);
                _kickPlayerCoroutine = null;
            }

            // Ensure the TargetRPC_OnNameChange is received before the kick.
            _kickPlayerCoroutine = StartCoroutine(KickIfNameNotSet(0.1f));
        }

        private IEnumerator KickIfNameNotSet(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (GetPlayerState<GlobalPlayerState>().Name != "null")
                yield break;

            Owner.Kick(KickReason.UnusualActivity, LoggingType.Warning, $"Kicking player {OwnerId}. Player did not send a name update.");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _kickPlayerCoroutine = StartCoroutine(KickIfNameNotSet(3));
        }

        [Server]
        private PlayerNameUpdateResult SetName_Internal(string playerName)
        {
            playerName = Regex.Replace(playerName.Trim(), @"\s+", " ");

            if (playerName.Equals(GetPlayerState<GlobalPlayerState>().Name))
            {
                return PlayerNameUpdateResult.AlreadySet;
            }

            PlayerNameUpdateResult result = PlayerNameValidator.ValidatePlayerName(playerName);
            if (result == PlayerNameUpdateResult.Success)
            {
                GetPlayerState<GlobalPlayerState>().SetName(playerName);
            }

            return result;
        }

#region RPCs

        [ServerRpc]
        private void ServerRPC_CreateLobby(string lobbyName, string password, int sceneId, ushort maxClients, DateTime? expirationDate)
        {
            LobbyManagerInternal.Instance.Server_CreateLobby(lobbyName, password, sceneId, maxClients, expirationDate, this);
        }

        [ServerRpc]
        private void ServerRPC_QuickConnect(uint quickConnect)
        {
            ILobbyInfo state = GlobalGameState.Instance.GetLobbyInfo(quickConnect);
            if (state == null)
            {
                ObserverRPC_OnJoinLobbyResult(JoinLobbyResult.LobbyDoesNotExist);
                return;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyManagerInternal), $"{OwnerId} connecting to lobby '{state.LobbyId} via quickConnect");
            // TODO: handle password protected lobbies
            LobbyManagerInternal.Instance.Server_JoinLobby(state.LobbyId, string.Empty, this);
        }

        [ServerRpc]
        private void ServerRPC_JoinLobby(Guid lobbyId, string password)
        {
            LobbyManagerInternal.Instance.Server_JoinLobby(lobbyId, password, this);
        }

#endregion
    }
}
