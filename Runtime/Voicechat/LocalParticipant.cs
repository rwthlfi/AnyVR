namespace AnyVR.Voicechat
{
    public class LocalParticipant : Participant
    {
        private readonly LiveKitClient _client;

        internal LocalParticipant(LiveKitClient client, string sid, string identity, string name) : base(sid, identity, name)
        {
            _client = client;
        }

        public bool IsMicEnabled { get; protected set; }

        public void PublishMicrophone(string deviceName = null)
        {
            _client.PublishMicrophone(deviceName);
        }

        public void UnpublishMicrophone()
        {
            _client.UnpublishMicrophone();
        }
    }
}
