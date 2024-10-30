using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
#if UNITY_WEBGL
using UnityEngine.Networking;
#else
using System.Net.Http;
using System.Threading.Tasks;
#endif

namespace Voicechat
{
    public class LiveKitManager : MonoBehaviour
    {
        #region Singleton

        public static LiveKitManager s_instance; // TODO: set singleton accessibility to internal

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

        private Dictionary<string, Participant> RemoteParticipants => _chatClient.RemoteParticipants;
        private Participant LocalParticipant => _chatClient.LocalParticipant;

        private VoiceChatClient _chatClient;
        [Header("LiveKit")] [SerializeField] private string _tokenServerAddr;

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

        private const string k_tokenURL = "http://{0}/requestToken?room_name={1}&user_name={2}";

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
        public void TryConnectToRoom(string roomName, string userName, bool activateMic = false)
        {
            if (_chatClient == null)
            {
                Debug.LogWarning("VoiceChatClient is not initialized!");
                return;
            }
            // TODO: ensure that the passed names will result in a valid url for token server

            RequestTokenAndServerAddressAsync(roomName, userName, (token, serverAddress) =>
            {
                _chatClient.Connect(serverAddress, token);
                _chatClient.ConnectedToRoom += () =>
                {
                    if (activateMic)
                    {
                        _chatClient.SetMicrophoneEnabled(true);
                    }
                };

                // TODO: Handle Exception when token not received
                // OnRoomConnectionFailed?.Invoke();
            });
        }

        /// <summary>
        ///     Disconnect from the current room
        /// </summary>
        public void Disconnect()
        {
            if(_chatClient != null)
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

        private void RequestTokenAndServerAddressAsync(string roomName, string participantName,
            Action<string, string> callback)
        {
            StartCoroutine(GetToken(roomName, participantName, response =>
            {
                // const string decryptionKey = "cKvNaFjpeDXLWxKWQvgmLseWaTrWLeqS";
                TokenResponse jsonResponse = JsonUtility.FromJson<TokenResponse>(response);
                callback?.Invoke(jsonResponse.token, jsonResponse.livekit_server_address);
            }));
        }

#if UNITY_WEBGL
        private IEnumerator GetToken(string roomName, string participantName, Action<string> callback)
        {
            string url = string.Format(k_tokenURL, _tokenServerAddr, roomName, participantName);
            using UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            if (webRequest.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("Error: " + webRequest.error);
            }
            else
            {
                callback.Invoke(webRequest.downloadHandler.text);
            }
        }
#else
        // ReSharper disable Unity.PerformanceAnalysis
        private IEnumerator GetToken(string roomName, string participantName, Action<string> callback)
        {
            string url = string.Format(k_tokenURL, _tokenServerAddr, roomName, participantName);
            Debug.Log($"Getting token from: {url}");
            HttpClient client = new();
            Task<HttpResponseMessage> task = client.GetAsync(url);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            callback.Invoke(task.Result.Content.ReadAsStringAsync().Result);
        }
#endif
        private void OnApplicationQuit()
        {
            Disconnect();
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
    }
}