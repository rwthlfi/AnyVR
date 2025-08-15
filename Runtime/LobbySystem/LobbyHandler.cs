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
    public class LobbyHandler : NetworkBehaviour
    {
        public delegate void ClientEvent(PlayerState clientId);

        private const string Tag = nameof(LobbyHandler);

        private readonly SyncVar<Guid> _lobbyId = new();
        
        private readonly SyncVar<uint> _quickConnectCode = new();
        
        public uint QuickConnectCode => _quickConnectCode.Value;

        /// <summary>
        ///     A dictionary that contains all player information of the players in this lobby.
        ///     The keys are integers and correspond to the fishnet client ID of that player.
        /// </summary>
        private readonly SyncDictionary<int, PlayerState> _players = new();

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

        public ushort CurrentClientCount => (ushort)_players.Count;

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

        private void OnDestroy()
        {
            _players.OnChange -= OnPlayersChange;
        }

        /// <summary>
        ///     Invoked when a remote client joined the lobby of the local client.
        /// </summary>
        public event ClientEvent OnPlayerJoined;

        /// <summary>
        ///     Invoked when a remote client left the lobby of thk local client.
        /// </summary>
        public event ClientEvent OnPlayerLeft;

        // Only invoked on client after ClientStart
        public static event Action PostInit; // TODO delete this

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
            if (ClientManager.Clients.TryGetValue(player.ID, out NetworkConnection client))
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

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _players.OnChange += OnPlayersChange;
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

            OnPlayerJoined += Client_OnPlayerJoined;
            // OnPlayerLeft += Client_OnPlayerLeft;

            RegisterPlayer(LocalConnection);
            Logger.Log(LogLevel.Debug, Tag, "local player:" + ConnectionManager.UserName);

            PostInit?.Invoke();
        }
        private static void OnConnectedToLiveKitRoom()
        {
            Logger.Log(LogLevel.Verbose, Tag, "Connected to LiveKit room");
        }

        private void Client_OnPlayerJoined(PlayerState playerState)
        {
            if (playerState.ID != LocalConnection.ClientId)
            {
                return;
            }
            Logger.Log(LogLevel.Verbose, Tag, $"Local player registered in lobby '{_lobbyId.Value}'");
            Logger.Log(LogLevel.Verbose, Tag, "Connecting to LiveKit room ...");
            _voiceChatManager.TryConnectToRoom(_lobbyId.Value, _players[LocalConnection.ClientId].PlayerName, ConnectionManager.GetInstance()!.UseSecureProtocol);
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

        private void OnPlayersChange(SyncDictionaryOperation op, int playerId, PlayerState value, bool asServer)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    Debug.Log($"Player Added: Key: {playerId}, ID: {value.ID}, Name: {value.PlayerName}");
                    OnPlayerJoined?.Invoke(value);
                    Logger.Log(LogLevel.Verbose, Tag, $"Client {playerId} joined lobby {_lobbyId.Value}");
                    break;
                case SyncDictionaryOperation.Clear:
                    break;
                case SyncDictionaryOperation.Remove:
                    Logger.Log(LogLevel.Verbose, Tag, $"Client {playerId} left lobby {_lobbyId.Value}");
                    OnPlayerLeft?.Invoke(value);
                    break;
                case SyncDictionaryOperation.Set:
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        /// <summary>
        ///     Registers a player in the <see cref="_players" /> dict.
        /// </summary>
        /// <param name="conn"></param>
        [ServerRpc(RequireOwnership = false)]
        private void RegisterPlayer(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            Server_AddPlayer(conn.ClientId);
        }

        [Server]
        private void Server_AddPlayer(int clientId)
        {
            PlayerState playerState = LobbyManager.GetInstance()?.GetPlayerInfo(clientId);
            if (playerState == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Failed to get player info for client " + clientId);
                return;
            }
            Debug.Log($"Server_AddPlayer: {playerState.PlayerName}");
            _players.Add(playerState.ID, playerState);
        }

        [ServerRpc(RequireOwnership = false)]
        internal void RemoveClient(int clientId, NetworkConnection conn = null)
        {
            // TODO Players can only leave by themselves. Add Kick functionality
            if (conn == null || clientId != conn.ClientId)
            {
                return;
            }

            Server_RemoveClient(clientId);
        }

        [Server]
        internal void Server_RemoveClient(int clientId)
        {
            _players.Remove(clientId);
        }

        public ICollection<PlayerState> GetPlayers()
        {
            return _players.Values;
        }
        
        [CanBeNull]
        public PlayerState GetPlayer(int clientId)
        {
            return _players.GetValueOrDefault(clientId);
        }

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
