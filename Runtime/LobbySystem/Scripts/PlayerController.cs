using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public class PlayerController : NetworkBehaviour
    {
        private readonly SyncVar<int> _playerId = new();

        internal void SetPlayerId(int playerId)
        {
            _playerId.Value = playerId;
        }

#region Public API

        [Server]
        public PlayerStateBase GetPlayerState()
        {
            return this.GetGameState().GetPlayerState(_playerId.Value);
        }

        [Server]
        public T GetPlayerState<T>() where T : PlayerStateBase
        {
            return this.GetGameState().GetPlayerState(_playerId.Value) as T;
        }

#endregion
    }
}
