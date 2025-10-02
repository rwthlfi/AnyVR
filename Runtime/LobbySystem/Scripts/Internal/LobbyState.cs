using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyState : NetworkBehaviour, ILobbyInfo
    {
#region Replicated Properties
        private readonly SyncVar<int> _creatorId = new();

        private readonly ObservedSyncVar<DateTime?> _expirationDate = new();

        private readonly ObservedSyncVar<bool> _isPasswordProtected = new();

        private readonly SyncVar<ushort> _lobbyCapacity = new();
        
        private readonly SyncVar<Guid> _lobbyId = new();

        private readonly ObservedSyncVar<string> _name = new();

        private readonly ObservedSyncVar<ushort> _numPlayers = new();

        private readonly SyncVar<ushort> _sceneId = new();
#endregion
        
#region Public API
        public Guid LobbyId => _lobbyId.Value;
        public IReadOnlyObservedVar<string> Name => _name;
        public IReadOnlyObservedVar<bool> IsPasswordProtected => _isPasswordProtected;
        public IReadOnlyObservedVar<ushort> NumPlayers => _numPlayers;
        public IReadOnlyObservedVar<DateTime?> ExpirationDate => _expirationDate;
        public int CreatorId => _creatorId.Value;
        public GlobalPlayerState Creator => GlobalGameState.Instance.GetPlayerState(CreatorId);
        public ushort LobbyCapacity => _lobbyCapacity.Value;
        public LobbySceneMetaData Scene => LobbyManager.LobbyConfiguration.LobbyScenes[_sceneId.Value];
#endregion

        internal void Init(string lobbyName, int creatorId, ushort sceneId, ushort lobbyCapacity, bool isPasswordProtected)
        {
            _lobbyId.Value = Guid.NewGuid();
            _name.Value = lobbyName;
            _creatorId.Value = creatorId;
            _sceneId.Value = sceneId;
            _lobbyCapacity.Value = lobbyCapacity;
            _isPasswordProtected.Value = isPasswordProtected;
        }

        [Server]
        internal void SetPlayerNum(ushort playerNum)
        {
            _numPlayers.Value = playerNum;
        }

        public override string ToString()
        {
            return
                $"LobbyMetaData (Id={LobbyId}, Name={Name}, Scene={_sceneId.Value}, Creator={CreatorId}, MaxClients={LobbyCapacity}, IsPasswordProtected={IsPasswordProtected})";
        }
    }
}
