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

        [SerializeField] private Button leaveServerButton;
        [SerializeField] private TMP_InputField lobbyNameInputField;
        [SerializeField] private TMP_InputField lobbyPasswordInputField;
        [SerializeField] private TMP_Dropdown lobbySceneDropdown;
        [SerializeField] private Button openLobbyButton;

        [SerializeField] private GameObject lobbyItemPrefab;
        [SerializeField] private RectTransform lobbyListContainer;

        [Header("Password Panel")] [SerializeField]
        private RectTransform passwordPanel;

        [SerializeField] private TMP_InputField passwordInputField;
        [SerializeField] private Button joinButton, cancelButton;


        private Dictionary<Guid, LobbyMetaData> _lobbies = new();

        private void Awake()
        {
            connectionPanel.gameObject.SetActive(true);
            lobbyPanel.gameObject.SetActive(false);
            passwordPanel.gameObject.SetActive(false);
        }

        private void Start()
        {
            cancelButton.onClick.AddListener(() =>
            {
                passwordPanel.gameObject.SetActive(false);
            });
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
            leaveServerButton.onClick.AddListener(() =>
            {
                ConnectionManager.GetInstance()?.LeaveServer();
            });
            lobbySceneDropdown.ClearOptions();
        }

        public void SetAvailableLobbyScenes(IEnumerable<LobbySceneMetaData> lobbyScenes)
        {
            lobbySceneDropdown.options = lobbyScenes.Select(lmd => new TMP_Dropdown.OptionData(lmd.Name)).ToList();
        }

        internal event Action<string, string> OnConnectButtonPressed;
        internal event Action<string, string, string> OnLobbyOpenButtonPressed;
        internal event Action<Guid, string> OnLobbyJoinButtonPressed;

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
                    .AddListener(() => JoinLobby(kvp.Key));
            }
        }

        private void JoinLobby(Guid id)
        {
            if (!_lobbies.TryGetValue(id, out LobbyMetaData lobby))
            {
                return;
            }

            if (!lobby.IsPasswordProtected)
            {
                OnLobbyJoinButtonPressed?.Invoke(id, null);
            }
            else
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(() =>
                {
                    OnLobbyJoinButtonPressed?.Invoke(id, passwordInputField.text);
                });
                passwordPanel.gameObject.SetActive(true);
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