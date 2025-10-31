using System;
using System.IO;
using System.Threading.Tasks;
using AnyVR.Logging;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    internal partial class LobbySceneService
    {
        private TaskCompletionSource<LobbyHandler> _loadSceneTcs;

        private void Server_Constructor()
        {
            _internal.SceneManager.OnLoadEnd += Server_TryRegisterLobbyHandler;
        }

        /// <summary>
        ///     Starts a lobby scene for the passed lobby state.
        ///     Asynchronously returns the corresponding LobbyHandler of the started scene or returns null if the scene creation
        ///     failed.
        /// </summary>
        [Server]
        internal async Task<LobbyHandler> StartLobbyScene(GlobalLobbyState globalLobbyState)
        {
            Assert.IsTrue(_internal.ServerManager.Started);

            if (_loadSceneTcs != null)
            {
                // Another scene is already loading.
                // TODO queue scene loading
                return null;
            }

            _loadSceneTcs = new TaskCompletionSource<LobbyHandler>(TaskCreationOptions.RunContinuationsAsynchronously);

            Logger.Log(LogLevel.Verbose, nameof(LobbySceneService), "Loading lobby scene. Waiting for lobby handler");
            _internal.SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), CreateSceneLoadData(globalLobbyState));

            Task delay = Task.Delay(TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_loadSceneTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                Logger.Log(LogLevel.Error, nameof(LobbySceneService), "Timeout while waiting for LobbyHandler");
                _loadSceneTcs = null;
                return null;
            }

            LobbyHandler result = await _loadSceneTcs.Task;

            Assert.IsTrue(_loadSceneTcs.Task.IsCompletedSuccessfully);

            _loadSceneTcs = null;

            return result;
        }

        [Server]
        private void Server_TryRegisterLobbyHandler(SceneLoadEndEventArgs loadArgs)
        {
            if (_loadSceneTcs == null)
            {
                return;
            }

            if (!IsLoadingLobby(loadArgs.QueueData, true))
            {
                return;
            }

            object[] serverParams = loadArgs.QueueData.SceneLoadData.Params.ServerParams;

            Assert.IsTrue(serverParams[1] is Guid);

            Guid lobbyId = (Guid)serverParams[1];

            Assert.IsFalse(_internal.GetLobbyState(lobbyId) != null);

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

            Logger.Log(LogLevel.Verbose, nameof(LobbySceneService), $"Found LobbyHandler with lobbyId '{lobbyId}'");
            _loadSceneTcs.SetResult(lobbyHandler);
        }

        [Server]
        private static SceneLoadData CreateSceneLoadData(GlobalLobbyState state)
        {
            return CreateSceneLoadDataInternal(
                state.Scene.ScenePath,
                state.LobbyId
            );
        }

        [Server]
        private static SceneLoadData CreateSceneLoadData(LobbyHandler handler)
        {
            return CreateSceneLoadDataInternal(
                handler.gameObject.scene.handle,
                handler.GetGameState().LobbyId
            );
        }

        [Server]
        private static SceneLoadData CreateSceneLoadDataInternal(object sceneIdentifier, Guid lobbyId)
        {
            SceneLoadData sld = sceneIdentifier switch
            {
                string path => new SceneLoadData(path),
                int handle => new SceneLoadData(handle),
                _ => throw new ArgumentException("Unsupported scene identifier type", nameof(sceneIdentifier))
            };

            sld.Params.ServerParams = new object[]
            {
                SceneLoadParam.Lobby, lobbyId
            };

            sld.ReplaceScenes = ReplaceOption.OnlineOnly;
            sld.Options.AllowStacking = true;
            sld.Options.LocalPhysics = LocalPhysicsMode.None;
            sld.Options.AutomaticallyUnload = false;
            sld.Params.ClientParams = SerializeClientParams(sld.Params.ServerParams);
            return sld;
        }

        [Server]
        internal static SceneUnloadData CreateUnloadData(LobbyHandler handler, UnloadOptions.ServerUnloadMode unloadMode = UnloadOptions.ServerUnloadMode.KeepUnused)
        {
            Assert.IsNotNull(handler);

            SceneLookupData sld = new()
            {
                Handle = handler.gameObject.scene.handle, Name = handler.GetGameState().LobbyInfo.Scene.ScenePath
            };

            object[] unloadParams =
            {
                SceneLoadParam.Lobby, handler.GetGameState().LobbyId
            };
            SceneUnloadData sud = new(new[]
            {
                sld
            })
            {
                Options =
                {
                    Mode = unloadMode
                },
                Params =
                {
                    ServerParams = unloadParams, ClientParams = SerializeClientParams(unloadParams)
                }
            };
            return sud;
        }

        [Server]
        public void LoadLobbySceneForPlayer(NetworkConnection conn, LobbyHandler lobbyHandler)
        {
            SceneLoadData sld = CreateSceneLoadData(lobbyHandler);
            Assert.IsNotNull(sld);

            _internal.SceneManager.LoadConnectionScenes(conn, sld);
        }

        [Server]
        internal void UnloadLobbySceneForPlayer(NetworkConnection conn, LobbyHandler lobbyHandler)
        {
            SceneUnloadData sud = CreateUnloadData(lobbyHandler);
            Assert.IsNotNull(sud);

            _internal.SceneManager.UnloadConnectionScenes(conn, sud);
        }

        internal static SceneLoadData GlobalSceneLoadData()
        {
            string scene = Path.GetFileNameWithoutExtension(GlobalScene);
            SceneLoadData sld = new(scene)
            {
                Params =
                {
                    ServerParams = new[]
                    {
                        (object)SceneLoadParam.Global
                    },
                    ClientParams = SerializeClientParams(new[]
                    {
                        (object)SceneLoadParam.Global
                    })
                }
            };
            return sld;
        }

        [Server]
        public void UnloadLobby(LobbyHandler lobbyHandler)
        {
            SceneUnloadData sud = CreateUnloadData(lobbyHandler, UnloadOptions.ServerUnloadMode.UnloadUnused);
            Assert.IsNotNull(sud);
            _internal.SceneManager.UnloadConnectionScenes(sud);
        }
    }
}
