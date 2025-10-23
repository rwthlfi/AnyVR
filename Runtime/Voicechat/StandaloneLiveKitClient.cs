#if UNITY_STANDALONE
using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using LiveKit;
using LiveKit.Proto;
using UnityEngine;
using RoomOptions = LiveKit.RoomOptions;
using Logger = AnyVR.Logging.Logger;
using LogLevel = AnyVR.Logging.LogLevel;
using LiveKitRemoteParticipant = LiveKit.RemoteParticipant;

namespace AnyVR.Voicechat
{
    internal class StandaloneLiveKitClient : LiveKitClient
    {
        public override bool IsConnected => _room is { IsConnected: true };

        public override void Init()
        {
            _room = new Room();

            _room.ParticipantConnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), $"Participant Connected! Name: {participant.Name}");
                Remotes.Add(participant.Identity, new RemoteParticipant(participant.Sid, participant.Identity, participant.Name));
                OnParticipantConnected?.Invoke(Remotes[participant.Identity]);
            };

            _room.ParticipantDisconnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), $"Participant Disconnected! Name: {participant.Identity}");
                Remotes.Remove(participant.Identity);
                OnParticipantDisconnected?.Invoke(participant.Identity);
            };

            _room.TrackSubscribed += (track, publication, participant) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), $"Track Subscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                OnTrackSubscribed(track, publication, participant);
            };

            _room.ActiveSpeakersChanged += speakers =>
            {
                UpdateActiveSpeakers(speakers.Select(speaker => speaker.Identity).ToHashSet());
            };
        }

        public override Task<ConnectionResult> Connect(string address, string token)
        {
            Task<ConnectionResult> result = ConnectionAwaiter.WaitForResult();
            StartCoroutine(Co_Connect(address, token));
            return result;
        }

        internal override Task<MicrophonePublishResult> PublishMicrophone(string deviceName)
        {
            if (!IsConnected)
            {
                return Task.FromResult(MicrophonePublishResult.NotConnected);
            }

            if (_audioSource != null)
            {
                UnpublishMicrophone();
            }

            if (Microphone.devices.All(device => device != deviceName))
            {
                Logger.Log(LogLevel.Error, nameof(StandaloneLiveKitClient), $"Microphone '{deviceName}' is not available.");
                return Task.FromResult(MicrophonePublishResult.MicrophoneNotAvailable);
            }

            _activeMicName = deviceName;

            _audioSource = new MicrophoneSource(_activeMicName, gameObject);
            LocalAudioTrack track = LocalAudioTrack.CreateAudioTrack($"audio-track-{LocalParticipant.Identity}", _audioSource, _room);

            Task<MicrophonePublishResult> task = TrackPublishResult.WaitForResult();
            StartCoroutine(Co_PublishTrack(track));
            return task;
        }

        private IEnumerator Co_PublishTrack(LocalAudioTrack track)
        {
            TrackPublishOptions options = new()
            {
                AudioEncoding = new AudioEncoding
                {
                    MaxBitrate = 64000
                },
                Source = TrackSource.SourceMicrophone
            };

            PublishTrackInstruction publish = _room.LocalParticipant.PublishTrack(track, options);
            yield return publish;

            if (publish.IsError)
            {
                Logger.Log(LogLevel.Error, nameof(StandaloneLiveKitClient), "Failed to publish audio track.");
                TrackPublishResult.Complete(MicrophonePublishResult.Error);
            }
            else
            {
                Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), $"Published audio track! Active microphone: {_activeMicName}");
                _audioSource.Start();
                TrackPublishResult.Complete(MicrophonePublishResult.Published);
            }
        }

        public override void UnpublishMicrophone()
        {
            _room.LocalParticipant.UnpublishTrack(_audioTrack, true);
            _audioSource.Dispose();
            _audioSource = null;
            Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), "Local audio track unpublished.");
        }

        public override void Disconnect()
        {
            _room.Disconnect();
        }

        private IEnumerator Co_Connect(string address, string token)
        {
            if (IsConnected)
            {
                ConnectionAwaiter.Complete(ConnectionResult.AlreadyConnected);
                yield break;
            }

            ConnectInstruction op = _room.Connect(address, token, new RoomOptions());

            yield return op;

            if (op.IsError)
            {
                Logger.Log(LogLevel.Error, nameof(StandaloneLiveKitClient), $"Failed to connect to LiveKit room. address = {address}, token = {token}");
                ConnectionAwaiter.Complete(ConnectionResult.Error);
            }
            else
            {
                LocalParticipant = new LocalParticipant(this, _room.LocalParticipant.Sid, _room.LocalParticipant.Identity, _room.LocalParticipant.Name);

                foreach (LiveKitRemoteParticipant remote in _room.RemoteParticipants.Values)
                {
                    Remotes.Add(remote.Identity, new RemoteParticipant(remote.Sid, remote.Identity, remote.Name));
                }
                ConnectionAwaiter.Complete(ConnectionResult.Connected);
            }
        }

        private void OnTrackSubscribed(IRemoteTrack track, RemoteTrackPublication _, LiveKitRemoteParticipant participant)
        {
            switch (track)
            {
                case RemoteVideoTrack _:
                    Logger.Log(LogLevel.Warning, nameof(StandaloneLiveKitClient), "Video tracks currently not supported."); //TODO
                    break;
                case RemoteAudioTrack audioTrack:
                    GameObject audioObject = AudioObjectMap(participant.Identity);

                    if (audioObject == null)
                    {
                        Logger.Log(LogLevel.Error, nameof(StandaloneLiveKitClient), $"Could not fetch audio object for participant {participant.Identity}");
                        return;
                    }

                    AudioSource audioSource = audioObject.AddComponent<AudioSource>();
                    AudioStream audioStream = new(audioTrack, audioSource);
                    Remotes[participant.Identity].SetAudioSource(audioSource);
                    break;
            }
        }

#region Private Fields

        private string _activeMicName;

        private RtcAudioSource _audioSource;

        private LocalAudioTrack _audioTrack;

        private Room _room;

#endregion

#region Public API

        public override event Action<RemoteParticipant> OnParticipantConnected;
        public override event Action<string> OnParticipantDisconnected;

#endregion
    }
}
#endif
