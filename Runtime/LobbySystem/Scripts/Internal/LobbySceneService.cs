using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbySceneService
    {
        private const string Tag = nameof(LobbySceneService);
        private readonly LobbyManagerInternal _internal;

        private TaskCompletionSource<LobbyHandler> _loadSceneTcs;

        internal LobbySceneService(LobbyManagerInternal @internal)
        {
            _internal = @internal;
            _internal.SceneManager.OnUnloadEnd += OnUnloadEnd;

            if (_internal.ServerManager.Started)
                _internal.SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
        }

        [Server]
        internal async Task<LobbyHandler> StartConnectionScene(LobbyState lobbyState)
        {
            Assert.IsTrue(_internal.ServerManager.Started);

            if (_loadSceneTcs != null)
            {
                // Another scene is already loading.
                // TODO queue scene loading
                return null;
            }

            _loadSceneTcs = new TaskCompletionSource<LobbyHandler>(TaskCreationOptions.RunContinuationsAsynchronously);

            Logger.Log(LogLevel.Verbose, Tag, "Loading lobby scene. Waiting for lobby handler");
            _internal.SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), CreateSceneLoadData(lobbyState));

            Task delay = Task.Delay(TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_loadSceneTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                Logger.Log(LogLevel.Error, Tag, "Timeout while waiting for LobbyHandler");
                _loadSceneTcs = null;
                return null;
            }

            LobbyHandler result = await _loadSceneTcs.Task;

            Assert.IsTrue(_loadSceneTcs.Task.IsCompletedSuccessfully);

            _loadSceneTcs = null;

            return result;
        }

        [Server]
        private void TryRegisterLobbyHandler(SceneLoadEndEventArgs loadArgs)
        {
            if (_loadSceneTcs == null)
            {
                return;
            }

            if (!IsLoadingLobby(loadArgs.QueueData, true, out string errorMsg))
            {
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.Log(LogLevel.Warning, Tag, $"Can't register LobbyHandler. {errorMsg}");
                }

                return;
            }


            object[] serverParams = loadArgs.QueueData.SceneLoadData.Params.ServerParams;

            if (serverParams[1] is not Guid lobbyId)
            {
                return;
            }

            int creatorId = (int)serverParams[2];

            Assert.IsTrue(_internal.ServerManager.Clients.ContainsKey(creatorId));
            Assert.IsFalse(_internal.Lobbies.ContainsKey(lobbyId));

            GameObject[] rootObjects = loadArgs.LoadedScenes[0].GetRootGameObjects();

            LobbyHandler lobbyHandler = null;
            foreach (GameObject root in rootObjects)
            {
                LobbyHandler comp = root.GetComponent<LobbyHandler>();
                if (comp == null)
                    continue;

                lobbyHandler = comp;
                break;
            }

            Assert.IsNotNull(lobbyHandler);

            Logger.Log(LogLevel.Verbose, Tag, $"Found LobbyHandler with lobbyId '{lobbyId}'");
            _loadSceneTcs.SetResult(lobbyHandler);
        }

        [Client]
        private void OnUnloadEnd(SceneUnloadEndEventArgs args)
        {
            if (args.QueueData.AsServer)
            {
                return;
            }

            if (!IsUnloadingLobby(args.QueueData, false))
            {
                return;
            }

            AsyncOperation op = USceneManager.LoadSceneAsync(LobbyManager.LobbyConfiguration.OfflineScene, LoadSceneMode.Additive);
            if (op != null)
            {
                op.completed += _ =>
                {
                    USceneManager.SetActiveScene(USceneManager.GetSceneByPath(LobbyManager.LobbyConfiguration.OfflineScene));
                };
            }
        }

        private static bool IsUnloadingLobby(UnloadQueueData queueData, bool asServer)
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

        private bool IsLoadingLobby(LoadQueueData queueData, bool asServer, out string errorMsg)
        {
            object[] loadParams = asServer
                ? queueData.SceneLoadData.Params.ServerParams
                : DeserializeClientParams(queueData.SceneLoadData.Params.ClientParams);

            errorMsg = string.Empty;

            if (loadParams.Length < 3 || loadParams[0] is not SceneLoadParam)
            {
                return false;
            }

            // Lobbies must have this flag
            if ((SceneLoadParam)loadParams[0] != SceneLoadParam.Lobby)
            {
                return false;
            }

            // Try get corresponding lobbyId
            Guid lobbyId = (Guid)loadParams[1];
            if (Guid.Empty == lobbyId)
            {
                errorMsg = "The passed lobbyId is null.";
                return false;
            }

            // Check that the creating client is passed as param
            if (loadParams[2] is int)
                return true;

            errorMsg = "The clientId should be passed as an int.";
            return false;
        }

        /// <summary>
        ///     Returns the SceneLoadData of the lobby as handle if possible.
        /// </summary>
        [Server]
        internal static SceneLoadData CreateSceneLoadData(LobbyState lobbyState)
        {
            LobbyManagerInternal manager = LobbyManager.Instance.Internal;
            Assert.IsNotNull(manager);

            int sceneHandle;
            string scenePath;

            LobbyHandler handler = manager.GetLobbyHandler(lobbyState.LobbyId);
            if (handler != null)
            {
                // Scene already loaded, use handle
                sceneHandle = handler.gameObject.scene.handle;
                scenePath = null;
            }
            else
            {
                sceneHandle = 0;
                scenePath = lobbyState.Scene.ScenePath;
            }

            SceneLoadData sceneLoadData = scenePath != null
                ? new SceneLoadData(scenePath)
                : new SceneLoadData(sceneHandle);

            sceneLoadData.ReplaceScenes = ReplaceOption.OnlineOnly;
            sceneLoadData.Options.AllowStacking = true;
            sceneLoadData.Options.LocalPhysics = LocalPhysicsMode.None;
            sceneLoadData.Options.AutomaticallyUnload = false;

            sceneLoadData.Params.ServerParams = new object[]
            {
                SceneLoadParam.Lobby, lobbyState.LobbyId, lobbyState.CreatorId
            };

            sceneLoadData.Params.ClientParams = SerializeClientParams(sceneLoadData.Params.ServerParams);

            return sceneLoadData;
        }

        [Server]
        internal static SceneUnloadData CreateUnloadData(ILobbyInfo lmd)
        {
            LobbyManagerInternal manager = LobbyManager.Instance.Internal;
            Assert.IsNotNull(manager);

            LobbyHandler handler = manager.GetLobbyHandler(lmd.LobbyId);
            if (handler == null)
            {
                return null;
            }

            SceneLookupData sld = new()
            {
                Handle = handler.gameObject.scene.handle, Name = lmd.Scene.ScenePath
            };
            object[] unloadParams =
            {
                SceneLoadParam.Lobby, lmd.LobbyId
            };
            SceneUnloadData sud = new(new[]
            {
                sld
            })
            {
                Options =
                {
                    Mode = UnloadOptions.ServerUnloadMode.KeepUnused
                },
                Params =
                {
                    ServerParams = unloadParams, ClientParams = SerializeClientParams(unloadParams)
                }
            };
            return sud;
        }

        internal static byte[] SerializeClientParams(object[] objects)
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
    }
}
