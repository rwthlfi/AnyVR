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
                    s_instance = FindObjectOfType<PlatformManager>();
                    if (s_instance == null)
                    {
                        Debug.LogError("PlattformManager not found in scene. Please add it to a gameobject.");
                    }
                }

                return s_instance;
            }
        }

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

            StartCoroutine(InitializeXR());
        }

        public event Action OnXRInitializationFinished;

        private IEnumerator InitializeXR()
        {
            while (XRGeneralSettings.Instance == null)
            {
                yield return null;
            }
            XRManagerSettings settingsManager = XRGeneralSettings.Instance.Manager;
            yield return settingsManager.InitializeLoader();
            OnXRInitializationFinished?.Invoke();
        }
    }
}