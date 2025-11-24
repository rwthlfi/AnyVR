using System;
using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class LobbyUIEntry : MonoBehaviour
    {
        public delegate void JoinEvent(Guid lobbyId);

        [SerializeField] private TextMeshProUGUI _lobbyNameText;

        [SerializeField] private TextMeshProUGUI _lobbySceneNameText;

        [SerializeField] private TextMeshProUGUI _lobbyCreatorText;

        [SerializeField] private TextMeshProUGUI _lobbyCapacityText;

        [SerializeField] private Button _joinBtn;

        private ILobbyInfo _lobbyInfo;

        public event JoinEvent OnJoinButtonPressed;

        private void UpdateCapacityLabel()
        {
            _lobbyCapacityText.text = $"{_lobbyInfo.NumPlayers.Value} / {_lobbyInfo.LobbyCapacity}";
        }

        public void SetLobby(ILobbyInfo lobby)
        {
            _lobbyInfo = lobby;

            _lobbyNameText.text = lobby.Name.Value;
            _lobbySceneNameText.text = lobby.Scene.Name;
            _lobbyCreatorText.text = lobby.Creator != null ? lobby.Creator.Name : "N/A";

            UpdateCapacityLabel();
            lobby.NumPlayers.OnValueChanged += _ => UpdateCapacityLabel();

            _joinBtn.onClick.RemoveAllListeners();
            _joinBtn.onClick.AddListener(() => OnJoinButtonPressed?.Invoke(lobby.LobbyId));
        }
    }
}
