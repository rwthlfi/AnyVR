using System;
using LiveKit;
using UnityEngine;

namespace AnyVR.Voicechat
{
    public class RemoteParticipant : Participant, IDisposable
    {
        private GameObject _audioObject;
        
        private AudioSource _audioSource;
        
        private AudioStream _audioStream;

        private RemoteAudioTrack _audioTrack;
        
        private bool _disposed;

        internal RemoteParticipant(string sid) : base(sid)
        {
        }

        internal void SetAudioTrack(RemoteAudioTrack track)
        {
            _audioTrack = track;
            if (_audioObject != null)
            {
                // Reattach
                Attach(_audioObject);
            }
        }
        
        /// <summary>
        /// Updates the speaker volume for the local participant.
        /// </summary>
        /// <param name="volume">A value in the range [0,1]</param>
        public void SetVolume(float volume)
        {
            // TODO
        }

        public void Attach(GameObject audioObject)
        {
            if (_audioSource != null)
            {
                Dispose();
            }
            
            Debug.Log("Attaching audio object");
            _audioObject = audioObject;
            _audioSource = _audioObject.AddComponent<AudioSource>();
            _audioStream = new AudioStream(_audioTrack, _audioSource);
            Debug.Log($"source: {_audioSource}");
            
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _audioSource?.Stop();
            _audioStream?.Dispose();
            if (_audioSource != null)
            {
                UnityEngine.Object.Destroy(_audioSource);
            }

            _audioSource = null;
            _audioStream = null;
            _audioObject = null;
        }
    }
}
