using System;
using System.Collections;
using System.Linq;
using AnyVR.Logging;
using AnyVR.TextChat;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = AnyVR.Logging.Logger;
using USceneManger = UnityEngine.SceneManagement.SceneManager;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     The Lobby Handler is automatically spawned as a networked object upon the creation of a lobby scene and serves as a
    ///     container for the lobby's functionalities.
    /// </summary>
    [RequireComponent(typeof(TextChatManager))]
    public class LobbyHandler : GameState
    {
        public delegate void ClientEvent(PlayerState clientId);

        private const string Tag = nameof(LobbyHandler);

        private readonly SyncVar<Guid> _lobbyId = new();

        private readonly SyncVar<uint> _quickConnectCode = new();

        private Coroutine _expirationCoroutine;

        public uint QuickConnectCode => _quickConnectCode.Value;

        //TODO: This returns null in Start
        public LobbyMetaData MetaData
        {
            get
            {
                LobbyManager lobbyManager = LobbyManager.GetInstance();
                if (lobbyManager == null)
                {
                    return null;
                }

                return !lobbyManager.TryGetLobbyMeta(_lobbyId.Value, out LobbyMetaData lmd) ? null : lmd;
            }
        }

        private void Awake()
        {
#if !UNITY_SERVER
            if (_instance != null)
            {
                Logger.Log(LogLevel.Error, Tag, "Instance of LobbyHandler is already initialized");
                return;
            }

            _instance = this;
#endif
        }

        [Server]
        internal void Init(Guid lobbyId, uint quickConnectCode)
        {
            _lobbyId.Value = lobbyId;
            _quickConnectCode.Value = quickConnectCode;

            if (MetaData.ExpireDate.HasValue)
            {
                StartCoroutine(ExpireLobby(MetaData.ExpireDate.Value));
            }
        }

        public override void OnSpawnServer(NetworkConnection conn)
        {
            base.OnSpawnServer(conn);
            AddPlayerState(conn);
        }
        
        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            RemovePlayerState(conn);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // Reapply environmental settings
            Scene lobbyScene = USceneManger.GetSceneByPath(MetaData.ScenePath);
            USceneManger.SetActiveScene(lobbyScene);
        }

        [Server]
        private IEnumerator ExpireLobby(DateTime expirationDate)
        {
            float timeUntilExpiration = (float)(expirationDate - DateTime.UtcNow).TotalSeconds;

            Logger.Log(LogLevel.Verbose, Tag, $"Expire lobby {_lobbyId.Value} in {timeUntilExpiration} seconds");
            if (timeUntilExpiration > 0)
            {
                yield return new WaitForSeconds(timeUntilExpiration);
            }

            Logger.Log(LogLevel.Verbose, Tag, $"Lobby {_lobbyId.Value} expired");
            LobbyManager.Instance.Server_CloseLobby(_lobbyId.Value);
        }

        /// <summary>
        ///     Checks if the lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="duration">Duration in seconds until expiration</param>
        [Server]
        private IEnumerator CloseInactiveLobby(ushort duration)
        {
            float elapsed = 0;
            const float interval = 1;

            while (elapsed < duration)
            {
                Logger.Log(LogLevel.Verbose, Tag, $"Closing lobby {_lobbyId.Value} in {duration - elapsed} seconds due to inactivity.");
                if (GetPlayerStates().Any())
                {
                    Logger.Log(LogLevel.Verbose, Tag, $"Cancel inactive lobby closing. Lobby {_lobbyId.Value} is no longer inactive.");
                    yield break;
                }

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            Logger.Log(LogLevel.Warning, Tag, $"Closing lobby {_lobbyId.Value} due to inactivity.");
            LobbyManager.Instance.Server_CloseLobby(_lobbyId.Value);
        }

        [CanBeNull]
        public static LobbyHandler GetInstance()
        {
            return _instance;
        }

        [Client]
        public void Leave()
        {
            LobbyManager.Instance.ServerRPC_LeaveLobby();
        }

        public Guid GetLobbyId()
        {
            return _lobbyId.Value;
        }

#if !UNITY_SERVER
        private GUIStyle _style;
        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.yellow
                }
            };

            const float x = 10f;
            const float y = 10f;
            const float width = 500f;
            const float height = 20f;
            Rect labelRect = new(x, y, width, height);

            string debugMsg = $"QuickConnectCode: {_quickConnectCode.Value.ToString()}";
            GUI.Label(labelRect, debugMsg, _style);

            const float buttonWidth = 200f;
            const float buttonHeight = 30f;
            const float padding = 10f;
            Rect buttonRect = new(Screen.width - buttonWidth - padding, Screen.height - buttonHeight - padding, buttonWidth, buttonHeight);

            if (GUI.Button(buttonRect, "Leave", GUI.skin.button))
            {
                Leave();
            }
        }
#endif


#region ClientOnly

        [CanBeNull] private static LobbyHandler _instance;
        private VoiceChatManager _voiceChatManager;

#endregion
    }
}
