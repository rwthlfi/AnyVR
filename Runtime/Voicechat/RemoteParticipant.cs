using System;
using LiveKit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnyVR.Voicechat
{
    public class RemoteParticipant : Participant, IDisposable
    {
        private AudioSource _audioSource;

        private IDisposable _audioStream;

        internal RemoteParticipant(string sid, string identity, string name) : base(sid, identity, name)
        {
        }

        public void Dispose()
        {
            Object.Destroy(_audioSource);
        }

        internal void SetAudioSource(AudioSource audioSource)
        {
            _audioSource = audioSource;
            SetIsMicPublished(audioSource != null);
        }

        /// <summary>
        ///     Returns the AudioSource that plays the participant's audio track.
        /// </summary>
        public AudioSource GetAudioSource()
        {
            return _audioSource;
        }

        internal IDisposable GetAudioStream()
        {
            return _audioStream;
        }

        /// <summary>
        ///     Sets the playback volume of this remote player's audio stream.
        ///     The volume is clamped to the range [0, 1].
        /// </summary>
        /// <param name="volume">The desired volume level, where 0 is silent and 1 is full volume.</param>
        public void SetVolume(float volume)
        {
            _audioSource.volume = Math.Clamp(volume, 0, 1);
        }

#if !UNITY_WEBGL
        internal void SetAudioStream(AudioStream audioStream)
        {
            _audioStream = audioStream;
        }
#endif
    }
}
