using System;

namespace AnyVR.Voicechat
{
    public abstract class Participant
    {
        internal readonly string Sid;

        internal Participant(string sid, string identity, string name)
        {
            Sid = sid;
            Identity = identity;
            Name = name;
        }

        internal void SetIsSpeaking(bool speaking)
        {
            IsSpeaking = speaking;
            IsSpeakingUpdate?.Invoke(speaking);
        }

        internal void SetIsMicPublished(bool isPublished)
        {
            IsMicPublished = isPublished;
            IsMicPublishedUpdate?.Invoke(isPublished);
        }

        internal void SetIsMicMuted(bool isMuted)
        {
            IsMicMuted = isMuted;
            IsMicMutedUpdate?.Invoke(isMuted);
        }

#region Public API

        public string Name { get; private set; }

        public string Identity { get; private set; }

        /// <summary>
        ///     If the microphone of this participant is published.
        /// </summary>
        public bool IsMicPublished { get; private set; }

        /// <summary>
        ///     If the microphone of this participant is published and muted.
        /// </summary>
        public bool IsMicMuted { get; private set; }

        /// <summary>
        ///     If the participant is currently speaking.
        /// </summary>
        public bool IsSpeaking { get; private set; }

        public event Action<bool> IsSpeakingUpdate;

        public event Action<bool> IsMicPublishedUpdate;

        public event Action<bool> IsMicMutedUpdate;

#endregion
    }
}
