using FishNet.Managing.Scened;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobbySystem
{
    public class LobbyHandler
    {
        private readonly HashSet<int> _clientIds;

        // private readonly LobbyMetaData _metaData;
        private readonly SceneLoadData _sceneLoadData;
        private bool _isSceneHandleRegistered;
        private Scene _scene;

        public LobbyHandler(LobbyMetaData lobbyMetaData, int id)
        {
            // _metaData = lobbyMetaData;
            _clientIds = new HashSet<int>();
            _sceneLoadData = new SceneLoadData(lobbyMetaData.Location)
            {
                ReplaceScenes = ReplaceOption.All,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.Physics3D }
            };
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