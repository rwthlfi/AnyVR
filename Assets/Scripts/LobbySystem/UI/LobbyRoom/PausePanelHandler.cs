using UnityEngine;

namespace LobbySystem.UI.LobbyRoom
{
    public class PausePanelHandler : MonoBehaviour
    {
        [SerializeField] private GameObject _pausePanel;

        private bool _pauseMenuActive;

        private void Start()
        {
            _pauseMenuActive = gameObject.activeSelf;
            if (_pauseMenuActive)
            {
                TogglePauseMenu();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }
        }

        public void Continue()
        {
            _pauseMenuActive = false;
            _pausePanel.gameObject.SetActive(_pauseMenuActive);
        }

        public void TogglePauseMenu()
        {
            _pauseMenuActive = !_pauseMenuActive;
            _pausePanel.gameObject.SetActive(_pauseMenuActive);
        }

        public void QuitToMainMenu()
        {
            UIHandler.s_instance.LeaveLobby();
        }
    }
}