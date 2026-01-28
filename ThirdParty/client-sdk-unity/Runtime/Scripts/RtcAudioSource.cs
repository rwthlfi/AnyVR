using LiveKit.Internal;
using LiveKit.Internal.FFIClients.Requests;
using LiveKit.Proto;
using System;
using System.Buffers;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace LiveKit
{
    /// <summary>
    /// Defines the type of audio source, influencing processing behavior.
    /// </summary>
    public enum RtcAudioSourceType
    {
        AudioSourceCustom = 0,
        AudioSourceMicrophone = 1
    }

    /// <summary>
    /// Capture source for a local audio track.
    /// Handles audio format conversion (channels and sample rate) automatically.
    /// </summary>
    public abstract class RtcAudioSource : IRtcSource, IDisposable
    {
        /// <summary>
        /// Event triggered when audio samples are captured from the underlying source.
        /// Provides the audio data, channel count, and sample rate.
        /// </summary>
        /// <remarks>
        /// This event is not guaranteed to be called on the main thread.
        /// </remarks>
        public abstract event Action<float[], int, int> AudioRead;

        // ============================================================
        // Default Sample Rate Configuration
        // ============================================================
        // Platform-specific defaults based on typical hardware capabilities:
        //
        // | Platform | Default    | Reason                                    |
        // |----------|------------|-------------------------------------------|
        // | iOS      | 24000Hz    | iOS mics typically output 24kHz natively  |
        // | Android  | 24000Hz    | Good balance for mobile devices           |
        // | Desktop  | 48000Hz    | Standard professional audio rate          |
        // ============================================================
#if UNITY_IOS && !UNITY_EDITOR || UNITY_ANDROID && !UNITY_EDITOR
        public static uint DefaultSampleRate = 48000;
        public static uint DefaultMicrophoneSampleRate = 24000;  // Optimized for mobile mics
#else
        public static uint DefaultSampleRate = 48000;
        public static uint DefaultMicrophoneSampleRate = DefaultSampleRate;
#endif
        public static uint DefaultChannels = 2;

        private readonly RtcAudioSourceType _sourceType;
        public RtcAudioSourceType SourceType => _sourceType;

        internal readonly FfiHandle Handle;
        protected AudioSourceInfo _info;

        // Expected audio format - incoming audio will be converted to match these values
        private readonly uint _expectedChannels;
        private readonly uint _expectedSampleRate;

        /// <summary>
        /// Temporary frame buffer for invoking the FFI capture method.
        /// </summary>
        private NativeArray<short> _frameData;

        /// <summary>
        /// Cache for resampling to reduce allocations.
        /// </summary>
        private float[] _resampleBuffer = null;

        private bool _muted = false;
        public override bool Muted => _muted;

        private bool _started = false;
        private bool _disposed = false;

        private readonly object _lock = new object();

        /// <summary>
        /// Creates a new RTC audio source with specified parameters.
        /// </summary>
        /// <param name="channels">
        /// Number of audio channels to send to LiveKit (1=mono, 2=stereo).
        /// Incoming audio will be converted to this channel count if different.
        /// </param>
        /// <param name="audioSourceType">
        /// Type of audio source. Affects default sample rate selection:
        /// - AudioSourceMicrophone: Uses DefaultMicrophoneSampleRate
        /// - AudioSourceCustom: Uses DefaultSampleRate
        /// </param>
        /// <param name="customSampleRate">
        /// Custom sample rate in Hz. If null or &lt;= 0, uses platform-specific default.
        /// Common values: 16000 (speech), 24000 (mobile), 44100 (CD), 48000 (professional)
        /// </param>
        protected RtcAudioSource(int channels = 2, RtcAudioSourceType audioSourceType = RtcAudioSourceType.AudioSourceCustom, int? customSampleRate = null)
        {
            _sourceType = audioSourceType;

            try
            {
                using var request = FFIBridge.Instance.NewRequest<NewAudioSourceRequest>();
                var newAudioSource = request.request;
                newAudioSource.Type = AudioSourceType.AudioSourceNative;
                newAudioSource.NumChannels = (uint)channels;

                // Sample rate selection: custom value takes priority, otherwise use platform default
                if (customSampleRate.HasValue && customSampleRate.Value > 0)
                {
                    newAudioSource.SampleRate = (uint)customSampleRate.Value;
                    UnityEngine.Debug.Log($"Using custom sample rate: {customSampleRate.Value}Hz");
                }
                else
                {
                    newAudioSource.SampleRate = _sourceType == RtcAudioSourceType.AudioSourceMicrophone 
                        ? DefaultMicrophoneSampleRate 
                        : DefaultSampleRate;
                    UnityEngine.Debug.Log($"Using default sample rate: {newAudioSource.SampleRate}Hz");
                }

                // Store expected values for format conversion in OnAudioRead
                _expectedChannels = newAudioSource.NumChannels;
                _expectedSampleRate = newAudioSource.SampleRate;

                UnityEngine.Debug.Log($"NewAudioSource: {newAudioSource.NumChannels}ch @ {newAudioSource.SampleRate}Hz");

                newAudioSource.Options = request.TempResource<AudioSourceOptions>();
                newAudioSource.Options.EchoCancellation = true;
                newAudioSource.Options.AutoGainControl = true;
                newAudioSource.Options.NoiseSuppression = true;

                using var response = request.Send();
                FfiResponse res = response;
                _info = res.NewAudioSource.Source.Info;
                Handle = FfiHandle.FromOwnedHandle(res.NewAudioSource.Source.Handle);

                if (Handle == null || Handle.IsInvalid)
                {
                    throw new InvalidOperationException("Failed to create audio source handle");
                }

                UnityEngine.Debug.Log($"✓ Audio source created: Handle={Handle.DangerousGetHandle()}, Channels={_expectedChannels}, SampleRate={_expectedSampleRate}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"RtcAudioSource constructor failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Begin capturing audio samples from the underlying source.
        /// </summary>
        public virtual void Start()
        {
            if (_started) return;
            AudioRead += OnAudioRead;
            _started = true;
        }

        /// <summary>
        /// Stop capturing audio samples from the underlying source.
        /// </summary>
        public virtual void Stop()
        {
            if (!_started) return;
            AudioRead -= OnAudioRead;
            _started = false;
        }

        /// <summary>
        /// Processes incoming audio data and sends it to LiveKit.
        /// Automatically handles format conversion if needed.
        /// </summary>
        /// <remarks>
        /// Processing flow:
        /// 1. Check if channel conversion needed (e.g., mono mic → stereo expected)
        /// 2. Check if sample rate conversion needed (e.g., 44100Hz → 48000Hz)
        /// 3. Convert float to S16 and send to LiveKit
        /// </remarks>
        private void OnAudioRead(float[] data, int channels, int sampleRate)
        {
            if (_muted) return;

            lock (_lock)
            {
                try
                {
                    bool needsChannelConversion = channels != _expectedChannels;
                    bool needsSampleRateConversion = sampleRate != _expectedSampleRate;

                    float[] processedData = data;
                    int processedChannels = channels;
                    int processedSampleRate = sampleRate;

                    // === Channel Conversion ===
                    if (needsChannelConversion)
                    {
                        processedData = ConvertChannels(processedData, channels, (int)_expectedChannels);
                        processedChannels = (int)_expectedChannels;
                    }

                    // === Sample Rate Conversion ===
                    if (needsSampleRateConversion)
                    {
                        processedData = ResampleAudio(processedData, processedChannels, sampleRate, (int)_expectedSampleRate);
                        processedSampleRate = (int)_expectedSampleRate;
                    }

                    // === Convert to S16 and Send ===
                    SendAudioFrame(processedData, processedChannels, processedSampleRate);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"OnAudioRead failed: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Resamples audio using linear interpolation.
        /// </summary>
        /// <remarks>
        /// Formula: output[i] = sample1 + (sample2 - sample1) × fraction
        /// 
        /// Linear interpolation is computationally efficient and acceptable
        /// for real-time voice communication.
        /// </remarks>
        private float[] ResampleAudio(float[] inputData, int channels, int inputSampleRate, int outputSampleRate)
        {
            if (inputSampleRate == outputSampleRate)
                return inputData;

            int inputSamplesPerChannel = inputData.Length / channels;
            double ratio = (double)outputSampleRate / inputSampleRate;
            int outputSamplesPerChannel = (int)Math.Ceiling(inputSamplesPerChannel * ratio);
            int outputLength = outputSamplesPerChannel * channels;

            float[] result = ArrayPool<float>.Shared.Rent(outputLength);

            try
            {
                var inputSpan = inputData.AsSpan();
                var outputSpan = result.AsSpan(0, outputLength);

                for (int ch = 0; ch < channels; ch++)
                {
                    for (int i = 0; i < outputSamplesPerChannel; i++)
                    {
                        double srcPos = i / ratio;
                        int srcIndex = (int)srcPos;
                        double frac = srcPos - srcIndex;

                        int idx1 = Math.Min(srcIndex * channels + ch, inputData.Length - 1);
                        int idx2 = Math.Min((srcIndex + 1) * channels + ch, inputData.Length - 1);

                        float sample1 = inputSpan[idx1];
                        float sample2 = inputSpan[idx2];

                        outputSpan[i * channels + ch] = (float)(sample1 + (sample2 - sample1) * frac);
                    }
                }

                float[] finalResult = new float[outputLength];
                outputSpan.CopyTo(finalResult);
                return finalResult;
            }
            finally
            {
                ArrayPool<float>.Shared.Return(result);
            }
        }

        /// <summary>
        /// Converts audio between different channel configurations.
        /// </summary>
        /// <remarks>
        /// Mono → Stereo: Duplicate to both channels
        ///   [M0][M1] → [L0][R0][L1][R1] where L=R=M
        /// 
        /// Stereo → Mono: Average both channels
        ///   [L0][R0][L1][R1] → [M0][M1] where M=(L+R)/2
        /// </remarks>
        private float[] ConvertChannels(float[] data, int sourceChannels, int targetChannels)
        {
            if (sourceChannels == targetChannels)
                return data;

            int samplesPerChannel = data.Length / sourceChannels;
            float[] converted = new float[samplesPerChannel * targetChannels];

            // Mono → Stereo
            if (sourceChannels == 1 && targetChannels == 2)
            {
                for (int i = 0; i < samplesPerChannel; i++)
                {
                    float sample = data[i];
                    converted[i * 2] = sample;       // Left
                    converted[i * 2 + 1] = sample;   // Right
                }
            }
            // Stereo → Mono
            else if (sourceChannels == 2 && targetChannels == 1)
            {
                for (int i = 0; i < samplesPerChannel; i++)
                {
                    converted[i] = (data[i * 2] + data[i * 2 + 1]) * 0.5f;
                }
            }
            // Other conversions (multi-channel)
            else
            {
                for (int i = 0; i < samplesPerChannel; i++)
                {
                    for (int ch = 0; ch < targetChannels; ch++)
                    {
                        int sourceChannel = Math.Min(ch, sourceChannels - 1);
                        converted[i * targetChannels + ch] = data[i * sourceChannels + sourceChannel];
                    }
                }
            }

            return converted;
        }

        /// <summary>
        /// Converts float samples to S16 format and sends to LiveKit.
        /// </summary>
        /// <remarks>
        /// Float to S16 conversion:
        /// - Input range: [-1.0, 1.0]
        /// - Output range: [-32768, 32767]
        /// </remarks>
        private void SendAudioFrame(float[] data, int channels, int sampleRate)
        {
            if (data == null || data.Length == 0)
            {
                UnityEngine.Debug.LogWarning("Empty audio data, skipping frame");
                return;
            }

            int samplesPerChannel = data.Length / channels;
            if (samplesPerChannel <= 0)
            {
                UnityEngine.Debug.LogWarning($"Invalid audio data: length={data.Length}, channels={channels}");
                return;
            }

            // Allocate or resize frame buffer
            if (_frameData.Length != data.Length)
            {
                if (_frameData.IsCreated)
                    _frameData.Dispose();
                _frameData = new NativeArray<short>(data.Length, Allocator.Persistent);
            }

            // Convert float to S16
            static short FloatToS16(float v)
            {
                v *= 32768f;
                v = Math.Min(v, 32767f);
                v = Math.Max(v, -32768f);
                return (short)(v + Math.Sign(v) * 0.5f);
            }

            for (int i = 0; i < data.Length; i++)
            {
                _frameData[i] = FloatToS16(data[i]);
            }

            // Send frame to RTC
            using var request = FFIBridge.Instance.NewRequest<CaptureAudioFrameRequest>();
            using var audioFrameBufferInfo = request.TempResource<AudioFrameBufferInfo>();

            var pushFrame = request.request;
            pushFrame.SourceHandle = (ulong)Handle.DangerousGetHandle();
            pushFrame.Buffer = audioFrameBufferInfo;

            unsafe
            {
                pushFrame.Buffer.DataPtr = (ulong)NativeArrayUnsafeUtility.GetUnsafePtr(_frameData);
            }

            pushFrame.Buffer.NumChannels = (uint)channels;
            pushFrame.Buffer.SampleRate = (uint)sampleRate;
            pushFrame.Buffer.SamplesPerChannel = (uint)samplesPerChannel;

            using var response = request.Send();
            FfiResponse res = response;

            var asyncId = res.CaptureAudioFrame.AsyncId;
            void Callback(CaptureAudioFrameCallback callback)
            {
                if (callback.AsyncId != asyncId) return;
                if (callback.HasError)
                {
                    UnityEngine.Debug.LogError($"Audio capture failed: {callback.Error}");
                }
                FfiClient.Instance.CaptureAudioFrameReceived -= Callback;
            }
            FfiClient.Instance.CaptureAudioFrameReceived += Callback;
        }

        /// <summary>
        /// Mutes or unmutes the audio source.
        /// </summary>
        public override void SetMute(bool muted)
        {
            _muted = muted;
        }

        /// <summary>
        /// Disposes of the audio source, stopping it first if necessary.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Stop();
                if (_frameData.IsCreated)
                    _frameData.Dispose();
                _resampleBuffer = null;
                Handle?.Dispose();
            }

            _disposed = true;
        }

        ~RtcAudioSource()
        {
            Dispose(false);
        }

        [Obsolete("No longer used, audio sources should perform any preparation in Start() asynchronously")]
        public virtual IEnumerator Prepare(float timeout = 0) { yield break; }

        [Obsolete("Use Start() instead")]
        public IEnumerator PrepareAndStart()
        {
            Start();
            yield break;
        }
    }
}