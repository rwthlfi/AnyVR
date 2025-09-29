using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    [RequireComponent(typeof(LobbyManagerInternal))]
    public class LobbyManager : MonoBehaviour
    {
        internal LobbyManagerInternal Internal;

        public static LobbyManager Instance { get; private set; }

        public IEnumerable<ILobbyInfo> Lobbies => Internal.Lobbies.Values;

        /// <summary>
        ///     All available scenes for a lobby
        /// </summary>
        public IReadOnlyCollection<LobbySceneMetaData> LobbyScenes => LobbyConfiguration.LobbyScenes;
        public static LobbyConfiguration LobbyConfiguration { get; set; }

        private void Awake()
        {
            InitSingleton();

            Internal = GetComponent<LobbyManagerInternal>();
            Internal.OnLobbyOpened += lobbyId =>
            {
                LobbyState state = Internal.GetLobbyMeta(lobbyId);
                Assert.IsNotNull(state.Name);
                OnLobbyOpened?.Invoke(state);
            };
            Internal.OnLobbyClosed += OnLobbyClosed;
            Internal.OnClientInitialized += () => OnClientInitialized?.Invoke(this);

            Assert.IsNotNull(Internal);
        }

        public event Action<ILobbyInfo> OnLobbyOpened;

        public event Action<Guid> OnLobbyClosed;

        /// <summary>
        ///     This event is invoked after the (local) LobbyManager is spawned and initialized.
        /// </summary>
        public static event Action<LobbyManager> OnClientInitialized;

        public void CreateLobby(string lobbyName, string password, LobbySceneMetaData scene, ushort maxClients)
        {
            Internal.CreateLobby(lobbyName, password, scene, maxClients);
        }

        public Task<JoinLobbyResult> JoinLobby(Guid lobbyId, string password = null, TimeSpan? timeout = null)
        {
            return Internal.JoinLobby(lobbyId, password, timeout);
        }

        public Task<JoinLobbyResult> QuickConnect(string code, TimeSpan? timeout = null)
        {
            return Internal.QuickConnect(code, timeout);
        }

        public bool TryGetLobby(Guid lobbyId, out ILobbyInfo lobbyInfo)
        {
            bool found = Internal.Lobbies.TryGetValue(lobbyId, out LobbyState lmd);
            lobbyInfo = lmd;
            return found;
        }

        private void InitSingleton()
        {
            if (Instance != null) Destroy(this);
            Instance = this;
        }
    }
}
