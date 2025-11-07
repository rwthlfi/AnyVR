using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AnyVR.Logging;
using FishNet.Object;
using UnityEngine.Assertions;
using Logger = AnyVR.Logging.Logger;

namespace AnyVR.LobbySystem.Internal
{
    internal class LobbyRegistry : NetworkBehaviour
    {
#region Lifecycle Overrides

        public override void OnStartServer()
        {
            base.OnStartServer();
            _quickConnectHandler = new QuickConnectHandler();
            _lobbyGameModes = new Dictionary<Guid, LobbyGameMode>();
            _passwordHashes = new Dictionary<Guid, byte[]>();
            _globalGameState = GlobalGameState.Instance;
        }

#endregion

#region Lobby Accessors

        [Server]
        internal LobbyGameMode GetLobbyGameMode(Guid lobbyId)
        {
            return _lobbyGameModes.GetValueOrDefault(lobbyId);
        }

#endregion

#region Server Only Properties

        private QuickConnectHandler _quickConnectHandler;

        private Dictionary<Guid, LobbyGameMode> _lobbyGameModes;

        private Dictionary<Guid, byte[]> _passwordHashes;

        private GlobalGameState _globalGameState;

#endregion

#region Server Methods

        [Server]
        internal bool RegisterLobby(GlobalLobbyState globalLobbyState, LobbyGameMode gameMode, string password = null)
        {
            Assert.IsNotNull(globalLobbyState);
            Assert.IsNotNull(gameMode);

            if (GlobalGameState.Instance.GetLobbyInfo(globalLobbyState.LobbyId) != null)
            {
                Logger.Log(LogLevel.Warning, nameof(LobbyRegistry), $"Lobby {globalLobbyState.LobbyId} already registered. Skipping.");
                return false;
            }

            Assert.IsFalse(_lobbyGameModes.ContainsKey(globalLobbyState.LobbyId));
            Assert.IsFalse(_passwordHashes.ContainsKey(globalLobbyState.LobbyId));

            _lobbyGameModes[globalLobbyState.LobbyId] = gameMode;
            _globalGameState.AddGlobalLobbyState(globalLobbyState);

            if (!string.IsNullOrWhiteSpace(password))
            {
                _passwordHashes[globalLobbyState.LobbyId] = ComputeSha256(password);
            }

            bool success = _quickConnectHandler.RegisterLobby(globalLobbyState);
            Assert.IsTrue(success);

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {globalLobbyState.LobbyId} registered successfully.");
            return true;
        }

        [Server]
        internal void UnregisterLobby(GlobalLobbyState globalLobby)
        {
            _globalGameState.RemoveGlobalLobbyState(globalLobby.LobbyId);

            bool handlerRemoved = _lobbyGameModes.Remove(globalLobby.LobbyId);
            Assert.IsTrue(handlerRemoved);

            bool success = _quickConnectHandler.UnregisterLobby(globalLobby);
            Assert.IsTrue(success);

            _passwordHashes.Remove(globalLobby.LobbyId);

            Logger.Log(LogLevel.Verbose, nameof(LobbyRegistry), $"Lobby {globalLobby.LobbyId} unregistered successfully.");
        }

        [Server]
        internal bool ValidatePassword(Guid lobbyId, string password)
        {
            return !_passwordHashes.TryGetValue(lobbyId, out byte[] hash) || ComputeSha256(password).SequenceEqual(hash);
        }

        private static byte[] ComputeSha256(string s)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        }

#endregion
    }
}
