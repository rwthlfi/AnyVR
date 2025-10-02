using System;

namespace AnyVR.LobbySystem
{
    public struct JoinLobbyResult
    {
        public readonly JoinLobbyStatus Status;

        /// <summary>
        ///     Has value if <see cref="CreateLobbyStatus.Success" />.
        /// </summary>
        public readonly Guid? LobbyId;

        public JoinLobbyResult(JoinLobbyStatus status, Guid? lobbyId = null)
        {
            Status = status;
            LobbyId = lobbyId;
        }
    }
}
