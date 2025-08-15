using FishNet.Serializing;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    public class PlayerState
    {
        public int ID;
        public string PlayerName;
        public bool IsAdmin;
    }

    public static class PlayerInfoSerializer
    {
        public static void WritePlayerInfo(this Writer writer, PlayerState value)
        {
            Debug.Log("PlayerInfoSerializer::WritePlayerInfo");
            writer.WriteInt32(value.ID);
            writer.WriteString(value.PlayerName);
            writer.WriteBoolean(value.IsAdmin);
        }

        public static PlayerState ReadPlayerInfo(this Reader reader)
        {
            Debug.Log("Reading PlayerInfo");
            PlayerState p = new();
            p.ID = reader.ReadInt32();
            p.PlayerName = reader.ReadString();
            p.IsAdmin = reader.ReadBoolean();
            return p;
        }
    }
}
