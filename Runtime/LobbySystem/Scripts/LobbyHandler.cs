using System;
using AnyVR.LobbySystem.Internal;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyHandler : BaseGameState<LobbyPlayerState>
    {
        internal LobbyState State
        {
            get
            {
                Assert.IsNotNull(LobbyManager.Instance);
                Assert.IsFalse(_lobbyId.Value == Guid.Empty);
                LobbyState state = LobbyManager.Instance.Internal.GetLobbyState(_lobbyId.Value);
                Assert.IsNotNull(state);
                return state;
            }
        }

#region Replicated Fields

        private readonly SyncVar<Guid> _lobbyId = new();

        private readonly SyncVar<uint> _quickConnectCode = new();

#endregion

#region Public API

        public uint QuickConnectCode => _quickConnectCode.Value;

        public ILobbyInfo LobbyInfo => State;

        /// <summary>
        ///     Returns null if the creator disconnected.
        /// </summary>
        [CanBeNull]
        public LobbyPlayerState GetLobbyCreator()
        {
            return GetPlayerState(LobbyInfo.CreatorId);
        }

#endregion
    }
}
