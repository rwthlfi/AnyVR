using System;
using System.Collections.Generic;

namespace AnyVR.LobbySystem
{
    internal class QuickConnectHandler
    {
        private const int MaxCode = 99999;

        private readonly Dictionary<Guid, uint> _idToQuickConnect;
        private readonly Dictionary<uint, Guid> _quickConnectToId;
        private readonly Random _random;

        internal QuickConnectHandler()
        {
            _random = new Random();
            _idToQuickConnect = new Dictionary<Guid, uint>();
            _quickConnectToId = new Dictionary<uint, Guid>();
        }

        internal uint RegisterLobby(Guid lobbyId)
        {
            if (_idToQuickConnect.TryGetValue(lobbyId, out uint lobby))
                // already registered
                return lobby;

            uint code = NextUniqueCode();
            _idToQuickConnect[lobbyId] = code;
            _quickConnectToId[code] = lobbyId;
            return code;
        }

        internal void UnregisterLobby(Guid lobbyId)
        {
            if (!_idToQuickConnect.Remove(lobbyId, out uint code))
                return;

            _quickConnectToId.Remove(code);
        }

        internal bool TryGetQuickConnectCode(Guid lobbyId, out uint code)
        {
            return _idToQuickConnect.TryGetValue(lobbyId, out code);
        }

        internal bool TryGetLobbyId(uint code, out Guid lobbyId)
        {
            return _quickConnectToId.TryGetValue(code, out lobbyId);
        }

        private uint NextUniqueCode()
        {
            const uint maxRetries = 100;
            for (int i = 0; i < maxRetries; i++)
            {
                uint candidate = (uint)_random.Next(0, MaxCode + 1);
                if (!_quickConnectToId.ContainsKey(candidate))
                    return candidate;
            }
            throw new InvalidOperationException("Failed to generate unique quick connect code");
        }
    }
}
