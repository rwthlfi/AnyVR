using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;

namespace AnyVR.LobbySystem
{
    public class PlayerState : NetworkBehaviour
    {
        private readonly SyncVar<int> _id = new();
        private readonly SyncVar<string> _playerName = new();
        private readonly SyncVar<bool> _isConnectedToLobby = new();
        private readonly SyncVar<bool> _isAdmin = new();
        private readonly SyncVar<Guid> _lobbyId = new();

        public int GetID() => _id.Value;
        public string GetName() => _playerName.Value;
        public bool GetIsAdmin() => _isAdmin.Value;
        public Guid GetLobby() => _lobbyId.Value;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _id.Value = OwnerId;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsOwner)
            {
                SetName(ConnectionManager.UserName);
            }
        }

        public void SetName(string playerName)
        {
            if (IsServerStarted)
            {
                SetName_Internal(playerName);
            }
            else
            {
                ServerRPC_SetName(playerName);
            }
        }

        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetName(string playerName)
        {
            SetName_Internal(playerName);
        }

        private void SetName_Internal(string playerName)
        {
            _playerName.Value = playerName;
        }
    }
}
