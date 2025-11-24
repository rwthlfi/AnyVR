#if UNITY_WEBGL
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiveKit;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;
using LogLevel = AnyVR.Logging.LogLevel;

namespace AnyVR.Voicechat
{
    internal class WebGLVoicechatClient : LiveKitClient
    {
        private JSPromise<LocalTrackPublication> _audioTrack;

        private bool _isConnected;
        private Room _room;

        public override bool IsConnected => _isConnected;

        public override void Dispose()
        {
            _room.Disconnect();
        }

        // Ignores device name
        internal override Task<MicrophonePublishResult> PublishMicrophone(string deviceName)
        {
            _audioTrack = _room.LocalParticipant.SetMicrophoneEnabled(true);
            if (_audioTrack == null || _audioTrack.IsError)
            {
                return Task.FromResult(MicrophonePublishResult.Error);
            }
            return Task.FromResult(MicrophonePublishResult.Published);
        }

        internal override void UnpublishMicrophone()
        {
            _room.LocalParticipant.SetMicrophoneEnabled(false);
        }

        internal override void SetMute(bool mute)
        {
            if (mute)
            {
                _audioTrack.ResolveValue?.AudioTrack.Mute();
            }
            else
            {
                _audioTrack.ResolveValue?.AudioTrack.Unmute();
            }

        }

        protected override void Init()
        {
            _room = new Room();

            _room.ParticipantConnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), $"Participant Connected! Name: {participant.Identity}");
                Remotes.Add(participant.Identity, new RemoteParticipant(participant.Sid, participant.Identity, participant.Name));
                OnParticipantConnected?.Invoke(Remotes[participant.Identity]);
            };

            _room.ParticipantDisconnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), $"Participant Disconnected! Name: {participant.Identity}");
                OnParticipantDisconnected?.Invoke(participant.Identity);
            };

            _room.TrackSubscribed += (track, _, participant) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), $"Track Subscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                switch (track.Kind)
                {
                    case TrackKind.Video:
                        {
                            throw new NotImplementedException();
                        }
                    case TrackKind.Audio:
                        track.Attach(); // attaching an audio track suffices to play the audio 
                        Remotes[participant.Identity].SetIsMicPublished(true);
                        break;
                    case TrackKind.Unknown:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };

            _room.TrackUnsubscribed += (track, publication, participant) =>
            {
                if (track.Kind == TrackKind.Audio)
                {
                    Remotes[participant.Identity].SetIsMicPublished(false);
                }
            };

            _room.TrackPublished += (_, participant) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), $"Track Published! Participant: {participant.Identity}");
            };

            _room.ActiveSpeakersChanged += speakers =>
            {
                OnActiveSpeakerChange(speakers.Select(p => p is LiveKit.LocalParticipant ? (Participant)LocalParticipant : Remotes.GetValueOrDefault(p.Identity)).Where(p => p is not null).ToHashSet());
            };
            
            _room.LocalTrackPublished += (_, _) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), "Local Track Published!");
                LocalParticipant.SetIsMicPublished(true);
            };
            
            _room.LocalTrackUnpublished += (_, _) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), "Local Track Unpublished!");
                LocalParticipant.SetIsMicPublished(false);
            };

            _room.Disconnected += _ =>
            {
                _isConnected = false;
            };

            _room.TrackMuted += OnTrackMuteChange;

            _room.TrackUnmuted += OnTrackMuteChange;

            Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), "WebGLVoicechatClient initialized!");
        }

        private void OnTrackMuteChange(TrackPublication publication, LiveKit.Participant participant)
        {
            Debug.Log($"OnTrackMuteChange: {publication.IsMuted}");
            if (participant is LiveKit.RemoteParticipant)
            {
                Remotes[participant.Identity].SetIsMicMuted(publication.IsMuted);
            }
            else if (participant is LiveKit.LocalParticipant)
            {
                LocalParticipant.SetIsMicMuted(publication.IsMuted);
            }
        }

        public override Task<LiveKitConnectionResult> Connect(string address, string token)
        {
            Task<LiveKitConnectionResult> result = ConnectionAwaiter.WaitForResult();
            StartCoroutine(Co_Connect(address, token));
            return result;
        }

        public override void Disconnect()
        {
            _room.Disconnect();
        }

        public override event Action<RemoteParticipant> OnParticipantConnected;

        public override event Action<string> OnParticipantDisconnected;

        private IEnumerator Co_Connect(string address, string token)
        {
            ConnectOperation op = _room.Connect(address, token, new RoomConnectOptions
            {
                AutoSubscribe = true
            });

            yield return op;

            if (op.IsError)
            {
                Logger.Log(LogLevel.Error, nameof(WebGLVoicechatClient), $"Could not connect to LiveKit room!\n {op.Error}");
            }
            else
            {
                Logger.Log(LogLevel.Info, nameof(WebGLVoicechatClient), "Successfully connected to LiveKit room!");

                LocalParticipant = new LocalParticipant(this, _room.LocalParticipant.Sid, _room.LocalParticipant.Identity, _room.LocalParticipant.Name);
                foreach (LiveKit.RemoteParticipant remote in _room.RemoteParticipants.Values)
                {
                    Remotes.Add(remote.Identity, new RemoteParticipant(remote.Sid, remote.Identity, remote.Name));
                }

                _isConnected = true;
                ConnectionAwaiter.Complete(LiveKitConnectionResult.Connected);
            }
        }
    }
}
#endif
