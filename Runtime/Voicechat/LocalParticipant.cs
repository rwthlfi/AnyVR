namespace AnyVR.Voicechat
{
    public class LocalParticipant : Participant
    {
        private readonly LiveKitClient _client;

        internal LocalParticipant(LiveKitClient client, string sid, string identity, string name) : base(sid, identity, name)
        {
            _client = client;
        }

        /// <summary>
        ///     Publishes the microphone to the LiveKit server.
        ///     Unpublishes the currently published microphone.
        ///     On WebGL builds, the microphone device has to be set in the browser permissions and the <c>device</c> parameter
        ///     will be ignored.
        /// </summary>
        /// <param name="device">
        ///     A valid microphone device is one from <c>Microphone.devices</c>. Pass <c>null</c> for the default
        ///     device.
        /// </param>
        public void PublishMicrophone(string device = null)
        {
            _client.PublishMicrophone(device);
        }

        /// <summary>
        ///     Unpublishes the microphone's audio track.
        /// </summary>
        public void UnpublishMicrophone()
        {
            _client.UnpublishMicrophone();
            // TODO Remove this. Currently, _room.LocalTrackUnpublished does not fire in standalone builds (LiveKit Bug)
            SetIsMicPublished(false);
        }

        /// <summary>
        ///     Mutes or unmutes the local player's microphone.
        /// </summary>
        public void SetMute(bool mute)
        {
            _client.SetMute(mute);

            // TODO Remove this.
            // "https://github.com/livekit/client-sdk-unity/issues/152"
            SetIsMicMuted(mute);
        }
    }
}
