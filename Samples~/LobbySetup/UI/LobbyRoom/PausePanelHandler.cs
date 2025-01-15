using UnityEngine;

namespace AnyVr.Samples.LobbySetup
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