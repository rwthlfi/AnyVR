using GameKit.Dependencies.Utilities.Types;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnyVr.LobbySystem
{
    [CreateAssetMenu(menuName = "AnyVr/LobbySceneMetaData")]
    public class LobbySceneMetaData : ScriptableObject
    {
        [SerializeField] [Scene] private string scenePath;
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite thumbnail;
        
        public string Name => Path.GetFileNameWithoutExtension(scenePath);
        public string ScenePath => scenePath;
        public string Description => description;
        public Sprite Thumbnail => thumbnail;
    }
}