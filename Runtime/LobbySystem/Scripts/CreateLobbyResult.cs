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

    public static partial class EnumExtensions
    {
        /// <summary>
        ///     Converts a <see cref="CreateLobbyResult" /> value into a human-readable string, suitable for displaying to the user.
        ///     <returns>
        ///         A user-friendly description of the lobby creation result.
        ///     </returns>
        /// </summary>
        public static string ToFriendlyString(this CreateLobbyResult result)
        {
            return result.Status switch
            {
                CreateLobbyStatus.Success => "Lobby created successfully.",
                CreateLobbyStatus.InvalidLobbyName => "The lobby name is invalid.",
                CreateLobbyStatus.LobbyNameTaken => "A lobby with this name already exists.",
                CreateLobbyStatus.InvalidScene => "The selected scene is not valid.",
                CreateLobbyStatus.Timeout => "The server did not respond in time.",
                CreateLobbyStatus.CreationInProgress => "A lobby is already being created.",

                _ => result.ToString()
            };
        }
    }
}
