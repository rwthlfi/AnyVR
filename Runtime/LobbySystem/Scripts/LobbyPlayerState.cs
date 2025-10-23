using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState : NetworkBehaviour
    {
#region Lifecycle Overrides

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Global = GlobalGameState.Instance.GetPlayerState(OwnerId);
            Assert.IsNotNull(Global);
        }

#endregion

        internal NetworkObject GetAvatar()
        {
            return _playerAvatar;
        }

        internal void SetAvatar(NetworkObject avatar)
        {
            _playerAvatar = avatar;
        }

#region Replicated Properties

        private readonly SyncVar<bool> _isAdmin = new(); // WritePermission is ServerOnly by default

        private readonly SyncVar<Guid> _lobbyId = new(Guid.Empty);

        private NetworkObject _playerAvatar;
        // TODO add player avatar class

#endregion

#region Public API

        /// <summary>
        ///     The global player state of the player.
        /// </summary>
        public GlobalPlayerState Global { get; private set; }

        public bool IsAdmin()
        {
            return _isAdmin.Value;
        }

        public Guid GetLobbyId()
        {
            return _lobbyId.Value;
        }

#endregion
    }
}
