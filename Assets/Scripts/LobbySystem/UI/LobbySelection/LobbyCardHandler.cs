using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI.LobbySelection
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

        internal event Action JoinBtn;

        private void Start()
        {
            _joinBtn.onClick.AddListener(() =>
            {
                JoinBtn?.Invoke();
            });
        }

        public void SetLobbyMeta(UILobbyMetaData metaData)
        {
            _nameLabel.text = metaData.Name;
            _locationLabel.text = metaData.Location;
            _creatorLabel.text = PlayerNameTracker.GetPlayerName(metaData.Creator);
            _clientCountLabel.text = $"0/{metaData.MaxClients}"; //TODO: handle player count
            _joinLabel.text = "Join"; //TODO: localization
            MetaData = metaData;
        }
    }
}