using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    [CreateAssetMenu(fileName = "LobbyConfiguration", menuName = "AnyVR/LobbyConfiguration")]
    public class LobbyConfiguration : ScriptableObject
    {
        [Tooltip("The scene to load when leaving a lobby")]
        [Scene]
        public string OfflineScene;

        public LobbySceneMetaData[] LobbyScenes;
    }
}
