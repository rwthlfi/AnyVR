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
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace LobbySystem.UI
{
    public class ConnectMenuHandler : MonoBehaviour
    {
        [FormerlySerializedAs("_offlineScene")] [SerializeField] private WelcomeScene _welcomeScene;
        [Header("UI")] [SerializeField] private Button _goBtn;
        [SerializeField] private TMP_InputField _fishnetIpInputField;
        [SerializeField] private TMP_InputField _livekitIpInputField;
        [SerializeField] private TMP_InputField _usernameInputField;

        private void Start()
        {
            _fishnetIpInputField.text = "134.130.88.71:7777";
            _livekitIpInputField.text = "134.130.88.71:3030";
            _goBtn.onClick.AddListener(OnGoBtn);
        }
        
        public void OnGoBtn()
        {
            string fishnetAddress = _fishnetIpInputField.text.Trim();
            string liveKitAddress = _livekitIpInputField.text.Trim();
            string userName = _usernameInputField.text.Trim();
            _welcomeScene.OnConnectBtn(fishnetAddress, liveKitAddress, userName);
        }
    }
}
