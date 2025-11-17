namespace AnyVR.Voicechat
{
    public class LocalParticipant : Participant
    {
        private readonly LiveKitClient _client;
        
        public bool IsMicEnabled { get; protected set; }

        internal LocalParticipant(LiveKitClient client, string sid, string identity, string name) : base(sid, identity, name)
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
    }
}
