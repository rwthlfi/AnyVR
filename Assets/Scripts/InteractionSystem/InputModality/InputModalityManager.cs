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

using AnyVR.PlatformManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace AnyVR.InteractionSystem
{
    public class InputModalityManager : MonoBehaviour
    {
        [SerializeField]
        private ContinuousTurnProviderBase _xrTurnProvider;
        [SerializeField]
        private PCTurnProvider _pcTurnProvider;
        [SerializeField]
        private PCInteractionSystem _pcInteractionSystem;

        private void Start()
        {
            PlatformManager.Instance.OnXRInitializationFinished += InitializeTurnProvider;
        }


        private void ToggleXRControls(bool isXRUsed)
        {
            _xrTurnProvider.enabled = isXRUsed;
            _pcTurnProvider.enabled = !isXRUsed;
            _pcInteractionSystem.gameObject.SetActive(!isXRUsed);
            _pcInteractionSystem.enabled = !isXRUsed;
        }


        private void InitializeTurnProvider()
        {
            Debug.Log($"Initializing input: {(PlatformInfo.IsXRPlatform() ? "XR" : "PC")}", gameObject);
            ToggleXRControls(PlatformInfo.IsXRPlatform());
            if (!PlatformInfo.IsXRPlatform())
            {
                GetComponentInChildren<Camera>().gameObject.GetComponent<TrackedPoseDriver>().enabled = false;
            }
        }


    }
}
