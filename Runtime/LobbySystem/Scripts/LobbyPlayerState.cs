using System;
using AnyVR.LobbySystem.Internal;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState : NetworkBehaviour
    {
#region Serialized Properties

        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [SerializeField] private NetworkObject _playerPrefab;

#endregion

#region Lifecycle Overrides

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Global = GlobalGameState.Instance.GetPlayerState(OwnerId);
            Assert.IsNotNull(Global);
        }

#endregion

#region Replicated Properties

        private readonly SyncVar<bool> _isAdmin = new(); // WritePermission is ServerOnly by default

        private readonly SyncVar<Guid> _lobbyId = new(Guid.Empty);

        private readonly SyncVar<string> _voiceId = new();

        private NetworkObject _playerAvatar;
        // TODO add player avatar class

#endregion

#region Public API

        public ILobbyInfo LobbyInfo
        {
            get
            {
                LobbyState state = LobbyManager.Instance.Internal.GetLobbyState(_lobbyId.Value);
                Assert.IsNotNull(state);
                return state;
            }
        }

        public LobbyHandler LobbyHandler
        {
            get
            {
                if (ServerManager.Started)
                {
                    LobbyHandler handler = LobbyManager.Instance.Internal.GetLobbyHandler(_lobbyId.Value);
                    Assert.IsNotNull(handler);
                    return handler;
                }

                Assert.IsTrue(ClientManager.Started);
                return LobbyHandler.Instance;
            }
        }

        /// <summary>
        ///     The global player state of the player.
        /// </summary>
        public GlobalPlayerState Global { get; private set; }

        public bool GetIsAdmin()
        {
            return _isAdmin.Value;
        }

#endregion
    }
}
