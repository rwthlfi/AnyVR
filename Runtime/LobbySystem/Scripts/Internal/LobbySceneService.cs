using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FishNet.Managing.Scened;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem.Internal
{
    internal partial class LobbySceneService
    {
        private readonly LobbyManagerInternal _internal;

        internal LobbySceneService(LobbyManagerInternal @internal)
        {
            _internal = @internal;

            if (_internal.ServerManager.Started)
            {
                Server_Constructor();
            }
        }

        internal static bool IsUnloadingLobby(UnloadQueueData queueData, bool asServer)
        {
            object[] loadParams = asServer
                ? queueData.SceneUnloadData.Params.ServerParams
                : DeserializeClientParams(queueData.SceneUnloadData.Params.ClientParams);

            if (loadParams.Length < 2 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }

            // Lobbies must have this flag
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.Lobby)
            {
                return false;
            }

            return loadParams[1] is Guid;
        }

        internal static bool IsLoadingLobby(LoadQueueData queueData, bool asServer)
        {
            object[] loadParams = asServer
                ? queueData.SceneLoadData.Params.ServerParams
                : DeserializeClientParams(queueData.SceneLoadData.Params.ClientParams);

            if (loadParams.Length < 2 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }

            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.Lobby)
            {
                return false;
            }

            Guid lobbyId = (Guid)loadParams[1];
            Assert.IsFalse(Guid.Empty == lobbyId);

            return true;
        }

        private static byte[] SerializeClientParams(object[] objects)
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

        private static object[] DeserializeClientParams(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return Array.Empty<object>();
            }

            using MemoryStream stream = new(bytes);
            BinaryFormatter formatter = new();
            return (object[])formatter.Deserialize(stream);
        }
    }
}
