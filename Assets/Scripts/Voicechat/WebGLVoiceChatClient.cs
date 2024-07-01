#if UNITY_WEBGL
using LiveKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Voicechat.WebGL
{
    internal class WebGLVoiceChatClient : VoiceChatClient
    {
        private Room _room;

        internal override event Action<Participant> ParticipantConnected;
        internal override event Action<string> ParticipantDisconnected;
        internal override event Action ConnectedToRoom;
        internal override event Action<string, byte[]> DataReceived;
        internal override event Action<string> VideoReceived;

        internal override void Init()
        {
            _room = new Room();
            _room.ParticipantConnected += participant =>
            {
                Debug.Log($"Participant Connected! Name: {participant.Identity}");
                Remotes.Add(participant.Sid, new Participant(participant.Sid, participant.Identity));
                ParticipantConnected?.Invoke(Remotes[participant.Sid]);
            };
            _room.ParticipantDisconnected += participant =>
            {
                Debug.Log($"Participant Disconnected! Name: {participant.Identity}");
                ParticipantDisconnected?.Invoke(participant.Sid);
            };
            _room.TrackSubscribed += (track, _, participant) =>
            {
                Debug.Log($"Track Subscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                switch (track.Kind)
                {
                    case TrackKind.Video:
                        {
                            HTMLVideoElement video = track.Attach() as HTMLVideoElement;
                            video!.VideoReceived += tex =>
                            {
                                Debug.Log("video test" + tex.format);
                                // VideoReceived is called every time the video resolution changes
                                Debug.Log(
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
                Debug.Log($"Track Published! Participant: {participant.Identity}");
            };
            _room.ActiveSpeakersChanged += speakers =>
            {
                HashSet<string> sids = speakers.Select(speaker => speaker.Sid).ToHashSet();
                UpdateActiveSpeakers(sids);
            };
            _room.LocalTrackPublished += (_, _) => { Debug.Log("Local Track Published!"); };
            _room.Disconnected += _ => Connected = false;

            _room.DataReceived += (data, participant, _) => { DataReceived?.Invoke(participant.Sid, data); };
            Debug.Log("WebGLVoiceChatClient initialized!");
        }


        internal override void Connect(string address, string token)
        {
            StartCoroutine(Co_Connect(address, token));
        }

        internal override void Disconnect()
        {
            _room.Disconnect();
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
                Debug.Log("Successfully connected to LiveKit room!");
                Connected = true;

                Local = new Participant(_room.LocalParticipant.Sid, _room.LocalParticipant.Identity);
                foreach (RemoteParticipant remote in _room.Participants.Values)
                {
                    Remotes.Add(remote.Sid, new Participant(remote.Sid, remote.Identity));
                }

                Connected = true;
                ConnectedToRoom?.Invoke();
            }
        }

        internal override void SetClientMute(string sid, bool mute)
        {
            if (_room.Participants.TryGetValue(sid, out RemoteParticipant remote))
            {
                remote.SetVolume(mute ? 0 : 1);
            }
        }

        internal override void SetMicrophoneEnabled(bool b)
        {
            IsMicEnabled = b;
            string state = b ? "enabled" : "disabled";
            Debug.Log($"Setting microphone state: {state}");
            _room.LocalParticipant.SetMicrophoneEnabled(b);
        }

        internal override void SetMicEnabled(bool b)
        {
            _room.LocalParticipant.SetCameraEnabled(b);
        }


        internal override void SendData(byte[] buffer)
        {
            _room.LocalParticipant.PublishData(buffer, DataPacketKind.LOSSY, _room.Participants.Values.ToArray());
        }
    }
}
#endif