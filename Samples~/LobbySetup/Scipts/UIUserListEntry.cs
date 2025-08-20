using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVR.Sample.LobbySetup
{
    public class UIUserListEntry : MonoBehaviour
    {
        [SerializeField] private Image _adminIcon;
        [SerializeField] private TextMeshProUGUI _userNameText;
        [SerializeField] private Button _promoteButton;
        [SerializeField] private Button _kickButton;
        
        public delegate void PlayerEvent(LobbyPlayerState player);
        
        public event PlayerEvent OnPromoteToAdminButtonPressed;
        public event PlayerEvent OnKickButtonPressed;

        public void SetPlayerInfo(LobbyPlayerState playerState)
        {
            _userNameText.text = playerState.GetName();
            _adminIcon.enabled = playerState.GetIsAdmin();
            
            _promoteButton.onClick.AddListener(() => OnPromoteToAdminButtonPressed?.Invoke(playerState));
            _kickButton.onClick.AddListener(() => OnKickButtonPressed?.Invoke(playerState));
        }

        private void OnDestroy()
        {
            OnPromoteToAdminButtonPressed = null;
            OnKickButtonPressed = null;
        }
    }
}
