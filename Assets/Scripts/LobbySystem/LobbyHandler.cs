using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TextChat;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voicechat;

namespace LobbySystem
{
    [RequireComponent(typeof(TextChatManager))]
    public class LobbyHandler : NetworkBehaviour
    {
        private readonly SyncHashSet<int> _clientIds = new();
        private readonly SyncVar<string> _lobbyId = new();
        private readonly SyncVar<int> _adminId = new();
        private readonly SyncVar<bool> _initialized = new(false);

        /// <summary>
        /// Invoked when an remote client joined the lobby of the local client
        /// </summary>
        public event Action<int, string> ClientJoin;
        
        /// <summary>
        /// Invoked when an remote client left the lobby of thk local client
        /// </summary>
        public event Action<int> ClientLeft;
        
        // Only assigned on client
        [CanBeNull] private static LobbyHandler s_instance;
        
        // Only invoked on client after ClientStart
        public static event Action PostInit;
        
        public TextChatManager TextChat { get; private set; }

        [Server]
        internal void Init(string lobbyId, int adminId)
        {
            _lobbyId.Value = lobbyId;
            _adminId.Value = adminId;
            _initialized.Value = true;
        }

        /// <summary>
        /// Returns the client id of the admin
        /// </summary>
        public int GetAdminId()
        {
            return _adminId.Value;
        }

        private void Awake()
        {
            TextChat = GetComponent<TextChatManager>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _clientIds.OnChange += OnClientUpdate;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            if (s_instance != null)
            {
                Debug.LogError("Instance of LobbyHandler is already initialized");
                return;
            }
            s_instance = this;

            _clientIds.OnChange += OnClientUpdate;
            
            AddClient();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/UIScene", LoadSceneMode.Additive);

            if (!LiveKitManager.s_instance.TryGetAvailableMicrophoneNames(out string[] micNames))
            {
                return;
            }

            string msg = micNames.Aggregate("Available Microphones:\n",
                (current, micName) => current + "\t" + micName + "\n");
            Debug.Log(msg);
            LiveKitManager.s_instance.SetActiveMicrophone(micNames[1]);
            
            PostInit?.Invoke();
        }

        private void OnClientUpdate(SyncHashSetOperation op, int item, bool asServer)
        {
            if (asServer)
            {
                // ClientJoin & ClientLeft callbacks are already invoked from the 'AddClient' and 'RemoveClient' server RPC
                return;
            }
            switch (op)
            {
                case SyncHashSetOperation.Add:
                    ClientJoin?.Invoke(item, PlayerNameTracker.GetPlayerName(item));
                    break;
                case SyncHashSetOperation.Remove:
                    ClientLeft?.Invoke(item);
                    break;
                case SyncHashSetOperation.Clear:
                    // TODO
                    break;
                case SyncHashSetOperation.Complete:
                    break;
                case SyncHashSetOperation.Update:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddClient(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }
            Server_AddClient(conn.ClientId, PlayerNameTracker.GetPlayerName(conn));
        }

        [Server]
        private void Server_AddClient(int clientId, string clientName)
        {
            _clientIds.Add(clientId);
            LobbyManager.s_instance.Log($"Client {clientId} joined lobby {_lobbyId.Value}");
            ClientJoin?.Invoke(clientId, clientName);
        }
        
        [ServerRpc(RequireOwnership = false)]
        internal void RemoveClient(int clientId, NetworkConnection conn = null)
        {
            if (conn == null || (clientId != conn.ClientId && _adminId.Value != conn.ClientId))
            {
                return;
            }
            Server_RemoveClient(clientId);
        }
        
        [Server]
        internal void Server_RemoveClient(int clientId)
        {
            _clientIds.Remove(clientId);
            LobbyManager.s_instance.Log($"Client {clientId} left lobby {_lobbyId.Value}");
            ClientLeft?.Invoke(clientId);
        }

        public (int, string)[] GetClients()
        {
            HashSet<int> clientIds = _clientIds.Collection;
            (int, string)[] clients = new (int, string)[clientIds.Count];
            int i = 0;
            foreach (int clientId in clientIds)
            {
                clients[i] = (clientId, PlayerNameTracker.GetPlayerName(clientId));
                i++;
            }
            return clients;
        }

        public void SetMuteSelf(bool muteSelf)
        {
            LiveKitManager.s_instance.SetMicrophoneEnabled(!muteSelf);
        }

        [CanBeNull]
        public static LobbyHandler GetInstance()
        {
            return s_instance;
        }

        private void OnDestroy()
        {
            _clientIds.OnChange -= OnClientUpdate;
        }

        [Client]
        public void Leave()
        {
            LobbyManager.s_instance.LeaveLobby();
        }
    }
}