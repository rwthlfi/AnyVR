using System;

namespace AnyVR.Voicechat
{
    [Serializable]
    public class TokenResponse
    {
        public string token;
        [NonSerialized] public bool IsSuccess;
    }
}