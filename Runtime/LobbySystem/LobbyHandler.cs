using System;
using System.Collections.Generic;
using AnyVR.Logging;
using AnyVR.TextChat;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
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
        
        public uint QuickConnectCode => _quickConnectCode.Value;

        public LobbyMetaData MetaData
        {
            get
            {
                LobbyManager lobbyManager = LobbyManager.GetInstance();
                if (lobbyManager == null)
                {
                    return null;
                }

                return !lobbyManager.TryGetLobby(_lobbyId.Value, out LobbyMetaData lmd) ? null : lmd;
            }
        }

        public TextChatManager TextChat { get; private set; }

        private void Awake()
        {
            TextChat = GetComponent<TextChatManager>();

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
            bool success = LobbyManager.Instance.TryGetQuickConnectCode(lobbyId, out uint code);
            Assert.IsTrue(success);
            _quickConnectCode.Value = quickConnectCode;
        }
        
        [Server]
        private void KickPlayer_Internal(PlayerState player)
        {
            if (ClientManager.Clients.TryGetValue(player.GetID(), out NetworkConnection client))
            {
                // TODO: Move TryRemoveClientFromLobby to LobbyHandler
                LobbyManager.Instance.TryRemoveClientFromLobby(client);
            }
        }

        [ServerRpc]
        private void ServerRPC_KickPlayer(PlayerState player)
        {
            KickPlayer_Internal(player);
        }

        public void KickPlayer(PlayerState player)
        {
            if (IsServerStarted)
            {
                KickPlayer_Internal(player);
            }
            else
            {
                ServerRPC_KickPlayer(player);
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

            _voiceChatManager = VoiceChatManager.GetInstance();
            if (_voiceChatManager != null)
            {
                _voiceChatManager.ConnectedToRoom += OnConnectedToLiveKitRoom;
            }
            Logger.Log(LogLevel.Debug, Tag, "local player:" + ConnectionManager.UserName);
        }
        private static void OnConnectedToLiveKitRoom()
        {
            Logger.Log(LogLevel.Verbose, Tag, "Connected to LiveKit room");
        }

        private void Client_OnPlayerJoined(PlayerState playerState)
        {
            if (playerState.GetID() != LocalConnection.ClientId)
            {
                return;
            }
            Logger.Log(LogLevel.Verbose, Tag, $"Local player registered in lobby '{_lobbyId.Value}'");
            Logger.Log(LogLevel.Verbose, Tag, "Connecting to LiveKit room ...");
            _voiceChatManager.TryConnectToRoom(_lobbyId.Value, GetPlayerState(LocalConnection.ClientId).GetName(), ConnectionManager.GetInstance()!.UseSecureProtocol);
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
                },
            };

            const float x = 10f;
            const float y = 10f;
            const float width = 500f;
            const float height = 20f;
            Rect labelRect = new(x, y, width, height);

            string debugMsg = $"QuickConnectCode: {_quickConnectCode.Value.ToString()}";
            GUI.Label(labelRect, debugMsg, _style);
        }
#endif

        public void SetMuteSelf(bool muteSelf)
        {
            VoiceChatManager.GetInstance()?.SetMicrophoneEnabled(!muteSelf);
        }

        [CanBeNull]
        public static LobbyHandler GetInstance()
        {
            return _instance;
        }

        [Client]
        public void Leave()
        {
            LobbyManager.Instance.LeaveLobby();
        }

        public Guid GetLobbyId()
        {
            return _lobbyId.Value;
        }


#region ClientOnly

        [CanBeNull] private static LobbyHandler _instance;
        private VoiceChatManager _voiceChatManager;

#endregion
    }
}
