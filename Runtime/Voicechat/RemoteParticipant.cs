using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnyVR.Voicechat
{
    public class RemoteParticipant : Participant, IDisposable
    {
        private AudioSource _audioSource;

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
        }

        public AudioSource GetAudioSource()
        {
            return _audioSource;
        }

        /// <summary>
        ///     Updates the speaker volume for the local participant.
        /// </summary>
        /// <param name="volume">A value in the range [0,1]</param>
        public void SetVolume(float volume)
        {
            _audioSource.volume = volume;
        }
    }
}
