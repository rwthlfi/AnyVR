#if UNITY_WEBGL
using LiveKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;
using LogLevel = AnyVR.Logging.LogLevel;

namespace AnyVR.Voicechat
{
    internal class WebGLVoicechatClient : LiveKitClient
    {
        private Room _room;
        
        private bool _isConnected;

        public override void Dispose()
        {
            _room.Disconnect();
        }
        
        // Ignores device name
        internal override Task<MicrophonePublishResult> PublishMicrophone(string deviceName)
        {
            _room.LocalParticipant.SetMicrophoneEnabled(true);
            return Task.FromResult(MicrophonePublishResult.Published);
        }
        
        internal override void UnpublishMicrophone()
        {
            _room.LocalParticipant.SetMicrophoneEnabled(false);
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
                        break;
                    case TrackKind.Unknown:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
            _room.TrackPublished += (_, participant) =>
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), $"Track Published! Participant: {participant.Identity}");
            };
            _room.ActiveSpeakersChanged += speakers =>
            {
                OnActiveSpeakerChanged?.Invoke(speakers.Select(participant => Remotes.GetValueOrDefault(participant.Identity)).Where(remote => remote != null));
            };
            _room.LocalTrackPublished += (_, _) => { Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), "Local Track Published!"); };

            _room.Disconnected += _ =>
            {
                _isConnected = false;
            };

            Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), "WebGLVoicechatClient initialized!");
        }
        
        public override bool IsConnected => _isConnected;

        public override bool IsMicPublished => IsConnected && _room.LocalParticipant.IsMicrophoneEnabled;
        
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
        public override event Action<IEnumerable<RemoteParticipant>> OnActiveSpeakerChanged;

        private IEnumerator Co_Connect(string address, string token)
        {
            ConnectOperation op = _room.Connect(address, token, new RoomConnectOptions { AutoSubscribe = true });

            yield return op;

            if (op.IsError)
            {
                Debug.LogError($"Could not connect to LiveKit room!\n {op.Error}");
            }
            else
            {
                Logger.Log(LogLevel.Verbose, nameof(WebGLVoicechatClient), "Successfully connected to LiveKit room!");

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
