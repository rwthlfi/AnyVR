using System;
using System.Threading.Tasks;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerController
    {
        public LiveKitClient Voice { get; private set; }

        public override void OnStartClient()
        {
            base.OnStartClient();

            Voice = LiveKitClient.Instantiate(gameObject);
            Voice.OnParticipantConnected += OnParticipantConnected;
            Voice.OnParticipantDisconnected += OnParticipantDisconnected;
        }

        /// <summary>
        ///     Connects the local player to the LiveKit voice room of the player's lobby.
        ///     Requests a LiveKit token from the token server before connecting to the LiveKit server.
        /// </summary>
        /// <returns>
        ///     A <see cref="VoiceConnectionResult" /> indicating the result of the connection attempt.
        /// </returns>
        [Client]
        public async Task<VoiceConnectionResult> ConnectToLiveKitRoom()
        {
            if (Voice == null)
            {
                return VoiceConnectionResult.PlatformNotSupported;
            }

            if (Voice.IsConnected)
            {
                return VoiceConnectionResult.AlreadyConnected;
            }

            string liveKitUrl = this.GetGameState<LobbyState>().LiveKitServerUrl;

            if (liveKitUrl == null)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyPlayerController), "This lobby is not set up for a LiveKit room. The LiveKit server url is not set.");
                return VoiceConnectionResult.LiveKitServerNotSet;
            }

            TokenResult tokenResult = await RequestLiveKitToken();

            if (tokenResult.State != TokenState.Success)
            {
                Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), $"LiveKit token retrieval failed. {tokenResult.State.ToFriendlyString()}");
                return VoiceConnectionResult.TokenRetrievalFailed;
            }

            string token = tokenResult.Token;
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Received LiveKit Token: '{token}'");

            if (string.IsNullOrWhiteSpace(token))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyPlayerController), "Received LiveKit Token is null or white space.");
                return VoiceConnectionResult.TokenRetrievalFailed;
            }

            Voice.SetAudioObjectMapping(GetRemoteParticipantAudioSource);


            LiveKitConnectionResult result = await Voice.Connect(this.GetGameState<LobbyState>().LiveKitServerUrl, token);

            if (result == LiveKitConnectionResult.Connected)
            {
                foreach (Participant participant in Voice.Participants)
                {
                    InitParticipant(participant);
                }
            }

            // Map LiveKitConnectionResult from the voicechat assembly to public VoiceConnectionResult
            VoiceConnectionResult res = result switch
            {
                LiveKitConnectionResult.Connected => VoiceConnectionResult.Connected,
                LiveKitConnectionResult.Timeout => VoiceConnectionResult.Timeout,
                LiveKitConnectionResult.Cancel => VoiceConnectionResult.Cancel,
                LiveKitConnectionResult.Error => VoiceConnectionResult.Error,
                _ => throw new ArgumentOutOfRangeException()
            };

            Logger.Log(LogLevel.Info, nameof(LobbyPlayerController), res.ToFriendlyString());
            return res;
        }

        [Client]
        private Task<TokenResult> RequestLiveKitToken()
        {
            Task<TokenResult> task = _tokenAwaiter.WaitForResult();
            ServerRPC_RequestLiveKitToken();
            return task;
        }

        /// <summary>
        ///     Disconnect from the LiveKit room.
        /// </summary>
        public void DisconnectFromLiveKitRoom()
        {
            Voice.Disconnect();
        }

        private void OnParticipantConnected(RemoteParticipant remote)
        {
            InitParticipant(remote);
        }

        private void OnParticipantDisconnected(string identity)
        {
            if (!int.TryParse(identity, out int id))
                return;

            LobbyPlayerState player = this.GetGameState().GetPlayerState<LobbyPlayerState>(id);
            player.SetIsConnectedToVoice(false);
        }

        private void InitParticipant(Participant participant)
        {
            if (!int.TryParse(participant.Identity, out int id))
                return;

            LobbyPlayerState player = this.GetGameState().GetPlayerState<LobbyPlayerState>(id);

            participant.IsSpeakingUpdate += isSpeaking =>
            {
                if (player != null)
                {
                    player.SetIsSpeaking(isSpeaking);
                }
            };
            participant.IsMicMutedUpdate += isMuted =>
            {
                if (player != null)
                {
                    player.SetIsMicrophoneMuted(isMuted);
                }
            };
            participant.IsMicPublishedUpdate += isPublished =>
            {
                if (player != null)
                {
                    player.SetIsMicrophonePublished(isPublished);
                }
            };

            player.SetIsConnectedToVoice(true);
        }

        /// <summary>
        ///     Promote another player in the lobby.
        ///     Only admins have the authority to promote other players.
        /// </summary>
        /// <param name="other">The player which should be promoted.</param>
        /// <returns>An asynchronous <see cref="PlayerPromotionResult" /> indicating if the operation was succeeded. </returns>
        [Client]
        public Task<PlayerPromotionResult> PromoteToAdmin(LobbyPlayerState other)
        {
            Task<PlayerPromotionResult> task = _playerPromoteUpdateAwaiter.WaitForResult();
            ServerRPC_PromoteToAdmin(other);
            return task;
        }

        /// <summary>
        ///     Kick another player from the lobby.
        ///     Only admins have the authority to kick other players.
        /// </summary>
        /// <param name="other">The player which should be kicked.</param>
        /// <returns>An asynchronous <see cref="PlayerKickResult" /> indicating if the operation was succeeded. </returns>
        [Client]
        public Task<PlayerKickResult> Kick(LobbyPlayerState other)
        {
            Task<PlayerKickResult> task = _playerKickUpdateAwaiter.WaitForResult();
            ServerRPC_KickPlayer(other);
            return task;
        }

        /// <summary>
        ///     Leave the current lobby.
        /// </summary>
        [Client]
        public void LeaveLobby()
        {
            ServerRPC_LeaveLobby();
        }

        /// <summary>
        ///     Is called when a remote player publishes an audio track.
        ///     Override this to specify the AudioSource the player's audio stream should be attached to.
        ///     The default implementation instantiates a non-spatial audio source on the corresponding player state.
        ///     The returned AudioSource component will be destroyed after the corresponding player unpublishes their audio track.
        /// </summary>
        /// <param name="player">The remote player who published the audio track.</param>
        [Client]
        protected virtual AudioSource ProvideRemoteAudioSource(LobbyPlayerState player)
        {
            return player.gameObject.AddComponent<AudioSource>();
        }

        [Client]
        internal AudioSource GetRemoteParticipantAudioSource(string liveKitIdentity)
        {
            if (int.TryParse(liveKitIdentity, out int clientId))
            {
                LobbyPlayerState playerState = this.GetGameState().GetPlayerState<LobbyPlayerState>(clientId);

                if (playerState != null)
                {
                    //TODO: Check that the corresponding player wants to publish their microphone.
                    //Currently, a malicious player could impersonate another player's voice.

                    return ProvideRemoteAudioSource(playerState);
                }
            }

            // This happens when someone joins the LiveKit room who is not participating in the lobby.
            // TODO: Allow third-party voice participants?
            Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), "Could not match LiveKit participant to player state.");
            return null;
        }

#region RPCs

        [TargetRpc]
        private void TargetRPC_OnPromotionResult(NetworkConnection _, PlayerPromotionResult playerNameUpdateResult)
        {
            Logger.Log(LogLevel.Info, nameof(LobbyPlayerController), $"Promotion result: {playerNameUpdateResult}");
            _playerPromoteUpdateAwaiter?.Complete(playerNameUpdateResult);
        }

        [TargetRpc]
        private void TargetRPC_OnKickResult(NetworkConnection _, PlayerKickResult playerKickResult)
        {
            Logger.Log(LogLevel.Info, nameof(LobbyPlayerController), $"Kick result: {playerKickResult}");
            _playerKickUpdateAwaiter?.Complete(playerKickResult);
        }

        [TargetRpc]
        private void TargetRPC_OnTokenResult(NetworkConnection _, TokenState tokenState, string token = null)
        {
            _tokenAwaiter?.Complete(new TokenResult(tokenState, token));
        }

#endregion
    }
}
