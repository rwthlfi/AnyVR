#if UNITY_WEBGL && !UNITY_EDITOR
using AnyVR.Logging;
using LiveKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;
using LogLevel = AnyVR.Logging.LogLevel;

namespace AnyVR.Voicechat
{
    internal class WebGLVoicechatClient : VoicechatClient
    {
        private const string Tag = nameof(WebGLVoicechatClient);
        private Room _room;
        internal override event Action<Participant> ParticipantConnected;
        internal override event Action<string> ParticipantDisconnected;
        internal override event Action ConnectedToRoom;
        internal override event Action<string> VideoReceived;

        internal override void Init()
        {
            _room = new Room();
            _room.ParticipantConnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Participant Connected! Name: {participant.Identity}");
                Remotes.Add(participant.Sid, new Participant(participant.Sid, participant.Identity));
                ParticipantConnected?.Invoke(Remotes[participant.Sid]);
            };
            _room.ParticipantDisconnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Participant Disconnected! Name: {participant.Identity}");
                ParticipantDisconnected?.Invoke(participant.Sid);
            };
            _room.TrackSubscribed += (track, _, participant) =>
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Track Subscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                switch (track.Kind)
                {
                    case TrackKind.Video:
                        {
                            HTMLVideoElement video = track.Attach() as HTMLVideoElement;
                            video!.VideoReceived += tex =>
                            {
                                Logger.Log(LogLevel.Verbose, Tag, "video test" + tex.format);
                                // VideoReceived is called every time the video resolution changes
                                Logger.Log(LogLevel.Verbose, Tag, 
                                    $"Sid: {participant.Sid}, Remotes: {string.Join('|', Remotes.Keys.ToArray())}");
                                Remotes[participant.Sid].VideoTexture = tex;

                                VideoReceived?.Invoke(participant.Sid);
                            };
                            break;
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
                Logger.Log(LogLevel.Verbose, Tag, $"Track Published! Participant: {participant.Identity}");
            };
            _room.ActiveSpeakersChanged += speakers =>
            {
                HashSet<string> sids = speakers.Select(speaker => speaker.Sid).ToHashSet();
                UpdateActiveSpeakers(sids);
            };
            _room.LocalTrackPublished += (_, _) => { Logger.Log(LogLevel.Verbose, Tag, "Local Track Published!"); };
            _room.Disconnected += _ => Connected = false;

            Logger.Log(LogLevel.Verbose, Tag, "WebGLVoicechatClient initialized!");
        }


        internal override void Connect(string address, string token)
        {
            StartCoroutine(Co_Connect(address, token));
        }

        internal override void Disconnect()
        {
            _room.Disconnect();
        }

        internal override void SetActiveMicrophone(string micName)
        {
        }
        
        public override string GetRoomName()
        {
            return _room.Name;
        }

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
                Logger.Log(LogLevel.Verbose, Tag, "Successfully connected to LiveKit room!");
                Connected = true;

                LocalParticipant = new Participant(_room.LocalParticipant.Sid, _room.LocalParticipant.Identity);
                foreach (RemoteParticipant remote in _room.RemoteParticipants.Values)
                {
                    Remotes.Add(remote.Sid, new Participant(remote.Sid, remote.Identity));
                }

                Connected = true;
                ConnectedToRoom?.Invoke();
            }
        }

        internal override void SetClientMute(string sid, bool mute)
        {
            if (_room.RemoteParticipants.TryGetValue(sid, out RemoteParticipant remote))
            {
                remote.SetVolume(mute ? 0 : 1);
            }
        }

        internal override void SetMicrophoneEnabled(bool b)
        {
            IsMicEnabled = b;
            Logger.Log(LogLevel.Verbose, Tag, $"Setting microphone state: {(b ? "enabled" : "disabled")}");
            _room.LocalParticipant.SetMicrophoneEnabled(b);
        }
    }
}
#endif
