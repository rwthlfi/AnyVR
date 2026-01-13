using System;
using System.Collections.Generic;
using System.Linq;
using AnyVR.LobbySystem.Internal;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Represents the global game state that is replicated to all clients.
    ///     Maintains a synchronized collection of <see cref="GlobalPlayerState" /> instances indexed by client ID.
    ///     Inherit from this class to add additional synchronized properties as needed.
    ///     <seealso cref="LobbyState" />
    /// </summary>
    public class GlobalGameState : GameStateBase
    {
#region Serialized Fields

        [SerializeField]
        private LobbyConfiguration _lobbyConfiguration;

#endregion

#region Private Fields

        private readonly SyncDictionary<Guid, GlobalLobbyState> _globalLobbyStates = new();

#endregion

#region Lifecycle

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _globalLobbyStates.OnChange += GlobalLobbyStatesOnOnChange;
        }

        private void GlobalLobbyStatesOnOnChange(SyncDictionaryOperation op, Guid key, GlobalLobbyState value, bool _)
        {
            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    OnLobbyOpened?.Invoke(value);
                    break;
                case SyncDictionaryOperation.Clear:
                    break;
                case SyncDictionaryOperation.Remove:
                    OnLobbyClosed?.Invoke(key);
                    break;
                case SyncDictionaryOperation.Set:
                    break;
                case SyncDictionaryOperation.Complete:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

#endregion

#region Server Methods

        [Server]
        internal void AddGlobalLobbyState(GlobalLobbyState globalLobbyState)
        {
            Assert.IsFalse(_globalLobbyStates.ContainsKey(globalLobbyState.LobbyId));
            _globalLobbyStates.Add(globalLobbyState.LobbyId, globalLobbyState);
        }

        [Server]
        public void RemoveGlobalLobbyState(Guid globalLobbyLobbyId)
        {
            Assert.IsTrue(_globalLobbyStates.ContainsKey(globalLobbyLobbyId));
            _globalLobbyStates.Remove(globalLobbyLobbyId);
        }

#endregion


#region Public API

        /// <summary>
        ///     Invoked when a remote client opened a new lobby.
        /// </summary>
        public event Action<ILobbyInfo> OnLobbyOpened;

        /// <summary>
        ///     Invoked when a remote client closed a lobby.
        /// </summary>
        public event Action<Guid> OnLobbyClosed;

        public LobbyConfiguration LobbyConfiguration => _lobbyConfiguration;

        public IEnumerable<ILobbyInfo> GetLobbies()
        {
            return _globalLobbyStates.Values;
        }

        public ILobbyInfo GetLobbyInfo(Guid lobbyId)
        {
            return _globalLobbyStates.GetValueOrDefault(lobbyId);
        }

        internal ILobbyInfo GetLobbyInfo(uint quickConnect)
        {
            return _globalLobbyStates.First(l => l.Value.QuickConnectCode == quickConnect).Value;
        }

        /// <summary>
        ///     Returns an enumeration containing all player states with a specific type.
        /// </summary>
        /// <typeparam name="T">A derived type of <see cref="GlobalPlayerState" />.</typeparam>
        public new IEnumerable<T> GetPlayerStates<T>() where T : GlobalPlayerState
        {
            return base.GetPlayerStates<T>();
        }

        /// <summary>
        ///     Returns an enumeration containing all player states.
        /// </summary>
        public new IEnumerable<GlobalPlayerState> GetPlayerStates()
        {
            return GetPlayerStates<GlobalPlayerState>();
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <typeparam name="T">A derived type of <see cref="GlobalPlayerState" /> to cast the player state to.</typeparam>
        public new T GetPlayerState<T>(int clientId) where T : GlobalPlayerState
        {
            return base.GetPlayerState<T>(clientId);
        }

        /// <summary>
        ///     Returns the player state of the specified player.
        /// </summary>
        /// <param name="clientId">The corresponding player's id.</param>
        /// <returns></returns>
        public new GlobalPlayerState GetPlayerState(int clientId)
        {
            return GetPlayerState<GlobalPlayerState>(clientId);
        }

#endregion

#region Singleton

        /// <summary>
        ///     The singleton instance of the <see cref="GlobalGameState" />.
        ///     Is not null if the local client is connected to a server.
        /// </summary>
        public static GlobalGameState Instance { get; private set; }

        protected void Awake()
        {
            InitSingleton();
        }

        private void InitSingleton()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

#endregion
    }
}
