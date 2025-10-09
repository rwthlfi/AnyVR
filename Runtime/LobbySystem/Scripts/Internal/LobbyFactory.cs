using System;
using System.Linq;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyFactory
    {
        private static readonly Random Random = new();
        private int _creatorId;
        private DateTime? _expirationDate;
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
            _expirationDate = expireDate;
            return this;
        }

        private static uint GenerateQuickConnectCode()
        {
            Assert.IsNotNull(LobbyManager.Instance);

            const uint maxRetries = 100;
            const int maxCode = 99999;

            for (int i = 0; i < maxRetries; i++)
            {
                uint candidate = (uint)Random.Next(0, maxCode + 1);
                if (LobbyManager.Instance.GetLobbies().All(lobby => lobby.QuickConnectCode != candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Failed to generate unique quick connect code");
        }

        public LobbyState Create()
        {
            LobbyManagerInternal @internal = LobbyManager.Instance.Internal;
            LobbyState lmd = Object.Instantiate(@internal._lobbyStatePrefab);
            SceneManager.MoveGameObjectToScene(lmd.gameObject, @internal.gameObject.scene);
            @internal.Spawn(lmd.NetworkObject);
            lmd.Init(Guid.NewGuid(), GenerateQuickConnectCode(), _name, _creatorId, (ushort)_sceneId, _lobbyCapacity, _isPasswordProtected, _expirationDate);
            return lmd;
        }
    }
}
