using System;
using UnityEngine.Serialization;

namespace AnyVr.Voicechat
{
    [Serializable]
    public class TokenResponse
    {
        public bool isSuccess;
        public string token;
    }
}