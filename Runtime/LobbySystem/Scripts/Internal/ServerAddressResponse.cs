using System;
using WebRequests;

namespace AnyVR.LobbySystem.Internal
{
    [Serializable]
    public class ServerAddressResponse : Response
    {
        // ReSharper disable once InconsistentNaming
        public string fishnet_server_address;
        // ReSharper disable once InconsistentNaming
        public string livekit_server_address;

        [NonSerialized] internal string FishnetHost;
        [NonSerialized] internal ushort FishnetPort;
        [NonSerialized] internal string LiveKitHost;
        [NonSerialized] internal ushort LiveKitPort;

        public ServerAddressResponse() { Success = false; }

        public ServerAddressResponse(string fishnetAddress, string livekitAddress)
        {
            fishnet_server_address = fishnetAddress;
            livekit_server_address = livekitAddress;
            Success = true;
        }
    }
}
