using System;
using System.Collections.Generic;
using AnyVR.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyRegistry : NetworkBehaviour
    {
        private readonly Dictionary<Guid, LobbyHandler> _handlers = new();
        private readonly SyncDictionary<Guid, LobbyMetaData> _meta = new();
        private readonly Dictionary<Guid, byte[]> _passwordHashes = new();

        internal event Action<Guid> OnLobbyRegistered;

        internal event Action<Guid> OnLobbyUnregistered;

        internal IReadOnlyDictionary<Guid, LobbyMetaData> Lobbies => _meta;

        public override void OnStartClient()
        {
            base.OnStartClient();
            _meta.OnChange += (op, key, lmd, _) =>
            {
                switch (op)
                {
                    case SyncDictionaryOperation.Add:
                        Debug.Log("LobbyRegistered");
                        Debug.Log(key);
                        Debug.Log(lmd.LobbyId);
                        Debug.Log(lmd.Name);
                        Debug.Log(lmd.Name.Value);
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
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyRegistry), $"Lobby {meta.LobbyId} already registered. Skipping.");
                return;
            }

            Assert.IsFalse(_handlers.ContainsKey(meta.LobbyId));
            Assert.IsFalse(_passwordHashes.ContainsKey(meta.LobbyId));

            _meta[meta.LobbyId] = meta;
            _handlers[meta.LobbyId] = handler;

            if (passwordHash != null)
                _passwordHashes[meta.LobbyId] = passwordHash;

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {meta.LobbyId} registered successfully.");
        }

        [Server]
        internal void Unregister(Guid lobbyId)
        {
            if (!_meta.Remove(lobbyId))
            {
                return;
            }

            bool handlerRemoved = _handlers.Remove(lobbyId);
            bool passwordRemoved = _passwordHashes.Remove(lobbyId);

            Assert.IsFalse(!handlerRemoved || !passwordRemoved, "Inconsistent state while unregistering lobby.");

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {lobbyId} unregistered successfully.");
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
