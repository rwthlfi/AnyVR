using AnyVR.Logging;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.Voicechat
{
    public class VoicechatManager : MonoBehaviour
    {
        public static LiveKitClient InstantiateClient()
        {
            LiveKitClient liveKitClient;
#if UNITY_EDITOR && false
            Logger.Log(LogLevel.Verbose, nameof(VoicechatManager), "VoiceChatManager not initialized. Platform: EDITOR");
            return null;
#elif UNITY_SERVER
            Logger.Log(LogLevel.Verbose, nameof(VoicechatManager), "VoiceChatManager not initialized. Platform: SERVER");
            return null;
#endif

            GameObject go = new("LiveKitClient");
#if UNITY_WEBGL
            liveKitClient = go.AddComponent<WebGLVoiceChatClient>();
            Logger.Log(LogLevel.Verbose, nameof(VoicechatManager),"VoiceChatManager initialized. Platform: WEBGL");
#elif UNITY_STANDALONE // && !UNITY_EDITOR
            liveKitClient = go.AddComponent<StandaloneLiveKitClient>();
            Logger.Log(LogLevel.Verbose, nameof(VoicechatManager), "VoiceChatManager initialized. Platform: STANDALONE");
#else
            // throw new PlatformNotSupportedException("VoiceChatManager not initialized. Platform: UNKNOWN");
#endif

            liveKitClient.Init();
            return liveKitClient;
        }
    }
}
