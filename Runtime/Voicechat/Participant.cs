using System;

namespace AnyVR.Voicechat
{
    public abstract class Participant
    {
        private bool _isSpeaking;
        
        public string Sid { get; }
        
        public event Action<bool> OnIsSpeakingUpdate;

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
        
        internal Participant(string sid)
        {
            Sid = sid;
        }
    }
}
