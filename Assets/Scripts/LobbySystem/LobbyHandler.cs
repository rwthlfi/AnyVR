using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Voicechat;

namespace LobbySystem
{
    public class LobbyHandler : NetworkBehaviour
    {
        private readonly SyncHashSet<int> _clientIds = new();
        private readonly SyncVar<string> _lobbyId = new();
        private readonly SyncVar<int> _adminId = new();
        private readonly SyncVar<bool> _initialized = new(false);

        [Server]
        internal void Init(string lobbyId, int adminId)
        {
            _lobbyId.Value = lobbyId;
            _adminId.Value = adminId;
            _initialized.Value = true;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            AddClient();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/UIScene", LoadSceneMode.Additive);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddClient(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            _clientIds.Add(conn.ClientId);
            LobbyManager.s_instance.Log($"Client {conn.ClientId} joined lobby {_lobbyId.Value}");
        }
        
        [ServerRpc(RequireOwnership = false)]
        internal void RemoveClient(int clientId, NetworkConnection conn = null)
        {
            if (conn == null || (clientId != conn.ClientId && _adminId.Value != conn.ClientId))
            {
                return;
            }

            _clientIds.Remove(clientId);
            LobbyManager.s_instance.Log($"Client {conn.ClientId} left lobby {_lobbyId.Value}");
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

        public void SetMuteSelf(bool muteSelf)
        {
            LiveKitManager.s_instance.SetMicrophoneEnabled(!muteSelf);
        }
    }
}