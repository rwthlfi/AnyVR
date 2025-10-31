using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem
{
    public partial class LobbyPlayerState
    {
        public override void OnStartServer()
        {
            base.OnStartServer();

            // Initialize replicated fields
            LobbyGameMode lobbyGameMode =
                gameObject.scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<LobbyGameMode>())
                    .FirstOrDefault(comp => comp != null);

            Assert.IsNotNull(lobbyGameMode, "LobbyHandler not found. Ensure there is one LobbyHandler placed in the lobby scene.");

            _lobbyId.Value = lobbyGameMode.GetGameState().LobbyId;
            _isAdmin.Value = lobbyGameMode.GetGameState().LobbyInfo.CreatorId == OwnerId;
        }

        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            if (conn == Owner)
            {
                Despawn(_playerAvatar);
            }
        }

        [Server]
        public void SetIsAdmin(bool b)
        {
            _isAdmin.Value = b;
        }
    }
}
