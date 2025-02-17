using System;

namespace AnyVr.Voicechat
{
    [Serializable]
    public class TokenResponse
    {
        public string token;
        [NonSerialized] public bool IsSuccess;
    }
}