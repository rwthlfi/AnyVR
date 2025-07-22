using System;
using WebRequests;

namespace AnyVR.LobbySystem
{
    [Serializable]
    public class ServerAddressResponse : Response
    {
        // ReSharper disable once InconsistentNaming
        public string fishnet_server_address;
        // ReSharper disable once InconsistentNaming
        public string livekit_server_address;

        public ServerAddressResponse() { Success = false; }

        public ServerAddressResponse(string fishnetAddress, string livekitAddress)
        {
            fishnet_server_address = fishnetAddress;
            livekit_server_address = livekitAddress;
            Success = true;
        }
    }
}
