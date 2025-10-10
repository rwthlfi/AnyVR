using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace AnyVR.LobbySystem.Internal
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal partial class LobbyManagerInternal : NetworkBehaviour
    {
#region Serialized Fields

        [SerializeField] internal LobbyState _lobbyStatePrefab;

#endregion

#region Lifecycle Overrides

        public override void OnStartNetwork()
        {
            _lobbyRegistry = GetComponent<LobbyRegistry>();
            _sceneService = new LobbySceneService(this);
        }

#endregion

#region Lobby Accessors

        internal LobbyState GetLobbyState(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyState(lobbyId);
        }

        internal IEnumerable<LobbyState> GetLobbyStates()
        {
            return _lobbyRegistry.GetLobbyStates();
        }

#endregion

#region Private Fields

        private LobbyRegistry _lobbyRegistry;

        private LobbySceneService _sceneService;

#endregion

#region Internal Callbacks

        internal event Action OnClientInitialized;

        internal event Action<Guid> OnLobbyOpened;

        internal event Action<Guid> OnLobbyClosed;

#endregion
    }
}
