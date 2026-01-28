using LiveKit.Internal;
using LiveKit.Internal.FFIClients.Requests;
using LiveKit.Proto;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LiveKit
{
    public sealed class AudioStream : IDisposable
    {
        internal readonly FfiHandle Handle;
        private readonly AudioSource _audioSource;
        private RingBuffer _buffer;
        private short[] _tempBuffer;
        private uint _numChannels;
        private uint _sampleRate;
        private AudioResampler _resampler = new AudioResampler();
        private object _lock = new object();
        private bool _disposed = false;

        // Buffers for mono to stereo conversion workaround
        private short[] _conversionBuffer = null;
        private short[] _resampleBuffer = null;

        public AudioStream(RemoteAudioTrack audioTrack, AudioSource source)
        {
            if (!audioTrack.Room.TryGetTarget(out var room))
                throw new InvalidOperationException("audiotrack's room is invalid");

            if (!audioTrack.Participant.TryGetTarget(out var participant))
                throw new InvalidOperationException("audiotrack's participant is invalid");

            using var request = FFIBridge.Instance.NewRequest<NewAudioStreamRequest>();
            var newAudioStream = request.request;
            newAudioStream.TrackHandle = (ulong)(audioTrack as ITrack).TrackHandle.DangerousGetHandle();
            newAudioStream.Type = AudioStreamType.AudioStreamNative;

            using var response = request.Send();
            FfiResponse res = response;
            Handle = FfiHandle.FromOwnedHandle(res.NewAudioStream.Stream.Handle);
            FfiClient.Instance.AudioStreamEventReceived += OnAudioStreamEvent;

            _audioSource = source;
            ConfigureAudioSource(_audioSource);

            var probe = _audioSource.gameObject.AddComponent<AudioProbe>();
            probe.AudioRead += OnAudioRead;

            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.mute = false;

            // ============================================================
            // Android-specific AudioSource configuration
            // ============================================================
            // priority = 0:
            //   - Highest priority (0 = highest, 256 = lowest/default)
            //   - Unity has limited audio channels on Android (~32)
            //   - When exceeded, lower priority sources are muted
            //   - Remote audio streams must never be muted by the system
            //
            // outputAudioMixerGroup = null:
            //   - Bypass AudioMixer, output directly to system
            //   - Reduces audio processing latency
            //   - Avoids AudioMixer effects (compression, EQ) affecting voice
            //   - Prevents AudioMixer-related issues on some Android devices
            // ============================================================
#if UNITY_ANDROID && !UNITY_EDITOR
            source.priority = 0;
            source.outputAudioMixerGroup = null;
#endif

            int frequency = AudioSettings.outputSampleRate;
            if (frequency == 0) frequency = 48000;

            var dummyClip = AudioClip.Create(
                "AudioStreamClip",
                frequency * 2,
                2,
                frequency,
                false
            );

            source.clip = dummyClip;
        }

        private void OnAudioRead(float[] data, int channels, int sampleRate)
        {
            lock (_lock)
            {
                Array.Clear(data, 0, data.Length);

                if (_buffer == null || channels != _numChannels || sampleRate != _sampleRate)
                {
                    // ============================================================
                    // Platform-specific buffer size configuration
                    // ============================================================
                    // Android: 500ms buffer - AudioTrack callback timing is unstable,
                    //          hardware varies across manufacturers
                    // iOS/Desktop: 200ms buffer - Core Audio and WASAPI provide 
                    //              stable, low-latency callbacks
                    // ============================================================
#if UNITY_ANDROID && !UNITY_EDITOR
                    int size = (int)(channels * sampleRate * 0.5);
#else
                    int size = (int)(channels * sampleRate * 0.2);
#endif

                    _buffer?.Dispose();
                    _buffer = new RingBuffer(size * sizeof(short));
                    _numChannels = (uint)channels;
                    _sampleRate = (uint)sampleRate;
                    _tempBuffer = null;
                }

                if (_tempBuffer == null || data.Length != _tempBuffer.Length)
                {
                    _tempBuffer = new short[data.Length];
                }

                Array.Clear(_tempBuffer, 0, _tempBuffer.Length);

                static float S16ToFloat(short v)
                {
                    return v / 32768f;
                }

                var temp = MemoryMarshal.Cast<short, byte>(_tempBuffer.AsSpan());
                int read = _buffer.Read(temp);
                int samplesRead = read / sizeof(short);

                if (samplesRead > 0)
                {
                    for (int i = 0; i < samplesRead && i < data.Length; i++)
                    {
                        data[i] = S16ToFloat(_tempBuffer[i]);
                    }
                }
            }
        }

        private void OnAudioStreamEvent(AudioStreamEvent e)
        {
            if ((ulong)Handle.DangerousGetHandle() != e.StreamHandle)
                return;

            if (e.MessageCase != AudioStreamEvent.MessageOneofCase.FrameReceived)
                return;

            var frame = new AudioFrame(e.FrameReceived.Frame);

            lock (_lock)
            {
                // Force-initialize buffer if not ready when frame arrives
                if (_buffer == null || _numChannels == 0)
                {
                    int channels = AudioSettings.GetConfiguration().speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
                    int sampleRate = AudioSettings.outputSampleRate;
                    if (sampleRate == 0) sampleRate = 48000;

#if UNITY_ANDROID && !UNITY_EDITOR
                    int size = (int)(channels * sampleRate * 0.5);
#else
                    int size = (int)(channels * sampleRate * 0.2);
#endif

                    _buffer?.Dispose();
                    _buffer = new RingBuffer(size * sizeof(short));
                    _numChannels = (uint)channels;
                    _sampleRate = (uint)sampleRate;
                }

                unsafe
                {
                    var uFrame = _resampler.RemixAndResample(frame, _numChannels, _sampleRate);

                    // ============================================================
                    // WORKAROUND: Enhanced Mono→Stereo conversion with resampling
                    // ============================================================
                    // When native resampler fails for mono→stereo conversion,
                    // perform manual conversion as fallback.
                    // ============================================================
                    if ((uFrame == null || uFrame.Length == 0) && frame.NumChannels == 1 && _numChannels == 2)
                    {
                        int inputSamplesPerChannel = (int)frame.SamplesPerChannel;

                        // Validate frame data
                        if (inputSamplesPerChannel <= 0 || frame.Length != inputSamplesPerChannel * sizeof(short))
                        {
                            return;
                        }

                        short* monoPtr = (short*)frame.Data.ToPointer();

                        // ---------------------------------------------------------
                        // Step 1: Resample mono data if sample rates differ
                        // ---------------------------------------------------------
                        short[] monoData;
                        int monoSampleCount;

                        if (frame.SampleRate != _sampleRate)
                        {
                            int outputSampleCount = (int)(inputSamplesPerChannel * _sampleRate / frame.SampleRate);

                            if (_resampleBuffer == null || _resampleBuffer.Length < outputSampleCount)
                            {
                                _resampleBuffer = new short[outputSampleCount];
                            }

                            // Linear interpolation resampling
                            float ratio = (float)frame.SampleRate / _sampleRate;
                            for (int i = 0; i < outputSampleCount; i++)
                            {
                                float srcIndex = i * ratio;
                                int srcIndexInt = (int)srcIndex;
                                float frac = srcIndex - srcIndexInt;

                                if (srcIndexInt + 1 < inputSamplesPerChannel)
                                {
                                    short sample1 = monoPtr[srcIndexInt];
                                    short sample2 = monoPtr[srcIndexInt + 1];
                                    _resampleBuffer[i] = (short)(sample1 + frac * (sample2 - sample1));
                                }
                                else
                                {
                                    _resampleBuffer[i] = monoPtr[srcIndexInt];
                                }
                            }

                            monoData = _resampleBuffer;
                            monoSampleCount = outputSampleCount;
                        }
                        else
                        {
                            // No resampling needed, just copy
                            if (_resampleBuffer == null || _resampleBuffer.Length < inputSamplesPerChannel)
                            {
                                _resampleBuffer = new short[inputSamplesPerChannel];
                            }

                            for (int i = 0; i < inputSamplesPerChannel; i++)
                            {
                                _resampleBuffer[i] = monoPtr[i];
                            }

                            monoData = _resampleBuffer;
                            monoSampleCount = inputSamplesPerChannel;
                        }

                        // ---------------------------------------------------------
                        // Step 2: Convert mono to stereo (duplicate to both channels)
                        // ---------------------------------------------------------
                        int stereoSampleCount = monoSampleCount * 2;

                        if (_conversionBuffer == null || _conversionBuffer.Length < stereoSampleCount)
                        {
                            _conversionBuffer = new short[stereoSampleCount];
                        }

                        for (int i = 0; i < monoSampleCount; i++)
                        {
                            short sample = monoData[i];
                            _conversionBuffer[i * 2] = sample;       // Left channel
                            _conversionBuffer[i * 2 + 1] = sample;   // Right channel
                        }

                        // ---------------------------------------------------------
                        // Step 3: Write to ring buffer
                        // ---------------------------------------------------------
                        var stereoBytes = MemoryMarshal.Cast<short, byte>(_conversionBuffer.AsSpan(0, stereoSampleCount));
                        _buffer.Write(stereoBytes);

                        return;
                    }

                    // Normal path: native resampler succeeded
                    if (uFrame != null && uFrame.Length > 0)
                    {
                        var data = new Span<byte>(uFrame.Data.ToPointer(), uFrame.Length);
                        _buffer.Write(data);
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                lock (_lock)
                {
                    FfiClient.Instance.AudioStreamEventReceived -= OnAudioStreamEvent;
                    _audioSource.Stop();

                    if (_audioSource.clip != null)
                    {
                        UnityEngine.Object.Destroy(_audioSource.clip);
                        _audioSource.clip = null;
                    }

                    var probe = _audioSource.GetComponent<AudioProbe>();
                    if (probe != null)
                    {
                        probe.AudioRead -= OnAudioRead;
                        UnityEngine.Object.Destroy(probe);
                    }

                    _buffer?.Dispose();
                    _buffer = null;
                    _conversionBuffer = null;
                    _resampleBuffer = null;
                }
            }
            _disposed = true;
        }

        ~AudioStream()
        {
            Dispose(false);
        }
    }
}