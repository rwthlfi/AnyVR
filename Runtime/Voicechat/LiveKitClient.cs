using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AnyVR.Voicechat
{
    public abstract class LiveKitClient : MonoBehaviour
    {
        internal abstract Task<MicrophonePublishResult> PublishMicrophone(string deviceName);

        public abstract void UnpublishMicrophone();

        protected void UpdateActiveSpeakers(HashSet<string> activeSpeakers)
        {
            foreach (RemoteParticipant participant in Remotes.Values)
            {
                participant.IsSpeaking = activeSpeakers.Contains(participant.Identity);
            }
        }

#region Private Fields

        protected readonly TimedAwaiter<ConnectionResult> ConnectionAwaiter = new(ConnectionResult.Timeout, ConnectionResult.Cancel);

        protected readonly TimedAwaiter<MicrophonePublishResult> TrackPublishResult = new(MicrophonePublishResult.Timeout, MicrophonePublishResult.Cancelled);

        protected readonly Dictionary<string, RemoteParticipant> Remotes = new();

        protected Func<string, GameObject> AudioObjectMap;

#endregion

#region Public API

        public abstract bool IsConnected { get; }

        public IReadOnlyDictionary<string, RemoteParticipant> RemoteParticipants => Remotes;

        public LocalParticipant LocalParticipant { get; protected set; }

        public bool IsMicEnabled { get; protected set; }

        public void SetAudioObjectMapping(Func<string, GameObject> audioObjectMap)
        {
            AudioObjectMap = audioObjectMap;
        }

        public abstract void Init();

        public abstract Task<ConnectionResult> Connect(string address, string token);

        public abstract void Disconnect();

#endregion

#region Public Events

        public abstract event Action<RemoteParticipant> OnParticipantConnected;

        public abstract event Action<string> OnParticipantDisconnected;

#endregion
    }
}
