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

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Management;

namespace AnyVR.PlatformManagement
{
    internal sealed class PlatformManager : MonoBehaviour
    {
        private const string Tag = nameof(PlatformManager);
        
        private static XRManagerSettings _xrManager;

        private void Awake()
        {
            Debug.Log("Initializing XR...");

            // XRGeneralSettings.Instance is null in headless server builds
            if (XRGeneralSettings.Instance != null)
            {
                _xrManager = XRGeneralSettings.Instance.Manager;
            }
            
            _xrManager?.InitializeLoaderSync();
            
            IsXrStartupAttempted = true;
            DontDestroyOnLoad(gameObject);
            
            Debug.Log($"XR initialization {(IsXrActive? "succeeded" : "failed")}");
        }

        /// <summary>
        /// True after XR initialization has been attempted, regardless of success.
        /// </summary>
        internal static bool IsXrStartupAttempted { get; private set; }
        
        /// <summary>
        /// True if XR successfully initialized and an active loader is available.
        /// </summary>
        public static bool IsXrActive => _xrManager != null && _xrManager.activeLoader != null;
    }
}