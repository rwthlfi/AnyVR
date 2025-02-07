using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AnyVr.Voicechat
{
    public class LiveKitManager : MonoBehaviour
    {
        private const string k_tokenURL = "http://{0}/requestToken?room_name={1}&user_name={2}";

        private VoiceChatClient _chatClient;
        private string _tokenServerAddr;

        private Dictionary<string, Participant> RemoteParticipants => _chatClient.RemoteParticipants;
        private Participant LocalParticipant => _chatClient.LocalParticipant;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Debug.Log("VoiceChatManager initialized. Platform: WEBGL");
            _chatClient = gameObject.AddComponent<WebGLVoiceChatClient>();
#elif !UNITY_WEBGL && !UNITY_SERVER
            Debug.Log("VoiceChatManager initialized. Platform: EDITOR/STANDALONE");
            _chatClient = gameObject.AddComponent<StandaloneVoiceChatClient>();
#else
            Debug.LogWarning("VoiceChatManager not initialized.");
            return;
#endif
            _chatClient.Init();
            _chatClient.ConnectedToRoom += () => { ConnectedToRoom?.Invoke(); };
            _chatClient.ParticipantConnected += p =>
            {
                ParticipantConnected?.Invoke(p);
            };
            _chatClient.ParticipantDisconnected += sid =>
            {
                if (TryGetParticipantBySid(sid, out Participant p))
                {
                    ParticipantDisconnected?.Invoke(p);
                }
            };
            _chatClient.ParticipantIsSpeakingUpdate += (sid, _) =>
            {
                if (TryGetParticipantBySid(sid, out Participant p))
                {
                    ParticipantIsSpeakingUpdate?.Invoke(p);
                }
            };
            _chatClient.VideoReceived += sid =>
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
        public static LiveKitManager GetInstance()
        {
            return s_instance;
        }

        /// <summary>
        ///     Start a connection to a LiveKit room.
        ///     This will request a LiveKit token from the token server and connect to the corresponding room if possible.
        ///     If the room with the passed name does not exist, it will be created.
        ///     The method will either invoke the callback <see cref="ConnectedToRoom" /> or
        ///     <see cref="RoomConnectionFailed" />
        /// </summary>
        /// <param name="roomName">The unique identifier of the room</param>
        /// <param name="userName">The name of the local user</param>
        /// <param name="activateMic">If the microphone track should automatically be published</param>
        public async void TryConnectToRoom(string roomName, string userName, bool activateMic = false)
        {
            if (_chatClient == null)
            {
                Debug.LogWarning("VoiceChatClient is not initialized!");
                return;
            }
            // TODO: ensure that the passed names will result in a valid url for token server

            TokenResponse response = await RequestLiveKitToken(roomName, userName);
            _chatClient.Connect(_tokenServerAddr, response.token);
            _chatClient.ConnectedToRoom += () =>
            {
                if (activateMic)
                {
                    _chatClient.SetMicrophoneEnabled(true);
                }
            };

            // TODO: Handle Exception when token not received
            // OnRoomConnectionFailed?.Invoke();
        }

        /// <summary>
        ///     Disconnect from the current room
        /// </summary>
        public void Disconnect()
        {
            if (_chatClient != null)
            {
                _chatClient.Disconnect();
            }
        }

        public void SetMicrophoneEnabled(bool b)
        {
            _chatClient.SetMicrophoneEnabled(b);
        }

        public void SetParticipantMuteActive(string sid, bool b)
        {
            _chatClient.SetClientMute(sid, b);
        }

        public void SetTokenServerAddress(string ip, ushort port)
        {
            SetTokenServerAddress($"{ip}:{port}");
        }

        private void SetTokenServerAddress(string address)
        {
            if (TryParseAddress(address, out (string ip, ushort port) res))
            {
                _tokenServerAddr = address;
            }
            else
            {
                Debug.LogWarning($"Error parsing token server address: {address}");
            }
        }

        private static bool TryParseAddress(string address, out (string, ushort) res)
        {
            res = (null, 0);
            if (!Regex.IsMatch(address, ".+:[0-9]+"))
            {
                return false;
            }

            string[] arr = address.Split(':'); // [ip, port]

            uint port = uint.Parse(arr[1]);
            if (port > ushort.MaxValue)
            {
                return false;
            }

            res = (arr[0], (ushort)port);
            return true;
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

        private async Task<TokenResponse> RequestLiveKitToken(string roomName, string participantName)
        {
            TokenResponse response = Application.platform == RuntimePlatform.WebGLPlayer
                ? await WEBGL_GetToken(roomName, participantName)
                : await GetToken(roomName, participantName);
            Debug.Log($"Token response: {response}");
            return response;
        }

        private async Task<TokenResponse> WEBGL_GetToken(string roomName, string participantName)
        {
            string url = string.Format(k_tokenURL, _tokenServerAddr, roomName, participantName);
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            await webRequest.SendWebRequest();

            TokenResponse res = new();
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                res = JsonUtility.FromJson<TokenResponse>(webRequest.downloadHandler.text);
            }
            else
            {
                Debug.Log("Error: " + webRequest.error);
            }

            res.isSuccess = webRequest.result == UnityWebRequest.Result.Success;
            return res;
        }

        private async Task<TokenResponse> GetToken(string roomName, string participantName)
        {
            string url = string.Format(k_tokenURL, _tokenServerAddr, roomName, participantName);
            Debug.Log($"Getting token from: {url}");
            HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            TokenResponse res = new();
            if (response.IsSuccessStatusCode)
            {
                res = JsonUtility.FromJson<TokenResponse>(response.Content.ReadAsStringAsync().Result);
            }

            res.isSuccess = response.IsSuccessStatusCode;
            return res;
        }

        /// <summary>
        ///     Sets the active microphone for the voice chat.
        ///     Only works on Standalone Win!
        /// </summary>
        public void SetActiveMicrophone(string micName)
        {
            _chatClient.SetActiveMicrophone(micName);
        }

        public bool TryGetAvailableMicrophoneNames(out string[] micNames)
        {
            if (_chatClient != null && _chatClient.TryGetAvailableMicrophoneNames(out string[] names))
            {
                micNames = names;
                return true;
            }

            micNames = null;
            return false;
        }

        #region Singleton

        private static LiveKitManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Destroy(gameObject);
                Destroy(this);
                return;
            }

            s_instance = this;
        }

        #endregion
    }
}