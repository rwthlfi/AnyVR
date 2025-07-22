using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FishNet.Managing.Scened;
using UnityEngine.SceneManagement;

namespace AnyVR.LobbySystem
{
    public class LobbyMetaData
    {
        private readonly SceneLoadData _sceneLoadData;
        public readonly int CreatorId;
        public readonly DateTime? ExpireDate;
        public readonly bool IsPasswordProtected;
        public readonly ushort LobbyCapacity;

        /// <summary>
        ///     Unique identifier
        /// </summary>
        public readonly Guid LobbyId;

        public readonly string Name;
        public readonly string SceneName;
        public readonly string ScenePath;

        public LobbyMetaData() { }

        private LobbyMetaData(Guid lobbyId, string name, int creatorId, string scenePath, string sceneName, ushort lobbyCapacity,
            bool isPasswordProtected, DateTime? expireDate)
        {
            Name = name;
            ScenePath = scenePath;
            SceneName = sceneName;
            CreatorId = creatorId;
            LobbyCapacity = lobbyCapacity;
            LobbyId = lobbyId;
            IsPasswordProtected = isPasswordProtected;
            ExpireDate = expireDate;
            SceneHandle = null;
            _sceneLoadData = new SceneLoadData(scenePath)
            {
                ReplaceScenes = ReplaceOption.OnlineOnly,
                Options =
                {
                    AllowStacking = true, LocalPhysics = LocalPhysicsMode.None, AutomaticallyUnload = false
                },
                // By adding SceneLoadParam.Lobby the LobbyManager knows this scene is a lobby when the SceneManager.LoadEnd callback fires.
                // By adding the lobbyId the LobbyManager can register a corresponding LobbyHandler.
                // By adding the creatorId the LobbyManager can give that client administration rights in the lobby
                Params =
                {
                    ServerParams = new object[]
                    {
                        SceneLoadParam.Lobby, lobbyId, creatorId
                    }
                }
            };
            _sceneLoadData.Params.ClientParams = SerializeObjects(_sceneLoadData.Params.ServerParams);
        }

        public int? SceneHandle { get; private set; }

        /// <summary>
        ///     Returns the SceneLoadData of the lobby as handle if possible.
        /// </summary>
        public SceneLoadData GetSceneLoadData()
        {
            if (SceneHandle == null)
            {
                return _sceneLoadData;
            }

            SceneLoadData sceneLoadData = new((int)SceneHandle)
            {
                ReplaceScenes = ReplaceOption.OnlineOnly,
                Options =
                {
                    AllowStacking = true, LocalPhysics = LocalPhysicsMode.None, AutomaticallyUnload = false
                },
                Params =
                {
                    ServerParams = new object[]
                    {
                        SceneLoadParam.Lobby, LobbyId, CreatorId
                    }
                }
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
            SceneHandle = sceneHandle;
        }

        public override string ToString()
        {
            return
                $"LobbyMetaData (Id={LobbyId}, Name={Name}, Scene={ScenePath}, Creator={CreatorId}, MaxClients={LobbyCapacity}, IsPasswordProtected={IsPasswordProtected})";
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
            return HashCode.Combine(LobbyId, Name, CreatorId, ScenePath, LobbyCapacity);
        }

        public class Builder
        {
            private int _creatorId;
            private DateTime? _expireDate;
            private bool _isPasswordProtected;
            private ushort _lobbyCapacity = 16;
            private string _name;
            private string _sceneName;
            private string _scenePath;

            public Builder WithName(string name)
            {
                _name = name;
                return this;
            }
            public Builder WithScene(string path) { return WithScene(path, Path.GetFileNameWithoutExtension(path)); }
            public Builder WithScene(string path, string name)
            {
                _scenePath = path;
                _sceneName = name;
                return this;
            }
            public Builder WithCreator(int id)
            {
                _creatorId = id;
                return this;
            }
            public Builder WithCapacity(ushort cap)
            {
                _lobbyCapacity = cap;
                return this;
            }
            public Builder WithPasswordProtection(bool enabled)
            {
                _isPasswordProtected = enabled;
                return this;
            }
            public Builder WithExpiration(DateTime? expireDate)
            {
                _expireDate = expireDate;
                return this;
            }

            public LobbyMetaData Build()
            {
                return new LobbyMetaData(Guid.NewGuid(), _name, _creatorId, _scenePath, _sceneName, _lobbyCapacity, _isPasswordProtected, _expireDate);
            }
        }
    }
}
