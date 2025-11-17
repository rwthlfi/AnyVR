using System.Threading.Tasks;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerController
    {
        public VoiceChatClient Voice;

        public override void OnStartClient()
        {
            base.OnStartClient();
            Voice = new VoiceChatClient(this);
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
        ///     Called when a remote player publishes an audio track.
        ///     Override this to specify a specific AudioSource the player's audio stream should be attached to.
        ///     The default implementation instantiates a non-spatial audio source on the corresponding player state.
        ///     The returned AudioSource component will be destroyed after the corresponding player unpublishes their audio track.
        /// </summary>
        /// <param name="player">The remote player who published the audio track.</param>
        [Client]
        protected virtual AudioSource GetRemotePlayerAudioSource(LobbyPlayerState player)
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

                    return GetRemotePlayerAudioSource(playerState);
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
