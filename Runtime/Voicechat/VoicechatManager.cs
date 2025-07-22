using System;
using System.Collections.Generic;
using AnyVR.Logging;
using AnyVR.WebRequests;
using JetBrains.Annotations;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.Voicechat
{
    public class VoiceChatManager : MonoBehaviour
    {
        private const string Tag = nameof(VoiceChatManager);

        private const string TokenUrl = "{0}://{1}/requestToken?room_name={2}&user_name={3}";

        private VoiceChatClient _client;
        private string _liveKitServerAddr;
        private string _tokenServerAddr;

        private Dictionary<string, Participant> RemoteParticipants => _client.RemoteParticipants;
        private Participant LocalParticipant => _client.LocalParticipant;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
#if UNITY_EDITOR
            Logger.Log(LogLevel.Verbose, Tag, "VoiceChatManager not initialized. Platform: EDITOR");
            return;
#elif UNITY_WEBGL
            _client = gameObject.AddComponent<WebGLVoiceChatClient>();
            Logger.Log(LogLevel.Verbose, Tag,"VoiceChatManager initialized. Platform: WEBGL");
#elif !UNITY_WEBGL && !UNITY_SERVER
            _client = gameObject.AddComponent<StandaloneVoiceChatClient>();
            Logger.Log(LogLevel.Verbose, Tag, "VoiceChatManager initialized. Platform: EDITOR/STANDALONE");
#elif UNITY_SERVER
            Logger.Log(LogLevel.Verbose, Tag, "VoiceChatManager not initialized. Platform: SERVER");
            return;
#else
            Logger.Log(LogLevel.Error, Tag, "VoiceChatManager not initialized. Platform: UNKNOWN");
            return;
#endif

            _client.Init();
            _client.ConnectedToRoom += () =>
            {
                Logger.Log(LogLevel.Debug, Tag, $"Connected to room '{_client.GetRoomName()}'");
                ConnectedToRoom?.Invoke();
                _client.SetMicrophoneEnabled(true);
            };
            _client.ParticipantConnected += p =>
            {
                ParticipantConnected?.Invoke(p);
            };
            _client.ParticipantDisconnected += sid =>
            {
                if (TryGetParticipantBySid(sid, out Participant p))
                {
                    ParticipantDisconnected?.Invoke(p);
                }
            };
            _client.ParticipantIsSpeakingUpdate += (sid, _) =>
            {
                if (TryGetParticipantBySid(sid, out Participant p))
                {
                    ParticipantIsSpeakingUpdate?.Invoke(p);
                }
            };
            _client.VideoReceived += sid =>
            {
                if (TryGetParticipantBySid(sid, out Participant p))
                {
                    VideoReceived?.Invoke(p);
                }
            };
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        /// <summary>
        ///     Will be invoked when the user successfully connects to a room
        /// </summary>
        public event Action ConnectedToRoom;

        /// <summary>
        ///     Will be invoked when a connection attempt to a room was unsuccessful
        /// </summary>
        public event Action RoomConnectionFailed;

        /// <summary>
        ///     Will be invoked when remote participant connected to the room.
        ///     The participant is the joined remote participant
        /// </summary>
        public event Action<Participant> ParticipantConnected;

        /// <summary>
        ///     Will be invoked when remote participant disconnected from the room.
        ///     The participant is the disconnected remote participant
        /// </summary>
        public event Action<Participant> ParticipantDisconnected;

        /// <summary>
        ///     Will be invoked when a participant started/stopped sending an audio stream
        ///     The participant is the participant who stopped/started sending an audio stream
        /// </summary>
        public event Action<Participant> ParticipantIsSpeakingUpdate;


        /// <summary>
        ///     Will be invoked when remote participant started sending a video stream
        ///     The participant is the participant who stopped/started sending an audio stream
        /// </summary>
        public event Action<Participant> VideoReceived;

        [CanBeNull]
        public static VoiceChatManager GetInstance()
        {
            return _instance;
        }

        /// <summary>
        ///     Start a connection to a LiveKit room.
        ///     This will request a LiveKit token from the token server and connect to the corresponding room if possible.
        ///     If the room with the passed name does not exist, it will be created.
        ///     The method will either invoke the callback <see cref="ConnectedToRoom" /> or
        ///     <see cref="RoomConnectionFailed" />
        /// </summary>
        /// <param name="roomId">The unique identifier of the room</param>
        /// <param name="userName">The name of the local user</param>
        /// <param name="useSecureProt"></param>
        public async void TryConnectToRoom(Guid roomId, string userName, bool useSecureProt = true)
        {
            if (_client == null)
            {
                Logger.Log(LogLevel.Warning, Tag, "Client is not initialized!");
                return;
            }

            Logger.Log(LogLevel.Verbose, Tag, "Requesting LiveKit Token ...");
            string url = string.Format(TokenUrl, useSecureProt ? "https" : "http", _tokenServerAddr, roomId, userName);
            TokenResponse response = await WebRequestHandler.GetAsync<TokenResponse>(url);

            if (!response.Success)
            {
                Logger.Log(LogLevel.Error, Tag, $"Token retrieval failed! url = '{url}'");
                // TODO: Handle Exception when token not received
                // OnTokenRetrievalFailed?.Invoke();
                return;
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Token received. Connecting to Room '{roomId}' ...");

            try
            {
                string address = $"{(useSecureProt ? "wss" : "ws")}://{_liveKitServerAddr}";
                _client.Connect(address, response.token);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, Tag, $"Could not connect to LiveKit room. \n{e.Message}");
                RoomConnectionFailed?.Invoke();
            }
        }

        /// <summary>
        ///     Disconnect from the current room
        /// </summary>
        public void Disconnect()
        {
            _client?.Disconnect();
        }

        public void SetMicrophoneEnabled(bool b)
        {
            _client?.SetMicrophoneEnabled(b);
        }

        public void SetParticipantMuteActive(string sid, bool b)
        {
            _client?.SetClientMute(sid, b);
        }

        public void SetTokenServerAddress(string address)
        {
            _tokenServerAddr = address;
        }
        public void SetLiveKitServerAddress(string address)
        {
            _liveKitServerAddr = address;
        }

        private bool TryGetParticipantBySid(string sid, out Participant participant)
        {
            if (sid != LocalParticipant.Sid)
            {
                return RemoteParticipants.TryGetValue(sid, out participant);
            }

            participant = LocalParticipant;
            return true;
        }

        /// <summary>
        ///     Sets the active microphone for the voice chat.
        /// </summary>
        public void SetActiveMicrophone(string micName)
        {
            Logger.Log(LogLevel.Debug, Tag, $"Selected Microphone: {micName}");
            _client.SetActiveMicrophone(micName);
        }

        #region Singleton

        private static VoiceChatManager _instance;

        private void InitSingleton()
        {
            if (_instance != null)
            {
                Destroy(gameObject);
                Destroy(this);
                return;
            }

            _instance = this;
        }

        #endregion
    }
}
