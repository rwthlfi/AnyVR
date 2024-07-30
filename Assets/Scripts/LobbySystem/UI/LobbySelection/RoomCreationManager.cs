using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LobbySystem.UI.LobbySelection
{
    public class RoomCreationManager : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Slider _userLimitSlider;

        [SerializeField] private LocationCardHandler _locationCardPrefab;
        [SerializeField] private Transform _locationCardParent;

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
            }
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
            ushort maxClients = (ushort)_userLimitSlider.value;
            UILobbyMetaData uiLobbyMeta = new(lobbyName, -1, location, maxClients);
            
            LobbySelectionMenuHandler.s_instance.CloseCreateRoomScene(uiLobbyMeta);
        }
    }
}