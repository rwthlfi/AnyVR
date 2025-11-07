using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public class PlayerController : NetworkBehaviour
    {
        private readonly SyncVar<PlayerStateBase> _playerState = new();

        internal void SetPlayerState(PlayerStateBase playerState)
        {
            _playerState.Value = playerState;
        }

        public PlayerStateBase GetPlayerState()
        {
            return _playerState.Value;
        }

        public T GetPlayerState<T>() where T : PlayerStateBase
        {
            return _playerState.Value as T;
        }
    }
}
