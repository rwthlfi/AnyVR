namespace AnyVR.LobbySystem
{
    public enum PlayerNameUpdateResult
    {
        Timeout = 0,
        Success,
        NameTaken,
        InvalidName,
        AlreadySet,
        TooLong,
        TooShort,
        Cancelled
    }
}
