using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public class PlayerState : NetworkBehaviour
    {
        internal Action PostServerInitialized;
        
#region Replicated Properties
        private readonly SyncVar<string> _playerName = new();
#endregion

#region Lifecycle Overrides
        public override void OnStartServer()
        {
            base.OnStartServer();
            PostServerInitialized?.Invoke();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                return;

            SetName(ConnectionManager.UserName);
        }
#endregion

#region Public API
        public int GetID()
        {
            return OwnerId;
        }

        public string GetName()
        {
            return _playerName.Value;
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
#endregion

#region Server Method
        [ServerRpc(RequireOwnership = true)]
        private void ServerRPC_SetName(string playerName)
        {
            SetName_Internal(playerName);
        }
        
        [Server]
        private void SetName_Internal(string playerName)
        {
            _playerName.Value = playerName;
        }
#endregion
    }
}
