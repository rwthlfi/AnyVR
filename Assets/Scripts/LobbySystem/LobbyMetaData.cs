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
        public readonly string Location;
        public readonly ushort MaxClients;
        private readonly SceneLoadData _sceneLoadData;
        private Scene? _sceneHandle;

        public LobbyMetaData(string id, string name, int creator, string location, ushort maxClients, SceneLoadData sceneLoadData)
        {
            Name = name;
            Location = location;
            Creator = creator;
            MaxClients = maxClients;
            ID = id;
            _sceneLoadData = sceneLoadData;
            _sceneHandle = null;
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
            SceneLoadData sceneLoadData = new(((Scene)_sceneHandle).handle)
            {
                ReplaceScenes = ReplaceOption.All,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.Physics3D }
            };
            return sceneLoadData;
        }

        internal void SetSceneHandle(Scene scene)
        {
            _sceneHandle = scene;
        }
    }
}