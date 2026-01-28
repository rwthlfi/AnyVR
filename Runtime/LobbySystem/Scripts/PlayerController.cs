using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Base implementation of the PlayerController.
    ///     <seealso cref="GlobalPlayerController" />
    ///     <seealso cref="LobbyPlayerController" />
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
#region Replicated Fields

        private readonly SyncVar<PlayerStateBase> _playerState = new();

#endregion

#region Internal Methods

        internal void SetPlayerState(PlayerStateBase playerState)
        {
            _playerState.Value = playerState;
        }

#endregion

#region Public API

        public PlayerStateBase GetPlayerState()
        {
            return _playerState.Value;
        }

        public T GetPlayerState<T>() where T : PlayerStateBase
        {
            return _playerState.Value as T;
        }

#endregion
    }
}
