namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Override this class to implement server-side gameplay logic on the server.
    ///     <seealso cref="GameModeBase" />
    /// </summary>
    public class GlobalGameMode : GameModeBase
    {
        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneManager.OnClientLoadedStartScenes += (conn, asServer) =>
            {
                PlayerStateBase ps = SpawnPlayerState(conn);
                SpawnPlayerController(conn, ps);
            };
        }
    }
}
