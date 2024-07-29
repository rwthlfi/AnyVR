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

        private readonly Dictionary<int, string> _clients = new();

        internal void AddClient(int clientId, string clientName, bool isAdmin = false)
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

        public void RemoveClient(int clientId)
        {
            if (_clientCards.Remove(clientId, out ClientCardHandler card))
            {
                Destroy(card.gameObject);
            }
        }

        public void UpdateClientList((int id, string name)[] clientIds, int adminId)
        {
            Debug.Log($"Updating clients! size: {clientIds.Length}");
            (int id, string name)[] clientArray = _clients.Select(kvp => (kvp.Key, kvp.Value)).ToArray();
            (int id, string name)[] toAdd = clientIds.Except(clientArray).ToArray();
            (int id, string name)[] toRemove = clientArray.Except(clientIds).ToArray();
            foreach ((int id, string name) client in toAdd)
            {
                _clients.Add(client.id, client.name);
                AddClient(client.id, client.name, client.id == adminId);
            }
            foreach ((int id, string name) client in toRemove)
            {
                _clients.Remove(client.id);
                RemoveClient(client.id);
            }
        }
    }
}