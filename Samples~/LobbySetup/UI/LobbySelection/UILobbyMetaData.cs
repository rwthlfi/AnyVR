using System;
using AnyVr.LobbySystem;

namespace AnyVr.Samples.LobbySetup
{
    public sealed class UILobbyMetaData
    {
        public readonly Guid ID;
        public readonly string Name;
        public readonly int Creator;
        public readonly string Location;
        public readonly ushort MaxClients;

        public UILobbyMetaData(string name, int creator, string location, ushort maxClients)
        {
            Name = name;
            Creator = creator;
            Location = location;
            MaxClients = maxClients;
            ID = Guid.Empty;
        }

        public UILobbyMetaData(LobbyMetaData lobby)
        {
            Name = lobby.Name;
            Creator = lobby.CreatorId;
            Location = lobby.Scene;
            MaxClients = lobby.LobbyCapacity;
            ID = lobby.LobbyId;
        }
    }
}