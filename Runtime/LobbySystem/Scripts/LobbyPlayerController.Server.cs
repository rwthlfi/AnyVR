using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using FishNet.Object;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerController
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            SpawnAvatar();
        }

        [Server]
        private void SpawnAvatar()
        {
            if (GetPlayerState<LobbyPlayerState>().GetAvatar() != null)
                return;

            if (_playerAvatarPrefab == null)
                return;

            NetworkObject nob = Instantiate(_playerAvatarPrefab);
            GetPlayerState<LobbyPlayerState>().SetAvatar(nob);
            Spawn(nob, Owner, gameObject.scene);
        }

#region RPCs

        [ServerRpc]
        private void ServerRPC_PromoteToAdmin(LobbyPlayerState other)
        {
            if (other.IsAdmin)
            {
                TargetRPC_OnPromotionResult(Owner, PlayerPromotionResult.AlreadyAdmin);
                return;
            }

            if (!GetPlayerState<LobbyPlayerState>().IsAdmin)
            {
                TargetRPC_OnPromotionResult(Owner, PlayerPromotionResult.InsufficientPermissions);
                return;
            }

            other.SetIsAdmin(true);
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Player {OwnerId} ({other.Global.Name}) promoted to admin.");
            TargetRPC_OnPromotionResult(Owner, PlayerPromotionResult.Success);
        }

        [ServerRpc]
        private void ServerRPC_RequestLiveKitToken()
        {
            string playerName = GetPlayerState<LobbyPlayerState>().Global.Name;
            Assert.IsNotNull(playerName);

            string token = this.GetGameMode<LobbyGameMode>().GenerateLiveKitToken(GetPlayerState<LobbyPlayerState>());

            if (string.IsNullOrEmpty(token))
            {
                TargetRPC_OnTokenResult(Owner, TokenState.TokenRetrievalFailed);
            }
            else
            {
                TargetRPC_OnTokenResult(Owner, TokenState.Success, token);
            }
        }

        [ServerRpc]
        private void ServerRPC_KickPlayer(LobbyPlayerState other)
        {
            if (!GetPlayerState<LobbyPlayerState>().IsAdmin)
            {
                TargetRPC_OnKickResult(Owner, PlayerKickResult.InsufficientPermissions);
                return;
            }

            LobbyManagerInternal.Instance.RemovePlayerFromLobby(other);
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Player {OwnerId} ({other.Global.Name}) kicked.");
            TargetRPC_OnKickResult(Owner, PlayerKickResult.Success);
        }

        [ServerRpc]
        private void ServerRPC_LeaveLobby()
        {
            LobbyManagerInternal.Instance.RemovePlayerFromLobby(GetPlayerState<LobbyPlayerState>());
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            NetworkObject avatar = GetPlayerState<LobbyPlayerState>().GetAvatar();
            Despawn(avatar, DespawnType.Destroy);
        }

  #endregion
    }
}
