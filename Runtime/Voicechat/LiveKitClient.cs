using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyVR.Logging;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.Voicechat
{
    public abstract class LiveKitClient : MonoBehaviour, IDisposable
    {
        private void OnDestroy()
        {
            Dispose();
        }

        public abstract void Dispose();
        internal abstract Task<MicrophonePublishResult> PublishMicrophone(string deviceName);

        internal abstract void UnpublishMicrophone();

        protected abstract void Init();

#region Private Fields

        protected readonly TimedAwaiter<LiveKitConnectionResult> ConnectionAwaiter = new(LiveKitConnectionResult.Timeout, LiveKitConnectionResult.Cancel);

        protected readonly TimedAwaiter<MicrophonePublishResult> TrackPublishResult = new(MicrophonePublishResult.Timeout, MicrophonePublishResult.Cancelled);

        protected readonly Dictionary<string, RemoteParticipant> Remotes = new();

        protected Func<string, AudioSource> AudioSourceMap;

#endregion

#region Public API

        public abstract bool IsConnected { get; }

        public IReadOnlyDictionary<string, RemoteParticipant> RemoteParticipants => Remotes;

        public LocalParticipant LocalParticipant { get; protected set; }

        public bool IsMicPublished { get; protected set; }

        public void SetAudioObjectMapping(Func<string, AudioSource> audioSourceMap)
        {
            AudioSourceMap = audioSourceMap;
        }

        public abstract Task<LiveKitConnectionResult> Connect(string address, string token);

        public abstract void Disconnect();

        public static LiveKitClient Instantiate(GameObject go)
        {
            // #if UNITY_EDITOR
//             Logger.Log(LogLevel.Verbose, nameof(LiveKitClient), "VoiceChatManager not initialized. Platform: EDITOR");
//             return null;
#if UNITY_SERVER
            Logger.Log(LogLevel.Verbose, nameof(LiveKitClient), "VoiceChatManager not initialized. Platform: SERVER");
            return null;
#endif

#if UNITY_WEBGL
            LiveKitClient liveKitClient = go.AddComponent<WebGLVoiceChatClient>();
            Logger.Log(LogLevel.Verbose, nameof(LiveKitClient),"VoiceChatManager initialized. Platform: WEBGL");
#elif UNITY_STANDALONE // && !UNITY_EDITOR
            LiveKitClient liveKitClient = go.AddComponent<StandaloneLiveKitClient>();
            Logger.Log(LogLevel.Verbose, nameof(LiveKitClient), "VoiceChatManager initialized. Platform: STANDALONE");
#else
            // throw new PlatformNotSupportedException("VoiceChatManager not initialized. Platform: UNKNOWN");
#endif

            liveKitClient.Init();
            return liveKitClient;
        }

#endregion

#region Public Events

        public abstract event Action<RemoteParticipant> OnParticipantConnected;

        public abstract event Action<string> OnParticipantDisconnected;

        public abstract event Action<IEnumerable<RemoteParticipant>> OnActiveSpeakerChanged;

#endregion
    }
}
