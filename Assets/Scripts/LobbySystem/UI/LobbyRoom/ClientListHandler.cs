using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobbySystem.UI.LobbyRoom
{
    internal class ClientListHandler : MonoBehaviour
    {
        [SerializeField] private ClientCardHandler _clientCardPrefab;

        [SerializeField] private Transform _clientCardParent;

        private readonly Dictionary<int, ClientCardHandler> _clientCards = new();

        public event Action<int, float> ClientVolumeChange;

        public event Action<int> ClientKick;
        
        private (int id, string name)[] _clients;

        private void AddClient(int clientId, string clientName, bool isAdmin = false)
        {
            if (_clientCards.ContainsKey(clientId))
            {
                Debug.LogWarning($"Client with the clientId {clientId} is already registered!");
                return;
            }

            ClientCardHandler card = Instantiate(_clientCardPrefab, _clientCardParent);
            card.SetClient(clientId, clientName, isAdmin);
            card.KickBtn += ClientKick;
            //card.MuteToggle += ClientMuteChange;
            _clientCards.Add(clientId, card);
        }

        private void RemoveClient(int clientId)
        {
            if (_clientCards.Remove(clientId, out ClientCardHandler card))
            {
                Destroy(card.gameObject);
            }
        }

        public void UpdateClientList((int id, string name)[] clientIds, int adminId)
        {
            (int id, string name)[] toAdd = clientIds.Except(_clients).ToArray();
            (int id, string name)[] toRemove = _clients.Except(clientIds).ToArray();
            foreach ((int id, string name) client in toAdd)
            {
                AddClient(client.id, client.name);
            }
            foreach ((int id, string name) client in toRemove)
            {
                RemoveClient(client.id);
            }
        }
    }
}