using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    public class LobbyPlayerState : PlayerState
    {
        /// <summary>
        ///     Prefab to spawn for the player.
        /// </summary>
        [SerializeField] private NetworkObject _playerPrefab;
        
        private NetworkObject _onlinePlayer;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_playerPrefab == null)
                return;
            
            NetworkObject nob = Instantiate(_playerPrefab);
            _onlinePlayer = nob;
            Spawn(nob, Owner, gameObject.scene);
        }

        public override void OnDespawnServer(NetworkConnection conn)
        {
            base.OnDespawnServer(conn);
            if (conn == Owner)
            {
                Despawn(_onlinePlayer);
            }
        }
    }
}
