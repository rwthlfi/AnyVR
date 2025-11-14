using System;
using AnyVR.LobbySystem;
using UnityEngine;

namespace AnyVR.Sample
{
    public class SamplePlayerController : LobbyPlayerController
    {
        private void OnGUI()
        {
            const int buttonWidth = 120;
            const int buttonHeight = 40;
            const int margin = 20;

            Rect buttonRect = new(
                Screen.width - buttonWidth - margin,
                Screen.height - buttonHeight - margin,
                buttonWidth,
                buttonHeight
            );

            if (GUI.Button(buttonRect, "Leave"))
            {
                Debug.Log("Button clicked!");
            }
        }
    }
}
