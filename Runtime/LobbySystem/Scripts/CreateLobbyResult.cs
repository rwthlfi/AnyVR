using System;

namespace AnyVR.LobbySystem
{
    public struct CreateLobbyResult
    {
        public readonly CreateLobbyStatus Status;

        /// <summary>
        ///     Has value if <see cref="CreateLobbyStatus.Success" />.
        /// </summary>
        public readonly Guid? LobbyId;

        public CreateLobbyResult(CreateLobbyStatus status, Guid? lobbyId = null)
        {
            Status = status;
            LobbyId = lobbyId;
        }
    }
}
