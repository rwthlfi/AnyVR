using System;
using System.Linq;
using System.Runtime.CompilerServices;
using GameKit.Dependencies.Utilities.Types;
using UnityEngine;

[assembly: InternalsVisibleTo("AnyVR.Tests.Runtime")]

namespace AnyVR.LobbySystem
{
    [CreateAssetMenu(fileName = "LobbySceneMetaData", menuName = "AnyVR/Lobby Scene MetaData")]
    public class LobbySceneMetaData : ScriptableObject
    {
        [SerializeField]
        [Scene]
        internal string _scenePath;

        [SerializeField]
        internal string _sceneName;

        [SerializeField]
        [TextArea]
        internal string _description;

        [SerializeField]
        internal Sprite _thumbnail;

        [SerializeField]
        internal MinMaxRange _recommendedUsers;

        public int ID
        {
            get
            {
                return Array.FindIndex(GlobalGameState.Instance.LobbyConfiguration.LobbyScenes, lmd => lmd == this);
            }
        }

        public string Name => _sceneName;
        public string ScenePath => _scenePath;
        public string Description => _description;
        public Sprite Thumbnail => _thumbnail;
        public ushort MinUsers => _recommendedUsers._min;
        public ushort MaxUsers => _recommendedUsers._max;

        public bool IsValid()
        {
            return GlobalGameState.Instance.LobbyConfiguration.LobbyScenes.Contains(this);
        }

        [Serializable]
        internal class MinMaxRange
        {
            public ushort _min;
            public ushort _max;

            public MinMaxRange(ushort min, ushort max)
            {
                _min = min;
                _max = max;
            }
        }
    }
}
