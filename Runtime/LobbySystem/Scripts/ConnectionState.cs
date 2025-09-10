using System;

namespace AnyVR.LobbySystem
{
    [Flags]
    public enum ConnectionState
    {
        Disconnected = 0, Client = 1 << 0, Server = 1 << 1
    }
}
