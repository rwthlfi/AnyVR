using AnyVr.LobbySystem;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVr.Samples.LobbySetup
{
    public class LobbyCardHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _locationLabel;
        [SerializeField] private TextMeshProUGUI _creatorLabel;
        [SerializeField] private TextMeshProUGUI _clientCountLabel;
        [SerializeField] private TextMeshProUGUI _joinLabel;
        [SerializeField] private Button _joinBtn;

        internal UILobbyMetaData MetaData { get; private set; }

        private void Start()
        {
            _joinBtn.onClick.AddListener(() =>
            {
                JoinBtn?.Invoke();
            });
        }

        internal event Action JoinBtn;

        public void SetLobbyMeta(UILobbyMetaData metaData)
        {
            _nameLabel.text = metaData.Name;
            _locationLabel.text = metaData.Location;
            _creatorLabel.text = PlayerNameTracker.GetPlayerName(metaData.Creator);
            _clientCountLabel.text = $"0/{metaData.MaxClients}";
            _joinLabel.text = "Join"; //TODO: localization
            MetaData = metaData;
        }

        public void SetCurrentPlayerCount(int playerCount)
        {
            _clientCountLabel.text = $"{playerCount}/{MetaData.MaxClients}";
        }
    }
}