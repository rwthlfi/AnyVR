namespace AnyVR.LobbySystem
{
    public enum CreateLobbyStatus
    {
        Success,
        LobbyNameTaken,
        InvalidScene,
        InvalidParameters,
        /// <summary>
        ///     Likely indicates a server malfunction.
        ///     When the user requests to join a lobby they are already connected to the server.
        ///     For connection timeouts refer to <see cref="ConnectionManager.OnClientTimeout" />.
        /// </summary>
        Timeout,
        /// <summary>
        /// There can only be one lobby creation in progress at a time.
        /// </summary>
        CreationInProgress
    }
}
