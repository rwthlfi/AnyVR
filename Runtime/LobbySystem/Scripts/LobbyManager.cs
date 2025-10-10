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
#region Internal Fields

        internal LobbyManagerInternal Internal;

#endregion

#region Lifecycle

        private void Awake()
        {
            // Init Singleton
            if (Instance != null) Destroy(this);
            Instance = this;

            Internal = GetComponent<LobbyManagerInternal>();

            Assert.IsNotNull(Internal);

            Internal.OnLobbyOpened += lobbyId =>
            {
                LobbyState state = Internal.GetLobbyState(lobbyId);
                OnLobbyOpened?.Invoke(state);
            };

            Internal.OnLobbyClosed += lobbyId => OnLobbyClosed?.Invoke(lobbyId);

            Internal.OnClientInitialized += () => OnClientInitialized?.Invoke(this);

            Assert.IsNotNull(Internal);
        }

#endregion

#region Public API

        public static LobbyManager Instance { get; private set; }

        public static LobbyConfiguration LobbyConfiguration { get; set; }

        /// <summary>
        ///     All available scenes for a lobby
        /// </summary>
        public IReadOnlyCollection<LobbySceneMetaData> LobbyScenes => LobbyConfiguration.LobbyScenes;

        /// <summary>
        ///     This event is invoked after the (local) LobbyManager is spawned and initialized.
        /// </summary>
        public static event Action<LobbyManager> OnClientInitialized;

        /// <summary>
        ///     Invoked when a remote client opened a new lobby.
        /// </summary>
        public event Action<ILobbyInfo> OnLobbyOpened;

        /// <summary>
        ///     Invoked when a remote client closed a lobby.
        /// </summary>
        public event Action<Guid> OnLobbyClosed;

        /// <summary>
        ///     Initiates the creation of a new lobby on the server via a remote procedure call.
        ///     Lobby creation will fail if the given name is already in use.
        /// </summary>
        /// <param name="lobbyName">The desired name of the lobby.</param>
        /// <param name="password">The password for the lobby. Pass null or white space for no password.</param>
        /// <param name="sceneMeta">The scene metadata of the lobby.</param>
        /// <param name="maxClients">The maximum number of clients allowed in the lobby.</param>
        /// <returns>An asynchronous task that returns the result of the lobby creation.</returns>
        public async Task<CreateLobbyResult> CreateLobby(string lobbyName, string password, LobbySceneMetaData sceneMeta, ushort maxClients)
        {
            CreateLobbyResult result = await Internal.Client_CreateLobby(lobbyName, password, sceneMeta, maxClients);
            LogCreateLobbyResult(result);
            return result;
        }

        /// <summary>
        ///     Attempts to join an existing lobby on the server.
        ///     <param name="lobby">The lobby to join.</param>
        ///     <param name="password">Pass a password if the target lobby is protected by one.</param>
        ///     <returns>An asynchronous task that returns the result of the join process.</returns>
        /// </summary>
        public Task<JoinLobbyResult> JoinLobby(ILobbyInfo lobby, string password = null)
        {
            if (lobby == null)
            {
                throw new ArgumentNullException(nameof(lobby));
            }

            return JoinLobby(lobby.LobbyId, password);
        }

        /// <summary>
        ///     Attempts to join an existing lobby on the server.
        ///     <param name="lobbyId">The id of the lobby to join.</param>
        ///     <param name="password">Pass a password if the target lobby is protected by one.</param>
        ///     <returns>An asynchronous task that returns the result of the join process.</returns>
        /// </summary>
        public async Task<JoinLobbyResult> JoinLobby(Guid lobbyId, string password = null)
        {
            JoinLobbyResult result = await Internal.JoinLobby(lobbyId, password);
            LogJoinLobbyResult(result);
            return result;
        }

        /// <summary>
        ///     Attempts to join an existing lobby on the server using the lobby's quick connect code.
        ///     <param name="quickConnectCode">The quick connect code of the target lobby.</param>
        ///     <returns>An asynchronous task that returns the result of the join process.</returns>
        /// </summary>
        public Task<JoinLobbyResult> QuickConnect(string quickConnectCode)
        {
            return Internal.QuickConnect(quickConnectCode);
        }

        public bool TryGetLobby(Guid lobbyId, out ILobbyInfo lobbyInfo)
        {
            lobbyInfo = Internal.GetLobbyState(lobbyId);
            return lobbyInfo != null;
        }

        public IEnumerable<ILobbyInfo> GetLobbies()
        {
            return Internal.GetLobbyStates();
        }

#endregion

#region Logs

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

#endregion
    }
}
