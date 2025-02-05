using AnyVr.LobbySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVr.Samples.NewLobbySetup
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Connection Panel")] [SerializeField]
        private RectTransform connectionPanel;

        [SerializeField] private TMP_InputField serverIpInputField;
        [SerializeField] private TMP_InputField userNameInputField;
        [SerializeField] private Button connectButton;

        [Header("Lobby Panel")] [SerializeField]
        private RectTransform lobbyPanel;

        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private TMP_InputField lobbyPasswordInputField;
        [SerializeField] private TMP_Dropdown lobbySceneDropdown;
        [SerializeField] private Button openLobbyButton;

        [SerializeField] private GameObject lobbyItemPrefab;
        [SerializeField] private RectTransform lobbyListContainer;

        private Dictionary<Guid, LobbyMetaData> _lobbies = new();

        private void Start()
        {
            connectionPanel.gameObject.SetActive(true);
            lobbyPanel.gameObject.SetActive(false);
            connectButton.onClick.AddListener(() =>
            {
                OnConnectButtonPressed?.Invoke(serverIpInputField.text, userNameInputField.text);
            });
            openLobbyButton.onClick.AddListener(() =>
            {
                string lobbyName = lobbyNameInputField.text;
                string lobbyPassword = lobbyPasswordInputField.text;
                string lobbyScene = lobbySceneDropdown.options[lobbySceneDropdown.value].text;

                OnLobbyOpenButtonPressed?.Invoke(lobbyName, lobbyPassword, lobbyScene);
            });
            lobbySceneDropdown.ClearOptions();
        }

        public void SetAvailableLobbyScenes(IEnumerable<LobbySceneMetaData> lobbyScenes)
        {
            lobbySceneDropdown.options = lobbyScenes.Select(lmd => new TMP_Dropdown.OptionData(lmd.Name)).ToList();
        }

        internal event Action<string, string> OnConnectButtonPressed;
        internal event Action<string, string, string> OnLobbyOpenButtonPressed;
        internal event Action<Guid> OnLobbyJoinButtonPressed;

        private void PopulateLobbyList()
        {
            foreach (Transform child in lobbyListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (KeyValuePair<Guid, LobbyMetaData> kvp in _lobbies)
            {
                GameObject entry = Instantiate(lobbyItemPrefab, lobbyListContainer);
                entry.transform.Find("LobbyName").GetComponent<TextMeshProUGUI>().text = kvp.Value.Name;
                entry.transform.Find("JoinButton").GetComponent<Button>().onClick
                    .AddListener(() => OnLobbyJoinButtonPressed?.Invoke(kvp.Key));
            }
        }

        public void SetLobbies(Dictionary<Guid, LobbyMetaData> newLobbies)
        {
            _lobbies = newLobbies;
            PopulateLobbyList();
        }

        public void SetLobbyPanelActive(bool isActive)
        {
            lobbyPanel.gameObject.SetActive(isActive);
            connectionPanel.gameObject.SetActive(!isActive);
        }
    }
}