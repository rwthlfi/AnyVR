using System;

namespace AnyVR.LobbySystem
{
    public struct JoinLobbyResult
    {
        public JoinLobbyStatus Status { get; }
        public Guid? LobbyId { get; }

        public JoinLobbyResult(JoinLobbyStatus status, Guid? lobbyId = null)
        {
            Status = status;
            LobbyId = lobbyId;
        }
    }
}
