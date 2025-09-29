using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AnyVR.LobbySystem.Internal;
using AnyVR.Logging;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem
{
    [RequireComponent(typeof(LobbyManagerInternal))]
    public class LobbyManager : MonoBehaviour
    {
        internal LobbyManagerInternal Internal;

        public static LobbyManager Instance { get; private set; }

        public IEnumerable<ILobbyInfo> GetLobbies() => Internal.GetLobbyStates();
        
        public ILobbyInfo GetLobbyInfo(Guid id) => Internal.GetLobbyState(id);

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
                LobbyState state = Internal.GetLobbyState(lobbyId);
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

        public async Task<CreateLobbyResult> CreateLobby(string lobbyName, string password, LobbySceneMetaData scene, ushort maxClients)
        {
            CreateLobbyResult result = await Internal.CreateLobby(lobbyName, password, scene, maxClients);
            LogCreateLobbyResult(result);
            return result;
        }

        public async Task<JoinLobbyResult> JoinLobby(Guid lobbyId, string password = null, TimeSpan? timeout = null)
        {
            JoinLobbyResult result = await Internal.JoinLobby(lobbyId, password, timeout);
            LogJoinLobbyResult(result);
            return result;
        }

        public Task<JoinLobbyResult> QuickConnect(string code, TimeSpan? timeout = null)
        {
            return Internal.QuickConnect(code, timeout);
        }
        
        [Conditional("ANY_VR_LOG")]
        private static void LogJoinLobbyResult(JoinLobbyResult result)
        {
            string message = result.Status switch
            {
                JoinLobbyStatus.Success => $"Successfully joined lobby {result.LobbyId.GetValueOrDefault()}.",
                JoinLobbyStatus.AlreadyConnected => "Failed to Join Lobby. You are already connected.",
                JoinLobbyStatus.LobbyDoesNotExist => "Failed to Join Lobby. The lobby does not exist.",
                JoinLobbyStatus.LobbyIsFull => "Failed to Join Lobby. The lobby is full.",
                JoinLobbyStatus.PasswordMismatch => "Failed to Join Lobby. Incorrect lobby password.",
                JoinLobbyStatus.AlreadyJoining => "Failed to Join Lobby. Already attempting to join a lobby.",
                JoinLobbyStatus.Timeout => "Failed to Join Lobby. Server did not handle join request (timeout).",
                JoinLobbyStatus.InvalidFormat => "Failed to Join Lobby. Quick connect code has an invalid format.",
                JoinLobbyStatus.OutOfRange => "Failed to Join Lobby. Quick connect code is out of range.",
                _ => throw new ArgumentOutOfRangeException()
            };

            Logger.Log(LogLevel.Verbose, nameof(LobbyManager), message);
        }
        
        [Conditional("ANY_VR_LOG")]
        private static void LogCreateLobbyResult(CreateLobbyResult result)
        {
            string message = result.Status switch
            {
                CreateLobbyStatus.Success => $"Successfully created lobby {result.LobbyId.GetValueOrDefault()}.",
                CreateLobbyStatus.LobbyNameTaken => "Lobby Creation Failed. Lobby name is taken.",
                CreateLobbyStatus.InvalidScene => "Lobby Creation Failed. Invalid scene.",
                CreateLobbyStatus.Timeout => "Lobby Creation Failed. Timeout occured.",
                CreateLobbyStatus.CreationInProgress => "Lobby Creation Failed. Creation is in progress.",
                CreateLobbyStatus.InvalidParameters => "Lobby Creation Failed. Invalid Parameters.",
                _ => throw new ArgumentOutOfRangeException()
            };

            Logger.Log(LogLevel.Verbose, nameof(LobbyManager), message);
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
