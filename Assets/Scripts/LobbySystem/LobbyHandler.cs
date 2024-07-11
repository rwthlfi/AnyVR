using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbySystem
{
    public class LobbyHandler : NetworkBehaviour
    {
        private readonly SyncHashSet<int> _clientIds = new();
        private readonly SyncVar<int> _lobbyId;

        // private readonly LobbyMetaData _metaData;
        private readonly SceneLoadData _sceneLoadData; // TODO: to remove (move to LobbyManager)
        private bool _isSceneHandleRegistered; // TODO: to remove (move to LobbyManager)
        private Scene _scene; // TODO: to remove (move to LobbyManager)

        public override void OnStartServer()
        {
            base.OnStartServer();
            //LobbyManager.s_instance.SetLobbySceneHandle(_lobbyId, gameObject.scene);
        }

        public void AddClient(int clientId)
        {
            _clientIds.Add(clientId);
        }

        public void RemoveClient(int clientId)
        {
            _clientIds.Remove(clientId);
            if (_clientIds.Count == 0)
            {
                _isSceneHandleRegistered = false;
            }
        }

        internal void RegisterScene(Scene scene)
        {
            _isSceneHandleRegistered = true;
            _scene = scene;
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

        public SceneLoadData GetSceneLoadData()
        {
            if (!_isSceneHandleRegistered)
            {
                Debug.Log("Loading by name");
                return _sceneLoadData;
            }

            SceneLoadData sceneLoadData = new(_scene.handle)
            {
                ReplaceScenes = ReplaceOption.All,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.Physics3D }
            };
            return sceneLoadData;
        }
    }
}