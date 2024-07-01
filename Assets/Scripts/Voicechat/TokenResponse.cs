using System;

namespace Voicechat
{
    [Serializable]
    public class TokenResponse
    {
        // ReSharper disable once InconsistentNaming
        public string token;

        // ReSharper disable once InconsistentNaming
        public string livekit_server_address;
    }
}