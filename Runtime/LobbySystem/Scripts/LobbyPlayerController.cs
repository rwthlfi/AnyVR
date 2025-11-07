using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public class LobbyPlayerController : PlayerController
    {
#region Serialized Properties

        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [FormerlySerializedAs("_playerPrefab")]
        [SerializeField] private NetworkObject _playerAvatarPrefab;

#endregion

        [Server]
        private void SpawnAvatar()
        {
            if (GetPlayerState<LobbyPlayerState>().GetAvatar() != null)
                return;

            if (_playerAvatarPrefab == null)
                return;

            NetworkObject nob = Instantiate(_playerAvatarPrefab);
            GetPlayerState<LobbyPlayerState>().SetAvatar(nob);
            Spawn(nob, Owner, gameObject.scene);
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

        [ServerRpc]
        private void ServerRPC_PromoteToAdmin(LobbyPlayerState other)
        {
            if (other.IsAdmin)
            {
                TargetRPC_OnPromotionResult(Owner, PlayerPromotionResult.AlreadyAdmin);
                return;
            }

            if (!GetPlayerState<LobbyPlayerState>().IsAdmin)
            {
                TargetRPC_OnPromotionResult(Owner, PlayerPromotionResult.InsufficientPermissions);
                return;
            }

            other.SetIsAdmin(true);
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({other.Global.Name}) promoted to admin.");
            TargetRPC_OnPromotionResult(Owner, PlayerPromotionResult.Success);
        }

        [ServerRpc]
        private void ServerRPC_KickPlayer(LobbyPlayerState other)
        {
            if (!GetPlayerState<LobbyPlayerState>().IsAdmin)
            {
                TargetRPC_OnKickResult(Owner, PlayerKickResult.InsufficientPermissions);
                return;
            }

            LobbyManagerInternal.Instance.RemovePlayerFromLobby(other);
            Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerState), $"Player {OwnerId} ({other.Global.Name}) kicked.");
            TargetRPC_OnKickResult(Owner, PlayerKickResult.Success);
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

            const string tokenUrl = "{0}://{1}/requestToken?room_name={2}&user_name={3}";
            bool useHttps = ConnectionManager.Instance.UseSecureProtocol;
            string tokenServerAddress = ConnectionManager.Instance.LiveKitTokenServer.ToString();

            string url = string.Format(tokenUrl, useHttps ? "https" : "http", tokenServerAddress, roomName, userName);

            TokenResponse response = await WebRequestHandler.GetAsync<TokenResponse>(url);

            if (!response.Success)
            {
                Logger.Log(LogLevel.Debug, nameof(LobbyGameMode), "LiveKit token retrieval failed!");
                return Voicechat.ConnectionResult.Error;
            }

            LiveKitClient.SetAudioObjectMapping(identity =>
            {
                return this.GetGameState().GetPlayerStates<LobbyPlayerState>().First(state => state.Global.Name == identity).gameObject;
            });

            return await LiveKitClient.Connect(ConnectionManager.Instance.LiveKitVoiceServer.ToString(), response.token);
        }

#region Private Fields

        protected LiveKitClient LiveKitClient;

        private readonly RpcAwaiter<PlayerKickResult> _playerKickUpdateAwaiter = new(PlayerKickResult.Timeout, PlayerKickResult.Cancelled);

        private readonly RpcAwaiter<PlayerPromotionResult> _playerPromoteUpdateAwaiter = new(PlayerPromotionResult.Timeout, PlayerPromotionResult.Cancelled);

#endregion

#region Livecycle Overrides

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            StartCoroutine(BeginPlay());
        }

        private IEnumerator BeginPlay()
        {
            while (GetPlayerState() == null)
            {
                yield return null;
            }

            if (ServerManager.Started)
            {
                OnServerBeginPlay();
            }
            if (ClientManager.Started)
            {
                OnClientBeginPlay();
            }
        }

        protected virtual void OnServerBeginPlay()
        {
            SpawnAvatar();
        }

        protected virtual void OnClientBeginPlay()
        {
            ConnectToLiveKitRoom(this.GetGameState<LobbyState>().LobbyInfo.Name.Value, GetPlayerState<LobbyPlayerState>().Global.Name).ContinueWith(task =>
            {
                if (task.Result != Voicechat.ConnectionResult.Connected)
                {
                    Logger.Log(LogLevel.Error, nameof(LobbyPlayerController), $"Voicechat connection failed: {task.Result}");
                    return;
                }

                Voicechat.ConnectionResult res = task.Result;
                Logger.Log(LogLevel.Verbose, nameof(LobbyPlayerController), $"Voicechat connection result: {res}");

                LiveKitClient.LocalParticipant.PublishMicrophone("RODE NT-USB Analog Stereo");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

#endregion

#region Singleton

        private static LobbyPlayerController _instance;

        private void Awake()
        {
            _instance = this;
        }

        [Client]
        public static LobbyPlayerController GetInstance()
        {
            return _instance;
        }

#endregion
    }
}
