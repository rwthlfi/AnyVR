using System;
using System.Linq;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public class LobbyPlayerState : NetworkBehaviour
    {
#region Serialized Properties

        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [SerializeField] private NetworkObject _playerPrefab;

#endregion

        private readonly RpcAwaiter<PlayerKickResult> _playerKickUpdateAwaiter = new(PlayerKickResult.Timeout, PlayerKickResult.Cancelled);
        private readonly RpcAwaiter<PlayerPromotionResult> _playerPromoteUpdateAwaiter = new(PlayerPromotionResult.Timeout, PlayerPromotionResult.Cancelled);

#region Replicated Properties

        private readonly SyncVar<bool> _isAdmin = new(); // WritePermission is ServerOnly by default

        private readonly SyncVar<Guid> _lobbyId = new(Guid.Empty);

        private NetworkObject _playerAvatar; // TODO add player avatar class

#endregion

#region Lifecycle Overrides

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Global = GlobalGameState.Instance.GetPlayerState(OwnerId);
            Assert.IsNotNull(Global);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            // Initialize replicated fields
            LobbyHandler lobbyHandler =
                gameObject.scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<LobbyHandler>())
                    .FirstOrDefault(comp => comp != null);

            Assert.IsNotNull(lobbyHandler, "LobbyHandler not found. Ensure there is one LobbyHandler placed in the lobby scene.");

            _lobbyId.Value = lobbyHandler.GetLobbyId();
            _isAdmin.Value = lobbyHandler.LobbyInfo.CreatorId == OwnerId;

            SpawnAvatar();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsFalse(_lobbyId.Value == Guid.Empty);
            VoiceChatManager.GetInstance()?.TryConnectToRoom(_lobbyId.Value, Global.GetName(), ConnectionManager.Instance.UseSecureProtocol);
        }

        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            if (conn == Owner)
            {
                Despawn(_playerAvatar);
            }
        }

#endregion

#region Public API

        public bool IsLocalPlayer => IsController;

        /// <summary>
        ///     The global player state of the player.
        /// </summary>
        public GlobalPlayerState Global { get; private set; }

        public LobbyHandler GetLobbyHandler()
        {
            LobbyHandler handler = LobbyManager.Instance.Internal.GetLobbyHandler(_lobbyId.Value);
            Assert.IsNotNull(handler);
            return handler;
        }

        public bool GetIsAdmin()
        {
            return _isAdmin.Value;
        }

        [Client]
        public Task<PlayerPromotionResult> PromoteToAdmin(TimeSpan? timeout = null)
        {
            Task<PlayerPromotionResult> task = _playerPromoteUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_PromoteToAdmin();
            return task;
        }

        [Client]
        public Task<PlayerKickResult> Kick(TimeSpan? timeout = null)
        {
            Task<PlayerKickResult> task = _playerKickUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_KickPlayer();
            return task;
        }

#endregion

#region Server Methods

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

        [TargetRpc]
        private void TargetRPC_OnPromotionResult(NetworkConnection _, PlayerPromotionResult playerNameUpdateResult)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Promotion result: {playerNameUpdateResult}");
            _playerPromoteUpdateAwaiter?.Complete(playerNameUpdateResult);
        }

        [TargetRpc]
        private void TargetRPC_OnKickResult(NetworkConnection _, PlayerKickResult playerKickResult)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Kick result: {playerKickResult}");
            _playerKickUpdateAwaiter?.Complete(playerKickResult);
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
            GetLobbyHandler().Server_RemovePlayer(Owner);
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({Global.GetName()}) kicked.");
        }

        [Server]
        private bool Server_IsCallerAdmin(NetworkConnection conn)
        {
            LobbyPlayerState caller = GetLobbyHandler().GetPlayerState<LobbyPlayerState>(conn.ClientId);
            return caller.GetIsAdmin();
        }

  #endregion
    }
}
