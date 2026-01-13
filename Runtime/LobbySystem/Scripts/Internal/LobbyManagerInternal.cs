using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem.Internal
{
    [RequireComponent(typeof(LobbyRegistry))]
    internal partial class LobbyManagerInternal : NetworkBehaviour
    {
#region Serialized Fields

        [SerializeField] internal GlobalLobbyState _globalLobbyStatePrefab;

#endregion

#region Lifecycle

        public void Start()
        {
            _lobbyRegistry = GetComponent<LobbyRegistry>();
            _sceneService = new LobbySceneService(this);
        }

#endregion

#region Singleton

        public static LobbyManagerInternal Instance { get; private set; }

        private void Awake()
        {
            Assert.IsTrue(Instance == null);
            Instance = this;
        }

#endregion

#region Private Fields

        private LobbyRegistry _lobbyRegistry;

        private LobbySceneService _sceneService;

#endregion
    }
}
