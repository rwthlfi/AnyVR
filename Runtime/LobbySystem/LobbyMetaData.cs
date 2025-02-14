using FishNet.Managing.Scened;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.SceneManagement;

namespace AnyVr.LobbySystem
{
    public class LobbyMetaData
    {
        private readonly SceneLoadData _sceneLoadData;
        public readonly int CreatorId;
        public readonly bool IsPasswordProtected;
        public readonly ushort LobbyCapacity;

        /// <summary>
        ///     Unique identifier
        /// </summary>
        public readonly Guid LobbyId;

        public readonly string Name;
        public readonly string Scene;
        public readonly DateTime? ExpireDate;
        private int? _sceneHandle;

        public LobbyMetaData() { }

        public LobbyMetaData(Guid lobbyId, string name, int creatorId, string scene, ushort lobbyCapacity,
            bool isPasswordProtected, DateTime? expireDate)
        {
            Name = name;
            Scene = scene;
            CreatorId = creatorId;
            LobbyCapacity = lobbyCapacity;
            LobbyId = lobbyId;
            IsPasswordProtected = isPasswordProtected;
            ExpireDate = expireDate;
            _sceneHandle = null;
            _sceneLoadData = new SceneLoadData(scene)
            {
                ReplaceScenes = ReplaceOption.OnlineOnly,
                Options = { AllowStacking = true, LocalPhysics = LocalPhysicsMode.None },
                // By adding SceneLoadParam.Lobby the LobbyManager knows this scene is a lobby when the SceneManager.LoadEnd callback fires.
                // By adding the lobbyId the LobbyManager can register a corresponding LobbyHandler.
                // By adding the creatorId the LobbyManager can give that client administration rights in the lobby
                Params = { ServerParams = new object[] { SceneLoadParam.k_lobby, lobbyId, creatorId } }
            };
            _sceneLoadData.Params.ClientParams = SerializeObjects(_sceneLoadData.Params.ServerParams);
        }

        /// <summary>
        ///     Returns the SceneLoadData of the lobby as handle if possible.
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
                Params = { ServerParams = new object[] { SceneLoadParam.k_lobby, LobbyId, CreatorId } }
            };
            sceneLoadData.Params.ClientParams = SerializeObjects(sceneLoadData.Params.ServerParams);
            return sceneLoadData;
        }

        internal static byte[] SerializeObjects(object[] objects)
        {
            if (objects == null)
            {
                return null;
            }

            using MemoryStream stream = new();
            BinaryFormatter formatter = new();
            formatter.Serialize(stream, objects);
            return stream.ToArray();
        }

        internal static object[] DeserializeClientParams(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return Array.Empty<object>();
            }

            using MemoryStream stream = new(bytes);
            BinaryFormatter formatter = new();
            return (object[])formatter.Deserialize(stream);
        }


        internal void SetSceneHandle(int sceneHandle)
        {
            _sceneHandle = sceneHandle;
        }

        public override string ToString()
        {
            return
                $"LobbyMetaData (Id={LobbyId}, Name={Name}, Scene={Scene}, Creator={CreatorId}, MaxClients={LobbyCapacity}, IsPasswordProtected={IsPasswordProtected})";
        }

        public override bool Equals(object obj)
        {
            if (obj is not LobbyMetaData other)
            {
                return false;
            }

            return GetHashCode() == other.GetHashCode();
        }


        public override int GetHashCode()
        {
            return HashCode.Combine(LobbyId, Name, CreatorId, Scene, LobbyCapacity);
        }
    }
}