namespace AnyVR.LobbySystem
{
    public enum JoinLobbyResult
    {
        Success,
        AlreadyConnected,
        LobbyDoesNotExist,
        LobbyIsFull,
        PasswordMismatch,
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
        InvalidFormat,
        /// <summary>
        ///     If provided quick connect code is out of range.
        /// </summary>
        OutOfRange
        //TODO: Add lobby configuration mismatch value
    }
}
