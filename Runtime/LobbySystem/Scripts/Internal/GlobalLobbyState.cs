using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem.Internal
{
    internal class GlobalLobbyState : NetworkBehaviour, ILobbyInfo
    {
        internal void Init(Guid lobbyId, uint quickConnectCode, string lobbyName, int creatorId, ushort sceneId, ushort lobbyCapacity, bool isPasswordProtected, DateTime? expirationDate)
        {
            _lobbyId.Value = lobbyId;
            _quickConnectCode.Value = quickConnectCode;
            _name.Value = lobbyName;
            _creatorId.Value = creatorId;
            _sceneId.Value = sceneId;
            _lobbyCapacity.Value = lobbyCapacity;
            _isPasswordProtected.Value = isPasswordProtected;
            _expirationDate.Value = expirationDate;
        }

        [Server]
        internal void SetPlayerNum(ushort playerNum)
        {
            _numPlayers.Value = playerNum;
        }

#region Replicated Properties

        private readonly SyncVar<int> _creatorId = new();

        private readonly ObservedSyncVar<DateTime?> _expirationDate = new();

        private readonly ObservedSyncVar<bool> _isPasswordProtected = new();

        private readonly SyncVar<ushort> _lobbyCapacity = new();

        private readonly SyncVar<Guid> _lobbyId = new();

        private readonly ObservedSyncVar<string> _name = new();

        private readonly ObservedSyncVar<ushort> _numPlayers = new();

        private readonly SyncVar<ushort> _sceneId = new();

        private readonly SyncVar<uint> _quickConnectCode = new();

#endregion

#region Public API

        public Guid LobbyId => _lobbyId.Value;
        public IReadOnlyReplicatedProperty<string> Name => _name;
        public IReadOnlyReplicatedProperty<bool> IsPasswordProtected => _isPasswordProtected;
        public IReadOnlyReplicatedProperty<ushort> NumPlayers => _numPlayers;
        public IReadOnlyReplicatedProperty<DateTime?> ExpirationDate => _expirationDate;
        public int CreatorId => _creatorId.Value;
        public GlobalPlayerState Creator => GlobalGameState.Instance.GetPlayerState(CreatorId);
        public ushort LobbyCapacity => _lobbyCapacity.Value;
        public LobbySceneMetaData Scene => LobbyManager.LobbyConfiguration.LobbyScenes[_sceneId.Value];
        public uint QuickConnectCode => _quickConnectCode.Value;

#endregion
    }
}
