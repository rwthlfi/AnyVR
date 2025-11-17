namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the result of a lobby join attempt.
    ///     Returned by the following methods:
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="GlobalPlayerController.JoinLobby(ILobbyInfo, string)" />
    ///         </item>
    ///         <item>
    ///             <see cref="GlobalPlayerController.JoinLobby(System.Guid, string)" />
    ///         </item>
    ///         <item>
    ///             <see cref="GlobalPlayerController.QuickConnect" />
    ///         </item>
    ///     </list>
    /// </summary>
    public enum JoinLobbyResult
    {
        /// <summary>
        ///     The lobby join attempt was successfully.
        /// </summary>
        Success,

        /// <summary>
        ///     The player is already connected to the lobby.
        /// </summary>
        AlreadyConnected,

        /// <summary>
        ///     The lobby does not exist on the server.
        /// </summary>
        LobbyDoesNotExist,

        /// <summary>
        ///     The lobby is full.
        /// </summary>
        LobbyIsFull,

        /// <summary>
        ///     Incorrect password.
        /// </summary>
        PasswordMismatch,

        /// <summary>
        ///     Already attempting to join a lobby.
        /// </summary>
        AlreadyJoining,

        /// <summary>
        ///     Likely indicates a server malfunction.
        ///     When the user requests to join a lobby they are already connected to the server.
        ///     For connection timeouts refer to <see cref="ConnectionManager.OnClientTimeout" />.
        /// </summary>
        Timeout,

        /// <summary>
        ///     If provided quick connect code is invalid.
        /// </summary>
        InvalidQuickConnectFormat,

        /// <summary>
        ///     If provided quick connect code is out of range.
        /// </summary>
        QuickConnectOutOfRange

        //TODO: Add lobby configuration mismatch value
    }

    public static partial class EnumExtensions
    {
        /// <summary>
        ///     Converts a <see cref="JoinLobbyResult" /> value into a human-readable string, suitable for displaying to the user.
        ///     <returns>
        ///         A user-friendly description of the join result.
        ///     </returns>
        /// </summary>
        public static string ToFriendlyString(this JoinLobbyResult result)
        {
            return result switch
            {
                JoinLobbyResult.Success => "Successfully joined lobby.",
                JoinLobbyResult.AlreadyConnected => "You are already connected.",
                JoinLobbyResult.LobbyDoesNotExist => "This lobby does not exist.",
                JoinLobbyResult.LobbyIsFull => "The lobby is full.",
                JoinLobbyResult.PasswordMismatch => "Incorrect password.",
                JoinLobbyResult.AlreadyJoining => "Already attempting to join a lobby.",
                JoinLobbyResult.Timeout => "Server did not respond in time.",
                JoinLobbyResult.InvalidQuickConnectFormat => "Invalid quick connect code.",
                JoinLobbyResult.QuickConnectOutOfRange => "Quick connect code is out of range.",
                _ => result.ToString()
            };
        }
    }
}
