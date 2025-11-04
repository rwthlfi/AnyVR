using System;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the result returned by the <see cref="LobbyManager.CreateLobby" /> method.
    /// </summary>
    public struct CreateLobbyResult
    {
        /// <summary>
        ///     The status indicating the result of the lobby creation attempt.
        /// </summary>
        public readonly CreateLobbyStatus Status;

        /// <summary>
        ///     The id assigned to the created lobby.
        ///     Has value if <see cref="Status" /> = <see cref="CreateLobbyStatus.Success" />.
        /// </summary>
        public readonly Guid? LobbyId;

        internal CreateLobbyResult(CreateLobbyStatus status, Guid? lobbyId = null)
        {
            Status = status;
            LobbyId = lobbyId;
        }
    }
}
