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
using UnityEngine.UI;

namespace TextChat.UI
{
    public class ChatBoxHandler : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _submitButton;
        [SerializeField] private VerticalLayoutGroup _chatMessagesParent;
        [SerializeField] private TextMessageHandler _chatMessageHandlerPrefab;

        private TextChatManager _textChat;
        
        private void Awake()
        {
            LobbyHandler.PostInit += Init;
        }

        private void Init()
        {
            _textChat = LobbyHandler.GetInstance()?.TextChat;
            if (_textChat == null)
            {
                Debug.LogWarning("Text Chat Manager not found!");
                return;
            }

            _textChat.MessagesSynced += () =>
            {
                SyncMessages(_textChat.GetBuffer());
            };
            _textChat.TextMessageReceived += AddTextMessageToList;
            
            SyncMessages(_textChat.GetBuffer());

            _submitButton.onClick.AddListener(SendTextMessage);
        }

        private void SendTextMessage()
        {
            if (_textChat == null)
            {
                return;
            }
            string message = _inputField.text;
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            _textChat.SendTextMessage(message);
            _inputField.text = string.Empty;
        }

        private void SyncMessages(CircularBuffer<TextMessage> buffer)
        {
            foreach (Transform child in _chatMessagesParent.transform)
            {
                Destroy(child.gameObject);
            }
            foreach (TextMessage message in buffer.GetAll())
            {
                AddTextMessageToList(message);
            }
        }

        private void AddTextMessageToList(TextMessage message)
        {
            TextMessageHandler messageHandler =
                Instantiate(_chatMessageHandlerPrefab, _chatMessagesParent.transform);
            messageHandler.SetTextMessage(message);
            //Todo: Lazy Rendering?
        }
    }
}
