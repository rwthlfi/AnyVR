using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public class PlayerState : NetworkBehaviour
    {
        private readonly SyncVar<int> _id = new();
        private readonly SyncVar<string> _playerName = new();

        internal Action PostServerInitialized;

        public int GetID()
        {
            return _id.Value;
        }

        public string GetName()
        {
            return _playerName.Value;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _id.Value = OwnerId;
            PostServerInitialized?.Invoke();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                return;

            SetName(ConnectionManager.UserName);
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
