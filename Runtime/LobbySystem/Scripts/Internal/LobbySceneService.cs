using System;
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
            _internal.SceneManager.OnLoadEnd += TryRegisterLobbyHandler;
            _internal.SceneManager.OnUnloadEnd += OnUnloadEnd;
        }

        internal async Task<LobbyHandler> StartConnectionScene(LobbyMetaData lmd)
        {
            Assert.IsTrue(_internal.ServerManager.Started);

            // Starts the lobby scene without clients. When loaded, the LoadEnd callback will be called, and we spawn a LobbyHandler.
            Logger.Log(LogLevel.Verbose, Tag, "Loading lobby scene. Waiting for lobby handler");
            _internal.SceneManager.LoadConnectionScenes(Array.Empty<NetworkConnection>(), lmd.GetSceneLoadData());

            if (_loadSceneTcs != null)
            {
                // Another scene is already loading.
                // TODO queue scene loading
                return null;
            }

            _loadSceneTcs = new TaskCompletionSource<LobbyHandler>(TaskCreationOptions.RunContinuationsAsynchronously);

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
            if (!IsLoadingLobby(loadArgs.QueueData, true, out string errorMsg))
            {
                if (!string.IsNullOrEmpty(errorMsg))
                {
                    Logger.Log(LogLevel.Warning, Tag, $"Can't register LobbyHandler. {errorMsg}");
                }

                return;
            }

            Assert.IsNotNull(_loadSceneTcs);

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

            // TODO:
            // lobbyHandler.Init(lobbyId, quickConnectCode);
            // lobbyHandler.OnPlayerJoin += _ =>
            // {
            //     int currentPlayerCount = _lobbyHandlers[lobbyId].GetPlayerStates().Count();
            //     OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            // };
            // lobbyHandler.OnPlayerLeave += _ =>
            // {
            //     int currentPlayerCount = _lobbyHandlers[lobbyId].GetPlayerStates().Count();
            //     OnLobbyPlayerCountUpdate(lobbyId, (ushort)currentPlayerCount);
            // };

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

            AsyncOperation op = USceneManager.LoadSceneAsync(_internal.LobbyConfiguration.OfflineScene, LoadSceneMode.Additive);
            if (op != null)
            {
                op.completed += _ =>
                {
                    USceneManager.SetActiveScene(USceneManager.GetSceneByPath(_internal.LobbyConfiguration.OfflineScene));
                };
            }
        }

        private static bool IsUnloadingLobby(UnloadQueueData queueData, bool asServer)
        {
            object[] loadParams = asServer
                ? queueData.SceneUnloadData.Params.ServerParams
                : LobbyMetaData.DeserializeClientParams(queueData.SceneUnloadData.Params.ClientParams);

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
                : LobbyMetaData.DeserializeClientParams(queueData.SceneLoadData.Params.ClientParams);

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

        internal static SceneUnloadData CreateUnloadData(LobbyMetaData lmd)
        {
            if (lmd.SceneHandle == null)
            {
                return null;
            }

            SceneLookupData sld = new()
            {
                Handle = lmd.SceneHandle.Value, Name = lmd.ScenePath
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
                    ServerParams = unloadParams, ClientParams = LobbyMetaData.SerializeObjects(unloadParams)
                }
            };
            return sud;
        }
    }
}
