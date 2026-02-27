using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AnyVR.TextChat.UI
{
    public class ChatBoxHandler : MonoBehaviour
    {
        [Header("UI")] [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _submitButton;
        [SerializeField] private VerticalLayoutGroup _chatMessagesParent;
        [SerializeField] private TextMessageHandler _chatMessageHandlerPrefab;

        private TextChatManager _textChat;

        private void Init()
        {
            // _textChat = LobbyHandler.GetInstance()?.TextChat;
            _textChat = null;
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
