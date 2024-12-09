// AnyVR is a multiuser, multiplatform XR framework.
// Copyright (C) 2024 Engineering Hydrology, RWTH Aachen University.
// 
// AnyVR is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published
// by the Free Software Foundation, either version 3 of the License,
// or (at your option) any later version.
// 
// AnyVR is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANT-
// ABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with AnyVR.
// If not, see <https://www.gnu.org/licenses/>.

using LobbySystem;
using TMPro;
using UnityEngine;

namespace TextChat.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextMessageHandler : MonoBehaviour
    {
        [SerializeField] private Color _serverTextColor;
        [SerializeField] private Color _playerTextColor;

        private TextMeshProUGUI _textMesh;

        private void Awake()
        {
            _textMesh = GetComponent<TextMeshProUGUI>();
        }

        public void SetTextMessage(TextMessage message)
        {
            bool fromServer = message.SenderId == -1;
            string senderName = fromServer ? "Server" : PlayerNameTracker.GetPlayerName(message.SenderId);

            _textMesh.color = fromServer ? _serverTextColor : _playerTextColor;
            _textMesh.text = $"[{senderName}]\t{message.Message}";
        }
    }
}