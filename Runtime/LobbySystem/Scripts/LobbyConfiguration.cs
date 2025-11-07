using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    [CreateAssetMenu(fileName = "LobbyConfiguration", menuName = "AnyVR/LobbyConfiguration")]
    public class LobbyConfiguration : ScriptableObject
    {
        [SerializeField] [Scene] [Tooltip("The scene to load when leaving a lobby")]
        private string _offlineScene;

        [SerializeField]
        private LobbySceneMetaData[] _lobbyScenes;

        public string OfflineScene => _offlineScene;

        public LobbySceneMetaData[] LobbyScenes => _lobbyScenes;
    }
}
