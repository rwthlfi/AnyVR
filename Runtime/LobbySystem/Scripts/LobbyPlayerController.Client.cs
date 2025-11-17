using System;
using System.Linq;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerController
    {
        public override void OnStartClient()
        {
            base.OnStartClient();

            ConnectToLiveKitRoom(this.GetGameState<LobbyState>().LobbyInfo.Name.Value, GetPlayerState<LobbyPlayerState>().Global.Name).ContinueWith(task =>
            {
                if (task.Result != Voicechat.ConnectionResult.Connected)
                {
                    Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), $"Voicechat connection failed: {task.Exception}");
                    return;
                }

                Voicechat.ConnectionResult res = task.Result;
                Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Voicechat connection result: {res}");

                LiveKitClient.LocalParticipant.PublishMicrophone("RODE NT-USB Analog Stereo");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        [Client]
        public Task<PlayerPromotionResult> PromoteToAdmin(LobbyPlayerState other, TimeSpan? timeout = null)
        {
            Task<PlayerPromotionResult> task = _playerPromoteUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_PromoteToAdmin(other);
            return task;
        }

        [Client]
        public Task<PlayerKickResult> Kick(LobbyPlayerState other, TimeSpan? timeout = null)
        {
            Task<PlayerKickResult> task = _playerKickUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_KickPlayer(other);
            return task;
        }

        [Client]
        private async Task<Voicechat.ConnectionResult> ConnectToLiveKitRoom(string roomName, string userName) // TODO use client id instead of name
        {
            Assert.IsNotNull(userName);

            Assert.IsNull(LiveKitClient);
            LiveKitClient = VoicechatManager.InstantiateClient();
            if (LiveKitClient == null)
            {
                return Voicechat.ConnectionResult.PlatformNotSupported;
            }

            Uri tokenUri = new UriBuilder(ConnectionManager.Instance.LiveKitTokenServer)
            {
                Scheme = ConnectionManager.Instance.UseSecureProtocol ? "https" : "http",
                Path = "requestToken",
                Query = $"room_name={Uri.EscapeDataString(roomName)}&user_name={Uri.EscapeDataString(userName)}"
            }.Uri;

            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Requesting LiveKit Token from '{tokenUri}'");
            TokenResponse response = await WebRequestHandler.GetAsync<TokenResponse>(tokenUri.ToString());

            if (!response.Success)
            {
                Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), "LiveKit token retrieval failed!");
                return Voicechat.ConnectionResult.TokenRetrievalFailed;
            }

            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Received LiveKit Token: '{response.token}'");

            if (string.IsNullOrWhiteSpace(response.token))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyPlayerController), "Received LiveKit Token is null or white space!");
                return Voicechat.ConnectionResult.TokenRetrievalFailed;
            }

            Assert.IsFalse(string.IsNullOrWhiteSpace(response.token), "Received LiveKit Token is null or white space!");

            LiveKitClient.SetAudioObjectMapping(identity =>
            {
                LobbyPlayerState playerState = this.GetGameState().GetPlayerStates<LobbyPlayerState>().FirstOrDefault(state => state.Global.Name == identity);

                if (playerState == null)
                {
                    // This happens, when someone joins the LiveKit room who is not participating in the lobby.
                    // TODO: Allow third-party voice participants?
                    Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), "Could not match LiveKit participant to player state.");
                    return null;
                }
                return playerState.gameObject;
            });
            
            Uri voicechatUri = new UriBuilder(ConnectionManager.Instance.LiveKitVoiceServer)
            {
                Scheme = ConnectionManager.Instance.UseSecureProtocol ? "wss" : "ws"
            }.Uri;

            return await LiveKitClient.Connect(voicechatUri.ToString(), response.token);
        }

        [Client]
        public void LeaveLobby()
        {
            LiveKitClient.Disconnect();
            ServerRPC_LeaveLobby();
        }

#region RPCs

        [TargetRpc]
        private void TargetRPC_OnPromotionResult(NetworkConnection _, PlayerPromotionResult playerNameUpdateResult)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Promotion result: {playerNameUpdateResult}");
            _playerPromoteUpdateAwaiter?.Complete(playerNameUpdateResult);
        }

        [TargetRpc]
        private void TargetRPC_OnKickResult(NetworkConnection _, PlayerKickResult playerKickResult)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Kick result: {playerKickResult}");
            _playerKickUpdateAwaiter?.Complete(playerKickResult);
        }

#endregion
    }
}
