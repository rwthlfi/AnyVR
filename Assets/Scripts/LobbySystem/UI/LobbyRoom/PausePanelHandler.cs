using UnityEngine;

namespace LobbySystem.UI.LobbyRoom
{
    public class PausePanelHandler : MonoBehaviour
    {
        public void Continue()
        {
            UIHandler.s_instance.SetPanelActive(Panel.PausePanel, false);
        }

        public void QuitToMainMenu()
        {
            UIHandler.s_instance.LeaveLobby();
        }
    }
}