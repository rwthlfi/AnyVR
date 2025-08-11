using TMPro;
using UnityEngine;

namespace AnyVR.Sample
{
    public class LobbyUIEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        
        [SerializeField] private TextMeshProUGUI _lobbySceneNameText;
        
        [SerializeField] private TextMeshProUGUI _lobbyCreatorText;
        
        [SerializeField] private TextMeshProUGUI _lobbyCapacityText;

        public void SetLobby(string lobbyName, string lobbySceneName, int lobbyCreatorId, ushort lobbyLobbyCapacity)
        {
            _lobbyNameText.text = lobbyName;
            _lobbySceneNameText.text = lobbySceneName;
            _lobbyCreatorText.text = lobbyCreatorId.ToString();
            _lobbyCapacityText.text = lobbyLobbyCapacity.ToString();
        }
    }
}
