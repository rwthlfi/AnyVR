using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class LobbyUIEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        
        [SerializeField] private TextMeshProUGUI _lobbySceneNameText;
        
        [SerializeField] private TextMeshProUGUI _lobbyCreatorText;
        
        [SerializeField] private TextMeshProUGUI _lobbyCapacityText;
        
        [SerializeField] private Button _joinBtn;
        
        public delegate void JoinEvent(Guid lobbyId);
        
        public event JoinEvent OnJoinButtonPressed;

        public void SetLobby(Guid id, string lobbyName, string lobbySceneName, int lobbyCreatorId, ushort lobbyLobbyCapacity)
        {
            _lobbyNameText.text = lobbyName;
            _lobbySceneNameText.text = lobbySceneName;
            _lobbyCreatorText.text = lobbyCreatorId.ToString();
            _lobbyCapacityText.text = lobbyLobbyCapacity.ToString();
            
            _joinBtn.onClick.RemoveAllListeners();
            _joinBtn.onClick.AddListener(() => OnJoinButtonPressed?.Invoke(id));
        }
    }
}
