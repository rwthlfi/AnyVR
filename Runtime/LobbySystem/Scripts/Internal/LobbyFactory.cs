using System;
using FishNet.Component.Ownership;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyFactory
    {
        private int _creatorId;
        private DateTime? _expireDate;
        private bool _isPasswordProtected;
        private ushort _lobbyCapacity = 16;
        private string _name;
        private int _sceneId;

        public LobbyFactory WithName(string name)
        {
            _name = name;
            return this;
        }

        public LobbyFactory WithScene(LobbySceneMetaData scene)
        {
            _sceneId = scene.ID;
            return this;
        }

        public LobbyFactory WithSceneID(int sceneId)
        {
            _sceneId = sceneId;
            return this;
        }

        public LobbyFactory WithCreator(int creatorClientId)
        {
            _creatorId = creatorClientId;
            return this;
        }

        public LobbyFactory WithCapacity(ushort cap)
        {
            _lobbyCapacity = cap;
            return this;
        }

        public LobbyFactory WithPasswordProtection(bool enabled)
        {
            _isPasswordProtected = enabled;
            return this;
        }

        public LobbyFactory WithExpiration(DateTime? expireDate)
        {
            _expireDate = expireDate;
            return this;
        }

        public LobbyInfo Create()
        {
            LobbyManagerInternal @internal = LobbyManager.Instance.Internal;
            LobbyInfo lmd = Object.Instantiate(@internal.LobbyInfoPrefab);
            SceneManager.MoveGameObjectToScene(lmd.gameObject, @internal.gameObject.scene);
            @internal.Spawn(lmd.NetworkObject);
            lmd.Init(_name, _creatorId, (ushort)_sceneId, _lobbyCapacity, _isPasswordProtected);
            // if (_expireDate.HasValue)
            // {
            //     lmd.SetExpiration(_expireDate);
            // }
            return lmd;
        }
    }
}
