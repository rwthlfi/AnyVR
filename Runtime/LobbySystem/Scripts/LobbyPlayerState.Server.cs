using System.Linq;
using AnyVR.LobbySystem.Internal;
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
            LobbyHandler lobbyHandler =
                gameObject.scene.GetRootGameObjects()
                    .Select(root => root.GetComponent<LobbyHandler>())
                    .FirstOrDefault(comp => comp != null);

            Assert.IsNotNull(lobbyHandler, "LobbyHandler not found. Ensure there is one LobbyHandler placed in the lobby scene.");

            _lobbyId.Value = lobbyHandler.State.LobbyId;
            _isAdmin.Value = lobbyHandler.State.Info.CreatorId == OwnerId;
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
