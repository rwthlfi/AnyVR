using System.Linq;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
        public override void OnStartServer()
        {
            base.OnStartServer();

            // Initialize replicated fields
            LobbyHandler lobbyHandler =
                gameObject.scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<LobbyHandler>())
                    .FirstOrDefault(comp => comp != null);

            Assert.IsNotNull(lobbyHandler, "LobbyHandler not found. Ensure there is one LobbyHandler placed in the lobby scene.");

            _lobbyId.Value = lobbyHandler.LobbyInfo.LobbyId;
            _isAdmin.Value = lobbyHandler.LobbyInfo.CreatorId == OwnerId;

            SpawnAvatar();
        }

        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            if (conn == Owner)
            {
                Despawn(_playerAvatar);
            }
        }


        [Server]
        private void SpawnAvatar()
        {
            if (_playerAvatar != null)
                return;

            if (_playerPrefab == null)
                return;

            NetworkObject nob = Instantiate(_playerPrefab);
            _playerAvatar = nob;
            Spawn(nob, Owner, gameObject.scene);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_PromoteToAdmin(NetworkConnection caller = null)
        {
            if (_isAdmin.Value)
            {
                TargetRPC_OnPromotionResult(caller, PlayerPromotionResult.AlreadyAdmin);
                return;
            }

            if (!Server_IsCallerAdmin(caller))
            {
                TargetRPC_OnPromotionResult(caller, PlayerPromotionResult.InsufficientPermissions);
                return;
            }

            PromoteToAdmin_Internal();
            TargetRPC_OnPromotionResult(caller, PlayerPromotionResult.Success);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_KickPlayer(NetworkConnection caller = null)
        {
            if (!Server_IsCallerAdmin(caller))
            {
                TargetRPC_OnKickResult(caller, PlayerKickResult.InsufficientPermissions);
                return;
            }

            KickPlayer_Internal();
            TargetRPC_OnKickResult(caller, PlayerKickResult.Success);
        }

        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetVoiceId(string id)
        {
            _voiceId.Value = id;
        }

        [Server]
        private void PromoteToAdmin_Internal()
        {
            _isAdmin.Value = true;
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({Global.GetName()}) promoted to admin.");
        }

        [Server]
        private void KickPlayer_Internal()
        {
            LobbyManager.Instance.Internal.RemovePlayerFromLobby(Owner, LobbyHandler);
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({Global.GetName()}) kicked.");
        }

        [Server]
        private bool Server_IsCallerAdmin(NetworkConnection conn)
        {
            LobbyPlayerState caller = LobbyHandler.GetPlayerState<LobbyPlayerState>(conn.ClientId);
            return caller.GetIsAdmin();
        }
    }
}
