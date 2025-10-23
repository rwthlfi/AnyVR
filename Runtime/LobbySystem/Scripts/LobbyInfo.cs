using System;

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
        GlobalPlayerState Creator { get; }

        int CreatorId { get; }

        ushort LobbyCapacity { get; }

        LobbySceneMetaData Scene { get; }

        public uint QuickConnectCode { get; }
    }
}
