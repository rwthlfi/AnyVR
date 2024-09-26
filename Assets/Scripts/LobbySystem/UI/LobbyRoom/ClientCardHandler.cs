using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI.LobbyRoom
{
    internal class ClientCardHandler : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerNameLabel;

        [SerializeField] private Toggle _muteToggle;
        [SerializeField] private Button _kickButton;

        private int _id;

        private void OnDestroy()
        {
            KickBtn = null;
            MuteToggle = null;
        }

        internal event Action<int> KickBtn;
        internal event Action<int, bool> MuteToggle;

        public void SetClient(int clientId, string clientName, bool isAdmin = false)
        {
            _id = clientId;
            _playerNameLabel.text = clientName;
            if (isAdmin)
            {
                _playerNameLabel.text += " (Admin)";
            }

            _muteToggle.SetIsOnWithoutNotify(false);
            _muteToggle.onValueChanged.AddListener(b =>
            {
                MuteToggle?.Invoke(_id, b);
            });
            _kickButton.interactable = UIHandler.s_instance.IsLocalClientAdmin();
        }

        public void KickBtnCallback()
        {
            KickBtn?.Invoke(_id);
        }
    }
}