using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace LobbySystem.UI
{
    public class OfflineScene : MonoBehaviour
    {
        [SerializeField] private LoginManager _loginManager;
        [Header("UI")] [SerializeField] private TMP_InputField _fishnetIpInputField;

        [SerializeField] private TMP_InputField _livekitIpInputField;
        [SerializeField] private TMP_InputField _usernameInputField;

        private void Start()
        {
            _fishnetIpInputField.text = "192.168.0.86:7777";
            _livekitIpInputField.text = "192.168.0.192:3030";
        }

        public void OnGoBtn()
        {
            string fishnetAddress = _fishnetIpInputField.text.Trim();
            string liveKitAddress = _livekitIpInputField.text.Trim();

            string userName = _usernameInputField.text.Trim();
            if (string.IsNullOrEmpty(userName))
            {
                return;
            }

            if (TryParseAddress(fishnetAddress, out (string ip, ushort port) fishnetRes))
            {
                if (TryParseAddress(liveKitAddress, out (string ip, ushort port) liveKitRes))
                {
                    _loginManager.ConnectToServer(fishnetRes, liveKitRes, userName);
                    return;
                }
            }

            Debug.LogError(
                "Invalid client address! Address has to match the pattern <ip>:<port>"); //TODO: display msg graphically
        }

        private static bool TryParseAddress(string address, out (string, ushort) res)
        {
            res = (null, 0);
            if (!Regex.IsMatch(address, ".+:[0-9]+"))
            {
                return false;
            }

            string[] arr = address.Split(':'); // [ip, port]
            const string ipPattern =
                @"^(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}";
            if (!Regex.IsMatch(arr[0], ipPattern))
            {
                return false;
            }

            uint port = uint.Parse(arr[1]);
            if (port > ushort.MaxValue)
            {
                return false;
            }

            res = (arr[0], (ushort)port);
            return true;
        }
    }
}