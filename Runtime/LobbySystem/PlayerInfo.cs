namespace AnyVR.LobbySystem
{
    public class PlayerInfo
    {
        public readonly int ID;
        public string PlayerName;

        // Required for FishNet custom serializer/deserializer
        public PlayerInfo() { }
        public PlayerInfo(string playerName, int id)
        {
            PlayerName = playerName;
            ID = id;
        }

        public void SetPlayerName(string playerName)
        {
            PlayerName = playerName;
        }
    }
}
