using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace AnyVR.LobbySystem.Internal
{
    internal partial class LobbySceneService
    {
        [Client]
        private void Client_Constructor()
        {
            _internal.SceneManager.OnLoadStart += Client_OnLoadStart;
            _internal.SceneManager.OnUnloadEnd += Client_OnUnloadEnd;
        }

        [Client]
        private static void Client_OnLoadStart(SceneLoadStartEventArgs args)
        {
            Assert.IsNotNull(LobbyManager.LobbyConfiguration);
            if (IsLoadingLobby(args.QueueData, false))
            {
                SceneManager.UnloadSceneAsync(LobbyManager.LobbyConfiguration.OfflineScene);
            }
        }

        [Client]
        private static void Client_OnUnloadEnd(SceneUnloadEndEventArgs args)
        {
            if (!IsUnloadingLobby(args.QueueData, false))
            {
                return;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(LobbyManager.LobbyConfiguration.OfflineScene, LoadSceneMode.Additive);
            if (op != null)
            {
                op.completed += _ =>
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneByPath(LobbyManager.LobbyConfiguration.OfflineScene));
                };
            }
        }
    }
}
