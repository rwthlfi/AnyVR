using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVR.LobbySystem
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal partial class LobbyManagerInternal : NetworkBehaviour
    {
#region Serialized Fields

        [FormerlySerializedAs("_lobbyStatePrefab")]
        [SerializeField] internal GlobalLobbyState _globalLobbyStatePrefab;

#endregion

#region Lifecycle Overrides

        public override void OnStartNetwork()
        {
            _lobbyRegistry = GetComponent<LobbyRegistry>();
            _sceneService = new LobbySceneService(this);
        }

#endregion

#region Lobby Accessors

        internal GlobalLobbyState GetLobbyState(Guid lobbyId)
        {
            return _lobbyRegistry.GetLobbyState(lobbyId);
        }

        internal IEnumerable<GlobalLobbyState> GetLobbyStates()
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
