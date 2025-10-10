using AnyVR.Logging;
using AnyVR.PlatformManagement;
using AnyVR.TextChat;
using AnyVR.Voicechat;
using FishNet.Object;
using JetBrains.Annotations;
using UnityEngine;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    public partial class LobbyHandler
    {
        
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
        }

#endregion
    }
}
