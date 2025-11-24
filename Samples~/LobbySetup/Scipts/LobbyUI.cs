using System.Linq;
using AnyVR.LobbySystem;
using AnyVR.Voicechat;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace AnyVR.Sample
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField]
        private UISessionPanel _uiSessionPanel;

        [SerializeField]
        private Button _leaveButton;

        [SerializeField]
        private Button _publishMicButton, _muteButton;

        [SerializeField]
        private TMP_Dropdown _microphoneDropdown;

        private bool _isMuted;

        private static LocalParticipant Voice => LobbyPlayerController.Instance?.Voice?.LocalParticipant;

        private void Start()
        {
            Assert.IsNotNull(_uiSessionPanel);
            _uiSessionPanel.gameObject.SetActive(false);
    #if UNITY_WEBGL
            _microphoneDropdown.gameObject.SetActive(false);
    #else
            _microphoneDropdown.options = Microphone.devices.Select(mic => new TMP_Dropdown.OptionData(mic)).ToList();
            _microphoneDropdown.onValueChanged.AddListener(_ => ChangeMicrophone());
    #endif
            _publishMicButton.GetComponentInChildren<TextMeshProUGUI>().text = "Publish Mic";
            _publishMicButton.onClick.AddListener(ToggleMicPublished);
            _muteButton.onClick.AddListener(ToggleMicMuted);
            _muteButton.GetComponentInChildren<TextMeshProUGUI>().text = "Mute Mic";
            _leaveButton.onClick.AddListener(() =>
            {
                LobbyPlayerController.Instance.LeaveLobby();
            });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _uiSessionPanel.gameObject.SetActive(!_uiSessionPanel.gameObject.activeSelf);
            }
        }

        private void FixedUpdate()
        {
            if (Voice == null)
                return;

            _publishMicButton.GetComponentInChildren<TextMeshProUGUI>().text = Voice.IsMicPublished ? "Unpublish Mic" : "Publish Mic";
            _muteButton.GetComponentInChildren<TextMeshProUGUI>().text = _isMuted ? "Unmute" : "Mute";
        }

        private void ToggleMicPublished()
        {
            if (Voice.IsMicPublished)
            {
                Voice.UnpublishMicrophone();
            }
            else
            {
                string selectedMicrophone = _microphoneDropdown.options[_microphoneDropdown.value].text;
                Voice.PublishMicrophone(selectedMicrophone);
            }
        }

        private void ToggleMicMuted()
        {
            _isMuted = !_isMuted;
            Voice.SetMute(_isMuted);
        }

        private void ChangeMicrophone()
        {
            if (!Voice.IsMicPublished)
                return;

            string selectedMicrophone = _microphoneDropdown.options[_microphoneDropdown.value].text;
            Voice.PublishMicrophone(selectedMicrophone);
        }
    }
}
