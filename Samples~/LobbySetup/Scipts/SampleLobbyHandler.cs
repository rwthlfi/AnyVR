using AnyVR.LobbySystem;
using UnityEngine;

namespace AnyVR.Sample
{
    public class SampleLobbyHandler : LobbyHandler
    {
        private void OnGUI()
        {
            const float buttonWidth = 100f;
            const float buttonHeight = 50f;

            float x = Screen.width - buttonWidth - 10;
            float y = Screen.height - buttonHeight - 10;

            if (!GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Leave"))
                return;
            
            Leave();
        }
    }
}
