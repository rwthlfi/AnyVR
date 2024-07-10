using System;
using System.Collections.Generic;
using UnityEngine;

namespace LobbySystem.UI
{
    internal class ClientListHandler : MonoBehaviour
    {
        [SerializeField] private ClientCardHandler _clientCardPrefab;

        [SerializeField] private Transform _clientCardParent;

        private readonly Dictionary<int, ClientCardHandler> _clientCards = new();

        public event Action<int, bool> ClientMuteChange;

        public event Action<int> ClientKick;

        public void AddClient(int clientId, string clientName, bool isAdmin = false)
        {
            if (_clientCards.ContainsKey(clientId))
            {
                Debug.LogWarning($"Client with the clientId {clientId} is already registered!");
                return;
            }

            ClientCardHandler card = Instantiate(_clientCardPrefab, _clientCardParent);
            card.SetClient(clientId, clientName, isAdmin);
            card.KickBtn += ClientKick;
            card.MuteToggle += ClientMuteChange;
            _clientCards.Add(clientId, card);
        }

        public void RemoveClient(int clientId)
        {
            if (_clientCards.Remove(clientId, out ClientCardHandler card))
            {
                Destroy(card.gameObject);
            }
        }

        public void UpdateClientList(IEnumerable<int> clientIds, int adminId)
        {
            foreach (ClientCardHandler card in _clientCards.Values)
            {
                Destroy(card.gameObject);
            }

            _clientCards.Clear();
            foreach (int id in clientIds)
            {
                AddClient(id, PlayerNameTracker.GetPlayerName(id), adminId == id);
            }
        }
    }
}