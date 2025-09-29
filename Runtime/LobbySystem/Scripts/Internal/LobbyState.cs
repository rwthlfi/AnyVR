using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyState : NetworkBehaviour, ILobbyInfo
    {
        private readonly SyncVar<Guid> _lobbyId = new();
        public Guid LobbyId => _lobbyId.Value;
        
        private readonly ObservedSyncVar<string> _name = new();
        public IReadOnlyObservedVar<string> Name => _name;
        
        private readonly ObservedSyncVar<bool> _isPasswordProtected = new();
        public IReadOnlyObservedVar<bool> IsPasswordProtected => _isPasswordProtected;
        
        private readonly ObservedSyncVar<ushort> _numPlayers = new();
        public IReadOnlyObservedVar<ushort> NumPlayers => _numPlayers;
        
        private readonly ObservedSyncVar<DateTime?> _expirationDate = new();
        public IReadOnlyObservedVar<DateTime?> ExpirationDate => _expirationDate;
        
        private readonly SyncVar<int> _creatorId = new();
        public int CreatorId => _creatorId.Value;
        public PlayerState Creator => GlobalGameState.Instance.GetPlayerState(CreatorId);
        
        private readonly SyncVar<ushort> _lobbyCapacity = new();
        public ushort LobbyCapacity => _lobbyCapacity.Value;

        private readonly SyncVar<ushort> _sceneId = new();
        public LobbySceneMetaData Scene => LobbyManager.LobbyConfiguration.LobbyScenes[_sceneId.Value];

        public void Init(string lobbyName, int creatorId, ushort sceneId, ushort lobbyCapacity, bool isPasswordProtected)
        {
            _lobbyId.Value = Guid.NewGuid();
            _name.Value = lobbyName;
            _creatorId.Value = creatorId;
            _sceneId.Value = sceneId;
            _lobbyCapacity.Value = lobbyCapacity;
            _isPasswordProtected.Value = isPasswordProtected;
        }

        [Server]
        public void SetPlayerNum(ushort playerNum)
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
