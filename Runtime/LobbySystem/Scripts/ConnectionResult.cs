using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public enum ConnectionStatus
    {
        Connected,
        Timeout,
        AlreadyConnected,
        ServerIpRequestFailed,
        Cancelled
    }

    public readonly struct ConnectionResult
    {
        public readonly ConnectionStatus ConnectionStatus;
        public readonly PlayerNameUpdateResult? PlayerNameResult;

        public bool IsSuccess => ConnectionStatus == ConnectionStatus.Connected && PlayerNameResult == PlayerNameUpdateResult.Success;

        internal ConnectionResult(ConnectionStatus connectionStatus, PlayerNameUpdateResult? playerNameResult)
        {
            ConnectionStatus = connectionStatus;
            PlayerNameResult = playerNameResult;
            Assert.IsTrue(ConnectionStatus != ConnectionStatus.Connected || PlayerNameResult != null);
        }
    }
}
