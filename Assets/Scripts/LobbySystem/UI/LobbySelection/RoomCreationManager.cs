using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LobbySystem.UI.LobbySelection
{
    public class RoomCreationManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Slider _userLimitSlider;

        [SerializeField] private Toggle _usePinToggle;
        [SerializeField] private TMP_InputField _pinInputField;
        [SerializeField] private Button _randomizePinBtn, _copyPinBtn;

        [SerializeField] private Button _openRoomBtn;

        [Header("Prefab Setup")]
        [SerializeField] private LocationCardHandler _locationCardPrefab;
        [SerializeField] private Transform _locationCardParent;

        [Header("Available Locations Setup")]
        [SerializeField] private LobbySceneMetaData[] _lobbySceneMetaData;

        private string _selectedRoom;
        
        private const ushort k_maxUserLimit = 99;

        private void Start()
        {
            foreach (LobbySceneMetaData metaData in _lobbySceneMetaData)
            {
                LocationCardHandler card = Instantiate(_locationCardPrefab, _locationCardParent);
                card.SetMetaData(metaData);
                card.Click += () =>
                {
                    _selectedRoom = card.MetaData.Scene;
                    Debug.Log(_selectedRoom);
                    UpdateUI();
                };
            }
            
            _pinInputField.characterLimit = 8;
            _randomizePinBtn.onClick.AddListener(() =>
            {
                _pinInputField.text = (Random.Range(0, ushort.MaxValue) % 9999).ToString();
            });
            _nameInputField.onValueChanged.AddListener(_ => UpdateUI());
            _pinInputField.onValueChanged.AddListener(_ => UpdateUI());
            _usePinToggle.onValueChanged.AddListener(_ =>
            {
                UpdateUI();
            });
            _userLimitSlider.maxValue = k_maxUserLimit;
            _copyPinBtn.onClick.AddListener(() =>
            {
                if (!string.IsNullOrWhiteSpace(_pinInputField.text))
                {
                    GUIUtility.systemCopyBuffer = _pinInputField.text;
                }
            });
            UpdateUI();
        }

        private bool CanOpenRoom()
        {
            bool intractable = true;
            intractable &= !string.IsNullOrEmpty(_nameInputField.text);
            intractable &= !string.IsNullOrEmpty(_selectedRoom);
            intractable &= !_usePinToggle.isOn || _pinInputField.text.Length >= 4;
            return intractable;
        }

        private void UpdateUI()
        {
            _pinInputField.interactable = _usePinToggle.isOn;
            _randomizePinBtn.interactable = _usePinToggle.isOn;
            _copyPinBtn.interactable = _usePinToggle.isOn;
            _openRoomBtn.interactable = CanOpenRoom();
        }


        public void OnBackBtn()
        {
            LobbySelectionMenuHandler.s_instance.CloseCreateRoomScene();
        }

        public void OnOpenRoomBtn()
        {
            if (string.IsNullOrEmpty(_selectedRoom))
            {
                return;
            }

            const string pattern = @"([^/]+)\.unity$";
            Match match = Regex.Match(_selectedRoom, pattern);

            if (!match.Success)
            {
                Debug.LogError("Error parsing location name");
                return;
            }

            string lobbyName = _nameInputField.text;
            string location = match.Groups[1].Value;
            ushort userLimit = (ushort)Math.Clamp(Convert.ToInt32(_userLimitSlider.value), 1, k_maxUserLimit);
             
            UILobbyMetaData uiLobbyMeta = new(lobbyName, -1, location, userLimit);
            LobbySelectionMenuHandler.s_instance.CloseCreateRoomScene(uiLobbyMeta);
        }
    }
}