using System;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem
{
    public class LobbyState : BaseGameState<LobbyPlayerState>
    {
#region Replicated Properties

        private readonly SyncVar<Guid> _lobbyId = new();

#endregion

        [Server]
        internal void Init(GlobalLobbyState global)
        {
            _lobbyId.Value = global.LobbyId;

            OnPlayerJoin += _ =>
            {
                ((GlobalLobbyState)LobbyInfo).SetPlayerNum((ushort)GetPlayerStates().Count());
            };
            OnPlayerLeave += _ =>
            {
                ((GlobalLobbyState)LobbyInfo).SetPlayerNum((ushort)GetPlayerStates().Count());
            };
        }

#region Public API

        public Guid LobbyId => _lobbyId.Value;

        public ILobbyInfo LobbyInfo => LobbyManager.Instance.TryGetLobby(LobbyId, out ILobbyInfo lobby) ? lobby : null;

#endregion

#region Singleton

        private static LobbyState _instance;

        private void Awake()
        {
            _instance = this;
        }

        [Client]
        public static LobbyState GetInstance()
        {
            return _instance;
        }

#endregion
    }
}
