using GameKit.Dependencies.Utilities.Types;
using System;
using System.IO;
using UnityEngine;

namespace AnyVR.LobbySystem
{
    [CreateAssetMenu(menuName = "AnyVr/LobbySceneMetaData")]
    public class LobbySceneMetaData : ScriptableObject
    {
        [SerializeField] 
        [Scene] 
        private string _scenePath;

        [SerializeField]
        private string _sceneName;

        [SerializeField] 
        [TextArea]
        private string _description;
        
        [SerializeField] 
        private Sprite _thumbnail;

        [SerializeField]
        private MinMaxRange _recommendedUsers;

        public string Name => _sceneName;
        public string ScenePath => _scenePath;
        public string Description => _description;
        public Sprite Thumbnail => _thumbnail;
        public int MinUsers => _recommendedUsers.min;
        public int MaxUsers => _recommendedUsers.max;

        [Serializable]
        public class MinMaxRange
        {
            public int min;
            public int max;
        }
    }
}