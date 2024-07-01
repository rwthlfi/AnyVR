using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voicechat
{
    internal abstract class VoiceChatClient : MonoBehaviour
    {
        protected readonly Dictionary<string, Participant> Remotes = new();
        private HashSet<string> _activeSpeakers = new();

        protected bool Connected;

        protected Participant Local;
        public bool IsMicEnabled { get; protected set; }

        internal Dictionary<string, Participant> RemoteParticipants => Remotes.ToDictionary(e => e.Key, e => e.Value);

        internal Participant LocalParticipant => Local;

        /// <summary>
        ///     Returns if the client is connected to any room
        /// </summary>
        internal bool IsConnected => Connected;

        internal abstract event Action<Participant> ParticipantConnected;

        internal abstract event Action<string> ParticipantDisconnected;

        internal event Action<string, bool> ParticipantIsSpeakingUpdate;

        internal abstract event Action ConnectedToRoom;

        internal abstract event Action<string, byte[]> DataReceived;

        internal abstract event Action<string> VideoReceived;

        internal abstract void Init();

        internal abstract void Connect(string address, string token);

        internal abstract void Disconnect();

        internal abstract void SetMicrophoneEnabled(bool b);

        internal abstract void SetMicEnabled(bool b);

        internal abstract void SendData(byte[] buffer);

        internal abstract void SetClientMute(string sid, bool mute);

        protected void UpdateActiveSpeakers(HashSet<string> activeSpeakers)
        {
            List<string> sids = Remotes.Keys.ToList();
            sids.Add(Local.Sid);
            foreach (string sid in sids)
            {
                if (_activeSpeakers.Contains(sid) && !activeSpeakers.Contains(sid))
                    // sid stopped speaking
                {
                    ParticipantIsSpeakingUpdate?.Invoke(sid, false);
                }
                else if (!_activeSpeakers.Contains(sid) && activeSpeakers.Contains(sid))
                    // sid started speaking
                {
                    ParticipantIsSpeakingUpdate?.Invoke(sid, true);
                }
            }

            _activeSpeakers = activeSpeakers;
        }
    }
}