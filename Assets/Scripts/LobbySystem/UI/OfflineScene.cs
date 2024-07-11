using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace LobbySystem.UI
{
    public class OfflineScene : MonoBehaviour
    {
        [Header("UI")] [SerializeField] private TMP_InputField _fishnetIpInputField;

        [SerializeField] private TMP_InputField _livekitIpInputField;
        [SerializeField] private TMP_InputField _usernameInputField;

        public static event Action<(string, ushort), (string, ushort), string> OnLoginRequest;
        
        private void Start()
        {
            _fishnetIpInputField.text = "127.0.0.1:7777";
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
                    OnLoginRequest?.Invoke(fishnetRes, liveKitRes, userName);
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