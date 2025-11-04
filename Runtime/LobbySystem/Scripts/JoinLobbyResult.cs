using System;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the result returned by the <see cref="LobbyManager.JoinLobby" /> method.
    /// </summary>
    public struct JoinLobbyResult
    {
        /// <summary>
        ///     The status indicating the result of the join attempt.
        /// </summary>
        public readonly JoinLobbyStatus Status;

        /// <summary>
        ///     Has value if <see cref="CreateLobbyStatus.Success" />.
        /// </summary>
        public readonly Guid? LobbyId;

        internal JoinLobbyResult(JoinLobbyStatus status, Guid? lobbyId = null)
        {
            Status = status;
            LobbyId = lobbyId;
        }
    }
}
