using System;
using System.Collections.Generic;
using AnyVR.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyRegistry : NetworkBehaviour
    {
        private readonly SyncDictionary<Guid, LobbyInfo> _lobbies = new();
        internal IReadOnlyDictionary<Guid, LobbyInfo> Lobbies => _lobbies;
        
#region ServerOnly
        private readonly Dictionary<Guid, LobbyHandler> _handlers = new();
        private readonly Dictionary<Guid, byte[]> _passwordHashes = new();
#endregion

        internal event Action<Guid> OnLobbyRegistered;
        internal event Action<Guid> OnLobbyUnregistered;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _lobbies.OnChange += (op, key, dto, _) =>
            {
                switch (op)
                {
                    case SyncDictionaryOperation.Add:
                        Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"LobbyRegistered: {key}");
                        OnLobbyRegistered?.Invoke(key);
                        break;

                    case SyncDictionaryOperation.Remove:
                        OnLobbyUnregistered?.Invoke(key);
                        break;
                }
            };
        }

        [Server]
        internal void Register(LobbyInfo lobbyInfo, LobbyHandler handler, byte[] passwordHash = null)
        {
            if (_lobbies.ContainsKey(lobbyInfo.LobbyId))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyRegistry), $"Lobby {lobbyInfo.LobbyId} already registered. Skipping.");
                return;
            }

            Assert.IsFalse(_handlers.ContainsKey(lobbyInfo.LobbyId));
            Assert.IsFalse(_passwordHashes.ContainsKey(lobbyInfo.LobbyId));

            _lobbies.Add(lobbyInfo.LobbyId, lobbyInfo);
            _handlers[lobbyInfo.LobbyId] = handler;

            if (passwordHash != null)
                _passwordHashes[lobbyInfo.LobbyId] = passwordHash;

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {lobbyInfo.LobbyId} registered successfully.");
        }

        [Server]
        internal void Unregister(Guid lobbyId)
        {
            if (!_lobbies.Remove(lobbyId))
                return;

            bool handlerRemoved = _handlers.Remove(lobbyId);
            bool passwordRemoved = _passwordHashes.Remove(lobbyId);

            Assert.IsFalse(!handlerRemoved || !passwordRemoved, "Inconsistent state while unregistering lobby.");

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {lobbyId} unregistered successfully.");
        }

        internal LobbyInfo GetLobbyMetaData(Guid lobbyId)
        {
            _lobbies.TryGetValue(lobbyId, out LobbyInfo lobby);
            return lobby;
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
