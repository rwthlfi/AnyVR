namespace LobbySystem.UI.LobbySelection
{
    public sealed class UILobbyMetaData
    {
        public readonly string ID;
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
            ID = GetHashCode().ToString();
        }

        public UILobbyMetaData(LobbyMetaData lobby)
        {
            Name = lobby.Name;
            Creator = lobby.Creator;
            Location = lobby.Scene;
            MaxClients = lobby.MaxClients;
            ID = lobby.ID;
        }
    }
}
