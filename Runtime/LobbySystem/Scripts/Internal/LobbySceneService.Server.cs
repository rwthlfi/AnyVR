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
        private TaskCompletionSource<LobbyGameMode> _loadSceneTcs;

        private void Server_Constructor()
        {
            _internal.SceneManager.OnLoadEnd += Server_TryRegisterLobbyGameMode;
        }

        /// <summary>
        ///     Starts a lobby scene for the passed lobby state.
        ///     Asynchronously returns the corresponding LobbyGameMode of the started scene or returns null if the scene creation
        ///     failed.
        /// </summary>
        [Server]
        internal async Task<LobbyGameMode> StartLobbyScene(GlobalLobbyState gls)
        {
            Assert.IsTrue(_internal.ServerManager.Started);

            if (_loadSceneTcs != null)
            {
                // Another scene is already loading.
                // TODO queue scene loading
                return null;
            }

            _loadSceneTcs = new TaskCompletionSource<LobbyGameMode>(TaskCreationOptions.RunContinuationsAsynchronously);

            Logger.Log(LogLevel.Verbose, nameof(LobbySceneService), "Loading lobby scene");
            _internal.SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), CreateSceneLoadData(gls));

            Task delay = Task.Delay(TimeSpan.FromSeconds(10));
            Task completed = await Task.WhenAny(_loadSceneTcs.Task, delay);

            if (ReferenceEquals(completed, delay))
            {
                Logger.Log(LogLevel.Error, nameof(LobbySceneService), "Timeout while waiting for the LobbyGameMode instance.");
                _loadSceneTcs = null;
                return null;
            }

            LobbyGameMode result = await _loadSceneTcs.Task;

            Assert.IsTrue(_loadSceneTcs.Task.IsCompletedSuccessfully);

            _loadSceneTcs = null;

            return result;
        }

        [Server]
        private void Server_TryRegisterLobbyGameMode(SceneLoadEndEventArgs loadArgs)
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

            Assert.IsFalse(GlobalGameState.Instance.GetLobbyInfo(lobbyId) != null);

            GameObject[] rootObjects = loadArgs.LoadedScenes[0].GetRootGameObjects();

            LobbyGameMode lobbyGameMode = null;
            foreach (GameObject root in rootObjects)
            {
                LobbyGameMode comp = root.GetComponent<LobbyGameMode>();
                if (comp == null)
                    continue;

                lobbyGameMode = comp;
                break;
            }

            Assert.IsNotNull(lobbyGameMode);

            Logger.Log(LogLevel.Verbose, nameof(LobbySceneService), $"Found LobbyGameMode with lobbyId '{lobbyId}'");
            _loadSceneTcs.SetResult(lobbyGameMode);
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
        private static SceneLoadData CreateSceneLoadData(LobbyGameMode gameMode)
        {
            return CreateSceneLoadDataInternal(
                gameMode.gameObject.scene.handle,
                gameMode.GetGameState<LobbyState>().LobbyId
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
        internal static SceneUnloadData CreateUnloadData(LobbyGameMode gameMode, UnloadOptions.ServerUnloadMode unloadMode = UnloadOptions.ServerUnloadMode.KeepUnused)
        {
            Assert.IsNotNull(gameMode);

            SceneLookupData sld = new()
            {
                Handle = gameMode.gameObject.scene.handle, Name = gameMode.GetGameState<LobbyState>().LobbyInfo.Scene.ScenePath
            };

            object[] unloadParams =
            {
                SceneLoadParam.Lobby, gameMode.GetGameState<LobbyState>().LobbyId
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
        public void LoadLobbySceneForPlayer(NetworkConnection conn, LobbyGameMode lobbyGameMode)
        {
            SceneLoadData sld = CreateSceneLoadData(lobbyGameMode);
            Assert.IsNotNull(sld);

            _internal.SceneManager.LoadConnectionScenes(conn, sld);
        }

        [Server]
        internal void UnloadLobbySceneForPlayer(NetworkConnection conn, LobbyGameMode lobbyGameMode)
        {
            SceneUnloadData sud = CreateUnloadData(lobbyGameMode);
            Assert.IsNotNull(sud);

            _internal.SceneManager.UnloadConnectionScenes(conn, sud);
        }

        internal static SceneLoadData GlobalSceneLoadData(string globalScenePath)
        {
            string scene = Path.GetFileNameWithoutExtension(globalScenePath);
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
        public void UnloadLobby(LobbyGameMode lobbyGameMode)
        {
            SceneUnloadData sud = CreateUnloadData(lobbyGameMode, UnloadOptions.ServerUnloadMode.UnloadUnused);
            Assert.IsNotNull(sud);
            _internal.SceneManager.UnloadConnectionScenes(sud);
        }
    }
}
