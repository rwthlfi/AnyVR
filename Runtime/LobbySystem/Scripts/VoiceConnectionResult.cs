namespace AnyVR.LobbySystem
{
    public enum VoiceConnectionResult
    {
        /// <summary>
        ///     Successfully connected to the LiveKit server.
        /// </summary>
        Connected,

        /// <summary>
        ///     Connection request to LiveKit server timed out.
        /// </summary>
        Timeout,

        /// <summary>
        ///     Connection request was cancelled by another request.
        ///     There can only be one request at a time.
        /// </summary>
        Cancel,

        /// <summary>
        ///     Failed to retrieve a valid token from the token-server.
        /// </summary>
        TokenRetrievalFailed,

        /// <summary>
        ///     There was an unexpected error connecting to the LiveKit server.
        /// </summary>
        Error,

        /// <summary>
        ///     Could not connect because the player is already another connection to a LiveKit server.
        /// </summary>
        AlreadyConnected,

        /// <summary>
        ///     Could not connect because the current platform is not supported.
        /// </summary>
        PlatformNotSupported
    }

    public static partial class EnumExtensions
    {
        /// <summary>
        ///     Converts a <see cref="VoiceConnectionResult" /> value into a human-readable string, suitable for displaying to the
        ///     user.
        ///     <returns>
        ///         A user-friendly description of the voice connection result.
        ///     </returns>
        /// </summary>
        public static string ToFriendlyString(this VoiceConnectionResult result)
        {
            return result switch
            {
                VoiceConnectionResult.Connected => "Successfully connected to the LiveKit server",
                VoiceConnectionResult.Cancel => "LiveKit connection cancelled due to another connection request. There can only be one connection process at the same time.",
                VoiceConnectionResult.TokenRetrievalFailed => "LiveKit connection failed. Could not retrieve a LiveKit token.",
                VoiceConnectionResult.Error => "There was an error connecting to the LiveKit server.",
                VoiceConnectionResult.AlreadyConnected => "Already connected to a LiveKit server.",
                VoiceConnectionResult.PlatformNotSupported => "The voicechat is not supported on this platform.",
                VoiceConnectionResult.Timeout => "Could not connect to the LiveKit server. Request timed out.",
                _ => result.ToString()
            };
        }
    }
}
