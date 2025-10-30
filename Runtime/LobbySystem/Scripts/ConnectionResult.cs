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

    public readonly struct ConnectionResult
    {
        /// <summary>
        ///     The connection status indicating the result of the connection attempt.
        /// </summary>
        public readonly ConnectionStatus ConnectionStatus;

        /// <summary>
        ///     The result of initializing the player's name after connecting.
        ///     Is <c>null</c> if <see cref="ConnectionStatus"/> != <see cref="ConnectionStatus.Connected"/>
        /// </summary>
        public readonly PlayerNameUpdateResult? PlayerNameResult;

        /// <summary>
        ///     <c>True</c>, if the local client successfully connected to the server **and** the desired username was accepted.
        ///     If the username is invalid or already assigned to another client, the local player will be kicked immediately by the server.
        /// </summary>
        public bool IsSuccess => ConnectionStatus == ConnectionStatus.Connected && PlayerNameResult == PlayerNameUpdateResult.Success;

        internal ConnectionResult(ConnectionStatus connectionStatus, PlayerNameUpdateResult? playerNameResult)
        {
            ConnectionStatus = connectionStatus;
            PlayerNameResult = playerNameResult;

            Assert.IsTrue(ConnectionStatus != ConnectionStatus.Connected || PlayerNameResult != null);
        }
    }
}
