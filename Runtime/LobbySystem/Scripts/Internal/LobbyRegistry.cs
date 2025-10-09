using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AnyVR.Logging;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyRegistry : NetworkBehaviour
    {
        private readonly SyncDictionary<Guid, LobbyState> _lobbyStates = new();
        
        private QuickConnectHandler _quickConnectHandler;
        
        internal event Action<Guid> OnLobbyRegistered;
        
        internal event Action<Guid> OnLobbyUnregistered;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();

            _lobbyStates.OnChange += (op, key, _, _) =>
            {
                switch (op)
                {
                    case SyncDictionaryOperation.Add:
                        Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"LobbyRegistered: {key}");
                        OnLobbyRegistered?.Invoke(key);
                        break;

                    case SyncDictionaryOperation.Remove:
                        Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"LobbyUnregistered: {key}");
                        OnLobbyUnregistered?.Invoke(key);
                        break;
                }
            };
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _quickConnectHandler = new QuickConnectHandler();
        }

        [Server]
        internal bool Register(LobbyState lobbyState, LobbyHandler handler, string password = null)
        {
            if (_lobbyStates.ContainsKey(lobbyState.LobbyId))
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyRegistry), $"Lobby {lobbyState.LobbyId} already registered. Skipping.");
                return false;
            }

            Assert.IsFalse(_handlers.ContainsKey(lobbyState.LobbyId));
            Assert.IsFalse(_passwordHashes.ContainsKey(lobbyState.LobbyId));

            _lobbyStates.Add(lobbyState.LobbyId, lobbyState);
            _handlers[lobbyState.LobbyId] = handler;

            if (!string.IsNullOrWhiteSpace(password))
            {
                _passwordHashes[lobbyState.LobbyId] = ComputeSha256(password);
            }

            bool success = _quickConnectHandler.RegisterLobby(lobbyState);
            Assert.IsTrue(success);

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {lobbyState.LobbyId} registered successfully.");
            return true;
        }

        [Server]
        internal void Unregister(LobbyState lobby)
        {
            if (!_lobbyStates.Remove(lobby.LobbyId))
                return;

            bool handlerRemoved = _handlers.Remove(lobby.LobbyId);
            Assert.IsTrue(handlerRemoved);
            
            bool success = _quickConnectHandler.UnregisterLobby(lobby);
            Assert.IsTrue(success);
            
            _passwordHashes.Remove(lobby.LobbyId);

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {lobby.LobbyId} unregistered successfully.");
        }

        public bool ValidatePassword(Guid lobbyId, string password)
        {
            return !_passwordHashes.TryGetValue(lobbyId, out byte[] hash) || ComputeSha256(password).SequenceEqual(hash);
        }

        private static byte[] ComputeSha256(string s)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

        internal LobbyState GetLobbyState(Guid lobbyId)
        {
            return _lobbyStates.GetValueOrDefault(lobbyId);
        }

        [CanBeNull]
        public LobbyState GetLobbyState(uint quickConnect)
        {
            return _quickConnectHandler.GetLobbyState(quickConnect);
        }

        internal byte[] GetPasswordHash(Guid lobbyId)
        {
            return _passwordHashes.GetValueOrDefault(lobbyId);
        }

        internal LobbyHandler GetLobbyHandler(Guid lobbyId)
        {
            return _handlers.GetValueOrDefault(lobbyId);
        }

        public IEnumerable<LobbyState> GetLobbyStates()
        {
            return _lobbyStates.Values;
        }

#region ServerOnly

        private readonly Dictionary<Guid, LobbyHandler> _handlers = new();

        private readonly Dictionary<Guid, byte[]> _passwordHashes = new();

#endregion
    }
}
