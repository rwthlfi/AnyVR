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

        public event JoinEvent OnJoinButtonPressed;

        public void SetLobby(Guid id, string lobbyName, string lobbySceneName, int lobbyCreatorId, ushort lobbyLobbyCapacity)
        {
            _lobbyNameText.text = lobbyName;
            _lobbySceneNameText.text = lobbySceneName;
            _lobbyCreatorText.text = lobbyCreatorId.ToString();

            GameState gameState = FindAnyObjectByType<GameState>();
            PlayerState playerState = gameState.GetPlayerState(lobbyCreatorId);

            _lobbyCapacityText.text = playerState != null ? playerState.GetName() : $"Client_{lobbyCreatorId.ToString()}";

            _joinBtn.onClick.RemoveAllListeners();
            _joinBtn.onClick.AddListener(() => OnJoinButtonPressed?.Invoke(id));
        }
    }
}
