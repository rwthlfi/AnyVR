using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyRegistry : NetworkBehaviour
    {
        private readonly Dictionary<Guid, LobbyHandler> _handlers = new();
        private readonly SyncDictionary<Guid, LobbyMetaData> _meta = new();
        private readonly Dictionary<Guid, byte[]> _passwordHashes = new();

        internal Action<Guid> OnLobbyRegistered;

        internal Action<Guid> OnLobbyUnregistered;

        internal IReadOnlyDictionary<Guid, LobbyMetaData> Lobbies => _meta;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _meta.OnChange += (op, key, _, _) =>
            {
                switch (op)
                {
                    case SyncDictionaryOperation.Add:
                        OnLobbyRegistered?.Invoke(key);
                        break;
                    case SyncDictionaryOperation.Remove:
                        OnLobbyUnregistered?.Invoke(key);
                        break;
                }
            };
        }

        [Server]
        internal void Register(LobbyMetaData meta, LobbyHandler handler, byte[] passwordHash = null)
        {
            if (_meta.ContainsKey(meta.LobbyId))
                return;

            Assert.IsFalse(_handlers.ContainsKey(meta.LobbyId));
            Assert.IsFalse(_passwordHashes.ContainsKey(meta.LobbyId));

            _meta.Add(meta.LobbyId, meta);
            _handlers.Add(meta.LobbyId, handler);

            if (passwordHash != null)
            {
                _passwordHashes.Add(meta.LobbyId, passwordHash);
            }
        }

        [Server]
        internal void Unregister(Guid lobbyId)
        {
            _meta.Remove(lobbyId);
            _handlers.Remove(lobbyId);
            _passwordHashes.Remove(lobbyId);
        }

        internal LobbyMetaData GetLobbyMetaData(Guid lobbyId)
        {
            return _meta[lobbyId];
        }

        internal byte[] GetPasswordHash(Guid lobbyId)
        {
            return _passwordHashes.GetValueOrDefault(lobbyId);
        }

        internal LobbyHandler GetLobbyHandler(Guid lobbyId)
        {
            return _handlers.GetValueOrDefault(lobbyId);
        }
    }
}
