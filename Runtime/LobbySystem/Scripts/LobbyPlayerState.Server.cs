using System;
using FishNet.Connection;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            if (conn == Owner)
            {
                Despawn(_playerAvatar);
            }
        }

        internal void SetIsAdmin(bool b)
        {
            _isAdmin.Value = b;
        }

        internal void SetLobbyId(Guid lobbyId)
        {
            _lobbyId.Value = lobbyId;
        }
    }
}
