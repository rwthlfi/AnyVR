using System;
using LiveKit;
using UnityEngine;

namespace AnyVR.Voicechat
{
    public class LocalParticipant : Participant, IDisposable
    {
        private readonly LiveKitClient _client;
        
        internal MicrophoneSource MicrophoneSource;

        internal LocalParticipant(LiveKitClient client, string sid) : base(sid)
        {
            _client = client;
        }

        public void PublishMicrophone(string deviceName)
        {
            _client.PublishMicrophone(deviceName);
        }
        
        public void UnpublishMicrophone()
        {
            _client.UnpublishMicrophone();
        }
        
        public void Dispose()
        {
            MicrophoneSource?.Dispose();
        }
    }
}
