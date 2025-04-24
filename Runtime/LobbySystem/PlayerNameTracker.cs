using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System;

namespace AnyVR.LobbySystem
{
    public class PlayerNameTracker : NetworkBehaviour
    {
        private static PlayerNameTracker s_instance;

        private readonly SyncDictionary<NetworkConnection, string> _playerNames = new();

        private void Awake()
        {
            s_instance = this;
            _playerNames.OnChange += _playerNames_OnChange;
        }

        public static event Action<NetworkConnection, string> NameChange;

        public override void OnStartClient()
        {
            base.OnStartClient();
            SetName(ConnectionManager.UserName);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            NetworkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
        }

        private void ServerManager_OnRemoteConnectionState(NetworkConnection connection,
            RemoteConnectionStateArgs state)
        {
            if (state.ConnectionState != RemoteConnectionState.Started)
            {
                _playerNames.Remove(connection);
            }
        }

        private static void _playerNames_OnChange(SyncDictionaryOperation op, NetworkConnection key, string value,
            bool server)
        {
            if (op is SyncDictionaryOperation.Add or SyncDictionaryOperation.Set)
            {
                NameChange?.Invoke(key, value);
            }
        }

        public static string GetPlayerName(NetworkConnection connection)
        {
            return s_instance._playerNames.TryGetValue(connection, out string name) ? name : string.Empty;
        }

        public static string GetPlayerName(int clientId)
        {
            return InstanceFinder.ClientManager.Clients.TryGetValue(clientId, out NetworkConnection connection)
                ? GetPlayerName(connection)
                : "invalidId";
        }

        [Client]
        private static void SetName(string playerName)
        {
            s_instance.ServerSetName(playerName);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerSetName(string playerName, NetworkConnection conn = null)
        {
            //TODO: Handle unique player id
            if (conn != null)
            {
                _playerNames[conn] = playerName;
            }
        }
    }
}