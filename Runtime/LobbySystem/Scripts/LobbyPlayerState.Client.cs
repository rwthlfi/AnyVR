using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
        private readonly RpcAwaiter<PlayerKickResult> _playerKickUpdateAwaiter = new(PlayerKickResult.Timeout, PlayerKickResult.Cancelled);

        private readonly RpcAwaiter<PlayerPromotionResult> _playerPromoteUpdateAwaiter = new(PlayerPromotionResult.Timeout, PlayerPromotionResult.Cancelled);

        public bool IsLocalPlayer => IsController;

        public static LobbyPlayerState Local { get; private set; }

        public Participant Voice
        {
            get
            {
                if (IsLocalPlayer)
                {
                    return LobbyHandler.LiveKitClient.LocalParticipant;
                }
                return LobbyHandler.LiveKitClient.RemoteParticipants.GetValueOrDefault(_voiceId.Value);
            }
        }

        private void OnDestroy()
        {
            if (Local == this)
            {
                Local = null;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Assert.IsFalse(_lobbyId.Value == Guid.Empty);
            Assert.IsNotNull(LobbyHandler);

            _ = InitializeVoice();
        }

        [Client]
        private async Task InitializeVoice()
        {
            Voicechat.ConnectionResult res = await LobbyHandler.InitializeVoicechatClient(this);
            switch (res)
            {
                case Voicechat.ConnectionResult.Connected:
                    Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), "Connected to LiveKit server.");
                    break;
                case Voicechat.ConnectionResult.AlreadyConnected:
                    break;
                default:
                    Logger.Log(LogLevel.Error, nameof(LobbyPlayerState), "Error connecting to the LiveKit server.");
                    break;
            }

            if (!LobbyHandler.LiveKitClient.IsConnected)
            {
                return;
            }

            if (IsLocalPlayer)
            {
                Assert.IsNull(Local);
                Local = this;

                Assert.IsNotNull(LobbyHandler.LiveKitClient.LocalParticipant);
                ServerRPC_SetVoiceId(LobbyHandler.LiveKitClient.LocalParticipant.Sid);
                return;
            }

            // Remote player
            GameObject audioObject = gameObject; // TODO: attach to avatar instead for spatial audio

            switch (Voice)
            {
                case null:
                    // If Voice == null wait for initialization.
                    // This happens when a remote player joins the lobby of the local player.
                    // The remote player's voice is initialized when the _voiceId gets replicated.
                    _voiceId.OnChange += (_, _, _) =>
                    {
                        Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), "Attaching audio object");
                        Assert.IsNotNull(Voice as RemoteParticipant);
                        (Voice as RemoteParticipant)!.Attach(audioObject);
                    };
                    break;
                case RemoteParticipant remote:
                    // If Voice != null, attach audioObject immediately.
                    // This happens when the local player joins a non-empty lobby.
                    remote.Attach(audioObject);
                    break;
                default:
                    Assert.IsTrue(false, "Invalid Voice type");
                    break;
            }
        }

        [Client]
        public Task<PlayerPromotionResult> PromoteToAdmin(TimeSpan? timeout = null)
        {
            Task<PlayerPromotionResult> task = _playerPromoteUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_PromoteToAdmin();
            return task;
        }

        [Client]
        public Task<PlayerKickResult> Kick(TimeSpan? timeout = null)
        {
            Task<PlayerKickResult> task = _playerKickUpdateAwaiter.WaitForResult(timeout);
            ServerRPC_KickPlayer();
            return task;
        }

        [TargetRpc]
        private void TargetRPC_OnPromotionResult(NetworkConnection _, PlayerPromotionResult playerNameUpdateResult)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Promotion result: {playerNameUpdateResult}");
            _playerPromoteUpdateAwaiter?.Complete(playerNameUpdateResult);
        }

        [TargetRpc]
        private void TargetRPC_OnKickResult(NetworkConnection _, PlayerKickResult playerKickResult)
        {
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Kick result: {playerKickResult}");
            _playerKickUpdateAwaiter?.Complete(playerKickResult);
        }
    }
}
