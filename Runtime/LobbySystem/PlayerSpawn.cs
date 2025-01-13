using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace LobbySystem
{
    /// <summary>
    ///     Spawns a player object for clients when they connect.
    ///     Must be placed on or beneath the NetworkManager object.
    /// </summary>
    public class PlayerSpawn : NetworkBehaviour
    {
        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [SerializeField] private NetworkObject _playerPrefab;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SpawnPlayer();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnPlayer(NetworkConnection conn = null)
        {
            NetworkObject nob = Instantiate(_playerPrefab);
            nob.gameObject.name = $"Player ({PlayerNameTracker.GetPlayerName(conn)})";
            nob.transform.position = transform.position;
            Spawn(nob, conn, gameObject.scene);
        }
    }
}