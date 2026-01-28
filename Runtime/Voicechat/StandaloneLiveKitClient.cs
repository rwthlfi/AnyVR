#if UNITY_STANDALONE || UNITY_ANDROID
using System;
using System.Collections;
using System.Collections.Generic;
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

        protected override void Init()
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

            _room.TrackUnsubscribed += (track, publication, participant) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), $"Track Unsubscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                OnTrackUnsubscribed(track, publication, participant);
            };

            _room.ActiveSpeakersChanged += speakers =>
            {
                OnActiveSpeakerChange(speakers.Select(p => p is LiveKit.LocalParticipant ? (Participant)LocalParticipant : Remotes.GetValueOrDefault(p.Identity)).Where(p => p is not null).ToHashSet());
            };

            _room.LocalTrackPublished += (publication, participant) =>
            {
                LocalParticipant.SetIsMicPublished(true);
            };

            _room.LocalTrackUnpublished += (publication, participant) =>
            {
                LocalParticipant.SetIsMicPublished(false);
            };

            _room.TrackMuted += OnTrackMuteChange;

            _room.TrackUnmuted += OnTrackMuteChange;
        }

        private void OnTrackMuteChange(TrackPublication publication, LiveKit.Participant participant)
        {
            Debug.Log($"OnTrackMuteChange: {publication.Muted}");
            if (participant is LiveKitRemoteParticipant)
            {
                Remotes[participant.Identity].SetIsMicMuted(publication.Muted);
            }
            else if (participant is LiveKit.LocalParticipant)
            {
                LocalParticipant.SetIsMicMuted(publication.Muted);
            }
        }

        public override Task<LiveKitConnectionResult> Connect(string address, string token)
        {
            Task<LiveKitConnectionResult> result = ConnectionAwaiter.WaitForResult();
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

            _audioSource = new MicrophoneSource(_activeMicName, gameObject, (int)RtcAudioSource.DefaultChannels, (int)RtcAudioSource.DefaultMicrophoneSampleRate);
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
                _audioTrack = track;
                TrackPublishResult.Complete(MicrophonePublishResult.Published);
            }
        }

        internal override void UnpublishMicrophone()
        {
            _room.LocalParticipant.UnpublishTrack(_audioTrack, true);
            _audioSource.Dispose();
            _audioSource = null;
            _audioTrack = null;
            Logger.Log(LogLevel.Verbose, nameof(StandaloneLiveKitClient), "Local audio track unpublished.");
        }

        public override void Disconnect()
        {
            _room.Disconnect();
        }

        internal override void SetMute(bool mute)
        {
            if (_audioTrack is ILocalTrack track)
            {
                track.SetMute(mute);
            }
        }

        private IEnumerator Co_Connect(string address, string token)
        {
            ConnectInstruction op = _room.Connect(address, token, new RoomOptions());

            yield return op;

            if (op.IsError)
            {
                Logger.Log(LogLevel.Error, nameof(StandaloneLiveKitClient), $"Failed to connect to LiveKit room. address = {address}, token = {token}");
                ConnectionAwaiter.Complete(LiveKitConnectionResult.Error);
            }
            else
            {
                LocalParticipant = new LocalParticipant(this, _room.LocalParticipant.Sid, _room.LocalParticipant.Identity, _room.LocalParticipant.Name);

                foreach (LiveKitRemoteParticipant remote in _room.RemoteParticipants.Values)
                {
                    Remotes.Add(remote.Identity, new RemoteParticipant(remote.Sid, remote.Identity, remote.Name));
                }
                ConnectionAwaiter.Complete(LiveKitConnectionResult.Connected);
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
                    AudioSource audioSource = AudioSourceMap(participant.Identity);

                    if (audioSource == null)
                    {
                        Logger.Log(LogLevel.Error, nameof(StandaloneLiveKitClient), $"Could not fetch audio object for participant {participant.Identity}");
                        return;
                    }

                    AudioStream audioStream = new(audioTrack, audioSource);
                    Remotes[participant.Identity].SetAudioStream(audioStream);
                    Remotes[participant.Identity].SetAudioSource(audioSource);
                    break;
            }
        }

        private void OnTrackUnsubscribed(IRemoteTrack track, RemoteTrackPublication publication, LiveKitRemoteParticipant participant)
        {
            RemoteParticipant remote = Remotes[participant.Identity];
            AudioSource audioSource = remote.GetAudioSource();
            remote.GetAudioStream().Dispose();
            remote.SetAudioSource(null);
            remote.SetAudioStream(null);
            Destroy(audioSource);
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

        public override void Dispose()
        {
            foreach (RemoteParticipant remote in Remotes.Values)
            {
                if (remote == null)
                    continue;

                AudioSource audioSource = remote.GetAudioSource();
                if (audioSource == null)
                    continue;

                audioSource.Stop();
                Destroy(audioSource);
            }
            _room.Disconnect();
        }

#endregion
    }
}
#endif
