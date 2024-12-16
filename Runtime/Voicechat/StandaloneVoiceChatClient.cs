#if !UNITY_WEBGL
using LiveKit;
using LiveKit.Proto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoomOptions = LiveKit.RoomOptions;

namespace Voicechat
{
    internal class StandaloneVoiceChatClient : VoiceChatClient
    {
        private string _activeMicName;

        private AudioSource _output, _input;
        private Room _room;

        internal override event Action<Participant> ParticipantConnected;
        internal override event Action<string> ParticipantDisconnected;
        internal override event Action ConnectedToRoom;
        internal override event Action<string> VideoReceived;

        internal override void Init()
        {
            _activeMicName = Microphone.devices[0];

            _room = new Room();
            _room.ParticipantConnected += participant =>
            {
                Debug.Log($"Participant Connected! Name: {participant.Name}");
                Remotes.Add(participant.Sid, new Participant(participant.Sid, participant.Identity));
                ParticipantConnected?.Invoke(Remotes[participant.Sid]);
            };
            _room.ParticipantDisconnected += participant =>
            {
                Debug.Log($"Participant Disconnected! Name: {participant.Identity}");
                ParticipantDisconnected?.Invoke(participant.Sid);
            };
            _room.TrackSubscribed += (track, publication, participant) =>
            {
                Debug.Log($"Track Subscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                TrackSubscribed(track, publication, participant);
            };
            _room.TrackPublished += (publication, participant) =>
            {
                Debug.Log($"Track Published! Participant: {participant.Identity}");
            };
            _room.ActiveSpeakersChanged += speakers =>
            {
                HashSet<string> sids = speakers.Select(speaker => speaker.Sid).ToHashSet();
                UpdateActiveSpeakers(sids);
            };
            _room.Connected += room =>
            {
                Connected = true;
            };
            _room.Disconnected += room =>
            {
                Connected = false;
            };
            _input = new GameObject("LiveKitAudioInput").AddComponent<AudioSource>();
            DontDestroyOnLoad(_input.gameObject);
            _output = new GameObject("LiveKitAudioOutput").AddComponent<AudioSource>();
            DontDestroyOnLoad(_output.gameObject);

            Debug.Log("StandaloneVoiceChatClient initialized!");
        }

        internal override void Connect(string address, string token)
        {
            StartCoroutine(Co_Connect(address, token));
        }

        internal override void Disconnect()
        {
            _room.Disconnect();
        }

        internal override bool TryGetAvailableMicrophoneNames(out string[] micNames)
        {
            micNames = Microphone.devices;
            return true;
        }

        internal override void SetActiveMicrophone(string micName)
        {
            bool b = IsMicEnabled;
            if (b)
            {
                SetMicrophoneEnabled(false);
            }

            _activeMicName = micName;
            if (b)
            {
                SetMicrophoneEnabled(true);
            }
        }

        internal override void SetMicrophoneEnabled(bool b)
        {
            StartCoroutine(Co_SetMicrophoneEnabled(b));
        }

        private IEnumerator Co_Connect(string address, string token)
        {
            Debug.Log($"Connecting to LiveKit room. Address: {address}, token: {token}");
            ConnectInstruction op = _room.Connect(address, token, new RoomOptions());

            yield return op;

            if (op.IsError)
            {
                Debug.LogError("Could not connect to LiveKit room!");
            }
            else
            {
                Debug.Log("Successfully connected to LiveKit room!");
                Local = new Participant(_room.LocalParticipant.Sid, _room.LocalParticipant.Identity);
                foreach (RemoteParticipant remote in _room.RemoteParticipants.Values)
                {
                    Remotes.Add(remote.Sid, new Participant(remote.Sid, remote.Identity));
                }

                ConnectedToRoom?.Invoke();
            }
        }

        internal override void SetClientMute(string sid, bool mute)
        {
            if (_room.RemoteParticipants.TryGetValue(sid, out RemoteParticipant remote))
            {
                //remote.SetVolume(mute ? 0 : 1); TODO: set volume currently not supported by the beta livekit sdk
            }
        }

        private IEnumerator Co_SetMicrophoneEnabled(bool b)
        {
            IsMicEnabled = b;

            if (b)
            {
                _input.clip = Microphone.Start(_activeMicName, true, 1, 16000);

                _input.loop = true;
                _input.Play();

                RtcAudioSource rtcSource = new(_input);
                LocalAudioTrack track = LocalAudioTrack.CreateAudioTrack("my-track", rtcSource, _room);

                TrackPublishOptions options = new() { Source = TrackSource.SourceMicrophone };

                PublishTrackInstruction publish = _room.LocalParticipant.PublishTrack(track, options);
                yield return publish;

                if (publish.IsError)
                {
                    Debug.LogError("Error publishing track!");
                }
                else
                {
                    Debug.Log($"Published audio track! Active microphone: {_activeMicName}");
                }
            }
            else
            {
                Microphone.End(_activeMicName);
                _input.Stop();

                Debug.Log($"Microphone ({_activeMicName}) deactivated!");
            }
        }

        private void TrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication,
            RemoteParticipant participant)
        {
            if (track is not RemoteAudioTrack audioTrack)
            {
                return;
            }

            AudioStream stream = new(audioTrack, _output);
            // Audio is being played on the source ..
        }
    }
}
#endif