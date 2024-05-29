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

namespace AnyVR.PlatformManagement
{
    /// <summary>
    /// Will print information about the current platform to the assigned display object.
    /// If no object is assigned, it will print to the debug console instead.
    /// </summary>
    public class PlatformInfoPrinter : MonoBehaviour
    {
        // Private fields
        
        [SerializeField]
        private TextMeshProUGUI _infoDisplay;



        // Methods
   
        // Start is called before the first frame update
        private void Start()
        {
            InvokeRepeating("PrintPlatformInfo", 0f, 1f);
        }

        private void PrintPlatformInfo()
        {
            string platformInfo = PlatformInfo.GetDeviceDescription();
            if (_infoDisplay) 
            {
                _infoDisplay.text = platformInfo;
            }
            else
            {
                Debug.Log(platformInfo, gameObject);
            }
        }

        public void PrintHelloWorld()
        {
            Debug.Log("Hello World");
        }

    }
}
