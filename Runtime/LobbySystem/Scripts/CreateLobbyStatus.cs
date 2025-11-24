namespace AnyVR.LobbySystem
{
    public enum CreateLobbyStatus
    {
        /// <summary>
        ///     The lobby creation was successful.
        /// </summary>
        Success,
        /// <summary>
        ///     The provided lobby name is either null or not allowed.
        /// </summary>
        InvalidLobbyName,
        /// <summary>
        ///     The chosen name is already in use.
        /// </summary>
        LobbyNameTaken,
        /// <summary>
        ///     The provided scene metadata is either null or not contained in the lobby configuration.
        /// </summary>
        InvalidScene,
        /// <summary>
        ///     Likely indicates a server malfunction.
        ///     For connection timeouts refer to <see cref="ConnectionManager.OnClientTimeout" />.
        /// </summary>
        Timeout,
        /// <summary>
        ///     There can only be one lobby creation in progress at a time.
        /// </summary>
        CreationInProgress
    }
}
