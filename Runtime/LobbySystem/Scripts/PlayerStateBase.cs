using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public abstract class PlayerStateBase : NetworkBehaviour
    {
        private readonly SyncVar<int> _playerId = new();
        /// <summary>
        ///     If this is the local player's state.
        /// </summary>
        public bool IsLocalPlayer => _playerId.Value.Equals(ClientManager.Connection.ClientId);

        public int ID => _playerId.Value;

        internal void SetPlayerId(int id)
        {
            _playerId.Value = id;
        }
    }
}
