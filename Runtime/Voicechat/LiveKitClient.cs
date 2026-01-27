using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyVR.Logging;
using UnityEngine;
using UnityEngine.Assertions;
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

        internal abstract void SetMute(bool mute);

        protected abstract void Init();

#region Private Fields

        protected readonly TimedAwaiter<LiveKitConnectionResult> ConnectionAwaiter = new(LiveKitConnectionResult.Timeout, LiveKitConnectionResult.Cancel);

        protected readonly TimedAwaiter<MicrophonePublishResult> TrackPublishResult = new(MicrophonePublishResult.Timeout, MicrophonePublishResult.Cancelled);

        protected readonly Dictionary<string, RemoteParticipant> Remotes = new();

        protected Func<string, AudioSource> AudioSourceMap;

#endregion

#region Public API

        /// <summary>
        ///     If the local player is connected to a LiveKit room.
        /// </summary>
        public abstract bool IsConnected { get; }

        public LocalParticipant LocalParticipant { get; protected set; }

        public IReadOnlyDictionary<string, RemoteParticipant> RemoteParticipants => Remotes;

        public IEnumerable<Participant> Participants
        {
            get
            {
                yield return LocalParticipant;
                foreach (RemoteParticipant remote in Remotes.Values)
                    yield return remote;
            }
        }

        public void SetAudioObjectMapping(Func<string, AudioSource> audioSourceMap)
        {
            AudioSourceMap = audioSourceMap;
        }

        public abstract Task<LiveKitConnectionResult> Connect(string address, string token);

        public abstract void Disconnect();

        protected void OnActiveSpeakerChange(HashSet<Participant> speakers)
        {
            foreach (Participant player in Participants)
            {
                player.SetIsSpeaking(speakers.Contains(player));
            }

            OnActiveSpeakerChanged?.Invoke(speakers);
        }

        public static LiveKitClient Instantiate(GameObject go)
        {
            LiveKitClient liveKitClient = null;
#if UNITY_SERVER
            Logger.Log(LogLevel.Verbose, nameof(LiveKitClient), "VoiceChatManager not initialized. Platform: SERVER");
            return null;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
            liveKitClient = go.AddComponent<WebGLVoicechatClient>();
            Logger.Log(LogLevel.Verbose, nameof(LiveKitClient), "VoiceChatManager initialized. Platform: WEBGL");
#elif UNITY_STANDALONE || UNITY_ANDROID
            liveKitClient = go.AddComponent<StandaloneLiveKitClient>();
            Logger.Log(LogLevel.Verbose, nameof(LiveKitClient), "VoiceChatManager initialized. Platform: STANDALONE");
#else
            // throw new PlatformNotSupportedException("VoiceChatManager not initialized. Platform: UNKNOWN");
#endif
            Assert.IsNotNull(liveKitClient);
            liveKitClient.Init();
            return liveKitClient;
        }

#endregion

#region Public Events

        public abstract event Action<RemoteParticipant> OnParticipantConnected;

        public abstract event Action<string> OnParticipantDisconnected;

        public event Action<IEnumerable<Participant>> OnActiveSpeakerChanged;

#endregion
    }
}
