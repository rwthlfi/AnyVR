using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobbySystem
{
    internal class LobbyHandler : NetworkBehaviour
    {
        private readonly SyncHashSet<int> _clientIds = new();
        private readonly SyncVar<int> _lobbyId = new();

        // The id of the admin
        private int _adminId;

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (!SceneManager.SceneConnections.TryGetValue(gameObject.scene,
                    out HashSet<NetworkConnection> connections))
            {
                return;
            }

            Debug.Log($"Lobby started on server!\nConnections: {connections.ToString()}");
            LobbyManager.s_instance.RegisterLobbyHandler(this, connections.First().ClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        internal void RemoveClient(int id, NetworkConnection conn = null)
        {
            if(conn != null && (id == conn.ClientId || _adminId == conn.ClientId))
            {
                _clientIds.Remove(id);
            }
        }

        public IEnumerable<int> GetClients()
        {
            int[] arr = new int[_clientIds.Count];
            uint i = 0;
            foreach (int id in _clientIds)
            {
                arr[i++] = id;
            }

            return arr;
        }
    }
}