using System;
using System.Linq;
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
    public class LobbyPlayerState : PlayerState
    {
#region Serialized Properties
        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [SerializeField] private NetworkObject _playerPrefab;
#endregion

#region Replicated Properties
        private readonly SyncVar<bool> _isAdmin = new(); // WritePermission is ServerOnly by default
        
        private readonly SyncVar<Guid> _lobbyId = new(Guid.Empty);
        
        private NetworkObject _playerAvatar; // TODO add player avatar class
#endregion
        
#region Lifecycle Overrides
        public override void OnStartServer()
        {
            base.OnStartServer();

            // Initialize replicated fields
            LobbyHandler lobbyHandler =
                gameObject.scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<LobbyHandler>())
                    .FirstOrDefault(comp => comp != null);

            Assert.IsNotNull(lobbyHandler);

            _lobbyId.Value = lobbyHandler.GetLobbyId();
            _isAdmin.Value = lobbyHandler.LobbyInfo.CreatorId == OwnerId;

            // spawn player avatar
            if (_playerPrefab == null)
                return;
            
            NetworkObject nob = Instantiate(_playerPrefab);
            _playerAvatar = nob;
            Spawn(nob, Owner, gameObject.scene);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsFalse(_lobbyId.Value == Guid.Empty);
            VoiceChatManager.GetInstance()?.TryConnectToRoom(_lobbyId.Value, GetName(), ConnectionManager.GetInstance()!.UseSecureProtocol);
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

        public void PromoteToAdmin()
        {
            if (IsServerStarted)
            {
                PromoteToAdmin_Internal();
            }
            else if (!_isAdmin.Value)
            {
                ServerRPC_PromoteToAdmin(LocalConnection);
            }
        }
        
        public void KickPlayer()
        {
            if (IsServerStarted)
            {
                KickPlayer_Internal();
            }
            else
            {
                ServerRPC_KickPlayer(LocalConnection);
            }
        }
#endregion

#region Server Methods
        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_PromoteToAdmin(NetworkConnection conn)
        {
            if (_isAdmin.Value)
                return;

            if (Server_IsCallerAdmin(conn))
                PromoteToAdmin_Internal();
        }

        [Server]
        private void PromoteToAdmin_Internal()
        {
            _isAdmin.Value = true;
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({GetName()}) promoted to admin.");
        }

        [Server]
        private void KickPlayer_Internal()
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({GetName()}) kicked.");
            //LobbyManager.Instance.Server_TryRemoveClientFromLobby(Owner);
            //TODO
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_KickPlayer(NetworkConnection conn)
        {
            if (Server_IsCallerAdmin(conn))
                KickPlayer_Internal();
        }

        [Server]
        private bool Server_IsCallerAdmin(NetworkConnection callerConn)
        {
            LobbyPlayerState caller = GetLobbyHandler().GetPlayerState<LobbyPlayerState>(callerConn.ClientId);
            return caller.GetIsAdmin();
        }
  #endregion
    }
}
