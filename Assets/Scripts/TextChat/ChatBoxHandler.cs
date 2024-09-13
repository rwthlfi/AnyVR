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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TextChat
{
    public class ChatBoxHandler : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _submitButton;
        [SerializeField] private TextMeshProUGUI _chatBox;

        private void Start()
        {
            TextChatManager.s_instance.GetBuffer();
            TextChatManager.s_instance.MessagesSynced += () =>
            {
                SyncMessages(TextChatManager.s_instance.GetBuffer());
            };
        }

        private void SyncMessages(CircularBuffer<TextMessage> buffer)
        {
            Debug.Log($"Syncing messages, count {buffer.Count}");
            _chatBox.text = "";
            foreach (TextMessage textMessage in buffer.GetAll())
            {
                Debug.Log($"sync textmessage: {textMessage._message}");
                _chatBox.text += textMessage._message;
            }
        }
    }
}
