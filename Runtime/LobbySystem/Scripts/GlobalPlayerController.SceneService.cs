using AnyVR.LobbySystem.Internal;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using AsyncOperation = UnityEngine.AsyncOperation;

namespace AnyVR.LobbySystem
{
    public partial class GlobalPlayerController
    {
        [Client]
        private static void Client_OnLoadEnd(SceneLoadEndEventArgs obj)
        {
            USceneManager.SetActiveScene(obj.LoadedScenes[0]);
        }

        [Client]
        private static void Client_OnLoadStart(SceneLoadStartEventArgs args)
        {
            if (!LobbySceneService.IsLoadingLobby(args.QueueData, false))
                return;
            
            Assert.IsNotNull(GlobalGameState.Instance.LobbyConfiguration);
            USceneManager.UnloadSceneAsync(GlobalGameState.Instance.LobbyConfiguration.OfflineScene);
        }

        [Client]
        private static void Client_OnUnloadEnd(SceneUnloadEndEventArgs args)
        {
            if (!LobbySceneService.IsUnloadingLobby(args.QueueData, false))
            {
                return;
            }

            AsyncOperation op = USceneManager.LoadSceneAsync(GlobalGameState.Instance.LobbyConfiguration.OfflineScene, LoadSceneMode.Additive);
            if (op != null)
            {
                op.completed += _ =>
                {
                    USceneManager.SetActiveScene(USceneManager.GetSceneByPath(GlobalGameState.Instance.LobbyConfiguration.OfflineScene));
                };
            }
        }
    }
}
