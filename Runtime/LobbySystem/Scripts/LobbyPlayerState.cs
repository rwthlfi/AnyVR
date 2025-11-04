using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the state of a player inside a lobby.
    ///     Only replicated to players in the same lobby.
    ///     Inherit from this class to add additional synchronized properties as needed.
    ///     By default, contains a field with the id of the lobby and if the player is an admin in the lobby.
    ///     Also holds a reference to the player's <see cref="GlobalPlayerState" />.
    /// </summary>
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

        /// <summary>
        ///     If the player is an admin in the lobby.
        /// </summary>
        public bool IsAdmin => _isAdmin.Value;

        /// <summary>
        ///     The id of the corresponding lobby.
        ///     Mainly used on the server as this always equals the local player's lobby id.
        /// </summary>
        public Guid LobbyId => _lobbyId.Value;

#endregion
    }
}
