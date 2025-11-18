using System;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;
using Object = UnityEngine.Object;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     High-level voicechat API
    /// </summary>
    public class VoiceChatClient
    {
        private readonly LobbyPlayerController _controller;

        private LiveKitClient _liveKitClient;

        internal VoiceChatClient(LobbyPlayerController controller)
        {
            _controller = controller;
        }

        [Client]
        public async Task<VoiceConnectionResult> ConnectToRoom()
        {
            string roomName = _controller.GetGameState<LobbyState>().LobbyInfo.Name.Value;
            string identity = _controller.GetPlayerState<LobbyPlayerState>().ID.ToString();

            Assert.IsNotNull(identity);

            if (_liveKitClient == null)
            {
                _liveKitClient = LiveKitClient.Instantiate(_controller.gameObject);
                if (_liveKitClient == null)
                {
                    return VoiceConnectionResult.PlatformNotSupported;
                }
            }

            if (_liveKitClient.IsConnected)
            {
                return VoiceConnectionResult.AlreadyConnected;
            }

            Uri tokenUri = new UriBuilder(ConnectionManager.Instance.LiveKitTokenServer)
            {
                Scheme = ConnectionManager.Instance.UseSecureProtocol ? "https" : "http", Path = "requestToken", Query = $"room_name={Uri.EscapeDataString(roomName)}&user_name={Uri.EscapeDataString(identity)}"
            }.Uri;

            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Requesting LiveKit Token from '{tokenUri}'");
            TokenResponse response = await WebRequestHandler.GetAsync<TokenResponse>(tokenUri.ToString());

            if (!response.Success)
            {
                Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), "LiveKit token retrieval failed!");
                return VoiceConnectionResult.TokenRetrievalFailed;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Received LiveKit Token: '{response.token}'");

            if (string.IsNullOrWhiteSpace(response.token))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyPlayerController), "Received LiveKit Token is null or white space!");
                return VoiceConnectionResult.TokenRetrievalFailed;
            }

            Assert.IsFalse(string.IsNullOrWhiteSpace(response.token), "Received LiveKit Token is null or white space!");

            _liveKitClient.SetAudioObjectMapping(_controller.GetRemoteParticipantAudioSource);

            Uri voicechatUri = new UriBuilder(ConnectionManager.Instance.LiveKitVoiceServer)
            {
                Scheme = ConnectionManager.Instance.UseSecureProtocol ? "wss" : "ws"
            }.Uri;

            LiveKitConnectionResult success = await _liveKitClient.Connect(voicechatUri.ToString(), response.token);

            // Map LiveKitConnectionResult from the voicechat assembly to public VoiceConnectionResult form lobby assembly
            return success switch
            {
                LiveKitConnectionResult.Connected => VoiceConnectionResult.Connected,
                LiveKitConnectionResult.Timeout => VoiceConnectionResult.Timeout,
                LiveKitConnectionResult.Cancel => VoiceConnectionResult.Cancel,
                LiveKitConnectionResult.Error => VoiceConnectionResult.Error,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void PublishMicrophone(string device)
        {
            _liveKitClient.LocalParticipant.PublishMicrophone(device);
        }

        public void SetPlayerVolume(LobbyPlayerState player, float volume)
        {
            SetPlayerVolume(player.ID, volume);
        }

        public void SetPlayerVolume(int playerID, float volume)
        {
            RemoteParticipant remoteParticipant = _liveKitClient.RemoteParticipants[playerID.ToString()];
            if (remoteParticipant == null)
                return;

            AudioSource source = remoteParticipant.GetAudioSource();
            source.volume = Math.Clamp(volume, 0, 1);
        }

        public void UnpublishMicrophone()
        {
            _liveKitClient.LocalParticipant.UnpublishMicrophone();
        }

        public void DisconnectFromLiveKitRoom()
        {
            // LiveKitClient disconnects automatically on destroy
            Object.Destroy(_liveKitClient);
        }
    }
}
