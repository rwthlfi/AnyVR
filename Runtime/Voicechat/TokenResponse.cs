using System;
using WebRequests;

namespace AnyVR.Voicechat
{
    [Serializable]
    public class TokenResponse : Response
    {
        // ReSharper disable once InconsistentNaming
        public string token;
    }
}
