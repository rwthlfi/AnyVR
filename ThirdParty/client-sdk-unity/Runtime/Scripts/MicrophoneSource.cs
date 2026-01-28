using System;
using System.Collections;
using UnityEngine;

namespace LiveKit
{
    /// <summary>
    /// An audio source which captures from the device's microphone.
    /// </summary>
    /// <remarks>
    /// Ensure microphone permissions are granted before calling <see cref="Start"/>.
    /// </remarks>
    sealed public class MicrophoneSource : RtcAudioSource
    {
        private readonly GameObject _sourceObject;
        private readonly string _deviceName;
        private readonly int _targetSampleRate;

        public override event Action<float[], int, int> AudioRead;

        private bool _disposed = false;
        private bool _started = false;

        /// <summary>
        /// Creates a new microphone source with specified audio parameters.
        /// </summary>
        /// <param name="deviceName">
        /// The microphone device name. Use <see cref="Microphone.devices"/> to get available devices.
        /// Pass null or empty string for the default device.
        /// </param>
        /// <param name="sourceObject">
        /// The GameObject to attach AudioSource and AudioProbe components.
        /// Must remain active in the scene during capture.
        /// </param>
        /// <param name="channels">
        /// Target number of audio channels (1=mono, 2=stereo).
        /// Note: Actual microphone output may differ; RtcAudioSource will convert if needed.
        /// </param>
        /// <param name="sampleRate">
        /// Target sample rate in Hz (e.g., 24000, 44100, 48000).
        /// 
        /// Platform recommendations:
        /// - iOS: 24000Hz (native mic output, avoids resampling)
        /// - Android: 24000Hz or 48000Hz
        /// - Desktop: 48000Hz
        /// </param>
        public MicrophoneSource(string deviceName, GameObject sourceObject, int channels, int sampleRate) 
            : base(channels, RtcAudioSourceType.AudioSourceMicrophone, sampleRate)
        {
            _deviceName = deviceName;
            _sourceObject = sourceObject;
            _targetSampleRate = sampleRate;
            UnityEngine.Debug.Log($"MicrophoneSource created: {channels}ch @ {sampleRate}Hz");
        }

        /// <summary>
        /// Creates a new microphone source with default audio parameters.
        /// Uses 2 channels and platform-specific default sample rate.
        /// </summary>
        /// <remarks>
        /// Default sample rates (defined in RtcAudioSource):
        /// - iOS/Android: 24000Hz (optimized for mobile microphones)
        /// - Desktop: 48000Hz (standard professional audio rate)
        /// </remarks>
        public MicrophoneSource(string deviceName, GameObject sourceObject) 
            : base(2, RtcAudioSourceType.AudioSourceMicrophone)
        {
            _deviceName = deviceName;
            _sourceObject = sourceObject;
        }

        /// <summary>
        /// Begins capturing audio from the microphone.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the microphone is not available or unauthorized.
        /// </exception>
        /// <remarks>
        /// Ensure microphone permissions are granted before calling this method
        /// by calling <see cref="Application.RequestUserAuthorization"/>.
        /// </remarks>
        public override void Start()
        {
            base.Start();
            if (_started) return;

            if (!Application.HasUserAuthorization(mode: UserAuthorization.Microphone))
                throw new InvalidOperationException("Microphone access not authorized");

            MonoBehaviourContext.OnApplicationPauseEvent += OnApplicationPause;
            MonoBehaviourContext.RunCoroutine(StartMicrophone());

            _started = true;
        }

        private IEnumerator StartMicrophone()
        {
            UnityEngine.Debug.Log($"Starting microphone: {_deviceName} @ {_targetSampleRate}Hz");

            // Note: The actual AudioClip parameters may differ from what was requested,
            // depending on hardware capabilities. Unity will use the closest supported configuration.
            var clip = Microphone.Start(
               _deviceName,
               loop: true,
               lengthSec: 1,
               frequency: _targetSampleRate
            );

            if (clip == null)
            {
                UnityEngine.Debug.LogError("Microphone.Start returned null!");
                yield break;
            }

            var source = _sourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;

            var probe = _sourceObject.AddComponent<AudioProbe>();
            probe.ClearAfterInvocation();
            probe.AudioRead += OnAudioRead;

            // Wait until the microphone has started recording
            float waitTime = 0f;
            while (Microphone.GetPosition(_deviceName) <= 0 && waitTime < 5f)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (Microphone.GetPosition(_deviceName) <= 0)
            {
                UnityEngine.Debug.LogError("Microphone failed to start within timeout!");
                yield break;
            }

            // Log actual vs requested parameters
            // The AudioClip's actual channels and frequency may differ from what was requested.
            // RtcAudioSource.OnAudioRead will handle any necessary conversion automatically.
            UnityEngine.Debug.Log($"✓ Microphone ready: {clip.channels}ch @ {clip.frequency}Hz (requested: {_targetSampleRate}Hz)");

            source.Play();

            UnityEngine.Debug.Log("✓ Microphone started and playing");
        }

        /// <summary>
        /// Stops capturing audio from the microphone.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            MonoBehaviourContext.RunCoroutine(StopMicrophone());
            MonoBehaviourContext.OnApplicationPauseEvent -= OnApplicationPause;
            _started = false;
        }

        private IEnumerator StopMicrophone()
        {
            if (Microphone.IsRecording(_deviceName))
                Microphone.End(_deviceName);

            var probe = _sourceObject.GetComponent<AudioProbe>();
            if (probe != null)
            {
                probe.AudioRead -= OnAudioRead;
                UnityEngine.Object.Destroy(probe);
            }

            var source = _sourceObject.GetComponent<AudioSource>();
            if (source != null)
            {
                UnityEngine.Object.Destroy(source);
            }
            yield return null;
        }

        /// <summary>
        /// Receives audio data from Unity's audio system and forwards it to RtcAudioSource.
        /// The callback provides ACTUAL audio data from the microphone hardware,
        /// which may differ from the requested format.
        /// RtcAudioSource.OnAudioRead will automatically convert if needed.
        /// </summary>
        private void OnAudioRead(float[] data, int channels, int sampleRate)
        {
            AudioRead?.Invoke(data, channels, sampleRate);
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause && _started)
                MonoBehaviourContext.RunCoroutine(RestartMicrophone());
        }

        private IEnumerator RestartMicrophone()
        {
            yield return StopMicrophone();
            yield return StartMicrophone();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing) Stop();
            _disposed = true;
            base.Dispose(disposing);
        }

        ~MicrophoneSource()
        {
            Dispose(false);
        }
    }
}