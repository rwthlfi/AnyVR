using System;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.Voicechat
{
    public static class VoiceConfig
    {
        public static readonly string TokenServerUrl;

        public static readonly string LiveKitServerUrl;

        static VoiceConfig()
        {
            TokenServerUrl = Environment.GetEnvironmentVariable("TOKEN_SERVER_URL");
            LiveKitServerUrl = Environment.GetEnvironmentVariable("LIVEKIT_SERVER_URL");

            if (TokenServerUrl == null)
            {
                Debug.LogWarning("The environment variable 'TOKEN_SERVER_URL' is not set.");
            }

            if (LiveKitServerUrl == null)
            {
                Debug.LogWarning("The environment variable 'LIVEKIT_SERVER_URL' is not set.");
            }
        }
    }

    [Serializable]
    internal class TokenResponse : Response
    {
        public string token;
    }

    public class LiveKitTokenClient
    {
        private readonly string _roomName;

        public LiveKitTokenClient(string roomName)
        {
            Assert.IsNotNull(roomName);
            _roomName = roomName;
        }

        public async Task<string> RequestToken(string identity)
        {
            Uri tokenUri = new UriBuilder(VoiceConfig.TokenServerUrl)
            {
                Scheme = "http", Path = "requestToken", Query = $"room_name={Uri.EscapeDataString(_roomName)}&identity={Uri.EscapeDataString(identity)}"
            }.Uri;

            TokenResponse response = await WebRequestHandler.GetAsync<TokenResponse>(tokenUri);
            return response?.token;
        }
    }
}
