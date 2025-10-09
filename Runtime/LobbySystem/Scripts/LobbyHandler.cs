using System;
using System.Collections;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using AnyVR.TextChat;
using AnyVR.Voicechat;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     The Lobby Handler is automatically spawned as a networked object upon the creation of a lobby scene and serves as a
    ///     container for the lobby's functionalities.
    /// </summary>
    [RequireComponent(typeof(TextChatManager))]
    public class LobbyHandler : BaseGameState<LobbyPlayerState>
    {
        public delegate void ClientEvent(GlobalPlayerState clientId);

        private const string Tag = nameof(LobbyHandler);

        private readonly SyncVar<Guid> _lobbyId = new();

        private readonly SyncVar<uint> _quickConnectCode = new();

        private Coroutine _expirationCoroutine;

        public uint QuickConnectCode => _quickConnectCode.Value;

        public ILobbyInfo LobbyInfo
        {
            get
            {
                Assert.IsNotNull(LobbyManager.Instance);
                Assert.IsFalse(_lobbyId.Value == Guid.Empty);
                ILobbyInfo result = !LobbyManager.Instance.TryGetLobby(_lobbyId.Value, out ILobbyInfo info) ? null : info;
                Assert.IsNotNull(result);
                return result;
            }
        }

        internal LobbyState State => (LobbyState)LobbyInfo;

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
        internal void Init(LobbyState state)
        {
            Logger.Log(LogLevel.Verbose, Tag, $"Initializing LobbyHandler: {state.LobbyId}");
            Assert.IsFalse(state.LobbyId == Guid.Empty);

            _lobbyId.Value = state.LobbyId;
            _quickConnectCode.Value = state.QuickConnectCode;

            DateTime? expiration = state.ExpirationDate.Value;
            if (expiration.HasValue)
            {
                StartCoroutine(ExpireLobby(expiration.Value));
            }
        }

        public override void OnSpawnServer(NetworkConnection conn)
        {
            base.OnSpawnServer(conn);

            OnPlayerJoin += _ =>
            {
                State.SetPlayerNum((ushort)GetPlayerStates().Count());
            };
            OnPlayerLeave += _ =>
            {
                State.SetPlayerNum((ushort)GetPlayerStates().Count());
                StartCoroutine(CloseInactiveLobby());
            };

            AddPlayerState(conn);
        }

        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            RemovePlayerState(conn);
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
            LobbyManager.Instance.Internal.Server_CloseLobby(_lobbyId.Value);
        }

        /// <summary>
        ///     Checks if the lobby remains empty for a duration. Then closes the lobby if it remained empty.
        /// </summary>
        /// <param name="duration">Duration in seconds until expiration</param>
        [Server]
        private IEnumerator CloseInactiveLobby(ushort duration = 10)
        {
            if (GetPlayerStates().Any())
            {
                yield break;
            }

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
            LobbyManager.Instance.Internal.Server_CloseLobby(_lobbyId.Value);
        }

        [Client]
        public void Leave()
        {
            ServerRPC_LeaveLobby(ClientManager.Connection);
        }

        [Server]
        internal void Server_RemovePlayer(NetworkConnection conn)
        {
            SceneUnloadData sud = LobbySceneService.CreateUnloadData(LobbyInfo);
            if (sud == null)
            {
                Logger.Log(LogLevel.Error, Tag, "Can't unload connection scene. SceneHandle is null");
            }
            else
            {
                SceneManager.UnloadConnectionScenes(conn, sud);
            }

            // TargetRPC_OnLobbyLeft(conn);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerRPC_LeaveLobby(NetworkConnection conn = null)
        {
            Server_RemovePlayer(conn);
        }

        [CanBeNull]
        public static LobbyHandler GetInstance()
        {
            return _instance;
        }

        public Guid GetLobbyId()
        {
            Assert.IsFalse(_lobbyId.Value == Guid.Empty);
            return _lobbyId.Value;
        }

        /// <summary>
        ///     Get the creator of the lobby.
        ///     Might return null if the creator disconnected.
        /// </summary>
        /// <returns></returns>
        [CanBeNull]
        public LobbyPlayerState GetLobbyOwner()
        {
            return GetPlayerState(LobbyInfo.CreatorId);
        }

#region ClientOnly

        [CanBeNull] private static LobbyHandler _instance;
        private VoiceChatManager _voiceChatManager;

#endregion
    }
}
