namespace AnyVR.LobbySystem
{
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
