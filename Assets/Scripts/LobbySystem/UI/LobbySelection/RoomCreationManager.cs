using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI.LobbySelection
{
    public class RoomCreationManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private TMP_InputField _userLimitInputField;

        [SerializeField] private Toggle _usePasswordToggle;
        [SerializeField] private TMP_InputField _passwordInputField;

        [Header("Prefab Setup")]
        [SerializeField] private LocationCardHandler _locationCardPrefab;
        [SerializeField] private Transform _locationCardParent;

        [Header("Available Locations Setup")]
        [SerializeField] private LobbySceneMetaData[] _lobbySceneMetaData;

        private string _selectedRoom;

        private void Start()
        {
            foreach (LobbySceneMetaData metaData in _lobbySceneMetaData)
            {
                LocationCardHandler card = Instantiate(_locationCardPrefab, _locationCardParent);
                card.SetMetaData(metaData);
                card.Click += () =>
                {
                    _selectedRoom = card.MetaData.Scene;
                };
                _userLimitInputField.characterLimit = 2;
            }
        }

        // private char ValidateInput(string text, int charindex, char addedchar)
        // {
        //     string newText = text + addedchar;
        //     if (!int.TryParse(newText, out int res))
        //     {
        //         return '\0';
        //     }
        //
        //     switch (res)
        //     {
        //         case < 0:
        //             _userLimitInputField.text = "0";
        //             return '\0';
        //         case > 20:
        //             _userLimitInputField.text = "20";
        //             return '\0';
        //         default:
        //             return addedchar;
        //     }
        // }

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
            ushort maxClients = 99;
            if (_userLimitInputField.text != string.Empty)
            {
                 maxClients = (ushort)Math.Max(Convert.ToInt32(_userLimitInputField.text), 1);   
            }
            UILobbyMetaData uiLobbyMeta = new(lobbyName, -1, location, maxClients);
            LobbySelectionMenuHandler.s_instance.CloseCreateRoomScene(uiLobbyMeta);
        }
    }
}