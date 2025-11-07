using AnyVR.PlatformManagement;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the global state of a player.
    ///     Is always replicated to all players on the server.
    ///     Inherit from this class to add additional synchronized properties as needed.
    ///     By default, contains the player's id, name and platform type.
    ///     The corresponding player does not own their player state and the replicated properties may only be updated by the
    ///     server.
    ///     <seealso cref="LobbyPlayerState" />
    /// </summary>
    public class GlobalPlayerState : PlayerStateBase
    {
#region Replicated Properties

        private readonly SyncVar<string> _playerName = new("null");

        private readonly SyncVar<PlatformType> _platformType = new(PlatformType.Unknown);
        public static GlobalPlayerState LocalPlayer => GlobalGameState.Instance.GetPlayerState(InstanceFinder.ClientManager.Connection.ClientId);

#endregion

#region Public API

        /// <summary>
        ///     The unique player's name on the server.
        /// </summary>
        public string Name => _playerName.Value;

        /// <summary>
        ///     The player's platform type.
        /// </summary>
        public PlatformType PlatformType => _platformType.Value;

#endregion

#region Server Methods

        [Server]
        internal void SetPlatformType(PlatformType platformType)
        {
            _platformType.Value = platformType;
        }

        [Server]
        internal void SetName(string playerName)
        {
            _playerName.Value = playerName;
        }

#endregion
    }
}
