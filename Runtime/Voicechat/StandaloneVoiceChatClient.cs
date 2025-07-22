#if !UNITY_WEBGL && !UNITY_EDITOR
using LiveKit;
using LiveKit.Proto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoomOptions = LiveKit.RoomOptions;
using Logger = AnyVR.Logging.Logger;
using LogLevel = AnyVR.Logging.LogLevel;

namespace AnyVR.Voicechat
{
    internal class StandaloneVoiceChatClient : VoiceChatClient
    {
        private const string Tag = nameof(StandaloneVoiceChatClient);
        private string _activeMicName;
        private readonly Dictionary<string, GameObject> _audioObjects = new();

        private RtcAudioSource _audioSource;
        private LocalAudioTrack _audioTrack;

        private Room _room;
        public override string GetRoomName()
        {
            return _room.Name;
        }
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
                Logger.Log(LogLevel.Verbose, Tag, $"Participant Connected! Name: {participant.Name}");
                Remotes.Add(participant.Sid, new Participant(participant.Sid, participant.Identity));
                ParticipantConnected?.Invoke(Remotes[participant.Sid]);
            };
            _room.ParticipantDisconnected += participant =>
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Participant Disconnected! Name: {participant.Identity}");
                ParticipantDisconnected?.Invoke(participant.Sid);
            };
            _room.TrackSubscribed += (track, publication, participant) =>
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Track Subscribed! Participant: {participant.Identity}, Kind: {track.Kind}");
                TrackSubscribed(track, publication, participant);
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
            _room.Connected += room =>
            {
                Connected = true;
                OnConnected();
            };
            _room.Disconnected += room =>
            {
                Connected = false;
            };

            Logger.Log(LogLevel.Verbose, Tag, "StandaloneVoiceChatClient initialized!");
        }
        private void OnConnected()
        {
            string msg = Microphone.devices.Aggregate("Available Microphones:\n",
                (current, micName) => current + "\t" + micName + "\n");
            Logger.Log(LogLevel.Verbose, Tag, msg);
            const int defaultMic = 7;
            SetActiveMicrophone(Microphone.devices[defaultMic]);
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
            if (micName == _activeMicName)
            {
                return;
            }

            if (Microphone.devices.All(device => device != micName))
            {
                Logger.Log(LogLevel.Error, Tag, $"Microphone '{micName}' is not available.");
                return;
            }

            _activeMicName = micName;

            if (!IsConnected)
            {
                return;
            }
            // Update the audio source if there is a published audio track
            if (!_audioObjects.TryGetValue(_room.LocalParticipant.Sid, out GameObject audioObject))
            {
                Logger.Log(LogLevel.Error, Tag, "Unable to find audio object for the local participant.");
                return;
            }

            if (_audioSource != null)
            {
                _audioSource = new MicrophoneSource(_activeMicName, audioObject);
            }
        }

        internal override void SetMicrophoneEnabled(bool b)
        {
            Logger.Log(LogLevel.Debug, Tag, $"SetMicrophoneEnabled: {b}");
            IsMicEnabled = b;
            if (IsMicEnabled)
            {
                StartCoroutine(PublishAudioTrack());
            }
            else
            {
                UnpublishAudioTrack();
            }
        }

        private IEnumerator Co_Connect(string address, string token)
        {
            Logger.Log(LogLevel.Verbose, Tag, "Connecting to LiveKit room ...");
            ConnectInstruction op = _room.Connect(address, token, new RoomOptions());

            yield return op;

            if (op.IsError)
            {
                Logger.Log(LogLevel.Error, Tag, $"Failed to connect to LiveKit room. address = {address}, token = {token}");
            }
            else
            {
                Logger.Log(LogLevel.Verbose, Tag, "Successfully connected to LiveKit room!");
                LocalParticipant = new Participant(_room.LocalParticipant.Sid, _room.LocalParticipant.Identity);
                _audioObjects.Add(LocalParticipant.Sid, new GameObject(LocalParticipant.Sid));
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

        private void UnpublishAudioTrack()
        {
            _room.LocalParticipant.UnpublishTrack(_audioTrack, true);
            _audioSource.Dispose(); //TODO .Stop()?
            Logger.Log(LogLevel.Verbose, Tag, "Local audio track unpublished.");
        }

        private IEnumerator PublishAudioTrack()
        {
            if (!_audioObjects.TryGetValue(LocalParticipant.Sid, out GameObject audioObject))
            {
                Logger.Log(LogLevel.Error, Tag, "Unable to find audio object for the local participant.");
                yield break;
            }
            _audioSource = new MicrophoneSource(_activeMicName, audioObject);
            LocalAudioTrack track = LocalAudioTrack.CreateAudioTrack("my-track", _audioSource, _room);

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
                Logger.Log(LogLevel.Error, Tag, "Failed to publish audio track.");
            }
            else
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Published audio track! Active microphone: {_activeMicName}");
                _audioSource.Start();
            }
        }

        private void TrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            switch (track)
            {
                case RemoteVideoTrack videoTrack:
                    Logger.Log(LogLevel.Warning, Tag, "Video tracks currently not supported."); //TODO
                    break;
                case RemoteAudioTrack audioTrack:
                    {
                        if (!_audioObjects.TryGetValue(participant.Sid, out GameObject audioObject))
                        {
                            audioObject = new GameObject(audioTrack.Sid);
                            audioObject.AddComponent<AudioSource>();
                            _audioObjects.Add(audioTrack.Sid, audioObject);
                        }

                        AudioStream _ = new(audioTrack, audioObject.GetComponent<AudioSource>());
                        // Audio is being played on the source ...
                        break;
                    }
            }
        }
    }
}
#endif
