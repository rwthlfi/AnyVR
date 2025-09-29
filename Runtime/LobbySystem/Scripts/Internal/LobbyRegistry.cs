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
        private readonly SyncDictionary<Guid, LobbyState> _lobbies = new();
        internal IReadOnlyDictionary<Guid, LobbyState> Lobbies => _lobbies;
        
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
        internal void Register(LobbyState lobbyState, LobbyHandler handler, byte[] passwordHash = null)
        {
            if (_lobbies.ContainsKey(lobbyState.LobbyId))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyRegistry), $"Lobby {lobbyState.LobbyId} already registered. Skipping.");
                return;
            }

            Assert.IsFalse(_handlers.ContainsKey(lobbyState.LobbyId));
            Assert.IsFalse(_passwordHashes.ContainsKey(lobbyState.LobbyId));

            _lobbies.Add(lobbyState.LobbyId, lobbyState);
            _handlers[lobbyState.LobbyId] = handler;

            if (passwordHash != null)
                _passwordHashes[lobbyState.LobbyId] = passwordHash;

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {lobbyState.LobbyId} registered successfully.");
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

        internal LobbyState GetLobbyMetaData(Guid lobbyId)
        {
            _lobbies.TryGetValue(lobbyId, out LobbyState lobby);
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
