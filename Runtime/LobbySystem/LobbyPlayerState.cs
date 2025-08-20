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
        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [SerializeField] private NetworkObject _playerPrefab;

        private readonly SyncVar<bool> _isAdmin = new(); // WritePermission is ServerOnly by default
        private readonly SyncVar<Guid> _lobbyId = new(Guid.Empty);

        private NetworkObject _onlinePlayer;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_playerPrefab == null)
                return;

            LobbyHandler lobbyHandler = 
                gameObject.scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<LobbyHandler>())
                    .FirstOrDefault(comp => comp != null);

            Assert.IsNotNull(lobbyHandler);

            _lobbyId.Value = lobbyHandler.GetLobbyId();
            _isAdmin.Value = lobbyHandler.MetaData.CreatorId == OwnerId;

            NetworkObject nob = Instantiate(_playerPrefab);
            _onlinePlayer = nob;
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
                Despawn(_onlinePlayer);
            }
        }

        public void PromoteToAdmin()
        {
            if (IsServerStarted)
            {
                PromoteToAdmin_Internal();
            }
            else if (!_isAdmin.Value)
            {
                ServerRPC_PromoteToAdmin(ClientManager.Connection);
            }
        }

        [ServerRpc]
        private void ServerRPC_PromoteToAdmin(NetworkConnection conn)
        {
            if (_isAdmin.Value)
                return;

            LobbyHandler lobbyHandler = LobbyHandler.GetInstance();
            if (lobbyHandler == null)
                return;

            LobbyPlayerState caller = lobbyHandler.GetPlayerState<LobbyPlayerState>(conn.ClientId);
            if (!caller.GetIsAdmin())
                return;

            PromoteToAdmin_Internal();
        }

        [Server]
        private void PromoteToAdmin_Internal()
        {
            _isAdmin.Value = true;
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} promoted to admin.");
        }

        public bool GetIsAdmin()
        {
            return _isAdmin.Value;
        }
        
        public Guid GetLobby()
        {
            return _lobbyId.Value;
        }
    }
}
