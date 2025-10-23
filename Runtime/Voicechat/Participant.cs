using System;

namespace AnyVR.Voicechat
{
    public abstract class Participant
    {
        public readonly string Identity;
        internal readonly string Sid;

        private bool _isSpeaking;

        internal Participant(string sid, string identity, string name)
        {
            Sid = sid;
            Identity = identity;
            Name = name;
        }

        public string Name { get; protected set; }

        public bool IsSpeaking
        {
            get => _isSpeaking;

            internal set
            {
                if (value != _isSpeaking)
                {
                    OnIsSpeakingUpdate?.Invoke(value);
                }
                _isSpeaking = value;
            }
        }

        public event Action<bool> OnIsSpeakingUpdate;
    }
}
