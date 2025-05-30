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

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace AnyVR.PlatformManagement
{
    public class PlatformManager : MonoBehaviour
    {
        private static PlatformManager s_instance;

        public static PlatformManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindAnyObjectByType<PlatformManager>();
                    if (s_instance == null)
                    {
                        GameObject platformManagerObject = new GameObject("PlatformManager");
                        s_instance = platformManagerObject.AddComponent<PlatformManager>();
                    }
                }
                return s_instance;
            }
        }

        /// <summary>
        /// Invoked when XR has been initialized.
        /// </summary>
        public event Action OnXRInitializationFinished;

        protected virtual void Awake()
        {
            bool isServer = Application.platform == RuntimePlatform.LinuxServer ||
                            Application.platform == RuntimePlatform.WindowsServer;
            if ((s_instance != null && s_instance != this) || isServer)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void Start()
        {
            StartCoroutine(TryInitializeXR());
        }

        
        
        private IEnumerator TryInitializeXR()
        {
            Debug.Log("[PlatformManager] Trying to start XR initialization...");
            // Waits until XR starts to initialize.
            while (!IsXRInitializing())
            {
                yield return null;
            }

            Debug.Log("[PlatformManager] XR initialization started.");
            // Waits until XR is initialized.
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.Log("[PlatformManager] XR initialization failed.");
                PlatformInfo.s_xRInitializationTCS.SetResult(false);
                OnXRInitializationFinished?.Invoke();
                yield break;
            }
            Debug.Log("[PlatformManager] XR initialization finished.");
            PlatformInfo.s_xRInitializationTCS.SetResult(XRSettings.isDeviceActive);
            OnXRInitializationFinished?.Invoke();
        }

        private bool IsXRInitializing()
        {
            XRManagerSettings xrManager = XRGeneralSettings.Instance.Manager;
            return xrManager != null;
        }
    }
}