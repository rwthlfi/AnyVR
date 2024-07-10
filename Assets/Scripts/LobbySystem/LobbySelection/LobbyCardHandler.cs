using TMPro;
using UnityEngine;

namespace LobbySystem.LobbySelection
{
    public class LobbyCardHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _locationLabel;
        [SerializeField] private TextMeshProUGUI _creatorLabel;
        [SerializeField] private TextMeshProUGUI _clientCountLabel;
        [SerializeField] private TextMeshProUGUI _joinLabel;

        private LobbyMetaData _metaData;

        public void OnJoinBtn()
        {
            LobbySelectionSceneHandler.JoinLobby(_metaData);
        }

        public void SetLobbyMeta(LobbyMetaData metaData)
        {
            _nameLabel.text = metaData.Name;
            _locationLabel.text = metaData.Location;
            _creatorLabel.text = PlayerNameTracker.GetPlayerName(metaData.Creator);
            _clientCountLabel.text = $"0/{metaData.MaxClients}"; //TODO: handle player count
            _joinLabel.text = "Join"; //TODO: localization
            _metaData = metaData;
        }
    }
}