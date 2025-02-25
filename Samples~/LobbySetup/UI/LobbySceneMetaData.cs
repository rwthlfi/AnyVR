using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

namespace AnyVr.Samples.LobbySetup
{
    [CreateAssetMenu(menuName = "LobbySystem/LobbySceneMetaData")]
    public class LobbySceneMetaData : ScriptableObject
    {
        [SerializeField] [Scene] private string _scene;
        [SerializeField] [TextArea] private string _description;
        [SerializeField] private Sprite _thumbnail;
        public string Scene => _scene;

        public string Description => _description;
        public Sprite Thumbnail => _thumbnail;
    }
}