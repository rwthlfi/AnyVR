namespace AnyVR.LobbySystem
{
    /// <summary>
    ///     Override this class to implement server-side gameplay logic on the server.
    ///     <seealso cref="GameModeBase" />
    /// </summary>
    public class GlobalGameMode : GameModeBase
    {
#region Lifecycle

        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneManager.OnClientLoadedStartScenes += (conn, _) =>
            {
                PlayerStateBase ps = SpawnPlayerState(conn);
                SpawnPlayerController(conn, ps);
            };
        }

#endregion
    }
}
