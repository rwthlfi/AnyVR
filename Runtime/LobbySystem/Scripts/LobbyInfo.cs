using System;
using AnyVR.LobbySystem.Internal;
using JetBrains.Annotations;

namespace AnyVR.LobbySystem
{
    public interface ILobbyInfo
    {
        /// <summary>
        ///     Unique identifier
        /// </summary>
        Guid LobbyId { get; }

        IReadOnlyObservedVar<string> Name { get; }

        IReadOnlyObservedVar<bool> IsPasswordProtected { get; }

        IReadOnlyObservedVar<ushort> NumPlayers { get; }

        IReadOnlyObservedVar<DateTime?> ExpirationDate { get; }

        /// <summary>
        ///     The creator of the lobby.
        ///     Can be null if the creator disconnected from the server.
        /// </summary>
        [CanBeNull] GlobalPlayerState Creator { get; } // TODO add readonly interface?

        int CreatorId { get; }

        ushort LobbyCapacity { get; }

        LobbySceneMetaData Scene { get; } // TODO add readonly interface?

        public uint QuickConnectCode { get; }
    }
}
