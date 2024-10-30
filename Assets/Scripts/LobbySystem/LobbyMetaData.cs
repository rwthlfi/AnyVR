using FishNet.Managing.Scened;
using UnityEngine.SceneManagement;

namespace LobbySystem
{
    public struct LobbyMetaData
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public readonly string ID;
        public readonly string Name;
        public readonly int Creator;
        public readonly string Scene;
        public readonly ushort LobbyCapacity;
        private readonly SceneLoadData _sceneLoadData;
        private int? _sceneHandle;

        public LobbyMetaData(string id, string name, int creator, string scene, ushort lobbyCapacity)
        {
            Name = name;
            Scene = scene;
            Creator = creator;
            LobbyCapacity = lobbyCapacity;
            ID = id;
            _sceneHandle = null;
            _sceneLoadData = new SceneLoadData(scene)
            {
                ReplaceScenes = ReplaceOption.OnlineOnly,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.None }
            };
        }

        /// <summary>
        /// Returns the SceneLoadData of the lobby as handle if possible.
        /// </summary>
        public SceneLoadData GetSceneLoadData()
        {
            if (_sceneHandle == null)
            {
                return _sceneLoadData;
            }
            SceneLoadData sceneLoadData = new((int)_sceneHandle)
            {
                ReplaceScenes = ReplaceOption.OnlineOnly,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.None }
            };
            return sceneLoadData;
        }

        internal void SetSceneHandle(int sceneHandle)
        {
            _sceneHandle = sceneHandle;
        }

        public override string ToString()
        {
            return $"LobbyMetaData (Id={ID}, Name={Name}, Scene={Scene}, Creator={Creator}, MaxClients={LobbyCapacity})";
        }
    }
}