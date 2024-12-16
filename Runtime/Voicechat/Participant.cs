using UnityEngine;

namespace Voicechat
{
    public class Participant
    {
        public Participant(string sid, string identity)
        {
            Sid = sid;
            Identity = identity;
        }

        public string Sid { get; }

        public string Identity { get; }
        public Texture2D VideoTexture { get; internal set; }
        public bool IsSpeaking { get; internal set; }

        public override string ToString()
        {
            return $"Participant({Sid}, {Identity})";
        }
    }
}