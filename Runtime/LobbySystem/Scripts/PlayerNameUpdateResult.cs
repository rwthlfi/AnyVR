namespace AnyVR.LobbySystem
{
    public enum PlayerNameUpdateResult
    {
        /// <summary>
        ///     The name update was successful.
        /// </summary>
        Success,

        /// <summary>
        ///     The desired name is invalid.
        /// </summary>
        InvalidName,

        /// <summary>
        ///     The desired name is already assigned to this player.
        /// </summary>
        AlreadySet,

        /// <summary>
        ///     The desired name is already taken by another player.
        /// </summary>
        NameTaken,

        /// <summary>
        ///     The desired name is too long.
        /// </summary>
        TooLong,

        /// <summary>
        ///     The desired name is too short.
        /// </summary>
        TooShort,

        /// <summary>
        ///     The name update attempt timed out before completion.
        /// </summary>
        Timeout,

        /// <summary>
        ///     The name update attempt was cancelled by another name update request.
        /// </summary>
        Cancelled
    }

    public static partial class EnumExtensions
    {
        /// <summary>
        ///     Converts a <see cref="PlayerNameUpdateResult" /> value into a human-readable string, suitable for displaying to the user.
        ///     <returns>
        ///         A user-friendly description of the name update result.
        ///     </returns>
        /// </summary>
        public static string ToFriendlyString(this PlayerNameUpdateResult result)
        {
            return result switch
            {
                PlayerNameUpdateResult.Success => "Name updated successfully.",
                PlayerNameUpdateResult.NameTaken => "This name is already taken.",
                PlayerNameUpdateResult.InvalidName => "The provided name is invalid.",
                PlayerNameUpdateResult.AlreadySet => "Your name has already been set.",
                PlayerNameUpdateResult.TooLong => "The name is too long.",
                PlayerNameUpdateResult.TooShort => "The name is too short.",
                PlayerNameUpdateResult.Timeout => "The server did not respond in time.",
                PlayerNameUpdateResult.Cancelled => "The name update was cancelled.",

                _ => result.ToString()
            };
        }
    }
}
