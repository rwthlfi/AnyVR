using System.Collections.Generic;
using AnyVR.LobbySystem;
using UnityEngine;

namespace AnyVR.Sample
{
    public class SamplePlayerController : LobbyPlayerController
    {
        private void OnGUI()
        {
            IEnumerable<LobbyPlayerState> playerStates = this.GetGameState().GetPlayerStates<LobbyPlayerState>();

            GUILayout.BeginArea(new Rect(10, 10, 400, Screen.height - 20));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Lobby Players</b>");
            GUILayout.Space(10);

            foreach (LobbyPlayerState player in playerStates)
            {
                GUILayout.BeginVertical("box");

                GUILayout.Label($"Name: {player.Global.Name}");
                GUILayout.Label($"Connected: {player.IsConnectedToVoice.Value}");
                GUILayout.Label($"Mic Published: {player.IsMicrophonePublished.Value}");
                GUILayout.Label($"Mic Muted: {player.IsMicrophoneMuted.Value}");
                GUILayout.Label($"Speaking: {player.IsSpeaking.Value}");

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        public override async void OnStartClient()
        {
            base.OnStartClient();

            // Automatically connects to a LiveKit room

            VoiceConnectionResult res = await ConnectToLiveKitRoom();

            if (res != VoiceConnectionResult.Connected)
            {
                // TODO Show error notification
            }
        }
    }
}
