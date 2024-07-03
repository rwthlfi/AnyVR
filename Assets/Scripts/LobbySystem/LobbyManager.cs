using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voicechat;

namespace LobbySystem
{
    public class LobbyManager : NetworkBehaviour
    {
        private readonly SyncHashSet<LobbyMetaData> _lobbies = new();

        // only on server
        private readonly Dictionary<LobbyMetaData, LobbyHandler> _lobbyHandlers = new();
        private LobbyMetaData? _currentLobby;

        private Dictionary<LobbyMetaData, SceneLoadData> _lobbySceneData;

        private void Awake()
        {
            InitSingleton();
        }

        private void Start()
        {
            _lobbies.OnChange += lobbies_OnChange;
        }

        private void OnDestroy()
        {
            _lobbies.OnChange -= lobbies_OnChange;
        }

        public event Action<LobbyMetaData> LobbyOpened;
        public event Action<LobbyMetaData> LobbyClosed;
        public event Action<LobbyMetaData[]> ReceivedCurrentLobbies;
        public event Action<LobbyMetaData> LobbyJoined;
        public event Action<int> ClientJoined;
        public event Action<int> ClientLeft;
        public event Action LobbyLeft;
        public event Action<int[]> LobbyClientListReceived;

        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneManager.OnLoadEnd += OnSceneLoadEnd;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            LobbyJoined += Client_OnLobbyJoined;
            LobbyLeft += Client_OnLobbyLeft;
        }

        private void Client_OnLobbyLeft()
        {
            _currentLobby = null;
            Debug.Log("Leaving voice chat");
            RequestCurrentLobbiesRpc();
            LiveKitManager.s_instance.Disconnect();
        }

        private void Client_OnLobbyJoined(LobbyMetaData metaData)
        {
            _currentLobby = metaData;
            if (LiveKitManager.s_instance != null)
            {
                LiveKitManager.s_instance.TryConnectToRoom(metaData.Id, LobbySystem.SceneManager.EnteredUserName);
            }
            else
            {
                Debug.LogWarning("LivKitManager is not initialized");
            }
        }

        private void lobbies_OnChange(SyncHashSetOperation op, LobbyMetaData item, bool asServer)
        {
            if (asServer)
            {
                return;
            }

            switch (op)
            {
                case SyncHashSetOperation.Add:
                    LobbyOpened?.Invoke(item);
                    break;
                case SyncHashSetOperation.Remove:
                    LobbyClosed?.Invoke(item);
                    break;
                case SyncHashSetOperation.Clear:
                    foreach (LobbyMetaData lobby in _lobbyHandlers.Keys)
                    {
                        LobbyClosed?.Invoke(lobby);
                    }

                    break;
                case SyncHashSetOperation.Complete:
                    break;
                case SyncHashSetOperation.Update:
                    //TODO: handle update
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestCurrentLobbiesRpc(NetworkConnection conn = null)
        {
            ReceiveCurrentLobbiesRpc(conn, _lobbies.ToArray());
        }

        [TargetRpc]
        private void ReceiveCurrentLobbiesRpc(NetworkConnection _, LobbyMetaData[] lobbies)
        {
            ReceivedCurrentLobbies?.Invoke(lobbies);
        }

        private void OnSceneLoadEnd(SceneLoadEndEventArgs obj)
        {
            if (!obj.QueueData.AsServer)
            {
                return;
            }

            foreach (Scene scene in obj.LoadedScenes)
            {
                if (obj.QueueData.Connections.Length == 0)
                {
                    return;
                }

                if (!TryGetLobbyOfClient(obj.QueueData.Connections.First().ClientId, out LobbyMetaData lmd))
                {
                    continue;
                }

                Debug.Log($"Client {obj.QueueData.Connections.First().ClientId} loaded scene!");
                _lobbyHandlers[lmd].RegisterScene(scene);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateLobbyRpc(LobbyMetaData lobbyMetaData, NetworkConnection _ = null)
        {
            // Each player can only be the owner of one lobby at a time. 
            // lobbyMetaData.Creator is therefore a sufficient unique lobby id
            LobbyHandler
                lobbyHandler =
                    new(lobbyMetaData, lobbyMetaData.Creator); //TODO replace creator with unique id of the player
            _lobbies.Add(lobbyMetaData);
            _lobbyHandlers.Add(lobbyMetaData, lobbyHandler);
            OnLobbyCreatedRpc(lobbyMetaData);
        }

        private void DestroyLobby(LobbyMetaData lmd)
        {
            if (!_lobbyHandlers.TryGetValue(lmd, out LobbyHandler handler))
            {
                return;
            }

            foreach (int client in handler.GetClients())
            {
                handler.RemoveClient(client);
                OnLobbyLeftRpc(ClientManager.Clients[client]);
            }

            _lobbyHandlers.Remove(lmd);
            _lobbies.Remove(lmd);
            OnLobbyDestroyedRpc(lmd);
        }

        [ObserversRpc]
        private void OnLobbyCreatedRpc(LobbyMetaData lobbyMeta)
        {
            if (lobbyMeta.Creator == ClientManager.Connection.ClientId)
            {
                JoinLobbyRpc(lobbyMeta);
            }
        }

        [ObserversRpc]
        private void OnLobbyDestroyedRpc(LobbyMetaData _)
        {
            RequestCurrentLobbiesRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinLobbyRpc(LobbyMetaData lmd, NetworkConnection conn = null)
        {
            //TODO: leave current lobby 
            if (!_lobbyHandlers.TryGetValue(lmd, out LobbyHandler lobby))
            {
                return;
            }

            if (conn == null)
            {
                return;
            }

            lobby.AddClient(conn.ClientId);

            foreach (int clientId in lobby.GetClients())
            {
                if (conn.ClientId == clientId)
                {
                    continue;
                }

                if (!ServerManager.Clients.TryGetValue(clientId, out NetworkConnection c))
                {
                    continue;
                }

                OnClientJoinedLobbyRpc(c, conn.ClientId);
            }

            SceneManager.LoadConnectionScenes(conn, lobby.GetSceneLoadData());
            Debug.LogWarning("Client joined lobby! sending callback ...");
            OnLobbyJoinedRpc(conn, lmd);
        }

        [TargetRpc]
        private void OnClientJoinedLobbyRpc(NetworkConnection _, int joinedClientId)
        {
            ClientJoined?.Invoke(joinedClientId);
        }

        [TargetRpc]
        private void OnClientLeftLobbyRpc(NetworkConnection _, int leftClientId)
        {
            ClientLeft?.Invoke(leftClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void LeaveLobbyRpc(NetworkConnection conn = null)
        {
            if (conn == null)
            {
                return;
            }

            if (!TryGetLobbyOfClient(conn.ClientId, out LobbyMetaData lmd))
            {
                return;
            }

            if (_lobbyHandlers.TryGetValue(lmd, out LobbyHandler handler))
            {
                handler.RemoveClient(conn.ClientId);
            }

            SceneLoadData sld = new(new[] { "LobbyScene" }) { ReplaceScenes = ReplaceOption.All };
            SceneManager.LoadConnectionScenes(conn, sld);
            OnLobbyLeftRpc(conn);

            if (handler == null)
            {
                return;
            }

            foreach (int clientId in handler.GetClients())
            {
                if (conn.ClientId == clientId)
                {
                    continue;
                }

                if (!ServerManager.Clients.TryGetValue(clientId, out NetworkConnection c))
                {
                    continue;
                }

                OnClientLeftLobbyRpc(c, conn.ClientId);
            }

            if (!handler.GetClients().Any())
            {
                DestroyLobby(lmd);
            }
        }


        [TargetRpc]
        private void OnLobbyLeftRpc(NetworkConnection _)
        {
            LobbyLeft?.Invoke();
        }

        [TargetRpc]
        private void OnLobbyJoinedRpc(NetworkConnection _, LobbyMetaData lmd)
        {
            LobbyJoined?.Invoke(lmd);
        }

        [Server]
        private bool TryGetLobbyOfClient(int clientId, out LobbyMetaData lmd)
        {
            foreach (KeyValuePair<LobbyMetaData, LobbyHandler> lobbyPair in from lobbyPair in _lobbyHandlers
                     let clients = lobbyPair.Value.GetClients()
                     where clients.Any(client => client == clientId)
                     select lobbyPair)
            {
                lmd = lobbyPair.Key;
                return true;
            }

            lmd = default;
            return false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestLobbyClientListRpc(LobbyMetaData lmd, NetworkConnection conn = null)
        {
            int[] clients = { };
            if (_lobbyHandlers.TryGetValue(lmd, out LobbyHandler handler))
            {
                clients = handler.GetClients().ToArray();
            }

            ReceiveLobbiesClientListRpc(conn, clients);
        }

        [TargetRpc]
        private void ReceiveLobbiesClientListRpc(NetworkConnection _, int[] clients)
        {
            LobbyClientListReceived?.Invoke(clients);
        }

        public LobbyMetaData? GetCurrentLobby()
        {
            return _currentLobby;
        }

        #region Singleton

        public static LobbyManager s_instance;

        private void InitSingleton()
        {
            if (s_instance != null)
            {
                Debug.LogWarning("Instance of LobbyManager already exists!");
                Destroy(this);
            }

            s_instance = this;
        }

        #endregion
    }
}