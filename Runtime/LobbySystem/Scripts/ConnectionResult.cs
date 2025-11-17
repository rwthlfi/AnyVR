using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public enum ConnectionStatus
    {
        /// <summary>
        ///     The connection to the server was successfully established.
        /// </summary>
        Connected,

        /// <summary>
        ///     The connection attempt timed out before completion.
        /// </summary>
        Timeout,

        /// <summary>
        ///     The local client is already connected to a server.
        /// </summary>
        AlreadyConnected,

        /// <summary>
        ///     The fishnet server uri could not be fetched from the token server.
        /// </summary>
        ServerIpRequestFailed,

        /// <summary>
        ///     The connection attempt was cancelled by another connection request.
        /// </summary>
        Cancelled
    }

    /// <summary>
    ///     Represents the result returned by the <see cref="ConnectionManager.ConnectToServer" /> method.
    /// </summary>
    public readonly struct ConnectionResult
    {
        /// <summary>
        ///     The connection status indicating the result of the connection attempt.
        /// </summary>
        public readonly ConnectionStatus ConnectionStatus;

        /// <summary>
        ///     The result of initializing the player's name after connecting.
        ///     Is <c>null</c> if <see cref="ConnectionStatus" /> != <see cref="ConnectionStatus.Connected" />
        /// </summary>
        public readonly PlayerNameUpdateResult? PlayerNameResult;

        /// <summary>
        ///     <c>True</c>, if the local client successfully connected to the server **and** the desired username was accepted.
        ///     If the username is invalid or already assigned to another client, the local player will be kicked immediately by
        ///     the server.
        /// </summary>
        public bool IsSuccess => ConnectionStatus == ConnectionStatus.Connected && PlayerNameResult == PlayerNameUpdateResult.Success;

        internal ConnectionResult(ConnectionStatus connectionStatus, PlayerNameUpdateResult? playerNameResult)
        {
            ConnectionStatus = connectionStatus;
            PlayerNameResult = playerNameResult;

            Assert.IsTrue(ConnectionStatus != ConnectionStatus.Connected || PlayerNameResult != null);
        }

        /// <summary>
        ///     Returns a human-readable message describing the connection attempt result.
        /// </summary>
        public string ToFriendlyString()
        {
            // First handle connection failure states
            switch (ConnectionStatus)
            {
                case ConnectionStatus.Timeout: return "The server did not respond in time.";
                case ConnectionStatus.AlreadyConnected: return "You are already connected to a server.";
                case ConnectionStatus.ServerIpRequestFailed: return "Could not retrieve the server address from the token server.";
                case ConnectionStatus.Cancelled: return "The connection attempt was cancelled.";
                case ConnectionStatus.Connected: break;
                default: return ConnectionStatus.ToString();
            }

            // At this point, ConnectionStatus == Connected
            Assert.IsTrue(PlayerNameResult != null);

            return PlayerNameResult == PlayerNameUpdateResult.Success ? "Connection successful." : $"You were kicked from the server {PlayerNameResult.Value.ToFriendlyString()}";
        }
    }
}
