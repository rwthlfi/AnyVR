namespace LobbySystem
{
    public struct LobbyMetaData
    {
        public string Name;
        public string Location;
        public int Creator;
        public ushort MaxClients;
        public string Id;

        public override string ToString()
        {
            return Name;
        }
    }
}