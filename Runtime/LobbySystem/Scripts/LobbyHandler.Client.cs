using System.Threading.Tasks;
using AnyVR.Logging;
using AnyVR.Voicechat;
using AnyVR.WebRequests;
using FishNet.Object;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyHandler
    {
        internal LiveKitClient LiveKitClient;

#if !UNITY_SERVER
        private void Awake()
        {
            if (Instance != null)
            {
                Logger.Log(LogLevel.Error, nameof(LobbyHandler), "Instance of LobbyHandler is already initialized");
                return;
            }

            Instance = this;
        }
#endif

        [Client]
        public override void OnStartClient()
        {
            base.OnStartClient();

            LiveKitClient = VoicechatManager.InstantiateClient();
        }

        [Client]
        internal async Task<Voicechat.ConnectionResult> InitializeVoicechatClient(LobbyPlayerState playerState)
        {
            if (LiveKitClient == null)
            {
                LiveKitClient = VoicechatManager.InstantiateClient();
            }
            else if (LiveKitClient.IsConnected)
            {
                return Voicechat.ConnectionResult.AlreadyConnected;
            }

            return await InitializeVoice(playerState.Global.GetName());
        }

        [Client]
        private async Task<Voicechat.ConnectionResult> InitializeVoice(string userName)
        {
            Assert.IsNotNull(userName);

            const string tokenUrl = "{0}://{1}/requestToken?room_name={2}&user_name={3}";
            bool useHttps = ConnectionManager.Instance.UseSecureProtocol;
            string tokenServerAddress = ConnectionManager.Instance.LiveKitTokenServer.ToString();

            string url = string.Format(tokenUrl, useHttps ? "https" : "http", tokenServerAddress, State.Name.Value, userName);

            TokenResponse response = await WebRequestHandler.GetAsync<TokenResponse>(url);

            if (!response.Success)
            {
                Logger.Log(LogLevel.Debug, nameof(LobbyHandler), "LiveKit token retrieval failed!");
                return Voicechat.ConnectionResult.Error;
            }

            string scheme = ConnectionManager.Instance.UseSecureProtocol ? "https" : "http";
            return await LiveKitClient.Connect($"{scheme}://{ConnectionManager.Instance.LiveKitVoiceServer}", response.token);
        }

#region Public API

        /// <summary>
        ///     A reference to the lobby handler of the current lobby.
        ///     Is null if the local player is not a participant in any lobby.
        /// </summary>
        [CanBeNull] public static LobbyHandler Instance { get; private set; }

        private VoicechatManager _voicechatManager;

        [Client]
        public void Leave()
        {
            ServerRPC_LeaveLobby(ClientManager.Connection);
            LiveKitClient.Disconnect();
        }

#endregion
    }
}
