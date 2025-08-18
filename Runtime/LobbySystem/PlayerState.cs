using System;
using AnyVR.Voicechat;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public class PlayerState : NetworkBehaviour
    {
        private readonly SyncVar<int> _id = new();
        private readonly SyncVar<bool> _isAdmin = new();
        private readonly SyncVar<bool> _isConnectedToLobby = new();
        private readonly SyncVar<Guid> _lobbyId = new();
        private readonly SyncVar<string> _playerName = new();

        public int GetID()
        {
            return _id.Value;
        }
        public string GetName()
        {
            return _playerName.Value;
        }
        public bool GetIsAdmin()
        {
            return _isAdmin.Value;
        }
        public Guid GetLobby()
        {
            return _lobbyId.Value;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _id.Value = OwnerId;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                return;

            SetName(ConnectionManager.UserName);
            VoiceChatManager.GetInstance()?.TryConnectToRoom(_lobbyId.Value, GetName(), ConnectionManager.GetInstance()!.UseSecureProtocol);
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
