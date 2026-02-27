using TMPro;
using UnityEngine;

namespace AnyVR.TextChat.UI
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
            // bool fromServer = message.SenderId == -1;
            // string senderName = fromServer ? "Server" : LobbyHandler.GetInstance()?.GetPlayer(message.SenderId).PlayerName;
            //
            // _textMesh.color = fromServer ? _serverTextColor : _playerTextColor;
            // _textMesh.text = $"[{senderName}]\t{message.Message}";
        }
    }
}
