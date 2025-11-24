using AnyVR.LobbySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class UIUserListEntry : MonoBehaviour
    {
        public delegate void PlayerEvent(LobbyPlayerState player);

        [SerializeField] private Image _adminIcon;
        [SerializeField] private TextMeshProUGUI _userNameText;
        [SerializeField] private Button _promoteButton;
        [SerializeField] private Button _kickButton;

        private void OnDestroy()
        {
            OnPromoteToAdminButtonPressed = null;
            OnKickButtonPressed = null;
        }

        public event PlayerEvent OnPromoteToAdminButtonPressed;
        public event PlayerEvent OnKickButtonPressed;

        public void SetPlayerInfo(LobbyPlayerState playerState)
        {
            _userNameText.text = playerState.Global.Name;
            _adminIcon.enabled = playerState.IsAdmin;

            bool isLocalPlayer = playerState.IsLocalPlayer;
            _promoteButton.interactable = !isLocalPlayer;
            _kickButton.interactable = !isLocalPlayer;

            _promoteButton.onClick.AddListener(() => OnPromoteToAdminButtonPressed?.Invoke(playerState));
            _kickButton.onClick.AddListener(() => OnKickButtonPressed?.Invoke(playerState));
        }
    }
}
