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
        public delegate void ClientEvent(int clientId);

        private const string Tag = nameof(LobbyHandler);

        private readonly SyncVar<Guid> _lobbyId = new();

        /// <summary>
        ///     A dictionary that contains all player information of the players in this lobby.
        ///     The keys are integers and correspond to the fishnet client ID of that player.
        /// </summary>
        private readonly SyncDictionary<int, PlayerInfo> _players = new();

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
        internal void Init(Guid lobbyId)
        {
            _lobbyId.Value = lobbyId;
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

        private void Client_OnPlayerJoined(int clientId)
        {
            if (clientId != LocalConnection.ClientId)
            {
                return;
            }
            Logger.Log(LogLevel.Verbose, Tag, $"Local player registered in lobby '{_lobbyId.Value}'");
            Logger.Log(LogLevel.Verbose, Tag, "Connecting to LiveKit room ...");
            _voiceChatManager.TryConnectToRoom(_lobbyId.Value, _players[LocalConnection.ClientId].PlayerName, ConnectionManager.GetInstance()!.UseSecureProtocol);
        }

        private void OnPlayersChange(SyncDictionaryOperation op, int playerId, PlayerInfo value, bool asServer)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    OnPlayerJoined?.Invoke(playerId);
                    Logger.Log(LogLevel.Verbose, Tag, $"Client {playerId} joined lobby {_lobbyId.Value}");
                    break;
                case SyncDictionaryOperation.Clear:
                    break;
                case SyncDictionaryOperation.Remove:
                    Logger.Log(LogLevel.Verbose, Tag, $"Client {playerId} left lobby {_lobbyId.Value}");
                    OnPlayerLeft?.Invoke(playerId);
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
            PlayerInfo playerInfo = LobbyManager.GetInstance()?.GetPlayerInfo(clientId);
            if (playerInfo == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Failed to get player info for client " + clientId);
                return;
            }
            _players.Add(playerInfo.ID, playerInfo);
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

        public Dictionary<int, PlayerInfo> GetPlayers()
        {
            return _players.Collection;
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
