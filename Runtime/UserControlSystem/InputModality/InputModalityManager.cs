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
using System;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace AnyVR.UserControlSystem
{
    /// <summary>
    ///     Manages the user input based on the XR platform availability. Supports PC and XR input modalities.
    /// </summary>
    public class InputModalityManager : MonoBehaviour
    {
        [SerializeField] private ContinuousTurnProvider _xrTurnProvider;
        [SerializeField] private PCTurnProvider _pcTurnProvider;
        [SerializeField] private PCInteractionSystem _pcInteractionSystem;
        [SerializeField] private TrackedPoseDriver _cameraTrackedPoseDriver;
        [SerializeField] private Transform _xrGazeInteractionOrigin;
        [SerializeField] private Transform _pcGazeInteractionOrigin;
        [SerializeField] private XRGazeInteractor _gazeInteractor;

        private void Start()
        {
            PlatformManager.Instance.OnXRInitializationFinished += InitializeUserInput;
        }

        /// <summary>
        ///     Initializes the user input based on the XR platform availability.
        /// </summary>
        private void InitializeUserInput()
        {
            bool isXRActive = PlatformInfo.IsXRPlatform();
            ToggleXRControls(isXRActive);
            InitializeTurnProvider(isXRActive);
            InitializeGazeInteractor(isXRActive);
            SetCursorVisibility(false);
        }

        private void ToggleXRControls(bool isXRActive)
        {
            _xrTurnProvider.enabled = isXRActive;
            _pcTurnProvider.enabled = !isXRActive;
            _pcInteractionSystem.gameObject.SetActive(!isXRActive);
            _pcInteractionSystem.enabled = !isXRActive;
        }

        private void InitializeTurnProvider(bool isXRActive)
        {
            _cameraTrackedPoseDriver.enabled = isXRActive;
        }

        private void InitializeGazeInteractor(bool isXRActive)
        {
            _gazeInteractor.rayOriginTransform = isXRActive ? _xrGazeInteractionOrigin : _pcGazeInteractionOrigin;
        }

        private static void SetCursorVisibility(bool visible)
        {
            Cursor.visible = visible;
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}