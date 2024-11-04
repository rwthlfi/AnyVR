using FishNet.Managing.Scened;
using UnityEngine.SceneManagement;

namespace LobbySystem
{
    public struct LobbyMetaData
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public readonly string LobbyId;
        public readonly string Name;
        public readonly int CreatorId;
        public readonly string Scene;
        public readonly ushort LobbyCapacity;
        private readonly SceneLoadData _sceneLoadData;
        private int? _sceneHandle;

        public LobbyMetaData(string lobbyId, string name, int creatorId, string scene, ushort lobbyCapacity)
        {
            Name = name;
            Scene = scene;
            CreatorId = creatorId;
            LobbyCapacity = lobbyCapacity;
            LobbyId = lobbyId;
            _sceneHandle = null;
            _sceneLoadData = new SceneLoadData(scene)
            {
                ReplaceScenes = ReplaceOption.OnlineOnly,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.None },
                // By adding the string "lobby" the LobbyManager knows this scene is a lobby when the SceneManager.LoadEnd callback fires.
                // By adding the lobbyId the LobbyManager can register a corresponding LobbyHandler.
                // By adding the creatorId the LobbyManager can give that client administration rights in the lobby
                Params = { ServerParams = new object[] { "lobby", lobbyId, creatorId} } 
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
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.None },
                Params = { ServerParams = new object[] { "lobby", LobbyId, CreatorId} } 
            };
            return sceneLoadData;
        }

        internal void SetSceneHandle(int sceneHandle)
        {
            _sceneHandle = sceneHandle;
        }

        public override string ToString()
        {
            return $"LobbyMetaData (Id={LobbyId}, Name={Name}, Scene={Scene}, Creator={CreatorId}, MaxClients={LobbyCapacity})";
        }
    }
}