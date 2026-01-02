using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public enum TokenState
    {
        /// <summary>
        ///     Successfully retrieved a LiveKit token.
        /// </summary>
        Success,

        /// <summary>
        ///     Token request timed out.
        /// </summary>
        Timeout,

        /// <summary>
        ///     Token request was cancelled by another request.
        ///     There can only be one request at a time.
        /// </summary>
        Cancel,

        /// <summary>
        ///     The unity server could not retrieve a token from the token server.
        /// </summary>
        TokenRetrievalFailed

        /// <summary>
        ///     The server dropped the request due to insufficient permissions.
        /// </summary>
        // Rejected,
    }

    public class TokenResult
    {
        public readonly string LiveKitServerUrl;
        public readonly TokenState State;

        public readonly string Token;

        internal TokenResult(TokenState state, string token = null, string liveKitServerUrl = null)
        {
            State = state;
            Token = token;
            LiveKitServerUrl = liveKitServerUrl;

            // state == success implies token != null.
            Assert.IsTrue(state != TokenState.Success || token != null);
            Assert.IsTrue(state != TokenState.Success || liveKitServerUrl != null);
        }
    }

    public static partial class EnumExtensions
    {
        /// <summary>
        ///     Converts a <see cref="JoinLobbyResult" /> value into a human-readable string, suitable for displaying to the user.
        ///     <returns>
        ///         A user-friendly description of the join result.
        ///     </returns>
        /// </summary>
        public static string ToFriendlyString(this TokenState tokenState)
        {
            return tokenState switch
            {
                TokenState.Success => "Successfully retrieved a LiveKit token",
                TokenState.TokenRetrievalFailed => "The unity server could not retrieve a token from the token server",
                TokenState.Timeout => "Server did not respond in time.",
                TokenState.Cancel => "Token request was cancelled by another request.",
                _ => tokenState.ToString()
            };
        }
    }
}
